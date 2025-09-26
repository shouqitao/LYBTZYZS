using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using LYBT.Entities.Auth;
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
            // 确保参数不为null，避免Claim构造函数异常
            userId = userId ?? string.Empty;
            userName = userName ?? string.Empty;

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
                            ClockSkew = TimeSpan.FromSeconds(_jwtOptions.ClockSkewSeconds),
                            NameClaimType = JwtRegisteredClaimNames.UniqueName,
                            RoleClaimType = ClaimTypes.Role
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

            // 尝试多种方式提取userId
            var userId = principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value 
                         ?? principal.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                         ?? string.Empty;

            // 尝试多种方式提取userName
            var userName = principal.FindFirst(JwtRegisteredClaimNames.UniqueName)?.Value
                          ?? principal.FindFirst(ClaimTypes.Name)?.Value
                          ?? string.Empty;

            var roleString = principal.FindFirst(ClaimTypes.Role)?.Value 
                            ?? principal.FindFirst("role")?.Value 
                            ?? RoleHelper.Roles.Doctor;

            if (Enum.TryParse<UserRole>(roleString, out var role))
            {
                return GenerateToken(userId, userName, role);
            }

            return GenerateToken(userId, userName, UserRole.Doctor);
        }

        /// <summary>
        /// 安全刷新JWT令牌（包含设备验证）
        /// </summary>
        public SecureTokenRefreshResult SecureRefreshToken(
            string token, 
            string? deviceFingerprint = null, 
            string? ipAddress = null, 
            string? userAgent = null)
        {
            try
            {
                _logger.LogInformation("开始安全令牌刷新，IP: {IpAddress}, DeviceFingerprint: {DeviceFingerprint}", 
                    ipAddress ?? "未知", deviceFingerprint ?? "未知");

                // 验证当前令牌
                var principal = ValidateToken(token);
                if (principal == null)
                {
                    _logger.LogWarning("令牌刷新失败：无效的令牌");
                    return SecureTokenRefreshResult.Failure("无效的令牌");
                }

                // 提取用户信息
                var userId = ExtractUserId(principal);
                var userName = ExtractUserName(principal);
                var role = ExtractUserRole(principal);

                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogWarning("令牌刷新失败：无法提取用户ID");
                    return SecureTokenRefreshResult.Failure("令牌中缺少用户信息");
                }

                // 模拟RefreshToken安全验证（在实际实现中应该从数据库获取）
                var refreshTokenValidation = ValidateRefreshTokenSecurity(
                    userId, deviceFingerprint, ipAddress, userAgent);

                // 根据安全级别决定处理策略
                if (!refreshTokenValidation.IsValid)
                {
                    _logger.LogWarning("令牌刷新失败：安全验证未通过 - {Reasons}", 
                        string.Join(", ", refreshTokenValidation.Reasons));
                    return SecureTokenRefreshResult.Failure("安全验证未通过：" + string.Join(", ", refreshTokenValidation.Reasons));
                }

                // 生成新的访问令牌
                var newAccessToken = GenerateToken(userId, userName, role);
                var newRefreshToken = GenerateRefreshTokenString();
                var expiresAt = DateTime.UtcNow.AddMinutes(_jwtOptions.ExpireMinutes);

                var result = SecureTokenRefreshResult.Success(
                    newAccessToken, newRefreshToken, expiresAt, refreshTokenValidation.SecurityLevel);

                // 添加安全警告
                if (refreshTokenValidation.Reasons.Any())
                {
                    result.SecurityWarnings.AddRange(refreshTokenValidation.Reasons);
                }

                // 设置是否需要额外验证
                result.RequiresAdditionalVerification = refreshTokenValidation.RequiresAdditionalVerification;

                _logger.LogInformation(
                    "令牌刷新成功，用户: {UserId}, 安全级别: {SecurityLevel}, 警告数量: {WarningCount}", 
                    userId, refreshTokenValidation.SecurityLevel, result.SecurityWarnings.Count);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "令牌安全刷新过程中发生未预期错误");
                return SecureTokenRefreshResult.Failure("令牌刷新过程中发生系统错误");
            }
        }

        /// <summary>
        /// 验证RefreshToken的安全性
        /// </summary>
        private TokenSecurityValidationResult ValidateRefreshTokenSecurity(
            string userId, 
            string? deviceFingerprint, 
            string? ipAddress, 
            string? userAgent)
        {
            // 创建模拟的RefreshToken实体进行验证
            var mockRefreshToken = new RefreshToken
            {
                UserId = Guid.Parse(userId),
                DeviceFingerprint = deviceFingerprint,
                OriginalIpAddress = ipAddress,
                UserAgent = userAgent,
                IsTrustedDevice = false, // 在实际实现中应该从数据库读取
                UsageCount = 1,
                LastUsedAt = DateTime.UtcNow.AddMinutes(-5),
                CreatedAt = DateTime.UtcNow.AddDays(-1),
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                IsUsed = false,
                IsRevoked = false
            };

            return mockRefreshToken.ValidateDeviceSecurity(deviceFingerprint, ipAddress, userAgent);
        }

        /// <summary>
        /// 生成RefreshToken字符串
        /// </summary>
        private string GenerateRefreshTokenString()
        {
            var randomBytes = new byte[64]; // 512位
            using (var rng = System.Security.Cryptography.RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomBytes);
            }
            return Convert.ToBase64String(randomBytes);
        }

        /// <summary>
        /// 从Claims中提取用户ID
        /// </summary>
        private string ExtractUserId(ClaimsPrincipal principal)
        {
            return principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                   ?? principal.FindFirst(ClaimTypes.NameIdentifier)?.Value
                   ?? string.Empty;
        }

        /// <summary>
        /// 从Claims中提取用户名
        /// </summary>
        private string ExtractUserName(ClaimsPrincipal principal)
        {
            return principal.FindFirst(JwtRegisteredClaimNames.UniqueName)?.Value
                   ?? principal.FindFirst(ClaimTypes.Name)?.Value
                   ?? string.Empty;
        }

        /// <summary>
        /// 从Claims中提取用户角色
        /// </summary>
        private UserRole ExtractUserRole(ClaimsPrincipal principal)
        {
            var roleString = principal.FindFirst(ClaimTypes.Role)?.Value
                            ?? principal.FindFirst("role")?.Value
                            ?? RoleHelper.Roles.Doctor;

            return Enum.TryParse<UserRole>(roleString, out var role) ? role : UserRole.Doctor;
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
        private Task<string> GetCurrentSecret()
        {
            if (_keyManagementService != null)
            {
                try
                {
                    // 简化实现：直接使用配置中的密钥
                    // 在实际生产环境中，这里应该从安全存储中获取当前密钥
                    return Task.FromResult(_jwtOptions.Secret);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "从密钥管理服务获取JWT密钥失败，使用配置文件中的密钥");
                }
            }

            return Task.FromResult(_jwtOptions.Secret);
        }

        /// <summary>
        /// 获取所有有效的JWT密钥（用于验证）
        /// </summary>
        private Task<IEnumerable<string>> GetValidSecrets()
        {
            if (_keyManagementService != null)
            {
                try
                {
                    // 简化实现：返回配置中的密钥
                    // 在实际生产环境中，这里应该返回所有有效的密钥列表
                    return Task.FromResult<IEnumerable<string>>(new[] { _jwtOptions.Secret });
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "从密钥管理服务获取有效JWT密钥列表失败");
                }
            }

            return Task.FromResult<IEnumerable<string>>(new[] { _jwtOptions.Secret });
        }

        #endregion
    }
}
