using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Text.Json;

namespace LYBT.WebAPI.Middleware
{

    /// <summary>
    /// 全局异常处理中间件
    /// </summary>
    public class GlobalExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionMiddleware> _logger;

        public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(context, ex);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            _logger.LogError(exception, "发生未处理的异常");

            context.Response.ContentType = "application/problem+json";

            var problemDetails = exception switch
            {
                UnauthorizedAccessException => new ProblemDetails
                {
                    Status = (int)HttpStatusCode.Unauthorized,
                    Title = "未授权",
                    Detail = "未授权访问",
                    Instance = context.Request.Path
                },
                ArgumentException => new ProblemDetails
                {
                    Status = (int)HttpStatusCode.BadRequest,
                    Title = "参数错误",
                    Detail = exception.Message,
                    Instance = context.Request.Path
                },
                KeyNotFoundException => new ProblemDetails
                {
                    Status = (int)HttpStatusCode.NotFound,
                    Title = "资源未找到",
                    Detail = "请求的资源不存在",
                    Instance = context.Request.Path
                },
                InvalidOperationException => new ProblemDetails
                {
                    Status = (int)HttpStatusCode.BadRequest,
                    Title = "操作无效",
                    Detail = exception.Message,
                    Instance = context.Request.Path
                },
                _ => new ProblemDetails
                {
                    Status = (int)HttpStatusCode.InternalServerError,
                    Title = "服务器内部错误",
                    Detail = context.RequestServices.GetRequiredService<IWebHostEnvironment>().IsDevelopment()
                        ? exception.Message
                        : "处理请求时发生错误，请稍后重试",
                    Instance = context.Request.Path
                }
            };

            // 添加追踪ID
            problemDetails.Extensions["traceId"] = context.TraceIdentifier;

            context.Response.StatusCode = problemDetails.Status ?? 500;

            var jsonResponse = JsonSerializer.Serialize(problemDetails, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            await context.Response.WriteAsync(jsonResponse);
        }
    }

    /// <summary>
    /// 全局异常处理中间件扩展方法
    /// </summary>
    public static class GlobalExceptionMiddlewareExtensions
    {

        public static IApplicationBuilder UseGlobalExceptionHandling(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<GlobalExceptionMiddleware>();
        }
    }
}