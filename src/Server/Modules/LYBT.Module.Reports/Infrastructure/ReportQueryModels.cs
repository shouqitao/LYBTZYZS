namespace LYBT.Module.Reports.Infrastructure;

/// <summary>
/// 按日聚合的数值点（趋势查询仓库返回形状）。
/// </summary>
public sealed record ReportDayValueDto(DateTime Date, decimal Value);

/// <summary>
/// 按日聚合的计数点（问诊趋势仓库返回形状）。
/// </summary>
public sealed record ReportDayCountDto(DateTime Date, int Count);

/// <summary>
/// 医生绩效聚合点（仓库返回形状，平均处方金额由服务层计算）。
/// </summary>
public sealed record DoctorPerformancePointDto(
    string DoctorName,
    int ConsultationCount,
    decimal RegistrationFeeTotal,
    decimal MedicineFeeTotal,
    int PrescriptionCount);

/// <summary>
/// 按日患者流量点（仓库返回形状，新老患者由服务层按粒度汇总）。
/// </summary>
public sealed record PatientFlowPointDto(DateTime Date, int NewPatients, int ReturningPatients);
