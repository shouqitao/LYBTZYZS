using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Auth;

namespace LYBT.Module.Auth.Interfaces
{
    /// <summary>
    /// 身份认证服务接口（简化版）
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

        /// <summary>
        /// 验证用户凭据
        /// </summary>
        Task<Result<string>> VerifyCredentialsAsync(LoginRequest request, CancellationToken cancellationToken = default);

        /// <summary>
        /// 验证Token有效性
        /// </summary>
        Task<Result<bool>> ValidateTokenAsync(string token);

        /// <summary>
        /// 获取用户会话信息
        /// </summary>
        Task<Result<object>> GetSessionInfoAsync(string token);

        /// <summary>
        /// 使用AutoLoginToken自动登录（已禁用）
        /// </summary>
        Task<Result<LoginResponse>> LoginWithAutoTokenAsync(AutoLoginRequest request, CancellationToken cancellationToken = default);

        /// <summary>
        /// 刷新Token（已禁用）
        /// </summary>
        Task<Result<LoginResponse>> RefreshTokenAsync(string refreshToken);
    }
}
