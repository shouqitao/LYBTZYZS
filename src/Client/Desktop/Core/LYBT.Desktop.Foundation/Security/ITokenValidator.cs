namespace LYBT.Desktop.Foundation.Security
{
    /// <summary>
    /// Token验证器接口 - 提供JWT Token的客户端验证功能
    /// </summary>
    /// <remarks>
    /// Issue #1863: Token认证安全重构 - 实现客户端JWT自验证
    ///
    /// 设计要点：
    /// 1. 使用JwtSecurityTokenHandler进行本地验证
    /// 2. 验证内容：签名、Issuer、Audience、Expiration、必需Claims
    /// 3. 移除Server API依赖（/api/v1/auth/validate POST端点）
    /// </remarks>
    public interface ITokenValidator
    {
        /// <summary>
        /// 验证JWT Token
        /// </summary>
        /// <param name="token">待验证的JWT Token</param>
        /// <returns>验证结果</returns>
        Task<TokenValidationResult> ValidateTokenAsync(string token);

        /// <summary>
        /// 验证Token并提取用户信息
        /// </summary>
        /// <param name="token">待验证的JWT Token</param>
        /// <returns>验证成功返回用户信息，失败返回null</returns>
        Task<TokenUserInfo?> ValidateAndGetUserInfoAsync(string token);
    }

    /// <summary>
    /// Token验证结果
    /// </summary>
    public class TokenValidationResult
    {
        /// <summary>
        /// 是否有效
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// 错误信息（验证失败时）
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// 用户信息（验证成功时）
        /// </summary>
        public TokenUserInfo? UserInfo { get; set; }
    }

    /// <summary>
    /// Token中的用户信息
    /// </summary>
    public class TokenUserInfo
    {
        /// <summary>
        /// 用户ID
        /// </summary>
        public Guid UserId { get; set; }

        /// <summary>
        /// 用户名
        /// </summary>
        public string UserName { get; set; } = string.Empty;

        /// <summary>
        /// 角色
        /// </summary>
        public string Role { get; set; } = string.Empty;

        /// <summary>
        /// 用户类型（superadmin/user）
        /// </summary>
        public string UserType { get; set; } = string.Empty;
    }
}
