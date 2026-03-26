using System.Diagnostics;
using LYBT.Shared.Logging.Masking;
using Microsoft.AspNetCore.Mvc.Filters;

namespace LYBT.WebAPI.Filters;

/// <summary>
/// API日志过滤器
/// LOG-014: 记录所有Controller Action的执行情况
/// </summary>
public class ApiLoggingFilter : IAsyncActionFilter
{
    private readonly ILogger<ApiLoggingFilter> _logger;

    public ApiLoggingFilter(ILogger<ApiLoggingFilter> logger)
    {
        _logger = logger;
    }

    public async Task OnActionExecutionAsync(
        ActionExecutingContext context,
        ActionExecutionDelegate next)
    {
        var actionName = context.ActionDescriptor.DisplayName ?? "Unknown";
        var correlationId = context.HttpContext.TraceIdentifier;
        var sw = Stopwatch.StartNew();

        // LOG-014: 记录Action开始
        _logger.LogInformation(
            "[API] >>> {Action} started. CorrelationId={CorrelationId}",
            actionName,
            correlationId);

        // 记录参数（脱敏）- Debug级别
        if (context.ActionArguments.Count > 0 && _logger.IsEnabled(LogLevel.Debug))
        {
            var sanitizedParams = SanitizeParameters(context.ActionArguments);
            _logger.LogDebug(
                "[API] Parameters: {Parameters} CorrelationId={CorrelationId}",
                sanitizedParams,
                correlationId);
        }

        var executedContext = await next();
        sw.Stop();

        // LOG-014: 记录Action结束
        if (executedContext.Exception != null)
        {
            _logger.LogError(executedContext.Exception,
                "[API] !!! {Action} failed after {Duration}ms. CorrelationId={CorrelationId}",
                actionName,
                sw.ElapsedMilliseconds,
                correlationId);
        }
        else
        {
            _logger.LogInformation(
                "[API] <<< {Action} completed in {Duration}ms. CorrelationId={CorrelationId}",
                actionName,
                sw.ElapsedMilliseconds,
                correlationId);
        }
    }

    /// <summary>
    /// 脱敏参数值
    /// </summary>
    private static string SanitizeParameters(IDictionary<string, object?> parameters)
    {
        var sanitized = parameters
            .Where(p => p.Value != null)
            .Select(p => $"{p.Key}={SanitizeValue(p.Key, p.Value)}");
        return string.Join(", ", sanitized);
    }

    /// <summary>
    /// 脱敏单个值
    /// </summary>
    private static string SanitizeValue(string key, object? value)
    {
        if (value == null) return "null";

        // 敏感字段名检查
        if (SensitiveDataMasker.IsSensitiveFieldName(key))
        {
            return "[REDACTED]";
        }

        var type = value.GetType();

        // 基本类型直接返回
        if (type.IsPrimitive || type == typeof(string) || type == typeof(Guid) || type == typeof(DateTime))
        {
            var strValue = value.ToString() ?? "null";
            // 限制字符串长度
            if (strValue.Length > 100)
            {
                return $"{strValue[..100]}...";
            }
            return strValue;
        }

        // 复杂类型显示类型名
        return $"[{type.Name}]";
    }
}
