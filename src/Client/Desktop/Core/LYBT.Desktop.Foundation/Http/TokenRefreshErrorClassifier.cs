using System.Net;
using LYBT.Desktop.Foundation.Security;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Foundation.Http;

/// <summary>
/// RefreshToken 失败响应的**分类器**（A-04 职责提取，2026-09-26）。
/// </summary>
/// <remarks>
/// <para>从 <see cref="TokenRefreshHandler"/> 抽出：HTTP 状态码/错误文案 → <see cref="TokenRefreshFailureReason"/>
/// 的判定是**纯函数**，与刷新编排（重试、并发闸、AutoLogin 降级、事件发布）无关。</para>
/// <para><b>行为不变</b>：方法体原样迁移（含日志语句与判定顺序）；调用方通过
/// <c>using static</c> 保持调用点不变。</para>
/// </remarks>
internal static class TokenRefreshErrorClassifier
{
    /// <summary>按 HTTP 状态码分类刷新失败，返回统一的失败结果（含告警日志）。</summary>
    internal static async Task<TokenRefreshResult> ClassifyAsync(HttpResponseMessage response, ILogger logger)
    {
        var errorContent = await response.Content.ReadAsStringAsync();
        logger.LogWarning("RefreshToken API调用失败 [StatusCode: {StatusCode}] [Error: {Error}]",
            response.StatusCode, errorContent);

        // 根据HTTP状态码分类错误
        return response.StatusCode switch
        {
            HttpStatusCode.Unauthorized => CategorizeUnauthorizedError(errorContent),
            HttpStatusCode.Forbidden => TokenRefreshResult.Failed(TokenRefreshFailureReason.UserDisabled, "账户被禁用"),
            HttpStatusCode.BadRequest => CategorizeApiError(errorContent),
            >= HttpStatusCode.InternalServerError => TokenRefreshResult.Failed(TokenRefreshFailureReason.ServerError, "服务器错误"),
            _ => TokenRefreshResult.Failed(TokenRefreshFailureReason.Unknown, $"HTTP {(int)response.StatusCode}: {errorContent}")
        };
    }

    /// <summary>
    /// 分类401未授权错误
    /// </summary>
    private static TokenRefreshResult CategorizeUnauthorizedError(string errorContent)
    {
        var lowerContent = errorContent.ToLowerInvariant();

        if (lowerContent.Contains("expired") || lowerContent.Contains("过期"))
        {
            return TokenRefreshResult.Failed(TokenRefreshFailureReason.RefreshTokenExpired, "RefreshToken已过期");
        }

        if (lowerContent.Contains("revoked") || lowerContent.Contains("撤销") || lowerContent.Contains("invalidated"))
        {
            return TokenRefreshResult.Failed(TokenRefreshFailureReason.RefreshTokenRevoked, "RefreshToken已被撤销");
        }

        if (lowerContent.Contains("invalid") || lowerContent.Contains("无效"))
        {
            return TokenRefreshResult.Failed(TokenRefreshFailureReason.RefreshTokenInvalid, "RefreshToken无效");
        }

        // 默认视为过期
        return TokenRefreshResult.Failed(TokenRefreshFailureReason.RefreshTokenExpired, "认证失败");
    }

    /// <summary>
    /// 分类API业务错误
    /// </summary>
    internal static TokenRefreshResult CategorizeApiError(string errorMessage)
    {
        var lowerMessage = errorMessage.ToLowerInvariant();

        if (lowerMessage.Contains("disabled") || lowerMessage.Contains("禁用"))
        {
            return TokenRefreshResult.Failed(TokenRefreshFailureReason.UserDisabled, errorMessage);
        }

        if (lowerMessage.Contains("expired") || lowerMessage.Contains("过期"))
        {
            return TokenRefreshResult.Failed(TokenRefreshFailureReason.RefreshTokenExpired, errorMessage);
        }

        if (lowerMessage.Contains("invalid") || lowerMessage.Contains("无效"))
        {
            return TokenRefreshResult.Failed(TokenRefreshFailureReason.RefreshTokenInvalid, errorMessage);
        }

        return TokenRefreshResult.Failed(TokenRefreshFailureReason.Unknown, errorMessage);
    }

    /// <summary>
    /// 创建失败事件参数
    /// </summary>
    internal static TokenRefreshFailedEventArgs CreateFailedEventArgs(
        TokenRefreshFailureReason reason, string detailedMessage)
    {
        return reason switch
        {
            TokenRefreshFailureReason.NetworkError => TokenRefreshFailedEventArgs.NetworkError(detailedMessage),
            TokenRefreshFailureReason.RefreshTokenExpired => TokenRefreshFailedEventArgs.RefreshTokenExpired(detailedMessage),
            TokenRefreshFailureReason.RefreshTokenRevoked => TokenRefreshFailedEventArgs.RefreshTokenRevoked(detailedMessage),
            TokenRefreshFailureReason.RefreshTokenInvalid => TokenRefreshFailedEventArgs.RefreshTokenInvalid(detailedMessage),
            TokenRefreshFailureReason.ServerError => TokenRefreshFailedEventArgs.ServerError(detailedMessage),
            TokenRefreshFailureReason.UserDisabled => TokenRefreshFailedEventArgs.UserDisabled(detailedMessage),
            _ => new TokenRefreshFailedEventArgs(reason, "刷新失败，请稍后重试", detailedMessage, canRetry: true, requiresReLogin: false)
        };
    }
}
