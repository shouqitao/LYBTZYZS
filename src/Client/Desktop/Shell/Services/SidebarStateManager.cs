using CommunityToolkit.Mvvm.ComponentModel;

namespace LYBT.Desktop.Shell.Services;

/// <summary>
/// 侧边栏状态管理器实现（Shell 单例）。
/// 宽度常量唯一来源 <see cref="ShellConstants"/>；宽度由展开态推导，不单独存储（避免两处状态漂移）。
/// </summary>
public partial class SidebarStateManager : ObservableObject, ISidebarStateManager
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SidebarWidth))]
    [NotifyPropertyChangedFor(nameof(IsNavTextVisible))]
    private bool _isSidebarExpanded;

    /// <inheritdoc/>
    public double SidebarWidth => IsSidebarExpanded
        ? ShellConstants.SidebarExpandedWidth
        : ShellConstants.SidebarCollapsedWidth;

    /// <inheritdoc/>
    public bool IsNavTextVisible => IsSidebarExpanded;

    /// <inheritdoc/>
    public void Toggle() => IsSidebarExpanded = !IsSidebarExpanded;
}
