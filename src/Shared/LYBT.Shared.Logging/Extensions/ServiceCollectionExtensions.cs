using LYBT.Shared.Logging.Abstractions;
using LYBT.Shared.Logging.Management;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Serilog.Events;

namespace LYBT.Shared.Logging.Extensions;

/// <summary>
/// IServiceCollection扩展方法
/// 提供日志相关服务的DI注册
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// 添加共享日志服务
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="defaultLevel">默认日志级别</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddSharedLogging(
        this IServiceCollection services,
        LogEventLevel defaultLevel = LogEventLevel.Information)
    {
        ArgumentNullException.ThrowIfNull(services);

        // 注册LoggingLevelManager为单例
        services.TryAddSingleton(new LoggingLevelManager(defaultLevel));

        return services;
    }

    /// <summary>
    /// 添加共享日志服务并指定CorrelationId提供者
    /// </summary>
    /// <typeparam name="TCorrelationIdProvider">CorrelationId提供者类型</typeparam>
    /// <param name="services">服务集合</param>
    /// <param name="defaultLevel">默认日志级别</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddSharedLogging<TCorrelationIdProvider>(
        this IServiceCollection services,
        LogEventLevel defaultLevel = LogEventLevel.Information)
        where TCorrelationIdProvider : class, ICorrelationIdProvider
    {
        ArgumentNullException.ThrowIfNull(services);

        // 注册CorrelationId提供者
        services.TryAddSingleton<ICorrelationIdProvider, TCorrelationIdProvider>();

        // 注册LoggingLevelManager
        services.TryAddSingleton(new LoggingLevelManager(defaultLevel));

        return services;
    }

    /// <summary>
    /// 添加AsyncLocal的CorrelationId提供者(适用于Desktop端)
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddAsyncLocalCorrelationIdProvider(
        this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddSingleton<ICorrelationIdProvider, AsyncLocalCorrelationIdProvider>();

        return services;
    }
}
