using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using System.Net;
using System.Text;
using System.Diagnostics;

namespace LYBT.Infrastructure.Security
{
    /// <summary>
    /// 综合安全中间件 - UltraThink重构安全防护
    /// 集成多种安全措施：HTTPS重定向、安全头部、请求验证等
    /// </summary>
    public class SecurityMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<SecurityMiddleware> _logger;
        private readonly IInputValidationService _validationService;

        private readonly SecurityMiddlewareOptions _options;

        public SecurityMiddleware(
            RequestDelegate next,
            ILogger<SecurityMiddleware> logger,
            IInputValidationService validationService,
            SecurityMiddlewareOptions options)
        {
            _next = next;
            _logger = logger;
            _validationService = validationService;

            _options = options;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var stopwatch = Stopwatch.StartNew();
            var requestId = Guid.NewGuid().ToString("N")[..8];
            
            try
            {
                // 设置请求ID
                context.Items["RequestId"] = requestId;
                context.Response.Headers["X-Request-ID"] = requestId;

                // HTTPS重定向
                if (_options.RequireHttps && !context.Request.IsHttps)
                {
                    await RedirectToHttps(context);
                    return;
                }

                // 添加安全头部
                AddSecurityHeaders(context);

                // 验证请求
                var validationResult = await ValidateRequestAsync(context);
                if (!validationResult.IsValid)
                {
                    await HandleInvalidRequest(context, validationResult, requestId);
                    return;
                }

                // 记录API访问（排除静态资源）
                if (ShouldAuditRequest(context))
                {
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await LogApiAccessAsync(context, requestId, stopwatch.ElapsedMilliseconds, true);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "记录API访问审计失败");
                        }
                    });
                }

                await _next(context);

                stopwatch.Stop();
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                await HandleException(context, ex, requestId, stopwatch.ElapsedMilliseconds);
                throw;
            }
        }

        /// <summary>
        /// HTTPS重定向
        /// </summary>
        private async Task RedirectToHttps(HttpContext context)
        {
            var httpsUrl = $"https://{context.Request.Host}{context.Request.Path}{context.Request.QueryString}";
            
            context.Response.StatusCode = (int)HttpStatusCode.MovedPermanently;
            context.Response.Headers.Location = httpsUrl;
            
            await context.Response.WriteAsync("Redirecting to HTTPS...");
            
            _logger.LogInformation("HTTPS重定向: {OriginalUrl} -> {HttpsUrl}", 
                context.Request.GetDisplayUrl(), httpsUrl);
        }

        /// <summary>
        /// 添加安全头部
        /// </summary>
        private void AddSecurityHeaders(HttpContext context)
        {
            var response = context.Response.Headers;
            
            // 基础安全头部
            if (!response.ContainsKey("X-Content-Type-Options"))
                response["X-Content-Type-Options"] = "nosniff";
            
            if (!response.ContainsKey("X-Frame-Options"))
                response["X-Frame-Options"] = _options.XFrameOptions;
            
            if (!response.ContainsKey("X-XSS-Protection"))
                response["X-XSS-Protection"] = "1; mode=block";
            
            if (!response.ContainsKey("Referrer-Policy"))
                response["Referrer-Policy"] = "strict-origin-when-cross-origin";
            
            // 内容安全策略
            if (!string.IsNullOrEmpty(_options.ContentSecurityPolicy))
            {
                response["Content-Security-Policy"] = _options.ContentSecurityPolicy;
            }

            // HSTS (HTTP Strict Transport Security)
            if (_options.RequireHttps && context.Request.IsHttps)
            {
                response["Strict-Transport-Security"] = 
                    $"max-age={_options.HstsMaxAge}; includeSubDomains{(_options.HstsPreload ? "; preload" : "")}";
            }

            // 权限策略
            if (!string.IsNullOrEmpty(_options.PermissionsPolicy))
            {
                response["Permissions-Policy"] = _options.PermissionsPolicy;
            }

            // 隐藏服务器信息
            response.Remove("Server");
            response.Remove("X-Powered-By");
            response.Remove("X-AspNet-Version");
        }

        /// <summary>
        /// 验证请求
        /// </summary>
        private async Task<RequestValidationResult> ValidateRequestAsync(HttpContext context)
        {
            var result = new RequestValidationResult { IsValid = true };
            
            try
            {
                // 检查请求大小
                if (context.Request.ContentLength > _options.MaxRequestSize)
                {
                    result.IsValid = false;
                    result.Reason = $"请求体大小超过限制 ({_options.MaxRequestSize} bytes)";
                    result.ThreatLevel = "Medium";
                    return result;
                }

                // 验证User-Agent
                if (_options.RequireUserAgent && string.IsNullOrEmpty(context.Request.Headers.UserAgent))
                {
                    result.IsValid = false;
                    result.Reason = "缺少User-Agent头部";
                    result.ThreatLevel = "Low";
                    return result;
                }

                // 检查请求头
                ValidateHeaders(context, result);
                if (!result.IsValid) return result;

                // 验证查询参数
                ValidateQueryParameters(context, result);
                if (!result.IsValid) return result;

                // 验证表单数据（如果是POST请求）
                if (context.Request.Method == "POST" && context.Request.HasFormContentType)
                {
                    await ValidateFormData(context, result);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "请求验证过程中发生错误");
                result.IsValid = false;
                result.Reason = "请求验证失败";
                result.ThreatLevel = "$1";
                return result;
            }
        }

        /// <summary>
        /// 验证请求头
        /// </summary>
        private void ValidateHeaders(HttpContext context, RequestValidationResult result)
        {
            foreach (var header in context.Request.Headers)
            {
                // 检查头部名称
                var nameValidation = _validationService.ValidateAndSanitize(header.Key, InputType.General);
                if (!nameValidation.IsValid)
                {
                    result.IsValid = false;
                    result.Reason = $"请求头名称包含恶意内容: {header.Key}";
                    result.ThreatLevel = "High";
                    result.ThreatType = nameValidation.ThreatType;
                    return;
                }

                // 检查头部值
                foreach (var value in header.Value)
                {
                    if (string.IsNullOrEmpty(value)) continue;

                    var valueValidation = _validationService.ValidateAndSanitize(value, InputType.General);
                    if (!valueValidation.IsValid)
                    {
                        result.IsValid = false;
                        result.Reason = $"请求头值包含恶意内容: {header.Key}";
                        result.ThreatLevel = "High";
                        result.ThreatType = valueValidation.ThreatType;
                        return;
                    }
                }
            }
        }

        /// <summary>
        /// 验证查询参数
        /// </summary>
        private void ValidateQueryParameters(HttpContext context, RequestValidationResult result)
        {
            foreach (var param in context.Request.Query)
            {
                // 检查参数名
                var nameValidation = _validationService.ValidateAndSanitize(param.Key, InputType.General);
                if (!nameValidation.IsValid)
                {
                    result.IsValid = false;
                    result.Reason = $"查询参数名包含恶意内容: {param.Key}";
                    result.ThreatLevel = "High";
                    result.ThreatType = nameValidation.ThreatType;
                    return;
                }

                // 检查参数值
                foreach (var value in param.Value)
                {
                    if (string.IsNullOrEmpty(value)) continue;

                    var valueValidation = _validationService.ValidateAndSanitize(value, InputType.General);
                    if (!valueValidation.IsValid)
                    {
                        result.IsValid = false;
                        result.Reason = $"查询参数值包含恶意内容: {param.Key}";
                        result.ThreatLevel = "High";
                        result.ThreatType = valueValidation.ThreatType;
                        return;
                    }
                }
            }
        }

        /// <summary>
        /// 验证表单数据
        /// </summary>
        private async Task ValidateFormData(HttpContext context, RequestValidationResult result)
        {
            try
            {
                var form = await context.Request.ReadFormAsync();
                
                foreach (var field in form)
                {
                    // 检查字段名
                    var nameValidation = _validationService.ValidateAndSanitize(field.Key, InputType.General);
                    if (!nameValidation.IsValid)
                    {
                        result.IsValid = false;
                        result.Reason = $"表单字段名包含恶意内容: {field.Key}";
                        result.ThreatLevel = "High";
                        result.ThreatType = nameValidation.ThreatType;
                        return;
                    }

                    // 检查字段值
                    foreach (var value in field.Value)
                    {
                        if (string.IsNullOrEmpty(value)) continue;

                        var valueValidation = _validationService.ValidateAndSanitize(value, InputType.General);
                        if (!valueValidation.IsValid)
                        {
                            result.IsValid = false;
                            result.Reason = $"表单字段值包含恶意内容: {field.Key}";
                            result.ThreatLevel = "High";
                            result.ThreatType = valueValidation.ThreatType;
                            return;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "验证表单数据时发生错误");
                result.IsValid = false;
                result.Reason = "表单数据验证失败";
                result.ThreatLevel = "$1";
            }
        }

        /// <summary>
        /// 处理无效请求
        /// </summary>
        private async Task HandleInvalidRequest(HttpContext context, RequestValidationResult validationResult, string requestId)
        {
            var clientIP = GetClientIP(context);
            
            _logger.LogWarning("阻止恶意请求 [RequestId: {RequestId}]: {Reason}, IP: {ClientIP}, Path: {Path}",
                requestId, validationResult.Reason, clientIP, context.Request.Path);

            // 记录安全日志
            _logger.LogWarning("恶意请求审计: 类型={ExceptionType}, 消息={Message}, IP={ClientIP}, UA={UserAgent}, 路径={Path}, 威胁级别={ThreatLevel}, 会话={SessionId}",
                "MaliciousRequest", validationResult.Reason ?? "恶意请求被阻止", clientIP, 
                context.Request.Headers.UserAgent.ToString(), context.Request.Path, 
                validationResult.ThreatLevel, requestId);

            // 返回错误响应
            context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            context.Response.ContentType = "application/json; charset=utf-8";

            var response = new
            {
                error = "请求被拒绝",
                message = "请求包含不安全内容",
                code = "MALICIOUS_REQUEST_BLOCKED",
                requestId = requestId
            };

            await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(response));
        }

        /// <summary>
        /// 处理异常
        /// </summary>
        private async Task HandleException(HttpContext context, Exception ex, string requestId, long responseTimeMs)
        {
            var clientIP = GetClientIP(context);
            
            _logger.LogError(ex, "请求处理异常 [RequestId: {RequestId}]: {Message}", requestId, ex.Message);

            // 记录异常安全日志
            _logger.LogError("异常安全审计: 类型={ExceptionType}, 消息={Message}, IP={ClientIP}, UA={UserAgent}, 路径={Path}, 威胁级别=Medium, 会话={SessionId}",
                ex.GetType().Name, ex.Message, clientIP, 
                context.Request.Headers.UserAgent.ToString(), context.Request.Path, requestId);

            // 记录API访问失败
            if (ShouldAuditRequest(context))
            {
                await LogApiAccessAsync(context, requestId, responseTimeMs, false);
            }
        }

        /// <summary>
        /// 记录API访问
        /// </summary>
        private async Task LogApiAccessAsync(HttpContext context, string requestId, long responseTimeMs, bool isSuccess)
        {
            var userId = context.User?.Identity?.IsAuthenticated == true 
                ? Guid.TryParse(context.User.FindFirst("sub")?.Value, out var id) ? (Guid?)id : null
                : null;

            _logger.LogInformation("API访问审计: 用户={UserId}, IP={ClientIP}, 端点={Endpoint}, 方法={HttpMethod}, 状态码={StatusCode}, 成功={IsSuccess}, 响应时间={ResponseTimeMs}ms, 请求ID={RequestId}",
                userId, GetClientIP(context), context.Request.Path, context.Request.Method, context.Response.StatusCode, isSuccess, responseTimeMs, requestId);
            await Task.CompletedTask;
        }

        /// <summary>
        /// 获取客户端IP
        /// </summary>
        private string GetClientIP(HttpContext context)
        {
            return context.Request.Headers["X-Forwarded-For"].FirstOrDefault()?.Split(',')[0].Trim()
                   ?? context.Request.Headers["X-Real-IP"].FirstOrDefault()
                   ?? context.Connection.RemoteIpAddress?.ToString()
                   ?? "unknown";
        }

        /// <summary>
        /// 判断是否应该审计请求
        /// </summary>
        private bool ShouldAuditRequest(HttpContext context)
        {
            var path = context.Request.Path.Value?.ToLower() ?? "";
            
            // 排除静态资源
            var staticExtensions = new[] { ".css", ".js", ".png", ".jpg", ".jpeg", ".gif", ".ico", ".svg", ".woff", ".woff2" };
            if (staticExtensions.Any(ext => path.EndsWith(ext)))
                return false;

            // 排除健康检查端点
            if (path.Contains("/health") || path.Contains("/metrics"))
                return false;

            return true;
        }
    }

    /// <summary>
    /// 请求验证结果
    /// </summary>
    public class RequestValidationResult
    {
        public bool IsValid { get; set; }
        public string? Reason { get; set; }
        public string ThreatLevel { get; set; } = "Low";
        public ThreatType ThreatType { get; set; } = ThreatType.None;
    }

    /// <summary>
    /// 安全中间件配置选项
    /// </summary>
    public class SecurityMiddlewareOptions
    {
        public bool RequireHttps { get; set; } = true;
        public bool RequireUserAgent { get; set; } = true;
        public long MaxRequestSize { get; set; } = 10 * 1024 * 1024; // 10MB
        public string XFrameOptions { get; set; } = "DENY";
        public string ContentSecurityPolicy { get; set; } = 
            "default-src 'self'; script-src 'self' 'unsafe-inline' 'unsafe-eval'; style-src 'self' 'unsafe-inline';";
        public string PermissionsPolicy { get; set; } = 
            "geolocation=(), microphone=(), camera=(), fullscreen=(self)";
        public int HstsMaxAge { get; set; } = 31536000; // 1 year
        public bool HstsPreload { get; set; } = false;
    }
}