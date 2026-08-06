namespace LYBT.Shared.Models.Contracts.Reports;

/// <summary>
/// 医生绩效 DTO - 医生工作量统计。
/// </summary>
public class DoctorPerformanceDto
{
    /// <summary>医生姓名</summary>
    public string DoctorName { get; set; } = string.Empty;

    /// <summary>问诊次数（完成的医案数）</summary>
    public int ConsultationCount { get; set; }

    /// <summary>挂号费收入合计</summary>
    public decimal RegistrationFeeTotal { get; set; }

    /// <summary>药费收入合计</summary>
    public decimal MedicineFeeTotal { get; set; }

    /// <summary>平均处方金额（药费合计 / 处方数）</summary>
    public decimal AveragePrescriptionPrice { get; set; }
}
