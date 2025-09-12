using System.Security.Claims;
using LYBT.Shared.Models.Enums;

namespace LYBT.Module.Auth.Interfaces
{

    /// <summary>
    /// JWT认证服务接口
    /// </summary>
    public interface IJwtAuthenticationService
    {

        /// <summary>
        /// 生成JWT令牌
        /// </summary>
        /// <param name="userId">用户ID</param>        /// <param name="userName">用户名</param>        /// <param name="roles">用户角色</param>        /// <param name="rememberMe">是否记住我（影响令牌过期时间）</param>        /// <returns>JWT令牌</returns>
        string GenerateToken(string userId, string userName, UserRole role, bool rememberMe = false);

        /// <summary>
        /// 验证JWT令牌
        /// </summary>
        /// <param name="token">JWT令牌</param>        /// <returns>验证结果</returns>
        ClaimsPrincipal? ValidateToken(string token);

        /// <summary>
        /// 刷新JWT令牌
        /// </summary>
        /// <param name="token">当前令牌</param>        /// <returns>新的令牌</returns>
        string RefreshToken(string token);

        /// <summary>
        /// 从令牌中提取用户信息
        /// </summary>
        /// <param name="token">JWT令牌</param>
        /// <returns>用户信息</returns>
        TokenUserInfo? ExtractUserInfo(string token);
    }

    /// <summary>
    /// 令牌用户信息
    /// </summary>
    public class TokenUserInfo
    {
        public string UserId { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public UserRole Role { get; set; } = UserRole.Doctor;
        public DateTime ExpiresAt { get; set; }
    }
}
