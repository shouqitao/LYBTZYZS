using LYBT.Infrastructure.Interfaces;
using LYBT.Entities.Auth;
using LYBT.Shared.Models.Enums;

namespace LYBT.Module.Auth.Interfaces
{
    /// <summary>
    /// 登录尝试仓储接口 - 记录和分析所有登录尝试
    /// 继承BaseRepository提供通用CRUD，扩展登录尝试跟踪特定业务方法
    /// </summary>
    public interface ILoginAttemptRepository : IBaseRepository<LoginAttemptModel>
    {
        /// <summary>
        /// 记录登录尝试
        /// </summary>
        Task<LoginAttemptModel> RecordAttemptAsync(LoginAttemptModel attempt);

        /// <summary>
        /// 获取用户最近的登录尝试
        /// </summary>
        Task<List<LoginAttemptModel>> GetRecentAttemptsByUsernameAsync(string username, TimeSpan timeSpan);

        /// <summary>
        /// 获取IP地址的最近登录尝试
        /// </summary>
        Task<List<LoginAttemptModel>> GetRecentAttemptsByIpAsync(string ipAddress, TimeSpan timeSpan);

        /// <summary>
        /// 统计用户最近失败次数
        /// </summary>
        Task<int> GetFailureCountByUsernameAsync(string username, TimeSpan timeSpan);

        /// <summary>
        /// 统计IP地址最近失败次数
        /// </summary>
        Task<int> GetFailureCountByIpAsync(string ipAddress, TimeSpan timeSpan);

        /// <summary>
        /// 获取可疑的登录尝试
        /// </summary>
        Task<List<LoginAttemptModel>> GetSuspiciousAttemptsAsync(TimeSpan timeSpan, SecurityLevel minRiskLevel = SecurityLevel.High);

        /// <summary>
        /// 获取需要审查的登录尝试
        /// </summary>
        Task<List<LoginAttemptModel>> GetAttemptsRequiringReviewAsync();

        /// <summary>
        /// 标记尝试为已审查
        /// </summary>
        Task MarkAsReviewedAsync(Guid attemptId, Guid reviewedBy, string? notes = null);

        /// <summary>
        /// 批量标记尝试为已审查
        /// </summary>
        Task MarkBatchAsReviewedAsync(List<Guid> attemptIds, Guid reviewedBy, string? notes = null);

        /// <summary>
        /// 获取登录成功率统计
        /// </summary>
        Task<(int TotalAttempts, int SuccessfulAttempts, double SuccessRate)> GetLoginStatsAsync(TimeSpan timeSpan);

        /// <summary>
        /// 获取风险级别统计
        /// </summary>
        Task<Dictionary<SecurityLevel, int>> GetRiskLevelStatsAsync(TimeSpan timeSpan);

        /// <summary>
        /// 按时间段统计登录尝试
        /// </summary>
        Task<Dictionary<DateTime, int>> GetAttemptsByHourAsync(DateTime startDate, DateTime endDate);

        /// <summary>
        /// 获取顶级攻击IP地址
        /// </summary>
        Task<List<(string IpAddress, int AttemptCount, int FailureCount)>> GetTopAttackingIpsAsync(TimeSpan timeSpan, int topCount = 10);

        /// <summary>
        /// 获取常用登录方式统计
        /// </summary>
        Task<Dictionary<LoginType, int>> GetLoginTypeStatsAsync(TimeSpan timeSpan);

        /// <summary>
        /// 删除旧的登录尝试记录
        /// </summary>
        Task CleanupOldAttemptsAsync(TimeSpan retentionPeriod);

        /// <summary>
        /// 检查是否为暴力破解攻击
        /// </summary>
        Task<bool> IsBruteForceAttackAsync(string username, string ipAddress, TimeSpan timeWindow, int threshold);

        /// <summary>
        /// 获取用户登录历史趋势
        /// </summary>
        Task<List<LoginAttemptModel>> GetUserLoginHistoryAsync(string username, DateTime startDate, DateTime endDate);
    }
}