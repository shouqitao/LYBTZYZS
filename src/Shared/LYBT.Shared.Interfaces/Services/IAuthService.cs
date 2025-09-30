using LYBT.Shared.Models.Contracts.Auth;
using LYBT.Shared.Models.Contracts.Common;

namespace LYBT.Shared.Interfaces.Services
{

    /// <summary>
    /// 身份认证服务接口 - UltraThink统一标准
    /// </summary>
    public interface IAuthService
    {

        /// <summary>
        /// 用户登录验证
        /// </summary>
        Task<ServiceResult<LoginResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);

        /// <summary>
        /// 用户登出
        /// </summary>
        Task<ServiceResult<bool>> LogoutAsync(LogoutRequest request);

        /// <summary>
        /// 修改sysadmin密码
        /// </summary>
        Task<ServiceResult<bool>> ChangeSysAdminPasswordAsync(ChangeSysAdminPassword request);

        /// <summary>
        /// 验证用户凭据
        /// </summary>
        Task<ServiceResult<string>> VerifyCredentialsAsync(LoginRequest request, CancellationToken cancellationToken = default);

        /// <summary>
        /// 刷新Token
        /// </summary>
        Task<ServiceResult<LoginResponse>> RefreshTokenAsync(string refreshToken);

        /// <summary>
        /// 验证Token有效性
        /// </summary>
        Task<ServiceResult<bool>> ValidateTokenAsync(string token);

        /// <summary>
        /// 获取用户会话信息
        /// </summary>
        Task<ServiceResult<object>> GetSessionInfoAsync(string token);

        /// <summary>
        /// 撤销RefreshToken
        /// </summary>
        Task<ServiceResult<bool>> RevokeTokenAsync(RevokeTokenRequest request);

        /// <summary>
        /// 保存认证信息到本地
        /// </summary>
        Task SaveAuthenticationAsync(LoginResponse loginResponse);
    }
}
