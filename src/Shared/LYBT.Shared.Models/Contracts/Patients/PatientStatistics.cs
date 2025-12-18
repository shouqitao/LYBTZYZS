namespace LYBT.Shared.Models.Contracts.Patients;

/// <summary>
/// 患者统计 - record类型
/// OpenSpec: refactor-dto-simplification - 简化为record
/// </summary>
public record PatientStatistics(
    int TotalCount,
    int ActiveCount,
    int NewThisMonth,
    int VisitThisMonth
);

/// <summary>
/// 患者主页统计 - record类型
/// OpenSpec: refactor-dto-simplification
/// </summary>
public record PatientMainStatistics(
    int TotalCount,
    int ActiveCount,
    int RecentVisitCount
);
