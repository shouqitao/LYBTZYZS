using LYBT.Shared.Models.Contracts.Auth;

namespace LYBT.Desktop.Services.Business
{
    /// <summary>
    /// Token 存储服务接口 - 用于管理 JWT Token 的本地存储
    /// </summary>
    public interface ITokenStorageService
    {
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
    }
}
