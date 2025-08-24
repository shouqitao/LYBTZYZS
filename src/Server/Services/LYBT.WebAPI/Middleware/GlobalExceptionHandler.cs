using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using LYBT.Shared.Models.Exceptions;

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

            // 根据异常类型设置不同的响应 - UltraThink统一异常体系
            switch (exception)
            {
                case ApiException apiException:
                    problemDetails.Status = (int)apiException.StatusCode;
                    problemDetails.Title = "API调用异常";
                    problemDetails.Detail = apiException.ShowDetailToUser ? apiException.UserMessage ?? apiException.Message : "API调用失败";
                    if (!string.IsNullOrEmpty(apiException.ErrorCode))
                        problemDetails.Extensions["errorCode"] = apiException.ErrorCode;
                    if (!string.IsNullOrEmpty(apiException.ResponseContent))
                        problemDetails.Extensions["responseContent"] = apiException.ResponseContent;
                    break;

                case BusinessException businessException:
                    problemDetails.Status = StatusCodes.Status400BadRequest;
                    problemDetails.Title = "业务错误";
                    problemDetails.Detail = businessException.UserMessage ?? businessException.Message;
                    if (!string.IsNullOrEmpty(businessException.ErrorCode))
                        problemDetails.Extensions["errorCode"] = businessException.ErrorCode;
                    if (!string.IsNullOrEmpty(businessException.BusinessRule))
                        problemDetails.Extensions["businessRule"] = businessException.BusinessRule;
                    break;

                case ValidationException validationException:
                    problemDetails.Status = StatusCodes.Status400BadRequest;
                    problemDetails.Title = "验证失败";
                    problemDetails.Detail = validationException.UserMessage ?? validationException.Message;
                    if (!string.IsNullOrEmpty(validationException.ErrorCode))
                        problemDetails.Extensions["errorCode"] = validationException.ErrorCode;
                    if (validationException.HasErrors)
                        problemDetails.Extensions["errors"] = validationException.Errors;
                    if (!string.IsNullOrEmpty(validationException.FieldName))
                        problemDetails.Extensions["fieldName"] = validationException.FieldName;
                    break;

                case NotFoundException notFoundException:
                    problemDetails.Status = StatusCodes.Status404NotFound;
                    problemDetails.Title = "资源未找到";
                    problemDetails.Detail = notFoundException.UserMessage ?? notFoundException.Message;
                    if (!string.IsNullOrEmpty(notFoundException.ErrorCode))
                        problemDetails.Extensions["errorCode"] = notFoundException.ErrorCode;
                    if (!string.IsNullOrEmpty(notFoundException.ResourceType))
                        problemDetails.Extensions["resourceType"] = notFoundException.ResourceType;
                    if (!string.IsNullOrEmpty(notFoundException.ResourceId))
                        problemDetails.Extensions["resourceId"] = notFoundException.ResourceId;
                    break;

                case AppException appException:
                    problemDetails.Status = StatusCodes.Status500InternalServerError;
                    problemDetails.Title = "应用程序异常";
                    problemDetails.Detail = appException.ShowDetailToUser ? (appException.UserMessage ?? appException.Message) : "应用程序处理异常";
                    if (!string.IsNullOrEmpty(appException.ErrorCode))
                        problemDetails.Extensions["errorCode"] = appException.ErrorCode;
                    break;

                case UnauthorizedAccessException:
                    problemDetails.Status = StatusCodes.Status401Unauthorized;
                    problemDetails.Title = "未授权";
                    problemDetails.Detail = "您没有权限访问此资源";
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