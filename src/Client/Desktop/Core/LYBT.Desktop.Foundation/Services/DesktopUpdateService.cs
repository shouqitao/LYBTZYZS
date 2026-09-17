using LYBT.Desktop.Contracts.Services;
using LYBT.Shared.Configuration.Options.Server;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Velopack;

namespace LYBT.Desktop.Foundation.Services;

/// <summary>
/// Desktop 自动更新服务（US-SHELL-010/012: Velopack 1.2.0 UpdateManager——检查/下载/应用）。
/// </summary>
/// <remarks>
/// <para>更新源由 <see cref="IUpdateSourceFactory"/> 按配置构造：自建静态目录（<c>FeedUrl</c>）或
/// Gitee Releases（<c>GiteeRepoUrl</c>）。</para>
/// <para>检查结果会被缓存：下载与应用复用同一次检查得到的 <see cref="UpdateInfo"/>，
/// 避免「检查→下载→应用」触发三次馈源往返，也避免馈源在两次调用之间变化导致应用错版本。</para>
/// </remarks>
public class DesktopUpdateService : IDesktopUpdateService, IDisposable
{
    private readonly DesktopUpdateOptions _options;
    private readonly IUpdateSourceFactory _sourceFactory;
    private readonly ILogger<DesktopUpdateService> _logger;
    private readonly SemaphoreSlim _gate = new(1, 1);

    private UpdateManager? _manager;
    private UpdateInfo? _pendingUpdate;
    private bool _disposed;

    public DesktopUpdateService(
        IOptions<DesktopUpdateOptions> options,
        IUpdateSourceFactory sourceFactory,
        ILogger<DesktopUpdateService> logger)
    {
        _options = options.Value ?? new DesktopUpdateOptions();
        _sourceFactory = sourceFactory ?? throw new ArgumentNullException(nameof(sourceFactory));
        _logger = logger;
    }

    /// <summary>
    /// 惰性创建 <see cref="UpdateManager"/>（配置在运行期不变，故可缓存；
    /// 也避免每次调用都新建 <see cref="IUpdateSourceFactory"/> 产出的下载器）。
    /// </summary>
    /// <returns>管理器；未启用、配置不完整、或运行环境不受 Velopack 管理时返回 null。</returns>
    /// <remarks>
    /// <see cref="UpdateManager"/> 的构造依赖 <c>VelopackApp.Build().Run()</c> 注册的定位器
    /// （未注册时抛 <c>InvalidOperationException: No VelopackLocator has been set</c>）。
    /// 开发态直接运行、绿色解压运行等场景都属于此类，此时自动更新整体不适用——
    /// 这里降级为「不可用」并记录日志，**不得**让可选能力影响启动。
    /// </remarks>
    private UpdateManager? GetManager()
    {
        if (_manager is not null)
            return _manager;

        if (!_options.Enabled)
            return null;

        try
        {
            var source = _sourceFactory.Create(_options);
            if (source is null)
                return null;

            _manager = new UpdateManager(source, new UpdateOptions());
            return _manager;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "[UPDATE] 更新管理器初始化失败（未通过 Velopack 安装，或更新源配置无效）——本次跳过自动更新");
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<DesktopUpdateInfo?> CheckForUpdatesAsync()
    {
        var manager = GetManager();
        if (manager is null)
            return null;

        await _gate.WaitAsync().ConfigureAwait(false);
        try
        {
            var update = await manager.CheckForUpdatesAsync().ConfigureAwait(false);
            if (update?.TargetFullRelease is null)
            {
                _pendingUpdate = null;
                return null;
            }

            _pendingUpdate = update;
            return new DesktopUpdateInfo(
                update.TargetFullRelease.Version.ToString(),
                update.TargetFullRelease.NotesMarkdown);
        }
        catch (Exception ex)
        {
            // 服务器/Gitee 不可达属预期场景（离线诊所），静默降级为「无更新」
            _logger.LogWarning(ex, "[UPDATE] 更新检查失败（更新源不可达或未配置）");
            return null;
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <inheritdoc />
    public async Task<bool> DownloadUpdateAsync()
    {
        var manager = GetManager();
        if (manager is null)
            return false;

        await _gate.WaitAsync().ConfigureAwait(false);
        try
        {
            var update = _pendingUpdate ?? await manager.CheckForUpdatesAsync().ConfigureAwait(false);
            if (update?.TargetFullRelease is null)
            {
                _logger.LogInformation("[UPDATE] 下载请求时无可用更新");
                return false;
            }

            _pendingUpdate = update;
            await manager.DownloadUpdatesAsync(update).ConfigureAwait(false);
            _logger.LogInformation("[UPDATE] 更新包下载完成: {Version}", update.TargetFullRelease.Version);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[UPDATE] 下载更新失败");
            return false;
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <inheritdoc />
    public async Task ApplyUpdateAndRestartAsync()
    {
        var manager = GetManager();
        if (manager is null)
            return;

        await _gate.WaitAsync().ConfigureAwait(false);
        try
        {
            var asset = _pendingUpdate?.TargetFullRelease;
            if (asset is null)
            {
                _logger.LogWarning("[UPDATE] 应用更新失败：无已下载的待应用版本（请先检查并下载更新）");
                return;
            }

            // 重启后由 Velopack 以 --veloapp-updated 拉起本进程（App.OnStartup 的 VelopackApp 钩子处理）
            manager.ApplyUpdatesAndRestart(asset, Array.Empty<string>());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[UPDATE] 应用更新失败");
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <inheritdoc />
    [Obsolete("Use ApplyUpdateAndRestartAsync instead")]
    public void ApplyUpdateAndRestart()
    {
        // R-21: 同步兼容层——ConfigureAwait(false) + GetAwaiter().GetResult()
        // 相对 .Wait() 不包装 AggregateException、可传播原始堆栈；
        // ConfigureAwait(false) 避免捕获 WPF Dispatcher 上下文（ApplyUpdateAndRestartAsync 内部亦已 ConfigureAwait(false)）
        ApplyUpdateAndRestartAsync().ConfigureAwait(false).GetAwaiter().GetResult();
    }

    /// <summary>
    /// 释放内部并发闸门（单例，由容器在退出时调用）。
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _gate.Dispose();
        GC.SuppressFinalize(this);
    }
}
