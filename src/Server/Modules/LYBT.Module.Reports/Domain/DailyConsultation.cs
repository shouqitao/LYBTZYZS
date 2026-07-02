namespace LYBT.Module.Reports.Domain;

/// <summary>
/// 每日问诊汇总值对象。
/// </summary>
public record DailyConsultation(int TotalCount, IReadOnlyList<DoctorCount> ByDoctor);

/// <summary>
/// 医生问诊次数统计。
/// </summary>
public record DoctorCount(string DoctorName, int Count);


