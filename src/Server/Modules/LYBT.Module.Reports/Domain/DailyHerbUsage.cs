namespace LYBT.Module.Reports.Domain;

/// <summary>
/// 每日草药使用汇总值对象。
/// </summary>
public record DailyHerbUsage(IReadOnlyList<HerbUsageItem> Items);

/// <summary>
/// 单味草药使用统计。
/// </summary>
public record HerbUsageItem(string HerbName, int UsageCount, decimal TotalDosage);


