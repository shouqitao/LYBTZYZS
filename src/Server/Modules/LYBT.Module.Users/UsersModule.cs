using LYBT.Module.Users.Interfaces;
using LYBT.Module.Users.Repositories;
using LYBT.Module.Users.Services;
using LYBT.Shared.Interfaces.Services;
using Microsoft.Extensions.DependencyInjection;

namespace LYBT.Module.Users
{

    /// <summary>
    /// 用户模块依赖注入注册入口（供主程序统一集成）
    /// </summary>
    public static class UsersModule
    {

        /// <summary>
        /// 注册本模块所有服务到 DI 容器（使用统一数据库上下文）
        /// UltraThink双层架构：Query(查询专业化) + Business(业务逻辑和CRUD)
        /// </summary>
        public static IServiceCollection AddUsersModuleServices(this IServiceCollection services)
        {
            // 仓储层
            services.AddScoped<IUserRepository, UserRepository>();

            // 统一用户服务 - 合并查询和业务逻辑
            services.AddScoped<LYBT.Module.Users.Interfaces.IUserService, UserService>();

            return services;
        }
    }
}
