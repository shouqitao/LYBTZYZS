using LYBT.Shared.Models.Contracts.Auth;

namespace LYBT.Desktop.Foundation.Security
{
    /// <summary>
    /// Token 存储服务接口 - 用于管理 JWT Token 的本地存储
    /// refactor-auth-role-system Phase 1.2: 添加同步方法避免死锁
    /// </summary>
    public interface ITokenStorageService
    {
        #region 异步方法

        /// <summary>
        /// 保存认证信息
        /// </summary>
        /// <param name="loginResponse">登录响应数据(包含Token、RefreshToken、用户信息)</param>
        /// <param name="rememberMe">是否持久化存储(true=保存到文件,false=仅内存)</param>
        Task SaveAuthenticationAsync(LoginResponse loginResponse, bool rememberMe);

        /// <summary>
        /// 获取当前保存的Token
        /// </summary>
        Task<string?> GetTokenAsync();

        /// <summary>
        /// 获取当前保存的RefreshToken
        /// </summary>
        Task<string?> GetRefreshTokenAsync();

        /// <summary>
        /// 获取完整的登录响应数据
        /// </summary>
        Task<LoginResponse?> GetLoginResponseAsync();

        /// <summary>
        /// 清除所有认证信息
        /// </summary>
        Task ClearAuthenticationAsync();

        /// <summary>
        /// 检查Token是否过期
        /// </summary>
        Task<bool> IsTokenExpiredAsync();

        #endregion

        #region 同步方法 (用于属性访问等无法使用async的场景)

        /// <summary>
        /// 同步获取当前Token (用于属性访问)
        /// </summary>
        /// <remarks>
        /// 适用于：
        /// - 属性getter
        /// - 同步回调
        /// - 无法使用async的接口实现
        /// 注意：仅当底层存储为内存时是安全的
        /// </remarks>
        string? GetToken();

        /// <summary>
        /// 同步获取登录响应数据 (用于属性访问)
        /// </summary>
        LoginResponse? GetLoginResponse();

        /// <summary>
        /// 同步清除认证信息
        /// </summary>
        void ClearAuthentication();

        #endregion
    }
}
