namespace LYBT.Shared.Configuration.Options.Client;

/// <summary>
/// 处方配置
/// </summary>
public sealed class PrescriptionOptions
{
    public const string SectionName = "Prescription";

    /// <summary>
    /// 重复药材合并策略 (Max/Sum/First)
    /// </summary>
    public string DuplicateHerbMergeStrategy { get; set; } = "Max";
}
