// ---------------------------------------------------------------------------
// IConnectionSettingsService — Connection URL management interface
// ---------------------------------------------------------------------------
// Manages the active connection URL for API access. Replaces the ApiMode
// concept with URL-driven connection selection:
//   - 127.0.0.1 / localhost → HttpClientApiClient (LocalWebAPI)
//   - any other address → RefitApiClient (Remote WebAPI)
// ---------------------------------------------------------------------------

namespace LYBT.Desktop.Contracts.Services;

/// <summary>
/// 管理活动 API 连接 URL，并在会话之间持久化保存。
/// </summary>
public interface IConnectionSettingsService
{
    /// <summary>当前连接 URL（例如 "http://192.168.190.248:5000"）。</summary>
    string CurrentUrl { get; }

    /// <summary>
    /// 当前 URL 是否指向本地服务
    /// （包含 "127.0.0.1" 或 "localhost"）。
    /// </summary>
    bool IsLocal { get; }

    /// <summary>LocalWebAPI 固定地址（始终为 http://localhost:5300）。</summary>
    string LocalUrl { get; }

    /// <summary>已保存的远程服务器 URL（持久化于 appsettings.json）。</summary>
    string RemoteUrl { get; }

    /// <summary>上次首选模式："Local" 或 "Remote"（已持久化）。</summary>
    string PreferredMode { get; }

    /// <summary>
    /// 设置新的连接 URL，持久化保存并通知订阅者。
    /// </summary>
    /// <param name="url">The new URL (e.g., "http://192.168.190.248:5000").</param>
    Task SetUrlAsync(string url);

    /// <summary>将远程 URL 保存到持久化存储。</summary>
    Task SaveRemoteUrlAsync(string url);

    /// <summary>将首选模式保存到持久化存储。</summary>
    Task SavePreferredModeAsync(string mode);

    /// <summary>连接 URL 变化时触发。载荷为新 URL。</summary>
    event EventHandler<string>? UrlChanged;

    /// <summary>
    /// 验证 URL 字符串是否为格式正确的 HTTP URL。
    /// </summary>
    /// <param name="url">URL string to validate.</param>
    /// <returns>True if the URL starts with "http://" or "https://" and is a valid URI.</returns>
    bool IsValidUrl(string url);
}
