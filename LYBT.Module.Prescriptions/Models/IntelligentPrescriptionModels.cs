using LYBT.Module.FormulaTemplates.Models.Dtos;
using LYBT.Module.Prescriptions.Models;

namespace LYBT.Module.Prescriptions.Models {

    /// <summary>
    /// 处方组合结果
    /// </summary>
    public class PrescriptionCompositionResult {
        /// <summary>
        /// 组合后的药材列表
        /// </summary>
        public List<PrescriptionItemModel> Items { get; set; } = new();

        /// <summary>
        /// 验方来源名称
        /// </summary>
        public string FormulaTemplateNames { get; set; } = string.Empty;

        /// <summary>
        /// 重复药材警告信息
        /// </summary>
        public string DuplicateHerbWarning { get; set; } = string.Empty;

        /// <summary>
        /// 药材供应状态
        /// </summary>
        public DrugAvailabilityStatus DrugAvailability { get; set; }

        /// <summary>
        /// 缺失的药材
        /// </summary>
        public string MissingHerbs { get; set; } = string.Empty;

        /// <summary>
        /// 单帖价格
        /// </summary>
        public decimal SingleDosePrice { get; set; }

        /// <summary>
        /// 总价格
        /// </summary>
        public decimal TotalPrice { get; set; }

        /// <summary>
        /// 总重量
        /// </summary>
        public decimal TotalWeight { get; set; }

        /// <summary>
        /// 帖数
        /// </summary>
        public int DosageCount { get; set; }
    }

    /// <summary>
    /// 重复药材检查结果
    /// </summary>
    public class PrescriptionDuplicateCheckResult {
        /// <summary>
        /// 是否有重复药材
        /// </summary>
        public bool HasDuplicates { get; set; }

        /// <summary>
        /// 重复的药材名称列表
        /// </summary>
        public List<string> DuplicateHerbs { get; set; } = new();

        /// <summary>
        /// 警告信息列表
        /// </summary>
        public List<string> Warnings { get; set; } = new();

        /// <summary>
        /// 综合警告消息
        /// </summary>
        public string WarningMessage { get; set; } = string.Empty;
    }

    /// <summary>
    /// 药材可用性检查结果
    /// </summary>
    public class HerbAvailabilityCheckResult {
        /// <summary>
        /// 供应状态
        /// </summary>
        public DrugAvailabilityStatus Status { get; set; } = DrugAvailabilityStatus.FullyAvailable;

        /// <summary>
        /// 缺失的药材名称列表
        /// </summary>
        public List<string> MissingHerbs { get; set; } = new();
    }

    /// <summary>
    /// 处方价格计算结果
    /// </summary>
    public class PrescriptionPriceCalculationResult {
        /// <summary>
        /// 单帖价格
        /// </summary>
        public decimal SingleDosePrice { get; set; }

        /// <summary>
        /// 总价格
        /// </summary>
        public decimal TotalPrice { get; set; }

        /// <summary>
        /// 总重量
        /// </summary>
        public decimal TotalWeight { get; set; }

        /// <summary>
        /// 帖数
        /// </summary>
        public int DosageCount { get; set; }
    }

    /// <summary>
    /// 处方建议结果
    /// </summary>
    public class PrescriptionSuggestionResult {
        /// <summary>
        /// 推荐的验方模板
        /// </summary>
        public List<FormulaTemplateDetailDto> SuggestedFormulas { get; set; } = new();

        /// <summary>
        /// 建议的医嘱
        /// </summary>
        public List<string> SuggestedAdvice { get; set; } = new();

        /// <summary>
        /// 注意事项
        /// </summary>
        public List<string> Precautions { get; set; } = new();
    }
}