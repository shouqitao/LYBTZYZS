using LYBT.Infrastructure.Services.CrossModule;
using LYBT.Module.Auth.Interfaces;
using LYBT.Shared.Configuration.Options.Server;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Auth;
using LYBT.Shared.Models.Enums;
using LYBT.Shared.Utilities.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using GenericErrorCode = LYBT.Shared.Primitives.ErrorCodes.ErrorCode;

namespace LYBT.Module.Auth.Services;

/// <summary>
/// 认证服务 - 负责登录、登出和凭据验证
/// 简化版：仅提供基础认证功能，无RefreshToken/AutoLogin/SecurityAudit
/// </summary>
public class AuthService : IAuthService
{
    private readonly IJwtService _jwtService;
    private readonly IUserCrossModuleService _crossModuleQuery;
    private readonly ILogger<AuthService> _logger;
    private readonly SecurityOptions _securityOptions;

    public AuthService(
        IJwtService jwtService,
        IUserCrossModuleService crossModuleQuery,
        ILogger<AuthService> logger,
        IOptions<SecurityOptions> securityOptions)
    {
        _jwtService = jwtService;
        _crossModuleQuery = crossModuleQuery;
        _logger = logger;
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

        if (user.Status == CommonStatus.Disabled)
        {
            _logger.LogWarning("[SVC] Auth.VerifyCredentials -> Failed - UserName={UserName} Reason=用户已被禁用",
                request.UserName);
            return Result<Shared.Models.DTOs.Users.UserCredentialDto>.Failure(GenericErrorCode.UserDisabled, "用户已被禁用");
        }

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

        if (verificationResult.NewHashedPassword != null)
        {
            await _crossModuleQuery.UpdateUserPasswordHashAsync(user.Id, verificationResult.NewHashedPassword);
        }

        await _crossModuleQuery.ResetLoginStateAsync(user.Id);

        _logger.LogInformation("[SVC] Auth.VerifyCredentials completed - UserName={UserName} Role={Role}",
            request.UserName, user.Role);
        return Result<Shared.Models.DTOs.Users.UserCredentialDto>.Success(user);
    }

    #endregion 核心认证操作

    #region 认证流程操作

    /// <summary>
    /// 用户登录（简化版：仅生成JWT，无RefreshToken/AutoLogin）
    /// </summary>
    public async Task<Result<LoginResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var credentialsResult = await VerifyCredentialsInternalAsync(request);
        if (!credentialsResult.IsSuccess)
        {
            return Result<LoginResponse>.Failure(
                credentialsResult.ModuleErrorCode ?? GenericErrorCode.AuthInvalidCredentials,
                credentialsResult.ErrorMessage);
        }

        var userBasic = credentialsResult.Data!;
        var userDto = TokenManagementHelper.MapToUserDetailDto(userBasic);
        string userType = userDto.Role == UserRole.SuperAdmin ? "superadmin" : "user";

        var token = _jwtService.GenerateToken(
            userDto.Id.ToString(),
            userDto.UserName,
            userDto.Role,
            userType);

        var tokenExpireMinutes = 60;

        var response = new LoginResponse
        {
            Token = token,
            User = userDto,
            ExpiresAt = DateTime.UtcNow.AddMinutes(tokenExpireMinutes),
            MustChangePassword = userBasic.MustChangeOnNextLogin
        };

        _logger.LogInformation("[SVC] Auth.Login completed - UserName={UserName} Role={Role}",
            request.UserName, userDto.Role);

        return Result<LoginResponse>.Success(response);
    }

    /// <summary>
    /// 用户登出（简化版：仅记录日志，无RefreshToken撤销）
    /// </summary>
    public Task<Result<bool>> LogoutAsync(LogoutRequest request)
    {
        _logger.LogInformation("[SVC] Auth.Logout completed - UserName={UserName}",
            request.UserName ?? "(unknown)");
        return Task.FromResult(Result<bool>.Success(true));
    }

    /// <summary>
    /// 刷新Token（简化版：不再支持）
    /// </summary>
    public Task<Result<LoginResponse>> RefreshTokenAsync(string refreshToken)
        => Task.FromResult(Result<LoginResponse>.Failure(GenericErrorCode.AuthInvalidCredentials, "RefreshToken功能已禁用"));

    /// <summary>
    /// 验证令牌
    /// </summary>
    public Task<Result<bool>> ValidateTokenAsync(string token)
    {
        var result = _jwtService.ValidateToken(token);
        return Task.FromResult(Result<bool>.Success(result != null));
    }

    /// <summary>
    /// 获取会话信息
    /// </summary>
    public Task<Result<object>> GetSessionInfoAsync(string token)
    {
        var principal = _jwtService.ValidateToken(token);
        if (principal == null)
            return Task.FromResult(Result<object>.Failure(GenericErrorCode.AuthInvalidCredentials, "Token无效"));

        var userId = principal.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var userName = principal.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value;
        var role = principal.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;

        return Task.FromResult(Result<object>.Success(new { UserId = userId, UserName = userName, Role = role }));
    }

    /// <summary>
    /// 使用 AutoLoginToken 自动登录（简化版：不再支持）
    /// </summary>
    public Task<Result<LoginResponse>> LoginWithAutoTokenAsync(AutoLoginRequest request, CancellationToken cancellationToken = default)
        => Task.FromResult(Result<LoginResponse>.Failure(GenericErrorCode.AuthInvalidCredentials, "AutoLogin功能已禁用"));

    #endregion 认证流程操作

}

/// <summary>
/// Token管理辅助方法（从TokenManagementService迁移的核心逻辑）
/// </summary>
internal static class TokenManagementHelper
{
    public static Shared.Models.Contracts.Users.UserDetailDto MapToUserDetailDto(Shared.Models.DTOs.Users.UserCredentialDto user)
    {
        return new Shared.Models.Contracts.Users.UserDetailDto
        {
            Id = user.Id,
            UserName = user.UserName,
            RealName = user.RealName,
            Role = user.Role,
            Status = user.Status,
            PhoneNumber = user.PhoneNumber,
            CreatedAt = user.CreatedAt
        };
    }
}
