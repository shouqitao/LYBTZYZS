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
}
