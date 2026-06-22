using System.Windows.Input;
using System.Windows.Media;

namespace LYBT.Desktop.Controls.Models;

/// <summary>
/// 侧边栏导航项数据模型
/// </summary>
public class NavigationItem
{
    public string Title { get; set; } = string.Empty;
    public string ViewName { get; set; } = string.Empty;
    public Geometry? IconData { get; set; }
    public bool IsVisible { get; set; } = true;
    public ICommand? Command { get; set; }

    /// <summary>
    /// 导航项所属分组：可选 "主页" / "业务" / "管理"。默认 "业务"。
    /// </summary>
    public string Group { get; set; } = "业务";
}
