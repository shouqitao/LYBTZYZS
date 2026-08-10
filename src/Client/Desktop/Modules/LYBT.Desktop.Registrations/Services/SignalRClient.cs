using LYBT.Desktop.Foundation.Application;
using LYBT.Desktop.Foundation.Security;
using LYBT.Desktop.Registrations.Events;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using Prism.Events;

namespace LYBT.Desktop.Registrations.Services;

/// <summary>
/// 挂号实时通知客户端 (US-REG-008)。
/// 连接 RegistrationHub，收到挂号推送后发布刷新事件；连接失败/断线时自动降级为轮询。
/// </summary>
public interface ISignalRClient
{
    /// <summary>以指定医生身份建立 SignalR 连接（幂等，重复调用无副作用）。</summary>
    Task StartAsync(Guid doctorId, CancellationToken cancellationToken = default);

    /// <summary>断开连接并停止降级轮询。</summary>
    Task StopAsync();
}

/// <inheritdoc cref="ISignalRClient" />
public sealed class SignalRClient : ISignalRClient
{
    private const string HubPath = "hubs/registration";
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(15);

    private readonly IApplicationStateService _applicationState;
    private readonly IEventAggregator _eventAggregator;
    private readonly ILogger<SignalRClient> _logger;

    private HubConnection? _connection;
    private CancellationTokenSource? _pollingCts;
    private bool _stopping;

    public SignalRClient(
        IApplicationStateService applicationState,
        IEventAggregator eventAggregator,
        ILogger<SignalRClient> logger)
    {
        _applicationState = applicationState ?? throw new ArgumentNullException(nameof(applicationState));
        _eventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public async Task StartAsync(Guid doctorId, CancellationToken cancellationToken = default)
    {
        if (doctorId == Guid.Empty || _connection is not null)
            return;

        _stopping = false;
        _connection = new HubConnectionBuilder()
            .WithUrl(BuildHubUrl(doctorId))
            .WithAutomaticReconnect([TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(30)])
            .Build();

        _connection.On<object>("NewRegistration", _ => OnNotificationReceived());
        _connection.On<Guid, string>("RegistrationStatusChanged", (_, _) => OnNotificationReceived());
        _connection.Reconnected += OnReconnectedAsync;
        _connection.Closed += OnClosedAsync;

        try
        {
            await _connection.StartAsync(cancellationToken);
            _logger.LogInformation("[SIGR] SignalR 已连接，DoctorId={DoctorId}", doctorId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[SIGR] SignalR 连接失败，降级为轮询刷新");
            StartPolling();
        }
    }

    /// <inheritdoc/>
    public async Task StopAsync()
    {
        _stopping = true;
        StopPolling();

        var connection = _connection;
        _connection = null;
        if (connection is null)
            return;

        try
        {
            await connection.StopAsync();
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "[SIGR] 停止 SignalR 连接时异常");
        }
        await connection.DisposeAsync();
    }

    private Uri BuildHubUrl(Guid doctorId)
    {
        var baseUrl = _applicationState.ApiBaseUrl.TrimEnd('/');
        return new Uri($"{baseUrl}/{HubPath}?doctorId={doctorId}");
    }

    private void OnNotificationReceived()
    {
        _logger.LogDebug("[SIGR] 收到挂号变更推送，触发待诊列表刷新");
        _eventAggregator.GetEvent<RegistrationRefreshedEvent>().Publish();
    }

    private Task OnReconnectedAsync(string? _)
    {
        _logger.LogInformation("[SIGR] SignalR 重连成功，恢复推送通道");
        StopPolling();
        return Task.CompletedTask;
    }

    private Task OnClosedAsync(Exception? exception)
    {
        if (_stopping)
            return Task.CompletedTask;

        _logger.LogWarning(exception, "[SIGR] SignalR 连接关闭，降级为轮询刷新");
        StartPolling();
        return Task.CompletedTask;
    }

    private void StartPolling()
    {
        StopPolling();

        var cts = new CancellationTokenSource();
        _pollingCts = cts;
        _ = PollLoopAsync(cts.Token);
    }

    private void StopPolling()
    {
        _pollingCts?.Cancel();
        _pollingCts?.Dispose();
        _pollingCts = null;
    }

    private async Task PollLoopAsync(CancellationToken cancellationToken)
    {
        using var timer = new PeriodicTimer(PollInterval);
        try
        {
            while (await timer.WaitForNextTickAsync(cancellationToken))
            {
                _logger.LogDebug("[SIGR] 降级轮询刷新待诊列表");
                _eventAggregator.GetEvent<RegistrationRefreshedEvent>().Publish();
            }
        }
        catch (OperationCanceledException)
        {
            // 正常停止
        }
    }
}
