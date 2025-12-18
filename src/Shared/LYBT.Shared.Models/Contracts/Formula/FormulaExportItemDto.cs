using System.ComponentModel;
using LYBT.Shared.Models.Enums;

namespace LYBT.Shared.Models.Contracts.Formula
{
    /// <summary>
    /// 验方导出DTO - 支持验方数据导出
    /// </summary>
    public class FormulaExportItemDto
    {

        [DisplayName("验方ID")]
        public Guid Id { get; set; }

        [DisplayName("验方名称")]
        public string Name { get; set; } = string.Empty;

        [DisplayName("功效")]
        public string? Effect { get; set; }

        [DisplayName("用法")]
        public string? Usage { get; set; }

        [DisplayName("性味归经")]
        public string? Property { get; set; }

        [DisplayName("是否共享")]
        public bool IsShared { get; set; }

        [DisplayName("用药指导")]
        public string? Instructions { get; set; }

        [DisplayName("主治症状")]
        public string? Indications { get; set; }

        [DisplayName("禁忌症")]
        public string? Contraindications { get; set; }

        [DisplayName("制备方法")]
        public string? Preparation { get; set; }

        [DisplayName("备注")]
        public string? Remark { get; set; }

        [DisplayName("来源")]
        public string? Source { get; set; }

        [DisplayName("状态")]
        public CommonStatus Status { get; set; }

        [DisplayName("中药材组成")]
        public List<FormulaHerbExportItemDto> Herbs { get; set; } = new();

        [DisplayName("药材总数")]
        public int HerbCount { get; set; }

        [DisplayName("总价格")]
        public decimal TotalPrice { get; set; }

        [DisplayName("导出时间")]
        public DateTime ExportTime { get; set; } = DateTime.Now;
    }
}
