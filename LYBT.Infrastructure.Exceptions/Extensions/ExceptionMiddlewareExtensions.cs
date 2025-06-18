using Microsoft.AspNetCore.Builder;

namespace LYBT.Infrastructure.Exceptions.Extensions {
    /// <summary>
    /// ExceptionMiddleware 扩展
    /// </summary>
    public static class ExceptionMiddlewareExtensions {
        public static IApplicationBuilder UseExceptionMiddleware(this IApplicationBuilder app) {
            return app.UseMiddleware<ExceptionMiddleware>();
        }
    }
}
