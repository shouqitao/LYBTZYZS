namespace LYBT.Desktop.Foundation.Security
{
    /// <summary>
    /// Token管理器接口 - 内存级Token存储
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
    }
}
