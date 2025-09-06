using System;
using System.Collections.Generic;
using Prism.Ioc;

namespace LYBT.Desktop.Services.Registration;

/// <summary>
/// 模块服务注册器接口
/// 用于实现模块化的服务自动发现和注册
/// </summary>
public interface IModuleServiceRegistrar
{
    /// <summary>
    /// 注册指定模块的所有服务
    /// </summary>
    /// <typeparam name="TModule">模块类型</typeparam>
    /// <param name="registry">容器注册器</param>
    void RegisterModuleServices<TModule>(IContainerRegistry registry) where TModule : class;

    /// <summary>
    /// 注册所有发现的模块服务
    /// </summary>
    /// <param name="registry">容器注册器</param>
    void RegisterAllDiscoveredServices(IContainerRegistry registry);

    /// <summary>
    /// 检查指定类型是否可以注册为服务
    /// </summary>
    /// <param name="serviceType">服务类型</param>
    /// <param name="implementationType">实现类型</param>
    /// <returns>是否可以注册</returns>
    bool CanRegisterService(Type serviceType, Type implementationType);

    /// <summary>
    /// 获取所有发现的服务注册信息
    /// </summary>
    /// <returns>服务注册信息列表</returns>
    IEnumerable<ServiceRegistrationInfo> GetDiscoveredServices();
}

/// <summary>
/// 服务注册信息
/// </summary>
public record ServiceRegistrationInfo(
    Type ServiceType,
    Type ImplementationType, 
    string ModuleName,
    ServiceLifetime Lifetime);

/// <summary>
/// 服务生命周期
/// </summary>
public enum ServiceLifetime
{
    Singleton,
    Transient,
    Scoped
}