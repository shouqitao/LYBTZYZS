namespace LYBT.Desktop.Shell.Services;

using LYBT.Desktop.Foundation.HealthCheck;
using MaterialDesignThemes.Wpf;
using System.Windows.Media;

/// <summary>
/// 状态栏管理器接口
/// </summary>
public interface IStatusBarManager
{
    ApiHealthStatus ApiStatus { get; }
    string ConnectionUrl { get; }
    bool IsLocal { get; }
    string ConnectionModeDisplay { get; }
    bool IsRemoteMode { get; }
    string CurrentTimeDisplay { get; }
    PackIconKind ApiStatusIcon { get; }
    Brush ApiStatusColor { get; }
    void UpdateTime();
    Task ForceCheckAsync();
    void Dispose();
}
