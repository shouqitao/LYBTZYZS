using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace LYBT.Desktop.Foundation.Security
{
    /// <summary>
    /// 本地Token验证器 - 实现客户端JWT自验证
    /// </summary>
    /// <remarks>
    /// Issue #1863: Token认证安全重构 - 实现客户端JWT自验证
    ///
    /// 设计要点：
    /// 1. 使用JwtSecurityTokenHandler进行本地验证
    /// 2. 验证内容：签名、Issuer、Audience、Expiration、必需Claims
    /// 3. 移除Server API依赖（/api/v1/auth/validate POST端点）
    /// 4. 配置来源：appsettings.json中的Lybt:Jwt配置
    /// </remarks>
    public class LocalTokenValidator : ITokenValidator
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<LocalTokenValidator> _logger;
        private readonly JwtSecurityTokenHandler _tokenHandler;

        public LocalTokenValidator(
            IConfiguration configuration,
            ILogger<LocalTokenValidator> logger)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _tokenHandler = new JwtSecurityTokenHandler();
        }

        /// <summary>
        /// 验证JWT Token
        /// </summary>
        public async Task<TokenValidationResult> ValidateTokenAsync(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return new TokenValidationResult
                {
                    IsValid = false,
                    ErrorMessage = "Token不能为空"
                };
            }

            try
            {
                // 读取JWT配置
                var secretKey = _configuration["Lybt:Jwt:SecretKey"];
                var issuer = _configuration["Lybt:Jwt:Issuer"];
                var audience = _configuration["Lybt:Jwt:Audience"];
                var clockSkewSeconds = _configuration.GetValue<int?>("Lybt:Jwt:ClockSkewSeconds") ?? 300;

                if (string.IsNullOrEmpty(secretKey))
                {
                    _logger.LogError("JWT SecretKey配置未找到或为空");
                    return new TokenValidationResult
                    {
                        IsValid = false,
                        ErrorMessage = "JWT配置错误：SecretKey未配置"
                    };
                }

                if (secretKey.Length < 32)
                {
                    _logger.LogWarning("JWT SecretKey长度不足32字符，存在安全风险");
                }

                // 配置验证参数
                var validationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = issuer,
                    ValidAudience = audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
                    ClockSkew = TimeSpan.FromSeconds(clockSkewSeconds),
                    RequireExpirationTime = true,
                    RequireSignedTokens = true
                };

                // 验证Token
                var principal = _tokenHandler.ValidateToken(token, validationParameters, out var validatedToken);

                // 提取用户信息
                var userInfo = ExtractUserInfo(principal);

                if (userInfo == null)
                {
                    _logger.LogWarning("Token验证成功但无法提取用户信息");
                    return new TokenValidationResult
                    {
                        IsValid = false,
                        ErrorMessage = "Token中缺少必需的用户信息"
                    };
                }

                _logger.LogDebug("Token验证成功，用户: {UserName}, 类型: {UserType}",
                    userInfo.UserName, userInfo.UserType);

                return new TokenValidationResult
                {
                    IsValid = true,
                    UserInfo = userInfo
                };
            }
            catch (SecurityTokenExpiredException ex)
            {
                _logger.LogWarning(ex, "Token已过期");
                return new TokenValidationResult
                {
                    IsValid = false,
                    ErrorMessage = "Token已过期"
                };
            }
            catch (SecurityTokenInvalidSignatureException ex)
            {
                _logger.LogError(ex, "Token签名无效");
                return new TokenValidationResult
                {
                    IsValid = false,
                    ErrorMessage = "Token签名无效"
                };
            }
            catch (SecurityTokenInvalidIssuerException ex)
            {
                _logger.LogError(ex, "Token发行者无效");
                return new TokenValidationResult
                {
                    IsValid = false,
                    ErrorMessage = "Token发行者无效"
                };
            }
            catch (SecurityTokenInvalidAudienceException ex)
            {
                _logger.LogError(ex, "Token接收者无效");
                return new TokenValidationResult
                {
                    IsValid = false,
                    ErrorMessage = "Token接收者无效"
                };
            }
            catch (SecurityTokenException ex)
            {
                _logger.LogError(ex, "Token验证失败");
                return new TokenValidationResult
                {
                    IsValid = false,
                    ErrorMessage = $"Token验证失败: {ex.Message}"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Token验证过程发生未知错误");
                return new TokenValidationResult
                {
                    IsValid = false,
                    ErrorMessage = $"验证过程发生错误: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// 验证Token并提取用户信息
        /// </summary>
        public async Task<TokenUserInfo?> ValidateAndGetUserInfoAsync(string token)
        {
            var result = await ValidateTokenAsync(token);
            return result.IsValid ? result.UserInfo : null;
        }

        /// <summary>
        /// 从ClaimsPrincipal中提取用户信息
        /// </summary>
        private TokenUserInfo? ExtractUserInfo(ClaimsPrincipal principal)
        {
            try
            {
                // 提取必需的Claims
                var userIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var userNameClaim = principal.FindFirst(ClaimTypes.Name)?.Value;
                var roleClaim = principal.FindFirst(ClaimTypes.Role)?.Value;
                var userTypeClaim = principal.FindFirst("user_type")?.Value;

                if (string.IsNullOrEmpty(userIdClaim) || string.IsNullOrEmpty(userNameClaim))
                {
                    _logger.LogWarning("Token中缺少必需的Claims: UserId或UserName");
                    return null;
                }

                if (!Guid.TryParse(userIdClaim, out var userId))
                {
                    _logger.LogWarning("UserId格式无效: {UserIdClaim}", userIdClaim);
                    return null;
                }

                return new TokenUserInfo
                {
                    UserId = userId,
                    UserName = userNameClaim,
                    Role = roleClaim ?? string.Empty,
                    UserType = userTypeClaim ?? "user"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "提取用户信息时发生错误");
                return null;
            }
        }
    }
}
