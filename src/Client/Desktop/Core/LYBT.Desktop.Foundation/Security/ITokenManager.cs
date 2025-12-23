namespace LYBT.Desktop.Foundation.Security
{
    /// <summary>
    /// Token管理器接口 - 内存级Token存储
    /// OpenSpec: refactor-login-authentication (TKM-001, TKM-002)
    /// 
    /// 设计原则：
    /// 1. Token = 会话级数据，仅存储在进程内存中
    /// 2. 同步方法（内存操作无需异步）
    /// 3. 职责单一：只管理Token，不涉及凭证存储
    /// 4. 线程安全：支持多线程访问
    /// 
    /// 与ITokenStorageService区别：
    /// - ITokenStorageService: 遗留接口，异步方法，包含LoginResponse
    /// - ITokenManager: 新接口，同步方法，只管理Token字符串
    /// </summary>
    public interface ITokenManager
    {
        /// <summary>
        /// 获取当前AccessToken
        /// </summary>
        string? AccessToken { get; }

        /// <summary>
        /// 获取当前RefreshToken
        /// </summary>
        string? RefreshToken { get; }

        /// <summary>
        /// 获取AccessToken过期时间
        /// </summary>
        DateTime? AccessTokenExpiry { get; }

        /// <summary>
        /// 设置Token（登录成功或Token刷新后调用）
        /// </summary>
        /// <param name="accessToken">访问令牌</param>
        /// <param name="refreshToken">刷新令牌</param>
        /// <param name="expiry">AccessToken过期时间(UTC)</param>
        void SetTokens(string accessToken, string refreshToken, DateTime expiry);

        /// <summary>
        /// 清除所有Token（登出或Token失效时调用）
        /// </summary>
        void ClearTokens();

        /// <summary>
        /// 检查Token是否有效（非空且未过期）
        /// </summary>
        /// <returns>true=Token有效，false=Token无效或已过期</returns>
        bool IsTokenValid();

        /// <summary>
        /// 检查Token是否即将过期
        /// </summary>
        /// <param name="threshold">提前预警时间（默认5分钟）</param>
        /// <returns>true=即将过期，需要刷新</returns>
        bool IsTokenExpiringSoon(TimeSpan threshold);
    }
}
