using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using LYBT.Desktop.Core.Services;
using LYBT.Desktop.Core.Services.Exceptions;
using LYBT.Shared.Interfaces.Api;
using LYBT.Shared.Interfaces.Services;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Auth;
using LYBT.Shared.Models.Contracts.Users;

namespace LYBT.Desktop.Auth.Services
{
    /// <summary>
    /// 认证服务 - 精简版实现
    /// 基于BaseApiService提供统一的错误处理和重试机制
    /// 直接调用后端API，不维护本地会话状态
    /// </summary>
    public class AuthService : BaseApiService<IAuthApi>, IAuthService
    {
        public AuthService(
            IAuthApi authApi,
            IExceptionHandler exceptionHandler,
            ILogger<AuthService> logger)
            : base(authApi, logger, exceptionHandler)
        {
        }

        /// <summary>
        /// 用户登录验证
        /// </summary>
        public async Task<ServiceResult<LoginResponse>> LoginAsync(LoginRequest request)
        {
            if (request == null)
                return ServiceResult<LoginResponse>.Failure("登录请求不能为空");

            if (string.IsNullOrWhiteSpace(request.Username))
                return ServiceResult<LoginResponse>.Failure("用户名不能为空");

            if (string.IsNullOrWhiteSpace(request.Password))
                return ServiceResult<LoginResponse>.Failure("密码不能为空");

            try
            {
                var response = await Api.LoginAsync(request);
                if (response != null && response.Success && response.Data != null)
                {
                    Logger.LogInformation("用户 {Username} 登录成功", request.Username);
                    return ServiceResult<LoginResponse>.Success(response.Data);
                }

                var errorMessage = response?.Message ?? "登录失败";
                Logger.LogWarning("用户 {Username} 登录失败: {Error}", request.Username, errorMessage);
                return ServiceResult<LoginResponse>.Failure(errorMessage);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "用户 {Username} 登录异常", request.Username);
                return ServiceResult<LoginResponse>.Failure($"登录失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 用户登出
        /// </summary>
        public async Task<ServiceResult<bool>> LogoutAsync(LogoutRequest request)
        {
            try
            {
                var response = await Api.LogoutAsync();
                if (response != null && response.Success)
                {
                    Logger.LogInformation("用户登出成功");
                    return ServiceResult<bool>.Success(true);
                }

                var errorMessage = response?.Message ?? "登出失败";
                Logger.LogWarning("用户登出失败: {Error}", errorMessage);
                return ServiceResult<bool>.Failure(errorMessage);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "用户登出异常");
                // 即使API调用失败，也返回成功，确保客户端清理本地会话
                return ServiceResult<bool>.Success(true);
            }
        }

        /// <summary>
        /// 修改sysadmin密码
        /// </summary>
        public async Task<ServiceResult<bool>> ChangeSysAdminPasswordAsync(ChangeSysAdminPassword request)
        {
            if (request == null)
                return ServiceResult<bool>.Failure("修改密码请求不能为空");

            if (string.IsNullOrWhiteSpace(request.OldPassword))
                return ServiceResult<bool>.Failure("原密码不能为空");

            if (string.IsNullOrWhiteSpace(request.NewPassword))
                return ServiceResult<bool>.Failure("新密码不能为空");

            if (request.NewPassword.Length < 6)
                return ServiceResult<bool>.Failure("新密码长度至少6位");

            try
            {
                var changePasswordRequest = new ChangePasswordRequest
                {
                    OldPassword = request.OldPassword,
                    NewPassword = request.NewPassword
                };

                var response = await Api.ChangePasswordAsync(changePasswordRequest);
                if (response != null && response.Success)
                {
                    Logger.LogInformation("管理员密码修改成功");
                    return ServiceResult<bool>.Success(true);
                }

                var errorMessage = response?.Message ?? "密码修改失败";
                Logger.LogWarning("管理员密码修改失败: {Error}", errorMessage);
                return ServiceResult<bool>.Failure(errorMessage);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "管理员密码修改异常");
                return ServiceResult<bool>.Failure($"密码修改失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 验证用户凭据
        /// </summary>
        public async Task<ServiceResult<string>> VerifyCredentialsAsync(LoginRequest request)
        {
            if (request == null)
                return ServiceResult<string>.Failure("验证请求不能为空");

            if (string.IsNullOrWhiteSpace(request.Username))
                return ServiceResult<string>.Failure("用户名不能为空");

            if (string.IsNullOrWhiteSpace(request.Password))
                return ServiceResult<string>.Failure("密码不能为空");

            // 使用登录API来验证凭据
            var loginResult = await LoginAsync(request);
            if (loginResult.IsSuccess && loginResult.Data != null)
            {
                return ServiceResult<string>.Success(loginResult.Data.Token);
            }

            return ServiceResult<string>.Failure(loginResult.ErrorMessage ?? "凭据验证失败");
        }

        /// <summary>
        /// 刷新Token
        /// </summary>
        public async Task<ServiceResult<LoginResponse>> RefreshTokenAsync(string refreshToken)
        {
            if (string.IsNullOrEmpty(refreshToken))
                return ServiceResult<LoginResponse>.Failure("刷新令牌不能为空");

            try
            {
                // 注意: RefreshTokenAsync在API中不需要参数，令牌通过HTTP头传递
                var response = await Api.RefreshTokenAsync();
                if (response != null && response.Success && response.Data != null)
                {
                    Logger.LogInformation("令牌刷新成功");
                    return ServiceResult<LoginResponse>.Success(response.Data);
                }

                var errorMessage = response?.Message ?? "令牌刷新失败";
                Logger.LogWarning("令牌刷新失败: {Error}", errorMessage);
                return ServiceResult<LoginResponse>.Failure(errorMessage);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "令牌刷新异常");
                return ServiceResult<LoginResponse>.Failure($"令牌刷新失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 验证Token有效性
        /// </summary>
        public async Task<ServiceResult<bool>> ValidateTokenAsync(string token)
        {
            if (string.IsNullOrEmpty(token))
                return ServiceResult<bool>.Failure("Token不能为空");

            try
            {
                // 使用GetCurrentUserAsync来验证token有效性
                var response = await Api.GetCurrentUserAsync();
                if (response != null && response.Success && response.Data != null)
                {
                    return ServiceResult<bool>.Success(true);
                }

                return ServiceResult<bool>.Success(false);
            }
            catch (Exception ex)
            {
                Logger.LogDebug(ex, "Token验证失败");
                return ServiceResult<bool>.Success(false);
            }
        }

        /// <summary>
        /// 获取用户会话信息
        /// </summary>
        public async Task<ServiceResult<object>> GetSessionInfoAsync(string token)
        {
            if (string.IsNullOrEmpty(token))
                return ServiceResult<object>.Failure("Token不能为空");

            try
            {
                // 使用GetCurrentUserAsync获取会话信息
                var response = await Api.GetCurrentUserAsync();
                if (response != null && response.Success && response.Data != null)
                {
                    return ServiceResult<object>.Success(response.Data);
                }

                var errorMessage = response?.Message ?? "获取会话信息失败";
                Logger.LogWarning("获取会话信息失败: {Error}", errorMessage);
                return ServiceResult<object>.Failure(errorMessage);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "获取会话信息异常");
                return ServiceResult<object>.Failure($"获取会话信息失败: {ex.Message}");
            }
        }
    }
}