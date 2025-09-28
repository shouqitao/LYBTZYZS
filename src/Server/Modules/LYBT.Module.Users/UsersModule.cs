using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using FluentValidation;
using LYBT.Module.Users.Interfaces;
using LYBT.Module.Users.Services;
using LYBT.Module.Users.Repositories;
using LYBT.Module.Users.Validators;
using LYBT.Module.Users.Mapping;
using LYBT.Shared.Models.Contracts.Users;
using AutoMapper;

namespace LYBT.Module.Users
{
    /// <summary>
    /// 用户模块服务注册（遵循适度设计原则的简化版本）
    /// 仅提供小型中医诊所系统所需的基础用户管理功能
    /// </summary>
    public static class UsersModule
    {
        /// <summary>
        /// 注册用户模块服务
        /// </summary>
        public static IServiceCollection AddUsersModule(this IServiceCollection services, IConfiguration configuration)
        {
            // 仅注册必要的核心服务
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IUserService, UserService>();

            // 保留基础验证器（简化但有用）
            services.AddScoped<IValidator<UserCreateDto>, UserCreateDtoValidator>();
            services.AddScoped<IValidator<UserUpdateDto>, UserUpdateDtoValidator>();

            // 保留AutoMapper（简化但有用）
            services.AddAutoMapper(typeof(UserMappingProfile));

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

    /// <summary>
    /// 用户模块扩展方法（保持向后兼容）
    /// </summary>
    public static class UsersModuleExtensions
    {
        /// <summary>
        /// 注册本模块所有服务到 DI 容器（使用统一数据库上下文）
        /// UltraThink双层架构：Query(查询专业化) + Business(业务逻辑和CRUD)
        /// </summary>
        [Obsolete("建议使用 UsersModule.AddUsersModule 方法")]
        public static IServiceCollection AddUsersModuleServices(this IServiceCollection services)
        {
            // 为保持向后兼容，委托给新的静态方法
            var configuration = services.BuildServiceProvider().GetService<IConfiguration>()
                ?? throw new InvalidOperationException("IConfiguration not found in service collection");

            return UsersModule.AddUsersModule(services, configuration);
        }
    }
}