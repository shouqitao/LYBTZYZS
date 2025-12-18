using AutoMapper;
using LYBT.Infrastructure.Data;
using LYBT.Module.Auth.Interfaces;
using LYBT.Module.Auth.Models;
using LYBT.Module.Users.Interfaces;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Auth;
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
    /// Issue #1864: 重新引入IUserService实现Auth/User职责分离，密码验证委托给UserService
    /// 仅提供小型中医诊所系统所需的基础认证功能，移除企业级复杂功能
    /// </summary>
    public class AuthService : IAuthService
    {
        private readonly IJwtService _jwtService;
        private readonly IUserRepository _userRepository;
        private readonly IUserService _userService; // Issue #1864: 职责分离
        private readonly IMapper _mapper;
        private readonly ILogger<AuthService> _logger;
        private readonly AppDbContext _dbContext;
        private readonly IConfiguration _configuration;
        private readonly ITokenRevocationService _revocationService; // Issue #1872
        private readonly ISecurityAuditService _auditService; // Issue #1872

        public AuthService(
            IJwtService jwtService,
            IUserRepository userRepository,
            IUserService userService, // Issue #1864: 职责分离
            IMapper mapper,
            ILogger<AuthService> logger,
            AppDbContext dbContext,
            IConfiguration configuration,
            ITokenRevocationService revocationService, // Issue #1872
            ISecurityAuditService auditService) // Issue #1872
        {
            _jwtService = jwtService;
            _userRepository = userRepository;
            _userService = userService; // Issue #1864: 职责分离
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
        /// Issue #1864: 返回结构化错误码，职责分离委托给IUserService验证密码
        /// </summary>
        public async Task<Result<string>> VerifyCredentialsAsync(LoginRequest request, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(request.UserName) || string.IsNullOrEmpty(request.Password))
                return Result<string>.Failure(AuthErrorCode.InvalidCredentials, "用户名和密码不能为空");

            try
            {
                // Issue #1864: 职责分离 - 密码验证委托给UserService
                var validationResult = await _userService.ValidatePasswordAsync(request.UserName, request.Password);

                if (!validationResult.IsSuccess)
                {
                    // 根据错误消息映射到对应的AuthErrorCode
                    var errorCode = validationResult.ErrorMessage switch
                    {
                        "用户已被禁用" => AuthErrorCode.UserDisabled,
                        _ => AuthErrorCode.InvalidCredentials
                    };

                    _logger.LogWarning("用户认证失败 [用户名: {UserName}] [原因: {Reason}] [时间: {Timestamp}]",
                        request.UserName, validationResult.ErrorMessage, DateTime.UtcNow);
                    return Result<string>.Failure(errorCode, validationResult.ErrorMessage);
                }

                var userDto = validationResult.Data!;
                _logger.LogInformation("用户认证成功 [用户名: {UserName}] [角色: {Role}] [时间: {Timestamp}]",
                    request.UserName, userDto.Role, DateTime.UtcNow);
                return Result<string>.Success(userDto.Id.ToString());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "验证用户凭据时发生错误 [时间: {Timestamp}]", DateTime.UtcNow);
                return Result<string>.Failure(AuthErrorCode.InternalError, "认证过程中发生错误");
            }
        }

        // Issue #1909: ChangeSysAdminPasswordAsync方法已移除
        // SuperAdmin现在统一使用UserService.ChangePasswordAsync进行密码修改

        #endregion 核心认证操作

        #region 认证流程操作

        /// <summary>
        /// 用户登录（统一流程）
        /// Issue #1909: 三角色统一登录流程（SuperAdmin/Admin/Doctor）
        /// Issue #1864: 返回结构化错误码
        /// </summary>
        public async Task<Result<LoginResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
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
                        ErrorMessage = credentialsResult.ErrorMessage ?? "凭据验证失败"
                    });
                    // Issue #1864: 传递错误码
                    return Result<LoginResponse>.Failure(
                        credentialsResult.ErrorCode ?? AuthErrorCode.InvalidCredentials,
                        credentialsResult.ErrorMessage);
                }

                // 统一用户登录流程（包括SuperAdmin） - 所有角色都从Users表获取
                var userEntity = await _userRepository.GetByUsernameAsync(request.UserName);
                if (userEntity == null)
                    return Result<LoginResponse>.Failure(AuthErrorCode.UserNotFound);

                var userDto = _mapper.Map<UserDetailDto>(userEntity);

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

                return Result<LoginResponse>.Success(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "用户登录时发生错误 [时间: {Timestamp}]", DateTime.UtcNow);
                return Result<LoginResponse>.Failure(AuthErrorCode.InternalError, "登录过程中发生错误");
            }
        }

        /// <summary>
        /// 用户登出
        /// Issue #1872: 集成Token撤销和审计日志
        /// Issue #1864 AUTH-008: 支持过期Token登出
        /// Issue #1864 AUTH-009: Logout后Token必须失效，强制重新登录
        /// </summary>
        public async Task<Result<bool>> LogoutAsync(LogoutRequest request)
        {
            try
            {
                LYBT.Entities.Auth.RefreshToken? tokenRecord = null;
                string? userName = request.UserName;

                // Issue #1864: 优先通过RefreshToken查找会话
                if (!string.IsNullOrEmpty(request.RefreshToken))
                {
                    tokenRecord = await _dbContext.RefreshTokens
                        .FirstOrDefaultAsync(t => t.Token == request.RefreshToken);

                    // 从Token记录获取用户信息（用于审计日志）
                    if (tokenRecord != null && string.IsNullOrEmpty(userName))
                    {
                        var user = await _userRepository.GetByIdAsync(tokenRecord.UserId);
                        userName = user?.UserName;
                    }

                    // Issue #1864 AUTH-009: 撤销RefreshToken确保无法恢复会话
                    // 即使Token已过期或已使用，也尝试撤销以确保安全
                    if (tokenRecord != null && !tokenRecord.IsRevoked)
                    {
                        await _revocationService.RevokeTokenAsync(request.RefreshToken, "用户主动登出");

                        // Issue #1864 AUTH-007: 同时撤销整个Token Family
                        if (!string.IsNullOrEmpty(tokenRecord.FamilyId))
                        {
                            await RevokeTokenFamilyAsync(tokenRecord.FamilyId, "用户主动登出，撤销整个Token Family");
                        }
                    }
                }

                // Issue #1872: 记录登出审计日志
                await _auditService.LogAsync(new SecurityAuditEvent
                {
                    EventType = "Logout",
                    UserId = tokenRecord?.UserId,
                    UserType = tokenRecord?.UserType,
                    UserName = userName,
                    Success = true
                });

                _logger.LogInformation("用户登出成功 [用户名: {UserName}] [时间: {Timestamp}]",
                    userName ?? "(unknown)", DateTime.UtcNow);

                return Result<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "用户登出时发生错误");
                // 即使撤销失败，也返回成功，因为客户端应该清除Token
                return Result<bool>.Success(true);
            }
        }

        /// <summary>
        /// 刷新令牌 - Issue #1838: 实现Token自动刷新机制
        /// Issue #1864: 返回结构化错误码
        /// </summary>
        public async Task<Result<LoginResponse>> RefreshTokenAsync(string refreshToken)
        {
            if (string.IsNullOrWhiteSpace(refreshToken))
                return Result<LoginResponse>.Failure(AuthErrorCode.RefreshTokenInvalid, "RefreshToken不能为空");

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
                    return Result<LoginResponse>.Failure(AuthErrorCode.RefreshTokenInvalid, "RefreshToken不存在");
                }

                // 2. Issue #1864 AUTH-007: 检测重放攻击
                // 如果Token已经被使用过，说明检测到重放攻击
                if (tokenRecord.IsUsed)
                {
                    _logger.LogWarning("检测到Token重放攻击！[TokenId: {TokenId}] [FamilyId: {FamilyId}] [UserId: {UserId}]",
                        tokenRecord.Id, tokenRecord.FamilyId, tokenRecord.UserId);

                    // 安全措施：使整个Token Family失效
                    if (!string.IsNullOrEmpty(tokenRecord.FamilyId))
                    {
                        await RevokeTokenFamilyAsync(tokenRecord.FamilyId, "检测到重放攻击，整个Token Family已失效");
                    }

                    // Issue #1872: 记录重放攻击审计日志
                    await _auditService.LogAsync(new SecurityAuditEvent
                    {
                        EventType = "TokenReplayAttack",
                        UserId = tokenRecord.UserId,
                        UserType = tokenRecord.UserType,
                        Success = false,
                        ErrorMessage = "检测到Token重放攻击"
                    });

                    return Result<LoginResponse>.Failure(AuthErrorCode.TokenRevoked, "检测到安全威胁，请重新登录");
                }

                // 3. 验证Token是否有效
                if (!tokenRecord.IsValid())
                {
                    AuthErrorCode errorCode;
                    string reason;

                    if (tokenRecord.IsRevoked)
                    {
                        errorCode = AuthErrorCode.TokenRevoked;
                        reason = "已撤销";
                    }
                    else if (tokenRecord.IsDeleted)
                    {
                        errorCode = AuthErrorCode.TokenRevoked;
                        reason = "已删除";
                    }
                    else
                    {
                        errorCode = AuthErrorCode.RefreshTokenExpired;
                        reason = "已过期";
                    }

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
                    return Result<LoginResponse>.Failure(errorCode, $"RefreshToken{reason}，请重新登录");
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
                    return Result<LoginResponse>.Failure(AuthErrorCode.UserNotFound);

                var userDto = _mapper.Map<UserDetailDto>(userEntity);

                // 确定用户类型（SuperAdmin特殊处理UserType）
                string userType = userDto.Role == UserRole.SuperAdmin ? "superadmin" : "user";

                _logger.LogInformation("Token刷新：[UserName: {UserName}] [Role: {Role}]", userDto.UserName, userDto.Role);

                // 5. 生成新的Access Token
                var newAccessToken = _jwtService.GenerateToken(
                    userDto.Id.ToString(),
                    userDto.UserName,
                    userDto.Role,
                    userType); // 传入userType以设置正确的user_type claim

                // 6. Issue #1864 AUTH-007: 生成新的Refresh Token（Token轮换）
                // 使用MarkAsUsed而非Revoke，以支持重放攻击检测
                var newRefreshToken = GenerateRefreshToken();
                var refreshTokenExpireDays = _configuration.GetValue<int?>("Lybt:Jwt:RefreshTokenExpirationDays") ?? 7;

                // 标记旧Token为已使用（而不是撤销）
                // 这样当攻击者尝试重用时，IsUsed=true会触发重放攻击检测
                tokenRecord.MarkAsUsed(newRefreshToken);

                // 创建新Token记录，继承FamilyId
                var newTokenRecord = new LYBT.Entities.Auth.RefreshToken
                {
                    Token = newRefreshToken,
                    UserId = userDto.Id,
                    UserType = userType, // Issue #1861: 继承用户类型
                    Jti = Guid.NewGuid().ToString(),
                    ExpiresAt = DateTime.UtcNow.AddDays(refreshTokenExpireDays),
                    FamilyId = tokenRecord.FamilyId ?? Guid.NewGuid().ToString() // 继承或创建家族ID
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

                return Result<LoginResponse>.Success(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "刷新Token时发生错误");
                return Result<LoginResponse>.Failure(AuthErrorCode.InternalError, "刷新Token失败");
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
        /// Issue #1864: 返回结构化错误码
        /// </summary>
        public async Task<Result<bool>> ValidateTokenAsync(string token)
        {
            await Task.CompletedTask;

            try
            {
                var principal = _jwtService.ValidateToken(token);
                if (principal != null)
                    return Result<bool>.Success(true);

                return Result<bool>.Failure(AuthErrorCode.TokenInvalid);
            }
            catch
            {
                return Result<bool>.Failure(AuthErrorCode.TokenInvalid);
            }
        }

        /// <summary>
        /// 获取会话信息
        /// Issue #1864: 返回结构化错误码
        /// </summary>
        public async Task<Result<object>> GetSessionInfoAsync(string token)
        {
            await Task.CompletedTask;

            try
            {
                var principal = _jwtService.ValidateToken(token);
                if (principal == null)
                    return Result<object>.Failure(AuthErrorCode.TokenInvalid);

                var sessionInfo = new
                {
                    UserId = principal.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value,
                    UserName = principal.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value,
                    Role = principal.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value
                };

                return Result<object>.Success(sessionInfo);
            }
            catch
            {
                return Result<object>.Failure(AuthErrorCode.InternalError, "获取会话信息失败");
            }
        }

        /// <summary>
        /// 撤销RefreshToken（简化版本不支持）
        /// </summary>
        public async Task<Result<bool>> RevokeTokenAsync(RevokeTokenRequest request)
        {
            await Task.CompletedTask;
            return Result<bool>.Success(true);
        }

        /// <summary>
        /// 撤销整个Token Family（用于重放攻击检测）
        /// Issue #1864 AUTH-007: 当检测到重放攻击时，使整个Token Family失效
        /// </summary>
        /// <param name="familyId">Token Family ID</param>
        /// <param name="reason">撤销原因</param>
        private async Task RevokeTokenFamilyAsync(string familyId, string reason)
        {
            try
            {
                var familyTokens = await _dbContext.RefreshTokens
                    .Where(t => t.FamilyId == familyId && !t.IsRevoked)
                    .ToListAsync();

                foreach (var token in familyTokens)
                {
                    token.Revoke(reason, "System:ReplayAttackDetection");
                }

                await _dbContext.SaveChangesAsync();

                _logger.LogWarning("Token Family已撤销 [FamilyId: {FamilyId}] [撤销Token数量: {Count}] [原因: {Reason}]",
                    familyId, familyTokens.Count, reason);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "撤销Token Family失败 [FamilyId: {FamilyId}]", familyId);
            }
        }

        #endregion 认证流程操作

        // Issue #1008: 移除SaveAuthenticationAsync（Desktop特定方法，已迁移到ILocalAuthService）
        // 移除私有密码验证方法，改为委托给用户服务进行验证
        // 这样符合单一职责原则，认证服务专注于认证流程，密码验证交给用户服务
    }
}
