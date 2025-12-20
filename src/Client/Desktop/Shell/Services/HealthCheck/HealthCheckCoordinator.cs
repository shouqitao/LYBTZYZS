using LYBT.Desktop.Foundation.HealthCheck;
using LYBT.Desktop.Contracts.Services;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Shell.Services.HealthCheck;

/// <summary>
/// 健康检查协调器
/// 负责管理API健康检查的调度和状态，从MainWindowViewModel提取的独立服务
/// </summary>
public class HealthCheckCoordinator : IHealthCheckCoordinator
{
    private readonly IApiHealthCheckService _apiHealthCheckService;
    private readonly IApplicationTickService _tickService;
    private readonly ILogger<HealthCheckCoordinator> _logger;

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
        ILogger<HealthCheckCoordinator> logger)
    {
        _apiHealthCheckService = apiHealthCheckService ?? throw new ArgumentNullException(nameof(apiHealthCheckService));
        _tickService = tickService ?? throw new ArgumentNullException(nameof(tickService));
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

        _lastHealthCheckTick = _tickService.TickCount;
        _tickService.Tick += OnTick;
        _isRunning = true;

        // 立即执行一次健康检查
        _ = Task.Run(CheckNowAsync);

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
