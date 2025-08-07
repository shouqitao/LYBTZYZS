using System;
using System.Collections.Generic;

namespace LYBT.Shared.Models.Contracts.Formula
{
    /// <summary>
    /// 从模板创建验方DTO
    /// </summary>
    public class FormulaFromTemplateDto
    {
        public Guid TemplateId { get; set; }
        public Guid PatientId { get; set; }
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
    /// 验方推荐
    /// </summary>
    public class FormulaRecommendation
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
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
    /// 验方历史记录DTO
    /// </summary>
    public class FormulaHistoryDto
    {
        public Guid Id { get; set; }
        public Guid FormulaId { get; set; }
        public string FormulaName { get; set; } = string.Empty;
        public DateTime PrescribedDate { get; set; }
        public string? Effectiveness { get; set; }
        public string? Notes { get; set; }
    }
}