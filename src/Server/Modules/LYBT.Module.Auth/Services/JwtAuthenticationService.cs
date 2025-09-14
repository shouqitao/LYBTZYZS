using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using LYBT.Infrastructure.Authorization;
using LYBT.Infrastructure.Configuration.Options;
using LYBT.Module.Auth.Interfaces;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace LYBT.Module.Auth.Services
{

    /// <summary>
    /// JWT认证服务实现
    /// </summary>
    public class JwtAuthenticationService(IOptions<JwtOptions> jwtOptions) : IJwtAuthenticationService
    {
        private readonly JwtOptions _jwtOptions = jwtOptions?.Value ?? throw new ArgumentNullException(nameof(jwtOptions));
        private readonly JwtSecurityTokenHandler _tokenHandler = new();

        /// <summary>
        /// 生成JWT令牌
        /// </summary>
        public string GenerateToken(string userId, string userName, UserRole role, bool rememberMe = false)
        {
            var claims = new List<Claim> {
                new(JwtRegisteredClaimNames.Sub, userId),
                new(JwtRegisteredClaimNames.UniqueName, userName),
                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
            };

            // 添加角色声明
            claims.Add(new Claim(ClaimTypes.Role, role.ToString()));

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.Secret));
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
                var validationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = _jwtOptions.Issuer,
                    ValidAudience = _jwtOptions.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.Secret)),
                    ClockSkew = TimeSpan.Zero
                };

                var principal = _tokenHandler.ValidateToken(token, validationParameters, out _);
                return principal;
            }
            catch (SecurityTokenValidationException ex)
            {
                _logger.LogWarning("JWT Token验证失败: {ErrorMessage}", ex.Message);
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
            var roleString = principal.FindFirst(ClaimTypes.Role)?.Value ?? RoleConstants.Doctor;
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
                var roleString = jsonToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value ?? RoleConstants.Doctor;

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
    }
}
