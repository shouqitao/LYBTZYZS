using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Herbs;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel;

namespace LYBT.Shared.Models.Contracts.Formula
{
    /// <summary>
    /// 验方基础DTO - UltraThink统一标准，包含完整业务属性
    /// </summary>
    public class FormulaDto : AuditableDto, IRemarkable
    {
        [DisplayName("验方名称")]
        public string Name { get; set; } = string.Empty;
        
        [DisplayName("功效")]
        public string Effect { get; set; } = string.Empty;
        
        [DisplayName("用法")]
        public string Usage { get; set; } = string.Empty;
        
        [DisplayName("是否共享")]
        public bool IsShared { get; set; }
        
        [DisplayName("创建者ID")]
        public Guid? CreatedById { get; set; }
        
        [DisplayName("创建者姓名")]
        public string? CreatedByName { get; set; }
        
        [DisplayName("药材数量")]
        public int HerbCount { get; set; }
        
        [DisplayName("备注")]
        [StringLength(500, ErrorMessage = "备注长度不能超过500个字符")]
        public string? Remark { get; set; }
        
        // UltraThink P0修复：添加Client层期望的缺失属性
        [DisplayName("分类")]
        public string? Category { get; set; }
        
        [DisplayName("主治症状")]
        public string? Indications { get; set; }
        
        [DisplayName("来源")]
        public string? Source { get; set; }
        
        [DisplayName("用药指导")]
        public string? Instructions { get; set; }
        
        [DisplayName("禁忌症")]
        public string? Contraindications { get; set; }
        
        [DisplayName("制备方法")]
        public string? Preparation { get; set; }
        
        // UltraThink P0修复：添加Client层期望的更多缺失属性
        [DisplayName("用药指导")]
        public string? DosageInstruction { get; set; }
        
        [DisplayName("药材组成")]
        public List<FormulaHerbItemDto> Herbs { get; set; } = new();
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
    /// 验方查询DTO - 继承完整分页查询DTO，提供分页、时间范围、关键词搜索功能
    /// </summary>
    public class FormulaQueryDto : FullPagedQueryDto
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
}