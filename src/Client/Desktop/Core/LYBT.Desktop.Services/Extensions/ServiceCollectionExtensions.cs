using System.Net.Http;
using LYBT.Desktop.Services.Business;
using LYBT.Desktop.Services.Http;
using LYBT.Desktop.Services.Repositories;
using LYBT.Desktop.Services.Repositories.Interfaces;
using LYBT.Shared.Interfaces.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LYBT.Desktop.Services.Extensions
{
    /// <summary>
    /// 服务集合扩展方法 - UltraThink架构
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// 注册桌面服务
        /// </summary>
        public static IServiceCollection AddDesktopServices(this IServiceCollection services, IConfiguration configuration)
        {
            // 配置HttpClient
            var apiBaseUrl = configuration["ApiSettings:BaseUrl"] ?? "https://localhost:5001";

            services.AddHttpClient<IApiService, ApiService>(client =>
            {
                client.BaseAddress = new Uri(apiBaseUrl);
                client.DefaultRequestHeaders.Add("Accept", "application/json");
                client.Timeout = TimeSpan.FromSeconds(30);
            });

            // 注册Repository层
            services.AddScoped<IPatientRepository, PatientRepository>();
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IMedicalCaseRepository, MedicalCaseRepository>();
            services.AddScoped<IPrescriptionRepository, PrescriptionRepository>();
            services.AddScoped<IHerbRepository, HerbRepository>();
            services.AddScoped<IFormulaRepository, FormulaRepository>();
            services.AddScoped<IConsultationRepository, ConsultationRepository>();

            // 注册Service层
            services.AddScoped<IPatientService, PatientService>();
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<IMedicalCaseService, MedicalCaseService>();
            services.AddScoped<IPrescriptionService, PrescriptionService>();
            services.AddScoped<IHerbService, HerbService>();
            services.AddScoped<IFormulaService, FormulaService>();
            services.AddScoped<IConsultationService, ConsultationService>();

            // Issue #1008: 注册ILocalAuthService（Desktop特定认证接口）
            services.AddScoped<ILocalAuthService, AuthService>();

            return services;
        }

        /// <summary>
        /// 配置API客户端
        /// </summary>
        public static IServiceCollection ConfigureApiClient(this IServiceCollection services, Action<HttpClient> configureClient)
        {
            services.ConfigureHttpClientDefaults(builder =>
            {
                builder.ConfigureHttpClient(configureClient);
            });

            return services;
        }
    }
}
