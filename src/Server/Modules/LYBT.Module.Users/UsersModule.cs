using FluentValidation;
using LYBT.Infrastructure.Services.CrossModule;
using LYBT.Module.Users.Application.Commands;
using LYBT.Module.Users.Application.Validators;
using LYBT.Module.Users.Infrastructure;
using LYBT.Module.Users.Interfaces;
using LYBT.Module.Users.Services;
using LYBT.Shared.Validators.Users;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LYBT.Module.Users
{
    /// <summary>
    /// 用户模块服务注册
    /// 统一基于 Identity 的 ApplicationUser 管理
    /// </summary>
    public static class UsersModule
    {
        /// <summary>
        /// 注册用户模块服务
        /// </summary>
        public static IServiceCollection AddUsersModule(this IServiceCollection services, IConfiguration configuration)
        {
            // 注册 DbContext（模块级）
            services.AddDbContext<UsersDbContext>(options =>
                options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

            // UserManagerService 是 UserManager<T> 的薄包装
            services.AddScoped<IUserManagerService, UserManagerService>();

            // 注册新架构：IUserRepository
            services.AddScoped<IUserRepository, UserRepository>();

            // 注册跨模块服务（替代 CrossModuleService 中的用户查询逻辑）
            services.AddScoped<IUserCrossModuleService, UserCrossModuleService>();

            // 注册 MediatR（Application层）
            services.AddMediatR(cfg =>
                cfg.RegisterServicesFromAssembly(typeof(CreateUserCommand).Assembly));

            // 注册验证器 - 自动注册所有Validator（包括Application层的验证器）
            services.AddValidatorsFromAssemblyContaining<CreateUserValidator>();

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


