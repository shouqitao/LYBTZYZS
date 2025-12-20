using System.Text;
using LYBT.Infrastructure.Configuration.Options;
using LYBT.Infrastructure.Data;
using LYBT.Infrastructure.Services;
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
            // 使用统一的 LybtOptions 配置系统
            var lybtOptions = configuration.GetSection("Lybt").Get<LybtOptions>();
            // Issue #1761 Phase 3.1: Authentication.Jwt → Jwt（完全扁平化）
            if (lybtOptions?.Jwt == null)
            {
                throw new InvalidOperationException(
                    "JWT 配置缺失。请检查 appsettings.json 中的 Lybt:Jwt 配置节。");
            }

            var jwtConfig = lybtOptions.Jwt;

            // 注册 LybtOptions 到 DI 容器（供其他服务如 JwtService 使用）
            services.Configure<LybtOptions>(configuration.GetSection("Lybt"));

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
                    ValidIssuer = jwtConfig.Issuer,
                    ValidAudience = jwtConfig.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtConfig.SecretKey)),
                    ClockSkew = TimeSpan.FromSeconds(jwtConfig.ClockSkewSeconds)
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

            // 添加跨模块查询服务 - 解耦模块间依赖
            services.AddScoped<ICrossModuleQueryService, CrossModuleQueryService>();

            // refactor-logging-system: 添加错误消息映射服务
            services.AddSingleton<IErrorMessageMapper, ConfigurableErrorMessageMapper>();

            // 注意：API版本控制在Program.cs中单独配置
            return services;
        }
    }
}
