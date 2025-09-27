using System.Security.Claims;
using LYBT.Shared.Models.Enums;
using LYBT.Entities.Auth;
using LYBT.Module.Auth.Models;

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
        /// <param name="userId">用户ID</param>
        /// <param name="userName">用户名</param>
        /// <param name="roles">用户角色</param>
        /// <param name="rememberMe">是否记住我（影响令牌过期时间）</param>
        /// <returns>JWT令牌</returns>
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
        /// 安全刷新JWT令牌（包含设备验证）
        /// </summary>
        /// <param name="token">当前令牌</param>
        /// <param name="deviceFingerprint">设备指纹</param>
        /// <param name="ipAddress">IP地址</param>
        /// <param name="userAgent">User-Agent</param>
        /// <returns>安全刷新结果</returns>
        SecureTokenRefreshResult SecureRefreshToken(
            string token, 
            string? deviceFingerprint = null, 
            string? ipAddress = null, 
            string? userAgent = null);

        /// <summary>
        /// 从令牌中提取用户信息
        /// </summary>
        /// <param name="token">JWT令牌</param>
        /// <returns>用户信息</returns>
        TokenUserInfo? ExtractUserInfo(string token);
    }

    /// <summary>
    /// 令牌用户信息.
    /// </summary>
    public class TokenUserInfo
    {
        public string UserId { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public UserRole Role { get; set; } = UserRole.Doctor;
        public DateTime ExpiresAt { get; set; }
    }

    /// <summary>
    /// 安全令牌刷新结果
    /// </summary>
    public class SecureTokenRefreshResult
    {
        /// <summary>
        /// 是否成功
        /// </summary>
        public bool IsSuccess { get; set; }

        /// <summary>
        /// 新的访问令牌
        /// </summary>
        public string? AccessToken { get; set; }

        /// <summary>
        /// 新的刷新令牌
        /// </summary>
        public string? RefreshToken { get; set; }

        /// <summary>
        /// 令牌过期时间
        /// </summary>
        public DateTime ExpiresAt { get; set; }

        /// <summary>
        /// 安全验证级别
        /// </summary>
        public TokenSecurityLevel SecurityLevel { get; set; }

        /// <summary>
        /// 错误消息
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// 安全警告信息
        /// </summary>
        public List<string> SecurityWarnings { get; set; } = new List<string>();

        /// <summary>
        /// 是否需要额外验证
        /// </summary>
        public bool RequiresAdditionalVerification { get; set; }

        /// <summary>
        /// 创建成功结果
        /// </summary>
        public static SecureTokenRefreshResult Success(
            string accessToken, 
            string refreshToken, 
            DateTime expiresAt,
            TokenSecurityLevel securityLevel = TokenSecurityLevel.Low)
        {
            return new SecureTokenRefreshResult
            {
                IsSuccess = true,
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExpiresAt = expiresAt,
                SecurityLevel = securityLevel
            };
        }

        /// <summary>
        /// 创建失败结果
        /// </summary>
        public static SecureTokenRefreshResult Failure(string errorMessage)
        {
            return new SecureTokenRefreshResult
            {
                IsSuccess = false,
                ErrorMessage = errorMessage
            };
        }
    }
}
