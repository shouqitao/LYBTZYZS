using System;

namespace LYBT.Shared.Models.Auth
{
    /// <summary>
    /// Token对 - 包含AccessToken和RefreshToken
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
        /// 令牌类型
        /// </summary>
        public string TokenType { get; set; } = "Bearer";

        /// <summary>
        /// AccessToken过期时间（秒）
        /// </summary>
        public int ExpiresIn { get; set; }

        /// <summary>
        /// AccessToken过期时间点
        /// </summary>
        public DateTime AccessTokenExpires { get; set; }

        /// <summary>
        /// RefreshToken过期时间点
        /// </summary>
        public DateTime RefreshTokenExpires { get; set; }

        /// <summary>
        /// 作用域
        /// </summary>
        public string? Scope { get; set; }

        /// <summary>
        /// JTI (JWT ID) - Token唯一标识
        /// </summary>
        public string? Jti { get; set; }
    }

    /// <summary>
    /// Token刷新请求
    /// </summary>
    public class RefreshTokenRequest
    {
        /// <summary>
        /// 刷新令牌
        /// </summary>
        public string RefreshToken { get; set; } = string.Empty;

        /// <summary>
        /// 设备ID（可选，用于多设备管理）
        /// </summary>
        public string? DeviceId { get; set; }
    }

    /// <summary>
    /// Token撤销请求
    /// </summary>
    public class RevokeTokenRequest
    {
        /// <summary>
        /// 要撤销的Token（可以是AccessToken或RefreshToken）
        /// </summary>
        public string Token { get; set; } = string.Empty;

        /// <summary>
        /// Token类型提示（可选）
        /// </summary>
        public string? TokenTypeHint { get; set; }

        /// <summary>
        /// 撤销原因
        /// </summary>
        public string? Reason { get; set; }
    }
}