namespace LYBT.Shared.Models.Contracts.Auth
{
    /// <summary>
    /// JWT令牌对 - 包含Access Token和Refresh Token
    /// </summary>
    public class TokenPair
    {
        /// <summary>
        /// 访问令牌
        /// </summary>
        public string AccessToken { get; set; } = string.Empty;

        /// <summary>
        /// 刷新令牌
        /// </summary>
        public string RefreshToken { get; set; } = string.Empty;

        /// <summary>
        /// 访问令牌过期时间
        /// </summary>
        public DateTime AccessTokenExpires { get; set; }

        /// <summary>
        /// 刷新令牌过期时间
        /// </summary>
        public DateTime RefreshTokenExpires { get; set; }

        /// <summary>
        /// 令牌类型
        /// </summary>
        public string TokenType { get; set; } = "Bearer";

        /// <summary>
        /// 访问令牌剩余有效时间（秒）
        /// </summary>
        public int ExpiresIn => (int)(AccessTokenExpires - DateTime.UtcNow).TotalSeconds;

        /// <summary>
        /// 用户ID
        /// </summary>
        public Guid? UserId { get; set; }

        /// <summary>
        /// 用户名
        /// </summary>
        public string? UserName { get; set; }

        /// <summary>
        /// 用户角色
        /// </summary>
        public string? UserRole { get; set; }

        /// <summary>
        /// 设备ID（用于多设备管理）
        /// </summary>
        public string? DeviceId { get; set; }
    }

    /// <summary>
    /// 刷新令牌请求
    /// </summary>
    public class RefreshTokenRequest
    {
        /// <summary>
        /// 要刷新的令牌
        /// </summary>
        public string RefreshToken { get; set; } = string.Empty;

        /// <summary>
        /// 设备ID（可选）
        /// </summary>
        public string? DeviceId { get; set; }
    }

    /// <summary>
    /// 撤销令牌请求
    /// </summary>
    public class RevokeTokenRequest
    {
        /// <summary>
        /// 要撤销的刷新令牌
        /// </summary>
        public string RefreshToken { get; set; } = string.Empty;

        /// <summary>
        /// 撤销原因
        /// </summary>
        public string? Reason { get; set; }

        /// <summary>
        /// 是否撤销所有设备的令牌
        /// </summary>
        public bool RevokeAll { get; set; }
    }
}
