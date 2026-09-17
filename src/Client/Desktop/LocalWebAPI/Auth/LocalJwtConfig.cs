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
using LYBT.Infrastructure.Services.CrossModule;
using LYBT.Shared.Configuration.Options.Server;
using LYBT.Shared.Models.Enums;
using System.Collections.Generic;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Options;

namespace LYBT.LocalWebAPI.Auth;

/// <summary>
/// 嵌入式 Web API 的本地 JWT 配置。
/// 简化为固定 HMAC-SHA256 密钥和 12 小时有效期（R-19：单机本地模式一个工作日足够，原 365 天过长）。
/// </summary>
public static class LocalJwtConfig
{
    private const int TokenExpirationHours = 12;
    private static string _secret = string.Empty;
    private static string _issuer = "LYBT-LocalWebAPI";
    private static string _audience = "LYBT-Desktop";

    /// <summary>
    /// 令牌过期时间（小时）
    /// </summary>
    public static int ExpirationHours => TokenExpirationHours;

    /// <summary>
    /// 初始化密钥与 Issuer/Audience（从 Options 读取）
    /// </summary>
    public static void Initialize(LocalJwtOptions options)
    {
        _secret = options.SecretKey;
        _issuer = string.IsNullOrWhiteSpace(options.Issuer) ? "LYBT-LocalWebAPI" : options.Issuer;
        _audience = string.IsNullOrWhiteSpace(options.Audience) ? "LYBT-Desktop" : options.Audience;
    }

    /// <summary>获取签名密钥（T4: 供令牌验签使用——本地 refresh 原只解析不验签，任意伪造 JWT 可换令牌）</summary>
    public static SymmetricSecurityKey GetSigningKey()
        => new(Encoding.UTF8.GetBytes(_secret));

    /// <summary>
    /// 配置 JWT 认证/授权服务。
    /// </summary>
    public static void ConfigureServices(IServiceCollection services, LocalJwtOptions jwtOptions)
    {
        Initialize(jwtOptions);
        var key = Encoding.UTF8.GetBytes(_secret);
        var tokenValidationParameters = new TokenValidationParameters
        {
            // X-2: 与 Remote JWT 对齐，校验 Issuer/Audience
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = _issuer,
            ValidAudience = _audience,
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

            // P3 (US-USER-010): 已禁用/已删除用户令牌拒绝（令牌有效期内的状态拦截）
            // 与 Remote AuthenticationServiceCollectionExtensions.OnTokenValidated 对齐
            options.Events = new JwtBearerEvents
            {
                OnTokenValidated = async context =>
                {
                    var userIdClaim = context.Principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                    if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
                    {
                        context.Fail("令牌缺少用户标识");
                        return;
                    }

                    var userService = context.HttpContext.RequestServices.GetService<IUserCrossModuleService>();
                    var user = userService != null
                        ? await userService.GetUserBasicInfoAsync(userId, context.HttpContext.RequestAborted)
                        : null;
                    if (user == null || user.Status != CommonStatus.Enabled)
                    {
                        context.Fail("用户已被禁用或不存在");
                    }
                }
            };
        });

        services.AddAuthorization(options =>
        {
            // 默认拒绝：未显式标注 [Authorize]/[AllowAnonymous] 的端点一律要求已认证，
            // 与 Remote（AuthenticationServiceCollectionExtensions 的 FallbackPolicy）语义对齐。
            // 匿名端点必须显式 [AllowAnonymous]（AuthController 的 login/logout/refresh/auto-login、
            // HealthController 类级、DownloadController.Index 已标注）。
            options.FallbackPolicy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build();

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

            // 仅系统运维（SuperAdmin）策略：配置/部署属运维操作——业务管理员（Admin）无访问
            options.AddPolicy(PolicyConstants.SysAdminOnly, policy =>
                policy.RequireAuthenticatedUser()
                      .RequireRole(RoleConstants.SuperAdmin));

            options.AddPolicy(PolicyConstants.DoctorOrReceptionist, policy =>
                policy.RequireAuthenticatedUser()
                      .RequireRole(RoleConstants.SuperAdmin, RoleConstants.Admin, RoleConstants.Doctor, RoleConstants.Receptionist));

            options.AddPolicy(PolicyConstants.ReceptionistOnly, policy =>
                policy.RequireAuthenticatedUser()
                      .RequireRole(RoleConstants.Receptionist));

            // 三角色策略：Doctor/Admin/SuperAdmin/Receptionist 任一可访问
            // 语义与 WebAPI AuthenticationServiceCollectionExtensions 注册一致（含 SuperAdmin，兼容系统运维）
            options.AddPolicy(PolicyConstants.DoctorOrAdminOrReceptionist, policy =>
                policy.RequireAuthenticatedUser()
                      .RequireRole(RoleConstants.Doctor, RoleConstants.Admin, RoleConstants.SuperAdmin, RoleConstants.Receptionist));
        });
    }

    /// <summary>
    /// 为给定 Identity 用户生成 JWT。
    /// Subject：user.Id，Role 声明（第一个 Identity 角色），12 小时有效期。
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
            issuer: _issuer,
            audience: _audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: DateTime.UtcNow.AddHours(TokenExpirationHours),
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
