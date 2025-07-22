using Microsoft.AspNetCore.Builder;

namespace LYBT.Infrastructure.Exceptions.Extensions {

    /// <summary>
    /// ExceptionMiddleware 扩展
    /// </summary>
    public static class ExceptionMiddlewareExtensions {

/// <summary>
/// 执行UseExceptionMiddleware操作。
/// </summary>
/// <param name="app">参数app</param>
/// <returns>返回值</returns>
        public static IApplicationBuilder UseExceptionMiddleware(this IApplicationBuilder app) {
            return app.UseMiddleware<ExceptionMiddleware>();
        }
    }
}
