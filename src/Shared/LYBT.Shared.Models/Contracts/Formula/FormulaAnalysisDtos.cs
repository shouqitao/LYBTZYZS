using System.ComponentModel;
using LYBT.Shared.Models.Contracts.Common;

namespace LYBT.Shared.Models.Contracts.Formula
{

    /// <summary>
    /// 从模板创建验方DTO
    /// </summary>
    public class FormulaFromTemplateDto
    {

        [DisplayName("模板ID")]
        public Guid TemplateId { get; set; }

        [DisplayName("患者ID")]
        public Guid PatientId { get; set; }

        [DisplayName("调整说明")]
        public string? Adjustments { get; set; }
    }


    /// <summary>
    /// 验方历史记录DTO - 继承基础DTO
    /// </summary>
    public class FormulaHistoryDto : BaseDto
    {

        [DisplayName("验方ID")]
        public Guid FormulaId { get; set; }

        [DisplayName("验方名称")]
        public string FormulaName { get; set; } = string.Empty;

        [DisplayName("开具日期")]
        public DateTime PrescribedDate { get; set; }

        [DisplayName("疗效")]
        public string? Effectiveness { get; set; }

        [DisplayName("备注")]
        public string? Notes { get; set; }
    }

    /// <summary>
    /// 验方类型枚举DTO
    /// </summary>
    public class FormulaTypeDto
    {
        public string Type { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int Count { get; set; }
    }


    /// <summary>
    /// 验方复制结果DTO
    /// </summary>
    public class FormulaCopyResultDto
    {
        public Guid NewFormulaId { get; set; }
        public string NewFormulaName { get; set; } = string.Empty;
        public bool IsSuccess { get; set; }
        public string? Message { get; set; }
        public DateTime CopyTime { get; set; }
        public string? CopiedBy { get; set; }
    }

    /// <summary>
    /// 验方使用统计DTO
    /// </summary>
    public class FormulaUsageStatDto
    {
        public Guid FormulaId { get; set; }
        public string FormulaName { get; set; } = string.Empty;
        public int UsageCount { get; set; }
        public int PatientCount { get; set; } // 使用过的患者数
        public decimal SuccessRate { get; set; } // 成功率
        public DateTime LastUsedDate { get; set; }
        public List<string> CommonSymptoms { get; set; } = new List<string>();
    }

    /// <summary>
    /// 验方效果评估DTO
    /// </summary>
    public class FormulaEffectivenessDto
    {
        public Guid FormulaId { get; set; }
        public string FormulaName { get; set; } = string.Empty;
        public decimal OverallRating { get; set; } // 1-5分
        public int TotalReviews { get; set; }
        public List<string> PositiveEffects { get; set; } = new List<string>();
        public List<string> SideEffects { get; set; } = new List<string>();
        public string? EffectLevel { get; set; }
    }
}
