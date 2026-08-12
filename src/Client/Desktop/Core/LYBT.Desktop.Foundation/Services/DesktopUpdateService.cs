using LYBT.Desktop.Contracts.Services;
using LYBT.Shared.Configuration.Options.Server;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Velopack;

namespace LYBT.Desktop.Foundation.Services;

/// <summary>
/// Desktop 自动更新服务（US-SHELL-010: Velopack 1.2.0 UpdateManager——检查/下载/应用）
/// 更新源 = 服务器 /releases/（DesktopUpdate:FeedUrl 绝对地址）
/// </summary>
public class DesktopUpdateService : IDesktopUpdateService
{
    private readonly DesktopUpdateOptions _options;
    private readonly ILogger<DesktopUpdateService> _logger;
    private UpdateManager? _manager;

    public DesktopUpdateService(
        IOptions<DesktopUpdateOptions> options,
        ILogger<DesktopUpdateService> logger)
    {
        _options = options.Value ?? new DesktopUpdateOptions();
        _logger = logger;
    }

    private bool IsEnabled => _options.Enabled && !string.IsNullOrWhiteSpace(_options.FeedUrl);

    private UpdateManager GetManager()
    {
        if (_manager is null)
        {
            _manager = new UpdateManager(
                _options.FeedUrl!,
                new UpdateOptions());
        }
        return _manager;
    }

    /// <inheritdoc />
    public async Task<DesktopUpdateInfo?> CheckForUpdatesAsync()
    {
        if (!IsEnabled)
            return null;

        try
        {
            var update = await GetManager().CheckForUpdatesAsync();
            if (update?.TargetFullRelease is null)
                return null;

            return new DesktopUpdateInfo(
                update.TargetFullRelease.Version.ToString(),
                update.TargetFullRelease.NotesMarkdown);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[UPDATE] 更新检查失败（服务器不可达或更新源未配置）");
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<bool> DownloadUpdateAsync()
    {
        if (!IsEnabled) return false;
        try
        {
            var update = await GetManager().CheckForUpdatesAsync();
            if (update?.TargetFullRelease is null) return false;
            await GetManager().DownloadUpdatesAsync(update);
            _logger.LogInformation("[UPDATE] 更新包下载完成: {Version}", update.TargetFullRelease.Version);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[UPDATE] 下载更新失败");
            return false;
        }
    }

    /// <inheritdoc />
    public void ApplyUpdateAndRestart()
    {
        if (!IsEnabled) return;
        try
        {
            var update = GetManager().CheckForUpdatesAsync().GetAwaiter().GetResult();
            if (update?.TargetFullRelease is null) return;
            GetManager().ApplyUpdatesAndRestart(update.TargetFullRelease, Array.Empty<string>());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[UPDATE] 应用更新失败");
        }
    }
}
