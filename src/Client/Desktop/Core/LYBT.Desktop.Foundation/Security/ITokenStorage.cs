using LYBT.Shared.Models.Contracts.Auth;

namespace LYBT.Desktop.Foundation.Security
{
    /// <summary>
    /// Token存储接口 - 提供加密的Token存储功能
    /// </summary>
    /// <remarks>
    /// Issue #1862: Token认证安全重构 - 使用DPAPI加密存储
    /// </remarks>
    public interface ITokenStorage
    {
        /// <summary>
        /// 保存Token到加密存储
        /// </summary>
        /// <param name="loginResponse">登录响应数据（包含Token、RefreshToken、用户信息）</param>
        /// <returns>保存操作的任务</returns>
        Task SaveTokenAsync(LoginResponse loginResponse);

        /// <summary>
        /// 从加密存储加载Token
        /// </summary>
        /// <returns>登录响应数据，如果不存在或解密失败则返回null</returns>
        Task<LoginResponse?> LoadTokenAsync();

        /// <summary>
        /// 清除加密存储中的Token
        /// </summary>
        /// <returns>清除操作的任务</returns>
        Task ClearTokenAsync();
    }
}
