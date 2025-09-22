using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using LYBT.Shared.Utilities.Security;
using LYBT.Infrastructure.Configuration.Options;
using LYBT.Infrastructure.Security;
using LYBT.Module.Auth.Interfaces;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace LYBT.Module.Auth.Services
{

    /// <summary>
    /// JWT认证服务实现 - 集成密钥管理服务
    /// </summary>
    public class JwtAuthenticationService : IJwtAuthenticationService
    {
        private readonly JwtOptions _jwtOptions;
        private readonly JwtSecurityTokenHandler _tokenHandler = new();
        private readonly ILogger<JwtAuthenticationService> _logger;
        private readonly IKeyManagementService? _keyManagementService;

        public JwtAuthenticationService(
            IOptions<JwtOptions> jwtOptions,
            ILogger<JwtAuthenticationService> logger,
            IKeyManagementService? keyManagementService = null)
        {
            _jwtOptions = jwtOptions?.Value ?? throw new ArgumentNullException(nameof(jwtOptions));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _keyManagementService = keyManagementService; // 可选注入，保持向后兼容
        }

        /// <summary>
        /// 生成JWT令牌（包含标准Claims）
        /// </summary>
        public string GenerateToken(string userId, string userName, UserRole role, bool rememberMe = false)
        {
            var claims = new List<Claim> {
                // JWT标准Claims
                new(JwtRegisteredClaimNames.Sub, userId),
                new(JwtRegisteredClaimNames.UniqueName, userName),
                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),

                // ClaimTypes标准Claims（兼容性）
                new(ClaimTypes.NameIdentifier, userId),
                new(ClaimTypes.Name, userName),

                // 角色声明（同时使用两种格式）
                new(ClaimTypes.Role, role.ToString()),
                new("role", role.ToString())  // JWT标准的role claim
            };

            // 获取密钥（优先使用密钥管理服务）
            var secret = GetCurrentSecret().GetAwaiter().GetResult();
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            // 根据"记住我"选项设置不同的过期时间
            var expireMinutes = rememberMe ? _jwtOptions.RememberMeExpireMinutes : _jwtOptions.ExpireMinutes;
            var expires = DateTime.UtcNow.AddMinutes(expireMinutes);

            var token = new JwtSecurityToken(
                issuer: _jwtOptions.Issuer,
                audience: _jwtOptions.Audience,
                claims: claims,
                expires: expires,
                signingCredentials: creds);

            return _tokenHandler.WriteToken(token);
        }

        /// <summary>
        /// 验证JWT令牌
        /// </summary>
        public ClaimsPrincipal? ValidateToken(string token)
        {
            try
            {
                // 获取所有有效的密钥
                var validSecrets = GetValidSecrets().GetAwaiter().GetResult();

                foreach (var secret in validSecrets)
                {
                    try
                    {
                        var validationParameters = new TokenValidationParameters
                        {
                            ValidateIssuer = true,
                            ValidateAudience = true,
                            ValidateLifetime = true,
                            ValidateIssuerSigningKey = true,
                            ValidIssuer = _jwtOptions.Issuer,
                            ValidAudience = _jwtOptions.Audience,
                            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret)),
                            ClockSkew = TimeSpan.Zero
                        };

                        var principal = _tokenHandler.ValidateToken(token, validationParameters, out _);
                        return principal; // 验证成功，返回
                    }
                    catch (SecurityTokenValidationException)
                    {
                        // 当前密钥验证失败，尝试下一个
                        continue;
                    }
                }

                _logger.LogWarning("JWT Token验证失败：所有密钥均无法验证");
                return null;
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning("JWT Token格式错误: {ErrorMessage}", ex.Message);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "JWT Token验证过程中发生未预期错误");
                return null;
            }
        }

        /// <summary>
        /// 刷新JWT令牌
        /// </summary>
        public string RefreshToken(string token)
        {
            var principal = ValidateToken(token);
            if (principal == null)
            {
                throw new SecurityTokenException("Invalid token");
            }

            var userId = principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value ?? string.Empty;
            var userName = principal.FindFirst(JwtRegisteredClaimNames.UniqueName)?.Value ?? string.Empty;
            var roleString = principal.FindFirst(ClaimTypes.Role)?.Value ?? RoleHelper.Roles.Doctor;
            if (Enum.TryParse<UserRole>(roleString, out var role))
            {
                return GenerateToken(userId, userName, role);
            }

            return GenerateToken(userId, userName, UserRole.Doctor);
        }

        /// <summary>
        /// 从令牌中提取用户信息
        /// </summary>
        public TokenUserInfo? ExtractUserInfo(string token)
        {
            try
            {
                var jsonToken = _tokenHandler.ReadJwtToken(token);

                var userId = jsonToken.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Sub)?.Value ?? string.Empty;
                var userName = jsonToken.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.UniqueName)?.Value ?? string.Empty;
                var roleString = jsonToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value ?? RoleHelper.Roles.Doctor;

                if (!Enum.TryParse<UserRole>(roleString, out var role))
                {
                    role = UserRole.Doctor;
                }

                return new TokenUserInfo
                {
                    UserId = userId,
                    Username = userName,
                    Role = role,
                    ExpiresAt = jsonToken.ValidTo
                };
            }
            catch (SecurityTokenMalformedException ex)
            {
                _logger.LogWarning("JWT Token格式错误，无法解析: {ErrorMessage}", ex.Message);
                return null;
            }
            catch (JsonException ex)
            {
                _logger.LogWarning("JWT Token JSON解析失败: {ErrorMessage}", ex.Message);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "JWT Token信息提取过程中发生未预期错误");
                return null;
            }
        }

        #region 私有辅助方法

        /// <summary>
        /// 获取当前JWT密钥
        /// </summary>
        private async Task<string> GetCurrentSecret()
        {
            if (_keyManagementService != null)
            {
                try
                {
                    return await _keyManagementService.GetCurrentJwtSecretAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "从密钥管理服务获取JWT密钥失败，使用配置文件中的密钥");
                }
            }

            return _jwtOptions.Secret;
        }

        /// <summary>
        /// 获取所有有效的JWT密钥（用于验证）
        /// </summary>
        private async Task<IEnumerable<string>> GetValidSecrets()
        {
            if (_keyManagementService != null)
            {
                try
                {
                    return await _keyManagementService.GetValidJwtSecretsAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "从密钥管理服务获取有效JWT密钥列表失败");
                }
            }

            return new[] { _jwtOptions.Secret };
        }

        #endregion
    }
}
