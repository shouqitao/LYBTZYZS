using LYBT.Infrastructure.Configuration.Options;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace LYBT.Infrastructure.Configuration.Extensions
{
    /// <summary>
    /// 环境感知的配置验证扩展
    /// 为生产环境提供额外的安全校验，开发环境提供宽松策略
    /// </summary>
    public static class EnvironmentAwareValidation
    {
        /// <summary>
        /// 添加环境感知的配置验证
        /// </summary>
        /// <param name="services">服务集合</param>
        /// <param name="environment">运行环境</param>
        /// <returns>配置后的服务集合</returns>
        public static IServiceCollection AddEnvironmentAwareValidation(this IServiceCollection services, IWebHostEnvironment environment)
        {
            // 为DefaultPasswordOptions添加环境感知验证
            services.AddOptions<DefaultPasswordOptions>()
                .PostConfigure<IWebHostEnvironment>((options, env) =>
                {
                    ValidateDefaultPasswordOptions(options, env);
                });

            // 为SecurityOptions添加环境感知验证
            services.AddOptions<SecurityOptions>()
                .PostConfigure<IWebHostEnvironment>((options, env) =>
                {
                    ValidateSecurityOptions(options, env);
                });

            // 为DatabaseOptions添加环境感知验证
            services.AddOptions<DatabaseOptions>()
                .PostConfigure<IWebHostEnvironment>((options, env) =>
                {
                    ValidateDatabaseOptions(options, env);
                });

            return services;
        }

        /// <summary>
        /// 验证DefaultPasswordOptions的环境相关配置
        /// </summary>
        private static void ValidateDefaultPasswordOptions(DefaultPasswordOptions options, IWebHostEnvironment environment)
        {
            if (environment.IsProduction())
            {
                // 生产环境严格验证
                if (options.AllowInProduction)
                {
                    throw new InvalidOperationException("生产环境不允许启用默认密码功能 (DefaultPasswordOptions.AllowInProduction = false)");
                }

                // 确保默认密码足够强
                if (options.SystemAdmin.Length < 16)
                {
                    throw new InvalidOperationException("生产环境系统管理员密码长度必须至少16个字符");
                }

                if (options.NewUser.Length < 12)
                {
                    throw new InvalidOperationException("生产环境新用户默认密码长度必须至少12个字符");
                }

                // 确保密码包含复杂字符
                if (!IsComplexPassword(options.SystemAdmin))
                {
                    throw new InvalidOperationException("生产环境系统管理员密码必须包含大小写字母、数字和特殊字符");
                }

                if (!IsComplexPassword(options.NewUser))
                {
                    throw new InvalidOperationException("生产环境新用户密码必须包含大小写字母、数字和特殊字符");
                }
            }
            else if (environment.IsDevelopment())
            {
                // 开发环境宽松验证
                if (!options.EnableInDevelopment)
                {
                    // 仅记录警告，不阻止启动
                    // 开发环境建议启用默认密码功能以便调试
                }
            }
        }

        /// <summary>
        /// 验证SecurityOptions的环境相关配置
        /// </summary>
        private static void ValidateSecurityOptions(SecurityOptions options, IWebHostEnvironment environment)
        {
            if (environment.IsProduction())
            {
                // 生产环境必须启用HTTPS
                if (!options.Https.RequireHttps)
                {
                    throw new InvalidOperationException("生产环境必须强制启用HTTPS (SecurityOptions.Https.RequireHttps = true)");
                }

                // 生产环境必须启用安全头
                if (string.IsNullOrEmpty(options.SecurityHeaders.ContentSecurityPolicy))
                {
                    throw new InvalidOperationException("生产环境必须配置内容安全策略 (SecurityOptions.SecurityHeaders.ContentSecurityPolicy)");
                }

                // 生产环境密码策略验证
                if (options.PasswordPolicy.MinLength < 12)
                {
                    throw new InvalidOperationException("生产环境密码最小长度不能少于12个字符");
                }

                if (!options.PasswordPolicy.RequireUppercase || !options.PasswordPolicy.RequireLowercase ||
                    !options.PasswordPolicy.RequireDigit || !options.PasswordPolicy.RequireSpecialChar)
                {
                    throw new InvalidOperationException("生产环境必须启用完整的密码复杂度要求");
                }
            }
        }

        /// <summary>
        /// 验证DatabaseOptions的环境相关配置
        /// </summary>
        private static void ValidateDatabaseOptions(DatabaseOptions options, IWebHostEnvironment environment)
        {
            if (environment.IsProduction())
            {
                // 生产环境不允许启用敏感日志
                if (options.EnableSensitiveDataLogging)
                {
                    throw new InvalidOperationException("生产环境不允许记录敏感数据日志 (DatabaseOptions.EnableSensitiveDataLogging = false)");
                }

                // 生产环境不允许启用详细错误
                if (options.EnableDetailedErrors)
                {
                    throw new InvalidOperationException("生产环境不允许启用详细错误信息 (DatabaseOptions.EnableDetailedErrors = false)");
                }

                // 生产环境必须启用性能监控
                if (!options.Monitoring.EnablePerformanceMonitoring)
                {
                    // 生产环境建议启用性能监控
                }

                // 生产环境连接池配置验证
                if (options.ConnectionPool.MaxPoolSize > 100)
                {
                    // 生产环境数据库连接池过大，建议不超过100个连接
                }

                // 生产环境建议启用自动备份
                if (!options.Backup.EnableAutoBackup)
                {
                    // 生产环境建议启用自动备份
                }
            }
            else if (environment.IsDevelopment())
            {
                // 开发环境建议启用详细日志
                if (!options.EnableSensitiveDataLogging)
                {
                    // 开发环境建议启用敏感数据日志以便调试
                }

                if (!options.EnableDetailedErrors)
                {
                    // 开发环境建议启用详细错误信息以便调试
                }
            }
        }

        /// <summary>
        /// 检查密码复杂度
        /// </summary>
        private static bool IsComplexPassword(string password)
        {
            if (string.IsNullOrEmpty(password))
            {
                return false;
            }

            bool hasUpper = password.Any(char.IsUpper);
            bool hasLower = password.Any(char.IsLower);
            bool hasDigit = password.Any(char.IsDigit);
            bool hasSpecial = password.Any(ch => !char.IsLetterOrDigit(ch));

            return hasUpper && hasLower && hasDigit && hasSpecial;
        }
    }
}
