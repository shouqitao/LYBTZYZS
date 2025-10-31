using AutoMapper;
using LYBT.Infrastructure.Data;
using LYBT.Module.Auth.Interfaces;
using LYBT.Module.Users.Interfaces;
using LYBT.Shared.Models.Contracts.Auth;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace LYBT.Module.Auth.Services
{
    /// <summary>
    /// 认证服务 - 简化版本（遵循适度设计原则）
    /// Issue #1008: 改为直接使用IUserRepository，移除对IUserService的依赖
    /// 仅提供小型中医诊所系统所需的基础认证功能，移除企业级复杂功能
    /// </summary>
    public class AuthService : IAuthService
    {
        private readonly IJwtService _jwtService;
        private readonly IUserRepository _userRepository;
        private readonly IMapper _mapper;
        private readonly ILogger<AuthService> _logger;
        private readonly AppDbContext _dbContext;
        private readonly IConfiguration _configuration;

        public AuthService(
            IJwtService jwtService,
            IUserRepository userRepository,
            IMapper mapper,
            ILogger<AuthService> logger,
            AppDbContext dbContext,
            IConfiguration configuration)
        {
            _jwtService = jwtService;
            _userRepository = userRepository;
            _mapper = mapper;
            _logger = logger;
            _dbContext = dbContext;
            _configuration = configuration;
        }

        #region 超级管理员认证

        /// <summary>
        /// 检查是否为超级管理员凭据
        /// 超级管理员不在Users表中，密码哈希独立存储在AdminSecrets表
        /// 用户名从配置文件读取，不存储在数据库中，防止SQL注入后暴露账户名
        /// </summary>
        private async Task<bool> IsSuperAdminCredentials(string username, string password, CancellationToken cancellationToken = default)
        {
            try
            {
                // 从配置获取超级管理员用户名
                var configUsername = _configuration["Lybt:Business:SystemAdmin:UserName"];
                if (string.IsNullOrEmpty(configUsername))
                {
                    _logger.LogWarning("配置中未找到超级管理员用户名");
                    return false;
                }

                // 验证用户名是否匹配
                if (!string.Equals(username, configUsername, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }

                // 从AdminSecrets表获取超级管理员密码哈希
                var adminSecret = await _dbContext.AdminSecrets.FirstOrDefaultAsync(cancellationToken);
                if (adminSecret == null)
                {
                    _logger.LogWarning("AdminSecrets表为空，超级管理员未初始化");
                    return false;
                }

                // 使用 BCrypt 验证密码（与普通用户一致）
                bool isValid = BCrypt.Net.BCrypt.Verify(password, adminSecret.PasswordHash);

                if (isValid)
                {
                    _logger.LogInformation("超级管理员登录成功");
                }
                else
                {
                    _logger.LogWarning("超级管理员认证失败：密码错误");
                }

                return isValid;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "验证超级管理员凭据时发生错误");
                return false;
            }
        }

        #endregion

        #region 核心认证操作

        /// <summary>
        /// 验证用户凭据
        /// Issue #1008: 改为直接使用IUserRepository和BCrypt验证
        /// </summary>
        public async Task<ServiceResult<string>> VerifyCredentialsAsync(LoginRequest request, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(request.UserName) || string.IsNullOrEmpty(request.Password))
                return ServiceResult<string>.Failure("用户名和密码不能为空");

            try
            {
                // 首先检查是否是超级管理员登录
                if (await IsSuperAdminCredentials(request.UserName, request.Password, cancellationToken))
                {
                    _logger.LogInformation("超级管理员认证成功 [用户名: {UserName}] [时间: {Timestamp}]",
                    request.UserName, DateTime.UtcNow);
                    // 返回特殊的超级管理员标识
                    return ServiceResult<string>.Success("SUPER_ADMIN:" + request.UserName);
                }

                // 普通用户认证流程 - 直接调用Repository
                var userEntity = await _userRepository.GetByUsernameAsync(request.UserName);
                if (userEntity == null)
                    return ServiceResult<string>.Failure("用户名或密码错误");

                // 直接使用BCrypt验证密码
                if (BCrypt.Net.BCrypt.Verify(request.Password, userEntity.PasswordHash))
                {
                    _logger.LogInformation("用户认证成功 [用户名: {UserName}] [时间: {Timestamp}]",
                    request.UserName, DateTime.UtcNow);
                    return ServiceResult<string>.Success(userEntity.Id.ToString());
                }

                _logger.LogWarning("用户认证失败 [用户名: {UserName}] [原因: 密码错误] [时间: {Timestamp}]",
                request.UserName, DateTime.UtcNow);
                return ServiceResult<string>.Failure("用户名或密码错误");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "验证用户凭据时发生错误 [时间: {Timestamp}]", DateTime.UtcNow);
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
        public async Task<ServiceResult<LoginResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
        {
            try
            {
                // 验证凭据
                var credentialsResult = await VerifyCredentialsAsync(request, cancellationToken);
                if (!credentialsResult.IsSuccess)
                    return ServiceResult<LoginResponse>.Failure(credentialsResult.Message ?? "凭据验证失败");

                LoginResponse response;

                // 检查是否是超级管理员
                if (credentialsResult.Data != null && credentialsResult.Data.StartsWith("SUPER_ADMIN:"))
                {
                    // 超级管理员登录
                    var sysAdminUsername = credentialsResult.Data.Substring("SUPER_ADMIN:".Length);

                    // 生成超级管理员专用的JWT令牌
                    var token = _jwtService.GenerateToken(
                        "00000000-0000-0000-0000-000000000000", // 特殊ID表示超级管理员
                        sysAdminUsername,
                        UserRole.Admin, // 使用Admin角色，但通过特殊ID区分
                        new Dictionary<string, string>
                        {
                            { "IsSuperAdmin", "true" },
                            { "AuthSource", "AdminSecrets" }
                        });

                    response = new LoginResponse
                    {
                        Token = token,
                        User = new UserDto
                        {
                            Id = Guid.Empty, // 特殊ID
                            UserName = sysAdminUsername,
                            RealName = "系统超级管理员",
                            Role = UserRole.Admin,
                            Email = _configuration["Lybt:Business:SystemAdmin:Email"] ?? "admin@lybt.com"
                        },
                        RefreshToken = "", // 简化版本不使用RefreshToken
                        ExpiresAt = DateTime.UtcNow.AddHours(8)
                    };

                    _logger.LogInformation("超级管理员登录成功（用户名已隐藏）");
                }
                else
                {
                    // 普通用户登录流程 - Issue #1008: 改为直接调用Repository
                    var userEntity = await _userRepository.GetByUsernameAsync(request.UserName);
                    if (userEntity == null)
                        return ServiceResult<LoginResponse>.Failure("获取用户信息失败");

                    var userDto = _mapper.Map<UserDto>(userEntity);

                    // 生成JWT令牌
                    var token = _jwtService.GenerateToken(
                        userDto.Id.ToString(),
                        userDto.UserName,
                        userDto.Role);

                    response = new LoginResponse
                    {
                        Token = token,
                        User = userDto,
                        RefreshToken = "", // 简化版本不使用RefreshToken
                        ExpiresAt = DateTime.UtcNow.AddHours(8) // 简化：固定8小时过期
                    };

                    _logger.LogInformation("用户登录成功 [用户名: {UserName}] [时间: {Timestamp}]",
                    request.UserName, DateTime.UtcNow);
                }

                return ServiceResult<LoginResponse>.Success(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "用户登录时发生错误 [时间: {Timestamp}]", DateTime.UtcNow);
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

        // Issue #1008: 移除SaveAuthenticationAsync（Desktop特定方法，已迁移到ILocalAuthService）
        // 移除私有密码验证方法，改为委托给用户服务进行验证
        // 这样符合单一职责原则，认证服务专注于认证流程，密码验证交给用户服务
    }
}
