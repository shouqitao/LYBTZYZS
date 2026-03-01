using System.Text;
using LYBT.Shared.Configuration.Options.Common;
using Microsoft.AspNetCore.Authentication.JwtBearer;

using Microsoft.IdentityModel.Tokens;

namespace LYBT.WebAPI.Extensions;

/// <summary>
/// 认证与授权服务注册扩展
/// Issue #1732 Phase 2.5: 从UnifiedServiceRegistration拆分
/// 职责：JWT认证、授权策略配置
/// unify-configuration-system: 迁移到 LYBT.Shared.Configuration
/// </summary>
public static class AuthenticationServiceCollectionExtensions
{
    /// <summary>
    /// 注册认证与授权服务
    /// </summary>
    public static IServiceCollection RegisterAuthenticationServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // unify-configuration-system: 使用强类型 JwtOptions
        var jwtOptions = new JwtOptions();
        configuration.GetSection(JwtOptions.SectionName).Bind(jwtOptions);

        // JWT 认证 - 从统一配置读取
        try
        {
            // unify-configuration-system: 使用扁平化配置路径
            var jwtSecret = Environment.GetEnvironmentVariable("JWT_SECRET") ??
                           jwtOptions.SecretKey;

            if (string.IsNullOrEmpty(jwtSecret))
            {
                var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";
                if (environment.Equals("Production", StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException("生产环境必须配置 JWT 密钥（JWT_SECRET 或 Jwt:SecretKey）。");
                }

                jwtSecret = "DefaultDevelopmentSecretKeyForJWTAuthentication_ShouldBeReplacedInProduction";
            }

            if (!string.IsNullOrEmpty(jwtSecret))
            {
                // unify-configuration-system: 使用强类型配置
                var issuer = jwtOptions.Issuer;
                var audience = jwtOptions.Audience;
                var clockSkew = jwtOptions.ClockSkewSeconds;

                services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
                }).AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        // 基本验证设置
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,

                        // 发行者和接收者
                        ValidIssuer = issuer,
                        ValidAudience = audience,

                        // 密钥设置 - 支持多密钥验证
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),

                        // 时钟偏差 - 使用配置值
                        ClockSkew = TimeSpan.FromSeconds(clockSkew),

                        // 增强安全设置
                        RequireExpirationTime = true,
                        RequireSignedTokens = true,
                        ValidateTokenReplay = false, // 如果需要防重放攻击可设为true

                        // Token类型验证
                        ValidTypes = new[] { "JWT" },

                        // 严格的签名验证
                        TryAllIssuerSigningKeys = true // 启用多密钥验证支持密钥轮换
                    };
                });
            }
            else
            {
                throw new InvalidOperationException("JWT 密钥为空，无法配置认证。");
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("配置 JWT 认证失败", ex);
        }

        // 配置授权策略
        services.AddAuthorization(options =>
        {
            // 设置默认策略 - 要求所有端点默认需要认证
            options.DefaultPolicy = new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build();

            // Sprint3-A3-08: 启用 FallbackPolicy，默认要求所有端点认证
            // Swagger 中间件已移至 UseRouting 之前，不受 FallbackPolicy 影响
            // /health 端点需要显式 AllowAnonymous() 豁免
            options.FallbackPolicy = new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build();

            // 定义基于角色的策略
            // optimize-api-permissions: 添加SuperAdmin支持
            options.AddPolicy("AdminOnly", policy =>
                policy.RequireAuthenticatedUser()
                      .RequireRole("SuperAdmin", "Admin"));

            options.AddPolicy("DoctorOrAdmin", policy =>
                policy.RequireAuthenticatedUser()
                      .RequireRole("SuperAdmin", "Admin", "Doctor"));

            // T5-P2-30: 患者模块访问策略 - 包含Receptionist
            options.AddPolicy("PatientAccess", policy =>
                policy.RequireAuthenticatedUser()
                      .RequireRole("SuperAdmin", "Admin", "Doctor", "Receptionist"));

            // CODE-04: 最高权限操作策略 (仅 SuperAdmin)
            options.AddPolicy("SuperAdminOnly", policy =>
                policy.RequireAuthenticatedUser()
                      .RequireRole("SuperAdmin"));

            options.AddPolicy("RequireAuthenticated", policy =>
                policy.RequireAuthenticatedUser());
        });

        return services;
    }
}
