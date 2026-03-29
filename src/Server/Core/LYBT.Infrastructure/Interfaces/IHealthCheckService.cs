using LYBT.Shared.Models.Contracts.Health;

namespace LYBT.Infrastructure.Interfaces
{
    /// <summary>
    /// 健康检查服务接口
    /// 提供系统健康状态检查功能，遵循三层架构
    /// </summary>
    public interface IHealthCheckService
    {
        /// <summary>
        /// 执行数据库连接健康检查
        /// </summary>
        /// <returns>数据库健康检查结果</returns>
        Task<DatabaseHealthCheckResult> CheckDatabaseAsync();

        /// <summary>
        /// 获取整体系统健康状态
        /// </summary>
        /// <returns>整体健康状态</returns>
        Task<HealthStatus> GetOverallStatusAsync();
    }
}
