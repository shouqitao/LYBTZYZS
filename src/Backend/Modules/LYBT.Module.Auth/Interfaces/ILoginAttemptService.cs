using LYBT.Shared.Models.Core;
using LYBT.Shared.Models.Enums;

namespace LYBT.Module.Auth.Interfaces
{
    /// <summary>
    /// 登录尝试服务接口 - 跟踪和分析所有登录尝试
    /// 提供登录尝试记录、风险评估、暴力破解检测等功能
    /// </summary>
    public interface ILoginAttemptService
    {
        /// <summary>
        /// 记录登录尝试
        /// </summary>
        Task<BaseLoginAttempt> RecordLoginAttemptAsync(string username, bool isSuccess, 
                                                      string? failureReason = null, Guid? userId = null,
                                                      string? clientIp = null, string? userAgent = null, 
                                                      LoginType loginType = LoginType.Password,
                                                      string? location = null, string? deviceFingerprint = null);

        /// <summary>
        /// 检查账户是否被锁定（暴力破解保护）
        /// </summary>
        Task<bool> IsAccountLockedAsync(string username);

        /// <summary>
        /// 获取账户剩余锁定时间（秒）
        /// </summary>
        Task<int> GetRemainingLockTimeAsync(string username);

        /// <summary>
        /// 清除用户的失败尝试记录
        /// </summary>
        Task ClearFailedAttemptsAsync(string username);

        /// <summary>
        /// 获取用户最近的登录尝试
        /// </summary>
        Task<List<BaseLoginAttempt>> GetUserRecentAttemptsAsync(string username, TimeSpan timeSpan);

        /// <summary>
        /// 获取IP地址最近的登录尝试
        /// </summary>
        Task<List<BaseLoginAttempt>> GetIpRecentAttemptsAsync(string ipAddress, TimeSpan timeSpan);

        /// <summary>
        /// 获取可疑的登录尝试
        /// </summary>
        Task<List<BaseLoginAttempt>> GetSuspiciousAttemptsAsync(TimeSpan timeSpan, SecurityLevel minRiskLevel = SecurityLevel.High);

        /// <summary>
        /// 获取需要审查的登录尝试
        /// </summary>
        Task<List<BaseLoginAttempt>> GetAttemptsRequiringReviewAsync();

        /// <summary>
        /// 标记尝试为已审查
        /// </summary>
        Task MarkAttemptAsReviewedAsync(Guid attemptId, Guid reviewedBy, string? notes = null);

        /// <summary>
        /// 批量审查登录尝试
        /// </summary>
        Task BatchReviewAttemptsAsync(List<Guid> attemptIds, Guid reviewedBy, string? notes = null);

        /// <summary>
        /// 评估登录尝试风险级别
        /// </summary>
        Task<SecurityLevel> AssessLoginRiskAsync(string username, string? clientIp, string? userAgent = null, 
                                                string? location = null, string? deviceFingerprint = null);

        /// <summary>
        /// 检测暴力破解攻击
        /// </summary>
        Task<bool> DetectBruteForceAttackAsync(string username, string? clientIp);

        /// <summary>
        /// 获取登录统计信息
        /// </summary>
        Task<(int TotalAttempts, int SuccessfulAttempts, double SuccessRate)> GetLoginStatisticsAsync(TimeSpan timeSpan);

        /// <summary>
        /// 获取攻击者TOP列表
        /// </summary>
        Task<List<(string IpAddress, int AttemptCount, int FailureCount)>> GetTopAttackingIpsAsync(TimeSpan timeSpan, int topCount = 10);

        /// <summary>
        /// 获取风险级别分布
        /// </summary>
        Task<Dictionary<SecurityLevel, int>> GetRiskLevelDistributionAsync(TimeSpan timeSpan);

        /// <summary>
        /// 获取登录方式统计
        /// </summary>
        Task<Dictionary<LoginType, int>> GetLoginTypeStatisticsAsync(TimeSpan timeSpan);

        /// <summary>
        /// 获取按小时统计的登录尝试
        /// </summary>
        Task<Dictionary<DateTime, int>> GetHourlyLoginAttemptsAsync(DateTime startDate, DateTime endDate);

        /// <summary>
        /// 获取用户登录历史分析
        /// </summary>
        Task<List<BaseLoginAttempt>> GetUserLoginHistoryAsync(string username, DateTime startDate, DateTime endDate);

        /// <summary>
        /// 清理过期的登录尝试记录
        /// </summary>
        Task CleanupOldAttemptsAsync(TimeSpan retentionPeriod);

        /// <summary>
        /// 检查IP地址是否在黑名单中
        /// </summary>
        Task<bool> IsIpBlacklistedAsync(string ipAddress);

        /// <summary>
        /// 将IP地址添加到临时黑名单
        /// </summary>
        Task BlacklistIpTemporarilyAsync(string ipAddress, TimeSpan duration, string reason);

        /// <summary>
        /// 生成登录尝试安全报告
        /// </summary>
        Task<string> GenerateSecurityReportAsync(DateTime startDate, DateTime endDate);

        /// <summary>
        /// 分析登录模式异常
        /// </summary>
        Task<List<string>> AnalyzeLoginAnomaliesAsync(TimeSpan analysisWindow);
    }
}