using LYBT.Infrastructure.Authentication;
using LYBT.Infrastructure.Configuration;
using LYBT.Infrastructure.Data;
using LYBT.Infrastructure.Logging;
using LYBT.Infrastructure.Options;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace LYBT.Infrastructure.Extensions {

    /// <summary>
    /// 服务集合扩展方法
    /// </summary>
    public static class ServiceCollectionExtensions {

        /// <summary>
        /// 添加JWT认证服务
        /// </summary>
        /// <param name="services">服务集合</param>
        /// <param name="configuration">配置</param>
        /// <returns>服务集合</returns>
        public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration) {
            var jwtSection = configuration.GetSection("JwtOptions");
            services.Configure<JwtOptions>(jwtSection);

            var jwtOptions = jwtSection.Get<JwtOptions>();
            if (jwtOptions == null) {
                throw new InvalidOperationException("JWT configuration is missing");
            }

            services.AddAuthentication(options => {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options => {
                options.TokenValidationParameters = new TokenValidationParameters {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtOptions.Issuer,
                    ValidAudience = jwtOptions.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Secret)),
                    ClockSkew = TimeSpan.FromSeconds(jwtOptions.ClockSkewSeconds)
                };

                options.Events = new JwtBearerEvents {
                    OnMessageReceived = context => {
                        // 支持从查询参数中获取令牌（用于SignalR等场景）
                        var accessToken = context.Request.Query["access_token"];
                        var path = context.HttpContext.Request.Path;
                        if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hub")) {
                            context.Token = accessToken;
                        }
                        return Task.CompletedTask;
                    },
                    OnAuthenticationFailed = context => {
                        // 记录认证失败日志
                        // TODO: 添加日志记录
                        return Task.CompletedTask;
                    }
                };
            });

            // 注册认证相关服务
            services.AddScoped<IJwtAuthenticationService, JwtAuthenticationService>();
            services.AddScoped<IAuthorizationService, AuthorizationService>();

            return services;
        }

        /// <summary>
        /// 添加认证配置
        /// </summary>
        /// <param name="services">服务集合</param>
        /// <param name="configuration">配置</param>
        /// <returns>服务集合</returns>
        public static IServiceCollection AddAuthConfiguration(this IServiceCollection services, IConfiguration configuration) {
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
        public static IServiceCollection AddCorsPolicies(this IServiceCollection services, string policyName = "DefaultPolicy", params string[] allowedOrigins) {
            services.AddCors(options => {
                options.AddPolicy(policyName, builder => {
                    if (allowedOrigins?.Length > 0) {
                        builder.WithOrigins(allowedOrigins);
                    } else {
                        builder.AllowAnyOrigin();
                    }

                    builder.AllowAnyMethod()
                           .AllowAnyHeader();

                    if (allowedOrigins?.Length > 0) {
                        builder.AllowCredentials();
                    }
                });
            });

            return services;
        }

        /// <summary>
        /// 添加缓存服务
        /// </summary>
        /// <param name="services">服务集合</param>
        /// <param name="configuration">配置</param>
        /// <returns>服务集合</returns>
        public static IServiceCollection AddCachingServices(this IServiceCollection services, IConfiguration configuration) {
            // 缓存服务已移除，保留空方法以维持接口兼容性
            return services;
        }

        /// <summary>
        /// 添加API版本控制（已禁用）
        /// </summary>
        /// <param name="services">服务集合</param>
        /// <returns>服务集合</returns>
        public static IServiceCollection AddApiVersioningDisabled(this IServiceCollection services) {
            // 方法已重命名以完全避免冲突
            return services;
        }

        /// <summary>
        /// 添加基础设施数据库上下文
        /// </summary>
        /// <param name="services">服务集合</param>
        /// <param name="configuration">配置</param>
        /// <returns>服务集合</returns>
        public static IServiceCollection AddInfrastructureDbContext(this IServiceCollection services, IConfiguration configuration) {
            var connectionString = configuration.GetConnectionString("InfrastructureConnection")
                                 ?? configuration.GetConnectionString("DefaultConnection");

            if (string.IsNullOrEmpty(connectionString)) {
                throw new InvalidOperationException("Infrastructure database connection string is not configured");
            }

            services.AddDbContext<AppDbContext>(options => {
                options.UseSqlServer(connectionString, sqlOptions => {
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
        /// 添加统一日志服务
        /// </summary>
        /// <param name="services">服务集合</param>
        /// <returns>服务集合</returns>
        public static IServiceCollection AddUnifiedLogging(this IServiceCollection services) {
            services.AddScoped<IUnifiedLogService, UnifiedLogService>();
            return services;
        }

        /// <summary>
        /// 添加统一配置服务
        /// </summary>
        /// <param name="services">服务集合</param>
        /// <returns>服务集合</returns>
        public static IServiceCollection AddUnifiedConfiguration(this IServiceCollection services) {
            // 统一配置服务已移除，保留空方法以维持接口兼容性
            return services;
        }

        /// <summary>
        /// 添加所有基础设施服务
        /// </summary>
        /// <param name="services">服务集合</param>
        /// <param name="configuration">配置</param>
        /// <returns>服务集合</returns>
        public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration) {
            // 添加数据库上下文
            services.AddInfrastructureDbContext(configuration);

            // 添加缓存服务
            services.AddCachingServices(configuration);

            // 添加JWT认证
            services.AddJwtAuthentication(configuration);

            // 添加认证配置
            services.AddAuthConfiguration(configuration);

            // 添加统一日志服务
            services.AddUnifiedLogging();

            // 添加统一配置服务
            services.AddUnifiedConfiguration();

            // 添加CORS策略
            services.AddCorsPolicies();

            // 注意：API版本控制在Program.cs中单独配置

            return services;
        }
    }
}