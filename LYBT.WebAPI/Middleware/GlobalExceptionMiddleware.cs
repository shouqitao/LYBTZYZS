using LYBT.Common.Responses;
using System.Net;
using System.Text.Json;

namespace LYBT.WebAPI.Middleware {

    /// <summary>
    /// 全局异常处理中间件
    /// </summary>
    public class GlobalExceptionMiddleware {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionMiddleware> _logger;

        public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger) {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context) {
            try {
                await _next(context);
            } catch (Exception ex) {
                await HandleExceptionAsync(context, ex);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception exception) {
            _logger.LogError(exception, "发生未处理的异常");

            context.Response.ContentType = "application/json";

            var response = exception switch {
                UnauthorizedAccessException => new {
                    StatusCode = (int)HttpStatusCode.Unauthorized,
                    Response = ApiResponse<object>.Fail("未授权访问", 401)
                },
                ArgumentException => new {
                    StatusCode = (int)HttpStatusCode.BadRequest,
                    Response = ApiResponse<object>.Fail(exception.Message, 400)
                },
                KeyNotFoundException => new {
                    StatusCode = (int)HttpStatusCode.NotFound,
                    Response = ApiResponse<object>.Fail("资源未找到", 404)
                },
                InvalidOperationException => new {
                    StatusCode = (int)HttpStatusCode.BadRequest,
                    Response = ApiResponse<object>.Fail(exception.Message, 400)
                },
                _ => new {
                    StatusCode = (int)HttpStatusCode.InternalServerError,
                    Response = ApiResponse<object>.Fail("服务器内部错误", 500)
                }
            };

            context.Response.StatusCode = response.StatusCode;

            var jsonResponse = JsonSerializer.Serialize(response.Response, new JsonSerializerOptions {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            await context.Response.WriteAsync(jsonResponse);
        }
    }

    /// <summary>
    /// 全局异常处理中间件扩展方法
    /// </summary>
    public static class GlobalExceptionMiddlewareExtensions {

        public static IApplicationBuilder UseGlobalExceptionHandling(this IApplicationBuilder builder) {
            return builder.UseMiddleware<GlobalExceptionMiddleware>();
        }
    }
}