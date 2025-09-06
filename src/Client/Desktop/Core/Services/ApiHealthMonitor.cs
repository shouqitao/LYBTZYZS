using LYBT.Desktop.Core.Interfaces.Services;

namespace LYBT.Desktop.Core.Services {

    /// <summary>
    /// API健康监控服务 - 专门负责监控API连接状态
    /// </summary>
    public sealed class ApiHealthMonitor : IApiHealthMonitor, IDisposable {
        private readonly IAuthenticationService _authService;
        private readonly Timer _healthCheckTimer;
        private readonly SemaphoreSlim _checkSemaphore = new(1, 1);

        private bool _isOnline;
        private string _statusMessage = "正在检测API连接...";
        private DateTime _lastCheckTime = DateTime.MinValue;
        private int _consecutiveFailures = 0;

        public event EventHandler<ApiHealthStatusChangedEventArgs>? StatusChanged;

        public bool IsOnline => _isOnline;
        public string StatusMessage => _statusMessage;
        public DateTime LastCheckTime => _lastCheckTime;
        public int ConsecutiveFailures => _consecutiveFailures;

        private const int CheckIntervalSeconds = 5;
        private const int MaxConsecutiveFailures = 3;

        public ApiHealthMonitor(IAuthenticationService authService) {
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));

            // 创建定时器但暂不启动
            _healthCheckTimer = new Timer(
                async _ => await PerformHealthCheckAsync(),
                null,
                Timeout.Infinite,
                Timeout.Infinite);
        }

        /// <summary>
        /// 启动健康监控
        /// </summary>
        public async Task StartMonitoringAsync() {
            // 立即执行第一次检查
            await PerformHealthCheckAsync();

            // 启动定时器
            _healthCheckTimer.Change(
                TimeSpan.FromSeconds(CheckIntervalSeconds),
                TimeSpan.FromSeconds(CheckIntervalSeconds));
        }

        /// <summary>
        /// 停止健康监控
        /// </summary>
        public void StopMonitoring() {
            _healthCheckTimer.Change(Timeout.Infinite, Timeout.Infinite);
        }

        /// <summary>
        /// 手动触发健康检查
        /// </summary>
        public async Task<bool> CheckHealthAsync() {
            await PerformHealthCheckAsync();
            return _isOnline;
        }

        private async Task PerformHealthCheckAsync() {
            // 防止并发健康检查
            if (!await _checkSemaphore.WaitAsync(0)) {
                return;
            }

            try {
                _lastCheckTime = DateTime.Now;
                var isHealthy = await _authService.CheckConnectionAsync();

                if (isHealthy) {
                    if (!_isOnline || _consecutiveFailures > 0) {
                        _consecutiveFailures = 0;
                        UpdateStatus(true, "✅ API连接正常");
                    }
                } else {
                    _consecutiveFailures++;

                    var message = _consecutiveFailures >= MaxConsecutiveFailures
                        ? "❌ API服务不可用（多次连接失败）"
                        : $"⚠️ API连接异常（失败{_consecutiveFailures}次）";

                    UpdateStatus(false, message);
                }
            } catch (Exception ex) {
                _consecutiveFailures++;
                UpdateStatus(false, $"❌ 连接检查失败: {ex.Message}");
            } finally {
                _checkSemaphore.Release();
            }
        }

        private void UpdateStatus(bool isOnline, string message) {
            var previousStatus = _isOnline;
            _isOnline = isOnline;
            _statusMessage = message;

            // 仅在状态改变时触发事件
            if (previousStatus != isOnline) {
                StatusChanged?.Invoke(this, new ApiHealthStatusChangedEventArgs {
                    IsOnline = isOnline,
                    Message = message,
                    Timestamp = _lastCheckTime
                });
            }
        }

        public void Dispose() {
            StopMonitoring();
            _healthCheckTimer?.Dispose();
            _checkSemaphore?.Dispose();
        }
    }

    /// <summary>
    /// API健康状态变更事件参数
    /// </summary>
    public class ApiHealthStatusChangedEventArgs : EventArgs {
        public bool IsOnline { get; set; }
        public string Message { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
    }
}
