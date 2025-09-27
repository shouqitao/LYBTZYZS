using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using LYBT.Entities.Auth;
using LYBT.Entities.Users;
using LYBT.Infrastructure.Configuration.Options;
using LYBT.Infrastructure.Data;
using LYBT.Infrastructure.Security;
using LYBT.Module.Auth.Interfaces;
using AuthISecurityKeyService = LYBT.Module.Auth.Interfaces.ISecurityKeyService;
using LYBT.Module.Users.Interfaces;
using LYBT.Shared.Models.Contracts.Auth;
using LYBT.Shared.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace LYBT.Module.Auth.Services
{
    /// <summary>
    /// 增强的JWT服务 - 支持RefreshToken和安全密钥管理
    /// </summary>
    public class EnhancedJwtService : IJwtAuthenticationService
    {
        private readonly JwtOptions _jwtOptions;
        private readonly AuthISecurityKeyService _securityKeyService;
        private readonly AppDbContext _context;
        private readonly IUserService _userService;
        private readonly ILogger<EnhancedJwtService> _logger;
        private readonly JwtSecurityTokenHandler _tokenHandler = new();

        public EnhancedJwtService(
            IOptions<JwtOptions> jwtOptions,
            AuthISecurityKeyService securityKeyService,
            AppDbContext context,
            IUserService userService,
            ILogger<EnhancedJwtService> logger)
        {
            _jwtOptions = jwtOptions.Value;
            _securityKeyService = securityKeyService;
            _context = context;
            _userService = userService;
            _logger = logger;
        }

        /// <summary>
        /// 生成Token对（AccessToken + RefreshToken）
        /// </summary>
        public async Task<TokenPair> GenerateTokenPairAsync(
            string userId, 
            string userName, 
            UserRole role,
            string? clientIp = null,
            string? userAgent = null,
            string? deviceId = null)
        {
            try
            {
                var jti = Guid.NewGuid().ToString();
                
                // 生成AccessToken
                var accessToken = await GenerateAccessTokenAsync(userId, userName, role, jti);
                
                // 生成RefreshToken
                var refreshToken = await GenerateRefreshTokenAsync(
                    userId, jti, clientIp, userAgent, deviceId);

                return new TokenPair
                {
                    AccessToken = accessToken,
                    RefreshToken = refreshToken.Token,
                    TokenType = "Bearer",
                    AccessTokenExpires = DateTime.UtcNow.AddMinutes(_jwtOptions.ExpireMinutes),
                    RefreshTokenExpires = refreshToken.ExpiresAt
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "生成Token对失败");
                throw;
            }
        }

        /// <summary>
        /// 生成AccessToken
        /// </summary>
        private async Task<string> GenerateAccessTokenAsync(
            string userId, 
            string userName, 
            UserRole role, 
            string jti)
        {
            var claims = new List<Claim>
            {
                // JWT标准Claims
                new(JwtRegisteredClaimNames.Sub, userId),
                new(JwtRegisteredClaimNames.UniqueName, userName),
                new(JwtRegisteredClaimNames.Jti, jti),
                new(JwtRegisteredClaimNames.Iat, 
                    new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds().ToString(), 
                    ClaimValueTypes.Integer64),
                
                // 自定义Claims
                new(ClaimTypes.NameIdentifier, userId),
                new(ClaimTypes.Name, userName),
                new(ClaimTypes.Role, role.ToString()),
                new("role_id", ((int)role).ToString()),
                
                // 安全相关Claims
                new("key_version", await _securityKeyService.GetCurrentKeyIdAsync())
            };

            // 获取签名密钥
            var signingKey = await _securityKeyService.GetCurrentKeyAsync();
            var signingCredentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

            // 设置Token过期时间（短生命周期：15分钟）
            var expireMinutes = Math.Min(_jwtOptions.ExpireMinutes, 15); // 强制最长15分钟
            var expires = DateTime.UtcNow.AddMinutes(expireMinutes);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = expires,
                NotBefore = DateTime.UtcNow,
                Issuer = _jwtOptions.Issuer,
                Audience = _jwtOptions.Audience,
                SigningCredentials = signingCredentials
            };

            var token = _tokenHandler.CreateToken(tokenDescriptor);
            var tokenString = _tokenHandler.WriteToken(token);

            _logger.LogInformation(
                "生成AccessToken成功 - 用户：{UserName}，过期时间：{ExpireMinutes}分钟",
                userName, expireMinutes);

            return tokenString;
        }

        /// <summary>
        /// 生成RefreshToken
        /// </summary>
        private async Task<RefreshToken> GenerateRefreshTokenAsync(
            string userId,
            string jti,
            string? clientIp,
            string? userAgent,
            string? deviceId)
        {
            // 生成安全的随机Token
            var tokenBytes = new byte[64];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(tokenBytes);
            }
            var tokenValue = Convert.ToBase64String(tokenBytes);

            // 创建RefreshToken实体
            var refreshToken = new RefreshToken
            {
                Id = Guid.NewGuid(),
                Token = tokenValue,
                UserId = Guid.Parse(userId),
                Jti = jti,
                ExpiresAt = DateTime.UtcNow.AddDays(_jwtOptions.RememberMeExpireMinutes / 1440.0), // 转换为天
                ClientIp = clientIp,
                UserAgent = userAgent,
                DeviceId = deviceId,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = Guid.Parse(userId)
            };

            // 保存到数据库
            _context.Set<RefreshToken>().Add(refreshToken);
            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "生成RefreshToken成功 - 用户：{UserId}，设备：{DeviceId}",
                userId, deviceId ?? "Unknown");

            return refreshToken;
        }

        /// <summary>
        /// 刷新Token
        /// </summary>
        public async Task<TokenPair?> RefreshTokenAsync(
            string refreshTokenValue,
            string? clientIp = null,
            string? userAgent = null)
        {
            try
            {
                // 查找RefreshToken
                var refreshToken = await _context.Set<RefreshToken>()
                    .FirstOrDefaultAsync(rt => rt.Token == refreshTokenValue);

                if (refreshToken == null)
                {
                    _logger.LogWarning("RefreshToken不存在：{Token}", refreshTokenValue.Substring(0, 10) + "...");
                    return null;
                }

                // 验证Token有效性
                if (!refreshToken.IsValid())
                {
                    _logger.LogWarning("RefreshToken无效或已过期：{TokenId}", refreshToken.Id);
                    return null;
                }

                // 获取用户信息
                var user = await _userService.GetByIdAsync(refreshToken.UserId);
                if (!user.IsSuccess || user.Data == null)
                {
                    _logger.LogWarning("RefreshToken关联的用户不存在：{UserId}", refreshToken.UserId);
                    return null;
                }

                // 记录使用
                refreshToken.RecordUsage();

                // 生成新的Token对
                var newTokenPair = await GenerateTokenPairAsync(
                    user.Data.Id.ToString(),
                    user.Data.UserName,
                    (UserRole)user.Data.Role,
                    clientIp ?? refreshToken.ClientIp,
                    userAgent ?? refreshToken.UserAgent,
                    refreshToken.DeviceId);

                // 可选：撤销旧的RefreshToken（单设备登录策略）
                // refreshToken.Revoke("Token rotated", "System");
                
                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "Token刷新成功 - 用户：{UserName}",
                    user.Data.UserName);

                return newTokenPair;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "刷新Token失败");
                return null;
            }
        }

        /// <summary>
        /// 撤销Token
        /// </summary>
        public async Task<bool> RevokeTokenAsync(
            string token,
            string reason,
            string? revokedBy = null)
        {
            try
            {
                // 尝试作为RefreshToken撤销
                var refreshToken = await _context.Set<RefreshToken>()
                    .FirstOrDefaultAsync(rt => rt.Token == token);

                if (refreshToken != null)
                {
                    refreshToken.Revoke(reason, revokedBy);
                    await _context.SaveChangesAsync();
                    
                    _logger.LogInformation(
                        "RefreshToken已撤销 - TokenId：{TokenId}，原因：{Reason}",
                        refreshToken.Id, reason);
                    
                    return true;
                }

                // 尝试解析为AccessToken并通过JTI撤销相关的RefreshToken
                try
                {
                    var tokenHandler = new JwtSecurityTokenHandler();
                    var jsonToken = tokenHandler.ReadJwtToken(token);
                    var jti = jsonToken.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Jti)?.Value;

                    if (!string.IsNullOrEmpty(jti))
                    {
                        var relatedToken = await _context.Set<RefreshToken>()
                            .FirstOrDefaultAsync(rt => rt.Jti == jti);

                        if (relatedToken != null)
                        {
                            relatedToken.Revoke(reason, revokedBy);
                            await _context.SaveChangesAsync();
                            
                            _logger.LogInformation(
                                "通过JTI撤销RefreshToken - JTI：{Jti}，原因：{Reason}",
                                jti, reason);
                            
                            return true;
                        }
                    }
                }
                catch
                {
                    // 不是有效的JWT，忽略
                }

                _logger.LogWarning("未找到要撤销的Token");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "撤销Token失败");
                return false;
            }
        }

        /// <summary>
        /// 撤销用户的所有Token
        /// </summary>
        public async Task<int> RevokeAllUserTokensAsync(
            Guid userId,
            string reason,
            string? revokedBy = null)
        {
            try
            {
                var tokens = await _context.Set<RefreshToken>()
                    .Where(rt => rt.UserId == userId && !rt.IsRevoked)
                    .ToListAsync();

                foreach (var token in tokens)
                {
                    token.Revoke(reason, revokedBy);
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "撤销用户所有Token - 用户：{UserId}，数量：{Count}，原因：{Reason}",
                    userId, tokens.Count, reason);

                return tokens.Count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "撤销用户所有Token失败");
                return 0;
            }
        }

        /// <summary>
        /// 验证Token（使用多密钥支持）
        /// </summary>
        public async Task<ClaimsPrincipal?> ValidateTokenAsync(string token)
        {
            try
            {
                // 获取所有验证密钥（包括历史密钥）
                var validationKeys = await _securityKeyService.GetAllKeysAsync();

                var validationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKeys = validationKeys,
                    ValidateIssuer = true,
                    ValidIssuer = _jwtOptions.Issuer,
                    ValidateAudience = true,
                    ValidAudience = _jwtOptions.Audience,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromSeconds(_jwtOptions.ClockSkewSeconds)
                };

                var principal = _tokenHandler.ValidateToken(token, validationParameters, out var validatedToken);

                // 检查Token是否在黑名单中
                if (validatedToken is JwtSecurityToken jwtToken)
                {
                    var jti = jwtToken.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Jti)?.Value;
                    if (!string.IsNullOrEmpty(jti))
                    {
                        var refreshToken = await _context.Set<RefreshToken>()
                            .FirstOrDefaultAsync(rt => rt.Jti == jti);

                        if (refreshToken?.IsRevoked == true)
                        {
                            _logger.LogWarning("Token已被撤销 - JTI：{Jti}", jti);
                            return null;
                        }
                    }
                }

                return principal;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "验证Token失败");
                return null;
            }
        }

        // 实现IJwtAuthenticationService接口的方法
        public string GenerateToken(string userId, string userName, UserRole role, bool rememberMe = false)
        {
            // 同步版本，调用异步方法
            var tokenPair = GenerateTokenPairAsync(userId, userName, role).GetAwaiter().GetResult();
            return tokenPair.AccessToken;
        }

        public ClaimsPrincipal? ValidateToken(string token)
        {
            return ValidateTokenAsync(token).GetAwaiter().GetResult();
        }

        public string? GetUserIdFromToken(string token)
        {
            try
            {
                var principal = ValidateToken(token);
                return principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            }
            catch
            {
                return null;
            }
        }

        public DateTime GetTokenExpiration(string token)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var jsonToken = tokenHandler.ReadJwtToken(token);
                return jsonToken.ValidTo;
            }
            catch
            {
                return DateTime.MinValue;
            }
        }

        public bool IsTokenExpired(string token)
        {
            var expiration = GetTokenExpiration(token);
            return expiration <= DateTime.UtcNow;
        }

        /// <summary>
        /// 刷新Token（同步方法，为了兼容接口）
        /// </summary>
        public string RefreshToken(string token)
        {
            // 同步调用异步方法
            var result = Task.Run(async () => await RefreshTokenAsync(token)).GetAwaiter().GetResult();
            return result?.AccessToken ?? string.Empty;
        }

        /// <summary>
        /// 从令牌中提取用户信息
        /// </summary>
        public TokenUserInfo? ExtractUserInfo(string token)
        {
            try
            {
                var principal = ValidateToken(token);
                if (principal == null) return null;

                var userIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var userNameClaim = principal.FindFirst(ClaimTypes.Name)?.Value;
                var roleClaim = principal.FindFirst(ClaimTypes.Role)?.Value;

                if (string.IsNullOrEmpty(userIdClaim) || string.IsNullOrEmpty(userNameClaim))
                    return null;

                return new TokenUserInfo
                {
                    UserId = userIdClaim,
                    Username = userNameClaim,
                    Role = Enum.TryParse<UserRole>(roleClaim, out var role) ? role : UserRole.Doctor
                };
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "从Token提取用户信息失败");
                return null;
            }
        }

        /// <summary>
        /// 生成安全的刷新令牌 - 简化实现
        /// </summary>
        public SecureTokenRefreshResult SecureRefreshToken(
            string token,
            string? deviceFingerprint = null,
            string? ipAddress = null,
            string? userAgent = null)
        {
            try
            {
                // 简化版实现：验证旧token，生成新token对
                var principal = ValidateToken(token);
                if (principal == null)
                {
                    return new SecureTokenRefreshResult
                    {
                        IsSuccess = false,
                        ErrorMessage = "Invalid token"
                    };
                }

                var userId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var userName = principal.FindFirst(ClaimTypes.Name)?.Value;
                var roleClaim = principal.FindFirst(ClaimTypes.Role)?.Value;

                if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(userName))
                {
                    return new SecureTokenRefreshResult
                    {
                        IsSuccess = false,
                        ErrorMessage = "Invalid token claims"
                    };
                }

                var role = Enum.TryParse<UserRole>(roleClaim, out var userRole) ? userRole : UserRole.Doctor;

                // 生成新的token对
                var tokenPair = GenerateTokenPairAsync(userId, userName, role).GetAwaiter().GetResult();

                return new SecureTokenRefreshResult
                {
                    IsSuccess = true,
                    AccessToken = tokenPair.AccessToken,
                    RefreshToken = tokenPair.RefreshToken,
                    ExpiresAt = DateTime.UtcNow.AddMinutes(_jwtOptions.ExpireMinutes)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "安全刷新令牌失败");
                return new SecureTokenRefreshResult
                {
                    IsSuccess = false,
                    ErrorMessage = "Refresh token failed"
                };
            }
        }
    }
}