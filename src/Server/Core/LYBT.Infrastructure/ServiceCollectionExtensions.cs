using System.Text;
using LYBT.Infrastructure.Configuration.Options;
using LYBT.Infrastructure.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using JwtOptions = LYBT.Infrastructure.Configuration.Options.JwtOptions;

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
            var jwtSection = configuration.GetSection("JwtOptions");
            services.Configure<JwtOptions>(jwtSection);

            var jwtOptions = jwtSection.Get<JwtOptions>();
            if (jwtOptions == null)
            {
                throw new InvalidOperationException("JWT configuration is missing");
            }

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
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Secret)),
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
                        // TODO: 添加日志记录
                        return Task.CompletedTask;
                    }
                };
            });

            // JWT服务已移至AuthModule中注册
            return services;
        }

        /// <summary>
        /// 添加认证配置
        /// </summary>
        /// <param name="services">服务集合</param>
        /// <param name="configuration">配置</param>
        /// <returns>服务集合</returns>
        public static IServiceCollection AddAuthConfiguration(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<AuthOptions>(configuration.GetSection("AuthOptions"));
            return services;
        }

        /// <summary>
        /// 添加CORS策略
        /// </summary>
        /// <param name="services">服务集合</param>
        /// <param name="policyName">策略名称</param>
        /// <param name="allowedOrigins">允许的来源</param>
        /// <returns>服务集合</returns>
        public static IServiceCollection AddCorsPolicies(this IServiceCollection services, string policyName = "DefaultPolicy", params string[] allowedOrigins)
        {
            services.AddCors(options =>
            {
                options.AddPolicy(policyName, builder =>
                {
                    if (allowedOrigins?.Length > 0)
                    {
                        builder.WithOrigins(allowedOrigins);
                    }
                    else
                    {
                        builder.AllowAnyOrigin();
                    }

                    builder.AllowAnyMethod()
                           .AllowAnyHeader();

                    if (allowedOrigins?.Length > 0)
                    {
                        builder.AllowCredentials();
                    }
                });
            });

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

            services.AddDbContext<AppDbContext>(options =>
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

            // 添加认证配置
            services.AddAuthConfiguration(configuration);

            // 添加CORS策略
            services.AddCorsPolicies();

            // 注意：API版本控制在Program.cs中单独配置
            return services;
        }
    }
}
