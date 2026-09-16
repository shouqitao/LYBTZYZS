// ---------------------------------------------------------------------------
// ApiErrorEnvelope — 错误响应体解析（本地/远程单一实现）
// ---------------------------------------------------------------------------
// 服务端错误响应有两种形状：
//   ① ApiResponse 信封：{ success, message, errors: { code, correlationId, traceId }, requestId }
//   ② ProblemDetails ：{ title, detail, status }
// 本类从原始响应体提取「面向用户的消息」与「错误码」，供：
//   - HttpApiClientBase.EnsureSuccessOrThrowAsync（本地非 2xx）
//   - RefitSettings.ExceptionFactory（远程非 2xx）
// 共用，避免两条传输路径各写一份解析。
// ---------------------------------------------------------------------------

using System.Text.Json;

namespace LYBT.Desktop.Foundation.Http;

/// <summary>
/// 错误响应体解析工具（大小写不敏感，解析失败返回空值，绝不抛异常）。
/// </summary>
public static class ApiErrorEnvelope
{
    /// <summary>
    /// 从错误响应体提取 <c>message</c>（或 ProblemDetails 的 <c>detail</c>/<c>title</c>）与 <c>errors.code</c>。
    /// </summary>
    /// <param name="body">原始响应体。</param>
    /// <returns>错误码与服务端消息；无法解析时均为 null。</returns>
    public static (string? ErrorCode, string? ServerMessage) TryExtract(string? body)
    {
        if (string.IsNullOrWhiteSpace(body))
            return (null, null);

        try
        {
            using var document = JsonDocument.Parse(body);
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
                return (null, null);

            var fields = new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase);
            foreach (var property in root.EnumerateObject())
                fields[property.Name] = property.Value;

            string? message = null;
            foreach (var key in new[] { "message", "detail", "title" })
            {
                if (fields.TryGetValue(key, out var value)
                    && value.ValueKind == JsonValueKind.String
                    && !string.IsNullOrWhiteSpace(value.GetString()))
                {
                    message = value.GetString();
                    break;
                }
            }

            string? code = null;
            if (fields.TryGetValue("errors", out var errors)
                && errors.ValueKind == JsonValueKind.Object
                && errors.TryGetProperty("code", out var codeElement)
                && codeElement.ValueKind == JsonValueKind.String)
            {
                code = codeElement.GetString();
            }

            return (code, message);
        }
        catch (JsonException)
        {
            return (null, null);
        }
    }
}
