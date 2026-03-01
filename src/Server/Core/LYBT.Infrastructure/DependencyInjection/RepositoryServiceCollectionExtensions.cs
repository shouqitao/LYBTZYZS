using Microsoft.Extensions.DependencyInjection;

namespace LYBT.Infrastructure.DependencyInjection
{
    /// <summary>
    /// Repository依赖注入统一扩展方法
    /// Project Standardization 3.0 - Task 1.5 Repository依赖注入标准化
    /// </summary>
    public static class RepositoryServiceCollectionExtensions
    {
        /// <summary>
        /// 注册指定类型的Repository
        /// </summary>
        /// <typeparam name="TRepository">Repository接口类型</typeparam>
        /// <typeparam name="TImplementation">Repository实现类型</typeparam>
        /// <param name="services">服务集合</param>
        /// <param name="lifetime">服务生命周期，默认为Scoped</param>
        /// <returns>服务集合</returns>
        public static IServiceCollection AddRepository<TRepository, TImplementation>(
            this IServiceCollection services,
            ServiceLifetime lifetime = ServiceLifetime.Scoped)
            where TRepository : class
            where TImplementation : class, TRepository
        {
            switch (lifetime)
            {
                case ServiceLifetime.Singleton:
                    services.AddSingleton<TRepository, TImplementation>();
                    break;
                case ServiceLifetime.Transient:
                    services.AddTransient<TRepository, TImplementation>();
                    break;
                case ServiceLifetime.Scoped:
                default:
                    services.AddScoped<TRepository, TImplementation>();
                    break;
            }

            return services;
        }

        /// <summary>
        /// 注册Server端核心Repository（基于BaseRepository）
        /// Phase 2 Task 2.4: 支持新旧两套接口注册（向后兼容）
        /// </summary>
        /// <param name="services">服务集合</param>
        /// <returns>服务集合</returns>
        public static IServiceCollection AddServerRepositories(this IServiceCollection services)
        {
            // 重要：不要注册开放泛型的基类（BaseRepository<>）
            // 原因：基类是抽象的，无法被DI容器实例化
            // 具体的Repository实现类已在各自模块的注册方法中注册

            // 注册核心Repository（如果存在）
            // 这里可以手动添加已知的Repository
            // services.AddScoped<IUserRepository, UserRepository>();
            // services.AddScoped<IPatientRepository, PatientRepository>();
            // 等等...

            return services;
        }

    }
}
