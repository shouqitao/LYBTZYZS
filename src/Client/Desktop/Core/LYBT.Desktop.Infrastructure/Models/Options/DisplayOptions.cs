namespace LYBT.Desktop.Infrastructure.Models.Options;

/// <summary>
/// 控件显示配置选项（不可变）
/// OpenSpec: unify-control-data-binding
/// </summary>
public record DisplayOptions(
    bool IsCompactMode = false,
    bool ShowHeader = true,
    bool ShowFooter = true
);
