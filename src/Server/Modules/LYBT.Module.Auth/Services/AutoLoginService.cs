using LYBT.Infrastructure.Services.CrossModule;
using LYBT.Module.Auth.Interfaces;
using LYBT.Module.Auth.Models;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Auth;
using LYBT.Shared.Models.Enums;
using LYBT.Shared.Primitives.ErrorCodes;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace LYBT.Module.Auth.Services
{
    /// <summary>
    /// AutoLoginToken 服务实现 - 负责 AutoLogin 的生成、验证、轮换
    /// 从 AuthService 分离，遵循单一职责原则
    /// </summary>
    public class AutoLoginService : IAutoLoginService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<AutoLoginService> _logger;
        private readonly IJwtService _jwtService;
        private readonly IUserCrossModuleService _crossModuleQuery;
        private readonly ISecurityAuditService _auditService;
        private readonly IRefreshTokenRepository _refreshTokenRepository;
        private readonly IAutoLoginTokenRepository _autoLoginTokenRepository;

        public AutoLoginService(
            IConfiguration configuration,
            ILogger<AutoLoginService> logger,
            IJwtService jwtService,
            IUserCrossModuleService crossModuleQuery,
            ISecurityAuditService auditService,
            IRefreshTokenRepository refreshTokenRepository,
            IAutoLoginTokenRepository autoLoginTokenRepository)
        {
            _configuration = configuration;
            _logger = logger;
            _jwtService = jwtService;
            _crossModuleQuery = crossModuleQuery;
            _auditService = auditService;
            _refreshTokenRepository = refreshTokenRepository;
            _autoLoginTokenRepository = autoLoginTokenRepository;
        }

        public async Task<Result<LoginResponse>> LoginWithAutoTokenAsync(AutoLoginRequest request, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(request.UserName))
                return Result<LoginResponse>.Failure(ErrorCode.AuthInvalidCredentials, "用户名不能为空");

            if (string.IsNullOrWhiteSpace(request.AutoLoginToken))
                return Result<LoginResponse>.Failure(ErrorCode.AuthInvalidCredentials, "AutoLoginToken不能为空");

            var tokenRecord = await _autoLoginTokenRepository.GetByTokenAndUsernameAsync(
                request.AutoLoginToken, request.UserName, cancellationToken);

            if (tokenRecord == null)
            {
                await _auditService.LogAsync(new SecurityAuditEvent
                {
                    EventType = "AutoLoginFailed",
                    UserName = request.UserName,
                    Success = false,
                    ErrorMessage = "AutoLoginToken不存在"
                });
                return Result<LoginResponse>.Failure(ErrorCode.AuthInvalidCredentials, "AutoLoginToken无效");
            }

            if (tokenRecord.IsUsed)
            {
                _logger.LogWarning("[SVC] AutoLogin.ReplayAttack - TokenId={TokenId} UserId={UserId} UserName={UserName}",
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

                return Result<LoginResponse>.Failure(ErrorCode.AuthTokenRevoked, "检测到安全威胁，请重新登录");
            }

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

                return Result<LoginResponse>.Failure(ErrorCode.AuthRefreshTokenExpired, $"AutoLoginToken{reason}，请重新登录");
            }

            var userBasic = await _crossModuleQuery.GetUserBasicInfoAsync(tokenRecord.UserId);
            if (userBasic == null)
            {
                return Result<LoginResponse>.Failure(ErrorCode.UserNotFound, "用户不存在");
            }

            if (userBasic.Status == CommonStatus.Disabled)
            {
                return Result<LoginResponse>.Failure(ErrorCode.UserDisabled, "用户已被禁用");
            }

            var userDto = TokenManagementService.MapToUserDetailDto(userBasic);
            string userType = userDto.Role == UserRole.SuperAdmin ? "superadmin" : "user";

            var jwtToken = _jwtService.GenerateToken(
                userDto.Id.ToString(),
                userDto.UserName,
                userDto.Role,
                userType);

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
            await _refreshTokenRepository.AddAsync(refreshTokenRecord, cancellationToken);

            var newAutoLoginToken = GenerateAutoLoginToken(
                userDto.Id,
                userDto.UserName,
                request.DeviceId,
                request.DeviceName,
                request.ClientIp,
                request.UserAgent,
                tokenRecord.FamilyId);

            tokenRecord.MarkAsUsed(newAutoLoginToken);
            await _autoLoginTokenRepository.SaveChangesAsync(cancellationToken);

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

            _logger.LogInformation("[SVC] AutoLogin completed - UserName={UserName} Role={Role}",
                userDto.UserName, userDto.Role);

            return Result<LoginResponse>.Success(response);
        }

        public string GenerateAutoLoginToken(
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

            _autoLoginTokenRepository.AddAsync(tokenRecord).Wait();
            return token;
        }

        public async Task RevokeAutoLoginTokenFamilyAsync(string familyId, string reason)
        {
            try
            {
                var familyTokens = await _autoLoginTokenRepository.GetActiveTokensByFamilyIdAsync(familyId);

                foreach (var token in familyTokens)
                {
                    token.Revoke(reason, "System:ReplayAttackDetection");
                }

                await _autoLoginTokenRepository.UpdateRangeAsync(familyTokens);

                _logger.LogWarning("[SVC] AutoLogin.RevokeFamily completed - FamilyId={FamilyId} RevokedCount={Count} Reason={Reason}",
                    familyId, familyTokens.Count, reason);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[SVC] AutoLogin.RevokeFamily failed - FamilyId={FamilyId}", familyId);
            }
        }
    }
}
