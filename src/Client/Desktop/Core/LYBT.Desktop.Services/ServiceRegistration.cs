using System.Net.Http;
using AutoMapper;
using LYBT.Desktop.Services.Business;
using LYBT.Desktop.Services.Mapping;
using LYBT.Desktop.Services.Exceptions;
using LYBT.Desktop.Services.Http;
using LYBT.Desktop.Services.Repositories;
using LYBT.Desktop.Services.Repositories.Interfaces;
using LYBT.Shared.Interfaces.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Linq;

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
        public static IServiceCollection AddDesktopServices(this IServiceCollection services, string apiBaseUrl = "https://localhost:5001", bool ignoreSslErrors = false)
        {
            // 注册日志服务 (如果尚未注册)
            if (!services.Any(s => s.ServiceType == typeof(ILoggerFactory)))
            {
                services.AddLogging(builder => builder.SetMinimumLevel(LogLevel.Information));
            }

            // 注册 Token 存储服务 (AuthorizationMessageHandler 依赖)
            services.AddSingleton<ITokenStorageService, TokenStorageService>();

            // 注册认证消息处理器
            services.AddTransient<AuthorizationMessageHandler>();

            // 配置 Named HttpClient - 添加认证处理器和 SSL 配置
            services.AddHttpClient("ApiService", client =>
            {
                client.BaseAddress = new Uri(apiBaseUrl);
                client.Timeout = TimeSpan.FromSeconds(30);
                client.DefaultRequestHeaders.Add("Accept", "application/json");
            })
            .ConfigurePrimaryHttpMessageHandler(() =>
            {
                var handler = new HttpClientHandler();
                if (ignoreSslErrors)
                {
                    handler.ServerCertificateCustomValidationCallback =
                        HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
                }
                return handler;
            })
            .AddHttpMessageHandler<AuthorizationMessageHandler>();

            // 注册 IApiService - 使用配置好的 HttpClient
            services.AddScoped<IApiService>(provider =>
            {
                var httpClientFactory = provider.GetRequiredService<IHttpClientFactory>();
                var httpClient = httpClientFactory.CreateClient("ApiService");  // 使用命名客户端
                var cache = provider.GetService<IMemoryCache>();
                var logger = provider.GetService<ILogger<ApiService>>();
                var retryOptions = provider.GetService<RetryPolicyOptions>();

                return new ApiService(httpClient, cache, logger, retryOptions);
            });

            // 注册 AutoMapper - 自动扫描所有 MappingProfile
            services.AddAutoMapper(typeof(HerbMappingProfile).Assembly);

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

            // Issue #1008: 注册ILocalAuthService（Desktop特定认证接口）
            services.AddScoped<ILocalAuthService, AuthService>();

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
    }
}
