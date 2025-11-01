using System.Text;
using LYBT.Infrastructure.Configuration.Extensions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace LYBT.WebAPI.Extensions;

/// <summary>
/// 认证与授权服务注册扩展
/// Issue #1732 Phase 2.5: 从UnifiedServiceRegistration拆分
/// 职责：JWT认证、授权策略配置
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
        // =========== UltraThink Phase 2：使用统一配置 ===========
        var lybtOptions = configuration.GetLybtOptions();

        // JWT 认证 - 从统一配置读取
        try
        {
            // Issue #1761 Phase 3.1: Authentication.Jwt → Jwt（完全扁平化）
            var jwtSecret = Environment.GetEnvironmentVariable("JWT_SECRET") ??
                           lybtOptions.Jwt.SecretKey;

            if (string.IsNullOrEmpty(jwtSecret))
            {
                var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";
                if (environment.Equals("Production", StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException("生产环境必须配置 JWT 密钥（JWT_SECRET 或 Lybt:Jwt:SecretKey）。");
                }

                jwtSecret = "DefaultDevelopmentSecretKeyForJWTAuthentication_ShouldBeReplacedInProduction";
            }

            if (!string.IsNullOrEmpty(jwtSecret))
            {
                // Issue #1761 Phase 3.1: Authentication.Jwt → Jwt（完全扁平化）
                var jwtConfig = lybtOptions.Jwt;
                var issuer = jwtConfig.Issuer;
                var audience = jwtConfig.Audience;
                var clockSkew = 300; // 5分钟时钟偏差（安全默认值）

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

            // 不设置全局回退策略，允许未标注授权属性的端点（如Swagger）匿名访问
            // options.FallbackPolicy = new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
            //     .RequireAuthenticatedUser()
            //     .Build();

            // 定义基于角色的策略
            options.AddPolicy("AdminOnly", policy =>
                policy.RequireAuthenticatedUser()
                      .RequireRole("Admin"));

            options.AddPolicy("DoctorOrAdmin", policy =>
                policy.RequireAuthenticatedUser()
                      .RequireRole("Doctor", "Admin"));

            options.AddPolicy("RequireAuthenticated", policy =>
                policy.RequireAuthenticatedUser());
        });

        return services;
    }
}
