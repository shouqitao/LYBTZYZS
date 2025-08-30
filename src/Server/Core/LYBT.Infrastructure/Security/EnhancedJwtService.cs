using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace LYBT.Infrastructure.Security
{
    /// <summary>
    /// 增强JWT服务 - UltraThink重构安全认证架构
    /// 提供更安全的JWT令牌生成和验证功能
    /// </summary>
    public class EnhancedJwtService : IEnhancedJwtService
    {
        private readonly JwtSecurityTokenHandler _tokenHandler;
        private readonly TokenValidationParameters _tokenValidationParameters;
        private readonly EnhancedJwtOptions _options;
        private readonly IEncryptionService _encryptionService;

        private readonly ILogger<EnhancedJwtService> _logger;

        public EnhancedJwtService(
            EnhancedJwtOptions options,
            IEncryptionService encryptionService,
            ILogger<EnhancedJwtService> logger)
        {
            _options = options;
            _encryptionService = encryptionService;

            _logger = logger;
            _tokenHandler = new JwtSecurityTokenHandler();

            _tokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = _options.Issuer,
                ValidAudience = _options.Audience,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SecretKey)),
                ClockSkew = TimeSpan.FromMinutes(_options.ClockSkewMinutes),
                RequireExpirationTime = true,
                RequireSignedTokens = true,
                RequireAudience = true
            };
        }

        /// <summary>
        /// 生成访问令牌
        /// </summary>
        public async Task<TokenResult> GenerateAccessTokenAsync(TokenRequest request)
        {
            try
            {
                var jti = Guid.NewGuid().ToString("N");
                var issuedAt = DateTime.UtcNow;
                var expires = issuedAt.AddMinutes(request.RememberMe 
                    ? _options.LongTermExpiryMinutes 
                    : _options.ShortTermExpiryMinutes);

                var claims = new List<Claim>
                {
                    new(ClaimTypes.NameIdentifier, request.UserId.ToString()),
                    new(ClaimTypes.Name, request.Username),
                    new(ClaimTypes.Role, request.Role),
                    new(JwtRegisteredClaimNames.Sub, request.UserId.ToString()),
                    new(JwtRegisteredClaimNames.Jti, jti),
                    new(JwtRegisteredClaimNames.Iat, new DateTimeOffset(issuedAt).ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),
                    new("client_ip", request.ClientIP),
                    new("session_id", request.SessionId ?? Guid.NewGuid().ToString("N")),
                    new("device_id", request.DeviceId ?? "unknown"),
                    new("token_type", "access_token")
                };

                // 添加自定义声明
                if (request.CustomClaims != null)
                {
                    foreach (var customClaim in request.CustomClaims)
                    {
                        claims.Add(new Claim(customClaim.Key, customClaim.Value));
                    }
                }

                var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SecretKey));
                var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

                var token = new JwtSecurityToken(
                    issuer: _options.Issuer,
                    audience: _options.Audience,
                    claims: claims,
                    notBefore: issuedAt,
                    expires: expires,
                    signingCredentials: credentials
                );

                var tokenString = _tokenHandler.WriteToken(token);
                var refreshToken = GenerateRefreshToken();

                // 计算令牌哈希用于安全审计
                var tokenHash = _encryptionService.Hash(tokenString);

                // 记录令牌生成审计日志
                await LogTokenGenerationAsync(request, jti, tokenHash);

                return new TokenResult
                {
                    AccessToken = tokenString,
                    RefreshToken = refreshToken,
                    TokenType = "Bearer",
                    ExpiresIn = (int)(expires - issuedAt).TotalSeconds,
                    ExpiresAt = expires,
                    TokenId = jti,
                    TokenHash = tokenHash
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "生成访问令牌失败: {Username}", request.Username);
                throw new SecurityException("令牌生成失败", ex);
            }
        }

        /// <summary>
        /// 验证访问令牌
        /// </summary>
        public async Task<TokenValidationResult> ValidateAccessTokenAsync(string token, string? clientIP = null)
        {
            var result = new TokenValidationResult { IsValid = false };

            try
            {
                if (string.IsNullOrEmpty(token))
                {
                    result.Error = "令牌不能为空";
                    return result;
                }

                // 验证令牌格式和签名
                var principal = _tokenHandler.ValidateToken(token, _tokenValidationParameters, out var validatedToken);
                
                if (validatedToken is not JwtSecurityToken jwt)
                {
                    result.Error = "无效的JWT令牌";
                    return result;
                }

                // 验证令牌类型
                var tokenType = principal.FindFirst("token_type")?.Value;
                if (tokenType != "access_token")
                {
                    result.Error = "令牌类型错误";
                    return result;
                }

                // 验证客户端IP（如果需要）
                if (_options.ValidateClientIP && !string.IsNullOrEmpty(clientIP))
                {
                    var tokenClientIP = principal.FindFirst("client_ip")?.Value;
                    if (tokenClientIP != clientIP)
                    {
                        result.Error = "客户端IP地址不匹配";
                        await LogSuspiciousActivityAsync("IP不匹配", token, clientIP, tokenClientIP);
                        return result;
                    }
                }

                // 检查令牌是否已被撤销
                var jti = principal.FindFirst(JwtRegisteredClaimNames.Jti)?.Value;
                if (!string.IsNullOrEmpty(jti) && await IsTokenRevokedAsync(jti))
                {
                    result.Error = "令牌已被撤销";
                    return result;
                }

                // 验证成功
                result.IsValid = true;
                result.Principal = principal;
                result.TokenId = jti;
                result.UserId = Guid.TryParse(principal.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var userId) ? userId : null;
                result.Username = principal.FindFirst(ClaimTypes.Name)?.Value;
                result.Role = principal.FindFirst(ClaimTypes.Role)?.Value;
                result.SessionId = principal.FindFirst("session_id")?.Value;
                result.DeviceId = principal.FindFirst("device_id")?.Value;

                return result;
            }
            catch (SecurityTokenExpiredException)
            {
                result.Error = "令牌已过期";
                return result;
            }
            catch (SecurityTokenInvalidSignatureException)
            {
                result.Error = "令牌签名无效";
                await LogSuspiciousActivityAsync("签名无效", token, clientIP);
                return result;
            }
            catch (SecurityTokenException ex)
            {
                result.Error = $"令牌验证失败: {ex.Message}";
                await LogSuspiciousActivityAsync("验证异常", token, clientIP, ex.Message);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "验证访问令牌时发生未知错误");
                result.Error = "令牌验证过程中发生错误";
                return result;
            }
        }

        /// <summary>
        /// 刷新访问令牌
        /// </summary>
        public async Task<TokenResult> RefreshAccessTokenAsync(string refreshToken, string? clientIP = null)
        {
            try
            {
                // 验证刷新令牌
                var storedToken = await GetStoredRefreshTokenAsync(refreshToken);
                if (storedToken == null || storedToken.ExpiresAt < DateTime.UtcNow)
                {
                    throw new SecurityException("刷新令牌无效或已过期");
                }

                // 验证客户端IP（如果需要）
                if (_options.ValidateClientIP && !string.IsNullOrEmpty(clientIP) 
                    && storedToken.ClientIP != clientIP)
                {
                    await LogSuspiciousActivityAsync("刷新令牌IP不匹配", refreshToken, clientIP, storedToken.ClientIP);
                    throw new SecurityException("客户端IP地址不匹配");
                }

                // 生成新的访问令牌
                var tokenRequest = new TokenRequest
                {
                    UserId = storedToken.UserId,
                    Username = storedToken.Username,
                    Role = storedToken.Role,
                    ClientIP = clientIP ?? storedToken.ClientIP,
                    SessionId = storedToken.SessionId,
                    DeviceId = storedToken.DeviceId,
                    RememberMe = storedToken.IsLongTerm
                };

                var newToken = await GenerateAccessTokenAsync(tokenRequest);

                // 生成新的刷新令牌
                newToken.RefreshToken = GenerateRefreshToken();
                await StoreRefreshTokenAsync(newToken.RefreshToken, storedToken);

                // 撤销旧的刷新令牌
                await RevokeRefreshTokenAsync(refreshToken);

                return newToken;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "刷新访问令牌失败");
                throw new SecurityException("令牌刷新失败", ex);
            }
        }

        /// <summary>
        /// 撤销访问令牌
        /// </summary>
        public async Task RevokeAccessTokenAsync(string tokenId, string reason = "用户注销")
        {
            try
            {
                await RevokeTokenAsync(tokenId, reason);
                _logger.LogInformation("访问令牌已撤销: {TokenId}, 原因: {Reason}", tokenId, reason);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "撤销访问令牌失败: {TokenId}", tokenId);
                throw;
            }
        }

        /// <summary>
        /// 撤销所有用户令牌
        /// </summary>
        public async Task RevokeAllUserTokensAsync(Guid userId, string reason = "安全操作")
        {
            try
            {
                await RevokeAllTokensByUserAsync(userId, reason);
                _logger.LogInformation("用户所有令牌已撤销: {UserId}, 原因: {Reason}", userId, reason);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "撤销用户所有令牌失败: {UserId}", userId);
                throw;
            }
        }

        /// <summary>
        /// 生成刷新令牌
        /// </summary>
        private string GenerateRefreshToken()
        {
            using var rng = RandomNumberGenerator.Create();
            var randomBytes = new byte[64];
            rng.GetBytes(randomBytes);
            return Convert.ToBase64String(randomBytes);
        }

        /// <summary>
        /// 记录令牌生成审计日志
        /// </summary>
        private Task LogTokenGenerationAsync(TokenRequest request, string tokenId, string tokenHash)
        {
            try
            {
                _logger.LogInformation("访问令牌已生成: {TokenId}, 用户: {Username}", tokenId, request.Username);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "记录令牌生成审计失败");
            }
            return Task.CompletedTask;
        }

        /// <summary>
        /// 记录可疑活动
        /// </summary>
        private Task LogSuspiciousActivityAsync(string activity, string? token = null, 
            string? clientIP = null, string? additionalInfo = null)
        {
            try
            {
                _logger.LogWarning("可疑令牌活动: {Activity}, IP: {ClientIP}, 详情: {AdditionalInfo}", 
                    activity, clientIP ?? "unknown", additionalInfo);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "记录可疑令牌活动失败");
            }
            return Task.CompletedTask;
        }

        // 以下方法需要根据实际的存储实现
        private async Task<bool> IsTokenRevokedAsync(string tokenId)
        {
            // TODO: 实现令牌撤销状态检查（Redis/数据库）
            return await Task.FromResult(false);
        }

        private async Task<StoredRefreshToken?> GetStoredRefreshTokenAsync(string refreshToken)
        {
            // TODO: 实现刷新令牌存储获取（Redis/数据库）
            return await Task.FromResult<StoredRefreshToken?>(null);
        }

        private async Task StoreRefreshTokenAsync(string refreshToken, StoredRefreshToken tokenInfo)
        {
            // TODO: 实现刷新令牌存储（Redis/数据库）
            await Task.CompletedTask;
        }

        private async Task RevokeRefreshTokenAsync(string refreshToken)
        {
            // TODO: 实现刷新令牌撤销（Redis/数据库）
            await Task.CompletedTask;
        }

        private async Task RevokeTokenAsync(string tokenId, string reason)
        {
            // TODO: 实现令牌撤销（Redis/数据库）
            await Task.CompletedTask;
        }

        private async Task RevokeAllTokensByUserAsync(Guid userId, string reason)
        {
            // TODO: 实现用户所有令牌撤销（Redis/数据库）
            await Task.CompletedTask;
        }
    }

    /// <summary>
    /// 存储的刷新令牌信息
    /// </summary>
    public class StoredRefreshToken
    {
        public Guid UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string ClientIP { get; set; } = string.Empty;
        public string? SessionId { get; set; }
        public string? DeviceId { get; set; }
        public DateTime ExpiresAt { get; set; }
        public bool IsLongTerm { get; set; }
    }
}