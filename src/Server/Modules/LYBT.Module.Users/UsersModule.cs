using FluentValidation;
using LYBT.Module.Users.Interfaces;
using LYBT.Module.Users.Mapping;
using LYBT.Module.Users.Repositories;
using LYBT.Module.Users.Services;
using LYBT.Shared.Validators.Users;
using LYBT.Shared.Models.Contracts.Users;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using LYBT.Infrastructure.DependencyInjection;

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
            services.AddRepository<IUserRepository, UserRepository>();

            // 注册服务实现类（统一使用Shared接口）
            services.AddScoped<IUserService, UserService>();

            // 注册验证器 - 自动注册所有Validator
            services.AddValidatorsFromAssemblyContaining<UserInputDtoValidator>();

            // AutoMapper配置已在UnifiedServiceRegistration中集中注册

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
