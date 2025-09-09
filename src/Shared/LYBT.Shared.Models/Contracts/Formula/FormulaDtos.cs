using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.Shared.Models.Enums;

namespace LYBT.Shared.Models.Contracts.Formula
{

    /// <summary>
    /// 验方信息DTO - UltraThink v2.0简化版
    /// 与Formula实体对齐，删除时间和创建者字段
    /// </summary>
    public class FormulaDto : StatusDto, IRemarkable
    {

        [DisplayName("验方名称")]
        public string Name { get; set; } = string.Empty;

        [DisplayName("功效")]
        public string? Effect { get; set; }

        [DisplayName("用法")]
        public string? Usage { get; set; }

        [DisplayName("性味归经")]
        public string? Property { get; set; }

        [DisplayName("是否共享")]
        public bool IsShared { get; set; } = false;

        /// <inheritdoc/>
        [DisplayName("备注")]
        [StringLength(500, ErrorMessage = "备注长度不能超过500个字符")]
        public string? Remark { get; set; }

        [DisplayName("药材组成")]
        public List<FormulaHerbItemDto> Herbs { get; set; } = new();

        /// <summary>药材组成别名（兼容性）</summary>
        [DisplayName("药材组成")]
        public List<FormulaHerbItemDto> Items
        {
            get => Herbs;
            set => Herbs = value;
        }

        /// <summary>验方描述</summary>
        [DisplayName("验方描述")]
        public string? Description { get; set; }

        /// <summary>配制难度</summary>
        [DisplayName("配制难度")]
        public string? Difficulty { get; set; }

        /// <summary>药材数量（计算属性）</summary>
        [DisplayName("药材数量")]
        public int HerbCount => Herbs?.Count ?? 0;

        /// <summary>总价格（计算属性）</summary>
        [DisplayName("总价格")]
        public decimal TotalPrice
        {
            get
            {
                if (Herbs == null || !Herbs.Any())
                {
                    return 0m;
                }

                return Herbs.Sum(h => (h.Herb?.Price ?? 0m) * h.Quantity);
            }
        }

        /// <summary>药材名称列表</summary>
        public string HerbNames
        {
            get
            {
                if (Herbs == null || !Herbs.Any())
                {
                    return "暂无药材";
                }

                var herbNames = Herbs
                    .Where(h => h.Herb != null)
                    .Select(h => $"{h.Herb!.Name}({h.Quantity}g)")
                    .ToList();
                return herbNames.Any() ? string.Join("、", herbNames) : "暂无药材";
            }
        }

        /// <summary>获取药材名称列表（带限制）</summary>
        public string GetHerbNamesList(int maxCount = 10)
        {
            if (Herbs == null || !Herbs.Any())
            {
                return "暂无药材";
            }

            var herbNames = Herbs
                .Take(maxCount)
                .Where(h => h.Herb != null)
                .Select(h => $"{h.Herb!.Name}({h.Quantity}g)")
                .ToList();
            return herbNames.Any() ? string.Join("、", herbNames) : "暂无药材";
        }

        /// <summary>分类</summary>
        public string Category
        {
            get
            {
                // 根据验方名称智能判断分类
                if (Name?.Contains("感冒") == true)
                {
                    return "内科方";
                }

                if (Name?.Contains("外伤") == true)
                {
                    return "外科方";
                }

                if (Name?.Contains("妇科") == true)
                {
                    return "妇科方";
                }

                if (Name?.Contains("儿童") == true)
                {
                    return "儿科方";
                }

                return "验方"; // 默认分类
            }
        }

        /// <summary>适应症</summary>
        public string? Indications { get; set; }

        /// <summary>来源</summary>
        public string? Source { get; set; }

        /// <summary>用药指导</summary>
        public string? Instructions { get; set; }

        /// <summary>禁忌症</summary>
        public string? Contraindications { get; set; }

        /// <summary>制备方法</summary>
        public string? Preparation { get; set; }

        /// <summary>用药指导</summary>
        public string? DosageInstruction { get; set; }
    }

    /// <summary>
    /// 验方详情DTO
    /// </summary>
    public class FormulaDetailDto : FormulaDto
    {
        public new List<FormulaHerbItemDto> Herbs { get; set; } = new();
        public new string? Instructions { get; set; }
        public new string? Indications { get; set; }
        public new string? Contraindications { get; set; }
        public new string? Preparation { get; set; }
    }

    /// <summary>
    /// 验方中药材组成项DTO - 继承基础DTO提供ID
    /// </summary>
    public class FormulaHerbItemDto : BaseDto
    {

        [DisplayName("中药材ID")]
        public Guid HerbId { get; set; }

        [DisplayName("中药材名称")]
        public string HerbName { get; set; } = string.Empty;

        [DisplayName("用量")]
        public decimal Quantity { get; set; }

        [DisplayName("单位")]
        public string Unit { get; set; } = string.Empty;

        [DisplayName("炮制方法")]
        public string? Preparation { get; set; }

        [DisplayName("用法")]
        public string? Usage { get; set; }

        [DisplayName("价格")]
        public decimal Price { get; set; }

        [DisplayName("单价")]
        public decimal UnitPrice => Price;

        [DisplayName("加工方法")]
        public string? ProcessingMethod { get; set; }

        [DisplayName("特殊说明")]
        public string? SpecialInstructions { get; set; }

        [DisplayName("排序")]
        public int SortOrder { get; set; }

        // UltraThink导航属性 - 确保架构统一

        /// <summary>中药材导航属性</summary>
        [DisplayName("中药材")]
        public HerbDto? Herb { get; set; }
    }

    /// <summary>
    /// 验方输入基础DTO - 提供验方基本信息的验证规则
    /// </summary>
    public abstract class FormulaInputBaseDto : IRemarkable
    {

        [Required(ErrorMessage = "验方名称不能为空")]
        [StringLength(100, ErrorMessage = "验方名称不能超过100个字符")]
        [DisplayName("验方名称")]
        public string Name { get; set; } = string.Empty;

        [StringLength(200, ErrorMessage = "功效描述不能超过200个字符")]
        [DisplayName("功效")]
        public string Effect { get; set; } = string.Empty;

        [StringLength(200, ErrorMessage = "用法描述不能超过200个字符")]
        [DisplayName("用法")]
        public string Usage { get; set; } = string.Empty;

        [DisplayName("是否共享")]
        public bool IsShared { get; set; } = false;

        [StringLength(500, ErrorMessage = "用药指导不能超过500个字符")]
        [DisplayName("用药指导")]
        public string? Instructions { get; set; }

        [StringLength(500, ErrorMessage = "主治症状不能超过500个字符")]
        [DisplayName("主治症状")]
        public string? Indications { get; set; }

        [StringLength(500, ErrorMessage = "禁忌症不能超过500个字符")]
        [DisplayName("禁忌症")]
        public string? Contraindications { get; set; }

        [StringLength(200, ErrorMessage = "制备方法不能超过200个字符")]
        [DisplayName("制备方法")]
        public string? Preparation { get; set; }

        /// <inheritdoc/>
        [StringLength(500, ErrorMessage = "备注不能超过500个字符")]
        [DisplayName("备注")]
        public string? Remark { get; set; }
    }

    /// <summary>
    /// 创建验方DTO - 继承验方输入基础DTO
    /// </summary>
    public class FormulaCreateDto : FormulaInputBaseDto
    {

        [Required(ErrorMessage = "必须包含至少一味中药材")]
        [DisplayName("中药材组成")]
        public List<FormulaHerbItemCreateDto> Herbs { get; set; } = new();
    }

    /// <summary>
    /// 创建验方药材组成项DTO
    /// </summary>
    public class FormulaHerbItemCreateDto
    {

        [Required]
        public Guid HerbId { get; set; }

        [Required]
        [Range(0.1, 1000)]
        public decimal Quantity { get; set; }

        [StringLength(50)]
        public string? Preparation { get; set; }

        [StringLength(100)]
        public string? Usage { get; set; }

        public int SortOrder { get; set; } = 0;
    }

    /// <summary>
    /// 更新验方DTO - 继承验方输入基础DTO并添加ID字段
    /// </summary>
    public class FormulaUpdateDto : FormulaInputBaseDto, IIdentifiable<Guid>
    {

        /// <inheritdoc/>
        [Required(ErrorMessage = "验方ID不能为空")]
        [DisplayName("验方ID")]
        public Guid Id { get; set; }

        [Required(ErrorMessage = "必须包含至少一味中药材")]
        [DisplayName("中药材组成")]
        public List<FormulaHerbItemUpdateDto> Herbs { get; set; } = new();
    }

    /// <summary>
    /// 更新验方药材组成项DTO
    /// </summary>
    public class FormulaHerbItemUpdateDto
    {
        public Guid? Id { get; set; }

        [Required]
        public Guid HerbId { get; set; }

        [Required]
        [Range(0.1, 1000)]
        public decimal Quantity { get; set; }

        [StringLength(50)]
        public string? Preparation { get; set; }

        [StringLength(100)]
        public string? Usage { get; set; }

        public int SortOrder { get; set; } = 0;
    }

    /// <summary>
    /// 验方分页查询DTO - 别名支持
    /// 为向后兼容而创建的别名，实际使用FormulaQueryDto
    /// </summary>
    public class FormulaPagedQueryDto : FormulaQueryDto
    {
        // 继承所有FormulaQueryDto功能，提供别名支持
    }

    /// <summary>
    /// 验方查询DTO - 继承完整分页查询DTO，提供分页、时间范围、关键词搜索功能
    /// </summary>
    public class FormulaQueryDto : ExtendedQueryDto
    {

        [DisplayName("验方名称")]
        public string? Name { get; set; }

        [DisplayName("功效")]
        public string? Effect { get; set; }

        [DisplayName("是否共享")]
        public bool? IsShared { get; set; }

        [DisplayName("创建者ID")]
        public Guid? CreatedById { get; set; }

        [DisplayName("排序字段")]
        public string OrderBy { get; set; } = "CreateTime";

        [DisplayName("升序排序")]
        public bool IsAscending { get; set; } = false;

        // UltraThink兼容性别名 - 确保架构统一

        /// <summary>页码兼容性别名</summary>
        public int Page { get => PageIndex; set => PageIndex = value; }

        /// <summary>页大小兼容性别名</summary>
        public int Size { get => PageSize; set => PageSize = value; }
    }

    /// <summary>
    /// 从处方创建验方DTO - 继承验方输入基础DTO
    /// </summary>
    public class CreateFormulaFromPrescriptionDto : FormulaInputBaseDto
    {

        [Required(ErrorMessage = "处方ID不能为空")]
        [DisplayName("处方ID")]
        public Guid PrescriptionId { get; set; }
    }

    /// <summary>
    /// 验方统计DTO - 继承统计DTO基础类
    /// </summary>
    public class FormulaStatisticsDto : StatisticsDto
    {

        [DisplayName("共享验方数量")]
        public int SharedCount { get; set; }

        [DisplayName("私有验方数量")]
        public int PrivateCount { get; set; }

        [DisplayName("已使用验方数量")]
        public int UsedCount { get; set; }

        [DisplayName("功效统计")]
        public Dictionary<string, int> EffectStats { get; set; } = new();

        [DisplayName("创建者统计")]
        public Dictionary<string, int> CreatorStats { get; set; } = new();

        [DisplayName("统计开始日期")]
        public DateTime StartDate { get; set; }

        [DisplayName("统计结束日期")]
        public DateTime EndDate { get; set; }
    }

    /// <summary>
    /// 验方推荐DTO - 继承基础DTO提供ID支持
    /// </summary>
    public class FormulaRecommendationDto : BaseDto
    {

        [DisplayName("验方名称")]
        public string FormulaName { get; set; } = string.Empty;

        [DisplayName("功效")]
        public string Effect { get; set; } = string.Empty;

        [DisplayName("匹配得分")]
        public double MatchScore { get; set; }

        [DisplayName("使用次数")]
        public int UsageCount { get; set; }

        [DisplayName("推荐理由")]
        public string MatchReason { get; set; } = string.Empty;
    }

    // UltraThink v2.0: 导入导出功能DTOs（应用户业务需求恢复）

    /// <summary>
    /// 验方导入DTO - 支持从老系统批量导入验方数据
    /// </summary>
    public class FormulaImportDto
    {

        [Required(ErrorMessage = "验方名称不能为空")]
        [StringLength(100, ErrorMessage = "验方名称不能超过100个字符")]
        [DisplayName("验方名称")]
        public string Name { get; set; } = string.Empty;

        [StringLength(200, ErrorMessage = "功效描述不能超过200个字符")]
        [DisplayName("功效")]
        public string? Effect { get; set; }

        [StringLength(200, ErrorMessage = "用法描述不能超过200个字符")]
        [DisplayName("用法")]
        public string? Usage { get; set; }

        [StringLength(200, ErrorMessage = "性味归经不能超过200个字符")]
        [DisplayName("性味归经")]
        public string? Property { get; set; }

        [DisplayName("是否共享")]
        public bool IsShared { get; set; } = false;

        [StringLength(500, ErrorMessage = "用药指导不能超过500个字符")]
        [DisplayName("用药指导")]
        public string? Instructions { get; set; }

        [StringLength(500, ErrorMessage = "主治症状不能超过500个字符")]
        [DisplayName("主治症状")]
        public string? Indications { get; set; }

        [StringLength(500, ErrorMessage = "禁忌症不能超过500个字符")]
        [DisplayName("禁忌症")]
        public string? Contraindications { get; set; }

        [StringLength(200, ErrorMessage = "制备方法不能超过200个字符")]
        [DisplayName("制备方法")]
        public string? Preparation { get; set; }

        [StringLength(500, ErrorMessage = "备注不能超过500个字符")]
        [DisplayName("备注")]
        public string? Remark { get; set; }

        [StringLength(200, ErrorMessage = "来源不能超过200个字符")]
        [DisplayName("来源")]
        public string? Source { get; set; }

        [Required(ErrorMessage = "必须包含至少一味中药材")]
        [DisplayName("中药材组成")]
        public List<FormulaHerbImportDto> Herbs { get; set; } = new();

        /// <summary>原系统ID（用于数据迁移）</summary>
        [DisplayName("原系统ID")]
        public string? OriginalId { get; set; }

        /// <summary>导入批次号</summary>
        [DisplayName("导入批次")]
        public string? ImportBatch { get; set; }
    }

    /// <summary>
    /// 验方中药材导入DTO
    /// </summary>
    public class FormulaHerbImportDto
    {

        [Required(ErrorMessage = "中药材名称不能为空")]
        [StringLength(100, ErrorMessage = "中药材名称不能超过100个字符")]
        [DisplayName("中药材名称")]
        public string HerbName { get; set; } = string.Empty;

        [Required(ErrorMessage = "用量必须大于0")]
        [Range(0.1, 1000, ErrorMessage = "用量必须在0.1-1000之间")]
        [DisplayName("用量")]
        public decimal Quantity { get; set; }

        [StringLength(10, ErrorMessage = "单位不能超过10个字符")]
        [DisplayName("单位")]
        public string Unit { get; set; } = "g";

        [StringLength(50, ErrorMessage = "炮制方法不能超过50个字符")]
        [DisplayName("炮制方法")]
        public string? Preparation { get; set; }

        [StringLength(100, ErrorMessage = "用法不能超过100个字符")]
        [DisplayName("用法")]
        public string? Usage { get; set; }

        [DisplayName("排序")]
        public int SortOrder { get; set; } = 0;

        /// <summary>原系统中药材ID（用于数据迁移）</summary>
        [DisplayName("原系统中药材ID")]
        public string? OriginalHerbId { get; set; }
    }

    /// <summary>
    /// 验方导出DTO - 支持验方数据导出
    /// </summary>
    public class FormulaExportDto
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
        public List<FormulaHerbExportDto> Herbs { get; set; } = new();

        [DisplayName("药材总数")]
        public int HerbCount { get; set; }

        [DisplayName("总价格")]
        public decimal TotalPrice { get; set; }

        [DisplayName("导出时间")]
        public DateTime ExportTime { get; set; } = DateTime.Now;
    }

    /// <summary>
    /// 验方中药材导出DTO
    /// </summary>
    public class FormulaHerbExportDto
    {

        [DisplayName("中药材ID")]
        public Guid HerbId { get; set; }

        [DisplayName("中药材名称")]
        public string HerbName { get; set; } = string.Empty;

        [DisplayName("用量")]
        public decimal Quantity { get; set; }

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

    /// <summary>
    /// 批量导入结果DTO
    /// </summary>
    public class FormulaImportResultDto
    {

        [DisplayName("导入批次号")]
        public string ImportBatch { get; set; } = string.Empty;

        [DisplayName("总数量")]
        public int TotalCount { get; set; }

        [DisplayName("成功数量")]
        public int SuccessCount { get; set; }

        [DisplayName("失败数量")]
        public int FailedCount { get; set; }

        [DisplayName("跳过数量")]
        public int SkippedCount { get; set; }

        [DisplayName("导入开始时间")]
        public DateTime StartTime { get; set; }

        [DisplayName("导入结束时间")]
        public DateTime EndTime { get; set; }

        [DisplayName("成功的验方列表")]
        public List<FormulaDto> SuccessfulFormulas { get; set; } = new();

        [DisplayName("失败的记录")]
        public List<FormulaImportErrorDto> FailedItems { get; set; } = new();

        /// <summary>导入是否成功</summary>
        public bool IsSuccess => FailedCount == 0;

        /// <summary>成功率</summary>
        public double SuccessRate => TotalCount > 0 ? (double)SuccessCount / TotalCount * 100 : 0;
    }

    /// <summary>
    /// 导入错误信息DTO
    /// </summary>
    public class FormulaImportErrorDto
    {

        [DisplayName("行号")]
        public int RowIndex { get; set; }

        [DisplayName("验方名称")]
        public string FormulaName { get; set; } = string.Empty;

        [DisplayName("错误原因")]
        public string ErrorMessage { get; set; } = string.Empty;

        [DisplayName("错误详情")]
        public string? ErrorDetails { get; set; }

        [DisplayName("原始数据")]
        public string? OriginalData { get; set; }
    }

    /// <summary>
    /// 验方导入选项DTO
    /// </summary>
    public class FormulaImportOptionsDto
    {

        [DisplayName("跳过重复验方")]
        public bool SkipDuplicates { get; set; } = true;

        [DisplayName("更新已存在验方")]
        public bool UpdateExisting { get; set; } = false;

        [DisplayName("自动匹配中药材")]
        public bool AutoMatchHerbs { get; set; } = true;

        [DisplayName("创建不存在的中药材")]
        public bool CreateMissingHerbs { get; set; } = false;

        [DisplayName("默认共享设置")]
        public bool DefaultIsShared { get; set; } = false;

        [DisplayName("导入批次号")]
        public string? ImportBatch { get; set; }

        [DisplayName("数据来源")]
        public string? DataSource { get; set; } = "老系统导入";
    }
}
