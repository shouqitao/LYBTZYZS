using System.ComponentModel;

namespace LYBT.Shared.Models.Contracts.Formula
{
    /// <summary>
    /// 验方中药材导出DTO
    /// </summary>
    public class FormulaHerbExportItemDto
    {

        [DisplayName("中药材ID")]
        public Guid HerbId { get; set; }

        [DisplayName("中药材名称")]
        public string HerbName { get; set; } = string.Empty;

        [DisplayName("用量")]
        public int Dosage { get; set; }

        [DisplayName("单位")]
        public string Unit { get; set; } = string.Empty;

        [DisplayName("炮制方法")]
        public string? Preparation { get; set; }

        [DisplayName("用法")]
        public string? Usage { get; set; }

        [DisplayName("单价")]
        public decimal Price { get; set; }

        [DisplayName("小计")]
        public decimal Subtotal { get; set; }

        [DisplayName("排序")]
        public int SortOrder { get; set; }
    }
}
