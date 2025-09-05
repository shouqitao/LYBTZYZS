using System;
using System.Threading.Tasks;
using LYBT.Module.Auth.Interfaces;
using LYBT.Shared.Interfaces.Services;
using LYBT.Shared.Models.Contracts.Auth;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Core;

namespace LYBT.Module.Auth.Services
{
    /// <summary>
    /// 认证服务 - UltraThink简化架构（纯委托模式）
    /// 职责：纯粹的服务委托，将请求分发到简化的AuthCore服务
    /// 简化架构：AuthCore(统一核心功能) + JwtService(JWT处理)
    /// </summary>
    public class AuthService(
        IAuthQueryService queryService,
        IAuthBusinessService businessService) : IAuthService
    {
        private readonly IAuthQueryService _queryService = queryService ?? throw new ArgumentNullException(nameof(queryService));
        private readonly IAuthBusinessService _businessService = businessService ?? throw new ArgumentNullException(nameof(businessService));

        #region 核心认证操作

        /// <summary>
        /// 验证用户凭据
        /// </summary>
        public async Task<ServiceResult<string>> VerifyCredentialsAsync(LoginRequest request)
            => await _businessService.VerifyCredentialsAsync(request);

        /// <summary>
        /// 修改系统管理员密码
        /// </summary>
        public async Task<ServiceResult<bool>> ChangeSysAdminPasswordAsync(ChangeSysAdminPassword request)
            => await _businessService.ChangeSysAdminPasswordAsync(request.NewPassword);

        #endregion

        #region 认证流程操作

        /// <summary>
        /// 用户登录
        /// </summary>
        public async Task<ServiceResult<LoginResponse>> LoginAsync(LoginRequest request)
            => await _businessService.ProcessLoginAsync(request);

        /// <summary>
        /// 用户登出
        /// </summary>
        public async Task<ServiceResult<bool>> LogoutAsync(LogoutRequest request)
            => await _businessService.ProcessLogoutAsync(request);

        #endregion

        #region Token和会话操作

        /// <summary>
        /// 验证Token有效性
        /// </summary>
        public async Task<ServiceResult<bool>> ValidateTokenAsync(string token)
            => await _queryService.ValidateTokenAsync(token);

        /// <summary>
        /// 获取会话信息
        /// </summary>
        public async Task<ServiceResult<object>> GetSessionInfoAsync(string token)
            => await _queryService.GetSessionInfoAsync(token);

        /// <summary>
        /// 刷新Token - UltraThink简化版（移除复杂刷新机制）
        /// </summary>
        public async Task<ServiceResult<LoginResponse>> RefreshTokenAsync(string refreshToken)
        {
            // UltraThink简化版：移除复杂的刷新令牌机制
            // 小诊所场景下，直接要求重新登录更简单可靠
            await Task.CompletedTask;
            return ServiceResult<LoginResponse>.Failure("请重新登录以获取新的访问令牌");
        }

        #endregion
    }
}
