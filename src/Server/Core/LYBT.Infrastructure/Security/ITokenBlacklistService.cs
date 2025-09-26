using System.Security.Claims;

namespace LYBT.Infrastructure.Security
{
    /// <summary>
    /// Token黑名单服务接口
    /// 用于管理被撤销的JWT Token，防止被撤销的Token继续被使用
    /// </summary>
    public interface ITokenBlacklistService
    {
        /// <summary>
        /// 将Token加入黑名单
        /// </summary>
        /// <param name="jwtId">JWT ID (jti claim)</param>
        /// <param name="expirationTime">Token的原始过期时间</param>
        /// <param name="reason">撤销原因</param>
        /// <returns>是否成功加入黑名单</returns>
        Task<bool> BlacklistTokenAsync(string jwtId, DateTime expirationTime, string reason = "Manual revocation");

        /// <summary>
        /// 将Token加入黑名单（从Claims中提取信息）
        /// </summary>
        /// <param name="principal">Token的Claims Principal</param>
        /// <param name="reason">撤销原因</param>
        /// <returns>是否成功加入黑名单</returns>
        Task<bool> BlacklistTokenAsync(ClaimsPrincipal principal, string reason = "Manual revocation");

        /// <summary>
        /// 检查Token是否在黑名单中
        /// </summary>
        /// <param name="jwtId">JWT ID (jti claim)</param>
        /// <returns>是否在黑名单中</returns>
        Task<bool> IsTokenBlacklistedAsync(string jwtId);

        /// <summary>
        /// 检查Token是否在黑名单中（从Claims中提取JTI）
        /// </summary>
        /// <param name="principal">Token的Claims Principal</param>
        /// <returns>是否在黑名单中</returns>
        Task<bool> IsTokenBlacklistedAsync(ClaimsPrincipal principal);

        /// <summary>
        /// 批量撤销用户的所有Token
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <param name="reason">撤销原因</param>
        /// <returns>撤销的Token数量</returns>
        Task<int> BlacklistAllUserTokensAsync(string userId, string reason = "User logout");

        /// <summary>
        /// 清理过期的黑名单条目
        /// </summary>
        /// <returns>清理的条目数量</returns>
        Task<int> CleanupExpiredEntriesAsync();

        /// <summary>
        /// 获取黑名单统计信息
        /// </summary>
        /// <returns>黑名单统计</returns>
        Task<TokenBlacklistStatistics> GetStatisticsAsync();
    }

    /// <summary>
    /// Token黑名单统计信息
    /// </summary>
    public class TokenBlacklistStatistics
    {
        /// <summary>
        /// 黑名单中的Token总数
        /// </summary>
        public int TotalBlacklistedTokens { get; set; }

        /// <summary>
        /// 今日新增的黑名单Token数量
        /// </summary>
        public int TodayBlacklistedCount { get; set; }

        /// <summary>
        /// 过期但未清理的条目数量
        /// </summary>
        public int ExpiredEntriesCount { get; set; }

        /// <summary>
        /// 最后一次清理时间
        /// </summary>
        public DateTime? LastCleanupTime { get; set; }
    }

    /// <summary>
    /// 黑名单条目
    /// </summary>
    public class BlacklistEntry
    {
        /// <summary>
        /// JWT ID
        /// </summary>
        public string JwtId { get; set; } = string.Empty;

        /// <summary>
        /// 用户ID
        /// </summary>
        public string UserId { get; set; } = string.Empty;

        /// <summary>
        /// 撤销时间
        /// </summary>
        public DateTime RevokedAt { get; set; }

        /// <summary>
        /// Token过期时间
        /// </summary>
        public DateTime ExpiresAt { get; set; }

        /// <summary>
        /// 撤销原因
        /// </summary>
        public string Reason { get; set; } = string.Empty;

        /// <summary>
        /// 检查条目是否已过期（可以清理）
        /// </summary>
        public bool IsExpired => DateTime.UtcNow > ExpiresAt;
    }
}