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
    /// 药材兼容性警告
    /// </summary>
    public class HerbCompatibilityWarning
    {
        public string HerbName1 { get; set; } = string.Empty;
        public string HerbName2 { get; set; } = string.Empty;
        public string WarningLevel { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }

    /// <summary>
    /// 验方推荐 - 继承基础DTO
    /// </summary>
    public class FormulaRecommendation : BaseDto
    {

        [DisplayName("验方名称")]
        public string Name { get; set; } = string.Empty;

        [DisplayName("推荐理由")]
        public string Reason { get; set; } = string.Empty;

        [DisplayName("匹配得分")]
        public decimal MatchScore { get; set; }
    }

    /// <summary>
    /// 验方分析结果
    /// </summary>
    public class FormulaAnalysisResult
    {
        public string Summary { get; set; } = string.Empty;
        public List<string> Effects { get; set; } = new();
        public List<string> Contraindications { get; set; } = new();
        public List<HerbCompatibilityWarning> Warnings { get; set; } = new();
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
    /// 验方搜索DTO
    /// </summary>
    public class FormulaSearchDto : PagedQueryBaseDto
    {

        [DisplayName("验方名称")]
        public string? Name { get; set; }

        [DisplayName("功效关键词")]
        public string? Effect { get; set; }

        [DisplayName("验方类型")]
        public string? Type { get; set; }

        [DisplayName("包含药材")]
        public List<Guid>? HerbIds { get; set; }

        [DisplayName("症状关键词")]
        public string? Symptoms { get; set; }

        [DisplayName("创建者")]
        public Guid? CreatedBy { get; set; }
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
        public string? RecommendationLevel { get; set; }
    }
}
