using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LYBT.EventBus.Module;

/// <summary>
/// 模块基础接口
/// 所有模块必须实现此接口
/// </summary>
/// <summary>
/// 模块基础接口
/// 所有模块必须实现此接口
/// </summary>
public interface IModule
{
    /// <summary>
    /// 模块描述符
    /// </summary>
    ModuleDescriptor Descriptor { get; }

    /// <summary>
    /// 模块当前状态
    /// </summary>
    ModuleState State { get; }

    /// <summary>
    /// 配置模块服务
    /// 在模块注册阶段调用，用于注册模块的服务和依赖
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configuration">配置对象</param>
    void ConfigureServices(IServiceCollection services, IConfiguration configuration);

    /// <summary>
    /// 配置模块
    /// 在应用程序配置阶段调用，用于配置模块的行为
    /// 使用object类型以支持不同的应用程序构建器类型
    /// </summary>
    /// <param name="app">应用程序构建器（如IApplicationBuilder）</param>
    /// <param name="environment">环境信息（如IWebHostEnvironment）</param>
    void Configure(object app, object environment);
}
