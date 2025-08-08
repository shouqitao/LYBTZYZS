using LYBT.Shared.Models.Enums;

namespace LYBT.Models.Prescriptions
{

    /// <summary>
    /// 处方组成结果
    /// </summary>
    public class PrescriptionCompositionResult
    {

        /// <summary>
        /// 是否成功
        /// </summary>
        public bool IsSuccess { get; set; }

        /// <summary>
        /// 消息
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// 组成数据
        /// </summary>
        public object? Data { get; set; }

        /// <summary>
        /// 处方项目列表
        /// </summary>
        public List<object> Items { get; set; } = new();

        /// <summary>
        /// 经验方模板名称列表
        /// </summary>
        public List<string> FormulaNames { get; set; } = new();

        /// <summary>
        /// 重复药材警告
        /// </summary>
        public string? DuplicateHerbWarning { get; set; }

        /// <summary>
        /// 药物可用性(简化为布尔值)
        /// </summary>
        public bool IsAvailable { get; set; } = true;

        /// <summary>
        /// 缺失药材列表
        /// </summary>
        public List<string> MissingHerbs { get; set; } = new();

        /// <summary>
        /// 单剂价格
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
        /// 剂数
        /// </summary>
        public int DosageCount { get; set; }
    }

    /// <summary>
    /// 处方重复检查结果
    /// </summary>
    public class PrescriptionDuplicateCheckResult
    {

        /// <summary>
        /// 是否有重复
        /// </summary>
        public bool HasDuplicate { get; set; }

        /// <summary>
        /// 是否有重复药材
        /// </summary>
        public bool HasDuplicates { get; set; }

        /// <summary>
        /// 重复项列表
        /// </summary>
        public List<string> DuplicateItems { get; set; } = new();

        /// <summary>
        /// 重复药材列表
        /// </summary>
        public List<string> DuplicateHerbs { get; set; } = new();

        /// <summary>
        /// 警告消息
        /// </summary>
        public string WarningMessage { get; set; } = string.Empty;

        /// <summary>
        /// 警告列表
        /// </summary>
        public List<string> Warnings { get; set; } = new();

        /// <summary>
        /// 检查消息
        /// </summary>
        public string Message { get; set; } = string.Empty;
    }

    /// <summary>
    /// 药材可用性检查结果
    /// </summary>
    public class HerbAvailabilityCheckResult
    {

        /// <summary>
        /// 是否可用
        /// </summary>
        public bool IsAvailable { get; set; }

        /// <summary>
        /// 可用性状态(简化为布尔值)
        /// </summary>
        public bool IsFullyAvailable { get; set; } = true;

        /// <summary>
        /// 不可用的药材列表
        /// </summary>
        public List<string> UnavailableHerbs { get; set; } = new();

        /// <summary>
        /// 缺失药材列表
        /// </summary>
        public List<string> MissingHerbs { get; set; } = new();

        /// <summary>
        /// 检查消息
        /// </summary>
        public string Message { get; set; } = string.Empty;
    }

    /// <summary>
    /// 处方价格计算结果
    /// </summary>
    public class PrescriptionPriceCalculationResult
    {

        /// <summary>
        /// 总价格
        /// </summary>
        public decimal TotalPrice { get; set; }

        /// <summary>
        /// 单剂价格
        /// </summary>
        public decimal SingleDosePrice { get; set; }

        /// <summary>
        /// 总重量
        /// </summary>
        public decimal TotalWeight { get; set; }

        /// <summary>
        /// 剂数
        /// </summary>
        public int DosageCount { get; set; }

        /// <summary>
        /// 明细列表
        /// </summary>
        public List<PriceDetailItem> Details { get; set; } = new();

        /// <summary>
        /// 计算消息
        /// </summary>
        public string Message { get; set; } = string.Empty;
    }

    /// <summary>
    /// 价格明细项
    /// </summary>
    public class PriceDetailItem
    {

        /// <summary>
        /// 项目名称
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 数量
        /// </summary>
        public decimal Quantity { get; set; }

        /// <summary>
        /// 单价
        /// </summary>
        public decimal UnitPrice { get; set; }

        /// <summary>
        /// 小计
        /// </summary>
        public decimal Subtotal { get; set; }
    }

    /// <summary>
    /// 处方建议结果
    /// </summary>
    public class PrescriptionSuggestionResult
    {

        /// <summary>
        /// 建议列表
        /// </summary>
        public List<PrescriptionSuggestion> Suggestions { get; set; } = new();

        /// <summary>
        /// 建议经验方
        /// </summary>
        public List<string> SuggestedFormulas { get; set; } = new();

        /// <summary>
        /// 建议提醒
        /// </summary>
        public List<string> SuggestedAdvice { get; set; } = new();

        /// <summary>
        /// 注意事项
        /// </summary>
        public List<string> Precautions { get; set; } = new();

        /// <summary>
        /// 建议消息
        /// </summary>
        public string Message { get; set; } = string.Empty;
    }

    /// <summary>
    /// 处方建议项
    /// </summary>
    public class PrescriptionSuggestion
    {

        /// <summary>
        /// 建议类型
        /// </summary>
        public string Type { get; set; } = string.Empty;

        /// <summary>
        /// 建议内容
        /// </summary>
        public string Content { get; set; } = string.Empty;

        /// <summary>
        /// 优先级
        /// </summary>
        public int Priority { get; set; }
    }
}