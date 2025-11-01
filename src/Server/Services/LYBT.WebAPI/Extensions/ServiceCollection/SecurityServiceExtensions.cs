using Microsoft.AspNetCore.DataProtection;

namespace LYBT.WebAPI.Extensions.ServiceCollection
{
    /// <summary>
    /// 安全服务配置扩展
    /// Issue #1743: 仅保留ASP.NET Core DataProtection配置
    /// </summary>
    public static class SecurityServiceExtensions
    {
        /// <summary>
        /// 添加安全相关服务（ASP.NET Core DataProtection）
        /// </summary>
        public static IServiceCollection AddSecurityServices(
            this IServiceCollection services,
            IConfiguration configuration,
            IWebHostEnvironment environment)
        {
            // 配置数据保护（ASP.NET Core密钥管理）
            services.AddDataProtection()
                .SetApplicationName("LYBT")
                .PersistKeysToFileSystem(new DirectoryInfo(
                    Path.Combine(environment.ContentRootPath, "DataProtection-Keys")));

            if (environment.IsProduction())
            {
                // 生产环境：使用证书保护密钥（可选，未来配置）
                // services.AddDataProtection().ProtectKeysWithCertificate(certificate);
            }

            return services;
        }
    }
}
