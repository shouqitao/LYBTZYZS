namespace LYBT.Shared.Models.Contracts.Reports;

/// <summary>
/// 收入趋势 DTO - 折线图数据（挂号费/药费/合计）。
/// </summary>
public class IncomeTrendDto
{
    /// <summary>时间桶标签（如 "08-01"，按月为 "2026-08"）</summary>
    public List<string> Labels { get; set; } = [];

    /// <summary>各桶挂号费收入</summary>
    public List<decimal> Registration { get; set; } = [];

    /// <summary>各桶药费收入</summary>
    public List<decimal> Medicine { get; set; } = [];

    /// <summary>各桶总收入</summary>
    public List<decimal> Total { get; set; } = [];
}
