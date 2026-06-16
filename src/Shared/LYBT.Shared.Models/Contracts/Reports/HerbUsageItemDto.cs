namespace LYBT.Shared.Models.Contracts.Reports;

public class HerbUsageItemDto
{
    public string HerbName { get; set; } = string.Empty;
    public int UsageCount { get; set; }
    public decimal TotalDosage { get; set; }
}
