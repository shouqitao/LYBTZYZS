using System.ComponentModel;

namespace LYBT.Desktop.Shell.Services;

/// <summary>
/// 侧边栏状态管理器 — 展开/收拢宽度与文字的单一真相源（SSOT）。
/// <para>宿主（MainWindowViewModel）与侧栏视图模型（SideNavViewModel）均只读/写此服务，
/// 避免两侧各持一份状态导致 Ctrl+M 与汉堡按钮不同步。</para>
/// </summary>
public interface ISidebarStateManager : INotifyPropertyChanged
{
    /// <summary>是否展开（true = 展开 240 / false = 收拢 64）</summary>
    bool IsSidebarExpanded { get; set; }

    /// <summary>当前侧栏宽度（由 <see cref="IsSidebarExpanded"/> 与 ShellConstants 推导）</summary>
    double SidebarWidth { get; }

    /// <summary>是否显示导航文字（收拢态仅图标）</summary>
    bool IsNavTextVisible { get; }

    /// <summary>切换展开/收拢</summary>
    void Toggle();
}
