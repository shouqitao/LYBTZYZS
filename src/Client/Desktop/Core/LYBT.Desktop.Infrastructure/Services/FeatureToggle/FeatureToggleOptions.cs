namespace LYBT.Desktop.Infrastructure.Services.FeatureToggle;

/// <summary>
/// 功能开关配置（T8: US-CFG-004）。
/// v1.0 仅策略级 2 键（业务规则 1）——不回到已废弃的 18 布尔开关反模式。
/// </summary>
public class FeatureToggleOptions
{
    /// <summary>配置节名（feature-toggles.json 根节点）</summary>
    public const string SectionName = "FeatureToggles";

    /// <summary>
    /// 同步/导入冲突时是否自动覆盖（false → 提示而非覆盖）
    /// </summary>
    public bool OverwriteConflicts { get; set; } = true;

    /// <summary>
    /// 处方合并重复药材策略（"Max"/"Min"/"Sum"/"Average"/"First"，非法值回退 Max）
    /// </summary>
    public string DuplicateHerbMergeStrategy { get; set; } = "Max";
}
