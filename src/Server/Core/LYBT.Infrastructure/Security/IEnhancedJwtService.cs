using System.Security.Claims;

namespace LYBT.Infrastructure.Security
{
    /// <summary>
    /// 增强JWT服务接口
    /// </summary>
    public interface IEnhancedJwtService
    {
        /// <summary>
        /// 生成访问令牌
        /// </summary>
        Task<TokenResult> GenerateAccessTokenAsync(TokenRequest request);

        /// <summary>
        /// 验证访问令牌
        /// </summary>
        Task<TokenValidationResult> ValidateAccessTokenAsync(string token, string? clientIP = null);

        /// <summary>
        /// 刷新访问令牌
        /// </summary>
        Task<TokenResult> RefreshAccessTokenAsync(string refreshToken, string? clientIP = null);

        /// <summary>
        /// 撤销访问令牌
        /// </summary>
        Task RevokeAccessTokenAsync(string tokenId, string reason = "用户注销");

        /// <summary>
        /// 撤销用户所有令牌
        /// </summary>
        Task RevokeAllUserTokensAsync(Guid userId, string reason = "安全操作");
    }

    /// <summary>
    /// 令牌请求
    /// </summary>
    public class TokenRequest
    {
        public Guid UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string ClientIP { get; set; } = string.Empty;
        public string? SessionId { get; set; }
        public string? DeviceId { get; set; }
        public string? UserAgent { get; set; }
        public bool RememberMe { get; set; } = false;
        public Dictionary<string, string>? CustomClaims { get; set; }
    }

    /// <summary>
    /// 令牌结果
    /// </summary>
    public class TokenResult
    {
        public string AccessToken { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public string TokenType { get; set; } = "Bearer";
        public int ExpiresIn { get; set; }
        public DateTime ExpiresAt { get; set; }
        public string? TokenId { get; set; }
        public string? TokenHash { get; set; }
    }

    /// <summary>
    /// 令牌验证结果
    /// </summary>
    public class TokenValidationResult
    {
        public bool IsValid { get; set; }
        public string? Error { get; set; }
        public ClaimsPrincipal? Principal { get; set; }
        public string? TokenId { get; set; }
        public Guid? UserId { get; set; }
        public string? Username { get; set; }
        public string? Role { get; set; }
        public string? SessionId { get; set; }
        public string? DeviceId { get; set; }
    }

    /// <summary>
    /// JWT配置选项
    /// </summary>
    public class EnhancedJwtOptions
    {
        public string SecretKey { get; set; } = string.Empty;
        public string Issuer { get; set; } = string.Empty;
        public string Audience { get; set; } = string.Empty;
        public int ShortTermExpiryMinutes { get; set; } = 480; // 8小时
        public int LongTermExpiryMinutes { get; set; } = 43200; // 30天
        public int ClockSkewMinutes { get; set; } = 5;
        public bool ValidateClientIP { get; set; } = true;
        public int RefreshTokenExpiryDays { get; set; } = 90; // 90天
    }

    /// <summary>
    /// 安全异常
    /// </summary>
    public class SecurityException : Exception
    {
        public SecurityException(string message) : base(message) { }
        public SecurityException(string message, Exception innerException) : base(message, innerException) { }
    }
}