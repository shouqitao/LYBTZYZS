using System.Diagnostics;
using Serilog.Context;

namespace LYBT.WebAPI.Middleware
{
    /// <summary>
    /// CorrelationId中间件 - 实现端到端请求追踪
    /// refactor-logging-system: 从请求头读取或自动生成CorrelationId，并通过LogContext传递
    /// LOG-013: 支持W3C traceparent header进行分布式追踪
    /// </summary>
    public class CorrelationIdMiddleware
    {
        /// <summary>
        /// CorrelationId HTTP请求/响应头名称
        /// </summary>
        public const string CorrelationIdHeader = "X-Correlation-ID";

        /// <summary>
        /// W3C Trace Context标准头名称
        /// LOG-013: 分布式追踪Header传递
        /// </summary>
        public const string TraceparentHeader = "traceparent";

        /// <summary>
        /// HttpContext.Items中存储CorrelationId的键名
        /// </summary>
        public const string CorrelationIdItemKey = "CorrelationId";

        private readonly RequestDelegate _next;
        private readonly ILogger<CorrelationIdMiddleware> _logger;

        public CorrelationIdMiddleware(
            RequestDelegate next,
            ILogger<CorrelationIdMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // LOG-013: 优先从traceparent header提取CorrelationId (W3C Trace Context)
            var correlationId = context.Request.Headers[TraceparentHeader].FirstOrDefault();
            
            // 回退到X-Correlation-ID header
            if (string.IsNullOrWhiteSpace(correlationId))
            {
                correlationId = context.Request.Headers[CorrelationIdHeader].FirstOrDefault();
            }

            // 如果都没有，生成新的
            if (string.IsNullOrWhiteSpace(correlationId))
            {
                // 使用短格式GUID，便于日志展示
                correlationId = Guid.NewGuid().ToString("N")[..12];
            }

            // 设置到HttpContext.TraceIdentifier
            context.TraceIdentifier = correlationId;

            // 存储到HttpContext.Items，供后续中间件和Controller使用
            context.Items[CorrelationIdItemKey] = correlationId;

            // LOG-013: 添加到响应头，便于客户端关联
            context.Response.OnStarting(() =>
            {
                context.Response.Headers[CorrelationIdHeader] = correlationId;
                return Task.CompletedTask;
            });

            // 使用LogContext.PushProperty将CorrelationId注入到所有日志
            using (LogContext.PushProperty("CorrelationId", correlationId))
            {
                await _next(context);
            }
        }
    }

    /// <summary>
    /// CorrelationId中间件扩展方法
    /// </summary>
    public static class CorrelationIdMiddlewareExtensions
    {
        /// <summary>
        /// 使用CorrelationId中间件
        /// 建议在请求管道早期注册，确保所有后续日志都包含CorrelationId
        /// </summary>
        public static IApplicationBuilder UseCorrelationId(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<CorrelationIdMiddleware>();
        }

        /// <summary>
        /// 从HttpContext获取当前请求的CorrelationId
        /// </summary>
        /// <param name="context">HTTP上下文</param>
        /// <returns>CorrelationId，如果不存在返回"N/A"</returns>
        public static string GetCorrelationId(this HttpContext context)
        {
            return context.Items[CorrelationIdMiddleware.CorrelationIdItemKey]?.ToString() ?? "N/A";
        }
    }
}
