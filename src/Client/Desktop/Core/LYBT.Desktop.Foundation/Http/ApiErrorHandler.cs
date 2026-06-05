// ---------------------------------------------------------------------------
// ApiErrorHandler — Maps HTTP/Refit exceptions to ServiceResult<T>
// ---------------------------------------------------------------------------
// Provides a unified error-handling entry point for Desktop API calls.
// Converts Refit.ApiException, HttpRequestException, and generic exceptions
// into ServiceResult<T> with Chinese user-friendly messages.
//
// Reuses ClientErrorMessageMapper for status-code → message mapping to avoid
// duplicating the shared dictionary.
// ---------------------------------------------------------------------------

using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text.Json;
using LYBT.Shared.ExceptionHandling.Mappers;
using LYBT.Shared.Models.Contracts.Common;
using Refit;

namespace LYBT.Desktop.Foundation.Http;

/// <summary>
/// API错误处理器 — 将HTTP/Refit异常转换为统一的ServiceResult&lt;T&gt;
/// </summary>
/// <remarks>
/// <para>所有Desktop模块的API调用异常都应通过此类转换为 <see cref="ServiceResult{T}"/>，
/// 以确保用户看到一致的中文错误消息。</para>
/// <para>状态码映射委托给 <see cref="ClientErrorMessageMapper"/>，保持共享层单一数据源。</para>
/// </remarks>
public static class ApiErrorHandler
{
    private const string DefaultErrorMessage = "操作失败，请稍后重试";

    #region Refit ApiException handling

    /// <summary>
    /// 处理Refit ApiException，转换为ServiceResult&lt;T&gt;
    /// </summary>
    /// <typeparam name="T">响应数据类型</typeparam>
    /// <param name="ex">Refit ApiException</param>
    /// <returns>包含中文错误消息的ServiceResult</returns>
    public static ServiceResult<T> HandleRefitException<T>(ApiException ex)
    {
        var statusCode = ex.StatusCode;
        var message = GetMessageFromStatusCode(statusCode);

        // 尝试从响应内容中提取服务器返回的具体错误消息
        var serverMessage = TryExtractServerMessage(ex.Content);
        if (!string.IsNullOrWhiteSpace(serverMessage))
        {
            message = serverMessage;
        }

        return ServiceResult<T>.Failure(message, ex);
    }

    #endregion

    #region HttpRequestException handling

    /// <summary>
    /// 处理HttpRequestException，转换为ServiceResult&lt;T&gt;
    /// </summary>
    /// <typeparam name="T">响应数据类型</typeparam>
    /// <param name="ex">HttpRequestException</param>
    /// <param name="statusCode">可选的HTTP状态码（当异常未携带状态码时由调用方提供）</param>
    /// <returns>包含中文错误消息的ServiceResult</returns>
    public static ServiceResult<T> HandleHttpException<T>(HttpRequestException ex, HttpStatusCode? statusCode = null)
    {
        // 优先使用异常自带的状态码，其次使用调用方提供的状态码
        var effectiveStatusCode = ex.StatusCode ?? statusCode;

        if (effectiveStatusCode.HasValue)
        {
            var message = GetMessageFromStatusCode(effectiveStatusCode.Value);
            return ServiceResult<T>.Failure(message, ex);
        }

        // 无状态码时，根据内部异常类型判断
        var fallbackMessage = ex.InnerException switch
        {
            SocketException => "无法连接到服务器，请检查网络连接",
            TaskCanceledException => "请求超时，请稍后重试",
            _ => "网络请求失败，请稍后重试"
        };

        return ServiceResult<T>.Failure(fallbackMessage, ex);
    }

    #endregion

    #region Generic exception dispatcher

    /// <summary>
    /// 通用异常处理入口 — 自动分派到对应的处理方法
    /// </summary>
    /// <typeparam name="T">响应数据类型</typeparam>
    /// <param name="ex">异常实例</param>
    /// <returns>包含中文错误消息的ServiceResult</returns>
    /// <remarks>
    /// <para>分派逻辑：</para>
    /// <list type="number">
    ///   <item><see cref="ApiException"/> (Refit) → <see cref="HandleRefitException{T}"/></item>
    ///   <item><see cref="HttpRequestException"/> → <see cref="HandleHttpException{T}(HttpRequestException, HttpStatusCode?)"/></item>
    ///   <item><see cref="TaskCanceledException"/> / <see cref="TimeoutException"/> → 超时消息</item>
    ///   <item><see cref="SocketException"/> → 网络连接消息</item>
    ///   <item>其他 → 默认错误消息</item>
    /// </list>
    /// </remarks>
    public static ServiceResult<T> HandleException<T>(Exception ex)
    {
        return ex switch
        {
            ApiException refitEx => HandleRefitException<T>(refitEx),
            HttpRequestException httpEx => HandleHttpException<T>(httpEx),
            TaskCanceledException => ServiceResult<T>.Failure("操作已取消", ex),
            TimeoutException => ServiceResult<T>.Failure("操作超时，请稍后重试", ex),
            SocketException => ServiceResult<T>.Failure("无法连接到服务器，请检查网络连接", ex),
            _ => ServiceResult<T>.Failure(DefaultErrorMessage, ex)
        };
    }

    #endregion

    #region Internal helpers

    /// <summary>
    /// 从HTTP状态码获取中文错误消息
    /// </summary>
    private static string GetMessageFromStatusCode(HttpStatusCode statusCode)
    {
        return ClientErrorMessageMapper.GetUserMessageFromStatusCode(statusCode);
    }

    /// <summary>
    /// 从Refit响应内容中提取服务器返回的具体错误消息
    /// </summary>
    /// <param name="content">响应体JSON字符串</param>
    /// <returns>提取到的消息，提取失败返回null</returns>
    private static string? TryExtractServerMessage(string? content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return null;
        }

        try
        {
            using var doc = JsonDocument.Parse(content);
            var root = doc.RootElement;

            // 检查 message 字段（ApiResponse格式）
            if (root.TryGetProperty("message", out var messageProp) &&
                messageProp.ValueKind == JsonValueKind.String)
            {
                var message = messageProp.GetString();
                if (!string.IsNullOrWhiteSpace(message))
                {
                    return message;
                }
            }

            // 检查 detail 字段（ProblemDetails格式）
            if (root.TryGetProperty("detail", out var detailProp) &&
                detailProp.ValueKind == JsonValueKind.String)
            {
                var detail = detailProp.GetString();
                if (!string.IsNullOrWhiteSpace(detail))
                {
                    return detail;
                }
            }

            // 检查 title 字段（ProblemDetails格式）
            if (root.TryGetProperty("title", out var titleProp) &&
                titleProp.ValueKind == JsonValueKind.String)
            {
                var title = titleProp.GetString();
                if (!string.IsNullOrWhiteSpace(title))
                {
                    return title;
                }
            }
        }
        catch (JsonException)
        {
            // JSON解析失败，忽略
        }

        return null;
    }

    #endregion
}
