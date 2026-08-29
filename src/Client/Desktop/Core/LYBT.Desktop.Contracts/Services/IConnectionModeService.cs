// ---------------------------------------------------------------------------
// IConnectionModeService — Connection mode management and switching
// ---------------------------------------------------------------------------
// Abstracts the dual-mode (Remote / Local) connection model on top of the
// URL-driven IConnectionSettingsService. Provides health probing for UI state
// and explicit user-driven mode switching — no automatic fallback.
// ---------------------------------------------------------------------------

namespace LYBT.Desktop.Contracts.Services;

/// <summary>
/// 连接模式限定符。由用户在 <see cref="Remote"/> 与 <see cref="Local"/> 之间
/// 显式选择，不自动探测或降级。
/// </summary>
public enum ConnectionMode
{
    /// <summary>远程 WebAPI（HTTP → SQL Server）。</summary>
    Remote,

    /// <summary>嵌入式 LocalWebAPI（→ SQL Server LocalDB）。</summary>
    Local
}

/// <summary>
/// 管理活动连接模式（Remote 或 Local）。模式由用户显式选择，
/// 远程健康探测仅用于 UI 状态显示。
/// </summary>
public interface IConnectionModeService
{
    /// <summary>
    /// 当前生效的模式。初始化后始终解析为 <see cref="ConnectionMode.Remote"/>
    /// 或 <see cref="ConnectionMode.Local"/>。
    /// </summary>
    ConnectionMode CurrentMode { get; }

    /// <summary>
    /// 当前模式的本地化显示标签
    /// （Remote 为"远程模式"，Local 为"本地模式"）。
    /// </summary>
    string CurrentModeDisplay { get; }

    /// <summary>当前生效模式为 <see cref="ConnectionMode.Remote"/> 时为 true。</summary>
    bool IsRemote { get; }

    /// <summary>当前生效模式为 <see cref="ConnectionMode.Local"/> 时为 true。</summary>
    bool IsLocal { get; }

    /// <summary>
    /// 上次远程可用性探测的缓存结果。由
    /// <see cref="CheckRemoteAvailableAsync"/> 更新。UI 据此启用或禁用
    /// "切换到 Remote" 按钮。
    /// </summary>
    bool IsRemoteAvailable { get; }

    /// <summary>含模式信息的 API 状态显示文本（例如"远程 WebAPI 已连接"）。</summary>
    string ApiStatusDisplay { get; }

    /// <summary>
    /// 重新探测配置的远程 URL，并将结果缓存到
    /// <see cref="IsRemoteAvailable"/>。返回探测结果。未配置
    /// 远程 URL 时不执行任何操作。
    /// </summary>
    /// <returns>True when the remote server is reachable.</returns>
    Task<bool> CheckRemoteAvailableAsync();

    /// <summary>
    /// 测试 <paramref name="url"/> 处的远程 WebAPI 是否可达。
    /// 对 <c>{url}/api/v1/health</c> 执行匿名 GET 请求。
    /// </summary>
    /// <param name="url">Remote server base URL (e.g., "http://192.168.1.10:5000").</param>
    /// <returns>True when the health endpoint responded successfully.</returns>
    Task<bool> TestRemoteConnectionAsync(string url);

    /// <summary>
    /// 显式切换生效模式（D1 设计：切换不阻塞，先切 URL 立即生效）。
    /// 相应地更新底层 <see cref="IConnectionSettingsService"/> 的 URL：
    /// <list type="bullet">
    ///   <item><see cref="ConnectionMode.Local"/> → 将 URL 指向 localhost。</item>
    ///   <item><see cref="ConnectionMode.Remote"/> → 保留当前远程 URL。</item>
    /// </list>
    /// 唯一阻断场景：未配置远程 URL（<c>NO_REMOTE_URL</c>）。远程不可达不阻塞切换——
    /// 模式立即生效，可达性由后台探测更新 <see cref="IsRemoteAvailable"/> 缓存；
    /// 未完成医案守卫（ERR-70506）同样在后台探测中执行。
    /// </summary>
    /// <param name="mode">The mode to activate.</param>
    Task<ModeSwitchResult> SetModeAsync(ConnectionMode mode);

    /// <summary>
    /// <see cref="CurrentMode"/> 变化时触发。载荷为新模式。
    /// </summary>
    event EventHandler<ConnectionMode>? ModeChanged;
}
