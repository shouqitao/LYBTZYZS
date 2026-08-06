namespace LYBT.Shared.Models.Contracts.Reports;

/// <summary>
/// 问诊趋势 DTO - 折线图数据。
/// </summary>
public class ConsultationTrendDto
{
    /// <summary>时间桶标签（如 "08-01"，按月为 "2026-08"）</summary>
    public List<string> Labels { get; set; } = [];

    /// <summary>各桶问诊数</summary>
    public List<int> Counts { get; set; } = [];
}
