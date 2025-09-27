using System;
using System.Security.Claims;
using System.Threading.Tasks;
using LYBT.Entities.Auth;
using LYBT.Entities.Users;
using LYBT.Shared.Models.Contracts.Auth;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Enums;

namespace LYBT.Module.Auth.Interfaces
{
    /// <summary>
    /// 增强的JWT服务接口 - 支持Refresh Token和安全特性
    /// </summary>
    public interface IEnhancedJwtService
    {
        /// <summary>
        /// 生成令牌对（Access Token + Refresh Token）
        /// </summary>
        Task<TokenPair> GenerateTokenPairAsync(User user, string clientIp, string userAgent, string? deviceId = null);

        /// <summary>
        /// 生成访问令牌
        /// </summary>
        string GenerateAccessToken(string userId, string userName, UserRole role, string? jti = null);

        /// <summary>
        /// 生成刷新令牌
        /// </summary>
        Task<RefreshToken> GenerateRefreshTokenAsync(User user, string jti, string clientIp, string userAgent, string? deviceId = null);

        /// <summary>
        /// 验证访问令牌
        /// </summary>
        ClaimsPrincipal? ValidateAccessToken(string token);

        /// <summary>
        /// 验证刷新令牌
        /// </summary>
        Task<RefreshToken?> ValidateRefreshTokenAsync(string refreshToken);

        /// <summary>
        /// 刷新令牌对
        /// </summary>
        Task<TokenPair> RefreshTokenPairAsync(string refreshToken, string clientIp, string userAgent);

        /// <summary>
        /// 撤销刷新令牌
        /// </summary>
        Task RevokeRefreshTokenAsync(string refreshToken, string reason);

        /// <summary>
        /// 撤销用户的所有刷新令牌
        /// </summary>
        Task RevokeAllUserTokensAsync(Guid userId, string reason);

        /// <summary>
        /// 获取用户的活跃令牌
        /// </summary>
        Task<IEnumerable<RefreshToken>> GetActiveUserTokensAsync(Guid userId);

        /// <summary>
        /// 清理过期的刷新令牌
        /// </summary>
        Task<int> CleanupExpiredTokensAsync();

        /// <summary>
        /// 从令牌中提取用户信息
        /// </summary>
        TokenUserInfo? ExtractUserInfo(string token);

        /// <summary>
        /// 从令牌中提取JTI
        /// </summary>
        string? ExtractJti(string token);

        /// <summary>
        /// 检查令牌是否在黑名单中
        /// </summary>
        Task<bool> IsTokenBlacklistedAsync(string jti);

        /// <summary>
        /// 将令牌添加到黑名单
        /// </summary>
        Task AddToBlacklistAsync(string jti, DateTime expiry);
    }
}