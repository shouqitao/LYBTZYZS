using Microsoft.Extensions.DependencyInjection;
using LYBT.Module.Registration.Interfaces;
using LYBT.Module.Registration.Repositories;
using LYBT.Module.Registration.Services;

namespace LYBT.Module.Registration {
    /// <summary>
    /// 挂号模块服务注册入口
    /// </summary>
    public static class RegistrationModule {
        /// <summary>
        /// 注册挂号相关依赖服务
        /// </summary>
        public static void Register(IServiceCollection services) {
            services.AddScoped<IRegistrationRepository, RegistrationRepository>();
            services.AddScoped<IRegistrationService, RegistrationService>();
        }
    }
}
