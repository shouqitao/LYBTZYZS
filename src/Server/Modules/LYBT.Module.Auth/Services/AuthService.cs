using LYBT.Infrastructure.Data;
using LYBT.Infrastructure.Services.CrossModule;
using LYBT.Module.Auth.Interfaces;
using LYBT.Module.Auth.Models;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Auth;
using LYBT.Shared.Models.Enums;
using LYBT.Shared.Utilities.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using GenericErrorCode = LYBT.Shared.Primitives.ErrorCodes.ErrorCode;

namespace LYBT.Module.Auth.Services;

/// <summary>
/// 认证服务 - 负责登录、登出和凭据验证
/// Token 管理职责已委托给 ITokenManagementService
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
    private readonly ITokenManagementService _tokenManagement;

    public AuthService(
        IJwtService jwtService,
        IUserCrossModuleService crossModuleQuery,
        ILogger<AuthService> logger,
        AppDbContext dbContext,
        IConfiguration configuration,
        ITokenRevocationService revocationService,
        ISecurityAuditService auditService,
        ITokenManagementService tokenManagement)
    {
        _jwtService = jwtService;
        _crossModuleQuery = crossModuleQuery;
        _logger = logger;
        _dbContext = dbContext;
        _configuration = configuration;
        _revocationService = revocationService;
        _auditService = auditService;
        _tokenManagement = tokenManagement;
    }

    #region 核心认证操作

    /// <summary>
    /// 验证用户凭据（统一认证）
    /// </summary>
    public async Task<Result<string>> VerifyCredentialsAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var result = await VerifyCredentialsInternalAsync(request);
        if (!result.IsSuccess)
            return Result<string>.Failure(result.ModuleErrorCode ?? GenericErrorCode.AuthInvalidCredentials, result.ErrorMessage);

        return Result<string>.Success(result.Data!.Id.ToString());
    }

    /// <summary>
    /// T5-P2-01: 最大失败登录次数
    /// </summary>
    private const int MaxFailedLoginCount = 5;

    /// <summary>
    /// T5-P2-01: 锁定时间（分钟）
    /// </summary>
    private const int LockoutMinutes = 15;

    private async Task<Result<Shared.Models.DTOs.Users.UserCredentialDto>> VerifyCredentialsInternalAsync(LoginRequest request)
    {
        if (string.IsNullOrEmpty(request.UserName) || string.IsNullOrEmpty(request.Password))
            return Result<Shared.Models.DTOs.Users.UserCredentialDto>.Failure(GenericErrorCode.AuthInvalidCredentials, "用户名和密码不能为空");

        var user = await _crossModuleQuery.GetUserByUsernameAsync(request.UserName);
        if (user == null)
        {
            _logger.LogWarning("[SVC] Auth.VerifyCredentials -> Failed - UserName={UserName} Reason=用户不存在",
                request.UserName);
            return Result<Shared.Models.DTOs.Users.UserCredentialDto>.Failure(GenericErrorCode.AuthInvalidCredentials, "用户名或密码错误");
        }

        // T5-P2-02: UserDisabled 返回 403
        if (user.Status == CommonStatus.Disabled)
        {
            _logger.LogWarning("[SVC] Auth.VerifyCredentials -> Failed - UserName={UserName} Reason=用户已被禁用",
                request.UserName);
            return Result<Shared.Models.DTOs.Users.UserCredentialDto>.Failure(GenericErrorCode.UserDisabled, "用户已被禁用");
        }

        // T5-P2-01: 检查账户锁定状态
        if (user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTime.UtcNow)
        {
            _logger.LogWarning("[SVC] Auth.VerifyCredentials -> Failed - UserName={UserName} Reason=账户已锁定至 {LockoutEnd}",
                request.UserName, user.LockoutEnd.Value);
            return Result<Shared.Models.DTOs.Users.UserCredentialDto>.Failure(GenericErrorCode.UserLocked, "账号已被锁定，请稍后重试");
        }

        var verificationResult = PasswordHelper.VerifyPassword(
            request.Password, user.PasswordHash,
            user.Role, _logger);

        if (!verificationResult.IsSuccess)
        {
            // T5-P2-01: 增加失败次数，达到阈值时锁定账户
            var newFailedCount = user.FailedLoginCount + 1;
            DateTime? lockoutEnd = null;

            if (newFailedCount >= MaxFailedLoginCount)
            {
                lockoutEnd = DateTime.UtcNow.AddMinutes(LockoutMinutes);
                _logger.LogWarning("[SVC] Auth.VerifyCredentials -> AccountLocked - UserName={UserName} FailedCount={Count} LockoutMinutes={Minutes}",
                    request.UserName, newFailedCount, LockoutMinutes);
            }
            else
            {
                _logger.LogWarning("[SVC] Auth.VerifyCredentials -> Failed - UserName={UserName} Reason=密码错误 FailedCount={Count}",
                    request.UserName, newFailedCount);
            }

            await _crossModuleQuery.UpdateLoginFailureAsync(user.Id, newFailedCount, lockoutEnd);
            return Result<Shared.Models.DTOs.Users.UserCredentialDto>.Failure(GenericErrorCode.AuthInvalidCredentials, "用户名或密码错误");
        }

        // BCrypt hash 升级
        if (verificationResult.NewHashedPassword != null)
        {
            await _crossModuleQuery.UpdateUserPasswordHashAsync(user.Id, verificationResult.NewHashedPassword);
        }

        // T5-P2-01: 登录成功，重置失败计数和锁定状态
        await _crossModuleQuery.ResetLoginStateAsync(user.Id);

        _logger.LogInformation("[SVC] Auth.VerifyCredentials completed - UserName={UserName} Role={Role}",
            request.UserName, user.Role);
        return Result<Shared.Models.DTOs.Users.UserCredentialDto>.Success(user);
    }

    #endregion 核心认证操作

    #region 认证流程操作

    /// <summary>
    /// 用户登录（统一流程）
    /// </summary>
    public async Task<Result<LoginResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var credentialsResult = await VerifyCredentialsInternalAsync(request);
        if (!credentialsResult.IsSuccess)
        {
            await _auditService.LogAsync(new SecurityAuditEvent
            {
                EventType = "LoginFailed",
                UserName = request.UserName,
                Success = false,
                ErrorMessage = credentialsResult.ErrorMessage ?? "凭据验证失败"
            });
            return Result<LoginResponse>.Failure(
                credentialsResult.ModuleErrorCode ?? GenericErrorCode.AuthInvalidCredentials,
                credentialsResult.ErrorMessage);
        }

        var userBasic = credentialsResult.Data!;
        var userDto = TokenManagementService.MapToUserDetailDto(userBasic);
        string userType = userDto.Role == UserRole.SuperAdmin ? "superadmin" : "user";

        // 生成 JWT 令牌
        var token = _jwtService.GenerateToken(
            userDto.Id.ToString(),
            userDto.UserName,
            userDto.Role,
            userType);

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
                _logger.LogInformation("[SVC] Auth.Login -> RevokedOldTokens - UserId={UserId} Count={Count}",
                    userDto.Id, oldTokens.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[SVC] Auth.Login -> RevokeOldTokensFailed - UserId={UserId}", userDto.Id);
        }

        // CODE-03: 撤销旧 AutoLoginToken Family
        try
        {
            var oldAutoTokens = await _dbContext.Set<LYBT.Entities.Auth.AutoLoginToken>()
                .Where(t => t.UserId == userDto.Id && !t.IsRevoked)
                .ToListAsync(cancellationToken);
            foreach (var oldAutoToken in oldAutoTokens)
            {
                oldAutoToken.Revoke("新登录会话，撤销旧 AutoLoginToken", "System:NewLoginSession");
            }
            if (oldAutoTokens.Count > 0)
            {
                await _dbContext.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("[SVC] Auth.Login -> RevokedOldAutoTokens - UserId={UserId} Count={Count}",
                    userDto.Id, oldAutoTokens.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[SVC] Auth.Login -> RevokeOldAutoTokensFailed - UserId={UserId}", userDto.Id);
        }

        // 生成并存储 RefreshToken
        var refreshToken = TokenManagementService.GenerateRefreshToken();
        var refreshTokenExpireDays = _configuration.GetValue<int?>("Lybt:Jwt:RefreshTokenExpirationDays") ?? 7;
        var tokenExpireMinutes = _configuration.GetValue<int?>("Lybt:Jwt:ExpireMinutes") ?? 15;
        var absoluteExpireDays = _configuration.GetValue<int?>("Lybt:Jwt:RefreshTokenAbsoluteExpirationDays") ?? 30;

        var refreshTokenRecord = new LYBT.Entities.Auth.RefreshToken
        {
            Token = refreshToken,
            UserId = userDto.Id,
            UserType = userType,
            Jti = Guid.NewGuid().ToString(),
            ExpiresAt = DateTime.UtcNow.AddDays(refreshTokenExpireDays),
            AbsoluteExpiresAt = DateTime.UtcNow.AddDays(absoluteExpireDays),
            FamilyId = Guid.NewGuid().ToString()
        };
        _dbContext.RefreshTokens.Add(refreshTokenRecord);
        await _dbContext.SaveChangesAsync();

        var response = new LoginResponse
        {
            Token = token,
            User = userDto,
            RefreshToken = refreshToken,
            ExpiresAt = DateTime.UtcNow.AddMinutes(tokenExpireMinutes),
            MustChangePassword = userBasic.MustChangeOnNextLogin
        };

        // RememberMe: 生成 AutoLoginToken
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
    /// </summary>
    public async Task<Result<bool>> LogoutAsync(LogoutRequest request)
    {
        try
        {
            LYBT.Entities.Auth.RefreshToken? tokenRecord = null;
            string? userName = request.UserName;

            if (!string.IsNullOrEmpty(request.RefreshToken))
            {
                tokenRecord = await _dbContext.RefreshTokens
                    .FirstOrDefaultAsync(t => t.Token == request.RefreshToken);

                if (tokenRecord != null && string.IsNullOrEmpty(userName))
                {
                    var user = await _crossModuleQuery.GetUserBasicInfoAsync(tokenRecord.UserId);
                    userName = user?.UserName;
                }

                if (tokenRecord != null && !tokenRecord.IsRevoked)
                {
                    await _revocationService.RevokeTokenAsync(request.RefreshToken, "用户主动登出");

                    // 委托 TokenManagementService 撤销整个 Family
                    if (!string.IsNullOrEmpty(tokenRecord.FamilyId))
                    {
                        await _tokenManagement.RevokeTokenFamilyAsync(tokenRecord.FamilyId, "用户主动登出，撤销整个Token Family");
                    }
                }
            }

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
            return Result<bool>.Success(true);
        }
    }

    /// <summary>
    /// 刷新令牌 - 委托给 ITokenManagementService
    /// </summary>
    public Task<Result<LoginResponse>> RefreshTokenAsync(string refreshToken)
        => _tokenManagement.RefreshTokenAsync(refreshToken);

    /// <summary>
    /// 验证令牌 - 委托给 ITokenManagementService
    /// </summary>
    public Task<Result<bool>> ValidateTokenAsync(string token)
        => _tokenManagement.ValidateTokenAsync(token);

    /// <summary>
    /// 获取会话信息 - 委托给 ITokenManagementService
    /// </summary>
    public Task<Result<object>> GetSessionInfoAsync(string token)
        => _tokenManagement.GetSessionInfoAsync(token);

    /// <summary>
    /// 使用 AutoLoginToken 自动登录
    /// </summary>
    public async Task<Result<LoginResponse>> LoginWithAutoTokenAsync(AutoLoginRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.UserName))
            return Result<LoginResponse>.Failure(GenericErrorCode.AuthInvalidCredentials, "用户名不能为空");

        if (string.IsNullOrWhiteSpace(request.AutoLoginToken))
            return Result<LoginResponse>.Failure(GenericErrorCode.AuthInvalidCredentials, "AutoLoginToken不能为空");

        // 1. 查找 AutoLoginToken 记录
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
            _logger.LogWarning("[SVC] Auth.AutoLogin -> ReplayAttack - TokenId={TokenId} UserId={UserId} UserName={UserName}",
                tokenRecord.Id, tokenRecord.UserId, tokenRecord.UserName);

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

        // 3. 验证 Token 是否有效
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

        // 4. 获取用户信息
        var userBasic = await _crossModuleQuery.GetUserBasicInfoAsync(tokenRecord.UserId);
        if (userBasic == null)
        {
            return Result<LoginResponse>.Failure(GenericErrorCode.UserNotFound, "用户不存在");
        }

        if (userBasic.Status == CommonStatus.Disabled)
        {
            return Result<LoginResponse>.Failure(GenericErrorCode.UserDisabled, "用户已被禁用");
        }

        var userDto = TokenManagementService.MapToUserDetailDto(userBasic);
        string userType = userDto.Role == UserRole.SuperAdmin ? "superadmin" : "user";

        // 5. 生成新的 JWT Token
        var jwtToken = _jwtService.GenerateToken(
            userDto.Id.ToString(),
            userDto.UserName,
            userDto.Role,
            userType);

        // 6. 生成 RefreshToken
        var refreshToken = TokenManagementService.GenerateRefreshToken();
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

        // 7. Token 轮换
        var newAutoLoginToken = GenerateAutoLoginToken(
            userDto.Id,
            userDto.UserName,
            request.DeviceId,
            request.DeviceName,
            request.ClientIp,
            request.UserAgent,
            tokenRecord.FamilyId);

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

    #endregion 认证流程操作

    #region 私有辅助方法

    /// <summary>
    /// 生成 AutoLoginToken
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
        var tokenBytes = new byte[64];
        using var rng = System.Security.Cryptography.RandomNumberGenerator.Create();
        rng.GetBytes(tokenBytes);
        var token = Convert.ToBase64String(tokenBytes);

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
        return token;
    }

    /// <summary>
    /// 撤销 AutoLoginToken Family
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

    #endregion 私有辅助方法
}
