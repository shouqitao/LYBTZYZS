using LYBT.Module.Auth.Interfaces;
using LYBT.Module.Users.Interfaces;
using LYBT.Shared.Interfaces.Services;
using LYBT.Shared.Models.Contracts.Auth;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Logging;

namespace LYBT.Module.Auth.Services
{
    /// <summary>
    /// 认证服务 - 简化版本（遵循适度设计原则）
    /// 仅提供小型中医诊所系统所需的基础认证功能，移除企业级复杂功能
    /// </summary>
    public class AuthService : IAuthService
    {
        private readonly IJwtService _jwtService;
        private readonly LYBT.Module.Users.Interfaces.IUserService _userService;
        private readonly ILogger<AuthService> _logger;

        public AuthService(
            IJwtService jwtService,
            LYBT.Module.Users.Interfaces.IUserService userService,
            ILogger<AuthService> logger)
        {
            _jwtService = jwtService;
            _userService = userService;
            _logger = logger;
        }

        #region 核心认证操作

        /// <summary>
        /// 验证用户凭据
        /// </summary>
        public async Task<ServiceResult<string>> VerifyCredentialsAsync(LoginRequest request)
        {
            if (string.IsNullOrEmpty(request.Username) || string.IsNullOrEmpty(request.Password))
                return ServiceResult<string>.Failure("用户名和密码不能为空");

            try
            {
                var userResult = await _userService.GetByUsernameAsync(request.Username);
                if (!userResult.IsSuccess || userResult.Data == null)
                    return ServiceResult<string>.Failure("用户名或密码错误");

                // 使用用户服务验证密码
                var passwordValidation = await _userService.ValidatePasswordAsync(userResult.Data.Id, request.Password);
                if (passwordValidation.IsSuccess && passwordValidation.Data)
                {
                    _logger.LogInformation("用户 {Username} 认证成功", request.Username);
                    return ServiceResult<string>.Success(userResult.Data.Id.ToString());
                }

                _logger.LogWarning("用户 {Username} 认证失败：密码错误", request.Username);
                return ServiceResult<string>.Failure("用户名或密码错误");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "验证用户凭据时发生错误");
                return ServiceResult<string>.Failure("认证过程中发生错误");
            }
        }

        /// <summary>
        /// 修改系统管理员密码
        /// </summary>
        public async Task<ServiceResult<bool>> ChangeSysAdminPasswordAsync(ChangeSysAdminPassword request)
        {
            // 简化实现：暂不支持此功能
            await Task.CompletedTask;
            return ServiceResult<bool>.Failure("系统管理员密码修改功能暂未实现");
        }

        #endregion 核心认证操作

        #region 认证流程操作

        /// <summary>
        /// 用户登录
        /// </summary>
        public async Task<ServiceResult<LoginResponse>> LoginAsync(LoginRequest request)
        {
            try
            {
                // 验证凭据
                var credentialsResult = await VerifyCredentialsAsync(request);
                if (!credentialsResult.IsSuccess)
                    return ServiceResult<LoginResponse>.Failure(credentialsResult.Message);

                // 获取用户信息
                var userResult = await _userService.GetByUsernameAsync(request.Username);
                if (!userResult.IsSuccess || userResult.Data == null)
                    return ServiceResult<LoginResponse>.Failure("获取用户信息失败");

                var user = userResult.Data;

                // 生成JWT令牌
                var token = _jwtService.GenerateToken(
                    user.Id.ToString(),
                    user.UserName,
                    user.Role);

                var response = new LoginResponse
                {
                    Token = token,
                    User = user,
                    RefreshToken = "", // 简化版本不使用RefreshToken
                    ExpiresAt = DateTime.UtcNow.AddHours(8) // 简化：固定8小时过期
                };

                _logger.LogInformation("用户 {Username} 登录成功", request.Username);
                return ServiceResult<LoginResponse>.Success(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "用户登录时发生错误");
                return ServiceResult<LoginResponse>.Failure("登录过程中发生错误");
            }
        }

        /// <summary>
        /// 用户登出
        /// </summary>
        public async Task<ServiceResult<bool>> LogoutAsync(LogoutRequest request)
        {
            // 简化实现：无状态JWT，登出仅在客户端清除令牌
            await Task.CompletedTask;
            return ServiceResult<bool>.Success(true, "登出成功");
        }

        /// <summary>
        /// 刷新令牌（简化版本不支持）
        /// </summary>
        public async Task<ServiceResult<LoginResponse>> RefreshTokenAsync(string refreshToken)
        {
            await Task.CompletedTask;
            return ServiceResult<LoginResponse>.Failure("简化版本不支持令牌刷新，请重新登录");
        }

        /// <summary>
        /// 验证令牌
        /// </summary>
        public async Task<ServiceResult<bool>> ValidateTokenAsync(string token)
        {
            await Task.CompletedTask;

            try
            {
                var principal = _jwtService.ValidateToken(token);
                return ServiceResult<bool>.Success(principal != null);
            }
            catch
            {
                return ServiceResult<bool>.Success(false);
            }
        }

        /// <summary>
        /// 获取会话信息
        /// </summary>
        public async Task<ServiceResult<object>> GetSessionInfoAsync(string token)
        {
            await Task.CompletedTask;

            try
            {
                var principal = _jwtService.ValidateToken(token);
                if (principal == null)
                    return ServiceResult<object>.Failure("令牌无效");

                var sessionInfo = new
                {
                    UserId = principal.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value,
                    UserName = principal.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value,
                    Role = principal.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value
                };

                return ServiceResult<object>.Success(sessionInfo);
            }
            catch
            {
                return ServiceResult<object>.Failure("获取会话信息失败");
            }
        }

        /// <summary>
        /// 撤销RefreshToken（简化版本不支持）
        /// </summary>
        public async Task<ServiceResult<bool>> RevokeTokenAsync(RevokeTokenRequest request)
        {
            await Task.CompletedTask;
            return ServiceResult<bool>.Success(true, "简化版本无需撤销令牌");
        }

        #endregion 认证流程操作

        // 移除私有密码验证方法，改为委托给用户服务进行验证
        // 这样符合单一职责原则，认证服务专注于认证流程，密码验证交给用户服务
    }
}