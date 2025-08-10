using LYBT.Shared.Models.Contracts.Common;
using System.ComponentModel;

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
}