using System.Text;
using LYBT.Infrastructure.Constants;
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
            // P2-2-5 JWT 顺序 ADR：AddIdentity 必须在 RegisterAuthenticationServices 之前（见 Program.cs 325-330 注释），
            // 本扩展依赖 Identity 已注册的 Cookie 默认方案被 JWT Bearer 覆盖，否则 302 重定向 /Account/Login。
            // unify-configuration-system: 使用扁平化配置路径
            var jwtSecret = Environment.GetEnvironmentVariable("JWT_SECRET") ??
                           jwtOptions.SecretKey;

            if (string.IsNullOrEmpty(jwtSecret))
            {
                throw new InvalidOperationException("必须配置 JWT 密钥（JWT_SECRET 环境变量或 Jwt:SecretKey 配置项）。");
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

                    // P3 (US-USER-010): 已禁用/已删除用户令牌拒绝（令牌有效期内的状态拦截）
                    options.Events = new JwtBearerEvents
                    {
                        OnTokenValidated = async context =>
                        {
                            var userIdClaim = context.Principal?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
                            {
                                context.Fail("令牌缺少用户标识");
                                return;
                            }

                            var userService = context.HttpContext.RequestServices.GetService<LYBT.Infrastructure.Services.CrossModule.IUserCrossModuleService>();
                            var user = userService != null
                                ? await userService.GetUserBasicInfoAsync(userId, context.HttpContext.RequestAborted)
                                : null;
                            if (user == null || user.Status != LYBT.Shared.Models.Enums.CommonStatus.Enabled)
                            {
                                context.Fail("用户已被禁用或不存在");
                            }
                        }
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

            // 纯 Admin（业务管理）策略：不含 SuperAdmin（系统运维不碰业务数据）
            options.AddPolicy(PolicyConstants.AdminBusinessOnly, policy =>
                policy.RequireAuthenticatedUser()
                      .RequireRole(RoleConstants.Admin));

            options.AddPolicy(PolicyConstants.DoctorOnly, policy =>
                policy.RequireAuthenticatedUser()
                      .RequireRole(RoleConstants.Doctor));

            options.AddPolicy(PolicyConstants.DoctorOrAdmin, policy =>
                policy.RequireAuthenticatedUser()
                      .RequireRole(RoleConstants.SuperAdmin, RoleConstants.Admin, RoleConstants.Doctor));

            options.AddPolicy(PolicyConstants.AdminOrSuperAdmin, policy =>
                policy.RequireAuthenticatedUser()
                      .RequireRole(RoleConstants.Admin, RoleConstants.SuperAdmin));

            options.AddPolicy(PolicyConstants.SysAdminOnly, policy =>
                policy.RequireAuthenticatedUser()
                      .RequireRole(RoleConstants.SuperAdmin));

            options.AddPolicy(PolicyConstants.DoctorOrReceptionist, policy =>
                policy.RequireAuthenticatedUser()
                      .RequireRole(RoleConstants.SuperAdmin, RoleConstants.Admin, RoleConstants.Doctor, RoleConstants.Receptionist));

            options.AddPolicy(PolicyConstants.ReceptionistOnly, policy =>
                policy.RequireAuthenticatedUser()
                      .RequireRole(RoleConstants.Receptionist));

            options.AddPolicy(PolicyConstants.DoctorOrAdminOrReceptionist, policy =>
                policy.RequireAuthenticatedUser()
                      .RequireRole(RoleConstants.Doctor, RoleConstants.Admin, RoleConstants.SuperAdmin, RoleConstants.Receptionist));
        });

        return services;
    }
}


