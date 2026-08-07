using System;
using System.Text;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using LYBT.Entities.Users;
using LYBT.Infrastructure.Constants;
using LYBT.Shared.Configuration.Options.Server;
using System.Collections.Generic;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Options;

namespace LYBT.LocalWebAPI.Auth;

/// <summary>
/// 嵌入式 Web API 的本地 JWT 配置。
/// 简化为固定 HMAC-SHA256 密钥和 1 年有效期。
/// </summary>
public static class LocalJwtConfig
{
    private const int TokenExpirationDays = 365;
    private static string _secret = string.Empty;

    /// <summary>
    /// 令牌过期时间（天数）
    /// </summary>
    public static int ExpirationDays => TokenExpirationDays;

    /// <summary>
    /// 初始化密钥（从 Options 读取）
    /// </summary>
    public static void Initialize(LocalJwtOptions options)
    {
        _secret = options.SecretKey;
    }

    /// <summary>
    /// 配置 JWT 认证/授权服务。
    /// </summary>
    public static void ConfigureServices(IServiceCollection services, LocalJwtOptions jwtOptions)
    {
        Initialize(jwtOptions);
        var key = Encoding.UTF8.GetBytes(_secret);
        var tokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(key)
        };

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = tokenValidationParameters;
        });

        services.AddAuthorization(options =>
        {
            // 纯 Admin（业务管理）策略：不含 SuperAdmin（系统运维不碰业务数据）
            options.AddPolicy(PolicyConstants.AdminBusinessOnly, policy =>
                policy.RequireAuthenticatedUser()
                      .RequireRole(RoleConstants.Admin));

            options.AddPolicy(PolicyConstants.DoctorOrAdmin, policy =>
                policy.RequireAuthenticatedUser()
                      .RequireRole(RoleConstants.SuperAdmin, RoleConstants.Admin, RoleConstants.Doctor));

            // 仅 Doctor（打印、接诊等操作，2026-08-03 决策）
            options.AddPolicy(PolicyConstants.DoctorOnly, policy =>
                policy.RequireAuthenticatedUser()
                      .RequireRole(RoleConstants.Doctor));

            options.AddPolicy(PolicyConstants.AdminOrSuperAdmin, policy =>
                policy.RequireAuthenticatedUser()
                      .RequireRole(RoleConstants.Admin, RoleConstants.SuperAdmin));

            options.AddPolicy(PolicyConstants.DoctorOrReceptionist, policy =>
                policy.RequireAuthenticatedUser()
                      .RequireRole(RoleConstants.Doctor, RoleConstants.Receptionist));
        });
    }

    /// <summary>
    /// 为给定 Identity 用户生成 JWT。
    /// Subject：user.Id，Role 声明（第一个 Identity 角色），365 天有效期。
    /// </summary>
    public static string GenerateToken(ApplicationUser user, IList<string> roles)
    {
        var roleClaim = ParseRoleClaim(roles);

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.UserName ?? string.Empty),
            new Claim(ClaimTypes.Role, roleClaim),
            // Include standard JWT subject claim for the user id
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString())
        };

        if (user.IsSysAdmin)
            claims.Add(new Claim("IsSysAdmin", "true"));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: DateTime.UtcNow.AddDays(TokenExpirationDays),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static string ParseRoleClaim(IList<string> roles)
    {
        // Identity roles seeded by IdentitySeedData ("Doctor", "Admin", "SuperAdmin", "Receptionist")
        // map 1:1 to UserRole enum names and to RoleConstants policy role names.
        return roles.Count > 0 ? roles[0] : RoleConstants.Doctor;
    }
}
