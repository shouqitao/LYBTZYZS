// ---------------------------------------------------------------------------
// ApiErrorEnvelope — 错误响应体解析（本地/远程单一实现）
// ---------------------------------------------------------------------------
// 服务端错误响应有两种形状（X-3 后 Server 异常路径统一 ProblemDetails，ApiResponse 保留兼容）：
//   ① ProblemDetails（RFC 7807，异常路径首选）：
//      { type, title, status, detail, instance, errorCode, correlationId, traceId, ... }
//   ② ApiResponse 信封（已知业务失败 / 旧格式回退）：
//      { success, message, errors: { code, correlationId, traceId }, requestId }
// 本类从原始响应体提取「面向用户的消息」与「错误码」，供：
//   - HttpApiClientBase.EnsureSuccessOrThrowAsync（本地非 2xx）
//   - RefitSettings.ExceptionFactory（远程非 2xx）
// 共用，避免两条传输路径各写一份解析。
// 解析顺序：优先 ProblemDetails（detail/errorCode），回退 ApiResponse（message/errors.code）。
// ---------------------------------------------------------------------------

using System.Text.Json;

namespace LYBT.Desktop.Foundation.Http;

/// <summary>
/// 错误响应体解析工具（大小写不敏感，解析失败返回空值，绝不抛异常）。
/// </summary>
public static class ApiErrorEnvelope
{
    /// <summary>
    /// 从错误响应体提取错误码与面向用户的消息。
    /// 优先 ProblemDetails（<c>detail</c>/<c>title</c> + 根级 <c>errorCode</c>），
    /// 回退 ApiResponse 信封（<c>message</c> + <c>errors.code</c>）。
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

            // ---- ① ProblemDetails（RFC 7807）----
            // 识别特征：含 status（数字）或同时有 title+detail；errorCode 为 ProblemDetails Extensions 根级展开。
            var looksLikeProblemDetails =
                (fields.TryGetValue("status", out var statusEl) && statusEl.ValueKind == JsonValueKind.Number)
                || (fields.ContainsKey("title") && fields.ContainsKey("detail"));

            if (looksLikeProblemDetails)
            {
                string? message = null;
                foreach (var key in new[] { "detail", "title" })
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
                if (fields.TryGetValue("errorCode", out var errorCodeEl)
                    && errorCodeEl.ValueKind == JsonValueKind.String)
                {
                    code = errorCodeEl.GetString();
                }

                if (message is not null || code is not null)
                    return (code, message);
            }

            // ---- ② ApiResponse 信封（回退）----
            string? envelopeMessage = null;
            foreach (var key in new[] { "message", "detail", "title" })
            {
                if (fields.TryGetValue(key, out var value)
                    && value.ValueKind == JsonValueKind.String
                    && !string.IsNullOrWhiteSpace(value.GetString()))
                {
                    envelopeMessage = value.GetString();
                    break;
                }
            }

            string? envelopeCode = null;
            if (fields.TryGetValue("errors", out var errors)
                && errors.ValueKind == JsonValueKind.Object
                && errors.TryGetProperty("code", out var codeElement)
                && codeElement.ValueKind == JsonValueKind.String)
            {
                envelopeCode = codeElement.GetString();
            }

            return (envelopeCode, envelopeMessage);
        }
        catch (JsonException)
        {
            return (null, null);
        }
    }
}
