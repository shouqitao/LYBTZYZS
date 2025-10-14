using Prism.Ioc;
using System.Reflection;

namespace LYBT.Desktop.Infrastructure.DependencyInjection
{
    /// <summary>
    /// Repository依赖注入统一扩展方法（Client端）
    /// Project Standardization 3.0 - Task 1.5 Repository依赖注入标准化
    /// </summary>
    public static class RepositoryContainerRegistryExtensions
    {
        /// <summary>
        /// 注册所有Repository服务
        /// 自动扫描并注册所有Repository实现
        /// </summary>
        /// <param name="containerRegistry">容器注册器</param>
        /// <param name="assemblies">要扫描的程序集，默认为当前程序集</param>
        /// <returns>容器注册器</returns>
        public static IContainerRegistry RegisterRepositories(this IContainerRegistry containerRegistry, params Assembly[] assemblies)
        {
            var assembliesToScan = assemblies.Length > 0
                ? assemblies
                : new[] { Assembly.GetExecutingAssembly() };

            return RegisterRepositoriesInternal(containerRegistry, assembliesToScan);
        }

        /// <summary>
        /// 注册指定类型的Repository
        /// </summary>
        /// <typeparam name="TRepository">Repository接口类型</typeparam>
        /// <typeparam name="TImplementation">Repository实现类型</typeparam>
        /// <param name="containerRegistry">容器注册器</param>
        /// <param name="useSingleton">是否使用单例模式，默认为true</param>
        /// <returns>容器注册器</returns>
        public static IContainerRegistry RegisterRepository<TRepository, TImplementation>(
            this IContainerRegistry containerRegistry,
            bool useSingleton = true)
            where TRepository : class
            where TImplementation : class, TRepository
        {
            if (useSingleton)
            {
                containerRegistry.RegisterSingleton<TRepository, TImplementation>();
            }
            else
            {
                containerRegistry.Register<TRepository, TImplementation>();
            }

            return containerRegistry;
        }

        /// <summary>
        /// 注册Repository基类支持
        /// </summary>
        /// <param name="containerRegistry">容器注册器</param>
        /// <returns>容器注册器</returns>
        public static IContainerRegistry RegisterRepositoryBase(this IContainerRegistry containerRegistry)
        {
            // 注册RepositoryBase泛型支持（如果需要）
            // 注意：Prism不支持直接的泛型注册，这里可以根据需要调整

            return containerRegistry;
        }

        /// <summary>
        /// 注册Client端核心Repository
        /// </summary>
        /// <param name="containerRegistry">容器注册器</param>
        /// <returns>容器注册器</returns>
        public static IContainerRegistry RegisterClientRepositories(this IContainerRegistry containerRegistry)
        {
            // 手动注册已知的Repository（确保正确的生命周期）
            // 这些通常在模块级别注册，但可以在这里提供默认配置

            // 示例：
            // containerRegistry.RegisterSingleton<IUserRepository, UserRepository>();
            // containerRegistry.RegisterSingleton<IPatientRepository, PatientRepository>();
            // containerRegistry.RegisterSingleton<IConsultationRepository, ConsultationRepository>();
            // containerRegistry.RegisterSingleton<IPrescriptionRepository, PrescriptionRepository>();
            // containerRegistry.RegisterSingleton<IFormulaRepository, FormulaRepository>();
            // containerRegistry.RegisterSingleton<IHerbRepository, HerbRepository>();
            // containerRegistry.RegisterSingleton<IMedicalCaseRepository, MedicalCaseRepository>();

            return containerRegistry;
        }

        #region 私有方法

        /// <summary>
        /// 内部Repository注册实现
        /// </summary>
        private static IContainerRegistry RegisterRepositoriesInternal(IContainerRegistry containerRegistry, Assembly[] assemblies)
        {
            foreach (var assembly in assemblies)
            {
                // 扫描Repository接口和实现
                var repositoryTypes = assembly.GetTypes()
                    .Where(t => t.IsClass && !t.IsAbstract && t.Name.EndsWith("Repository"))
                    .ToList();

                foreach (var repositoryType in repositoryTypes)
                {
                    // 查找对应的接口
                    var interfaceType = repositoryType.GetInterfaces()
                        .FirstOrDefault(i => i.Name.EndsWith("Repository") && i.IsInterface);

                    if (interfaceType != null)
                    {
                        // 默认使用单例模式注册Repository
                        containerRegistry.RegisterSingleton(interfaceType, repositoryType);
                    }
                }
            }

            return containerRegistry;
        }

        #endregion
    }

    /// <summary>
    /// Repository注册辅助类
    /// </summary>
    public static class RepositoryRegistrationHelper
    {
        /// <summary>
        /// 批量注册Repository模块
        /// </summary>
        /// <param name="containerRegistry">容器注册器</param>
        /// <param name="repositoryTypes">Repository类型字典（接口 -> 实现）</param>
        /// <returns>容器注册器</returns>
        public static IContainerRegistry RegisterRepositoryModules(
            this IContainerRegistry containerRegistry,
            Dictionary<Type, Type> repositoryTypes)
        {
            foreach (var kvp in repositoryTypes)
            {
                var interfaceType = kvp.Key;
                var implementationType = kvp.Value;

                if (interfaceType.IsInterface && implementationType.IsClass &&
                    interfaceType.IsAssignableFrom(implementationType))
                {
                    containerRegistry.RegisterSingleton(interfaceType, implementationType);
                }
            }

            return containerRegistry;
        }

        /// <summary>
        /// 获取所有Repository类型映射
        /// </summary>
        /// <param name="assembly">要扫描的程序集</param>
        /// <returns>Repository类型字典</returns>
        public static Dictionary<Type, Type> GetRepositoryTypeMappings(Assembly assembly)
        {
            var mappings = new Dictionary<Type, Type>();

            var repositoryTypes = assembly.GetTypes()
                .Where(t => t.IsClass && !t.IsAbstract && t.Name.EndsWith("Repository"))
                .ToList();

            foreach (var repositoryType in repositoryTypes)
            {
                var interfaceType = repositoryType.GetInterfaces()
                    .FirstOrDefault(i => i.Name.EndsWith("Repository") && i.IsInterface);

                if (interfaceType != null)
                {
                    mappings[interfaceType] = repositoryType;
                }
            }

            return mappings;
        }
    }
}