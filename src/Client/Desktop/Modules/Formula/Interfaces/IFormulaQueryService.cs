using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Formulas;
using LYBT.Shared.Models.Enums;

namespace LYBT.Desktop.Formula.Interfaces;

/// <summary>
/// 验方查询服务接口 - UltraThink三层架构查询专业层
/// 职责：复杂查询、搜索、筛选、统计、报表查询
/// </summary>
public interface IFormulaQueryService
{
    #region 分页和列表查询

    /// <summary>
    /// 分页查询验方
    /// </summary>
    Task<ServiceResult<PagedResult<FormulaDto>>> GetPagedAsync(FormulaPagedQueryDto query);

    /// <summary>
    /// 获取验方列表（无分页）
    /// </summary>
    Task<ServiceResult<List<FormulaDto>>> GetFormulaListAsync(FormulaQueryOptions? options = null);

    /// <summary>
    /// 根据ID列表批量获取验方
    /// </summary>
    Task<ServiceResult<List<FormulaDto>>> GetFormulasByIdsAsync(List<Guid> formulaIds);

    /// <summary>
    /// 获取验方概要信息
    /// </summary>
    Task<ServiceResult<List<FormulaSummaryDto>>> GetFormulaSummariesAsync(FormulaQueryOptions? options = null);

    #endregion

    #region 搜索和筛选

    /// <summary>
    /// 搜索验方（关键词搜索）
    /// </summary>
    Task<ServiceResult<PagedResult<FormulaDto>>> SearchFormulasAsync(FormulaSearchDto searchDto);

    /// <summary>
    /// 按名称搜索
    /// </summary>
    Task<ServiceResult<List<FormulaDto>>> SearchByNameAsync(string name);

    /// <summary>
    /// 按类型搜索
    /// </summary>
    Task<ServiceResult<List<FormulaDto>>> SearchByTypeAsync(string type);

    /// <summary>
    /// 按症状搜索
    /// </summary>
    Task<ServiceResult<List<FormulaDto>>> SearchBySymptomAsync(string symptom);

    /// <summary>
    /// 按药材搜索
    /// </summary>
    Task<ServiceResult<List<FormulaDto>>> SearchByIngredientAsync(string ingredientName);

    /// <summary>
    /// 按来源搜索
    /// </summary>
    Task<ServiceResult<List<FormulaDto>>> SearchBySourceAsync(string source);

    /// <summary>
    /// 按功效搜索
    /// </summary>
    Task<ServiceResult<List<FormulaDto>>> SearchByEffectAsync(string effect);

    /// <summary>
    /// 按创建者搜索
    /// </summary>
    Task<ServiceResult<List<FormulaDto>>> SearchByCreatorAsync(Guid creatorId);

    /// <summary>
    /// 高级筛选验方
    /// </summary>
    Task<ServiceResult<PagedResult<FormulaDto>>> GetFormulasWithAdvancedFilterAsync(FormulaAdvancedFilterDto filter);

    #endregion

    #region 特定查询

    /// <summary>
    /// 根据名称获取验方
    /// </summary>
    Task<ServiceResult<FormulaDto>> GetFormulaByNameAsync(string name);

    /// <summary>
    /// 获取活跃验方
    /// </summary>
    Task<ServiceResult<List<FormulaDto>>> GetActiveFormulasAsync();

    /// <summary>
    /// 获取禁用验方
    /// </summary>
    Task<ServiceResult<List<FormulaDto>>> GetDisabledFormulasAsync();

    /// <summary>
    /// 获取经典验方
    /// </summary>
    Task<ServiceResult<List<FormulaDto>>> GetClassicFormulasAsync();

    /// <summary>
    /// 获取个人验方
    /// </summary>
    Task<ServiceResult<List<FormulaDto>>> GetPersonalFormulasAsync(Guid userId);

    /// <summary>
    /// 获取最近创建的验方
    /// </summary>
    Task<ServiceResult<List<FormulaDto>>> GetRecentlyCreatedFormulasAsync(int days = 30);

    /// <summary>
    /// 获取最近使用的验方
    /// </summary>
    Task<ServiceResult<List<FormulaDto>>> GetRecentlyUsedFormulasAsync(int days = 30);

    /// <summary>
    /// 获取热门验方
    /// </summary>
    Task<ServiceResult<List<FormulaDto>>> GetPopularFormulasAsync(int limit = 10);

    /// <summary>
    /// 获取推荐验方
    /// </summary>
    Task<ServiceResult<List<FormulaDto>>> GetRecommendedFormulasAsync(Guid userId, int limit = 5);

    #endregion

    #region 统计查询

    /// <summary>
    /// 获取验方统计信息
    /// </summary>
    Task<ServiceResult<FormulaStatisticsDto>> GetFormulaStatisticsAsync();

    /// <summary>
    /// 获取验方数量统计
    /// </summary>
    Task<ServiceResult<Dictionary<string, int>>> GetFormulaCountStatisticsAsync();

    /// <summary>
    /// 获取验方类型分布统计
    /// </summary>
    Task<ServiceResult<Dictionary<string, int>>> GetFormulaTypeDistributionAsync();

    /// <summary>
    /// 获取验方创建趋势数据
    /// </summary>
    Task<ServiceResult<List<FormulaCreationTrendDto>>> GetCreationTrendAsync(int days = 30);

    /// <summary>
    /// 获取验方使用统计
    /// </summary>
    Task<ServiceResult<FormulaUsageStatisticsDto>> GetFormulaUsageStatisticsAsync(int days = 30);

    /// <summary>
    /// 获取药材使用频次统计
    /// </summary>
    Task<ServiceResult<List<IngredientUsageStatisticsDto>>> GetIngredientUsageStatisticsAsync();

    /// <summary>
    /// 获取验方效果统计
    /// </summary>
    Task<ServiceResult<Dictionary<string, int>>> GetEffectDistributionAsync();

    #endregion

    #region 关联查询

    /// <summary>
    /// 获取验方的相关验方
    /// </summary>
    Task<ServiceResult<List<FormulaDto>>> GetRelatedFormulasAsync(Guid formulaId, int limit = 5);

    /// <summary>
    /// 获取同类验方
    /// </summary>
    Task<ServiceResult<List<FormulaDto>>> GetSimilarFormulasAsync(Guid formulaId, int limit = 5);

    /// <summary>
    /// 获取包含指定药材的验方
    /// </summary>
    Task<ServiceResult<List<FormulaDto>>> GetFormulasContainingIngredientAsync(Guid herbId);

    /// <summary>
    /// 获取验方的使用记录
    /// </summary>
    Task<ServiceResult<List<FormulaUsageHistoryDto>>> GetFormulaUsageHistoryAsync(Guid formulaId);

    /// <summary>
    /// 获取验方的评价记录
    /// </summary>
    Task<ServiceResult<List<FormulaReviewDto>>> GetFormulaReviewsAsync(Guid formulaId);

    #endregion

    #region 查询优化和缓存

    /// <summary>
    /// 预加载查询缓存
    /// </summary>
    Task<ServiceResult> PreloadQueryCacheAsync();

    /// <summary>
    /// 清除查询缓存
    /// </summary>
    ServiceResult ClearQueryCache();

    /// <summary>
    /// 获取查询性能统计
    /// </summary>
    ServiceResult<QueryPerformanceDto> GetQueryPerformanceStats();

    /// <summary>
    /// 优化查询索引
    /// </summary>
    Task<ServiceResult> OptimizeQueryIndexAsync();

    #endregion

    #region 导出查询

    /// <summary>
    /// 查询验方数据用于导出
    /// </summary>
    Task<ServiceResult<List<FormulaExportDto>>> GetFormulasForExportAsync(FormulaExportQueryDto query);

    /// <summary>
    /// 获取验方基础信息（轻量级）
    /// </summary>
    Task<ServiceResult<List<FormulaBasicInfoDto>>> GetFormulaBasicInfoAsync(List<Guid>? formulaIds = null);

    /// <summary>
    /// 获取验方详细信息（完整数据）
    /// </summary>
    Task<ServiceResult<List<FormulaDetailedInfoDto>>> GetFormulaDetailedInfoAsync(List<Guid> formulaIds);

    #endregion
}

/// <summary>
/// 验方查询选项
/// </summary>
public class FormulaQueryOptions
{
    public bool IncludeDisabled { get; set; } = false;
    public string? FilterByType { get; set; }
    public string? FilterBySource { get; set; }
    public Guid? FilterByCreator { get; set; }
    public DateTime? CreatedAfter { get; set; }
    public DateTime? CreatedBefore { get; set; }
    public string? SortBy { get; set; } = "Name";
    public bool SortDescending { get; set; } = false;
}

/// <summary>
/// 验方搜索DTO
/// </summary>
public class FormulaSearchDto : PagedQueryBaseDto
{
    public string? Name { get; set; }
    public string? Type { get; set; }
    public string? Source { get; set; }
    public string? Effect { get; set; }
    public string? Symptom { get; set; }
    public string? IngredientName { get; set; }
    public Guid? CreatorId { get; set; }
    public bool? IsEnabled { get; set; }
}

/// <summary>
/// 验方高级筛选DTO
/// </summary>
public class FormulaAdvancedFilterDto : PagedQueryBaseDto
{
    public List<string>? Types { get; set; }
    public List<string>? Sources { get; set; }
    public List<Guid>? CreatorIds { get; set; }
    public bool? IsEnabled { get; set; }
    public DateTime? CreatedAfter { get; set; }
    public DateTime? CreatedBefore { get; set; }
    public DateTime? LastUsedAfter { get; set; }
    public DateTime? LastUsedBefore { get; set; }
    public List<Guid>? IncludedIngredients { get; set; }
    public List<Guid>? ExcludedIngredients { get; set; }
    public List<Guid>? ExcludeFormulaIds { get; set; }
}

/// <summary>
/// 验方概要DTO
/// </summary>
public class FormulaSummaryDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public string Effect { get; set; } = string.Empty;
    public int IngredientCount { get; set; }
    public bool IsEnabled { get; set; }
    public DateTime CreateTime { get; set; }
    public DateTime? LastUsedTime { get; set; }
}

/// <summary>
/// 验方创建趋势DTO
/// </summary>
public class FormulaCreationTrendDto
{
    public DateTime Date { get; set; }
    public int CreationCount { get; set; }
    public int CumulativeCount { get; set; }
}

/// <summary>
/// 验方使用统计DTO
/// </summary>
public class FormulaUsageStatisticsDto
{
    public int DailyUsage { get; set; }
    public int WeeklyUsage { get; set; }
    public int MonthlyUsage { get; set; }
    public double AverageUsagePerFormula { get; set; }
    public DateTime LastUsageTime { get; set; }
}

/// <summary>
/// 药材使用统计DTO
/// </summary>
public class IngredientUsageStatisticsDto
{
    public Guid HerbId { get; set; }
    public string HerbName { get; set; } = string.Empty;
    public int UsageCount { get; set; }
    public double UsagePercentage { get; set; }
    public List<string> CommonFormulas { get; set; } = new();
}

/// <summary>
/// 验方基础信息DTO
/// </summary>
public class FormulaBasicInfoDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public bool IsEnabled { get; set; }
}

/// <summary>
/// 验方详细信息DTO
/// </summary>
public class FormulaDetailedInfoDto : FormulaBasicInfoDto
{
    public string Effect { get; set; } = string.Empty;
    public string Indications { get; set; } = string.Empty;
    public string Contraindications { get; set; } = string.Empty;
    public string Usage { get; set; } = string.Empty;
    public string Preparation { get; set; } = string.Empty;
    public string Dosage { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public List<FormulaIngredientDto> Ingredients { get; set; } = new();
    public Guid CreatorId { get; set; }
    public string CreatorName { get; set; } = string.Empty;
    public DateTime CreateTime { get; set; }
    public DateTime UpdateTime { get; set; }
    public DateTime? LastUsedTime { get; set; }
    public int UsageCount { get; set; }
}

/// <summary>
/// 验方使用历史DTO
/// </summary>
public class FormulaUsageHistoryDto
{
    public Guid Id { get; set; }
    public Guid FormulaId { get; set; }
    public string FormulaName { get; set; } = string.Empty;
    public Guid UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public DateTime UsageTime { get; set; }
    public string UsageType { get; set; } = string.Empty;
    public string? Notes { get; set; }
}

/// <summary>
/// 验方评价DTO
/// </summary>
public class FormulaReviewDto
{
    public Guid Id { get; set; }
    public Guid FormulaId { get; set; }
    public Guid ReviewerId { get; set; }
    public string ReviewerName { get; set; } = string.Empty;
    public int Rating { get; set; }
    public string Comment { get; set; } = string.Empty;
    public DateTime ReviewTime { get; set; }
}