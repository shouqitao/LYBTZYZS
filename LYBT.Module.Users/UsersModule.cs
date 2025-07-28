using LYBT.Module.Users.Data;
using LYBT.Module.Users.Interfaces;
using LYBT.Module.Users.Repositories;
using LYBT.Module.Users.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LYBT.Module.Users {

    /// <summary>
    /// 用户模块依赖注入注册入口（供主程序统一集成）
    /// </summary>
    public static class UsersModule {

        /// <summary>
        /// 注册本模块所有服务到 DI 容器（使用统一数据库上下文）
        /// </summary>
        public static IServiceCollection AddUsersModuleServices(this IServiceCollection services) {
            services.AddScoped<IUserRepository, UserRepository>(); // 仓储层
            services.AddScoped<IUserService, UserService>();           // 业务层
            return services;
        }

        /// <summary>
        /// 注册本模块所有服务到 DI 容器（保留原方法用于兼容性）
        /// </summary>
        [Obsolete("请使用 AddUsersModuleServices() 方法，统一使用 LybtDbContext")]
        public static IServiceCollection AddUsersModule(this IServiceCollection services, string connectionString) {
            services.AddDbContext<UserDbContext>(opts => opts.UseSqlServer(connectionString));
            services.AddScoped<IUserRepository, UserRepository>(); // 仓储层
            services.AddScoped<IUserService, UserService>();           // 业务层
            return services;
        }
    }
}