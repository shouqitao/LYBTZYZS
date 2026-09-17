namespace LYBT.Desktop.Contracts.Services;

/// <summary>
/// Desktop 自动更新服务（US-SHELL-010: Velopack 检查/下载/应用——更新源 = 服务器 /releases/）
/// </summary>
public interface IDesktopUpdateService
{
    /// <summary>检查更新（服务器不可达/未配置 → null——静默）</summary>
    Task<DesktopUpdateInfo?> CheckForUpdatesAsync();

    /// <summary>下载更新包（有可用更新时返回 true）</summary>
    Task<bool> DownloadUpdateAsync();

    /// <summary>应用更新并重启应用（提示用户保存后调用）</summary>
    Task ApplyUpdateAndRestartAsync();

    /// <summary>应用更新并重启应用（同步兼容——内部 ConfigureAwait(false) 避免 Dispatcher 死锁，已废弃请用 Async）</summary>
    [Obsolete("Use ApplyUpdateAndRestartAsync instead")]
    void ApplyUpdateAndRestart();
}

/// <summary>更新检查结果</summary>
public sealed record DesktopUpdateInfo(string NewVersion, string? NotesFilename);
