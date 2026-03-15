using System.Net;
using System.Net.Http;
using LYBT.Desktop.Sync.ViewModels;
using Refit;

namespace LYBT.Desktop.Sync.Services;

/// <summary>
/// 同步错误分类器 - 将异常分类为可重试的错误类别
/// </summary>
public static class SyncErrorClassifier
{
    /// <summary>
    /// 根据异常类型和状态码分类错误
    /// </summary>
    public static SyncErrorCategory Classify(Exception ex)
    {
        if (ex is HttpRequestException or TaskCanceledException)
            return SyncErrorCategory.TransientNetwork;

        if (ex is ApiException apiEx)
        {
            return apiEx.StatusCode switch
            {
                HttpStatusCode.Unauthorized => SyncErrorCategory.AuthExpired,
                HttpStatusCode.Conflict => SyncErrorCategory.ConflictChanged,
                >= HttpStatusCode.BadRequest and < HttpStatusCode.InternalServerError
                    => SyncErrorCategory.BusinessReject,
                _ => SyncErrorCategory.Unknown
            };
        }

        return SyncErrorCategory.Unknown;
    }

    /// <summary>
    /// 判断错误类别是否可重试
    /// </summary>
    public static bool IsRetryable(SyncErrorCategory category)
    {
        return category is SyncErrorCategory.TransientNetwork
            or SyncErrorCategory.ConflictChanged
            or SyncErrorCategory.AuthExpired;
    }
}
