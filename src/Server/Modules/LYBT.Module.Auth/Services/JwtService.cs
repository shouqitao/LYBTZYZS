using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using LYBT.Infrastructure.Configuration.Options;
using LYBT.Module.Auth.Interfaces;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace LYBT.Module.Auth.Services;

/// <summary>
/// 简化的JWT服务实现
/// 遵循适度设计原则，仅提供必要的认证功能
/// </summary>
public class JwtService : IJwtService
{
    private readonly LybtOptions _options;
    private readonly IConfiguration _configuration;
    private readonly JwtSecurityTokenHandler _tokenHandler;

    public JwtService(IOptions<LybtOptions> options, IConfiguration configuration)
    {
        _options = options.Value ?? throw new ArgumentNullException(nameof(options));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _tokenHandler = new JwtSecurityTokenHandler();

        // 启动时验证 JWT 密钥强度(方案A:最小加固)
        ValidateSecretKeyStrength();
    }

    /// <summary>
    /// 验证 JWT 密钥强度,确保符合安全基线要求
    /// </summary>
    private void ValidateSecretKeyStrength()
    {
        var secretKey = _configuration["Lybt:Authentication:Jwt:SecretKey"];

        if (string.IsNullOrEmpty(secretKey))
        {
            throw new InvalidOperationException(
                "JWT SecretKey 未配置。请在 appsettings.json 中设置 Lybt:Authentication:Jwt:SecretKey 配置项。");
        }

        if (secretKey.Length < 32)
        {
            throw new ArgumentException(
                $"JWT SecretKey 长度不足,需至少 32 字符(当前 {secretKey.Length} 字符)。" +
                "这是安全基线要求,可使用以下命令生成符合要求的密钥:\n" +
                "PowerShell: [Convert]::ToBase64String([System.Security.Cryptography.RandomNumberGenerator]::GetBytes(64))");
        }
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

        // 直接从配置读取 JWT 密钥（解决配置绑定问题）
        // Issue #1761 Phase 3.1: Authentication.Jwt → Jwt（完全扁平化）
        var secretKey = _configuration["Lybt:Jwt:SecretKey"];
        if (string.IsNullOrEmpty(secretKey))
        {
            throw new InvalidOperationException("JWT SecretKey 配置未找到或为空。请检查 appsettings.json 中的 Lybt:Jwt:SecretKey 配置。");
        }

        var jwtConfig = _options.Jwt;

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
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
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
    /// 生成JWT访问令牌（支持额外声明）
    /// </summary>
    public string GenerateToken(string userId, string userName, UserRole role, Dictionary<string, string> additionalClaims)
    {
        if (string.IsNullOrEmpty(userId))
            throw new ArgumentException("用户ID不能为空", nameof(userId));

        if (string.IsNullOrEmpty(userName))
            throw new ArgumentException("用户名不能为空", nameof(userName));

        // 直接从配置读取 JWT 密钥（解决配置绑定问题）
        // Issue #1761 Phase 3.1: Authentication.Jwt → Jwt（完全扁平化）
        var secretKey = _configuration["Lybt:Jwt:SecretKey"];
        if (string.IsNullOrEmpty(secretKey))
        {
            throw new InvalidOperationException("JWT SecretKey 配置未找到或为空。请检查 appsettings.json 中的 Lybt:Jwt:SecretKey 配置。");
        }

        var jwtConfig = _options.Jwt;

        // 创建基础Claims
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim(ClaimTypes.Name, userName),
            new Claim(ClaimTypes.Role, role.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
        };

        // 添加额外的Claims
        if (additionalClaims != null)
        {
            foreach (var claim in additionalClaims)
            {
                claims.Add(new Claim(claim.Key, claim.Value));
            }
        }

        // 创建签名密钥
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
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
            // Issue #1761 Phase 3.1: Authentication.Jwt → Jwt（完全扁平化）
            var jwtConfig = _options.Jwt;
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
