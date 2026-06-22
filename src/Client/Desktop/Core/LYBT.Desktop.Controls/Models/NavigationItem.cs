using System.Windows.Input;

namespace LYBT.Desktop.Controls.Models;

/// <summary>
/// 侧边栏导航项数据模型
/// </summary>
public class NavigationItem
{
    public string Title { get; set; } = string.Empty;
    public string ViewName { get; set; } = string.Empty;

    /// <summary>
    /// MaterialDesign PackIcon Kind 字符串（如 "Home", "AccountGroup"）
    /// </summary>
    public string IconKind { get; set; } = string.Empty;

    public bool IsVisible { get; set; } = true;
    public ICommand? Command { get; set; }

    /// <summary>
    /// 导航项所属分组：可选 "主页" / "业务" / "管理"。默认 "业务"。
    /// </summary>
    public string Group { get; set; } = "业务";
}
