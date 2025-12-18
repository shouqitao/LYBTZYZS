namespace LYBT.Shared.Models.Contracts.Formula;

/// <summary>
/// 验方统计 - record类型
/// OpenSpec: refactor-dto-simplification - 简化为record
/// </summary>
public record FormulaStatistics(
    int TotalCount,
    int SharedCount,
    int PrivateCount,
    int UsedCount,
    int DraftCount,
    int ValidatedCount
);

/// <summary>
/// 验方主页统计 - record类型
/// OpenSpec: refactor-dto-simplification
/// </summary>
public record FormulaMainStatistics(
    int TotalCount,
    int SharedCount,
    int RecentAddedCount
);
