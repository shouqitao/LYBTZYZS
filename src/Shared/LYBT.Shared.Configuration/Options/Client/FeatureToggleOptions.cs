namespace LYBT.Shared.Configuration.Options.Client;

/// <summary>
/// 功能开关配置
/// </summary>
public sealed class FeatureToggleOptions
{
    public const string SectionName = "FeatureToggles";

    /// <summary>
    /// 重复药材合并策略: Max, Min, Sum, Import, Keep
    /// (PrescriptionSettingsService reads this via IConfiguration.GetValue)
    /// </summary>
    public string DuplicateHerbMergeStrategy { get; set; } = "Max";

    /// <summary>
    /// 同步冲突时是否覆盖服务器数据
    /// </summary>
    public bool OverwriteConflicts { get; set; } = false;
}
