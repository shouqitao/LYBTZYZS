using System.Text;
using LYBT.Infrastructure.Data;
using LYBT.Infrastructure.Services;
using LYBT.Shared.Configuration.Options.Common;
using LYBT.Shared.ExceptionHandling.Mappers;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace LYBT.Infrastructure
{

    /// <summary>
    /// 服务集合扩展方法
    /// </summary>
    public static class ServiceCollectionExtensions
    {

        /// <summary>
        /// 添加JWT认证服务
        /// </summary>
        /// <param name="services">服务集合</param>
        /// <param name="configuration">配置</param>
        /// <returns>服务集合</returns>
        public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
        {
            // unify-configuration-system: 使用强类型 JwtOptions
            var jwtOptions = new JwtOptions();
            configuration.GetSection(JwtOptions.SectionName).Bind(jwtOptions);

            if (string.IsNullOrEmpty(jwtOptions.SecretKey))
            {
                throw new InvalidOperationException(
                    "JWT 配置缺失。请检查 appsettings.json 中的 Jwt 配置节。");
            }

            // 注册 JwtOptions 到 DI 容器
            services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtOptions.Issuer,
                    ValidAudience = jwtOptions.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SecretKey)),
                    ClockSkew = TimeSpan.FromSeconds(jwtOptions.ClockSkewSeconds)
                };

                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        // 支持从查询参数中获取令牌（用于SignalR等场景）
                        var accessToken = context.Request.Query["access_token"];
                        var path = context.HttpContext.Request.Path;
                        if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hub"))
                        {
                            context.Token = accessToken;
                        }

                        return Task.CompletedTask;
                    },
                    OnAuthenticationFailed = context =>
                    {
                        // 记录认证失败日志
                        var logger = context.HttpContext.RequestServices.GetService<ILogger<JwtBearerHandler>>();
                        logger?.LogWarning(
                            "JWT认证失败: {Exception}, Path: {Path}, Token: {Token}",
                            context.Exception?.Message,
                            context.HttpContext.Request.Path,
                            context.Request.Headers.Authorization.FirstOrDefault()?.Replace("Bearer ", "")?[..Math.Min(10, context.Request.Headers.Authorization.FirstOrDefault()?.Replace("Bearer ", "")?.Length ?? 0)] + "...");
                        return Task.CompletedTask;
                    }
                };
            });

            // JWT服务已移至AuthModule中注册
            return services;
        }

        /// <summary>
        /// 添加基础设施数据库上下文
        /// </summary>
        /// <param name="services">服务集合</param>
        /// <param name="configuration">配置</param>
        /// <returns>服务集合</returns>
        public static IServiceCollection AddInfrastructureDbContext(this IServiceCollection services, IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("InfrastructureConnection")
                                 ?? configuration.GetConnectionString("DefaultConnection");

            if (string.IsNullOrEmpty(connectionString))
            {
                throw new InvalidOperationException("Infrastructure database connection string is not configured");
            }

            services.AddDbContext<AppDbContext>((serviceProvider, options) =>
            {
                options.UseSqlServer(connectionString, sqlOptions =>
                {
                    sqlOptions.MigrationsAssembly("LYBT.Infrastructure");
                    sqlOptions.EnableRetryOnFailure(
                        maxRetryCount: 3,
                        maxRetryDelay: TimeSpan.FromSeconds(30),
                        errorNumbersToAdd: null);
                });
                options.EnableSensitiveDataLogging(false);
                options.EnableServiceProviderCaching();
            });

            return services;
        }

        /// <summary>
        /// 添加所有基础设施服务
        /// </summary>
        /// <param name="services">服务集合</param>
        /// <param name="configuration">配置</param>
        /// <returns>服务集合</returns>
        public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
        {
            // UltraThink深度清理：只保留实际需要的服务
            // 添加数据库上下文
            services.AddInfrastructureDbContext(configuration);

            // 添加JWT认证
            services.AddJwtAuthentication(configuration);

            // 添加JWT黑名单服务
            // services.AddSingleton<ITokenBlacklistService, TokenBlacklistService>(); // 移除过度工程

            // ISP 拆分 (D5-1) -- 3 接口共享同一 Scoped 实例
            services.AddScoped<CrossModuleService>();
            services.AddScoped<Services.CrossModule.IPatientCrossModuleService>(sp => sp.GetRequiredService<CrossModuleService>());
            services.AddScoped<Services.CrossModule.IHerbCrossModuleService>(sp => sp.GetRequiredService<CrossModuleService>());
            services.AddScoped<Services.CrossModule.IUserCrossModuleService>(sp => sp.GetRequiredService<CrossModuleService>());
            // ICrossModuleAuthService 暂不注册 -- 待 S1 安全加固阶段实现后启用
            // 旧接口保留兼容 (标记 [Obsolete])
            services.AddScoped<ICrossModuleService>(sp => sp.GetRequiredService<CrossModuleService>());

            // refactor-logging-system: 添加错误消息映射服务
            services.AddSingleton<IErrorMessageMapper, ConfigurableErrorMessageMapper>();

            // 注意：API版本控制在Program.cs中单独配置
            return services;
        }
    }
}
