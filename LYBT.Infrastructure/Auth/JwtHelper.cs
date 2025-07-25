using Microsoft.IdentityModel.Tokens;
using System.ComponentModel;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace LYBT.Infrastructure.Auth {

    /// <summary>
    /// JWT工具类：生成Token
    /// </summary>
    [Description("JWT工具类")]
    public static class JwtHelper {

        /// <summary>
        /// 执行GenerateToken操作。
        /// </summary>
        /// <param name="userId">参数userId</param>
        /// <param name="userName">参数userName</param>
        /// <param name="options">参数options</param>
        /// <returns>返回值</returns>
        public static string GenerateToken(string userId, string userName, JwtOptions options) {
            var claims = new[] {
                new Claim(JwtRegisteredClaimNames.Sub, userId),
                new Claim(JwtRegisteredClaimNames.UniqueName, userName),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.Secret));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var expires = DateTime.UtcNow.AddMinutes(options.ExpireMinutes);

            var token = new JwtSecurityToken(
                issuer: options.Issuer,
                audience: options.Audience,
                claims: claims,
                expires: expires,
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}