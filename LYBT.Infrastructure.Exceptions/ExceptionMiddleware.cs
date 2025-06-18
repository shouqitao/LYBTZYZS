using LYBT.Common.Responses;
using Microsoft.AspNetCore.Http;
using System.Net;
using System.Text.Json;

namespace LYBT.Infrastructure.Exceptions {
    /// <summary>
    /// 统一异常处理中间件
    /// </summary>
    public class ExceptionMiddleware {
        private readonly RequestDelegate _next;

        public ExceptionMiddleware(RequestDelegate next) {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context) {
            try {
                await _next(context);
            } catch (BusinessException ex) {
                context.Response.ContentType = "application/json";
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                var response = ApiResponse<string>.Fail(ex.Message, ex.Code);
                var json = JsonSerializer.Serialize(response);
                await context.Response.WriteAsync(json);
            } catch (Exception ex) {
                context.Response.ContentType = "application/json";
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                var response = ApiResponse<string>.Fail("服务器内部错误：" + ex.Message);
                var json = JsonSerializer.Serialize(response);
                await context.Response.WriteAsync(json);
            }
        }
    }
}
