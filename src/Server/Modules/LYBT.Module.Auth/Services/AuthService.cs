using LYBT.Module.Auth.Interfaces;
using LYBT.Module.Users.Interfaces;
using LYBT.Shared.Models.Contracts.Auth;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Enums;
using LYBT.Entities.Users;
using Microsoft.Extensions.Logging;

namespace LYBT.Module.Auth.Services
{
    /// <summary>
    /// 认证服务 - 简化版本（删除UltraThink双层架构）
    /// </summary>
    public class AuthService : IAuthService
    {
        private readonly IEnhancedJwtService _jwtService;
        private readonly IUserService _userService;
        private readonly ILogger<AuthService> _logger;

        public AuthService(
            IEnhancedJwtService jwtService,
            IUserService userService,
            ILogger<AuthService> logger)
        {
            _jwtService = jwtService ?? throw new ArgumentNullException(nameof(jwtService));
            _userService = userService ?? throw new ArgumentNullException(nameof(userService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        #region 核心认证操作

        /// <summary>
        /// 验证用户凭据
        /// </summary>
        public async Task<ServiceResult<string>> VerifyCredentialsAsync(LoginRequest request)
        {
            try
            {
                // 1. 根据用户名或邮箱获取用户
                var userResult = await _userService.GetByUsernameOrEmailAsync(request.Username);
                if (!userResult.IsSuccess || userResult.Data == null)
                {
                    _logger.LogWarning("用户登录失败：用户不存在 {Username}", request.Username);
                    return ServiceResult<string>.Failure("用户名或密码错误");
                }

                var user = userResult.Data;

                // 2. 检查账户状态
                if (user.Status != CommonStatus.Enabled)
                {
                    _logger.LogWarning("用户登录失败：账户已禁用 {UserId}", user.Id);
                    return ServiceResult<string>.Failure("账户已被禁用");
                }

                // 3. 检查账户是否被锁定
                var lockResult = await _userService.IsAccountLockedAsync(user.Id);
                if (lockResult.IsSuccess && lockResult.Data)
                {
                    _logger.LogWarning("用户登录失败：账户已锁定 {UserId}", user.Id);
                    return ServiceResult<string>.Failure("账户已被锁定，请稍后再试");
                }

                // 4. 验证密码
                var passwordResult = await _userService.ValidatePasswordAsync(user.Id, request.Password);
                if (!passwordResult.IsSuccess || !passwordResult.Data)
                {
                    // 增加失败登录次数
                    await _userService.IncrementFailedLoginCountAsync(user.Id);
                    _logger.LogWarning("用户登录失败：密码错误 {UserId}", user.Id);
                    return ServiceResult<string>.Failure("用户名或密码错误");
                }

                // 5. 登录成功，重置失败次数并更新最后登录时间
                await _userService.ResetFailedLoginCountAsync(user.Id);
                await _userService.UpdateLastLoginTimeAsync(user.Id);

                _logger.LogInformation("用户登录成功 {UserId}", user.Id);
                return ServiceResult<string>.Success(user.Id.ToString());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "验证用户凭据时发生异常");
                return ServiceResult<string>.Failure("验证失败");
            }
        }

        /// <summary>
        /// 修改系统管理员密码
        /// </summary>
        public async Task<ServiceResult<bool>> ChangeSysAdminPasswordAsync(ChangeSysAdminPassword request)
        {
            try
            {
                // 查找系统管理员用户
                var adminResult = await _userService.GetByUsernameAsync("admin");
                if (!adminResult.IsSuccess || adminResult.Data == null)
                {
                    return ServiceResult<bool>.Failure("系统管理员账户不存在");
                }

                var admin = adminResult.Data;
                
                // 重置密码
                var resetResult = await _userService.ResetPasswordAsync(admin.Id, request.NewPassword);
                if (!resetResult.IsSuccess)
                {
                    return ServiceResult<bool>.Failure(resetResult.ErrorMessage ?? "密码修改失败");
                }

                _logger.LogInformation("系统管理员密码修改成功");
                return ServiceResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "修改系统管理员密码时发生异常");
                return ServiceResult<bool>.Failure("密码修改失败");
            }
        }

        #endregion

        #region 认证流程操作

        /// <summary>
        /// 用户登录
        /// </summary>
        public async Task<ServiceResult<LoginResponse>> LoginAsync(LoginRequest request)
        {
            try
            {
                // 1. 验证用户凭据
                var credentialsResult = await VerifyCredentialsAsync(request);
                if (!credentialsResult.IsSuccess)
                {
                    return ServiceResult<LoginResponse>.Failure(credentialsResult.ErrorMessage ?? "登录失败");
                }

                var userId = Guid.Parse(credentialsResult.Data!);

                // 2. 获取用户详细信息
                var userResult = await _userService.GetByIdAsync(userId);
                if (!userResult.IsSuccess || userResult.Data == null)
                {
                    return ServiceResult<LoginResponse>.Failure("获取用户信息失败");
                }

                var user = userResult.Data;

                // 3. 创建User实体（EnhancedJwtService需要）
                var userEntity = new User
                {
                    Id = user.Id,
                    UsernName = user.UserName ?? user.Email ?? "",
                    RealName = user.RealName ?? "",
                    Email = user.Email ?? "",
                    Role = user.Role
                };

                // 4. 生成JWT Token对
                var tokenPair = await _jwtService.GenerateTokenPairAsync(
                    userEntity, 
                    request.DeviceId, 
                    request.DeviceName);

                // 5. 构造登录响应
                var response = new LoginResponse
                {
                    Token = tokenPair.AccessToken,
                    RefreshToken = tokenPair.RefreshToken,
                    ExpiresAt = DateTime.UtcNow.AddSeconds(tokenPair.ExpiresIn),
                    User = user
                };

                _logger.LogInformation("用户登录成功，已生成Token {UserId}", userId);
                return ServiceResult<LoginResponse>.Success(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "用户登录时发生异常");
                return ServiceResult<LoginResponse>.Failure("登录失败");
            }
        }

        /// <summary>
        /// 用户登出
        /// </summary>
        public async Task<ServiceResult<bool>> LogoutAsync(LogoutRequest request)
        {
            try
            {
                // 如果有RefreshToken，撤销它
                if (!string.IsNullOrEmpty(request.RefreshToken))
                {
                    await _jwtService.RevokeTokenAsync(request.RefreshToken);
                    _logger.LogInformation("用户登出，已撤销RefreshToken {Username}", request.Username);
                }

                return ServiceResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "用户登出时发生异常 {Username}", request.Username);
                return ServiceResult<bool>.Failure("登出失败");
            }
        }

        /// <summary>
        /// 刷新令牌
        /// </summary>
        public async Task<ServiceResult<LoginResponse>> RefreshTokenAsync(string refreshToken)
        {
            try
            {
                // 1. 使用EnhancedJwtService刷新Token
                var tokenPair = await _jwtService.RefreshTokenAsync(refreshToken);

                // 2. 构造登录响应
                var response = new LoginResponse
                {
                    Token = tokenPair.AccessToken,
                    RefreshToken = tokenPair.RefreshToken,
                    ExpiresAt = DateTime.UtcNow.AddSeconds(tokenPair.ExpiresIn),
                    // User信息可以从新Token中解析获得
                    User = new UserDto() // TODO: 从Token中提取用户信息
                };

                _logger.LogInformation("Token刷新成功");
                return ServiceResult<LoginResponse>.Success(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "刷新Token时发生异常");
                return ServiceResult<LoginResponse>.Failure("Token刷新失败");
            }
        }

        /// <summary>
        /// 验证令牌
        /// </summary>
        public async Task<ServiceResult<bool>> ValidateTokenAsync(string token)
        {
            try
            {
                var validationResult = await _jwtService.ValidateTokenSecurityAsync(token);
                return ServiceResult<bool>.Success(validationResult.IsValid);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "验证Token时发生异常");
                return ServiceResult<bool>.Failure("Token验证失败");
            }
        }

        /// <summary>
        /// 获取会话信息
        /// </summary>
        public async Task<ServiceResult<object>> GetSessionInfoAsync(string token)
        {
            try
            {
                var validationResult = await _jwtService.ValidateTokenSecurityAsync(token);
                if (!validationResult.IsValid)
                {
                    return ServiceResult<object>.Failure("Token无效");
                }

                // TODO: 从Token中提取用户信息
                var sessionInfo = new
                {
                    Valid = true,
                    SecurityLevel = validationResult.SecurityLevel.ToString(),
                    Message = "Token有效"
                };

                return ServiceResult<object>.Success(sessionInfo);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取会话信息时发生异常");
                return ServiceResult<object>.Failure("获取会话信息失败");
            }
        }

        #endregion
    }
}