using System.ComponentModel.DataAnnotations;

namespace LYBT.Shared.Models.Contracts.Formula
{
    /// <summary>
    /// 验方基础DTO
    /// </summary>
    public class FormulaDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Effect { get; set; } = string.Empty;
        public string Usage { get; set; } = string.Empty;
        public bool IsShared { get; set; }
        public Guid? CreatedById { get; set; }
        public string? CreatedByName { get; set; }
        public DateTime CreateTime { get; set; }
        public DateTime? UpdateTime { get; set; }
        public int HerbCount { get; set; }
        public string? Remark { get; set; }
    }

    /// <summary>
    /// 验方详情DTO
    /// </summary>
    public class FormulaDetailDto : FormulaDto
    {
        public List<FormulaHerbItemDto> Herbs { get; set; } = new();
        public string? Instructions { get; set; }
        public string? Indications { get; set; }
        public string? Contraindications { get; set; }
        public string? Preparation { get; set; }
    }

    /// <summary>
    /// 验方中药材组成项DTO
    /// </summary>
    public class FormulaHerbItemDto
    {
        public Guid Id { get; set; }
        public Guid HerbId { get; set; }
        public string HerbName { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public string Unit { get; set; } = string.Empty;
        public string? Preparation { get; set; }
        public string? Usage { get; set; }
        public decimal Price { get; set; }
        public int SortOrder { get; set; }
    }

    /// <summary>
    /// 创建验方DTO
    /// </summary>
    public class FormulaCreateDto
    {
        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [StringLength(200)]
        public string Effect { get; set; } = string.Empty;

        [StringLength(200)]
        public string Usage { get; set; } = string.Empty;

        public bool IsShared { get; set; } = false;

        [StringLength(500)]
        public string? Instructions { get; set; }

        [StringLength(500)]
        public string? Indications { get; set; }

        [StringLength(500)]
        public string? Contraindications { get; set; }

        [StringLength(200)]
        public string? Preparation { get; set; }

        [StringLength(200)]
        public string? Remark { get; set; }

        [Required]
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
    /// 更新验方DTO
    /// </summary>
    public class FormulaUpdateDto
    {
        [Required]
        public Guid Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [StringLength(200)]
        public string Effect { get; set; } = string.Empty;

        [StringLength(200)]
        public string Usage { get; set; } = string.Empty;

        public bool IsShared { get; set; }

        [StringLength(500)]
        public string? Instructions { get; set; }

        [StringLength(500)]
        public string? Indications { get; set; }

        [StringLength(500)]
        public string? Contraindications { get; set; }

        [StringLength(200)]
        public string? Preparation { get; set; }

        [StringLength(200)]
        public string? Remark { get; set; }

        [Required]
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
    /// 验方查询DTO
    /// </summary>
    public class FormulaQueryDto
    {
        public string? Name { get; set; }
        public string? Effect { get; set; }
        public bool? IsShared { get; set; }
        public Guid? CreatedById { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string? SearchKeyword { get; set; }
        public int CurrentPage { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public string OrderBy { get; set; } = "CreateTime";
        public bool IsAscending { get; set; } = false;
    }

    /// <summary>
    /// 从处方创建验方DTO
    /// </summary>
    public class CreateFormulaFromPrescriptionDto
    {
        [Required]
        public Guid PrescriptionId { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [StringLength(200)]
        public string Effect { get; set; } = string.Empty;

        [StringLength(200)]
        public string Usage { get; set; } = string.Empty;

        public bool IsShared { get; set; } = false;

        [StringLength(200)]
        public string? Remark { get; set; }
    }

    /// <summary>
    /// 验方统计DTO
    /// </summary>
    public class FormulaStatisticsDto
    {
        public int TotalCount { get; set; }
        public int SharedCount { get; set; }
        public int PrivateCount { get; set; }
        public int UsedCount { get; set; }
        public Dictionary<string, int> EffectStats { get; set; } = new();
        public Dictionary<string, int> CreatorStats { get; set; } = new();
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }

    /// <summary>
    /// 验方推荐DTO
    /// </summary>
    public class FormulaRecommendationDto
    {
        public Guid FormulaId { get; set; }
        public string FormulaName { get; set; } = string.Empty;
        public string Effect { get; set; } = string.Empty;
        public double MatchScore { get; set; }
        public int UsageCount { get; set; }
        public string MatchReason { get; set; } = string.Empty;
    }
}