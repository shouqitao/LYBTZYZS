using LYBT.Shared.Interfaces.Services;
using LYBT.Module.Users.Interfaces;
using LYBT.Module.Users.Repositories;
using LYBT.Module.Users.Services;
using LYBT.Module.Users.Helpers;
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
        /// UltraThink Helper模式：注册Helper类进行业务逻辑分离
        /// </summary>
        public static IServiceCollection AddUsersModuleServices(this IServiceCollection services)
        {
            // 仓储层
            services.AddScoped<IUserRepository, UserRepository>();
            
            // Helper类 - UltraThink分层架构
            services.AddScoped<UserQueryHelper>();
            services.AddScoped<UserValidationHelper>();
            services.AddScoped<UserBusinessHelper>();
            
            // 业务层 - UltraThink重构：现在继承BaseService并委托给Helper类
            services.AddScoped<IUserService, UserService>();
            
            return services;
        }

        /// <summary>
        /// 注册本模块所有服务到 DI 容器（保留原方法用于兼容性）
        /// </summary>
        [Obsolete("请使用 AddUsersModuleServices() 方法，统一使用 AppDbContext")]
        public static IServiceCollection AddUsersModule(this IServiceCollection services, string connectionString)
        {
            // 已弃用：改为使用统一的 AppDbContext
            // 仓储层
            services.AddScoped<IUserRepository, UserRepository>();
            
            // Helper类 - UltraThink分层架构
            services.AddScoped<UserQueryHelper>();
            services.AddScoped<UserValidationHelper>();
            services.AddScoped<UserBusinessHelper>();
            
            // 业务层 - UltraThink重构：现在继承BaseService并委托给Helper类
            services.AddScoped<IUserService, UserService>();
            
            return services;
        }
    }
}