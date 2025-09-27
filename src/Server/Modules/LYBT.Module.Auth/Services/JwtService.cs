using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using LYBT.Infrastructure.Configuration.Options;
using LYBT.Module.Auth.Interfaces;
using LYBT.Shared.Models.Enums;

namespace LYBT.Module.Auth.Services;

/// <summary>
/// 简化的JWT服务实现
/// 遵循适度设计原则，仅提供必要的认证功能
/// </summary>
public class JwtService : IJwtService
{
    private readonly LybtOptions _options;
    private readonly JwtSecurityTokenHandler _tokenHandler;

    public JwtService(IOptions<LybtOptions> options)
    {
        _options = options.Value ?? throw new ArgumentNullException(nameof(options));
        _tokenHandler = new JwtSecurityTokenHandler();
    }

    /// <summary>
    /// 生成JWT访问令牌
    /// </summary>
    public string GenerateToken(string userId, string userName, UserRole role)
    {
        if (string.IsNullOrEmpty(userId))
            throw new ArgumentException("用户ID不能为空", nameof(userId));
        
        if (string.IsNullOrEmpty(userName))
            throw new ArgumentException("用户名不能为空", nameof(userName));

        var jwtConfig = _options.Authentication.Jwt;
        
        // 创建Claims
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim(ClaimTypes.Name, userName),
            new Claim(ClaimTypes.Role, role.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
        };

        // 创建签名密钥
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtConfig.SecretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        // 设置合理的过期时间（8小时，符合适度设计原则）
        var expirationHours = 8; // 简化配置，无需15分钟强制过期
        var expires = DateTime.UtcNow.AddHours(expirationHours);

        // 创建Token
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = expires,
            Issuer = jwtConfig.Issuer,
            Audience = jwtConfig.Audience,
            SigningCredentials = credentials
        };

        var token = _tokenHandler.CreateToken(tokenDescriptor);
        return _tokenHandler.WriteToken(token);
    }

    /// <summary>
    /// 验证JWT令牌并返回Claims主体
    /// </summary>
    public ClaimsPrincipal? ValidateToken(string token)
    {
        if (string.IsNullOrEmpty(token))
            return null;

        try
        {
            var jwtConfig = _options.Authentication.Jwt;
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtConfig.SecretKey));

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = key,
                ValidateIssuer = true,
                ValidIssuer = jwtConfig.Issuer,
                ValidateAudience = true,
                ValidAudience = jwtConfig.Audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromMinutes(5) // 5分钟时钟偏差容忍
            };

            var principal = _tokenHandler.ValidateToken(token, validationParameters, out _);
            return principal;
        }
        catch
        {
            // 验证失败，返回null
            return null;
        }
    }
}