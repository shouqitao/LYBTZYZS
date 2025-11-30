using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Auth;

namespace LYBT.Module.Auth.Interfaces
{

    /// <summary>
    /// 身份认证服务接口 - 使用统一Result返回值
    /// Issue #1008: 移除Desktop特定方法SaveAuthenticationAsync（已迁移到ILocalAuthService）
    /// </summary>
    public interface IAuthService
    {

        /// <summary>
        /// 用户登录验证
        /// </summary>
        Task<Result<LoginResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);

        /// <summary>
        /// 用户登出
        /// </summary>
        Task<Result<bool>> LogoutAsync(LogoutRequest request);

        // Issue #1909: ChangeSysAdminPasswordAsync已移除
        // SuperAdmin现在统一使用UserService.ChangePasswordAsync进行密码修改

        /// <summary>
        /// 验证用户凭据
        /// </summary>
        Task<Result<string>> VerifyCredentialsAsync(LoginRequest request, CancellationToken cancellationToken = default);

        /// <summary>
        /// 刷新Token
        /// </summary>
        Task<Result<LoginResponse>> RefreshTokenAsync(string refreshToken);

        /// <summary>
        /// 验证Token有效性
        /// </summary>
        Task<Result<bool>> ValidateTokenAsync(string token);

        /// <summary>
        /// 获取用户会话信息
        /// </summary>
        Task<Result<object>> GetSessionInfoAsync(string token);

        /// <summary>
        /// 撤销RefreshToken
        /// </summary>
        Task<Result<bool>> RevokeTokenAsync(RevokeTokenRequest request);
    }
}
