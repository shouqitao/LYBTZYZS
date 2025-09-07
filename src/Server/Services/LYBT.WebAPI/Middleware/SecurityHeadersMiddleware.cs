using LYBT.Infrastructure.Configuration.Options;
using Microsoft.Extensions.Options;

namespace LYBT.WebAPI.Middleware
{

    /// <summary>
    /// 安全头中间件
    /// </summary>
    public class SecurityHeadersMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly SecurityOptions _securityOptions;
        private readonly ILogger<SecurityHeadersMiddleware> _logger;

        public SecurityHeadersMiddleware(
            RequestDelegate next,
            IOptions<SecurityOptions> securityOptions,
            ILogger<SecurityHeadersMiddleware> logger)
        {
            _next = next;
            _securityOptions = securityOptions.Value;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                // 添加安全头
                AddSecurityHeaders(context.Response);

                // 隐藏服务器信息
                if (_securityOptions.Environment.HideServerInfo)
                {
                    context.Response.Headers.Remove("Server");
                    context.Response.Headers.Remove("X-Powered-By");
                    context.Response.Headers.Remove("X-AspNet-Version");
                    context.Response.Headers.Remove("X-AspNetMvc-Version");
                }

                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "安全头中间件处理错误");
                await _next(context);
            }
        }

        private void AddSecurityHeaders(HttpResponse response)
        {
            var headers = _securityOptions.SecurityHeaders;

            // Content Security Policy
            if (!string.IsNullOrEmpty(headers.ContentSecurityPolicy))
            {
                response.Headers.TryAdd("Content-Security-Policy", headers.ContentSecurityPolicy);
            }

            // X-Frame-Options
            if (!string.IsNullOrEmpty(headers.XFrameOptions))
            {
                response.Headers.TryAdd("X-Frame-Options", headers.XFrameOptions);
            }

            // X-Content-Type-Options
            if (!string.IsNullOrEmpty(headers.XContentTypeOptions))
            {
                response.Headers.TryAdd("X-Content-Type-Options", headers.XContentTypeOptions);
            }

            // Referrer-Policy
            if (!string.IsNullOrEmpty(headers.ReferrerPolicy))
            {
                response.Headers.TryAdd("Referrer-Policy", headers.ReferrerPolicy);
            }

            // Permissions-Policy
            if (!string.IsNullOrEmpty(headers.PermissionsPolicy))
            {
                response.Headers.TryAdd("Permissions-Policy", headers.PermissionsPolicy);
            }

            // X-XSS-Protection (虽然已弃用，但为了向后兼容)
            response.Headers.TryAdd("X-XSS-Protection", "0");

            // 确保不缓存敏感信息
            if (!response.Headers.ContainsKey("Cache-Control"))
            {
                response.Headers.TryAdd("Cache-Control", "no-cache, no-store, must-revalidate, private");
                response.Headers.TryAdd("Pragma", "no-cache");
                response.Headers.TryAdd("Expires", "0");
            }
        }
    }

    /// <summary>
    /// 安全头中间件扩展
    /// </summary>
    public static class SecurityHeadersMiddlewareExtensions
    {

        /// <summary>
        /// 使用安全头中间件
        /// </summary>
        public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<SecurityHeadersMiddleware>();
        }
    }
}
