namespace LYBT.Desktop.Auth.Models;

/// <summary>
/// 初始化向导完成页的校验清单项（B-07 Step 5）。
/// </summary>
public sealed class WizardChecklistItemModel
{
    /// <summary>校验项标题</summary>
    public string Title { get; init; } = string.Empty;

    /// <summary>是否通过</summary>
    public bool IsPassed { get; init; }

    /// <summary>说明（通过/未通过的具体值或原因）</summary>
    public string Detail { get; init; } = string.Empty;

    /// <summary>状态文本（供列表展示）</summary>
    public string StatusText => IsPassed ? "✓ 通过" : "✗ 待处理";
}
