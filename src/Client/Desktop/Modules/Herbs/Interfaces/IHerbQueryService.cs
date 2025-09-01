using LYBT.Shared.Models;
using LYBT.Shared.Models.Herbs;

namespace LYBT.Desktop.Herbs.Interfaces;

/// <summary>
/// 中药材查询服务接口 - UltraThink三层架构查询专业层
/// 职责：复杂查询、搜索筛选、统计分析、报表生成
/// </summary>
public interface IHerbQueryService
{
    #region 搜索和筛选功能
    
    /// <summary>
    /// 搜索中药材
    /// </summary>
    Task<ServiceResult<PagedResult<HerbDto>>> SearchHerbsAsync(HerbSearchDto searchDto);
    
    /// <summary>
    /// 按分类筛选中药材
    /// </summary>
    Task<ServiceResult<List<HerbDto>>> FilterByCategoryAsync(string category);
    
    /// <summary>
    /// 按功效筛选中药材
    /// </summary>
    Task<ServiceResult<List<HerbDto>>> FilterByEffectsAsync(List<string> effects);
    
    /// <summary>
    /// 按性味筛选中药材
    /// </summary>
    Task<ServiceResult<List<HerbDto>>> FilterByPropertiesAsync(string taste, string nature);
    
    /// <summary>
    /// 按价格范围筛选中药材
    /// </summary>
    Task<ServiceResult<List<HerbDto>>> FilterByPriceRangeAsync(decimal minPrice, decimal maxPrice);
    
    /// <summary>
    /// 模糊搜索中药材名称和别名
    /// </summary>
    Task<ServiceResult<List<HerbDto>>> FuzzySearchByNameAsync(string keyword);
    
    #endregion
    
    #region 统计分析功能
    
    /// <summary>
    /// 获取中药材统计信息
    /// </summary>
    Task<ServiceResult<HerbStatisticsDto>> GetHerbStatisticsAsync();
    
    /// <summary>
    /// 获取分类统计
    /// </summary>
    Task<ServiceResult<List<HerbCategoryStatDto>>> GetCategoryStatisticsAsync();
    
    /// <summary>
    /// 获取价格分析
    /// </summary>
    Task<ServiceResult<HerbPriceAnalysisDto>> GetPriceAnalysisAsync();
    
    /// <summary>
    /// 获取使用频率统计
    /// </summary>
    Task<ServiceResult<List<HerbUsageStatDto>>> GetUsageStatisticsAsync(int days = 30);
    
    /// <summary>
    /// 获取热门中药材排行
    /// </summary>
    Task<ServiceResult<List<HerbDto>>> GetPopularHerbsAsync(int topCount = 10);
    
    #endregion
    
    #region 价格趋势和历史
    
    /// <summary>
    /// 获取价格趋势分析
    /// </summary>
    Task<ServiceResult<List<HerbPriceTrendDto>>> GetPriceTrendsAsync(Guid herbId, int days = 30);
    
    /// <summary>
    /// 获取价格变更历史
    /// </summary>
    Task<ServiceResult<PagedResult<HerbPriceHistoryDto>>> GetPriceHistoryAsync(Guid herbId, int page = 1, int pageSize = 20);
    
    /// <summary>
    /// 比较多个药材价格趋势
    /// </summary>
    Task<ServiceResult<HerbPriceComparisonDto>> ComparePriceTrendsAsync(List<Guid> herbIds, int days = 30);
    
    /// <summary>
    /// 获取价格异常警报
    /// </summary>
    Task<ServiceResult<List<HerbPriceAlertDto>>> GetPriceAlertsAsync(decimal changeThreshold = 0.2m);
    
    #endregion
    
    #region 报表和导出查询
    
    /// <summary>
    /// 生成中药材库存报表
    /// </summary>
    Task<ServiceResult<HerbInventoryReportDto>> GenerateInventoryReportAsync(DateTime? startDate = null, DateTime? endDate = null);
    
    /// <summary>
    /// 生成价格变动报表
    /// </summary>
    Task<ServiceResult<HerbPriceChangeReportDto>> GeneratePriceChangeReportAsync(DateTime startDate, DateTime endDate);
    
    /// <summary>
    /// 获取导出用的中药材数据
    /// </summary>
    Task<ServiceResult<List<HerbExportItemDto>>> GetHerbsForExportAsync(HerbExportFilterDto filter);
    
    /// <summary>
    /// 获取高级搜索建议
    /// </summary>
    Task<ServiceResult<HerbSearchSuggestionDto>> GetSearchSuggestionsAsync(string keyword);
    
    #endregion
    
    #region 关联数据查询
    
    /// <summary>
    /// 获取药材在处方中的使用情况
    /// </summary>
    Task<ServiceResult<List<HerbUsageInPrescriptionDto>>> GetHerbUsageInPrescriptionsAsync(Guid herbId);
    
    /// <summary>
    /// 获取与指定药材常配伍的其他药材
    /// </summary>
    Task<ServiceResult<List<HerbCompatibilityDto>>> GetFrequentCombinationsAsync(Guid herbId, int topCount = 10);
    
    /// <summary>
    /// 根据症状推荐相关药材
    /// </summary>
    Task<ServiceResult<List<HerbRecommendationDto>>> GetRecommendedHerbsForSymptomsAsync(List<string> symptoms);
    
    #endregion
}