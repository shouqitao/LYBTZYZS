using LYBT.Desktop.CardReader.Abstractions;
using LYBT.Desktop.CardReader.Services;
using Microsoft.Extensions.DependencyInjection;
using Prism.Ioc;
using Prism.Modularity;

namespace LYBT.Desktop.CardReader;

/// <summary>
/// 读卡器模块
/// 注册读卡器相关服务到DI容器
/// </summary>
public class CardReaderModule : IModule
{
    /// <summary>
    /// 模块初始化（服务已注册后调用）
    /// </summary>
    public void OnInitialized(IContainerProvider containerProvider)
    {
        // 模块初始化逻辑（如需要）
    }

    /// <summary>
    /// 注册服务
    /// </summary>
    public void RegisterTypes(IContainerRegistry containerRegistry)
    {
        // 注册工厂（单例）
        containerRegistry.RegisterSingleton<ICardReaderFactory, CardReaderFactory>();

        // 注册服务（单例，整个应用共享一个读卡器实例）
        containerRegistry.RegisterSingleton<ICardReaderService, CardReaderService>();
    }
}

/// <summary>
/// IServiceCollection扩展方法
/// 用于非Prism环境（如测试）注册服务
/// </summary>
public static class CardReaderServiceCollectionExtensions
{
    /// <summary>
    /// 注册读卡器服务
    /// </summary>
    public static IServiceCollection AddCardReaderServices(this IServiceCollection services)
    {
        services.AddSingleton<ICardReaderFactory, CardReaderFactory>();
        services.AddSingleton<ICardReaderService, CardReaderService>();
        return services;
    }
}
