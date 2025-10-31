using LYBT.Infrastructure.Security;
using Microsoft.AspNetCore.DataProtection;

namespace LYBT.WebAPI.Extensions.ServiceCollection
{
    /// <summary>
    /// 安全服务配置扩展
    /// Issue #1732 Phase 3: 移除密钥轮转后台服务（MVP阶段过度设计）
    /// </summary>
    public static class SecurityServiceExtensions
    {
        /// <summary>
        /// 添加安全相关服务
        /// Issue #1732 Phase 3: 仅保留数据保护和基础密钥管理，移除密钥轮转
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

            // 注册基础密钥管理服务（保留基础设施以便未来扩展）
            services.AddScoped<IKeyManagementService, KeyManagementService>();
            services.AddSingleton<IKeyManagementServiceFactory, KeyManagementServiceFactory>();

            // Issue #1732 Phase 3: 移除以下过度设计配置
            // ❌ Token黑名单服务 - MVP阶段无需Token撤销功能
            // ❌ 密钥轮转后台服务 - MVP使用单一JWT密钥，无多密钥轮换需求
            //    - 当前仅v1.0 API，无多版本Token兼容性需求
            //    - 6-12个月内无密钥轮换场景
            //    - 密钥轮换属于高级安全特性，延后至生产环境实际需求时实施

            return services;
        }
    }
}
