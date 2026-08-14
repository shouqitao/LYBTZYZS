using System.Windows.Input;

namespace LYBT.Desktop.Contracts.UI;

/// <summary>
/// 面包屑导航项
/// 导航架构改进方案 — 面包屑导航
/// 统一类型（desktop-dead-code-audit DC-005）：原 Contracts record（导航历史：Title/ViewName/IsCurrent）
/// + Controls class（BreadcrumbBar 渲染：Label/Level/IsCurrent/IsLast/NavigateCommand）合并——
/// Contracts 为主，扩展 Controls 所需属性（Level/IsLast/NavigateCommand），显示字段统一为 Title。
/// </summary>
public record BreadcrumbItem(
    string Title,
    string ViewName,
    bool IsCurrent,
    int Level = 0,
    bool IsLast = false,
    ICommand? NavigateCommand = null
);
