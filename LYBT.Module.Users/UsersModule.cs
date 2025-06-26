using Microsoft.Extensions.DependencyInjection;

namespace LYBT.Module.Users {

    /// <summary>
    /// 用户模块依赖注入注册入口（供主程序统一集成）
    /// </summary>
    public static class UsersModule {

        /// <summary>
        /// 注册本模块所有服务到 DI 容器
        /// </summary>
        public static IServiceCollection AddUsersModule(this IServiceCollection services) {
            services.AddSingleton<IUserRepository, UserRepository>(); // 仓储层
            services.AddScoped<IUserService, UserService>();           // 业务层
            return services;
        }
    }
}