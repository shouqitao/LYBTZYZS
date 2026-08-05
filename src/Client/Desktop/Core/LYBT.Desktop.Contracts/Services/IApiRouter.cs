// ---------------------------------------------------------------------------
// IApiRouter — API connection query interface (simplified)
// ---------------------------------------------------------------------------
// Provides read-only access to the current connection URL and local-mode
// status. No manual override or mode switching — URL is managed by
// IConnectionSettingsService.
// ---------------------------------------------------------------------------

namespace LYBT.Desktop.Contracts.Services;

/// <summary>
/// 当前 API 连接的只读查询接口。
/// 委托给 <see cref="IConnectionSettingsService"/>。
/// </summary>
public interface IApiRouter
{
    /// <summary>当前连接 URL。</summary>
    string CurrentUrl { get; }

    /// <summary>当前 URL 是否指向本地服务。</summary>
    bool IsLocal { get; }
}
