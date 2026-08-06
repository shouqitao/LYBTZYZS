namespace LYBT.Shared.Models.Contracts.Reports;

/// <summary>
/// 患者流量 DTO - 新老患者来源分析。
/// </summary>
public class PatientFlowDto
{
    /// <summary>时间桶标签（如 "08-01"，按月为 "2026-08"）</summary>
    public List<string> Labels { get; set; } = [];

    /// <summary>各桶新患者数（首次就诊发生在该桶）</summary>
    public List<int> NewPatients { get; set; } = [];

    /// <summary>各桶回头患者数（之前已有就诊记录）</summary>
    public List<int> ReturningPatients { get; set; } = [];
}
