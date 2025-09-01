using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LYBT.Desktop.Herbs.Interfaces;
using LYBT.Shared.Models;
using LYBT.Shared.Models.Herbs;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Herbs.Services;

/// <summary>
/// 中药材查询服务实现 - UltraThink三层架构查询专业层
/// 职责：复杂查询、搜索筛选、统计分析、报表生成
/// </summary>
public class HerbQueryService : IHerbQueryService
{
    private readonly IHerbCoreService _coreService;
    private readonly ILogger<HerbQueryService> _logger;
    
    public HerbQueryService(
        IHerbCoreService coreService,
        ILogger<HerbQueryService> logger)
    {
        _coreService = coreService ?? throw new ArgumentNullException(nameof(coreService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }
    
    #region 搜索和筛选功能
    
    public async Task<ServiceResult<PagedResult<HerbDto>>> SearchHerbsAsync(HerbSearchDto searchDto)
    {
        try
        {
            _logger.LogInformation("执行中药材搜索: {Keyword}", searchDto.Keyword);
            
            // 获取所有中药材数据
            var allHerbsResult = await _coreService.GetAllHerbsAsync();
            if (!allHerbsResult.IsSuccess || allHerbsResult.Data == null)
            {
                return ServiceResult<PagedResult<HerbDto>>.Failure("获取中药材数据失败");
            }
            
            var allHerbs = allHerbsResult.Data;
            var filteredHerbs = allHerbs.AsQueryable();
            
            // 关键词搜索
            if (!string.IsNullOrWhiteSpace(searchDto.Keyword))
            {
                filteredHerbs = filteredHerbs.Where(h => 
                    h.Name.Contains(searchDto.Keyword, StringComparison.OrdinalIgnoreCase) ||
                    (h.Aliases != null && h.Aliases.Any(alias => alias.Contains(searchDto.Keyword, StringComparison.OrdinalIgnoreCase))));
            }
            
            // 分类筛选
            if (!string.IsNullOrWhiteSpace(searchDto.Category))
            {
                filteredHerbs = filteredHerbs.Where(h => 
                    h.Category.Equals(searchDto.Category, StringComparison.OrdinalIgnoreCase));
            }
            
            // 价格范围筛选
            if (searchDto.MinPrice.HasValue)
            {
                filteredHerbs = filteredHerbs.Where(h => h.Price >= searchDto.MinPrice.Value);
            }
            
            if (searchDto.MaxPrice.HasValue)
            {
                filteredHerbs = filteredHerbs.Where(h => h.Price <= searchDto.MaxPrice.Value);
            }
            
            // 排序
            filteredHerbs = searchDto.SortBy?.ToLower() switch
            {
                "name" => searchDto.SortDescending ? 
                    filteredHerbs.OrderByDescending(h => h.Name) : 
                    filteredHerbs.OrderBy(h => h.Name),
                "price" => searchDto.SortDescending ? 
                    filteredHerbs.OrderByDescending(h => h.Price) : 
                    filteredHerbs.OrderBy(h => h.Price),
                "category" => searchDto.SortDescending ? 
                    filteredHerbs.OrderByDescending(h => h.Category) : 
                    filteredHerbs.OrderBy(h => h.Category),
                _ => filteredHerbs.OrderBy(h => h.Name)
            };
            
            var totalCount = filteredHerbs.Count();
            
            // 分页
            var pagedHerbs = filteredHerbs
                .Skip((searchDto.Page - 1) * searchDto.PageSize)
                .Take(searchDto.PageSize)
                .ToList();
            
            var pagedResult = new PagedResult<HerbDto>(pagedHerbs, totalCount, searchDto.Page, searchDto.PageSize);
            
            _logger.LogInformation("中药材搜索完成，找到 {Count} 条记录", totalCount);
            return ServiceResult<PagedResult<HerbDto>>.Success(pagedResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "中药材搜索异常");
            return ServiceResult<PagedResult<HerbDto>>.Failure($"搜索异常: {ex.Message}");
        }
    }
    
    public async Task<ServiceResult<List<HerbDto>>> FilterByCategoryAsync(string category)
    {
        try
        {
            var allHerbsResult = await _coreService.GetAllHerbsAsync();
            if (!allHerbsResult.IsSuccess || allHerbsResult.Data == null)
            {
                return ServiceResult<List<HerbDto>>.Failure("获取中药材数据失败");
            }
            
            var filteredHerbs = allHerbsResult.Data
                .Where(h => h.Category.Equals(category, StringComparison.OrdinalIgnoreCase))
                .ToList();
            
            return ServiceResult<List<HerbDto>>.Success(filteredHerbs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "按分类筛选中药材异常: {Category}", category);
            return ServiceResult<List<HerbDto>>.Failure($"筛选异常: {ex.Message}");
        }
    }
    
    public async Task<ServiceResult<List<HerbDto>>> FilterByEffectsAsync(List<string> effects)
    {
        try
        {
            var allHerbsResult = await _coreService.GetAllHerbsAsync();
            if (!allHerbsResult.IsSuccess || allHerbsResult.Data == null)
            {
                return ServiceResult<List<HerbDto>>.Failure("获取中药材数据失败");
            }
            
            var filteredHerbs = allHerbsResult.Data
                .Where(h => effects.Any(effect => 
                    h.Effects != null && h.Effects.Contains(effect, StringComparison.OrdinalIgnoreCase)))
                .ToList();
            
            return ServiceResult<List<HerbDto>>.Success(filteredHerbs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "按功效筛选中药材异常");
            return ServiceResult<List<HerbDto>>.Failure($"筛选异常: {ex.Message}");
        }
    }
    
    public async Task<ServiceResult<List<HerbDto>>> FilterByPropertiesAsync(string taste, string nature)
    {
        try
        {
            var allHerbsResult = await _coreService.GetAllHerbsAsync();
            if (!allHerbsResult.IsSuccess || allHerbsResult.Data == null)
            {
                return ServiceResult<List<HerbDto>>.Failure("获取中药材数据失败");
            }
            
            var filteredHerbs = allHerbsResult.Data.Where(h =>
            {
                var matchesTaste = string.IsNullOrWhiteSpace(taste) || 
                    (h.Taste != null && h.Taste.Contains(taste, StringComparison.OrdinalIgnoreCase));
                var matchesNature = string.IsNullOrWhiteSpace(nature) || 
                    (h.Nature != null && h.Nature.Contains(nature, StringComparison.OrdinalIgnoreCase));
                return matchesTaste && matchesNature;
            }).ToList();
            
            return ServiceResult<List<HerbDto>>.Success(filteredHerbs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "按性味筛选中药材异常");
            return ServiceResult<List<HerbDto>>.Failure($"筛选异常: {ex.Message}");
        }
    }
    
    public async Task<ServiceResult<List<HerbDto>>> FilterByPriceRangeAsync(decimal minPrice, decimal maxPrice)
    {
        try
        {
            var allHerbsResult = await _coreService.GetAllHerbsAsync();
            if (!allHerbsResult.IsSuccess || allHerbsResult.Data == null)
            {
                return ServiceResult<List<HerbDto>>.Failure("获取中药材数据失败");
            }
            
            var filteredHerbs = allHerbsResult.Data
                .Where(h => h.Price >= minPrice && h.Price <= maxPrice)
                .ToList();
            
            return ServiceResult<List<HerbDto>>.Success(filteredHerbs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "按价格范围筛选中药材异常: {MinPrice}-{MaxPrice}", minPrice, maxPrice);
            return ServiceResult<List<HerbDto>>.Failure($"筛选异常: {ex.Message}");
        }
    }
    
    public async Task<ServiceResult<List<HerbDto>>> FuzzySearchByNameAsync(string keyword)
    {
        try
        {
            var allHerbsResult = await _coreService.GetAllHerbsAsync();
            if (!allHerbsResult.IsSuccess || allHerbsResult.Data == null)
            {
                return ServiceResult<List<HerbDto>>.Failure("获取中药材数据失败");
            }
            
            var searchResults = allHerbsResult.Data
                .Where(h => h.Name.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                           (h.Aliases != null && h.Aliases.Any(alias => 
                               alias.Contains(keyword, StringComparison.OrdinalIgnoreCase))))
                .OrderBy(h => h.Name.Length) // 按名称长度排序，更精确匹配优先
                .ToList();
            
            return ServiceResult<List<HerbDto>>.Success(searchResults);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "模糊搜索中药材异常: {Keyword}", keyword);
            return ServiceResult<List<HerbDto>>.Failure($"搜索异常: {ex.Message}");
        }
    }
    
    #endregion
    
    #region 统计分析功能
    
    public async Task<ServiceResult<HerbStatisticsDto>> GetHerbStatisticsAsync()
    {
        try
        {
            var allHerbsResult = await _coreService.GetAllHerbsAsync();
            if (!allHerbsResult.IsSuccess || allHerbsResult.Data == null)
            {
                return ServiceResult<HerbStatisticsDto>.Failure("获取中药材数据失败");
            }
            
            var allHerbs = allHerbsResult.Data;
            
            var statistics = new HerbStatisticsDto
            {
                TotalCount = allHerbs.Count,
                CategoryCount = allHerbs.Select(h => h.Category).Distinct().Count(),
                AveragePrice = allHerbs.Any() ? allHerbs.Average(h => h.Price) : 0,
                MinPrice = allHerbs.Any() ? allHerbs.Min(h => h.Price) : 0,
                MaxPrice = allHerbs.Any() ? allHerbs.Max(h => h.Price) : 0,
                LastUpdated = DateTime.Now
            };
            
            return ServiceResult<HerbStatisticsDto>.Success(statistics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取中药材统计信息异常");
            return ServiceResult<HerbStatisticsDto>.Failure($"统计异常: {ex.Message}");
        }
    }
    
    public async Task<ServiceResult<List<HerbCategoryStatDto>>> GetCategoryStatisticsAsync()
    {
        try
        {
            var allHerbsResult = await _coreService.GetAllHerbsAsync();
            if (!allHerbsResult.IsSuccess || allHerbsResult.Data == null)
            {
                return ServiceResult<List<HerbCategoryStatDto>>.Failure("获取中药材数据失败");
            }
            
            var categoryStats = allHerbsResult.Data
                .GroupBy(h => h.Category)
                .Select(g => new HerbCategoryStatDto
                {
                    Category = g.Key,
                    Count = g.Count(),
                    AveragePrice = g.Average(h => h.Price),
                    MinPrice = g.Min(h => h.Price),
                    MaxPrice = g.Max(h => h.Price)
                })
                .OrderByDescending(s => s.Count)
                .ToList();
            
            return ServiceResult<List<HerbCategoryStatDto>>.Success(categoryStats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取分类统计异常");
            return ServiceResult<List<HerbCategoryStatDto>>.Failure($"统计异常: {ex.Message}");
        }
    }
    
    public async Task<ServiceResult<HerbPriceAnalysisDto>> GetPriceAnalysisAsync()
    {
        try
        {
            var allHerbsResult = await _coreService.GetAllHerbsAsync();
            if (!allHerbsResult.IsSuccess || allHerbsResult.Data == null)
            {
                return ServiceResult<HerbPriceAnalysisDto>.Failure("获取中药材数据失败");
            }
            
            var allHerbs = allHerbsResult.Data;
            var prices = allHerbs.Select(h => h.Price).OrderBy(p => p).ToList();
            
            var analysis = new HerbPriceAnalysisDto
            {
                AveragePrice = prices.Any() ? prices.Average() : 0,
                MedianPrice = prices.Any() ? GetMedian(prices) : 0,
                MinPrice = prices.Any() ? prices.Min() : 0,
                MaxPrice = prices.Any() ? prices.Max() : 0,
                PriceRanges = GetPriceRangeDistribution(prices)
            };
            
            return ServiceResult<HerbPriceAnalysisDto>.Success(analysis);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取价格分析异常");
            return ServiceResult<HerbPriceAnalysisDto>.Failure($"分析异常: {ex.Message}");
        }
    }
    
    public async Task<ServiceResult<List<HerbUsageStatDto>>> GetUsageStatisticsAsync(int days = 30)
    {
        try
        {
            // 注意：这里需要结合处方数据来统计使用频率
            // 暂时返回模拟数据，实际应该调用处方服务获取数据
            var allHerbsResult = await _coreService.GetAllHerbsAsync();
            if (!allHerbsResult.IsSuccess || allHerbsResult.Data == null)
            {
                return ServiceResult<List<HerbUsageStatDto>>.Failure("获取中药材数据失败");
            }
            
            // 模拟使用统计数据
            var usageStats = allHerbsResult.Data
                .Take(20) // 只取前20个作为示例
                .Select(h => new HerbUsageStatDto
                {
                    HerbId = h.Id,
                    HerbName = h.Name,
                    UsageCount = new Random().Next(0, 50), // 模拟使用次数
                    LastUsedDate = DateTime.Now.AddDays(-new Random().Next(0, days))
                })
                .OrderByDescending(s => s.UsageCount)
                .ToList();
            
            return ServiceResult<List<HerbUsageStatDto>>.Success(usageStats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取使用统计异常");
            return ServiceResult<List<HerbUsageStatDto>>.Failure($"统计异常: {ex.Message}");
        }
    }
    
    public async Task<ServiceResult<List<HerbDto>>> GetPopularHerbsAsync(int topCount = 10)
    {
        try
        {
            var usageStatsResult = await GetUsageStatisticsAsync();
            if (!usageStatsResult.IsSuccess || usageStatsResult.Data == null)
            {
                return ServiceResult<List<HerbDto>>.Failure("获取使用统计失败");
            }
            
            var allHerbsResult = await _coreService.GetAllHerbsAsync();
            if (!allHerbsResult.IsSuccess || allHerbsResult.Data == null)
            {
                return ServiceResult<List<HerbDto>>.Failure("获取中药材数据失败");
            }
            
            var popularHerbIds = usageStatsResult.Data
                .OrderByDescending(s => s.UsageCount)
                .Take(topCount)
                .Select(s => s.HerbId)
                .ToList();
            
            var popularHerbs = allHerbsResult.Data
                .Where(h => popularHerbIds.Contains(h.Id))
                .ToList();
            
            return ServiceResult<List<HerbDto>>.Success(popularHerbs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取热门中药材异常");
            return ServiceResult<List<HerbDto>>.Failure($"获取异常: {ex.Message}");
        }
    }
    
    #endregion
    
    #region 价格趋势和历史 (简化实现)
    
    public async Task<ServiceResult<List<HerbPriceTrendDto>>> GetPriceTrendsAsync(Guid herbId, int days = 30)
    {
        try
        {
            // 简化实现：返回模拟的价格趋势数据
            // 实际应该从价格历史表中获取数据
            var herbResult = await _coreService.GetHerbByIdAsync(herbId);
            if (!herbResult.IsSuccess || herbResult.Data == null)
            {
                return ServiceResult<List<HerbPriceTrendDto>>.Failure("中药材不存在");
            }
            
            var currentPrice = herbResult.Data.Price;
            var trends = new List<HerbPriceTrendDto>();
            var random = new Random();
            
            for (int i = days; i >= 0; i--)
            {
                trends.Add(new HerbPriceTrendDto
                {
                    Date = DateTime.Now.AddDays(-i),
                    Price = currentPrice + (decimal)(random.NextDouble() - 0.5) * currentPrice * 0.1m,
                    HerbId = herbId
                });
            }
            
            return ServiceResult<List<HerbPriceTrendDto>>.Success(trends);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取价格趋势异常: {HerbId}", herbId);
            return ServiceResult<List<HerbPriceTrendDto>>.Failure($"获取异常: {ex.Message}");
        }
    }
    
    // 其他方法的简化实现...
    public async Task<ServiceResult<PagedResult<HerbPriceHistoryDto>>> GetPriceHistoryAsync(Guid herbId, int page = 1, int pageSize = 20)
    {
        // 简化实现，返回空结果
        var emptyResult = new PagedResult<HerbPriceHistoryDto>(new List<HerbPriceHistoryDto>(), 0, page, pageSize);
        return ServiceResult<PagedResult<HerbPriceHistoryDto>>.Success(emptyResult);
    }
    
    public async Task<ServiceResult<HerbPriceComparisonDto>> ComparePriceTrendsAsync(List<Guid> herbIds, int days = 30)
    {
        var comparison = new HerbPriceComparisonDto { HerbComparisons = new List<HerbPriceTrendDto>() };
        return ServiceResult<HerbPriceComparisonDto>.Success(comparison);
    }
    
    public async Task<ServiceResult<List<HerbPriceAlertDto>>> GetPriceAlertsAsync(decimal changeThreshold = 0.2m)
    {
        return ServiceResult<List<HerbPriceAlertDto>>.Success(new List<HerbPriceAlertDto>());
    }
    
    // 其他接口方法的简化实现...
    public async Task<ServiceResult<HerbInventoryReportDto>> GenerateInventoryReportAsync(DateTime? startDate = null, DateTime? endDate = null)
    {
        var report = new HerbInventoryReportDto();
        return ServiceResult<HerbInventoryReportDto>.Success(report);
    }
    
    public async Task<ServiceResult<HerbPriceChangeReportDto>> GeneratePriceChangeReportAsync(DateTime startDate, DateTime endDate)
    {
        var report = new HerbPriceChangeReportDto();
        return ServiceResult<HerbPriceChangeReportDto>.Success(report);
    }
    
    public async Task<ServiceResult<List<HerbExportItemDto>>> GetHerbsForExportAsync(HerbExportFilterDto filter)
    {
        var allHerbsResult = await _coreService.GetAllHerbsAsync();
        if (!allHerbsResult.IsSuccess || allHerbsResult.Data == null)
        {
            return ServiceResult<List<HerbExportItemDto>>.Failure("获取中药材数据失败");
        }
        
        var exportItems = allHerbsResult.Data.Select(h => new HerbExportItemDto
        {
            Id = h.Id,
            Name = h.Name,
            Category = h.Category,
            Price = h.Price
        }).ToList();
        
        return ServiceResult<List<HerbExportItemDto>>.Success(exportItems);
    }
    
    public async Task<ServiceResult<HerbSearchSuggestionDto>> GetSearchSuggestionsAsync(string keyword)
    {
        var suggestion = new HerbSearchSuggestionDto { Suggestions = new List<string>() };
        return ServiceResult<HerbSearchSuggestionDto>.Success(suggestion);
    }
    
    public async Task<ServiceResult<List<HerbUsageInPrescriptionDto>>> GetHerbUsageInPrescriptionsAsync(Guid herbId)
    {
        return ServiceResult<List<HerbUsageInPrescriptionDto>>.Success(new List<HerbUsageInPrescriptionDto>());
    }
    
    public async Task<ServiceResult<List<HerbCompatibilityDto>>> GetFrequentCombinationsAsync(Guid herbId, int topCount = 10)
    {
        return ServiceResult<List<HerbCompatibilityDto>>.Success(new List<HerbCompatibilityDto>());
    }
    
    public async Task<ServiceResult<List<HerbRecommendationDto>>> GetRecommendedHerbsForSymptomsAsync(List<string> symptoms)
    {
        return ServiceResult<List<HerbRecommendationDto>>.Success(new List<HerbRecommendationDto>());
    }
    
    #endregion
    
    #region 辅助方法
    
    private decimal GetMedian(List<decimal> values)
    {
        if (!values.Any()) return 0;
        
        var sorted = values.OrderBy(x => x).ToList();
        var mid = sorted.Count / 2;
        
        return sorted.Count % 2 == 0
            ? (sorted[mid - 1] + sorted[mid]) / 2
            : sorted[mid];
    }
    
    private List<HerbPriceRangeDto> GetPriceRangeDistribution(List<decimal> prices)
    {
        if (!prices.Any()) return new List<HerbPriceRangeDto>();
        
        var ranges = new List<HerbPriceRangeDto>
        {
            new() { RangeName = "0-10元", MinPrice = 0, MaxPrice = 10, Count = prices.Count(p => p >= 0 && p <= 10) },
            new() { RangeName = "10-50元", MinPrice = 10, MaxPrice = 50, Count = prices.Count(p => p > 10 && p <= 50) },
            new() { RangeName = "50-100元", MinPrice = 50, MaxPrice = 100, Count = prices.Count(p => p > 50 && p <= 100) },
            new() { RangeName = "100元以上", MinPrice = 100, MaxPrice = decimal.MaxValue, Count = prices.Count(p => p > 100) }
        };
        
        return ranges;
    }
    
    #endregion
}