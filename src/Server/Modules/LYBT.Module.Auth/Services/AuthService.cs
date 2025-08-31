using System;
using System.Threading.Tasks;
using LYBT.Shared.Interfaces.Services;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Core;
using LYBT.Shared.Models.Contracts.Auth;

namespace LYBT.Module.Auth.Services
{
    /// <summary>
    /// 认证服务 - UltraThink三层架构（纯委托模式）
    /// 职责：纯粹的服务委托，将请求分发到对应的专业服务层
    /// 三层架构：Core(CRUD) + Query(查询) + Business(业务逻辑)
    /// </summary>
    public class AuthService : IAuthService
    {
        private readonly AuthServiceCore _coreService;
        private readonly AuthQueryService _queryService;
        private readonly AuthBusinessService _businessService;

        public AuthService(
            AuthServiceCore coreService,
            AuthQueryService queryService,
            AuthBusinessService businessService)
        {
            _coreService = coreService ?? throw new ArgumentNullException(nameof(coreService));
            _queryService = queryService ?? throw new ArgumentNullException(nameof(queryService));
            _businessService = businessService ?? throw new ArgumentNullException(nameof(businessService));
        }

        #region Core Authentication Operations (委托给CoreService)

        public async Task<ServiceResult<string>> VerifyCredentialsAsync(LoginRequest request)
        {
            var userResult = await _coreService.GetUserForAuthenticationAsync(request.Username);
            if (!userResult.IsSuccess)
                return ServiceResult<string>.Failure(userResult.ErrorMessage ?? "获取用户信息失败");

            var passwordResult = await _coreService.ValidatePasswordAsync(userResult.Data!, request.Password);
            if (!passwordResult.IsSuccess || !passwordResult.Data)
                return ServiceResult<string>.Failure("用户名或密码错误");

            return ServiceResult<string>.Success("凭据验证成功");
        }

        public async Task<ServiceResult<bool>> ChangeSysAdminPasswordAsync(ChangeSysAdminPassword request)
            => await _coreService.ChangeSysAdminPasswordAsync(request.NewPassword);

        #endregion

        #region Query Operations

        public async Task<ServiceResult<bool>> ValidateTokenAsync(string token)
            => await _queryService.ValidateTokenAsync(token);

        public async Task<ServiceResult<object>> GetSessionInfoAsync(string token)
            => await _queryService.GetSessionInfoAsync(token);

        #endregion

        #region Business Operations

        public async Task<ServiceResult<LoginResponse>> LoginAsync(LoginRequest request)
            => await _businessService.ProcessLoginAsync(request);

        public async Task<ServiceResult<bool>> LogoutAsync(LogoutRequest request)
            => await _businessService.ProcessLogoutAsync(request);

        public async Task<ServiceResult<LoginResponse>> RefreshTokenAsync(string refreshToken)
            => await _businessService.RefreshAccessTokenAsync(refreshToken);

        #endregion
    }
}