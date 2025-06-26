using LYBT.Common.Responses;
using Microsoft.AspNetCore.Http;
using System.Text.Json;

namespace LYBT.Infrastructure.Exceptions {

    /// <summary>
    /// 全局异常处理中间件
    /// </summary>
    public class ExceptionMiddleware {
        private readonly RequestDelegate _next;

        public ExceptionMiddleware(RequestDelegate next) {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context) {
            try {
                await _next(context);
            } catch (BusinessException bex) {
                await HandleExceptionAsync(context, bex.Message, 400);
            } catch (Exception) {
                await HandleExceptionAsync(context, "服务器内部错误", 500);
            }
        }

        private static Task HandleExceptionAsync(HttpContext context, string message, int statusCode) {
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = statusCode;
            // 只需统一用泛型响应体（这里Data为null）
            var response = ApiResponse<object>.Fail(message, statusCode);
            return context.Response.WriteAsync(JsonSerializer.Serialize(response));
        }
    }
}