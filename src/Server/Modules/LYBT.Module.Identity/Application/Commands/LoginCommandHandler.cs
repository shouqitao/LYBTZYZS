using System.Security.Cryptography;
using System.Text;
using MediatR;
using LYBT.Infrastructure.Services.CrossModule;
using LYBT.Entities.Auth;
using LYBT.Module.Identity.Application.Mappers;
using LYBT.Module.Identity.Interfaces;
using LYBT.Shared.Configuration.Options.Common;
using LYBT.Shared.Configuration.Options.Server;
using LYBT.Shared.Models.Contracts.Auth;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Enums;
using LYBT.Shared.Models.Primitives.ErrorCodes;
using LYBT.Shared.Models.Contracts.Common;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LYBT.Module.Identity.Application.Commands;

public class LoginCommandHandler : IRequestHandler<LoginCommand, Result<LoginResponse>>
{
    private readonly IJwtService _jwtService;
    private readonly IUserCrossModuleService _crossModuleService;
    private readonly IAuthSessionRepository _authSessionRepository;
    private readonly ISecurityAuditService _securityAuditService;
    private readonly ISender _sender;
    private readonly ILogger<LoginCommandHandler> _logger;
    private readonly SecurityOptions _securityOptions;
    private readonly JwtOptions _jwtOptions;
    private readonly LoginOptions _loginOptions;

    public LoginCommandHandler(
        IJwtService jwtService,
        IUserCrossModuleService crossModuleService,
        IAuthSessionRepository authSessionRepository,
        ISecurityAuditService securityAuditService,
        ISender sender,
        ILogger<LoginCommandHandler> logger,
        IOptions<SecurityOptions> securityOptions,
        IOptions<JwtOptions> jwtOptions,
        IOptions<LoginOptions> loginOptions)
    {
        _jwtService = jwtService;
        _crossModuleService = crossModuleService;
        _authSessionRepository = authSessionRepository;
        _securityAuditService = securityAuditService;
        _sender = sender;
        _logger = logger;
        _securityOptions = securityOptions?.Value ?? throw new ArgumentNullException(nameof(securityOptions));
        _jwtOptions = jwtOptions?.Value ?? throw new ArgumentNullException(nameof(jwtOptions));
        _loginOptions = loginOptions?.Value ?? throw new ArgumentNullException(nameof(loginOptions));
    }

    public async Task<Result<LoginResponse>> Handle(
        LoginCommand request, CancellationToken cancellationToken)
    {
        var input = request.Input;

        if (string.IsNullOrEmpty(input.UserName) || string.IsNullOrEmpty(input.Password))
            return Result<LoginResponse>.Failure(ErrorCode.AuthInvalidCredentials, "用户名和密码不能为空");

        var user = await _crossModuleService.GetUserByUsernameAsync(input.UserName, cancellationToken);
        if (user == null)
        {
            _logger.LogWarning("[Handler] Login failed - UserName={UserName} Reason=用户不存在", input.UserName);
            await RecordAuditAsync(new SecurityAuditEvent
            {
                UserName = input.UserName,
                EventType = "LoginFailed",
                IpAddress = input.ClientIp,
                UserAgent = input.UserAgent,
                IsSuccess = false,
                FailureReason = "用户不存在"
            }, cancellationToken);
            return Result<LoginResponse>.Failure(ErrorCode.AuthInvalidCredentials, ErrorMessages.Get(ErrorCode.AuthInvalidCredentials));
        }

        if (user.Status == CommonStatus.Disabled)
        {
            // 生命周期回归（2026-08-13）: sysadmin 禁用 → 明确运维提示（区别于普通用户通用错误；
            // 软删用户查询已滤（GetUserByUsernameAsync !IsDeleted）——走通用「用户不存在」路径）
            if (user.IsSysAdmin)
            {
                _logger.LogWarning(
                    "[Handler] sysadmin 登录被拒——账号禁用（Status={Status}）——需密码初始化工具恢复",
                    user.Status);
                return Result<LoginResponse>.Failure(
                    ErrorCode.UserDisabled,
                    "系统管理员账号异常，请联系运维使用密码初始化工具恢复");
            }

            _logger.LogWarning("[Handler] Login failed - UserName={UserName} Reason=用户已被禁用", input.UserName);
            await RecordAuditAsync(new SecurityAuditEvent
            {
                UserId = user.Id,
                UserName = input.UserName,
                EventType = "LoginFailed",
                IpAddress = input.ClientIp,
                UserAgent = input.UserAgent,
                IsSuccess = false,
                FailureReason = "用户已被禁用"
            }, cancellationToken);
            return Result<LoginResponse>.Failure(ErrorCode.UserDisabled, ErrorMessages.Get(ErrorCode.UserDisabled));
        }

        if (_loginOptions.LockoutEnabled
            && user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTime.UtcNow)
        {
            _logger.LogWarning("[Handler] Login failed - UserName={UserName} Reason=账户已锁定至 {LockoutEnd}",
                input.UserName, user.LockoutEnd.Value);
            await RecordAuditAsync(new SecurityAuditEvent
            {
                UserId = user.Id,
                UserName = input.UserName,
                EventType = "LoginFailed",
                IpAddress = input.ClientIp,
                UserAgent = input.UserAgent,
                IsSuccess = false,
                FailureReason = $"账户已锁定至 {user.LockoutEnd.Value}"
            }, cancellationToken);
            return Result<LoginResponse>.Failure(ErrorCode.UserLocked, ErrorMessages.Get(ErrorCode.UserLocked));
        }

        var isPasswordValid = await _crossModuleService.VerifyPasswordAsync(input.UserName, input.Password, cancellationToken);

        if (!isPasswordValid)
        {
            var newFailedCount = user.FailedLoginCount + 1;
            DateTime? lockoutEnd = null;

            if (_loginOptions.LockoutEnabled
                && _securityOptions.AccountLockout.Enabled
                && newFailedCount >= _securityOptions.AccountLockout.MaxFailedCount)
            {
                lockoutEnd = DateTime.UtcNow.AddMinutes(_securityOptions.AccountLockout.LockoutMinutes);
                _logger.LogWarning("[Handler] Login account locked - UserName={UserName} FailedCount={Count} LockoutMinutes={Minutes}",
                    input.UserName, newFailedCount, _securityOptions.AccountLockout.LockoutMinutes);
            }
            else
            {
                _logger.LogWarning("[Handler] Login failed - UserName={UserName} Reason=密码错误 FailedCount={Count}",
                    input.UserName, newFailedCount);
            }

            await RecordAuditAsync(new SecurityAuditEvent
            {
                UserId = user.Id,
                UserName = input.UserName,
                EventType = "LoginFailed",
                IpAddress = input.ClientIp,
                UserAgent = input.UserAgent,
                IsSuccess = false,
                FailureReason = lockoutEnd.HasValue ? $"密码错误，账户已锁定至 {lockoutEnd}" : "密码错误"
            }, cancellationToken);

            // P1-14: 账户锁定单独审计（阈值触发时）
            if (lockoutEnd.HasValue)
            {
                await RecordAuditAsync(new SecurityAuditEvent
                {
                    UserId = user.Id,
                    UserName = input.UserName,
                    EventType = "AccountLockout",
                    IpAddress = input.ClientIp,
                    UserAgent = input.UserAgent,
                    IsSuccess = false,
                    FailureReason = $"账户因连续 {newFailedCount} 次失败登录被锁定至 {lockoutEnd}（阈值 {_securityOptions.AccountLockout.MaxFailedCount}）"
                }, cancellationToken);
            }

            await _crossModuleService.UpdateLoginFailureAsync(user.Id, newFailedCount, lockoutEnd, cancellationToken);
            return Result<LoginResponse>.Failure(ErrorCode.AuthInvalidCredentials, ErrorMessages.Get(ErrorCode.AuthInvalidCredentials));
        }

        await _crossModuleService.ResetLoginStateAsync(user.Id, cancellationToken);

        var userDetail = IdentityMapper.ToUserDetailDto(user);
        string userType = userDetail.Role == UserRole.SuperAdmin ? "superadmin" : "user";

        // 系统管理员附加 IsSysAdmin claim（sysadmin 独立用户标识，本地/远程统一）
        var token = user.IsSysAdmin
            ? _jwtService.GenerateToken(
                userDetail.Id.ToString(),
                userDetail.UserName,
                userDetail.Role,
                new Dictionary<string, string> { ["IsSysAdmin"] = "true" },
                userType)
            : _jwtService.GenerateToken(
                userDetail.Id.ToString(),
                userDetail.UserName,
                userDetail.Role,
                userType);

        var tokenExpireMinutes = _jwtOptions.AccessTokenExpirationMinutes;

        var response = new LoginResponse
        {
            Token = token,
            // T4(P0#2): 服务端签发刷新令牌——access token 即刷新凭据（服务端按会话哈希校验+旋转换新），
            // 修复客户端 TokenRefreshHandler 拿到空 RefreshToken 导致远程会话无法续期的问题
            RefreshToken = token,
            // T4(P1#11): RememberMe 时签发自动登录令牌（30 天，服务端轮换）
            AutoLoginToken = input.RememberMe
                ? _jwtService.GenerateAutoLoginToken(userDetail.Id.ToString(), userDetail.UserName, userDetail.Role, userType)
                : null,
            User = userDetail,
            ExpiresAt = DateTime.UtcNow.AddMinutes(tokenExpireMinutes),
            MustChangePassword = user.MustChangeOnNextLogin
        };

        var tokenHash = ComputeTokenHash(token);
        var session = AuthSession.Create(
            user.Id,
            tokenHash,
            response.ExpiresAt,
            input.ClientIp ?? "unknown",
            input.UserAgent);

        // Token 族旋转：新登录撤销该用户全部旧会话（登录踢出）
        await _sender.Send(
            new RevokeAllUserTokensCommand(user.Id, "新设备登录，旧会话已撤销"),
            cancellationToken);

        await _authSessionRepository.AddAsync(session, cancellationToken);

        await RecordAuditAsync(new SecurityAuditEvent
        {
            UserId = user.Id,
            UserName = input.UserName,
            EventType = "LoginSuccess",
            IpAddress = input.ClientIp,
            UserAgent = input.UserAgent,
            IsSuccess = true
        }, cancellationToken);

        _logger.LogInformation("[Handler] Login completed - UserName={UserName} Role={Role}", input.UserName, userDetail.Role);

        return Result<LoginResponse>.Success(response);
    }

    /// <summary>
    /// 按 LoginOptions.AuditLevel 决定是否落安全审计（None=不写，Minimal=仅失败，Full=全部）。
    /// </summary>
    private async Task RecordAuditAsync(SecurityAuditEvent auditEvent, CancellationToken ct)
    {
        if (_loginOptions.AuditLevel == SecurityAuditLevel.None)
            return;
        if (_loginOptions.AuditLevel == SecurityAuditLevel.Minimal && auditEvent.IsSuccess)
            return;

        await _securityAuditService.RecordEventAsync(auditEvent, ct);
    }

    private static string ComputeTokenHash(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
