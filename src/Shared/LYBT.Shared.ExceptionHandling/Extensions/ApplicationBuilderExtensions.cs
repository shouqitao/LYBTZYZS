using Microsoft.AspNetCore.Builder;

namespace LYBT.Shared.ExceptionHandling.Extensions;

/// <summary>
/// 应用程序构建器扩展
/// consolidate-exception-handling: 统一中间件配置入口
/// </summary>
public static class ApplicationBuilderExtensions
{
    /// <summary>
    /// 使用异常处理中间件
    /// </summary>
    /// <remarks>
    /// 必须在管道早期调用，以确保能捕获所有异常
    /// </remarks>
    public static IApplicationBuilder UseExceptionHandlingMiddleware(this IApplicationBuilder app)
    {
        // 使用ASP.NET Core内置的异常处理中间件
        // 它会调用注册的IExceptionHandler链
        app.UseExceptionHandler(options => { });

        return app;
    }
}
