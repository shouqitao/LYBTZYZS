using LYBT.Module.Users.Interfaces;
using LYBT.Module.Users.Repositories;
using LYBT.Module.Users.Services;
using LYBT.Module.Users.Services.Interfaces;
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

            // UltraThink双层架构服务 - 查询和业务逻辑分离
            services.AddScoped<IUserQueryService, UserQueryService>();
            services.AddScoped<IUserBusinessService, UserBusinessService>();

            // 主服务 - UltraThink纯委托模式，委托给专业服务层
            services.AddScoped<IUserService, UserService>();

            return services;
        }
    }
}
