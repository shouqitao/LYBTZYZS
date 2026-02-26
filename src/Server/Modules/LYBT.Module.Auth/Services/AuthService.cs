using LYBT.Infrastructure.Data;
using LYBT.Infrastructure.Services.CrossModule;
using LYBT.Module.Auth.Interfaces;
using LYBT.Module.Auth.Models;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Auth;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.DTOs.Users;
using LYBT.Shared.Models.Enums;
using LYBT.Shared.Utilities.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using GenericErrorCode = LYBT.Shared.Primitives.ErrorCodes.ErrorCode;

namespace LYBT.Module.Auth.Services
{
    /// <summary>
    /// 认证服务 - 通过 ICrossModuleService 解耦 Users 模块依赖
    /// 密码验证使用 PasswordHelper (LYBT.Shared.Utilities)，用户查询通过 CMQS 间接访问
    /// 仅提供小型中医诊所系统所需的基础认证功能
    /// </summary>
    public class AuthService : IAuthService
    {
        private readonly IJwtService _jwtService;
        private readonly IUserCrossModuleService _crossModuleQuery;
        private readonly ILogger<AuthService> _logger;
        private readonly AppDbContext _dbContext;
        private readonly IConfiguration _configuration;
        private readonly ITokenRevocationService _revocationService;
        private readonly ISecurityAuditService _auditService;

        public AuthService(
            IJwtService jwtService,
            IUserCrossModuleService crossModuleQuery,
            ILogger<AuthService> logger,
            AppDbContext dbContext,
            IConfiguration configuration,
            ITokenRevocationService revocationService,
            ISecurityAuditService auditService)
        {
            _jwtService = jwtService;
            _crossModuleQuery = crossModuleQuery;
            _logger = logger;
            _dbContext = dbContext;
            _configuration = configuration;
            _revocationService = revocationService;
            _auditService = auditService;
        }

        #region 核心认证操作

        /// <summary>
        /// 验证用户凭据（统一认证）
        /// 通过 ICrossModuleService + PasswordHelper 实现密码验证，解耦 Users 模块
        /// </summary>
        public async Task<Result<string>> VerifyCredentialsAsync(LoginRequest request, CancellationToken cancellationToken = default)
        {
            var result = await VerifyCredentialsInternalAsync(request);
            if (!result.IsSuccess)
                return Result<string>.Failure(result.ModuleErrorCode ?? GenericErrorCode.AuthInvalidCredentials, result.ErrorMessage);

            return Result<string>.Success(result.Data!.Id.ToString());
        }

        /// <summary>
        /// 内部凭据验证 - 返回 UserCredentialDto，供 LoginAsync 复用避免双重查询
        /// </summary>
        private async Task<Result<UserCredentialDto>> VerifyCredentialsInternalAsync(LoginRequest request)
        {
            if (string.IsNullOrEmpty(request.UserName) || string.IsNullOrEmpty(request.Password))
                return Result<UserCredentialDto>.Failure(GenericErrorCode.AuthInvalidCredentials, "用户名和密码不能为空");

            var user = await _crossModuleQuery.GetUserByUsernameAsync(request.UserName);
            if (user == null)
            {
                _logger.LogWarning("[SVC] Auth.VerifyCredentials -> Failed - UserName={UserName} Reason=用户不存在",
                    request.UserName);
                return Result<UserCredentialDto>.Failure(GenericErrorCode.AuthInvalidCredentials, "用户名或密码错误");
            }

            if (user.Status == CommonStatus.Disabled)
            {
                _logger.LogWarning("[SVC] Auth.VerifyCredentials -> Failed - UserName={UserName} Reason=用户已被禁用",
                    request.UserName);
                return Result<UserCredentialDto>.Failure(GenericErrorCode.UserDisabled, "用户已被禁用");
            }

            var verificationResult = PasswordHelper.VerifyPassword(
                request.Password, user.PasswordHash,
                user.Role, _logger);

            if (!verificationResult.IsSuccess)
            {
                _logger.LogWarning("[SVC] Auth.VerifyCredentials -> Failed - UserName={UserName} Reason=密码错误",
                    request.UserName);
                return Result<UserCredentialDto>.Failure(GenericErrorCode.AuthInvalidCredentials, "用户名或密码错误");
            }

            // BCrypt hash 升级
            if (verificationResult.NewHashedPassword != null)
            {
                await _crossModuleQuery.UpdateUserPasswordHashAsync(user.Id, verificationResult.NewHashedPassword);
            }

            _logger.LogInformation("[SVC] Auth.VerifyCredentials completed - UserName={UserName} Role={Role}",
                request.UserName, user.Role);
            return Result<UserCredentialDto>.Success(user);
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
            // eliminate-service-catch-return: 移除冗余try-catch，异常由IExceptionHandler统一处理
            // 验证凭据 (使用内部方法直接获取 UserBasicDto，避免双重数据库查询)
            var credentialsResult = await VerifyCredentialsInternalAsync(request);
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
                // T3-X1-01: 传递统一错误码
                return Result<LoginResponse>.Failure(
                    credentialsResult.ModuleErrorCode ?? GenericErrorCode.AuthInvalidCredentials,
                    credentialsResult.ErrorMessage);
            }

            // 复用 VerifyCredentialsInternalAsync 返回的用户信息，无需二次查询
            var userBasic = credentialsResult.Data!;
            var userDto = MapToUserDetailDto(userBasic);

            // 确定用户类型（SuperAdmin特殊处理UserType）
            string userType = userDto.Role == UserRole.SuperAdmin ? "superadmin" : "user";

            // 生成JWT令牌
            var token = _jwtService.GenerateToken(
                userDto.Id.ToString(),
                userDto.UserName,
                userDto.Role,
                userType); // Issue #1909: 根据角色设置正确的user_type claim

            // X3-01: 登录时撤销旧会话的所有 Token
            try
            {
                var oldTokens = await _dbContext.RefreshTokens
                    .Where(t => t.UserId == userDto.Id && !t.IsRevoked)
                    .ToListAsync(cancellationToken);

                foreach (var oldToken in oldTokens)
                {
                    oldToken.Revoke("新登录会话，撤销旧 Token", "System:NewLoginSession");
                }

                if (oldTokens.Count > 0)
                {
                    await _dbContext.SaveChangesAsync(cancellationToken);
                    _logger.LogInformation("[SVC] Auth.Login → RevokedOldTokens - UserId={UserId} Count={Count}",
                        userDto.Id, oldTokens.Count);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[SVC] Auth.Login → RevokeOldTokensFailed - UserId={UserId}", userDto.Id);
            }

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

            // OpenSpec: refactor-login-authentication (CVT-001)
            // 当RememberMe=true时，生成AutoLoginToken供下次自动登录使用
            if (request.RememberMe)
            {
                var autoLoginToken = GenerateAutoLoginToken(
                    userDto.Id,
                    userDto.UserName,
                    request.DeviceId,
                    request.DeviceName,
                    request.ClientIp,
                    request.UserAgent);
                response.AutoLoginToken = autoLoginToken;
            }

            // Issue #1872: 记录登录成功审计日志
            await _auditService.LogAsync(new SecurityAuditEvent
            {
                EventType = "Login",
                UserId = userDto.Id,
                UserType = userType,
                UserName = userDto.UserName,
                Success = true
            });

            _logger.LogInformation("[SVC] Auth.Login completed - UserName={UserName} Role={Role}",
                request.UserName, userDto.Role);

            return Result<LoginResponse>.Success(response);
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
                        var user = await _crossModuleQuery.GetUserBasicInfoAsync(tokenRecord.UserId);
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

                _logger.LogInformation("[SVC] Auth.Logout completed - UserName={UserName}",
                    userName ?? "(unknown)");

                return Result<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[SVC] Auth.Logout failed");
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
            // eliminate-service-catch-return: 移除冗余try-catch，异常由IExceptionHandler统一处理
            if (string.IsNullOrWhiteSpace(refreshToken))
                return Result<LoginResponse>.Failure(GenericErrorCode.AuthRefreshTokenInvalid, "RefreshToken不能为空");

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
                return Result<LoginResponse>.Failure(GenericErrorCode.AuthRefreshTokenInvalid, "RefreshToken不存在");
            }

            // 2. Issue #1864 AUTH-007: 检测重放攻击
            // 如果Token已经被使用过，说明检测到重放攻击
            if (tokenRecord.IsUsed)
            {
                _logger.LogWarning("[SVC] Auth.RefreshToken → ReplayAttack - TokenId={TokenId} FamilyId={FamilyId} UserId={UserId}",
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

                return Result<LoginResponse>.Failure(GenericErrorCode.AuthTokenRevoked, "检测到安全威胁，请重新登录");
            }

            // 3. 验证Token是否有效
            if (!tokenRecord.IsValid())
            {
                GenericErrorCode errorCode;
                string reason;

                if (tokenRecord.IsRevoked)
                {
                    errorCode = GenericErrorCode.AuthTokenRevoked;
                    reason = "已撤销";
                }
                else if (tokenRecord.IsDeleted)
                {
                    errorCode = GenericErrorCode.AuthTokenRevoked;
                    reason = "已删除";
                }
                else
                {
                    errorCode = GenericErrorCode.AuthRefreshTokenExpired;
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

                _logger.LogWarning("[SVC] Auth.RefreshToken → Invalid - Reason={Reason}", reason);
                return Result<LoginResponse>.Failure(errorCode, $"RefreshToken{reason}，请重新登录");
            }

            // 3. 记录使用并检查异常使用
            tokenRecord.RecordUsage();
            if (tokenRecord.UsageCount > 100)
            {
                _logger.LogWarning("[SVC] Auth.RefreshToken → AbnormalUsage - UsageCount={Count}", tokenRecord.UsageCount);
            }

            // 4. 通过 ICrossModuleService 获取用户信息
            var userBasic = await _crossModuleQuery.GetUserBasicInfoAsync(tokenRecord.UserId);
            if (userBasic == null)
                return Result<LoginResponse>.Failure(GenericErrorCode.UserNotFound);

            var userDto = MapToUserDetailDto(userBasic);

            // 确定用户类型（SuperAdmin特殊处理UserType）
            string userType = userDto.Role == UserRole.SuperAdmin ? "superadmin" : "user";

            _logger.LogDebug("[SVC] Auth.RefreshToken → UserLoaded - UserName={UserName} Role={Role}", userDto.UserName, userDto.Role);

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

            _logger.LogInformation("[SVC] Auth.RefreshToken completed - UserName={UserName}",
                userDto.UserName);

            return Result<LoginResponse>.Success(response);
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

                return Result<bool>.Failure(GenericErrorCode.AuthTokenInvalid);
            }
            catch
            {
                return Result<bool>.Failure(GenericErrorCode.AuthTokenInvalid);
            }
        }

        /// <summary>
        /// 获取会话信息
        /// Issue #1864: 返回结构化错误码
        /// </summary>
        public async Task<Result<object>> GetSessionInfoAsync(string token)
        {
            // eliminate-service-catch-return: 移除冗余try-catch，异常由IExceptionHandler统一处理
            await Task.CompletedTask;

            var principal = _jwtService.ValidateToken(token);
            if (principal == null)
                return Result<object>.Failure(GenericErrorCode.AuthTokenInvalid);

            var sessionInfo = new
            {
                UserId = principal.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value,
                UserName = principal.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value,
                Role = principal.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value
            };

            return Result<object>.Success(sessionInfo);
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
        /// 使用AutoLoginToken自动登录
        /// OpenSpec: refactor-login-authentication (CVT-001)
        /// </summary>
        public async Task<Result<LoginResponse>> LoginWithAutoTokenAsync(AutoLoginRequest request, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(request.UserName))
                return Result<LoginResponse>.Failure(GenericErrorCode.AuthInvalidCredentials, "用户名不能为空");

            if (string.IsNullOrWhiteSpace(request.AutoLoginToken))
                return Result<LoginResponse>.Failure(GenericErrorCode.AuthInvalidCredentials, "AutoLoginToken不能为空");

            // 1. 查找AutoLoginToken记录
            var tokenRecord = await _dbContext.Set<LYBT.Entities.Auth.AutoLoginToken>()
                .FirstOrDefaultAsync(t =>
                    t.Token == request.AutoLoginToken &&
                    t.UserName.ToLower() == request.UserName.ToLower(),
                    cancellationToken);

            if (tokenRecord == null)
            {
                await _auditService.LogAsync(new SecurityAuditEvent
                {
                    EventType = "AutoLoginFailed",
                    UserName = request.UserName,
                    Success = false,
                    ErrorMessage = "AutoLoginToken不存在"
                });
                return Result<LoginResponse>.Failure(GenericErrorCode.AuthInvalidCredentials, "AutoLoginToken无效");
            }

            // 2. 检测重放攻击
            if (tokenRecord.IsUsed)
            {
                _logger.LogWarning("[SVC] Auth.AutoLogin → ReplayAttack - TokenId={TokenId} UserId={UserId} UserName={UserName}",
                    tokenRecord.Id, tokenRecord.UserId, tokenRecord.UserName);

                // 撤销同一家族的所有Token
                if (!string.IsNullOrEmpty(tokenRecord.FamilyId))
                {
                    await RevokeAutoLoginTokenFamilyAsync(tokenRecord.FamilyId, "检测到重放攻击");
                }

                await _auditService.LogAsync(new SecurityAuditEvent
                {
                    EventType = "AutoLoginReplayAttack",
                    UserId = tokenRecord.UserId,
                    UserName = request.UserName,
                    Success = false,
                    ErrorMessage = "检测到AutoLoginToken重放攻击"
                });

                return Result<LoginResponse>.Failure(GenericErrorCode.AuthTokenRevoked, "检测到安全威胁，请重新登录");
            }

            // 3. 验证Token是否有效
            if (!tokenRecord.IsValid())
            {
                string reason = tokenRecord.IsRevoked ? "已撤销" :
                               tokenRecord.IsDeleted ? "已删除" : "已过期";

                await _auditService.LogAsync(new SecurityAuditEvent
                {
                    EventType = "AutoLoginFailed",
                    UserId = tokenRecord.UserId,
                    UserName = request.UserName,
                    Success = false,
                    ErrorMessage = $"AutoLoginToken{reason}"
                });

                return Result<LoginResponse>.Failure(GenericErrorCode.AuthRefreshTokenExpired, $"AutoLoginToken{reason}，请重新登录");
            }

            // 4. 通过 ICrossModuleService 获取用户信息
            var userBasic = await _crossModuleQuery.GetUserBasicInfoAsync(tokenRecord.UserId);
            if (userBasic == null)
            {
                return Result<LoginResponse>.Failure(GenericErrorCode.UserNotFound, "用户不存在");
            }

            // 检查用户是否被禁用
            if (userBasic.Status == CommonStatus.Disabled)
            {
                return Result<LoginResponse>.Failure(GenericErrorCode.UserDisabled, "用户已被禁用");
            }

            var userDto = MapToUserDetailDto(userBasic);
            string userType = userDto.Role == UserRole.SuperAdmin ? "superadmin" : "user";

            // 5. 生成新的JWT Token
            var jwtToken = _jwtService.GenerateToken(
                userDto.Id.ToString(),
                userDto.UserName,
                userDto.Role,
                userType);

            // 6. 生成RefreshToken
            var refreshToken = GenerateRefreshToken();
            var refreshTokenExpireDays = _configuration.GetValue<int?>("Lybt:Jwt:RefreshTokenExpirationDays") ?? 7;
            var tokenExpireMinutes = _configuration.GetValue<int?>("Lybt:Jwt:ExpireMinutes") ?? 15;

            var refreshTokenRecord = new LYBT.Entities.Auth.RefreshToken
            {
                Token = refreshToken,
                UserId = userDto.Id,
                UserType = userType,
                Jti = Guid.NewGuid().ToString(),
                ExpiresAt = DateTime.UtcNow.AddDays(refreshTokenExpireDays),
                FamilyId = Guid.NewGuid().ToString()
            };
            _dbContext.RefreshTokens.Add(refreshTokenRecord);

            // 7. Token轮换：标记旧Token为已使用，生成新的AutoLoginToken
            var newAutoLoginToken = GenerateAutoLoginToken(
                userDto.Id,
                userDto.UserName,
                request.DeviceId,
                request.DeviceName,
                request.ClientIp,
                request.UserAgent,
                tokenRecord.FamilyId); // 继承FamilyId

            tokenRecord.MarkAsUsed(newAutoLoginToken);
            await _dbContext.SaveChangesAsync(cancellationToken);

            var response = new LoginResponse
            {
                Token = jwtToken,
                User = userDto,
                RefreshToken = refreshToken,
                ExpiresAt = DateTime.UtcNow.AddMinutes(tokenExpireMinutes),
                AutoLoginToken = newAutoLoginToken
            };

            await _auditService.LogAsync(new SecurityAuditEvent
            {
                EventType = "AutoLogin",
                UserId = userDto.Id,
                UserType = userType,
                UserName = userDto.UserName,
                Success = true
            });

            _logger.LogInformation("[SVC] Auth.AutoLogin completed - UserName={UserName} Role={Role}",
                userDto.UserName, userDto.Role);

            return Result<LoginResponse>.Success(response);
        }

        /// <summary>
        /// 生成AutoLoginToken
        /// OpenSpec: refactor-login-authentication (CVT-001)
        /// </summary>
        private string GenerateAutoLoginToken(
            Guid userId,
            string userName,
            string? deviceId,
            string? deviceName,
            string? clientIp,
            string? userAgent,
            string? familyId = null)
        {
            // 生成安全的随机Token
            var tokenBytes = new byte[64];
            using var rng = System.Security.Cryptography.RandomNumberGenerator.Create();
            rng.GetBytes(tokenBytes);
            var token = Convert.ToBase64String(tokenBytes);

            // AutoLoginToken有效期：默认30天
            var autoLoginTokenExpireDays = _configuration.GetValue<int?>("Lybt:Auth:AutoLoginTokenExpirationDays") ?? 30;

            var tokenRecord = new LYBT.Entities.Auth.AutoLoginToken
            {
                Token = token,
                UserId = userId,
                UserName = userName,
                ExpiresAt = DateTime.UtcNow.AddDays(autoLoginTokenExpireDays),
                DeviceId = deviceId,
                DeviceName = deviceName,
                ClientIp = clientIp,
                UserAgent = userAgent,
                FamilyId = familyId ?? Guid.NewGuid().ToString()
            };

            _dbContext.Set<LYBT.Entities.Auth.AutoLoginToken>().Add(tokenRecord);
            // 注意：调用者负责SaveChanges

            return token;
        }

        /// <summary>
        /// 撤销AutoLoginToken Family
        /// OpenSpec: refactor-login-authentication (CVT-001)
        /// </summary>
        private async Task RevokeAutoLoginTokenFamilyAsync(string familyId, string reason)
        {
            try
            {
                var familyTokens = await _dbContext.Set<LYBT.Entities.Auth.AutoLoginToken>()
                    .Where(t => t.FamilyId == familyId && !t.IsRevoked)
                    .ToListAsync();

                foreach (var token in familyTokens)
                {
                    token.Revoke(reason, "System:ReplayAttackDetection");
                }

                await _dbContext.SaveChangesAsync();

                _logger.LogWarning("[SVC] Auth.RevokeAutoLoginTokenFamily completed - FamilyId={FamilyId} RevokedCount={Count} Reason={Reason}",
                    familyId, familyTokens.Count, reason);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[SVC] Auth.RevokeAutoLoginTokenFamily failed - FamilyId={FamilyId}", familyId);
            }
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

                _logger.LogWarning("[SVC] Auth.RevokeTokenFamily completed - FamilyId={FamilyId} RevokedCount={Count} Reason={Reason}",
                    familyId, familyTokens.Count, reason);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[SVC] Auth.RevokeTokenFamily failed - FamilyId={FamilyId}", familyId);
            }
        }

        #endregion 认证流程操作

        /// <summary>
        /// 将 UserBasicDto 映射为 UserDetailDto (LoginResponse 需要)
        /// 映射全部 UserDetailDto 字段，避免客户端功能退化
        /// </summary>
        private static UserDetailDto MapToUserDetailDto(UserBasicDto basic)
        {
            return new UserDetailDto
            {
                Id = basic.Id,
                UserName = basic.UserName,
                RealName = basic.RealName,
                Role = basic.Role,
                Status = basic.Status,
                PhoneNumber = basic.PhoneNumber,
                Email = basic.Email,
                PinYinCode = basic.PinYinCode,
                LastLoginTime = basic.LastLoginTime,
                FailedLoginCount = basic.FailedLoginCount,
                CreatedAt = basic.CreatedAt,
                UpdatedAt = basic.UpdatedAt,
                Remark = basic.Remark
            };
        }
    }
}
