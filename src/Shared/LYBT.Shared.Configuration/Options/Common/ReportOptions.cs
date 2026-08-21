namespace LYBT.Shared.Configuration.Options.Common;

/// <summary>
/// 报表配置（T2.3）
/// 集中管理报表 Top 阈值，避免散落硬编码 10/50
/// </summary>
public sealed class ReportOptions
{
    public const string SectionName = "Report";

    /// <summary>热门药材排行默认 Top</summary>
    public int DefaultTop { get; set; } = 10;

    /// <summary>热门药材排行最大 Top</summary>
    public int MaxTop { get; set; } = 50;

    public const int DefaultTopConst = 10;
    public const int MaxTopConst = 50;
}
