using LYBT.WebAPI.Services;
using System.Diagnostics;
using System.Text.Json;

namespace LYBT.WebAPI.Middleware
{
    /// <summary>
    /// 性能监控中间件
    /// </summary>
    public class PerformanceMonitoringMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<PerformanceMonitoringMiddleware> _logger;
        private readonly ISystemMetricsCollector _metricsCollector;

        public PerformanceMonitoringMiddleware(
            RequestDelegate next,
            ILogger<PerformanceMonitoringMiddleware> logger,
            ISystemMetricsCollector metricsCollector)
        {
            _next = next;
            _logger = logger;
            _metricsCollector = metricsCollector;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var stopwatch = Stopwatch.StartNew();
            var requestId = Guid.NewGuid().ToString("N")[..8];
            
            // 记录请求开始
            var requestInfo = new RequestInfo
            {
                RequestId = requestId,
                Method = context.Request.Method,
                Path = context.Request.Path.Value ?? string.Empty,
                QueryString = context.Request.QueryString.Value ?? string.Empty,
                UserAgent = context.Request.Headers.UserAgent.ToString(),
                ClientIp = GetClientIpAddress(context),
                StartTime = DateTime.UtcNow
            };

            // 添加请求ID到响应头
            context.Response.Headers.TryAdd("X-Request-Id", requestId);
            context.Items["RequestId"] = requestId;
            context.Items["RequestStartTime"] = requestInfo.StartTime;

            Exception? exception = null;
            var originalBodyStream = context.Response.Body;

            try
            {
                // 记录请求详情（仅在调试模式下）
                if (_logger.IsEnabled(LogLevel.Debug))
                {
                    await LogRequestDetailsAsync(context, requestInfo);
                }

                await _next(context);
            }
            catch (Exception ex)
            {
                exception = ex;
                throw;
            }
            finally
            {
                stopwatch.Stop();
                
                // 创建性能指标
                var performanceMetrics = new RequestPerformanceMetrics
                {
                    RequestId = requestId,
                    Method = requestInfo.Method,
                    Path = requestInfo.Path,
                    StatusCode = context.Response.StatusCode,
                    Duration = stopwatch.Elapsed,
                    Success = exception == null && context.Response.StatusCode < 400,
                    Exception = exception?.GetType().Name,
                    Timestamp = requestInfo.StartTime,
                    ContentLength = context.Response.ContentLength ?? 0,
                    ClientIp = requestInfo.ClientIp
                };

                // 记录到指标收集器
                await _metricsCollector.RecordRequestMetricsAsync(performanceMetrics);

                // 记录性能日志
                await LogPerformanceMetricsAsync(performanceMetrics, exception);
            }
        }

        private static string GetClientIpAddress(HttpContext context)
        {
            var ipAddress = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (string.IsNullOrEmpty(ipAddress))
            {
                ipAddress = context.Request.Headers["X-Real-IP"].FirstOrDefault();
            }
            if (string.IsNullOrEmpty(ipAddress))
            {
                ipAddress = context.Connection.RemoteIpAddress?.ToString();
            }
            
            return ipAddress ?? "Unknown";
        }

        private async Task LogRequestDetailsAsync(HttpContext context, RequestInfo requestInfo)
        {
            try
            {
                var requestBody = string.Empty;
                
                // 读取请求体（仅适用于POST/PUT等）
                if (context.Request.ContentLength > 0 && context.Request.ContentLength < 4096) // 限制大小
                {
                    context.Request.EnableBuffering();
                    var buffer = new byte[Convert.ToInt32(context.Request.ContentLength)];
                    await context.Request.Body.ReadExactlyAsync(buffer, 0, buffer.Length);
                    requestBody = System.Text.Encoding.UTF8.GetString(buffer);
                    context.Request.Body.Position = 0;
                }

                _logger.LogDebug("Request started: {RequestId} {Method} {Path} from {ClientIp}. Body: {RequestBody}",
                    requestInfo.RequestId,
                    requestInfo.Method,
                    requestInfo.Path,
                    requestInfo.ClientIp,
                    SanitizeRequestBody(requestBody));
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to log request details for {RequestId}", requestInfo.RequestId);
            }
        }

        private async Task LogPerformanceMetricsAsync(RequestPerformanceMetrics metrics, Exception? exception)
        {
            var logLevel = DetermineLogLevel(metrics, exception);
            
            if (!_logger.IsEnabled(logLevel))
                return;

            var logMessage = "Request completed: {RequestId} {Method} {Path} responded {StatusCode} in {Duration}ms";
            var logArgs = new object[]
            {
                metrics.RequestId,
                metrics.Method,
                metrics.Path,
                metrics.StatusCode,
                metrics.Duration.TotalMilliseconds
            };

            if (exception != null)
            {
                _logger.Log(logLevel, exception, logMessage + " with exception {ExceptionType}", 
                    logArgs.Concat(new object[] { exception.GetType().Name }).ToArray());
            }
            else
            {
                _logger.Log(logLevel, logMessage, logArgs);
            }

            // 记录慢请求
            if (metrics.Duration.TotalMilliseconds > 5000) // 5秒以上的请求
            {
                _logger.LogWarning("Slow request detected: {RequestId} {Method} {Path} took {Duration}ms",
                    metrics.RequestId, metrics.Method, metrics.Path, metrics.Duration.TotalMilliseconds);
            }

            await Task.CompletedTask;
        }

        private static LogLevel DetermineLogLevel(RequestPerformanceMetrics metrics, Exception? exception)
        {
            if (exception != null)
                return LogLevel.Error;

            if (metrics.StatusCode >= 500)
                return LogLevel.Error;

            if (metrics.StatusCode >= 400)
                return LogLevel.Warning;

            if (metrics.Duration.TotalMilliseconds > 2000) // 2秒以上
                return LogLevel.Warning;

            if (metrics.Duration.TotalMilliseconds > 1000) // 1秒以上
                return LogLevel.Information;

            return LogLevel.Debug;
        }

        private static string SanitizeRequestBody(string requestBody)
        {
            if (string.IsNullOrEmpty(requestBody))
                return string.Empty;

            try
            {
                // 尝试解析JSON并移除敏感字段
                var jsonDoc = JsonDocument.Parse(requestBody);
                var sanitized = SanitizeJsonElement(jsonDoc.RootElement);
                return JsonSerializer.Serialize(sanitized);
            }
            catch
            {
                // 如果不是JSON，只显示长度
                return $"[Non-JSON content, length: {requestBody.Length}]";
            }
        }

        private static object SanitizeJsonElement(JsonElement element)
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.Object:
                    var obj = new Dictionary<string, object>();
                    foreach (var property in element.EnumerateObject())
                    {
                        var key = property.Name.ToLower();
                        if (IsSensitiveField(key))
                        {
                            obj[property.Name] = "***";
                        }
                        else
                        {
                            obj[property.Name] = SanitizeJsonElement(property.Value);
                        }
                    }
                    return obj;

                case JsonValueKind.Array:
                    return element.EnumerateArray()
                        .Select(SanitizeJsonElement)
                        .ToArray();

                case JsonValueKind.String:
                    return element.GetString() ?? string.Empty;

                case JsonValueKind.Number:
                    return element.GetDecimal();

                case JsonValueKind.True:
                    return true;

                case JsonValueKind.False:
                    return false;

                case JsonValueKind.Null:
                    return null!;

                default:
                    return element.GetRawText();
            }
        }

        private static bool IsSensitiveField(string fieldName)
        {
            var sensitiveFields = new[]
            {
                "password", "pwd", "secret", "token", "key", "authorization",
                "credit", "card", "ssn", "social", "phone", "email"
            };

            return sensitiveFields.Any(field => fieldName.Contains(field));
        }
    }

    /// <summary>
    /// 性能监控中间件扩展
    /// </summary>
    public static class PerformanceMonitoringMiddlewareExtensions
    {
        /// <summary>
        /// 使用性能监控中间件
        /// </summary>
        public static IApplicationBuilder UsePerformanceMonitoring(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<PerformanceMonitoringMiddleware>();
        }
    }

    // 数据模型
    public class RequestInfo
    {
        public string RequestId { get; set; } = string.Empty;
        public string Method { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
        public string QueryString { get; set; } = string.Empty;
        public string UserAgent { get; set; } = string.Empty;
        public string ClientIp { get; set; } = string.Empty;
        public DateTime StartTime { get; set; }
    }

    public class RequestPerformanceMetrics
    {
        public string RequestId { get; set; } = string.Empty;
        public string Method { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
        public int StatusCode { get; set; }
        public TimeSpan Duration { get; set; }
        public bool Success { get; set; }
        public string? Exception { get; set; }
        public DateTime Timestamp { get; set; }
        public long ContentLength { get; set; }
        public string ClientIp { get; set; } = string.Empty;
    }
}