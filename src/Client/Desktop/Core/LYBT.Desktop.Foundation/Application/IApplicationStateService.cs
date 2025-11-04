namespace LYBT.Desktop.Foundation.Application
{
    /// <summary>
    /// 应用程序状态服务接口
    /// 负责管理应用程序全局状态，包括API健康状态、连接状态等
    /// Issue #1823: API健康检查前置优化
    /// </summary>
    public interface IApplicationStateService
    {
        /// <summary>
        /// API是否健康（可访问）
        /// </summary>
        bool IsApiHealthy { get; set; }

        /// <summary>
        /// API基础URL
        /// </summary>
        string ApiBaseUrl { get; set; }

        /// <summary>
        /// 连接状态描述
        /// 例如："已连接"、"连接失败"、"连接超时"
        /// </summary>
        string ConnectionStatus { get; set; }

        /// <summary>
        /// 最后一次健康检查时间
        /// </summary>
        DateTime? LastHealthCheckTime { get; set; }

        /// <summary>
        /// 执行API健康检查
        /// </summary>
        /// <param name="timeoutSeconds">超时时间（秒），默认10秒</param>
        /// <returns>健康检查是否成功</returns>
        Task<bool> CheckApiHealthAsync(int timeoutSeconds = 10);
    }
}
