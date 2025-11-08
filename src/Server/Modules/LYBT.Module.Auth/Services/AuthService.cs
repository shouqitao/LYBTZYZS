using AutoMapper;
using LYBT.Infrastructure.Data;
using LYBT.Module.Auth.Interfaces;
using LYBT.Module.Auth.Models;
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
        private readonly ITokenRevocationService _revocationService; // Issue #1872
        private readonly ISecurityAuditService _auditService; // Issue #1872

        public AuthService(
            IJwtService jwtService,
            IUserRepository userRepository,
            IMapper mapper,
            ILogger<AuthService> logger,
            AppDbContext dbContext,
            IConfiguration configuration,
            ITokenRevocationService revocationService, // Issue #1872
            ISecurityAuditService auditService) // Issue #1872
        {
            _jwtService = jwtService;
            _userRepository = userRepository;
            _mapper = mapper;
            _logger = logger;
            _dbContext = dbContext;
            _configuration = configuration;
            _revocationService = revocationService; // Issue #1872
            _auditService = auditService; // Issue #1872
        }

        #region 核心认证操作

        /// <summary>
        /// 验证用户凭据（统一认证）
        /// Issue #1008: 改为直接使用IUserRepository和BCrypt验证
        /// Issue #1909: 三角色统一认证（SuperAdmin/Admin/Doctor）
        /// </summary>
        public async Task<ServiceResult<string>> VerifyCredentialsAsync(LoginRequest request, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(request.UserName) || string.IsNullOrEmpty(request.Password))
                return ServiceResult<string>.Failure("用户名和密码不能为空");

            try
            {
                // 统一认证流程 - 所有用户（包括SuperAdmin）都从Users表验证
                var userEntity = await _userRepository.GetByUsernameAsync(request.UserName);
                if (userEntity == null)
                    return ServiceResult<string>.Failure("用户名或密码错误");

                // 使用BCrypt验证密码
                if (BCrypt.Net.BCrypt.Verify(request.Password, userEntity.PasswordHash))
                {
                    _logger.LogInformation("用户认证成功 [用户名: {UserName}] [角色: {Role}] [时间: {Timestamp}]",
                        request.UserName, userEntity.Role, DateTime.UtcNow);
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

        // Issue #1909: ChangeSysAdminPasswordAsync方法已移除
        // SuperAdmin现在统一使用UserService.ChangePasswordAsync进行密码修改

        #endregion 核心认证操作

        #region 认证流程操作

        /// <summary>
        /// 用户登录（统一流程）
        /// Issue #1909: 三角色统一登录流程（SuperAdmin/Admin/Doctor）
        /// </summary>
        public async Task<ServiceResult<LoginResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
        {
            try
            {
                // 验证凭据
                var credentialsResult = await VerifyCredentialsAsync(request, cancellationToken);
                if (!credentialsResult.IsSuccess)
                {
                    // Issue #1872: 记录登录失败审计日志
                    await _auditService.LogAsync(new SecurityAuditEvent
                    {
                        EventType = "LoginFailed",
                        UserName = request.UserName,
                        Success = false,
                        ErrorMessage = credentialsResult.Message ?? "凭据验证失败"
                    });
                    return ServiceResult<LoginResponse>.Failure(credentialsResult.Message ?? "凭据验证失败");
                }

                // 统一用户登录流程（包括SuperAdmin） - 所有角色都从Users表获取
                var userEntity = await _userRepository.GetByUsernameAsync(request.UserName);
                if (userEntity == null)
                    return ServiceResult<LoginResponse>.Failure("获取用户信息失败");

                var userDto = _mapper.Map<UserDto>(userEntity);

                // 确定用户类型（SuperAdmin特殊处理UserType）
                string userType = userDto.Role == UserRole.SuperAdmin ? "superadmin" : "user";

                // 生成JWT令牌
                var token = _jwtService.GenerateToken(
                    userDto.Id.ToString(),
                    userDto.UserName,
                    userDto.Role,
                    userType); // Issue #1909: 根据角色设置正确的user_type claim

                // Issue #1838: 生成并存储RefreshToken
                var refreshToken = GenerateRefreshToken();
                var refreshTokenExpireDays = _configuration.GetValue<int?>("Lybt:Jwt:RefreshTokenExpirationDays") ?? 7;
                var tokenExpireMinutes = _configuration.GetValue<int?>("Lybt:Jwt:ExpireMinutes") ?? 15;

                var refreshTokenRecord = new LYBT.Entities.Auth.RefreshToken
                {
                    Token = refreshToken,
                    UserId = userDto.Id,
                    UserType = userType, // Issue #1909: 根据角色设置用户类型
                    Jti = Guid.NewGuid().ToString(),
                    ExpiresAt = DateTime.UtcNow.AddDays(refreshTokenExpireDays),
                    FamilyId = Guid.NewGuid().ToString() // 新家族ID
                };
                _dbContext.RefreshTokens.Add(refreshTokenRecord);
                await _dbContext.SaveChangesAsync();

                var response = new LoginResponse
                {
                    Token = token,
                    User = userDto,
                    RefreshToken = refreshToken, // Issue #1838: 返回RefreshToken
                    ExpiresAt = DateTime.UtcNow.AddMinutes(tokenExpireMinutes)
                };

                // Issue #1872: 记录登录成功审计日志
                await _auditService.LogAsync(new SecurityAuditEvent
                {
                    EventType = "Login",
                    UserId = userDto.Id,
                    UserType = userType,
                    UserName = userDto.UserName,
                    Success = true
                });

                _logger.LogInformation("用户登录成功 [用户名: {UserName}] [角色: {Role}] [时间: {Timestamp}]",
                    request.UserName, userDto.Role, DateTime.UtcNow);

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
        /// Issue #1872: 集成Token撤销和审计日志
        /// </summary>
        public async Task<ServiceResult<bool>> LogoutAsync(LogoutRequest request)
        {
            try
            {
                // Issue #1872: 查找并撤销RefreshToken
                var tokenRecord = await _dbContext.RefreshTokens
                    .FirstOrDefaultAsync(t => t.Token == request.RefreshToken);

                if (tokenRecord != null && !tokenRecord.IsRevoked && !string.IsNullOrEmpty(request.RefreshToken))
                {
                    await _revocationService.RevokeTokenAsync(request.RefreshToken, "用户主动登出");
                }

                // Issue #1872: 记录登出审计日志
                await _auditService.LogAsync(new SecurityAuditEvent
                {
                    EventType = "Logout",
                    UserId = tokenRecord?.UserId,
                    UserType = tokenRecord?.UserType,
                    UserName = request.Username, // 修正：属性名为Username
                    Success = true
                });

                return ServiceResult<bool>.Success(true, "登出成功");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "用户登出时发生错误");
                // 即使撤销失败，也返回成功，因为客户端已经清除了Token
                return ServiceResult<bool>.Success(true, "登出成功");
            }
        }

        /// <summary>
        /// 刷新令牌 - Issue #1838: 实现Token自动刷新机制
        /// </summary>
        public async Task<ServiceResult<LoginResponse>> RefreshTokenAsync(string refreshToken)
        {
            if (string.IsNullOrWhiteSpace(refreshToken))
                return ServiceResult<LoginResponse>.Failure("RefreshToken不能为空");

            try
            {
                // 1. 查询RefreshToken记录
                var tokenRecord = await _dbContext.RefreshTokens
                    .FirstOrDefaultAsync(t => t.Token == refreshToken);

                if (tokenRecord == null)
                {
                    // Issue #1872: 记录Token不存在审计日志
                    await _auditService.LogAsync(new SecurityAuditEvent
                    {
                        EventType = "RefreshTokenRejected",
                        Success = false,
                        ErrorMessage = "Token不存在"
                    });
                    return ServiceResult<LoginResponse>.Failure("RefreshToken不存在");
                }

                // 2. 验证Token是否有效
                if (!tokenRecord.IsValid())
                {
                    var reason = tokenRecord.IsRevoked ? "已撤销" :
                                tokenRecord.IsDeleted ? "已删除" :
                                "已过期";

                    // Issue #1872: 记录Token无效审计日志
                    await _auditService.LogAsync(new SecurityAuditEvent
                    {
                        EventType = "RefreshTokenRejected",
                        UserId = tokenRecord.UserId,
                        UserType = tokenRecord.UserType,
                        Success = false,
                        ErrorMessage = $"Token{reason}"
                    });

                    _logger.LogWarning("RefreshToken验证失败：{Reason}", reason);
                    return ServiceResult<LoginResponse>.Failure($"RefreshToken{reason}，请重新登录");
                }

                // 3. 记录使用并检查异常使用
                tokenRecord.RecordUsage();
                if (tokenRecord.UsageCount > 100)
                {
                    _logger.LogWarning("RefreshToken使用次数异常：{Count}", tokenRecord.UsageCount);
                }

                // 4. 统一从Users表获取用户信息（包括SuperAdmin） - Issue #1909
                var userEntity = await _userRepository.GetByIdAsync(tokenRecord.UserId);
                if (userEntity == null)
                    return ServiceResult<LoginResponse>.Failure("用户不存在");

                var userDto = _mapper.Map<UserDto>(userEntity);

                // 确定用户类型（SuperAdmin特殊处理UserType）
                string userType = userDto.Role == UserRole.SuperAdmin ? "superadmin" : "user";

                _logger.LogInformation("Token刷新：[UserName: {UserName}] [Role: {Role}]", userDto.UserName, userDto.Role);

                // 5. 生成新的Access Token
                var newAccessToken = _jwtService.GenerateToken(
                    userDto.Id.ToString(),
                    userDto.UserName,
                    userDto.Role,
                    userType); // 传入userType以设置正确的user_type claim

                // 6. 生成新的Refresh Token并撤销旧Token（Token轮换）
                var newRefreshToken = GenerateRefreshToken();
                var refreshTokenExpireDays = _configuration.GetValue<int?>("Lybt:Jwt:RefreshTokenExpirationDays") ?? 7;

                // 撤销旧Token
                tokenRecord.Revoke("已被新Token替换", $"User:{userDto.Id}");
                tokenRecord.ReplacedByToken = newRefreshToken;

                // 创建新Token记录
                var newTokenRecord = new LYBT.Entities.Auth.RefreshToken
                {
                    Token = newRefreshToken,
                    UserId = userDto.Id,
                    UserType = userType, // Issue #1861: 继承用户类型
                    Jti = Guid.NewGuid().ToString(),
                    ExpiresAt = DateTime.UtcNow.AddDays(refreshTokenExpireDays),
                    FamilyId = tokenRecord.FamilyId // 继承家族ID
                };

                _dbContext.RefreshTokens.Add(newTokenRecord);
                await _dbContext.SaveChangesAsync();

                // 7. 返回新Token对
                var response = new LoginResponse
                {
                    Token = newAccessToken,
                    User = userDto,
                    RefreshToken = newRefreshToken,
                    ExpiresAt = DateTime.UtcNow.AddMinutes(
                        _configuration.GetValue<int?>("Lybt:Jwt:ExpireMinutes") ?? 15)
                };

                // Issue #1872: 记录Token刷新成功审计日志
                await _auditService.LogAsync(new SecurityAuditEvent
                {
                    EventType = "RefreshToken",
                    UserId = userDto.Id,
                    UserType = userType,
                    UserName = userDto.UserName,
                    Success = true
                });

                _logger.LogInformation("Token刷新成功 [用户: {UserName}] [时间: {Timestamp}]",
                    userDto.UserName, DateTime.UtcNow);

                return ServiceResult<LoginResponse>.Success(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "刷新Token时发生错误");
                return ServiceResult<LoginResponse>.Failure("刷新Token失败");
            }
        }

        /// <summary>
        /// 生成安全的RefreshToken字符串
        /// Issue #1838: 使用加密安全的随机数生成器
        /// </summary>
        private static string GenerateRefreshToken()
        {
            var randomBytes = new byte[64];
            using var rng = System.Security.Cryptography.RandomNumberGenerator.Create();
            rng.GetBytes(randomBytes);
            return Convert.ToBase64String(randomBytes);
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
