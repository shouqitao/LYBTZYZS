using Microsoft.Extensions.DependencyInjection;

namespace LYBT.Shared.ExceptionHandling.Handlers;

/// <summary>
/// 异常处理器注册扩展
/// A-31-C2: 统一注册入口（替代 ApiServiceCollectionExtensions 两行直登）
/// </summary>
public static class ExceptionHandlingServiceCollectionExtensions
{
    /// <summary>
    /// 注册异常处理器链（先注册的先处理，顺序保持 Business 先 System 后）
    /// </summary>
    public static IServiceCollection AddLybtExceptionHandling(this IServiceCollection services)
    {
        services.AddExceptionHandler<BusinessExceptionHandler>();
        services.AddExceptionHandler<SystemExceptionHandler>();
        return services;
    }
}
