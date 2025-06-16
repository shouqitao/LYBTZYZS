using LYBT.Common.Responses;
using System.Net;
using System.Text.Json;

namespace LYBT.WebAPI.Middlewares;

/// <summary>
/// 全局异常处理
/// </summary>
public class GlobalExceptionMiddleware {
    private readonly RequestDelegate _next;

    public GlobalExceptionMiddleware(RequestDelegate next) {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context) {
        try {
            await _next(context);
        } catch (Exception ex) {
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

            var response = ApiResponse<string>.Fail("服务器内部错误：" + ex.Message);
            var json = JsonSerializer.Serialize(response);
            await context.Response.WriteAsync(json);
        }
    }
}
