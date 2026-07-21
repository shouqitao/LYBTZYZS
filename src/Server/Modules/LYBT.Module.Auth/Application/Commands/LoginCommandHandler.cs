using System.Security.Cryptography;
using System.Text;
using MediatR;
using LYBT.Infrastructure.Services.CrossModule;
using LYBT.Module.Auth.Domain;
using LYBT.Module.Auth.Domain.Events;
using LYBT.Module.Auth.Interfaces;
using LYBT.Module.Auth.Models;
using LYBT.Shared.Configuration.Options.Server;
using LYBT.Shared.Models.Contracts.Auth;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.DTOs.Users;
using LYBT.Shared.Models.Enums;
using LYBT.Shared.Models.Primitives.ErrorCodes;
using LYBT.Shared.Models.Utilities.Security;
using LYBT.Shared.Models.Contracts.Common;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LYBT.Module.Auth.Application.Commands;

public class LoginCommandHandler : IRequestHandler<LoginCommand, Result<LoginResponse>>
{
    private readonly IJwtService _jwtService;
    private readonly ICrossModuleService _crossModuleService;
    private readonly IAuthSessionRepository _authSessionRepository;
    private readonly ISecurityAuditService _securityAuditService;
    private readonly IPublisher _publisher;
    private readonly ILogger<LoginCommandHandler> _logger;
    private readonly SecurityOptions _securityOptions;

    public LoginCommandHandler(
        IJwtService jwtService,
        ICrossModuleService crossModuleService,
        IAuthSessionRepository authSessionRepository,
        ISecurityAuditService securityAuditService,
        IPublisher publisher,
        ILogger<LoginCommandHandler> logger,
        IOptions<SecurityOptions> securityOptions)
    {
        _jwtService = jwtService;
        _crossModuleService = crossModuleService;
        _authSessionRepository = authSessionRepository;
        _securityAuditService = securityAuditService;
        _publisher = publisher;
        _logger = logger;
        _securityOptions = securityOptions?.Value ?? throw new ArgumentNullException(nameof(securityOptions));
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
            await _securityAuditService.RecordEventAsync(new SecurityAuditEvent
            {
                UserName = input.UserName,
                EventType = "LoginFailed",
                IpAddress = input.ClientIp,
                UserAgent = input.UserAgent,
                IsSuccess = false,
                FailureReason = "用户不存在"
            }, cancellationToken);
            return Result<LoginResponse>.Failure(ErrorCode.AuthInvalidCredentials, "用户名或密码错误");
        }

        if (user.Status == CommonStatus.Disabled)
        {
            _logger.LogWarning("[Handler] Login failed - UserName={UserName} Reason=用户已被禁用", input.UserName);
            await _securityAuditService.RecordEventAsync(new SecurityAuditEvent
            {
                UserId = user.Id,
                UserName = input.UserName,
                EventType = "LoginFailed",
                IpAddress = input.ClientIp,
                UserAgent = input.UserAgent,
                IsSuccess = false,
                FailureReason = "用户已被禁用"
            }, cancellationToken);
            return Result<LoginResponse>.Failure(ErrorCode.UserDisabled, "用户已被禁用");
        }

        if (user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTime.UtcNow)
        {
            _logger.LogWarning("[Handler] Login failed - UserName={UserName} Reason=账户已锁定至 {LockoutEnd}",
                input.UserName, user.LockoutEnd.Value);
            await _securityAuditService.RecordEventAsync(new SecurityAuditEvent
            {
                UserId = user.Id,
                UserName = input.UserName,
                EventType = "LoginFailed",
                IpAddress = input.ClientIp,
                UserAgent = input.UserAgent,
                IsSuccess = false,
                FailureReason = $"账户已锁定至 {user.LockoutEnd.Value}"
            }, cancellationToken);
            return Result<LoginResponse>.Failure(ErrorCode.UserLocked, "账号已被锁定，请稍后重试");
        }

        var isPasswordValid = await _crossModuleService.VerifyPasswordAsync(input.UserName, input.Password, cancellationToken);

        if (!isPasswordValid)
        {
            var newFailedCount = user.FailedLoginCount + 1;
            DateTime? lockoutEnd = null;

            if (_securityOptions.AccountLockout.Enabled && newFailedCount >= _securityOptions.AccountLockout.MaxFailedCount)
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

            await _securityAuditService.RecordEventAsync(new SecurityAuditEvent
            {
                UserId = user.Id,
                UserName = input.UserName,
                EventType = "LoginFailed",
                IpAddress = input.ClientIp,
                UserAgent = input.UserAgent,
                IsSuccess = false,
                FailureReason = lockoutEnd.HasValue ? $"密码错误，账户已锁定至 {lockoutEnd}" : "密码错误"
            }, cancellationToken);

            await _crossModuleService.UpdateLoginFailureAsync(user.Id, newFailedCount, lockoutEnd, cancellationToken);
            return Result<LoginResponse>.Failure(ErrorCode.AuthInvalidCredentials, "用户名或密码错误");
        }

        await _crossModuleService.ResetLoginStateAsync(user.Id, cancellationToken);

        var userDetail = MapToUserDetailDto(user);
        string userType = userDetail.Role == UserRole.SuperAdmin ? "superadmin" : "user";

        var token = _jwtService.GenerateToken(
            userDetail.Id.ToString(),
            userDetail.UserName,
            userDetail.Role,
            userType);

        var tokenExpireMinutes = 60;

        var response = new LoginResponse
        {
            Token = token,
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
        await _authSessionRepository.AddAsync(session, cancellationToken);

        await _publisher.Publish(
            new SessionCreatedEvent(session.Id, user.Id, session.IpAddress),
            cancellationToken);

        await _securityAuditService.RecordEventAsync(new SecurityAuditEvent
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

    private static UserDetailDto MapToUserDetailDto(UserCredentialDto user) => new()
    {
        Id = user.Id,
        UserName = user.UserName,
        RealName = user.RealName,
        Role = user.Role,
        Status = user.Status,
        PhoneNumber = user.PhoneNumber,
        CreatedAt = user.CreatedAt
    };

    private static string ComputeTokenHash(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
