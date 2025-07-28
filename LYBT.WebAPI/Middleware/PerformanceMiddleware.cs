using System.Diagnostics;

namespace LYBT.WebAPI.Middleware {

    /// <summary>
    /// 性能监控中间件
    /// </summary>
    public class PerformanceMiddleware {
        private readonly RequestDelegate _next;
        private readonly ILogger<PerformanceMiddleware> _logger;

        public PerformanceMiddleware(RequestDelegate next, ILogger<PerformanceMiddleware> logger) {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context) {
            var stopwatch = Stopwatch.StartNew();

            // 记录请求开始
            var requestPath = context.Request.Path;
            var requestMethod = context.Request.Method;

            await _next(context);

            stopwatch.Stop();
            var elapsed = stopwatch.ElapsedMilliseconds;

            // 记录慢请求（超过1秒）
            if (elapsed > 1000) {
                _logger.LogWarning("慢请求检测: {Method} {Path} 耗时 {ElapsedMs}ms",
                    requestMethod, requestPath, elapsed);
            }

            // 记录所有请求的性能信息（Debug级别）
            _logger.LogDebug("请求完成: {Method} {Path} 状态码: {StatusCode} 耗时: {ElapsedMs}ms",
                requestMethod, requestPath, context.Response.StatusCode, elapsed);

            // 添加响应头包含执行时间
            context.Response.Headers.Add("X-Response-Time", $"{elapsed}ms");
        }
    }

    /// <summary>
    /// 性能监控中间件扩展方法
    /// </summary>
    public static class PerformanceMiddlewareExtensions {

        public static IApplicationBuilder UsePerformanceMonitoring(this IApplicationBuilder builder) {
            return builder.UseMiddleware<PerformanceMiddleware>();
        }
    }
}