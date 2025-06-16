using Microsoft.Extensions.DependencyInjection;
using LYBT.Module.Doctors.Interfaces;
using LYBT.Module.Doctors.Repositories;
using LYBT.Module.Doctors.Services;

namespace LYBT.Module.Doctors {
    /// <summary>
    /// 医生模块服务注册入口
    /// </summary>
    public static class DoctorsModule {
        /// <summary>
        /// 注册医生相关依赖服务
        /// </summary>
        public static void Register(IServiceCollection services) {
            services.AddScoped<IDoctorRepository, DoctorRepository>();
            services.AddScoped<IDoctorService, DoctorService>();
        }
    }
}
