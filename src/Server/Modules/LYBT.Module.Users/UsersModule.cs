using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using LYBT.Module.Users.Interfaces;
using LYBT.Module.Users.Services;
using LYBT.Module.Users.Repositories;
using LYBT.Module.Users.Configuration;
using LYBT.Module.Users.HealthChecks;
using AutoMapper;

namespace LYBT.Module.Users
{
    /// <summary>
    /// 用户模块
    /// 负责用户管理相关的业务逻辑和服务注册
    /// </summary>
    /// <summary>
/// 用户模块服务注册（简化版本）
/// </summary>
public class UsersModule
{
    /// <summary>
    /// 注册用户模块服务
    /// </summary>
    public static IServiceCollection AddUsersModule(this IServiceCollection services, IConfiguration configuration)
    {
        // 注册仓储
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        
        // 注册服务
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IUserQueryService, UserQueryService>();
        
        // 注册验证器
        services.AddScoped<IValidator<UserCreateDto>, UserCreateDtoValidator>();
        services.AddScoped<IValidator<UserUpdateDto>, UserUpdateDtoValidator>();
        
        // 注册AutoMapper配置
        services.AddAutoMapper(typeof(UserMappingProfile));
        
        // 注册模块特定的配置
        services.Configure<UserModuleOptions>(configuration.GetSection("Modules:Users"));
        
        return services;
    }
    
    /// <summary>
    /// 配置用户模块中间件（如有需要）
    /// </summary>
    public static IApplicationBuilder UseUsersModule(this IApplicationBuilder app)
    {
        // 当前无特殊中间件需求
        return app;
    }
    
    /// <summary>
    /// 验证模块健康状态
    /// </summary>
    public static IHealthChecksBuilder AddUsersModuleHealthCheck(this IHealthChecksBuilder builder)
    {
        return builder.AddCheck<UsersModuleHealthCheck>("users_module");
    }
}

    /// <summary>
    /// 用户模块扩展方法（保持向后兼容）
    /// </summary>
    public class UsersModuleExtensions
    {
        /// <summary>
        /// 注册本模块所有服务到 DI 容器（使用统一数据库上下文）
        /// UltraThink双层架构：Query(查询专业化) + Business(业务逻辑和CRUD)
        /// </summary>
        [Obsolete("建议使用 UsersModule 类替代静态扩展方法")]
        public static IServiceCollection AddUsersModuleServices(this IServiceCollection services)
        {
            // 为保持向后兼容，委托给模块实例
            var module = new UsersModule();
            var configuration = services.BuildServiceProvider().GetService<IConfiguration>() 
                ?? throw new InvalidOperationException("IConfiguration not found in service collection");
            
            module.ConfigureServices(services, configuration);
            return services;
        }
    }
}