using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using MediatR;
using LYBT.Infrastructure.CQRS.Behaviors;
using LYBT.Infrastructure.CQRS.Commands.Users;
using LYBT.Infrastructure.CQRS.Queries.Users;

namespace LYBT.Infrastructure.CQRS.Extensions
{
    /// <summary>
    /// CQRS服务配置扩展 - UltraThink重构架构
    /// 配置MediatR和CQRS相关服务的依赖注入
    /// </summary>
    public static class CqrsServiceCollectionExtensions
    {
        /// <summary>
        /// 添加CQRS服务配置
        /// </summary>
        /// <param name="services">服务集合</param>
        /// <returns>服务集合</returns>
        public static IServiceCollection AddCqrsServices(this IServiceCollection services)
        {
            // 注册MediatR
            services.AddMediatR(cfg =>
            {
                // 注册当前程序集中的处理器
                cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
                
                // 也可以注册其他程序集
                // cfg.RegisterServicesFromAssemblies(typeof(CreateUserCommand).Assembly);
            });

            // 注册行为管道（Behaviors） - 按顺序执行
            services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
            services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
            services.AddTransient(typeof(IPipelineBehavior<,>), typeof(PerformanceBehavior<,>));
            services.AddTransient(typeof(IPipelineBehavior<,>), typeof(CachingBehavior<,>));

            // 显式注册命令处理器（可选，MediatR会自动发现）
            RegisterCommandHandlers(services);

            // 显式注册查询处理器（可选，MediatR会自动发现）
            RegisterQueryHandlers(services);

            return services;
        }

        /// <summary>
        /// 注册命令处理器
        /// </summary>
        private static void RegisterCommandHandlers(IServiceCollection services)
        {
            // 用户命令处理器
            services.AddScoped<CreateUserCommandHandler>();
            services.AddScoped<UpdateUserCommandHandler>();
            services.AddScoped<DeleteUserCommandHandler>();
            services.AddScoped<UpdateUserPasswordCommandHandler>();
            services.AddScoped<UpdateUserLastLoginCommandHandler>();
            services.AddScoped<BatchDeleteUsersCommandHandler>();
        }

        /// <summary>
        /// 注册查询处理器
        /// </summary>
        private static void RegisterQueryHandlers(IServiceCollection services)
        {
            // 用户查询处理器
            services.AddScoped<GetUserByIdQueryHandler>();
            services.AddScoped<GetUsersPagedQueryHandler>();
            services.AddScoped<GetUserByUsernameQueryHandler>();
            services.AddScoped<GetUserStatisticsQueryHandler>();
            services.AddScoped<SearchUsersQueryHandler>();
        }

        /// <summary>
        /// 添加CQRS验证服务
        /// </summary>
        /// <param name="services">服务集合</param>
        /// <returns>服务集合</returns>
        public static IServiceCollection AddCqrsValidation(this IServiceCollection services)
        {
            // 注册FluentValidation验证器（如果使用）
            // services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

            return services;
        }

        /// <summary>
        /// 添加CQRS性能监控
        /// </summary>
        /// <param name="services">服务集合</param>
        /// <returns>服务集合</returns>
        public static IServiceCollection AddCqrsPerformanceMonitoring(this IServiceCollection services)
        {
            // 可以注册性能监控相关服务
            // services.AddSingleton<IPerformanceMonitor, PerformanceMonitor>();
            
            return services;
        }

        /// <summary>
        /// 完整的CQRS配置
        /// </summary>
        /// <param name="services">服务集合</param>
        /// <returns>服务集合</returns>
        public static IServiceCollection AddFullCqrsConfiguration(this IServiceCollection services)
        {
            return services
                .AddCqrsServices()
                .AddCqrsValidation()
                .AddCqrsPerformanceMonitoring();
        }
    }
}