using Microsoft.Extensions.DependencyInjection;
using LYBT.Module.Pharmacy.Interfaces;
using LYBT.Module.Pharmacy.Repositories;
using LYBT.Module.Pharmacy.Services;

namespace LYBT.Module.Pharmacy {
    /// <summary>
    /// 药房模块服务注册入口
    /// </summary>
    public static class PharmacyModule {
        /// <summary>
        /// 注册药房相关依赖服务
        /// </summary>
        public static void Register(IServiceCollection services) {
            services.AddScoped<IPharmacyRepository, PharmacyRepository>();
            services.AddScoped<IPharmacyService, PharmacyService>();
        }
    }
}
