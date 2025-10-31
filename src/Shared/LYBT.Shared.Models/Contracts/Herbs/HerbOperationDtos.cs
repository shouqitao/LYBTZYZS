using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Enums;

namespace LYBT.Shared.Models.Contracts.Herbs
{

    /// <summary>
    /// 方剂药材成分DTO - 前后端共享API契约
    /// 用于在药方中表示单味药材的用量和计价信息
    /// </summary>
    public class FormulaIngredientDto
    {

        /// <summary>药材ID</summary>
        [Required(ErrorMessage = "药材ID不能为空")]
        [DisplayName("药材ID")]
        public Guid HerbId { get; set; }

        /// <summary>药材名称</summary>
        [DisplayName("药材名称")]
        public string Name { get; set; } = string.Empty;

        /// <summary>单位</summary>
        [DisplayName("单位")]
        public string? Unit { get; set; }

        /// <summary>单价</summary>
        [DisplayName("单价")]
        public decimal Price { get; set; }

        /// <summary>数量</summary>
        [Required(ErrorMessage = "数量不能为空")]
        [Range(0.1, 999999, ErrorMessage = "数量必须大于0")]
        [DisplayName("数量")]
        public decimal Quantity { get; set; }

        /// <summary>小计（由Service层计算）</summary>
        [DisplayName("小计")]
        public decimal TotalPrice { get; set; }

        /// <summary>备注</summary>
        [StringLength(200, ErrorMessage = "备注长度不能超过200个字符")]
        [DisplayName("备注")]
        public string? Remark { get; set; }
    }

    /// <summary>
    /// 中药材导入DTO - 前后端共享API契约
    /// 用于批量导入中药材档案的请求模型
    /// </summary>
    public class HerbImportDto
    {

        /// <summary>药材名称</summary>
        [Required(ErrorMessage = "药材名称不能为空")]
        [StringLength(100, ErrorMessage = "药材名称长度不能超过100个字符")]
        [DisplayName("药材名称")]
        public string Name { get; set; } = string.Empty;

        /// <summary>产地</summary>
        [StringLength(100, ErrorMessage = "产地长度不能超过100个字符")]
        [DisplayName("产地")]
        public string? Origin { get; set; }

        /// <summary>规格</summary>
        [StringLength(50, ErrorMessage = "规格长度不能超过50个字符")]
        [DisplayName("规格")]
        public string? Spec { get; set; }

        /// <summary>单位</summary>
        [StringLength(20, ErrorMessage = "单位长度不能超过20个字符")]
        [DisplayName("单位")]
        public string? Unit { get; set; }

        /// <summary>单价</summary>
        [Range(0, 999999.99, ErrorMessage = "单价必须在0-999999.99之间")]
        [DisplayName("单价")]
        public decimal Price { get; set; }

        /// <summary>批号</summary>
        [StringLength(50, ErrorMessage = "批号长度不能超过50个字符")]
        [DisplayName("批号")]
        public string? BatchNo { get; set; }

        /// <summary>有效期</summary>
        [DisplayName("有效期")]
        public DateTime? ExpireDate { get; set; }

        /// <summary>功效说明</summary>
        [StringLength(1000, ErrorMessage = "功效说明长度不能超过1000个字符")]
        [DisplayName("功效说明")]
        public string? Effect { get; set; }

        /// <summary>备注</summary>
        [StringLength(500, ErrorMessage = "备注长度不能超过500个字符")]
        [DisplayName("备注")]
        public string? Remark { get; set; }
    }

    /// <summary>
    /// 中药材状态更新DTO - 前后端共享API契约
    /// 用于更新中药材状态的请求模型
    /// </summary>
    public class CommonStatusUpdateDto
    {

        /// <summary>药材ID</summary>
        [Required(ErrorMessage = "药材ID不能为空")]
        [DisplayName("药材ID")]
        public Guid Id { get; set; }

        /// <summary>药材状态</summary>
        [DisplayName("药材状态")]
        public CommonStatus Status { get; set; }

        /// <summary>是否启用</summary>
        [DisplayName("是否启用")]
        public bool IsEnabled { get; set; }

        /// <summary>更新原因</summary>
        [StringLength(500, ErrorMessage = "更新原因长度不能超过500个字符")]
        [DisplayName("更新原因")]
        public string? Reason { get; set; }

        /// <summary>更新备注</summary>
        [StringLength(500, ErrorMessage = "更新备注长度不能超过500个字符")]
        [DisplayName("更新备注")]
        public string? Remark { get; set; }
    }

    /// <summary>
    /// 药材价格更新DTO
    /// </summary>
    public class HerbPriceUpdateDto
    {

        [Required]
        public Guid Id { get; set; }

        /// <summary>
        /// 成本价（元/单位）
        /// </summary>
        [Range(0, double.MaxValue, ErrorMessage = "成本价必须大于等于0")]
        public decimal? CostPrice { get; set; }

        /// <summary>
        /// 零售价（元/单位）
        /// </summary>
        [Range(0, double.MaxValue, ErrorMessage = "零售价必须大于等于0")]
        public decimal? Price { get; set; }

        /// <summary>
        /// 会员价（元/单位）
        /// </summary>
        [Range(0, double.MaxValue, ErrorMessage = "会员价必须大于等于0")]
        public decimal? MemberPrice { get; set; }

        /// <summary>
        /// 更新原因
        /// </summary>
        public string? Reason { get; set; }
    }

    /// <summary>
    /// 特价设置DTO
    /// </summary>
    public class HerbSpecialPriceDto
    {

        [Required]
        public Guid Id { get; set; }

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "特价必须大于0")]
        public decimal SpecialPrice { get; set; }

        [Required]
        public DateTime StartTime { get; set; }

        [Required]
        public DateTime EndTime { get; set; }

        /// <summary>
        /// 促销说明
        /// </summary>
        public string? Description { get; set; }
    }

    /// <summary>
    /// 特价设置请求
    /// </summary>
    public class SpecialPriceRequest
    {

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "特价必须大于0")]
        public decimal SpecialPrice { get; set; }

        [Required]
        public DateTime StartTime { get; set; }

        [Required]
        public DateTime EndTime { get; set; }

        public string? Description { get; set; }


    }

    /// <summary>
    /// 药材过期预警DTO
    /// </summary>
    public class HerbExpiryWarningDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? BatchNumber { get; set; }
        public string Unit { get; set; } = "克";
        public DateTime? ExpiryDate { get; set; }

        /// <summary>剩余天数（由Service层计算）</summary>
        public int DaysRemaining { get; set; }

        /// <summary>是否已过期（由Service层计算）</summary>
        public bool IsExpired { get; set; }

        /// <summary>预警级别（由Service层计算）</summary>
        public string WarningLevel { get; set; } = "Normal";
    }

    /// <summary>
    /// 药材价格更新结果DTO
    /// </summary>
    public class HerbPriceUpdateResultDto
    {
        public Guid HerbId { get; set; }
        public string HerbName { get; set; } = string.Empty;
        public decimal OldPrice { get; set; }
        public decimal NewPrice { get; set; }
        public bool IsSuccess { get; set; }
        public string? Message { get; set; }
        public DateTime UpdateTime { get; set; }
        public string? UpdatedBy { get; set; }
    }

    /// <summary>
    /// 配伍禁忌检查结果
    /// </summary>
    public class CompatibilityCheckResult
    {

        /// <summary>是否安全（无配伍禁忌）</summary>
        public bool IsSafe { get; set; }

        /// <summary>配伍冲突列表</summary>
        public List<CompatibilityConflict> Conflicts { get; set; } = new List<CompatibilityConflict>();

        /// <summary>警告信息</summary>
        public List<string> Warnings { get; set; } = new List<string>();

        /// <summary>建议信息</summary>
        public List<string> Suggestions { get; set; } = new List<string>();
    }

    /// <summary>
    /// 配伍冲突信息
    /// </summary>
    public class CompatibilityConflict
    {
        public Guid Herb1Id { get; set; }
        public string Herb1Name { get; set; } = string.Empty;
        public Guid Herb2Id { get; set; }
        public string Herb2Name { get; set; } = string.Empty;
        public string ConflictType { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Severity { get; set; } = "Medium"; // Low, Medium, High, Critical
    }

    /// <summary>
    /// 处方验证结果
    /// </summary>
    public class PrescriptionValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
        public List<string> Warnings { get; set; } = new List<string>();
        public CompatibilityCheckResult? CompatibilityResult { get; set; }
        public decimal TotalPrice { get; set; }
    }

    /// <summary>
    /// 药材使用注意事项DTO
    /// </summary>
    public class HerbUsagePrecautionDto
    {
        public Guid HerbId { get; set; }
        public string HerbName { get; set; } = string.Empty;
        public List<string> Precautions { get; set; } = new List<string>();
        public List<string> Contraindications { get; set; } = new List<string>();
        public List<string> SideEffects { get; set; } = new List<string>();
        public string? MaxDailyDosage { get; set; }
        public string? PregnancyCategory { get; set; }
    }

    /// <summary>
    /// 配伍建议DTO
    /// </summary>
    public class CompatibilitySuggestionDto
    {
        public Guid HerbId { get; set; }
        public string HerbName { get; set; } = string.Empty;
        public string SuggestionType { get; set; } = string.Empty; // Enhance, Reduce, Replace
        public string Reason { get; set; } = string.Empty;
        public decimal RecommendedDosage { get; set; }
        public string? Usage { get; set; }
    }

    /// <summary>
    /// 药材导入结果DTO - 继承自通用导入结果基类
    /// </summary>
    public class HerbImportResultDto : ImportResultDto
    {
        /// <summary>导入者</summary>
        [DisplayName("导入者")]
        public string ImportedBy { get; set; } = string.Empty;

        /// <summary>警告信息</summary>
        [DisplayName("警告信息")]
        public List<string> Warnings { get; set; } = new();
    }

    /// <summary>
    /// 药材导出设置DTO
    /// </summary>
    public class HerbExportDto
    {
        public List<Guid> HerbIds { get; set; } = new List<Guid>();
        public string ExportFormat { get; set; } = "Excel"; // Excel, CSV, PDF
        public bool IncludePriceInfo { get; set; } = true;
        public string? FileName { get; set; }
    }

    /// <summary>
    /// 药材导入验证结果DTO - 继承自通用验证结果基类
    /// </summary>
    public class HerbImportValidationDto : ValidationResultDto
    {
        /// <summary>有效行数</summary>
        [DisplayName("有效行数")]
        public int ValidRowCount { get; set; }

        /// <summary>无效行数</summary>
        [DisplayName("无效行数")]
        public int InvalidRowCount { get; set; }
    }

}
