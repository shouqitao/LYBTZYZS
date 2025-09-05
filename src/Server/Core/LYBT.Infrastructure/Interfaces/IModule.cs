using Microsoft.Extensions.DependencyInjection;

namespace LYBT.Infrastructure.Interfaces
{
    /// <summary>
    /// 模块接口 - Solution级架构标准化
    /// 定义所有模块的统一注册规范，确保依赖注入架构一致性
    /// </summary>
    public interface IModule
    {
        /// <summary>
        /// 模块名称
        /// </summary>
        string Name { get; }

        /// <summary>
        /// 模块版本
        /// </summary>
        string Version { get; }

        /// <summary>
        /// 模块描述
        /// </summary>
        string Description { get; }

        /// <summary>
        /// 配置模块服务
        /// </summary>
        /// <param name="services">服务集合</param>
        /// <returns>配置后的服务集合</returns>
        IServiceCollection ConfigureServices(IServiceCollection services);
    }

    /// <summary>
    /// 模块基础抽象类 - 提供通用模块实现
    /// </summary>
    public abstract class BaseModule : IModule
    {
        public abstract string Name { get; }
        public virtual string Version => "1.0.0";
        public abstract string Description { get; }

        public virtual IServiceCollection ConfigureServices(IServiceCollection services)
        {
            // 注册仓储服务
            ConfigureRepositories(services);

            // 注册业务服务
            ConfigureBusinessServices(services);

            // 注册AutoMapper配置
            ConfigureMapping(services);

            // 注册模块特定服务
            ConfigureModuleSpecificServices(services);

            return services;
        }

        /// <summary>
        /// 配置仓储服务
        /// </summary>
        protected abstract void ConfigureRepositories(IServiceCollection services);

        /// <summary>
        /// 配置业务服务
        /// </summary>
        protected abstract void ConfigureBusinessServices(IServiceCollection services);

        /// <summary>
        /// 配置AutoMapper映射
        /// </summary>
        protected abstract void ConfigureMapping(IServiceCollection services);

        /// <summary>
        /// 配置模块特定服务 - 子类可选择重写
        /// </summary>
        protected virtual void ConfigureModuleSpecificServices(IServiceCollection services)
        {
            // 默认为空，子类可以重写添加特定服务
        }
    }

    /// <summary>
    /// 模块扩展方法
    /// </summary>
    public static class ModuleExtensions
    {
        /// <summary>
        /// 添加模块服务
        /// </summary>
        public static IServiceCollection AddModule<T>(this IServiceCollection services)
            where T : IModule, new()
        {
            var module = new T();
            return module.ConfigureServices(services);
        }

        /// <summary>
        /// 添加模块服务（使用实例）
        /// </summary>
        public static IServiceCollection AddModule(this IServiceCollection services, IModule module)
        {
            return module.ConfigureServices(services);
        }
    }
}
