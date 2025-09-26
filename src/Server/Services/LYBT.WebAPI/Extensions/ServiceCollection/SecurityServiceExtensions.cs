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

            // 移除对不存在服务的注册，使用简化的密钥管理
            // 注册密钥管理服务
            services.AddScoped<IKeyManagementService, KeyManagementService>();
            
            // 注册密钥管理服务工厂（避免Service Locator反模式）
            services.AddSingleton<IKeyManagementServiceFactory, KeyManagementServiceFactory>();

            // JWT安全服务
            // 注册Token黑名单服务
            services.AddScoped<ITokenBlacklistService, TokenBlacklistService>();

            // 添加密钥旋转后台服务（使用工厂模式）
            services.AddHostedService<KeyRotationBackgroundService>();

            return services;
        }
    }
}