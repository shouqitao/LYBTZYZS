using LYBT.Infrastructure.Logging;
using LYBT.Shared.ExceptionHandling.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace LYBT.WebAPI.Middleware
{

    /// <summary>
    /// 全局异常处理器（已废弃）
    /// refactor-logging-system: 此类已被IExceptionHandler链模式替代
    /// 请使用 BusinessExceptionHandler 处理业务异常，使用 SystemExceptionHandler 处理系统异常
    /// </summary>
    /// <remarks>
    /// 迁移说明：
    /// - BusinessExceptionHandler: 处理 AppException 及其子类（ValidationException, NotFoundException, ConflictException, UnauthorizedException 等）
    /// - SystemExceptionHandler: 兜底处理所有未被处理的系统异常
    /// 新处理器在 ApiServiceCollectionExtensions.RegisterApiServices() 中注册
    /// </remarks>
    [Obsolete("请使用 BusinessExceptionHandler 和 SystemExceptionHandler 替代。此类将在下一版本中移除。")]
    public class GlobalExceptionHandler : IExceptionHandler
    {
        private readonly ILogger<GlobalExceptionHandler> _logger;

        public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
        {
            _logger = logger;
        }

        /// <inheritdoc/>
        public async ValueTask<bool> TryHandleAsync(
            HttpContext httpContext,
            Exception exception,
            CancellationToken cancellationToken)
        {
            // refactor-logging-system: 获取CorrelationId
            var correlationId = httpContext.GetCorrelationId();
            var sanitizedMessage = SensitiveDataMasker.SanitizeText(exception.Message);

            // refactor-logging-system: 根据异常类型区分日志级别
            // 业务异常(AppException及子类)使用Warning，系统异常使用Error
            var isBusinessException = exception is AppException;
            if (isBusinessException)
            {
                _logger.LogWarning(
                    exception,
                    "业务异常 - 类型: {ExceptionType}, 消息: {Message}, CorrelationId: {CorrelationId}, 路径: {RequestPath}, 方法: {HttpMethod}, 用户: {UserId}",
                    exception.GetType().Name,
                    sanitizedMessage,
                    correlationId,
                    httpContext.Request.Path,
                    httpContext.Request.Method,
                    httpContext.User?.Identity?.Name ?? "匿名用户");
            }
            else
            {
                _logger.LogError(
                    exception,
                    "系统异常 - 类型: {ExceptionType}, 消息: {Message}, CorrelationId: {CorrelationId}, 路径: {RequestPath}, 方法: {HttpMethod}, 用户: {UserId}",
                    exception.GetType().Name,
                    sanitizedMessage,
                    correlationId,
                    httpContext.Request.Path,
                    httpContext.Request.Method,
                    httpContext.User?.Identity?.Name ?? "匿名用户");
            }

            var problemDetails = new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "服务器内部错误",
                Detail = "处理请求时发生错误，请稍后重试",
                Instance = httpContext.Request.Path
            };

            // refactor-logging-system: 添加CorrelationId用于日志关联
            problemDetails.Extensions["correlationId"] = correlationId;
            problemDetails.Extensions["traceId"] = httpContext.TraceIdentifier;
            problemDetails.Extensions["timestamp"] = DateTimeOffset.UtcNow;
            problemDetails.Extensions["requestMethod"] = httpContext.Request.Method;
            problemDetails.Extensions["userAgent"] = httpContext.Request.Headers.UserAgent.ToString();

            // 添加用户上下文（如果已认证）
            if (httpContext.User?.Identity?.IsAuthenticated == true)
            {
                problemDetails.Extensions["userId"] = httpContext.User.Identity.Name;
            }

            // 根据异常类型设置不同的响应 - UltraThink统一异常体系
            switch (exception)
            {
                case ApiException apiException:
                    problemDetails.Status = (int)apiException.StatusCode;
                    problemDetails.Title = "API调用异常";
                    problemDetails.Detail = apiException.ShowDetailToUser ? apiException.UserMessage ?? apiException.Message : "API调用失败";
                    if (!string.IsNullOrEmpty(apiException.ErrorCode))
                    {
                        problemDetails.Extensions["errorCode"] = apiException.ErrorCode;
                    }

                    if (!string.IsNullOrEmpty(apiException.ResponseContent))
                    {
                        problemDetails.Extensions["responseContent"] = apiException.ResponseContent;
                    }

                    break;

                case BusinessException businessException:
                    problemDetails.Status = StatusCodes.Status400BadRequest;
                    problemDetails.Title = "业务错误";
                    problemDetails.Detail = businessException.UserMessage ?? businessException.Message;
                    if (!string.IsNullOrEmpty(businessException.ErrorCode))
                    {
                        problemDetails.Extensions["errorCode"] = businessException.ErrorCode;
                    }

                    if (!string.IsNullOrEmpty(businessException.BusinessRule))
                    {
                        problemDetails.Extensions["businessRule"] = businessException.BusinessRule;
                    }

                    break;

                // Epic #1731 Phase 3: 处理FluentValidation.ValidationException
                case FluentValidation.ValidationException fluentValidationException:
                    problemDetails.Status = StatusCodes.Status400BadRequest;
                    problemDetails.Title = "验证失败";
                    problemDetails.Detail = "请求数据验证失败，请检查输入";

                    // 格式化验证错误
                    var errors = fluentValidationException.Errors
                        .GroupBy(e => e.PropertyName)
                        .ToDictionary(
                            g => g.Key,
                            g => g.Select(e => e.ErrorMessage).ToArray()
                        );

                    problemDetails.Extensions["errors"] = errors;
                    problemDetails.Extensions["errorCode"] = "VALIDATION_FAILED";
                    break;

                case ValidationException validationException:
                    problemDetails.Status = StatusCodes.Status400BadRequest;
                    problemDetails.Title = "验证失败";
                    problemDetails.Detail = validationException.UserMessage ?? validationException.Message;
                    if (!string.IsNullOrEmpty(validationException.ErrorCode))
                    {
                        problemDetails.Extensions["errorCode"] = validationException.ErrorCode;
                    }

                    if (validationException.HasErrors)
                    {
                        problemDetails.Extensions["errors"] = validationException.Errors;
                    }

                    if (!string.IsNullOrEmpty(validationException.FieldName))
                    {
                        problemDetails.Extensions["fieldName"] = validationException.FieldName;
                    }

                    break;

                case NotFoundException notFoundException:
                    problemDetails.Status = StatusCodes.Status404NotFound;
                    problemDetails.Title = "资源未找到";
                    problemDetails.Detail = notFoundException.UserMessage ?? notFoundException.Message;
                    if (!string.IsNullOrEmpty(notFoundException.ErrorCode))
                    {
                        problemDetails.Extensions["errorCode"] = notFoundException.ErrorCode;
                    }

                    if (!string.IsNullOrEmpty(notFoundException.ResourceType))
                    {
                        problemDetails.Extensions["resourceType"] = notFoundException.ResourceType;
                    }

                    if (!string.IsNullOrEmpty(notFoundException.ResourceId))
                    {
                        problemDetails.Extensions["resourceId"] = notFoundException.ResourceId;
                    }

                    break;

                case AppException appException:
                    problemDetails.Status = StatusCodes.Status500InternalServerError;
                    problemDetails.Title = "应用程序异常";
                    problemDetails.Detail = appException.ShowDetailToUser ? (appException.UserMessage ?? appException.Message) : "应用程序处理异常";
                    if (!string.IsNullOrEmpty(appException.ErrorCode))
                    {
                        problemDetails.Extensions["errorCode"] = appException.ErrorCode;
                    }

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
                        // 开发环境也需要脱敏
                        problemDetails.Detail = SensitiveDataMasker.SanitizeText(exception.Message);
                    }

                    break;
            }

            httpContext.Response.StatusCode = problemDetails.Status.Value;
            await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

            return true;
        }
    }
}
