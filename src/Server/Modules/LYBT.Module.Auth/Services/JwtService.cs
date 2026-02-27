using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using LYBT.Module.Auth.Interfaces;
using LYBT.Shared.Configuration.Options.Common;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace LYBT.Module.Auth.Services;

/// <summary>
/// 简化的JWT服务实现
/// 遵循适度设计原则，仅提供必要的认证功能
/// unify-configuration-system: 迁移到 LYBT.Shared.Configuration
/// </summary>
public class JwtService : IJwtService
{
    private readonly JwtOptions _jwtOptions;
    private readonly IConfiguration _configuration;
    private readonly JwtSecurityTokenHandler _tokenHandler;

    public JwtService(IOptions<JwtOptions> jwtOptions, IConfiguration configuration)
    {
        _jwtOptions = jwtOptions.Value ?? throw new ArgumentNullException(nameof(jwtOptions));
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
        // unify-configuration-system: 使用扁平化配置路径
        var secretKey = _jwtOptions.SecretKey;

        if (string.IsNullOrEmpty(secretKey))
        {
            throw new InvalidOperationException(
                "JWT SecretKey 未配置。请在 appsettings.json 中设置 Jwt:SecretKey 配置项。");
        }

        if (secretKey.Length < 32)
        {
            throw new ArgumentException(
                $"JWT SecretKey 长度不足,需至少 32 字符(当前 {secretKey.Length} 字符)。" +
                "这是安全基线要求,可使用以下命令生成符合要求的密钥:\n" +
                "PowerShell: [Convert]::ToBase64String([System.Security.Cryptography.RandomNumberGenerator]::GetBytes(64))");
        }

        // T5-P2-45: 生产环境禁止使用已知默认密钥
        var environment = _configuration["ASPNETCORE_ENVIRONMENT"]
            ?? Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
            ?? "Development";

        const string knownDefaultKey = "DefaultDevelopmentSecretKeyForJWTAuthentication_ShouldBeReplacedInProduction";
        if (environment.Equals("Production", StringComparison.OrdinalIgnoreCase) &&
            secretKey.Contains(knownDefaultKey, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "生产环境禁止使用默认 JWT 密钥。请使用以下命令生成安全密钥:\n" +
                "PowerShell: [Convert]::ToBase64String([System.Security.Cryptography.RandomNumberGenerator]::GetBytes(64))");
        }
    }

    /// <summary>
    /// 生成JWT访问令牌
    /// </summary>
    public string GenerateToken(string userId, string userName, UserRole role, string userType = "user")
    {
        if (string.IsNullOrEmpty(userId))
            throw new ArgumentException("用户ID不能为空", nameof(userId));

        if (string.IsNullOrEmpty(userName))
            throw new ArgumentException("用户名不能为空", nameof(userName));

        // unify-configuration-system: 使用强类型 JwtOptions
        var secretKey = _jwtOptions.SecretKey;
        if (string.IsNullOrEmpty(secretKey))
        {
            throw new InvalidOperationException("JWT SecretKey 配置未找到或为空。请检查 appsettings.json 中的 Jwt:SecretKey 配置。");
        }

        // 创建Claims
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim(ClaimTypes.Name, userName),
            new Claim(ClaimTypes.Role, role.ToString()),
            new Claim("user_type", userType), // Issue #1861: 用户类型区分SuperAdmin和User
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
        };

        // 创建签名密钥
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        // unify-configuration-system: 从强类型配置读取Token过期时间
        var expireMinutes = _jwtOptions.AccessTokenExpirationMinutes;
        var expires = DateTime.UtcNow.AddMinutes(expireMinutes);

        // 创建Token
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = expires,
            Issuer = _jwtOptions.Issuer,
            Audience = _jwtOptions.Audience,
            SigningCredentials = credentials
        };

        var token = _tokenHandler.CreateToken(tokenDescriptor);
        return _tokenHandler.WriteToken(token);
    }

    /// <summary>
    /// 生成JWT访问令牌（支持额外声明）
    /// </summary>
    public string GenerateToken(string userId, string userName, UserRole role, Dictionary<string, string> additionalClaims, string userType = "user")
    {
        if (string.IsNullOrEmpty(userId))
            throw new ArgumentException("用户ID不能为空", nameof(userId));

        if (string.IsNullOrEmpty(userName))
            throw new ArgumentException("用户名不能为空", nameof(userName));

        // unify-configuration-system: 使用强类型 JwtOptions
        var secretKey = _jwtOptions.SecretKey;
        if (string.IsNullOrEmpty(secretKey))
        {
            throw new InvalidOperationException("JWT SecretKey 配置未找到或为空。请检查 appsettings.json 中的 Jwt:SecretKey 配置。");
        }

        // 创建基础Claims
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim(ClaimTypes.Name, userName),
            new Claim(ClaimTypes.Role, role.ToString()),
            new Claim("user_type", userType), // Issue #1861: 用户类型区分SuperAdmin和User
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

        // unify-configuration-system: 从强类型配置读取Token过期时间
        var expireMinutes = _jwtOptions.AccessTokenExpirationMinutes;
        var expires = DateTime.UtcNow.AddMinutes(expireMinutes);

        // 创建Token
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = expires,
            Issuer = _jwtOptions.Issuer,
            Audience = _jwtOptions.Audience,
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
            // unify-configuration-system: 使用强类型 JwtOptions
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.SecretKey));

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = key,
                ValidateIssuer = true,
                ValidIssuer = _jwtOptions.Issuer,
                ValidateAudience = true,
                ValidAudience = _jwtOptions.Audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromSeconds(_jwtOptions.ClockSkewSeconds)
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
