namespace LYBT.Shared.Models.Contracts.Herbs
{

    /// <summary>
    /// 药材导出设置DTO - 批量导出配置选项
    /// </summary>
    public class HerbExportItemDto
    {
        public List<Guid> HerbIds { get; set; } = new List<Guid>();
        public string ExportFormat { get; set; } = "Excel"; // Excel, CSV, PDF
        public bool IncludePriceInfo { get; set; } = true;
        public string? FileName { get; set; }
    }

}
