using LYBT.Infrastructure.Options;
using Microsoft.AspNetCore.Cors.Infrastructure;

namespace LYBT.WebAPI.Extensions;

/// <summary>
/// 安全的CORS策略扩展
/// </summary>
public static class CorsExtension
{
    /// <summary>
    /// 注册安全的跨域策略
    /// </summary>
    public static IServiceCollection AddSecureCorsPolicy(this IServiceCollection services, IConfiguration configuration, IWebHostEnvironment environment)
    {
        var securityOptions = configuration.GetSection(SecurityOptions.SectionName).Get<SecurityOptions>() ?? new SecurityOptions();
        
        services.AddCors(options =>
        {
            // 开发环境策略
            options.AddPolicy("Development", builder =>
            {
                if (securityOptions.Cors.AllowedOrigins.Any())
                {
                    builder.WithOrigins(securityOptions.Cors.AllowedOrigins.ToArray());
                }
                else
                {
                    // 开发环境默认配置
                    builder.WithOrigins("http://localhost:3000", "http://localhost:5000", "https://localhost:5001", "https://localhost:7001");
                }
                
                builder.WithMethods(securityOptions.Cors.AllowedMethods.ToArray())
                       .WithHeaders(securityOptions.Cors.AllowedHeaders.ToArray())
                       .SetPreflightMaxAge(TimeSpan.FromSeconds(securityOptions.Cors.PreflightMaxAge));
                       
                if (securityOptions.Cors.AllowCredentials)
                {
                    builder.AllowCredentials();
                }
            });

            // 生产环境策略
            options.AddPolicy("Production", builder =>
            {
                if (!securityOptions.Cors.AllowedOrigins.Any())
                {
                    throw new InvalidOperationException("生产环境必须配置具体的CORS源");
                }

                builder.WithOrigins(securityOptions.Cors.AllowedOrigins.ToArray())
                       .WithMethods(securityOptions.Cors.AllowedMethods.ToArray())
                       .WithHeaders(securityOptions.Cors.AllowedHeaders.ToArray())
                       .SetPreflightMaxAge(TimeSpan.FromSeconds(securityOptions.Cors.PreflightMaxAge));
                       
                if (securityOptions.Cors.AllowCredentials)
                {
                    builder.AllowCredentials();
                }
            });

            // 默认策略（基于环境）
            options.AddDefaultPolicy(builder =>
            {
                if (environment.IsDevelopment() && !securityOptions.Cors.AllowedOrigins.Any())
                {
                    // 开发环境宽松配置
                    builder.WithOrigins("http://localhost:3000", "http://localhost:5000", "https://localhost:5001", "https://localhost:7001")
                           .AllowAnyMethod()
                           .AllowAnyHeader()
                           .AllowCredentials();
                }
                else
                {
                    // 生产环境严格配置
                    if (!securityOptions.Cors.AllowedOrigins.Any())
                    {
                        throw new InvalidOperationException("生产环境必须配置具体的CORS源");
                    }

                    builder.WithOrigins(securityOptions.Cors.AllowedOrigins.ToArray())
                           .WithMethods(securityOptions.Cors.AllowedMethods.ToArray())
                           .WithHeaders(securityOptions.Cors.AllowedHeaders.ToArray())
                           .SetPreflightMaxAge(TimeSpan.FromSeconds(securityOptions.Cors.PreflightMaxAge));
                           
                    if (securityOptions.Cors.AllowCredentials)
                    {
                        builder.AllowCredentials();
                    }
                }
            });
        });

        return services;
    }

    /// <summary>
    /// 保留向后兼容的方法（已废弃）
    /// </summary>
    [Obsolete("请使用 AddSecureCorsPolicy 方法")]
    public static IServiceCollection AddCorsPolicy(this IServiceCollection services)
    {
        services.AddCors(options =>
        {
            options.AddPolicy("AllowAll", builder =>
                builder.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
        });
        return services;
    }
}