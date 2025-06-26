using LYBT.Module.Settings.Interfaces;
using LYBT.Module.Settings.Repositories;
using LYBT.Module.Settings.Services;
using Microsoft.Extensions.DependencyInjection;

namespace LYBT.Module.Settings {

    /// <summary>
    /// 系统设置模块服务注册入口
    /// </summary>
    public static class SettingsModule {

        /// <summary>
        /// 注册系统设置相关依赖服务
        /// </summary>
        public static void Register(IServiceCollection services) {
            services.AddScoped<ISettingsRepository, SettingsRepository>();
            services.AddScoped<ISettingsService, SettingsService>();

            services.AddScoped<IDiagnosisCatalogRepository, DiagnosisCatalogRepository>();
            services.AddScoped<IDiagnosisCatalogService, DiagnosisCatalogService>();

            services.AddScoped<ITreatmentCatalogRepository, TreatmentCatalogRepository>();
            services.AddScoped<ITreatmentCatalogService, TreatmentCatalogService>();

            services.AddScoped<IGlobalSettingsRepository, GlobalSettingsRepository>();
            services.AddScoped<IGlobalSettingsService, GlobalSettingsService>();

            services.AddScoped<IEnumMappingsService, EnumMappingsService>();
        }
    }
}