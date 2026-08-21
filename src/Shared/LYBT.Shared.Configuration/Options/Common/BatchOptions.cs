namespace LYBT.Shared.Configuration.Options.Common;

/// <summary>
/// 批量操作配置（T1.3）
/// 集中管理所有批量接口的上限，避免散落硬编码 100
/// 默认 100，满足 US-HERB-012/US-PAT-012/US-MC-015 等批量上限验收
/// </summary>
public sealed class BatchOptions
{
    public const string SectionName = "Batch";

    /// <summary>批量单次默认上限</summary>
    public const int DefaultMaxBatchSize = 100;

    /// <summary>批量单次上限（可通过配置覆盖）</summary>
    public int MaxBatchSize { get; set; } = DefaultMaxBatchSize;

    /// <summary>导入单次上限（药材/验方/患者）</summary>
    public int MaxImportSize { get; set; } = 10000;
}
