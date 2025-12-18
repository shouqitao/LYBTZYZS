namespace LYBT.Shared.Models.Contracts.Prescriptions;

/// <summary>
/// 处方统计 - record类型
/// OpenSpec: refactor-dto-simplification - 简化为record
/// </summary>
public record PrescriptionStatistics(
    int TotalCount,
    int DraftCount,
    int PendingCount,
    int CompletedCount,
    int CancelledCount,
    decimal TotalAmount,
    decimal AverageAmount
);

/// <summary>
/// 处方主页统计 - record类型
/// Issue #1163: 为Desktop端PrescriptionsMainViewModel提供统计数据
/// </summary>
public record PrescriptionMainStatistics(
    int TotalCount,
    int TodayCount,
    decimal TodayTotalAmount
);

/// <summary>
/// 处方日期范围统计 - record类型
/// Issue #1163: 为Desktop端提供日期范围统计数据
/// </summary>
public record PrescriptionRangeStatistics(
    int Count,
    decimal TotalAmount,
    decimal AvgAmount
);
