namespace LYBT.Module.Reports.Domain;

/// <summary>
/// 每日收入汇总值对象。
/// </summary>
public record DailyIncome(decimal TotalIncome, decimal RegistrationFeeTotal, decimal MedicineFeeTotal);


