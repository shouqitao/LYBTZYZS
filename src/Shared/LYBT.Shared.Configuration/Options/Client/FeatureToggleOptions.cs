namespace LYBT.Shared.Configuration.Options.Client;

/// <summary>
/// 功能开关配置
/// </summary>
public sealed class FeatureToggleOptions
{
    public const string SectionName = "FeatureToggles";

    // Prescription 模块
    public bool PrescriptionCreate { get; set; } = false;
    public bool PrescriptionDelete { get; set; } = false;
    public bool PrescriptionClone { get; set; } = true;
    public bool PrescriptionExport { get; set; } = true;
    public bool PrescriptionViewDetail { get; set; } = true;
    public bool PrescriptionSearch { get; set; } = true;

    // MedicalCase 模块
    public bool MedicalCaseCreate { get; set; } = true;
    public bool MedicalCaseEdit { get; set; } = true;
    public bool MedicalCaseDelete { get; set; } = true;
    public bool MedicalCaseViewDetail { get; set; } = true;
    public bool MedicalCaseSearch { get; set; } = true;

    // 硬件设备
    /// <summary>
    /// T5-P2-44: 读卡器功能开关，默认关闭
    /// </summary>
    public bool CardReaderEnabled { get; set; } = false;

    // 处方配置 (从 PrescriptionOptions 合并)
    /// <summary>
    /// 重复药材合并策略: Max, Min, Sum, Import, Keep
    /// </summary>
    public string DuplicateHerbMergeStrategy { get; set; } = "Max";

    // 同步配置 (从 SyncOptions 合并)
    /// <summary>
    /// 同步冲突时是否覆盖服务器数据
    /// </summary>
    public bool OverwriteConflicts { get; set; } = false;
}
