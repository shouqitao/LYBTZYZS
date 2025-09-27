using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using LYBT.Entities.Auth;
using LYBT.Entities.Users;
using LYBT.Infrastructure.Configuration.Options;
using LYBT.Infrastructure.Security;
using LYBT.Module.Auth.Interfaces;
using LYBT.Shared.Models.Contracts.Auth;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace LYBT.Module.Auth.Services
{
    /// <summary>
    /// 增强的JWT服务实现
    /// 支持RefreshToken、密钥轮换和安全级别验证
    /// </summary>
    public class EnhancedJwtService : IEnhancedJwtService
    {
        private readonly IRefreshTokenRepository _refreshTokenRepository;
        private readonly ISecurityKeyService _securityKeyService;
        private readonly ITokenBlacklistService _blacklistService;
        private readonly JwtOptions _jwtOptions;
        private readonly ILogger<EnhancedJwtService> _logger;
        private readonly JwtSecurityTokenHandler _tokenHandler;

        public EnhancedJwtService(
            IRefreshTokenRepository refreshTokenRepository,
            ISecurityKeyService securityKeyService,
            ITokenBlacklistService blacklistService,
            IOptions<JwtOptions> jwtOptions,
            ILogger<EnhancedJwtService> logger)
        {
            _refreshTokenRepository = refreshTokenRepository;
            _securityKeyService = securityKeyService;
            _blacklistService = blacklistService;
            _jwtOptions = jwtOptions.Value;
            _logger = logger;
            _tokenHandler = new JwtSecurityTokenHandler();
        }

        /// <summary>
        /// 生成Token对（AccessToken + RefreshToken）
        /// </summary>
        public async Task<TokenPair> GenerateTokenPairAsync(User user, string? deviceId = null, string? deviceName = null)
        {
            try
            {
                // 生成JWT ID
                var jwtId = Guid.NewGuid().ToString();

                // 生成Access Token
                var accessToken = await GenerateAccessTokenAsync(user, jwtId);

                // 生成Refresh Token
                var refreshToken = await GenerateRefreshTokenAsync(user, jwtId, deviceId, deviceName);

                return new TokenPair
                {
                    AccessToken = accessToken,
                    RefreshToken = refreshToken.Token,
                    ExpiresIn = (int)TimeSpan.FromMinutes(_jwtOptions.ExpireMinutes).TotalSeconds,
                    RefreshExpiresIn = (int)TimeSpan.FromDays(_jwtOptions.RefreshTokenExpireDays).TotalSeconds,
                    TokenType = "Bearer"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "生成Token对失败: UserId={UserId}", user.Id);
                throw;
            }
        }

        /// <summary>
        /// 刷新Token
        /// </summary>
        public async Task<TokenPair> RefreshTokenAsync(string refreshToken, string? newDeviceInfo = null)
        {
            try
            {
                // 查找RefreshToken
                var token = await _refreshTokenRepository.GetByTokenAsync(refreshToken);
                if (token == null)
                {
                    _logger.LogWarning("RefreshToken不存在: {Token}", refreshToken);
                    throw new SecurityTokenException("Invalid refresh token");
                }

                // 验证RefreshToken
                if (token.IsUsed)
                {
                    _logger.LogWarning("RefreshToken已被使用，可能存在安全风险: {Token}", refreshToken);
                    // 撤销该用户所有token（安全措施）
                    await _refreshTokenRepository.RevokeAllUserTokensAsync(token.UserId);
                    throw new SecurityTokenException("Refresh token has been used");
                }

                if (token.IsRevoked)
                {
                    _logger.LogWarning("RefreshToken已被撤销: {Token}", refreshToken);
                    throw new SecurityTokenException("Refresh token has been revoked");
                }

                if (token.ExpiresAt < DateTime.UtcNow)
                {
                    _logger.LogWarning("RefreshToken已过期: {Token}", refreshToken);
                    throw new SecurityTokenException("Refresh token has expired");
                }

                // 标记为已使用（单次使用）
                await _refreshTokenRepository.MarkAsUsedAsync(refreshToken);

                // 生成新的Token对
                var user = token.User;
                if (user == null)
                {
                    _logger.LogError("RefreshToken关联的用户不存在: {UserId}", token.UserId);
                    throw new SecurityTokenException("Associated user not found");
                }

                return await GenerateTokenPairAsync(user, token.DeviceId, newDeviceInfo ?? token.DeviceName);
            }
            catch (Exception ex) when (ex is not SecurityTokenException)
            {
                _logger.LogError(ex, "刷新Token失败: {Token}", refreshToken);
                throw new SecurityTokenException("Failed to refresh token", ex);
            }
        }

        /// <summary>
        /// 撤销Token
        /// </summary>
        public async Task RevokeTokenAsync(string refreshToken)
        {
            try
            {
                var token = await _refreshTokenRepository.GetByTokenAsync(refreshToken);
                if (token == null)
                {
                    _logger.LogWarning("尝试撤销不存在的RefreshToken: {Token}", refreshToken);
                    return;
                }

                // 将关联的JWT ID添加到黑名单
                if (!string.IsNullOrEmpty(token.JwtId))
                {
                    // 计算JWT过期时间（RefreshToken过期时间通常比JWT长，这里使用JWT的实际过期时间）
                    var jwtExpiration = DateTime.UtcNow.AddMinutes(_jwtOptions.ExpireMinutes);
                    await _blacklistService.BlacklistTokenAsync(token.JwtId, jwtExpiration, "Token revoked via refresh token");
                    _logger.LogInformation("JWT ID已添加到黑名单: {JwtId}", token.JwtId);
                }

                // 撤销RefreshToken
                token.IsRevoked = true;
                token.RevokedAt = DateTime.UtcNow;
                await _refreshTokenRepository.UpdateAsync(token);

                _logger.LogInformation("RefreshToken已撤销: {Token}", refreshToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "撤销Token失败: {Token}", refreshToken);
                throw;
            }
        }

        /// <summary>
        /// 撤销用户的所有Token
        /// </summary>
        public async Task RevokeAllUserTokensAsync(Guid userId)
        {
            try
            {
                // 先获取用户所有有效的RefreshToken
                var activeTokens = await _refreshTokenRepository.GetActiveTokensByUserAsync(userId);
                
                // 逐个添加JWT ID到黑名单
                var jwtExpiration = DateTime.UtcNow.AddMinutes(_jwtOptions.ExpireMinutes);
                var blacklistedCount = 0;
                
                foreach (var token in activeTokens)
                {
                    if (!string.IsNullOrEmpty(token.JwtId))
                    {
                        var success = await _blacklistService.BlacklistTokenAsync(token.JwtId, jwtExpiration, "User logout - all tokens revoked");
                        if (success) blacklistedCount++;
                    }
                }

                // 使用现有的批量撤销方法
                var userTokensBlacklisted = await _blacklistService.BlacklistAllUserTokensAsync(userId.ToString(), "All user tokens revoked");
                
                _logger.LogInformation("已将用户 {UserId} 的 {BlacklistedCount} 个JWT ID添加到黑名单，批量撤销 {UserTokens} 个Token", 
                    userId, blacklistedCount, userTokensBlacklisted);

                // 撤销所有RefreshToken
                await _refreshTokenRepository.RevokeAllUserTokensAsync(userId);
                _logger.LogInformation("已撤销用户所有Token: {UserId}", userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "撤销用户所有Token失败: {UserId}", userId);
                throw;
            }
        }

        /// <summary>
        /// 撤销设备的所有Token
        /// </summary>
        public async Task RevokeDeviceTokensAsync(Guid userId, string deviceId)
        {
            try
            {
                // 先获取设备的所有有效RefreshToken
                var deviceTokens = await _refreshTokenRepository.GetActiveTokensByUserAsync(userId);
                var deviceSpecificTokens = deviceTokens.Where(t => t.DeviceId == deviceId).ToList();
                
                // 逐个添加JWT ID到黑名单
                var jwtExpiration = DateTime.UtcNow.AddMinutes(_jwtOptions.ExpireMinutes);
                var blacklistedCount = 0;
                
                foreach (var token in deviceSpecificTokens)
                {
                    if (!string.IsNullOrEmpty(token.JwtId))
                    {
                        var success = await _blacklistService.BlacklistTokenAsync(token.JwtId, jwtExpiration, $"Device {deviceId} logout");
                        if (success) blacklistedCount++;
                    }
                }

                if (blacklistedCount > 0)
                {
                    _logger.LogInformation("已将设备 {DeviceId} 的 {Count} 个JWT ID添加到黑名单", deviceId, blacklistedCount);
                }

                // 撤销设备的所有RefreshToken
                await _refreshTokenRepository.RevokeDeviceTokensAsync(userId, deviceId);
                _logger.LogInformation("已撤销设备所有Token: UserId={UserId}, DeviceId={DeviceId}", userId, deviceId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "撤销设备Token失败: DeviceId={DeviceId}", deviceId);
                throw;
            }
        }

        /// <summary>
        /// 验证Token的安全级别
        /// </summary>
        public async Task<TokenSecurityValidationResult> ValidateTokenSecurityAsync(string token)
        {
            var result = new TokenSecurityValidationResult
            {
                IsValid = false,
                SecurityLevel = TokenSecurityLevel.None,
                Reasons = new List<string>()
            };

            try
            {
                // 获取验证密钥
                var keys = await _securityKeyService.GetAllKeysAsync();
                var validationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = _jwtOptions.Issuer,
                    ValidAudience = _jwtOptions.Audience,
                    IssuerSigningKeys = keys.Select(k => new SymmetricSecurityKey(Encoding.UTF8.GetBytes(k.Key))),
                    ClockSkew = TimeSpan.FromSeconds(_jwtOptions.ClockSkewSeconds)
                };

                // 验证Token
                var principal = _tokenHandler.ValidateToken(token, validationParameters, out var validatedToken);

                if (validatedToken is not JwtSecurityToken jwtToken)
                {
                    result.Reasons.Add("Token格式无效");
                    return result;
                }

                // 基础验证通过
                result.IsValid = true;
                result.SecurityLevel = TokenSecurityLevel.Basic;

                // 检查黑名单
                var jwtIdClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Jti);
                if (jwtIdClaim != null && !string.IsNullOrEmpty(jwtIdClaim.Value))
                {
                    var isBlacklisted = await _blacklistService.IsTokenBlacklistedAsync(jwtIdClaim.Value);
                    if (isBlacklisted)
                    {
                        result.IsValid = false;
                        result.SecurityLevel = TokenSecurityLevel.None;
                        result.Reasons.Add("Token已被撤销（在黑名单中）");
                        _logger.LogWarning("检测到黑名单JWT Token: {JwtId}", jwtIdClaim.Value);
                        return result;
                    }
                }
                else
                {
                    result.Reasons.Add("Token缺少JWT ID claim");
                }

                // 检查算法
                if (jwtToken.Header.Alg.Equals("HS512", StringComparison.OrdinalIgnoreCase))
                {
                    result.SecurityLevel = TokenSecurityLevel.Standard;
                }
                else
                {
                    result.Reasons.Add($"使用了较弱的算法: {jwtToken.Header.Alg}");
                }

                // 检查密钥强度
                var keyVersion = jwtToken.Claims.FirstOrDefault(c => c.Type == "key_version")?.Value;
                if (!string.IsNullOrEmpty(keyVersion))
                {
                    var currentKey = await _securityKeyService.GetCurrentKeyAsync();
                    if (keyVersion == currentKey.Version)
                    {
                        result.SecurityLevel = TokenSecurityLevel.Enhanced;
                    }
                    else
                    {
                        result.Reasons.Add("使用了旧版本的密钥");
                    }
                }

                // 检查Token年龄
                var issuedAt = jwtToken.IssuedAt;
                var tokenAge = DateTime.UtcNow - issuedAt;
                if (tokenAge > TimeSpan.FromHours(1))
                {
                    result.Reasons.Add($"Token已签发超过1小时: {tokenAge.TotalHours:F1}小时");
                    result.RequiresAdditionalVerification = true;
                }

                return result;
            }
            catch (SecurityTokenExpiredException)
            {
                result.Reasons.Add("Token已过期");
                return result;
            }
            catch (SecurityTokenInvalidSignatureException)
            {
                result.Reasons.Add("Token签名无效");
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "验证Token安全级别失败");
                result.Reasons.Add($"验证失败: {ex.Message}");
                return result;
            }
        }

        #region 私有方法

        /// <summary>
        /// 生成Access Token
        /// </summary>
        private async Task<string> GenerateAccessTokenAsync(User user, string jwtId)
        {
            var key = await _securityKeyService.GetCurrentKeyAsync();
            var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key.Key));
            var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha512);

            var claims = new List<Claim>
            {
                new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new(JwtRegisteredClaimNames.Jti, jwtId),
                new(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),
                new(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new(ClaimTypes.Name, user.RealName ?? user.Email),
                new(ClaimTypes.Email, user.Email),
                new(ClaimTypes.Role, user.Role.ToString()),
                new("key_version", key.Version)
            };

            var token = new JwtSecurityToken(
                issuer: _jwtOptions.Issuer,
                audience: _jwtOptions.Audience,
                claims: claims,
                notBefore: DateTime.UtcNow,
                expires: DateTime.UtcNow.AddMinutes(_jwtOptions.ExpireMinutes),
                signingCredentials: credentials
            );

            return _tokenHandler.WriteToken(token);
        }

        /// <summary>
        /// 生成Refresh Token
        /// </summary>
        private async Task<RefreshToken> GenerateRefreshTokenAsync(User user, string jwtId, string? deviceId, string? deviceName)
        {
            // 生成安全的随机Token
            var randomBytes = new byte[64];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomBytes);
            }
            var tokenValue = Convert.ToBase64String(randomBytes);

            var refreshToken = new RefreshToken
            {
                Id = Guid.NewGuid(),
                Token = tokenValue,
                JwtId = jwtId,
                UserId = user.Id,
                DeviceId = deviceId ?? Guid.NewGuid().ToString(),
                DeviceName = deviceName ?? "Unknown Device",
                UserAgent = null, // 需要从HTTP上下文获取
                IpAddress = null, // 需要从HTTP上下文获取
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddDays(_jwtOptions.RefreshTokenExpireDays),
                IsUsed = false,
                IsRevoked = false,
                IsDeleted = false
            };

            await _refreshTokenRepository.AddAsync(refreshToken);
            return refreshToken;
        }

        #endregion
    }
}