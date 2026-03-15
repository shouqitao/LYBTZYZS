using LYBT.Desktop.Contracts;
using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Foundation.Application;
using LYBT.Desktop.Foundation.HealthCheck;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Shell.Services.HealthCheck;

/// <summary>
/// 健康检查协调器
/// 负责管理API健康检查的调度和状态，从MainWindowViewModel提取的独立服务
/// OpenSpec: implement-local-mode - 本地模式下跳过 API 健康检查
/// </summary>
public class HealthCheckCoordinator : IHealthCheckCoordinator
{
    private readonly IApiHealthCheckService _apiHealthCheckService;
    private readonly IApplicationTickService _tickService;
    private readonly IApplicationStateService _applicationStateService;
    private readonly ILogger<HealthCheckCoordinator> _logger;
    private readonly IConnectionModeProvider _connectionModeProvider;

    private ApiHealthStatus _currentStatus = ApiHealthStatus.Checking;
    private long _lastHealthCheckTick;
    private bool _isRunning;
    private bool _disposed;

    /// <summary>默认健康检查间隔（秒）</summary>
    private const int DefaultCheckIntervalSeconds = 10;

    /// <summary>健康检查超时时间（毫秒）</summary>
    private const int HealthCheckTimeoutMs = 5000;

    /// <summary>构造函数</summary>
    public HealthCheckCoordinator(
        IApiHealthCheckService apiHealthCheckService,
        IApplicationTickService tickService,
        IApplicationStateService applicationStateService,
        IConnectionModeProvider connectionModeProvider,
        ILogger<HealthCheckCoordinator> logger)
    {
        _apiHealthCheckService = apiHealthCheckService ?? throw new ArgumentNullException(nameof(apiHealthCheckService));
        _tickService = tickService ?? throw new ArgumentNullException(nameof(tickService));
        _applicationStateService = applicationStateService ?? throw new ArgumentNullException(nameof(applicationStateService));
        _connectionModeProvider = connectionModeProvider ?? throw new ArgumentNullException(nameof(connectionModeProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public ApiHealthStatus CurrentStatus => _currentStatus;

    /// <inheritdoc />
    public int CheckIntervalSeconds => DefaultCheckIntervalSeconds;

    /// <inheritdoc />
    public event EventHandler<HealthStatusChangedEventArgs>? StatusChanged;

    /// <inheritdoc />
    public void Start()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(HealthCheckCoordinator));
        }

        if (_isRunning)
        {
            _logger.LogDebug("健康检查协调器已在运行中");
            return;
        }

        // OpenSpec: implement-local-mode - 本地模式直接设置为健康状态
        if (_connectionModeProvider.CurrentMode == ConnectionMode.Local)
        {
            var previousStatus = _currentStatus;
            _currentStatus = ApiHealthStatus.Healthy;
            _isRunning = true;
            RaiseStatusChanged(previousStatus, _currentStatus);
            SyncToApplicationState(ApiHealthStatus.Healthy);
            _logger.LogInformation("健康检查协调器已启动 [本地模式，跳过 API 检查]");
            return;
        }

        _lastHealthCheckTick = _tickService.TickCount;
        _tickService.Tick += OnTick;
        _isRunning = true;

        // 延迟1秒后执行首次健康检查，避免启动时阻塞
        _ = Task.Run(async () =>
        {
            await Task.Delay(1000);
            await CheckNowAsync();
        });

        _logger.LogInformation("健康检查协调器已启动 [间隔: {Interval}秒]", CheckIntervalSeconds);
    }

    /// <inheritdoc />
    public void Stop()
    {
        if (!_isRunning)
        {
            return;
        }

        _tickService.Tick -= OnTick;
        _isRunning = false;

        _logger.LogInformation("健康检查协调器已停止");
    }

    /// <inheritdoc />
    public async Task CheckNowAsync()
    {
        if (_disposed)
        {
            return;
        }

        // OpenSpec: implement-local-mode - 本地模式直接返回健康状态
        if (_connectionModeProvider.CurrentMode == ConnectionMode.Local)
        {
            if (_currentStatus != ApiHealthStatus.Healthy)
            {
                var previousStatus = _currentStatus;
                _currentStatus = ApiHealthStatus.Healthy;
                RaiseStatusChanged(previousStatus, _currentStatus);
                SyncToApplicationState(ApiHealthStatus.Healthy);
            }
            return;
        }

        try
        {
            var previousStatus = _currentStatus;
            _currentStatus = ApiHealthStatus.Checking;

            // 如果状态变更，触发事件
            if (previousStatus != ApiHealthStatus.Checking)
            {
                RaiseStatusChanged(previousStatus, _currentStatus);
            }

            var status = await _apiHealthCheckService.CheckHealthAsync(timeout: HealthCheckTimeoutMs);

            previousStatus = _currentStatus;
            _currentStatus = status;

            if (previousStatus != _currentStatus)
            {
                RaiseStatusChanged(previousStatus, _currentStatus);
            }

            // OpenSpec: refactor-startup-connection-resilience - 同步状态到ApplicationStateService
            SyncToApplicationState(status);

            if (status == ApiHealthStatus.Unhealthy)
            {
                _logger.LogWarning("API健康检查失败: {ErrorMessage}", _apiHealthCheckService.LastErrorMessage);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "执行API健康检查时发生异常");

            var previousStatus = _currentStatus;
            _currentStatus = ApiHealthStatus.Unhealthy;

            if (previousStatus != _currentStatus)
            {
                RaiseStatusChanged(previousStatus, _currentStatus);
            }

            // OpenSpec: refactor-startup-connection-resilience - 异常时也同步状态
            SyncToApplicationState(ApiHealthStatus.Unhealthy, ex.Message);
        }
    }

    /// <summary>Tick事件处理 - 定时触发健康检查</summary>
    private void OnTick(object? sender, ApplicationTickEventArgs e)
    {
        if (!_isRunning || _disposed)
        {
            return;
        }

        if (e.TickCount - _lastHealthCheckTick >= CheckIntervalSeconds)
        {
            _lastHealthCheckTick = e.TickCount;
            _ = CheckNowAsync();
        }
    }

    /// <summary>触发状态变更事件</summary>
    private void RaiseStatusChanged(ApiHealthStatus previousStatus, ApiHealthStatus currentStatus)
    {
        StatusChanged?.Invoke(this, new HealthStatusChangedEventArgs
        {
            PreviousStatus = previousStatus,
            CurrentStatus = currentStatus
        });
    }

    /// <summary>
    /// 同步健康检查结果到ApplicationStateService
    /// OpenSpec: refactor-startup-connection-resilience - 状态中枢同步
    /// </summary>
    private void SyncToApplicationState(ApiHealthStatus status, string? errorOverride = null)
    {
        try
        {
            _applicationStateService.IsApiHealthy = status == ApiHealthStatus.Healthy;
            _applicationStateService.LastHealthCheckTime = DateTime.Now;

            switch (status)
            {
                case ApiHealthStatus.Healthy:
                    _applicationStateService.ConnectionStatus = "已连接";
                    _applicationStateService.LastError = null;
                    break;
                case ApiHealthStatus.Unhealthy:
                    var error = errorOverride ?? _apiHealthCheckService.LastErrorMessage ?? "连接失败";
                    _applicationStateService.ConnectionStatus = $"连接失败: {error}";
                    _applicationStateService.LastError = error;
                    break;
                default:
                    _applicationStateService.ConnectionStatus = "正在检查...";
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "同步状态到ApplicationStateService失败");
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        Stop();
        _disposed = true;

        _logger.LogDebug("健康检查协调器已释放");
    }
}
