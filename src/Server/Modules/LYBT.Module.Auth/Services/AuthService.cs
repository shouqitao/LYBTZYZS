using LYBT.Infrastructure.Services.CrossModule;
using LYBT.Module.Auth.Interfaces;
using LYBT.Module.Auth.Models;
using LYBT.Shared.Configuration.Options.Server;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Auth;
using LYBT.Shared.Models.Enums;
using LYBT.Shared.Utilities.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
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
    private readonly IConfiguration _configuration;
    private readonly ITokenRevocationService _revocationService;
    private readonly ISecurityAuditService _auditService;
    private readonly ITokenManagementService _tokenManagement;
    private readonly IAutoLoginService _autoLoginService;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IAutoLoginTokenRepository _autoLoginTokenRepository;
    private readonly SecurityOptions _securityOptions;

    public AuthService(
        IJwtService jwtService,
        IUserCrossModuleService crossModuleQuery,
        ILogger<AuthService> logger,
        IConfiguration configuration,
        ITokenRevocationService revocationService,
        ISecurityAuditService auditService,
        ITokenManagementService tokenManagement,
        IAutoLoginService autoLoginService,
        IRefreshTokenRepository refreshTokenRepository,
        IAutoLoginTokenRepository autoLoginTokenRepository,
        IOptions<SecurityOptions> securityOptions)
    {
        _jwtService = jwtService;
        _crossModuleQuery = crossModuleQuery;
        _logger = logger;
        _configuration = configuration;
        _revocationService = revocationService;
        _auditService = auditService;
        _tokenManagement = tokenManagement;
        _autoLoginService = autoLoginService;
        _refreshTokenRepository = refreshTokenRepository;
        _autoLoginTokenRepository = autoLoginTokenRepository;
        _securityOptions = securityOptions?.Value ?? throw new ArgumentNullException(nameof(securityOptions));
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

            if (_securityOptions.AccountLockout.Enabled && newFailedCount >= _securityOptions.AccountLockout.MaxFailedCount)
            {
                lockoutEnd = DateTime.UtcNow.AddMinutes(_securityOptions.AccountLockout.LockoutMinutes);
                _logger.LogWarning("[SVC] Auth.VerifyCredentials -> AccountLocked - UserName={UserName} FailedCount={Count} LockoutMinutes={Minutes}",
                    request.UserName, newFailedCount, _securityOptions.AccountLockout.LockoutMinutes);
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
            var oldTokens = await _refreshTokenRepository.GetActiveTokensByUserIdAsync(userDto.Id, cancellationToken);

            foreach (var oldToken in oldTokens)
            {
                oldToken.Revoke("新登录会话，撤销旧 Token", "System:NewLoginSession");
            }

            if (oldTokens.Count > 0)
            {
                await _refreshTokenRepository.UpdateRangeAsync(oldTokens, cancellationToken);
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
            var oldAutoTokens = await _autoLoginTokenRepository.GetActiveTokensByUserIdAsync(userDto.Id, cancellationToken);
            foreach (var oldAutoToken in oldAutoTokens)
            {
                oldAutoToken.Revoke("新登录会话，撤销旧 AutoLoginToken", "System:NewLoginSession");
            }
            if (oldAutoTokens.Count > 0)
            {
                await _autoLoginTokenRepository.UpdateRangeAsync(oldAutoTokens, cancellationToken);
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
        await _refreshTokenRepository.AddAsync(refreshTokenRecord, cancellationToken);
        await _refreshTokenRepository.SaveChangesAsync(cancellationToken);

        var response = new LoginResponse
        {
            Token = token,
            User = userDto,
            RefreshToken = refreshToken,
            ExpiresAt = DateTime.UtcNow.AddMinutes(tokenExpireMinutes),
            MustChangePassword = userBasic.MustChangeOnNextLogin
        };

        // RememberMe: 生成 AutoLoginToken (委托给 IAutoLoginService)
        if (request.RememberMe)
        {
            var autoLoginToken = _autoLoginService.GenerateAutoLoginToken(
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
                tokenRecord = await _refreshTokenRepository.GetByTokenAsync(request.RefreshToken);

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
    /// 使用 AutoLoginToken 自动登录 - 委托给 IAutoLoginService
    /// </summary>
    public Task<Result<LoginResponse>> LoginWithAutoTokenAsync(AutoLoginRequest request, CancellationToken cancellationToken = default)
        => _autoLoginService.LoginWithAutoTokenAsync(request, cancellationToken);

    #endregion 认证流程操作

}
