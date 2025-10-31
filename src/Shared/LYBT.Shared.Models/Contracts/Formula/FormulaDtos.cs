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

        [DisplayName("主治")]
        public string? Indications { get; set; }

        [DisplayName("功效")]
        public string? Effects { get => Effect; set => Effect = value; }

        [DisplayName("验方描述")]
        [StringLength(1000, ErrorMessage = "验方描述长度不能超过1000个字符")]
        public string? Description { get; set; }

        [DisplayName("用法")]
        public string? Usage { get; set; }

        [DisplayName("性味归经")]
        public string? Property { get; set; }

        [DisplayName("是否共享")]
        public bool IsShared { get; set; } = false;

        /// <summary>
        /// 验证状态 - 标识验方是否已验证（Draft=草稿/未验证，Validated=已验证）
        /// 从老系统导入的验方初始为Draft状态，经过医生审核后标记为Validated
        /// </summary>
        [DisplayName("验证状态")]
        public FormulaValidationStatus ValidationStatus { get; set; } = FormulaValidationStatus.Draft;

        [DisplayName("来源")]
        [StringLength(100, ErrorMessage = "来源长度不能超过100个字符")]
        public string? Source { get; set; }

        /// <inheritdoc/>
        [DisplayName("备注")]
        [StringLength(500, ErrorMessage = "备注长度不能超过500个字符")]
        public string? Remark { get; set; }

        [DisplayName("禁忌症")]
        [StringLength(500, ErrorMessage = "禁忌症长度不能超过500个字符")]
        public string? Contraindications { get; set; }

        /// <summary>备注别名（兼容性）</summary>
        [DisplayName("备注")]
        public string? Notes
        {
            get => Remark;
            set => Remark = value;
        }

        [DisplayName("药材组成")]
        public List<FormulaHerbItemDto> Herbs { get; set; } = new();

        /// <summary>药材组成别名（兼容性）</summary>
        [DisplayName("药材组成")]
        public List<FormulaHerbItemDto> Items
        {
            get => Herbs;
            set => Herbs = value;
        }

        /// <summary>药材数量（由Service层计算）</summary>
        [DisplayName("药材数量")]
        public int HerbCount { get; set; }

        /// <summary>总价格（由Service层计算）</summary>
        [DisplayName("总价格")]
        public decimal TotalPrice { get; set; }

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
    }

    /// <summary>
    /// 验方详情DTO
    /// </summary>
    public class FormulaDetailDto : FormulaDto
    {
        public new List<FormulaHerbItemDto> Herbs { get; set; } = new();
    }

    /// <summary>
    /// 验方中药材组成项DTO - 继承基础DTO提供ID
    /// 支持延迟绑定：允许先保存原始药材名称，稍后再绑定到药材库
    /// </summary>
    public class FormulaHerbItemDto : BaseDto
    {
        /// <summary>
        /// 药材ID（可空，支持延迟绑定）
        /// </summary>
        [DisplayName("中药材ID")]
        public Guid? HerbId { get; set; }

        /// <summary>
        /// 原始药材名称（从老系统导入时保存，用于延迟绑定）
        /// </summary>
        [DisplayName("原始药材名称")]
        public string? OriginalHerbName { get; set; }

        /// <summary>
        /// 是否已验证绑定（true表示HerbId已绑定到药材库）
        /// </summary>
        [DisplayName("已验证")]
        public bool IsValidated { get; set; }

        [DisplayName("中药材名称")]
        public string HerbName { get; set; } = string.Empty;

        [DisplayName("用量")]
        public decimal Quantity { get; set; }

        [DisplayName("单位")]
        public string Unit { get; set; } = string.Empty;

        [DisplayName("炮制方法")]
        public string? Preparation { get; set; }

        [DisplayName("加工方法")]
        public string? Processing { get => ProcessingMethod; set => ProcessingMethod = value; }

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
    /// 验方输入DTO - 统一创建和更新
    /// Phase 3: 合并FormulaCreateDto和FormulaUpdateDto
    /// </summary>
    public class FormulaInputDto : IRemarkable
    {

        [Required(ErrorMessage = "验方名称不能为空")]
        [StringLength(100, ErrorMessage = "验方名称不能超过100个字符")]
        [DisplayName("验方名称")]
        public string Name { get; set; } = string.Empty;

        [StringLength(200, ErrorMessage = "功效描述不能超过200个字符")]
        [DisplayName("功效")]
        public string Effect { get; set; } = string.Empty;
        [StringLength(1000, ErrorMessage = "验方描述不能超过1000个字符")]
        [DisplayName("验方描述")]
        public string? Description { get; set; }

        [StringLength(200, ErrorMessage = "用法描述不能超过200个字符")]
        [DisplayName("用法")]
        public string Usage { get; set; } = string.Empty;
        [StringLength(200, ErrorMessage = "性味归经不能超过200个字符")]
        [DisplayName("性味归经")]
        public string? Property { get; set; }

        [StringLength(100, ErrorMessage = "验方分类不能超过100个字符")]
        [DisplayName("验方分类")]
        public string? Category { get; set; }

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

        /// <summary>验方ID（更新时必填，创建时为null）</summary>
        [DisplayName("验方ID")]
        public Guid? Id { get; set; }

        /// <summary>状态</summary>
        [DisplayName("状态")]
        public CommonStatus Status { get; set; } = CommonStatus.Enabled;

        /// <summary>中药材组成</summary>
        [Required(ErrorMessage = "必须包含至少一味中药材")]
        [DisplayName("中药材组成")]
        public List<FormulaHerbItemInputDto> Herbs { get; set; } = new();
    }

    /// <summary>
    /// 验方药材组成项输入DTO - 统一创建和更新
    /// Phase 3: 合并FormulaHerbItemCreateDto和FormulaHerbItemUpdateDto
    /// 支持延迟绑定：HerbId可空
    /// </summary>
    public class FormulaHerbItemInputDto
    {
        /// <summary>项ID（更新时可填，创建时为null）</summary>
        public Guid? Id { get; set; }

        /// <summary>药材ID（可空，支持延迟绑定）</summary>
        public Guid? HerbId { get; set; }

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
    /// 验方查询DTO - 基础查询条件
    /// </summary>
    public class FormulaQueryDto : PagedQueryBaseDto
    {
        /// <summary>验方名称</summary>
        [DisplayName("验方名称")]
        public string? Name { get; set; }

        /// <summary>功效关键词</summary>
        [DisplayName("功效")]
        public string? Effect { get; set; }

        /// <summary>是否共享</summary>
        [DisplayName("是否共享")]
        public bool? IsShared { get; set; }

        /// <summary>关键词搜索</summary>
        [DisplayName("关键词")]
        public new string? Keyword { get; set; }

        /// <summary>状态（兼容旧代码）</summary>
        [DisplayName("状态")]
        public CommonStatus? Status { get; set; }
    }

    /// <summary>
    /// 验方搜索DTO - 高级搜索条件
    /// </summary>
    public class FormulaSearchDto : FormulaQueryDto
    {
        /// <summary>创建者ID</summary>
        [DisplayName("创建者ID")]
        public Guid? CreatedById { get; set; }

        /// <summary>主治症状</summary>
        [DisplayName("主治症状")]
        public string? Indications { get; set; }

        /// <summary>来源</summary>
        [DisplayName("来源")]
        public string? Source { get; set; }

        /// <summary>创建日期范围-开始日期</summary>
        [DisplayName("开始日期")]
        public DateTime? StartDate { get; set; }

        /// <summary>创建日期范围-结束日期</summary>
        [DisplayName("结束日期")]
        public DateTime? EndDate { get; set; }

        /// <summary>排序字段</summary>
        [DisplayName("排序字段")]
        public string OrderBy { get; set; } = "CreateTime";

        /// <summary>升序排序</summary>
        [DisplayName("升序排序")]
        public bool IsAscending { get; set; } = false;
    }


    /// <summary>
    /// 从处方创建验方DTO - 继承验方输入基础DTO
    /// </summary>
    public class CreateFormulaFromPrescriptionDto : FormulaInputDto
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
    /// 批量导入结果DTO - 继承自通用导入结果基类
    /// </summary>
    public class FormulaImportResultDto : ImportResultDto
    {
        /// <summary>导入批次号（兼容别名）</summary>
        [DisplayName("导入批次号")]
        public string ImportBatch => ImportBatchId;

        /// <summary>导入开始时间</summary>
        [DisplayName("导入开始时间")]
        public DateTime StartTime { get; set; }

        /// <summary>导入结束时间</summary>
        [DisplayName("导入结束时间")]
        public DateTime EndTime { get; set; }

        /// <summary>成功匹配的药材数量（自动匹配到药材库）</summary>
        [DisplayName("成功匹配药材数")]
        public int MatchedHerbsCount { get; set; }

        /// <summary>未匹配的药材数量（需要手动校验）</summary>
        [DisplayName("未匹配药材数")]
        public int UnmatchedHerbsCount { get; set; }

        /// <summary>成功的验方列表</summary>
        [DisplayName("成功的验方列表")]
        public List<FormulaDto> SuccessfulFormulas { get; set; } = new();

        /// <summary>失败的记录</summary>
        [DisplayName("失败的记录")]
        public List<FormulaImportErrorDto> FailedItems { get; set; } = new();
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
