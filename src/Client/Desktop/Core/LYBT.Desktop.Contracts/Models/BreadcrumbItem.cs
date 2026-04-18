namespace LYBT.Desktop.Contracts.Models;

/// <summary>
/// 面包屑导航项
/// 导航架构改进方案 — 面包屑导航
/// </summary>
public record BreadcrumbItem(
    string Title,
    string ViewName,
    bool IsCurrent
);
