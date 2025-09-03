using LYBT.Infrastructure.Security.Services;

namespace LYBT.Infrastructure.Security
{
    /// <summary>
    /// JWT令牌存储服务接口 - UltraThink安全优化
    /// </summary>
    public interface ITokenStoreService
    {
        /// <summary>
        /// 存储访问令牌
        /// </summary>
        /// <param name="tokenId">令牌ID (JTI)</param>
        /// <param name="tokenInfo">令牌信息</param>
        /// <returns>是否存储成功</returns>
        Task<bool> StoreAccessTokenAsync(string tokenId, TokenStoreInfo tokenInfo);

        /// <summary>
        /// 检查令牌是否已撤销
        /// </summary>
        /// <param name="tokenId">令牌ID</param>
        /// <returns>是否已撤销</returns>
        Task<bool> IsTokenRevokedAsync(string tokenId);

        /// <summary>
        /// 撤销指定令牌
        /// </summary>
        /// <param name="tokenId">令牌ID</param>
        /// <param name="reason">撤销原因</param>
        /// <returns>是否撤销成功</returns>
        Task<bool> RevokeTokenAsync(string tokenId, string reason = "用户注销");

        /// <summary>
        /// 撤销用户的所有令牌
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <param name="reason">撤销原因</param>
        /// <returns>撤销的令牌数量</returns>
        Task<int> RevokeAllUserTokensAsync(Guid userId, string reason = "安全操作");

        /// <summary>
        /// 存储刷新令牌
        /// </summary>
        /// <param name="refreshToken">刷新令牌</param>
        /// <param name="tokenInfo">令牌信息</param>
        /// <returns>是否存储成功</returns>
        Task<bool> StoreRefreshTokenAsync(string refreshToken, StoredRefreshToken tokenInfo);

        /// <summary>
        /// 获取存储的刷新令牌
        /// </summary>
        /// <param name="refreshToken">刷新令牌</param>
        /// <returns>存储的令牌信息</returns>
        Task<StoredRefreshToken?> GetStoredRefreshTokenAsync(string refreshToken);

        /// <summary>
        /// 撤销刷新令牌
        /// </summary>
        /// <param name="refreshToken">刷新令牌</param>
        /// <param name="reason">撤销原因</param>
        /// <returns>是否撤销成功</returns>
        Task<bool> RevokeRefreshTokenAsync(string refreshToken, string reason = "令牌刷新");

        /// <summary>
        /// 记录可疑活动
        /// </summary>
        /// <param name="activityType">活动类型</param>
        /// <param name="tokenId">令牌ID</param>
        /// <param name="userId">用户ID</param>
        /// <param name="clientIP">客户端IP</param>
        /// <param name="userAgent">用户代理</param>
        /// <param name="details">详细信息</param>
        Task LogSuspiciousActivityAsync(
            string activityType, 
            string? tokenId = null, 
            Guid? userId = null, 
            string? clientIP = null, 
            string? userAgent = null, 
            string? details = null);

        /// <summary>
        /// 清理过期令牌
        /// </summary>
        /// <returns>清理的令牌数量</returns>
        Task<int> CleanupExpiredTokensAsync();
    }
}