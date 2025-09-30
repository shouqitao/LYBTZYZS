using System.Net.Http;
using LYBT.Desktop.Services.Business;
using LYBT.Desktop.Services.Exceptions;
using LYBT.Desktop.Services.Http;
using LYBT.Desktop.Services.Repositories;
using LYBT.Desktop.Services.Repositories.Interfaces;
using LYBT.Shared.Interfaces.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Services
{
    /// <summary>
    /// 服务注册扩展 - UltraThink架构
    /// </summary>
    public static class ServiceRegistration
    {
        /// <summary>
        /// 注册所有服务 - UltraThink架构完整配置
        /// </summary>
        public static IServiceCollection AddDesktopServices(this IServiceCollection services, string apiBaseUrl = "https://localhost:5001")
        {
            // 配置HttpClient和ApiService - 修复重复注册问题
            services.AddHttpClient<ApiService>(client =>
            {
                client.BaseAddress = new Uri(apiBaseUrl);
                client.Timeout = TimeSpan.FromSeconds(30);
                client.DefaultRequestHeaders.Add("Accept", "application/json");
            });

            // 注册IApiService - 使用HttpClient工厂创建的实例
            services.AddScoped<IApiService>(provider =>
            {
                var httpClientFactory = provider.GetRequiredService<IHttpClientFactory>();
                var httpClient = httpClientFactory.CreateClient(nameof(ApiService));
                var cache = provider.GetService<IMemoryCache>();
                var logger = provider.GetService<ILogger<ApiService>>();
                var retryOptions = provider.GetService<RetryPolicyOptions>();

                return new ApiService(httpClient, cache, logger, retryOptions);
            });

            // 注册异常处理器
            services.AddScoped<IExceptionHandler, StandardExceptionHandler>();

            // 注册Repository接口和实现
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IPatientRepository, PatientRepository>();
            services.AddScoped<IHerbRepository, HerbRepository>();
            services.AddScoped<IFormulaRepository, FormulaRepository>();
            services.AddScoped<IMedicalCaseRepository, MedicalCaseRepository>();
            services.AddScoped<IPrescriptionRepository, PrescriptionRepository>();
            services.AddScoped<IConsultationRepository, ConsultationRepository>();

            // 注册业务服务接口 - 使用Shared.Interfaces统一接口
            services.AddScoped<IPrescriptionService, PrescriptionService>();
            services.AddScoped<IHerbService, HerbService>();
            services.AddScoped<IFormulaService, FormulaService>();
            services.AddScoped<IMedicalCaseService, MedicalCaseService>();
            services.AddScoped<IPatientService, PatientService>();
            services.AddScoped<IConsultationService, ConsultationService>();
            services.AddScoped<IUserService, UserService>();
            // TODO: AuthService 接口签名不兼容，需要单独Issue处理 - 暂时注释
            // services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<AuthService>(); // 暂时注册为具体类型

            // 注册内存缓存 - 带配置优化
            services.AddMemoryCache(options =>
            {
                options.SizeLimit = 1000; // 限制缓存项数量
                options.CompactionPercentage = 0.25; // 内存压力时压缩25%
                options.ExpirationScanFrequency = TimeSpan.FromMinutes(5); // 每5分钟扫描过期项
            });

            // 注册Polly重试策略配置
            services.AddSingleton<RetryPolicyOptions>();

            return services;
        }

        /// <summary>
        /// 向后兼容的方法
        /// </summary>
        [Obsolete("请使用 AddDesktopServices 方法")]
        public static IServiceCollection AddBusinessServices(this IServiceCollection services)
        {
            return AddDesktopServices(services);
        }
    }
}
