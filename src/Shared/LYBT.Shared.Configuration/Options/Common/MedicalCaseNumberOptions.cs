namespace LYBT.Shared.Configuration.Options.Common;

/// <summary>
/// 医案编号配置（T2.3）
/// 集中管理 MC 前缀与流水位数，避免硬编码 $"MC{dateStr}" + D3 散落
/// </summary>
public sealed class MedicalCaseNumberOptions
{
    public const string SectionName = "MedicalCaseNumber";

    /// <summary>医案编号前缀</summary>
    public string Prefix { get; set; } = "MC";

    /// <summary>流水位数（补零）</summary>
    public int PadLength { get; set; } = 3;

    public const string DefaultPrefix = "MC";
    public const int DefaultPadLength = 3;
}
