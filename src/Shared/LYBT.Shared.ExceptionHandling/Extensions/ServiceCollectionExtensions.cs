using LYBT.Shared.ExceptionHandling.Handlers;
using LYBT.Shared.ExceptionHandling.Mappers;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace LYBT.Shared.ExceptionHandling.Extensions;

/// <summary>
/// 服务注册扩展
/// consolidate-exception-handling: 统一DI注册入口
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// 添加服务端异常处理服务
    /// </summary>
    /// <remarks>
    /// 注册顺序决定异常处理链的执行顺序:
    /// 1. BusinessExceptionHandler - 处理AppException及其子类
    /// 2. SystemExceptionHandler - 兜底处理所有其他异常
    /// </remarks>
    public static IServiceCollection AddServerExceptionHandling(this IServiceCollection services)
    {
        // 注册异常处理器链
        services.AddExceptionHandler<BusinessExceptionHandler>();
        services.AddExceptionHandler<SystemExceptionHandler>();

        // 注册错误消息映射器
        services.AddSingleton<IErrorMessageMapper, ConfigurableErrorMessageMapper>();

        return services;
    }

    /// <summary>
    /// 添加Desktop端异常处理服务
    /// </summary>
    public static IServiceCollection AddDesktopExceptionHandling(this IServiceCollection services)
    {
        // 注册Desktop异常处理器
        services.AddSingleton<IDesktopExceptionHandler, DesktopExceptionHandler>();

        // 注册错误消息映射器
        services.AddSingleton<IErrorMessageMapper, ConfigurableErrorMessageMapper>();

        return services;
    }

    /// <summary>
    /// 添加共享异常处理服务（仅映射器，不含处理器）
    /// </summary>
    public static IServiceCollection AddSharedExceptionHandling(this IServiceCollection services)
    {
        // 仅注册错误消息映射器
        services.AddSingleton<IErrorMessageMapper, ConfigurableErrorMessageMapper>();

        return services;
    }
}
