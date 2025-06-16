using Microsoft.Extensions.DependencyInjection;
using LYBT.Module.TreatmentRoom.Interfaces;
using LYBT.Module.TreatmentRoom.Repositories;
using LYBT.Module.TreatmentRoom.Services;

namespace LYBT.Module.TreatmentRoom {
    /// <summary>
    /// 治疗室模块服务注册入口
    /// </summary>
    public static class TreatmentRoomModule {
        /// <summary>
        /// 注册治疗室相关依赖服务
        /// </summary>
        public static void Register(IServiceCollection services) {
            services.AddScoped<ITreatmentRoomRepository, TreatmentRoomRepository>();
            services.AddScoped<ITreatmentRoomService, TreatmentRoomService>();
        }
    }
}
