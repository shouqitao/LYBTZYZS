using LYBT.Infrastructure.Services.CrossModule;
using LYBT.Module.Auth.Interfaces;
using LYBT.Module.Auth.Models;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Auth;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.DTOs.Users;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using GenericErrorCode = LYBT.Shared.Primitives.ErrorCodes.ErrorCode;

namespace LYBT.Module.Auth.Services;

/// <summary>
/// Token 管理服务 - 负责 Token 刷新、验证、会话查询和 Family 撤销
/// 从 AuthService 拆分而来，职责: Token 生命周期管理
/// </summary>
public class TokenManagementService : ITokenManagementService
{
    private readonly IJwtService _jwtService;
    private readonly IUserCrossModuleService _crossModuleQuery;
    private readonly ILogger<TokenManagementService> _logger;
    private readonly IRefreshTokenRepository _tokenRepository;
    private readonly IConfiguration _configuration;
    private readonly ISecurityAuditService _auditService;

    public TokenManagementService(
        IJwtService jwtService,
        IUserCrossModuleService crossModuleQuery,
        ILogger<TokenManagementService> logger,
        IRefreshTokenRepository tokenRepository,
        IConfiguration configuration,
        ISecurityAuditService auditService)
    {
        _jwtService = jwtService;
        _crossModuleQuery = crossModuleQuery;
        _logger = logger;
        _tokenRepository = tokenRepository;
        _configuration = configuration;
        _auditService = auditService;
    }

    /// <inheritdoc/>
    public async Task<Result<LoginResponse>> RefreshTokenAsync(string refreshToken)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
            return Result<LoginResponse>.Failure(GenericErrorCode.AuthRefreshTokenInvalid, "RefreshToken不能为空");

        // 1. 查询 RefreshToken 记录
        var tokenRecord = await _tokenRepository.GetByTokenAsync(refreshToken);

        if (tokenRecord == null)
        {
            await _auditService.LogAsync(new SecurityAuditEvent
            {
                EventType = "RefreshTokenRejected",
                Success = false,
                ErrorMessage = "Token不存在"
            });
            return Result<LoginResponse>.Failure(GenericErrorCode.AuthRefreshTokenInvalid, "RefreshToken不存在");
        }

        // 2. 检测重放攻击
        if (tokenRecord.IsUsed)
        {
            _logger.LogWarning("[SVC] TokenMgmt.RefreshToken -> ReplayAttack - TokenId={TokenId} FamilyId={FamilyId} UserId={UserId}",
                tokenRecord.Id, tokenRecord.FamilyId, tokenRecord.UserId);

            if (!string.IsNullOrEmpty(tokenRecord.FamilyId))
            {
                await RevokeTokenFamilyAsync(tokenRecord.FamilyId, "检测到重放攻击，整个Token Family已失效");
            }

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

        // 3. 验证 Token 是否有效
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

            await _auditService.LogAsync(new SecurityAuditEvent
            {
                EventType = "RefreshTokenRejected",
                UserId = tokenRecord.UserId,
                UserType = tokenRecord.UserType,
                Success = false,
                ErrorMessage = $"Token{reason}"
            });

            _logger.LogWarning("[SVC] TokenMgmt.RefreshToken -> Invalid - Reason={Reason}", reason);
            return Result<LoginResponse>.Failure(errorCode, $"RefreshToken{reason}，请重新登录");
        }

        // 4. 记录使用并检查异常使用
        tokenRecord.RecordUsage();
        if (tokenRecord.UsageCount > 100)
        {
            _logger.LogWarning("[SVC] TokenMgmt.RefreshToken -> AbnormalUsage - UsageCount={Count}", tokenRecord.UsageCount);
        }

        // 5. 通过 IUserCrossModuleService 获取用户信息
        var userBasic = await _crossModuleQuery.GetUserBasicInfoAsync(tokenRecord.UserId);
        if (userBasic == null)
            return Result<LoginResponse>.Failure(GenericErrorCode.UserNotFound);

        var userDto = MapToUserDetailDto(userBasic);
        string userType = userDto.Role == UserRole.SuperAdmin ? "superadmin" : "user";

        _logger.LogDebug("[SVC] TokenMgmt.RefreshToken -> UserLoaded - UserName={UserName} Role={Role}", userDto.UserName, userDto.Role);

        // 6. 生成新的 Access Token
        var newAccessToken = _jwtService.GenerateToken(
            userDto.Id.ToString(),
            userDto.UserName,
            userDto.Role,
            userType);

        // 7. Token 轮换
        var newRefreshToken = GenerateRefreshToken();
        var refreshTokenExpireDays = _configuration.GetValue<int?>("Lybt:Jwt:RefreshTokenExpirationDays") ?? 7;

        tokenRecord.MarkAsUsed(newRefreshToken);

        var newTokenRecord = new LYBT.Entities.Auth.RefreshToken
        {
            Token = newRefreshToken,
            UserId = userDto.Id,
            UserType = userType,
            Jti = Guid.NewGuid().ToString(),
            ExpiresAt = DateTime.UtcNow.AddDays(refreshTokenExpireDays),
            AbsoluteExpiresAt = tokenRecord.AbsoluteExpiresAt,
            FamilyId = tokenRecord.FamilyId ?? Guid.NewGuid().ToString()
        };

        await _tokenRepository.AddAsync(newTokenRecord);
        await _tokenRepository.SaveChangesAsync();

        // 8. 返回新 Token 对
        var response = new LoginResponse
        {
            Token = newAccessToken,
            User = userDto,
            RefreshToken = newRefreshToken,
            ExpiresAt = DateTime.UtcNow.AddMinutes(
                _configuration.GetValue<int?>("Lybt:Jwt:ExpireMinutes") ?? 15)
        };

        await _auditService.LogAsync(new SecurityAuditEvent
        {
            EventType = "RefreshToken",
            UserId = userDto.Id,
            UserType = userType,
            UserName = userDto.UserName,
            Success = true
        });

        _logger.LogInformation("[SVC] TokenMgmt.RefreshToken completed - UserName={UserName}",
            userDto.UserName);

        return Result<LoginResponse>.Success(response);
    }

    /// <inheritdoc/>
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
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[SVC] TokenMgmt.ValidateToken -> Failed");
            return Result<bool>.Failure(GenericErrorCode.AuthTokenInvalid);
        }
    }

    /// <inheritdoc/>
    public async Task<Result<object>> GetSessionInfoAsync(string token)
    {
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

    /// <inheritdoc/>
    public async Task RevokeTokenFamilyAsync(string familyId, string reason)
    {
        try
        {
            var familyTokens = await _tokenRepository.GetActiveTokensByFamilyIdAsync(familyId);

            foreach (var token in familyTokens)
            {
                token.Revoke(reason, "System:ReplayAttackDetection");
            }

            await _tokenRepository.UpdateRangeAsync(familyTokens);

            _logger.LogWarning("[SVC] TokenMgmt.RevokeTokenFamily completed - FamilyId={FamilyId} RevokedCount={Count} Reason={Reason}",
                familyId, familyTokens.Count, reason);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[SVC] TokenMgmt.RevokeTokenFamily failed - FamilyId={FamilyId}", familyId);
        }
    }

    /// <summary>
    /// 生成安全的 RefreshToken 字符串
    /// </summary>
    internal static string GenerateRefreshToken()
    {
        var randomBytes = new byte[64];
        using var rng = System.Security.Cryptography.RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes);
    }

    /// <summary>
    /// 将 UserBasicDto 映射为 UserDetailDto (LoginResponse 需要)
    /// </summary>
    internal static UserDetailDto MapToUserDetailDto(UserBasicDto basic)
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
