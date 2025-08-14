using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using LYBT.WebAPI.Exceptions;

namespace LYBT.WebAPI.Middleware
{
    /// <summary>
    /// 全局异常处理器
    /// </summary>
    public class GlobalExceptionHandler : IExceptionHandler
    {
        private readonly ILogger<GlobalExceptionHandler> _logger;

        public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
        {
            _logger = logger;
        }

        public async ValueTask<bool> TryHandleAsync(
            HttpContext httpContext,
            Exception exception,
            CancellationToken cancellationToken)
        {
            _logger.LogError(exception, "An exception occurred: {Message}", exception.Message);

            var problemDetails = new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "服务器内部错误",
                Detail = "处理请求时发生错误，请稍后重试",
                Instance = httpContext.Request.Path
            };

            // 添加追踪ID
            problemDetails.Extensions["traceId"] = httpContext.TraceIdentifier;

            // 根据异常类型设置不同的响应
            switch (exception)
            {
                case BusinessException businessException:
                    problemDetails.Status = StatusCodes.Status400BadRequest;
                    problemDetails.Title = "业务错误";
                    problemDetails.Detail = businessException.Message;
                    break;

                case NotFoundException notFoundException:
                    problemDetails.Status = StatusCodes.Status404NotFound;
                    problemDetails.Title = "资源未找到";
                    problemDetails.Detail = notFoundException.Message;
                    break;

                case UnauthorizedAccessException:
                    problemDetails.Status = StatusCodes.Status401Unauthorized;
                    problemDetails.Title = "未授权";
                    problemDetails.Detail = "您没有权限访问此资源";
                    break;

                case ValidationException validationException:
                    problemDetails.Status = StatusCodes.Status400BadRequest;
                    problemDetails.Title = "验证失败";
                    problemDetails.Detail = validationException.Message;
                    if (validationException.Errors != null && validationException.Errors.Any())
                    {
                        problemDetails.Extensions["errors"] = validationException.Errors;
                    }
                    break;

                default:
                    // 生产环境不暴露详细错误信息
                    if (httpContext.RequestServices.GetRequiredService<IWebHostEnvironment>().IsDevelopment())
                    {
                        problemDetails.Detail = exception.Message;
                    }
                    break;
            }

            httpContext.Response.StatusCode = problemDetails.Status.Value;
            await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

            return true;
        }
    }
}