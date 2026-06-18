using FluentValidation;
using LYBT.Module.Users.Interfaces;
using LYBT.Module.Users.Services;
using LYBT.Shared.Validators.Users;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LYBT.Module.Users
{
    /// <summary>
    /// 用户模块服务注册
    /// 统一基于 Identity 的 ApplicationUser 管理（旧三层的 UserService/UserRepository 已删除）
    /// </summary>
    public static class UsersModule
    {
        /// <summary>
        /// 注册用户模块服务
        /// </summary>
        public static IServiceCollection AddUsersModule(this IServiceCollection services, IConfiguration configuration)
        {
            // UserManagerService 是 UserManager<T> 的薄包装，供 UsersController 使用
            services.AddScoped<IUserManagerService, UserManagerService>();

            // 注册验证器 - 自动注册所有Validator
            services.AddValidatorsFromAssemblyContaining<UserInputDtoValidator>();

            return services;
        }

        /// <summary>
        /// 配置用户模块中间件
        /// </summary>
        public static IApplicationBuilder UseUsersModule(this IApplicationBuilder app)
        {
            return app;
        }
    }
}
