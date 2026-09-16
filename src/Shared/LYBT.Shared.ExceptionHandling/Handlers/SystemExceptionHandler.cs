using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace LYBT.Shared.ExceptionHandling.Handlers;

/// <summary>
/// 系统异常处理器 - 兜底处理所有未被其他处理器处理的异常
/// A-31-C2: 从 LYBT.Infrastructure.ExceptionHandling 迁移
/// X-3: 异常路径统一写 ProblemDetails（RFC 7807）；ApiResponse 仅用于成功/已知业务失败响应
/// </summary>
public class SystemExceptionHandler : IExceptionHandler
{
    private readonly ILogger<SystemExceptionHandler> _logger;
    private readonly IHostEnvironment _environment;

    public SystemExceptionHandler(
        ILogger<SystemExceptionHandler> logger,
        IHostEnvironment environment)
    {
        _logger = logger;
        _environment = environment;
    }

    /// <inheritdoc/>
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var correlationId = GetCorrelationId(httpContext);

        // 系统异常使用Error级别日志
        _logger.LogError(
            exception,
            "系统异常 - 类型: {ExceptionType}, 消息: {Message}, CorrelationId: {CorrelationId}, 路径: {RequestPath}, 方法: {HttpMethod}, 用户: {UserId}",
            exception.GetType().Name,
            exception.Message,
            correlationId,
            httpContext.Request.Path,
            httpContext.Request.Method,
            httpContext.User?.Identity?.Name ?? "匿名用户");

        var (statusCode, title, detail) = GetExceptionInfo(exception);

        // RFC 7807：Extensions 在 ProblemDetailsConfiguration.CustomizeProblemDetails 中
        // 自动注入 correlationId/timestamp/traceId/severity/type；此处仅补诊断扩展。
        var extensions = new Dictionary<string, object?>();
        if (_environment.IsDevelopment())
        {
            extensions["exceptionType"] = exception.GetType().FullName;
            extensions["stackTrace"] = exception.StackTrace;
        }
        if (exception is FluentValidation.ValidationException valEx)
        {
            extensions["validationErrors"] = valEx.Errors
                .Select(e => new { field = e.PropertyName, error = e.ErrorMessage })
                .ToList();
        }

        await Results.Problem(
            statusCode: statusCode,
            title: title,
            detail: detail,
            instance: httpContext.Request.Path,
            extensions: extensions.Count > 0 ? extensions : null)
            .ExecuteAsync(httpContext);

        return true; // 始终返回true，作为兜底处理器
    }

    private (int StatusCode, string Title, string Detail) GetExceptionInfo(Exception exception)
    {
        // 按类型名匹配外部框架异常（EF Core），避免 Shared 层引入 EF Core 依赖——
        // 同 ClientErrorMessageMapper 对 Refit.ApiException 的类型名匹配先例
        var exceptionTypeName = exception.GetType().FullName;

        return exception switch
        {
            // FluentValidation 异常
            FluentValidation.ValidationException => (
                400,
                "验证失败",
                "请求数据验证失败，请检查输入"
            ),

            // 权限拒绝 (已认证但无权限): HTTP 403 Forbidden
            UnauthorizedAccessException => (
                403,
                "权限不足",
                "您没有权限执行此操作"
            ),

            // 功能未实现 (BaseCrudController 抽象桩): HTTP 501 Not Implemented
            NotSupportedException => (
                501,
                "功能未实现",
                _environment.IsDevelopment() ? exception.Message : "该操作暂不支持"
            ),

            // 操作取消
            OperationCanceledException => (
                499, // Client Closed Request
                "请求已取消",
                "客户端取消了请求"
            ),

            // 超时
            TimeoutException => (
                504,
                "请求超时",
                "服务器处理请求超时，请稍后重试"
            ),

            // 数据库相关（EF Core 按类型名匹配，DbUpdateConcurrencyException 为 DbUpdateException 子类，精确名命中各自分支）
            _ when exceptionTypeName == "Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException" => (
                409,
                "并发冲突",
                "数据已被其他用户修改，请刷新后重试"
            ),

            _ when exceptionTypeName == "Microsoft.EntityFrameworkCore.DbUpdateException" => (
                500,
                "数据库错误",
                "数据保存失败，请稍后重试"
            ),

            // HTTP相关
            HttpRequestException => (
                502,
                "外部服务错误",
                "调用外部服务失败，请稍后重试"
            ),

            // 参数异常
            ArgumentException => (
                400,
                "参数错误",
                _environment.IsDevelopment() ? exception.Message : "请求参数无效"
            ),

            // 空引用
            NullReferenceException => (
                500,
                "服务器内部错误",
                "处理请求时发生错误，请稍后重试"
            ),

            // 无效操作
            InvalidOperationException => (
                500,
                "操作无效",
                _environment.IsDevelopment() ? exception.Message : "操作无法执行，请稍后重试"
            ),

            // 资源未找到
            KeyNotFoundException => (
                404,
                "资源未找到",
                _environment.IsDevelopment() ? exception.Message : "请求的资源不存在"
            ),

            // 默认：生产环境隐藏详细信息
            _ => (
                500,
                "服务器内部错误",
                _environment.IsDevelopment() ? exception.Message : "处理请求时发生错误，请稍后重试"
            )
        };
    }

    private static string GetCorrelationId(HttpContext httpContext)
    {
        if (httpContext.Request.Headers.TryGetValue("X-Correlation-Id", out var correlationId)
            && !string.IsNullOrEmpty(correlationId))
        {
            return correlationId!;
        }
        return httpContext.TraceIdentifier;
    }

}
