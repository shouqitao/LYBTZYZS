using Microsoft.AspNetCore.Hosting;

namespace LYBT.WebAPI.Middleware
{
    /// <summary>
    /// 安全头中间件 - 添加安全相关的HTTP响应头
    /// </summary>
    public class SecurityHeadersMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<SecurityHeadersMiddleware> _logger;

        public SecurityHeadersMiddleware(
            RequestDelegate next,
            IWebHostEnvironment environment,
            ILogger<SecurityHeadersMiddleware> logger)
        {
            _next = next;
            _environment = environment;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // 添加安全响应头
            AddSecurityHeaders(context);

            await _next(context);
        }

        private void AddSecurityHeaders(HttpContext context)
        {
            var headers = context.Response.Headers;

            // X-Content-Type-Options: 防止MIME类型嗅探
            headers["X-Content-Type-Options"] = "nosniff";

            // X-Frame-Options: 防止点击劫持
            headers["X-Frame-Options"] = "DENY";

            // X-XSS-Protection: 启用XSS过滤（旧浏览器）
            headers["X-XSS-Protection"] = "1; mode=block";

            // Referrer-Policy: 控制引用信息
            headers["Referrer-Policy"] = "strict-origin-when-cross-origin";

            // Permissions-Policy: 控制浏览器功能权限
            headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";

            // 移除不必要的响应头
            headers.Remove("X-Powered-By");
            headers.Remove("Server");

            // Content-Security-Policy (CSP)
            if (_environment.IsProduction())
            {
                // 生产环境：严格的CSP策略
                headers["Content-Security-Policy"] = GetProductionCspPolicy();

                // HSTS: 强制HTTPS（仅生产环境）
                headers["Strict-Transport-Security"] = "max-age=31536000; includeSubDomains; preload";
            }
            else
            {
                // 开发环境：宽松的CSP策略
                headers["Content-Security-Policy-Report-Only"] = GetDevelopmentCspPolicy();
            }
        }

        /// <summary>
        /// 获取生产环境CSP策略
        /// </summary>
        private static string GetProductionCspPolicy()
        {
            var policies = new[]
            {
                "default-src 'self'",                              // 默认只允许同源
                "script-src 'self'",                               // JavaScript只允许同源（禁用unsafe-inline和unsafe-eval）
                "style-src 'self'",                                // CSS只允许同源（生产环境禁用unsafe-inline）
                "img-src 'self' data: https:",                     // 图片允许同源、data URL和HTTPS
                "font-src 'self'",                                  // 字体只允许同源
                "connect-src 'self'",                              // AJAX/WebSocket只允许同源
                "media-src 'none'",                                // 禁止音视频
                "object-src 'none'",                               // 禁止插件
                "frame-src 'none'",                                // 禁止iframe
                "base-uri 'self'",                                 // base标签只允许同源
                "form-action 'self'",                              // 表单提交只允许同源
                "frame-ancestors 'none'",                          // 禁止被嵌入iframe
                "worker-src 'self'",                               // Web Worker只允许同源
                "manifest-src 'self'",                             // Manifest只允许同源
                "upgrade-insecure-requests",                       // 自动升级HTTP到HTTPS
                "block-all-mixed-content",                         // 阻止混合内容
                "require-trusted-types-for 'script'"               // 要求可信类型（防XSS）
            };

            return string.Join("; ", policies);
        }

        /// <summary>
        /// 获取开发环境CSP策略（仅报告，不阻止）
        /// </summary>
        private static string GetDevelopmentCspPolicy()
        {
            var policies = new[]
            {
                "default-src 'self' 'unsafe-inline' 'unsafe-eval'",   // 开发环境允许内联和eval
                "script-src 'self' 'unsafe-inline' 'unsafe-eval'",
                "style-src 'self' 'unsafe-inline'",
                "img-src 'self' data: http: https:",
                "font-src 'self' data:",
                "connect-src 'self' ws: wss: http: https:",           // 允许WebSocket（热重载）
                "media-src 'self'",
                "object-src 'none'",
                "frame-src 'self'",
                "base-uri 'self'",
                "form-action 'self'",
                "frame-ancestors 'self'"
            };

            return string.Join("; ", policies);
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