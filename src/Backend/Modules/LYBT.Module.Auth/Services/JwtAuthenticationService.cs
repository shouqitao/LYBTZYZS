using LYBT.Infrastructure.Configuration.Options;
using LYBT.Module.Auth.Interfaces;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace LYBT.Module.Auth.Services
{

    /// <summary>
    /// JWT认证服务实现
    /// </summary>
    public class JwtAuthenticationService : IJwtAuthenticationService
    {
        private readonly JwtOptions _jwtOptions;
        private readonly JwtSecurityTokenHandler _tokenHandler;

        public JwtAuthenticationService(IOptions<JwtOptions> jwtOptions)
        {
            _jwtOptions = jwtOptions.Value;
            _tokenHandler = new JwtSecurityTokenHandler();
        }

        /// <summary>
        /// 生成JWT令牌
        /// </summary>
        public string GenerateToken(string userId, string userName, IEnumerable<string> roles, bool rememberMe = false)
        {
            var claims = new List<Claim> {
                new(JwtRegisteredClaimNames.Sub, userId),
                new(JwtRegisteredClaimNames.UniqueName, userName),
                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
            };

            // 添加角色声明
            claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

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
                signingCredentials: creds
            );

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
            catch
            {
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
            var roles = principal.FindAll(ClaimTypes.Role).Select(c => c.Value);

            return GenerateToken(userId, userName, roles);
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
                var roles = jsonToken.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value);

                return new TokenUserInfo
                {
                    UserId = userId,
                    UserName = userName,
                    Roles = roles,
                    ExpiresAt = jsonToken.ValidTo
                };
            }
            catch
            {
                return null;
            }
        }
    }
}