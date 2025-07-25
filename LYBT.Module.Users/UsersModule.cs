using LYBT.Module.Users.Interfaces;
using LYBT.Module.Users.Repositories;
using LYBT.Module.Users.Services;
using LYBT.Module.Users.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LYBT.Module.Users {

    /// <summary>
    /// 用户模块依赖注入注册入口（供主程序统一集成）
    /// </summary>
    public static class UsersModule {

        /// <summary>
        /// 注册本模块所有服务到 DI 容器
        /// </summary>
        public static IServiceCollection AddUsersModule(this IServiceCollection services, string connectionString) {
            services.AddDbContext<UsersDbContext>(opts => opts.UseSqlServer(connectionString));
            services.AddScoped<IUserRepository, UserRepository>(); // 仓储层
            services.AddScoped<IUserService, UserService>();           // 业务层
            return services;
        }
    }
}