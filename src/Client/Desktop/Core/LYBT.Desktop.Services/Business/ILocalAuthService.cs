using LYBT.Shared.Interfaces.Services;
using LYBT.Shared.Models.Contracts.Auth;

namespace LYBT.Desktop.Services.Business
{
    /// <summary>
    /// Desktop本地认证服务接口
    /// 继承跨平台IAuthService，添加Desktop特定的本地存储方法
    /// </summary>
    public interface ILocalAuthService : IAuthService
    {
        /// <summary>
        /// 保存认证信息到本地（Desktop特定方法）
        /// </summary>
        /// <param name="loginResponse">登录响应数据</param>
        Task SaveAuthenticationAsync(LoginResponse loginResponse);
    }
}
