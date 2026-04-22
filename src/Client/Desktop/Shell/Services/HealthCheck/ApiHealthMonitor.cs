using System.Diagnostics;
using LYBT.Desktop.Contracts;
using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Foundation.HealthCheck;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Shell.Services.HealthCheck;

/// <summary>
/// 全局 API 健康监控器实现
/// 统一管理远程 API 可用性检测，支持订阅模式和断路器保护
/// </summary>
public sealed class ApiHealthMonitor : IApiHealthMonitor
{
    private readonly IApiHealthCheckService _healthCheckService;
    private readonly IConnectionModeProvider _connectionModeProvider;
    private readonly ILogger<ApiHealthMonitor> _logger;

    private Timer? _checkTimer;
    private readonly SemaphoreSlim _checkLock = new(1, 1);
    private CancellationTokenSource? _cts;
    private bool _disposed;

    private ApiMonitorHealthStatus _status = ApiMonitorHealthStatus.Checking;
    private ApiConnectionState _connectionState = ApiConnectionState.Unknown;
    private DateTime? _lastCheckTime;
    private DateTime? _nextCheckTime;
    private string? _lastError;
    private bool _isChecking;
    private int _consecutiveFailures;

    private CircuitState _circuitState = CircuitState.Closed;
    private DateTime? _circuitOpenedAt;

    public ApiHealthMonitor(
        IApiHealthCheckService healthCheckService,
        IConnectionModeProvider connectionModeProvider,
        ILogger<ApiHealthMonitor> logger)
    {
        _healthCheckService = healthCheckService;
        _connectionModeProvider = connectionModeProvider;
        _logger = logger;
    }

    public ApiMonitorHealthStatus Status => _status;
    public ApiConnectionState ConnectionState => _connectionState;
    public DateTime? LastCheckTime => _lastCheckTime;
    public DateTime? NextCheckTime => _nextCheckTime;
    public int ConsecutiveFailures => _consecutiveFailures;
    public string? LastError => _lastError;
    public bool IsChecking => _isChecking;

    public TimeSpan CheckInterval { get; set; } = TimeSpan.FromSeconds(10);
    public TimeSpan CheckTimeout { get; set; } = TimeSpan.FromSeconds(5);
    public int CircuitBreakerThreshold { get; set; } = 3;
    public TimeSpan CircuitBreakerRecoveryTime { get; set; } = TimeSpan.FromSeconds(30);

    public event EventHandler<ApiHealthMonitorChangedEventArgs>? StatusChanged;
    public event EventHandler<HealthCheckCompletedEventArgs>? CheckCompleted;

#pragma warning disable CS1998
    public async Task StartMonitoringAsync(CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        _cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        _logger.LogInformation("[HEALTH-MON] 启动 API 健康监控，间隔: {Interval}s", CheckInterval.TotalSeconds);

        _checkTimer = new Timer(
            async _ => await PerformCheckAsync(),
            null,
            TimeSpan.Zero,
            CheckInterval);

        _nextCheckTime = DateTime.UtcNow.Add(CheckInterval);
    }
#pragma warning restore CS1998

#pragma warning disable CS1998
    public async Task StopMonitoringAsync()
    {
        if (_disposed) return;

        _logger.LogInformation("[HEALTH-MON] 停止 API 健康监控");
        _checkTimer?.Dispose();
        _checkTimer = null;

        if (_cts != null)
        {
            _cts.Cancel();
            _cts.Dispose();
            _cts = null;
        }

        UpdateState(ApiMonitorHealthStatus.Unhealthy, ApiConnectionState.Disconnected, "监控已停止");
    }
#pragma warning restore CS1998

    public async Task<ApiMonitorHealthStatus> ForceCheckAsync()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        _logger.LogDebug("[HEALTH-MON] 强制执行健康检查");
        return await PerformCheckAsync();
    }

    public void ResetCircuitBreaker()
    {
        _logger.LogInformation("[HEALTH-MON] 重置断路器");
        _circuitState = CircuitState.Closed;
        _circuitOpenedAt = null;
        _consecutiveFailures = 0;
    }

    private async Task<ApiMonitorHealthStatus> PerformCheckAsync()
    {
        if (_disposed) return ApiMonitorHealthStatus.Unhealthy;

        if (!await _checkLock.WaitAsync(0))
        {
            _logger.LogDebug("[HEALTH-MON] 检查已在进行中，跳过");
            return _status;
        }

        try
        {
            _isChecking = true;
            UpdateState(ApiMonitorHealthStatus.Checking, ApiConnectionState.Checking, null);

            if (_connectionModeProvider.CurrentMode == ConnectionMode.Local)
            {
                _logger.LogDebug("[HEALTH-MON] 本地模式，跳过 API 检查");
                OnSuccess();
                return ApiMonitorHealthStatus.Healthy;
            }

            if (_circuitState == CircuitState.Open)
            {
                if (_circuitOpenedAt.HasValue &&
                    DateTime.UtcNow - _circuitOpenedAt.Value >= CircuitBreakerRecoveryTime)
                {
                    _logger.LogInformation("[HEALTH-MON] 断路器恢复时间已到，转为半开状态");
                    _circuitState = CircuitState.HalfOpen;
                }
                else
                {
                    _logger.LogDebug("[HEALTH-MON] 断路器开启，快速失败");
                    return ApiMonitorHealthStatus.Unhealthy;
                }
            }

            var sw = Stopwatch.StartNew();
            var timeoutMs = (int)CheckTimeout.TotalMilliseconds;

            try
            {
                var apiStatus = await _healthCheckService.CheckHealthAsync(timeoutMs);
                sw.Stop();

                var healthStatus = apiStatus switch
                {
                    ApiHealthStatus.Healthy => ApiMonitorHealthStatus.Healthy,
                    ApiHealthStatus.Unhealthy => ApiMonitorHealthStatus.Unhealthy,
                    _ => ApiMonitorHealthStatus.Checking
                };

                if (healthStatus == ApiMonitorHealthStatus.Healthy)
                {
                    OnSuccess();
                    CheckCompleted?.Invoke(this, new HealthCheckCompletedEventArgs
                    {
                        Status = ApiMonitorHealthStatus.Healthy,
                        Duration = sw.Elapsed
                    });
                    return ApiMonitorHealthStatus.Healthy;
                }
                else
                {
                    OnFailure(_healthCheckService.LastErrorMessage ?? "API 返回不健康状态");
                    CheckCompleted?.Invoke(this, new HealthCheckCompletedEventArgs
                    {
                        Status = ApiMonitorHealthStatus.Unhealthy,
                        Duration = sw.Elapsed,
                        ErrorMessage = _lastError
                    });
                    return ApiMonitorHealthStatus.Unhealthy;
                }
            }
            catch (Exception ex)
            {
                sw.Stop();
                OnFailure(ex.Message);
                CheckCompleted?.Invoke(this, new HealthCheckCompletedEventArgs
                {
                    Status = ApiMonitorHealthStatus.Unhealthy,
                    Duration = sw.Elapsed,
                    ErrorMessage = ex.Message
                });
                return ApiMonitorHealthStatus.Unhealthy;
            }
        }
        finally
        {
            _isChecking = false;
            _nextCheckTime = DateTime.UtcNow.Add(CheckInterval);
            _checkLock.Release();
        }
    }

    private void OnSuccess()
    {
        _consecutiveFailures = 0;
        _lastError = null;

        if (_circuitState == CircuitState.HalfOpen)
        {
            _logger.LogInformation("[HEALTH-MON] 断路器恢复成功，转为关闭状态");
            _circuitState = CircuitState.Closed;
            _circuitOpenedAt = null;
        }

        UpdateState(ApiMonitorHealthStatus.Healthy, ApiConnectionState.Connected, null);
    }

    private void OnFailure(string errorMessage)
    {
        _consecutiveFailures++;
        _lastError = errorMessage;

        _logger.LogWarning("[HEALTH-MON] 健康检查失败 ({Count}/{Threshold}): {Error}",
            _consecutiveFailures, CircuitBreakerThreshold, errorMessage);

        if (_circuitState == CircuitState.HalfOpen)
        {
            _logger.LogWarning("[HEALTH-MON] 断路器半开检查失败，重新开启");
            _circuitState = CircuitState.Open;
            _circuitOpenedAt = DateTime.UtcNow;
        }
        else if (_consecutiveFailures >= CircuitBreakerThreshold && _circuitState == CircuitState.Closed)
        {
            _logger.LogError("[HEALTH-MON] 连续失败 {Count} 次，断路器开启", _consecutiveFailures);
            _circuitState = CircuitState.Open;
            _circuitOpenedAt = DateTime.UtcNow;
        }

        UpdateState(ApiMonitorHealthStatus.Unhealthy, ApiConnectionState.Disconnected, errorMessage);
    }

    private void UpdateState(ApiMonitorHealthStatus newStatus, ApiConnectionState newConnectionState, string? error)
    {
        var oldStatus = _status;
        var oldConnectionState = _connectionState;

        _status = newStatus;
        _connectionState = newConnectionState;
        _lastCheckTime = DateTime.UtcNow;

        if (oldStatus != newStatus || oldConnectionState != newConnectionState)
        {
            _logger.LogInformation(
                "[HEALTH-MON] 状态变更: {OldStatus}/{OldState} -> {NewStatus}/{NewState}",
                oldStatus, oldConnectionState, newStatus, newConnectionState);

            StatusChanged?.Invoke(this, new ApiHealthMonitorChangedEventArgs
            {
                OldStatus = oldStatus,
                NewStatus = newStatus,
                OldConnectionState = oldConnectionState,
                NewConnectionState = newConnectionState,
                ErrorMessage = error
            });
        }
    }

    public void Dispose()
    {
        if (_disposed) return;

        _disposed = true;
        _logger.LogInformation("[HEALTH-MON] 释放资源");

        _checkTimer?.Dispose();
        _cts?.Cancel();
        _cts?.Dispose();
        _checkLock.Dispose();
    }
}
