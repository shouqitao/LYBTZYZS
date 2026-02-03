using LYBT.Desktop.Foundation.HealthCheck;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Foundation.Application
{
    /// <summary>
    /// 应用程序状态服务实现
    /// 负责管理应用程序全局状态，包括API健康状态、连接状态等
    /// Issue #1823: API健康检查前置优化
    /// </summary>
    public class ApplicationStateService : IApplicationStateService
    {
        private readonly IApiHealthCheckService? _apiHealthCheckService;
        private readonly ILogger<ApplicationStateService> _logger;
        private readonly string _apiBaseUrl;

        /// <summary>
        /// API是否健康（可访问）
        /// </summary>
        public bool IsApiHealthy { get; set; }

        /// <summary>
        /// API基础URL
        /// </summary>
        public string ApiBaseUrl
        {
            get => _apiBaseUrl;
            set => throw new InvalidOperationException("ApiBaseUrl是只读的，从配置文件加载");
        }

        /// <summary>
        /// 连接状态描述
        /// 例如："已连接"、"连接失败"、"连接超时"
        /// </summary>
        public string ConnectionStatus { get; set; } = "未检查";

        /// <summary>
        /// 最后一次健康检查时间
        /// </summary>
        public DateTime? LastHealthCheckTime { get; set; }

        /// <summary>
        /// 最后一次错误信息
        /// OpenSpec: refactor-startup-connection-resilience
        /// </summary>
        public string? LastError { get; set; }

        /// <summary>
        /// API状态变更事件
        /// OpenSpec: refactor-startup-connection-resilience - 事件驱动状态更新
        /// </summary>
        public event EventHandler<ApiStatusChangedEventArgs>? StatusChanged;

        /// <summary>
        /// 构造函数
        /// </summary>
        public ApplicationStateService(
            IApiHealthCheckService? apiHealthCheckService,
            IConfiguration configuration,
            ILogger<ApplicationStateService> logger)
        {
            _apiHealthCheckService = apiHealthCheckService;
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            // 从配置文件读取API基础URL
            _apiBaseUrl = configuration["ApiSettings:BaseUrl"] ?? "http://localhost:5000";

            _logger.LogInformation("ApplicationStateService初始化，API基础URL: {ApiBaseUrl}", _apiBaseUrl);
        }

        /// <summary>
        /// 执行API健康检查
        /// </summary>
        /// <param name="timeoutSeconds">超时时间（秒），默认10秒</param>
        /// <returns>健康检查是否成功</returns>
        public async Task<bool> CheckApiHealthAsync(int timeoutSeconds = 10)
        {
            _logger.LogInformation("开始API健康检查，超时时间: {Timeout}秒", timeoutSeconds);

            if (_apiHealthCheckService == null)
            {
                _logger.LogWarning("API健康检查服务未配置");
                UpdateState(false, "健康检查服务未配置", "健康检查服务未配置");
                return false;
            }

            try
            {
                // 转换超时时间：秒 → 毫秒
                var timeoutMs = timeoutSeconds * 1000;

                var status = await _apiHealthCheckService.CheckHealthAsync(timeoutMs);

                LastHealthCheckTime = DateTime.Now;

                switch (status)
                {
                    case ApiHealthStatus.Healthy:
                        UpdateState(true, "已连接", null);
                        _logger.LogInformation("API健康检查成功: {ApiBaseUrl}", _apiBaseUrl);
                        return true;

                    case ApiHealthStatus.Unhealthy:
                        var errorMsg = _apiHealthCheckService.LastErrorMessage;
                        UpdateState(false, $"连接失败: {errorMsg}", errorMsg);
                        _logger.LogWarning("API健康检查失败: {ApiBaseUrl}, 错误: {Error}",
                            _apiBaseUrl, errorMsg);
                        return false;

                    case ApiHealthStatus.Checking:
                    default:
                        UpdateState(false, "正在检查连接...", null);
                        _logger.LogWarning("API健康检查状态异常: {Status}", status);
                        return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "API健康检查发生异常: {ApiBaseUrl}", _apiBaseUrl);
                UpdateState(false, "健康检查失败，请稍后重试", ex.Message);
                return false;
            }
        }

        /// <summary>
        /// 更新状态并触发事件
        /// OpenSpec: refactor-startup-connection-resilience
        /// </summary>
        private void UpdateState(bool isHealthy, string connectionStatus, string? lastError)
        {
            var previousHealthy = IsApiHealthy;
            var previousStatus = ConnectionStatus;

            IsApiHealthy = isHealthy;
            ConnectionStatus = connectionStatus;
            LastError = lastError;
            LastHealthCheckTime = DateTime.Now;

            // 状态变化时触发事件
            if (previousHealthy != isHealthy || previousStatus != connectionStatus)
            {
                StatusChanged?.Invoke(this, new ApiStatusChangedEventArgs(isHealthy, connectionStatus, lastError));
            }
        }
    }
}
