using LYBT.Infrastructure.Security;
using Microsoft.AspNetCore.DataProtection;

namespace LYBT.WebAPI.Extensions.ServiceCollection
{
    /// <summary>
    /// 安全服务配置扩展
    /// </summary>
    public static class SecurityServiceExtensions
    {
        /// <summary>
        /// 添加安全相关服务
        /// </summary>
        public static IServiceCollection AddSecurityServices(
            this IServiceCollection services,
            IConfiguration configuration,
            IWebHostEnvironment environment)
        {
            // 配置数据保护
            services.AddDataProtection()
                .SetApplicationName("LYBT")
                .PersistKeysToFileSystem(new DirectoryInfo(
                    Path.Combine(environment.ContentRootPath, "DataProtection-Keys")));

            if (environment.IsProduction())
            {
                // 生产环境：使用证书保护密钥
                // services.AddDataProtection().ProtectKeysWithCertificate(certificate);
            }

            // 注册数据保护服务
            services.AddSingleton<IDataProtectionService, DataProtectionService>();

            // 注册密钥管理服务（使用简化版本）
            services.AddScoped<IKeyManagementService, SimpleKeyManagementService>();

            // 添加密钥旋转后台服务
            services.AddHostedService<KeyRotationBackgroundService>();

            return services;
        }
    }
}