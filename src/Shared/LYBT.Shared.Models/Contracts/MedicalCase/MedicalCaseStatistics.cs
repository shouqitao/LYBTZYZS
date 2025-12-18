namespace LYBT.Shared.Models.Contracts.MedicalCase;

/// <summary>
/// 医案统计 - record类型
/// OpenSpec: refactor-dto-simplification - 简化为record
/// </summary>
public record MedicalCaseStatistics(
    int TotalCount,
    int InProgressCount,
    int CompletedCount,
    int CancelledCount
);

/// <summary>
/// 医案主页统计 - record类型
/// OpenSpec: refactor-dto-simplification
/// </summary>
public record MedicalCaseMainStatistics(
    int TotalCount,
    int TodayCount,
    int InProgressCount
);
