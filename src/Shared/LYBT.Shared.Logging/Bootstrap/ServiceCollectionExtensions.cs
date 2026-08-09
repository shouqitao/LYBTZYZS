using LYBT.Shared.Logging.Correlation;
using LYBT.Shared.Logging.Management;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Serilog;

namespace LYBT.Shared.Logging.Bootstrap;

/// <summary>
/// 统一日志 DI 注册扩展
/// 合并原 Desktop Shell LoggingRegistrationExtensions（RegisterLogging）：
/// ICorrelationIdProvider + LoggingLevelManager + ILoggerFactory + ILogger&lt;&gt;
/// </summary>
public static class LoggingServiceCollectionExtensions
{
    /// <summary>
    /// 注册统一日志服务
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddLybtLogging(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        // CorrelationId 提供者单例（与静态 Logger 共用同一实例，解决零注册问题）
        services.AddSingleton<ICorrelationIdProvider>(_ => LoggingBootstrap.CorrelationIdProvider);

        // 日志级别管理器单例（与 Final Logger 的 LevelSwitch 共用同一实例，支持运行时动态调整）
        services.AddSingleton(LoggingBootstrap.LoggingLevelManager);

        // LoggerFactory 单例 + 开放泛型 ILogger<>（宿主已注册时 TryAdd 不覆盖，保持宿主管道）
        services.TryAddSingleton<ILoggerFactory>(_ => LoggingBootstrap.CreateLoggerFactory());
        services.TryAddSingleton(typeof(ILogger<>), typeof(Logger<>));

        return services;
    }
}
