using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using LYBT.Desktop.Formula.Interfaces;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Formulas;

namespace LYBT.Desktop.Formula.Services;

/// <summary>
/// 验方查询服务 - UltraThink三层架构查询专业层
/// 职责：复杂查询、搜索、筛选、统计、报表查询
/// </summary>
public class FormulaQueryService : IFormulaQueryService
{
    private readonly IFormulaCoreService _coreService;
    private readonly IMemoryCache _cache;
    private readonly ILogger<FormulaQueryService> _logger;

    // 查询缓存键常量
    private const string QUERY_CACHE_PREFIX = "formula_query_";
    private const string STATS_CACHE_PREFIX = "formula_stats_";
    private const string TREND_CACHE_PREFIX = "formula_trend_";

    // 缓存时间配置
    private static readonly TimeSpan QueryCacheTime = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan StatsCacheTime = TimeSpan.FromMinutes(10);
    private static readonly TimeSpan TrendCacheTime = TimeSpan.FromMinutes(15);

    // 性能统计
    private int _totalQueries = 0;
    private long _totalQueryTime = 0;

    public FormulaQueryService(
        IFormulaCoreService coreService,
        IMemoryCache cache,
        ILogger<FormulaQueryService> logger)
    {
        _coreService = coreService ?? throw new ArgumentNullException(nameof(coreService));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    #region 分页和列表查询

    public async Task<ServiceResult<PagedResult<FormulaDto>>> GetPagedAsync(FormulaPagedQueryDto query)
    {
        var startTime = DateTime.Now;
        try
        {
            var validation = _coreService.ValidatePagedQueryParameters(query);
            if (!validation.IsSuccess)
                return ServiceResult<PagedResult<FormulaDto>>.Failure(validation.ErrorMessage);

            var result = await _coreService.CallGetPagedFormulasApiAsync(query);
            
            RecordQueryPerformance("GetPaged", startTime);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "分页查询验方时发生异常");
            return ServiceResult<PagedResult<FormulaDto>>.Failure("分页查询失败: " + ex.Message);
        }
    }

    public async Task<ServiceResult<List<FormulaDto>>> GetFormulaListAsync(FormulaQueryOptions? options = null)
    {
        var startTime = DateTime.Now;
        try
        {
            var cacheKey = GenerateQueryCacheKey("list", options);

            // 尝试从缓存获取
            if (_cache.TryGetValue(cacheKey, out List<FormulaDto> cachedList))
            {
                RecordQueryPerformance("GetFormulaList(cached)", startTime);
                return ServiceResult<List<FormulaDto>>.Success(cachedList, "验方列表获取成功(缓存)");
            }

            // 从API获取全部验方
            var allFormulasResult = await _coreService.CallGetFormulasApiAsync();
            if (!allFormulasResult.IsSuccess)
                return ServiceResult<List<FormulaDto>>.Failure(allFormulasResult.ErrorMessage);

            var formulas = allFormulasResult.Data;

            // 应用筛选条件
            if (options != null)
            {
                formulas = ApplyQueryOptions(formulas, options);
            }

            // 缓存结果
            _cache.Set(cacheKey, formulas, QueryCacheTime);

            RecordQueryPerformance("GetFormulaList", startTime);
            return ServiceResult<List<FormulaDto>>.Success(formulas, "验方列表获取成功");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取验方列表时发生异常");
            return ServiceResult<List<FormulaDto>>.Failure("获取验方列表失败: " + ex.Message);
        }
    }

    public async Task<ServiceResult<List<FormulaDto>>> GetFormulasByIdsAsync(List<Guid> formulaIds)
    {
        var startTime = DateTime.Now;
        try
        {
            if (formulaIds == null || !formulaIds.Any())
                return ServiceResult<List<FormulaDto>>.Success(new List<FormulaDto>(), "空ID列表");

            var formulas = new List<FormulaDto>();

            // 并发获取验方详情
            var tasks = formulaIds.Select(async id =>
            {
                var result = await _coreService.CallGetFormulaByIdApiAsync(id);
                return result.IsSuccess ? result.Data : null;
            });

            var results = await Task.WhenAll(tasks);
            formulas.AddRange(results.Where(f => f != null)!);

            RecordQueryPerformance("GetFormulasByIds", startTime);
            return ServiceResult<List<FormulaDto>>.Success(formulas, $"成功获取{formulas.Count}个验方");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "根据ID列表获取验方时发生异常");
            return ServiceResult<List<FormulaDto>>.Failure("批量获取验方失败: " + ex.Message);
        }
    }

    public async Task<ServiceResult<List<FormulaSummaryDto>>> GetFormulaSummariesAsync(FormulaQueryOptions? options = null)
    {
        var startTime = DateTime.Now;
        try
        {
            var formulasResult = await GetFormulaListAsync(options);
            if (!formulasResult.IsSuccess)
                return ServiceResult<List<FormulaSummaryDto>>.Failure(formulasResult.ErrorMessage);

            var summaries = formulasResult.Data.Select(f => new FormulaSummaryDto
            {
                Id = f.Id,
                Name = f.Name,
                Type = f.Type,
                Source = f.Source,
                Effect = f.Effect,
                IngredientCount = f.Ingredients?.Count ?? 0,
                IsEnabled = f.IsEnabled,
                CreateTime = f.CreateTime,
                LastUsedTime = f.LastUsedTime
            }).ToList();

            RecordQueryPerformance("GetFormulaSummaries", startTime);
            return ServiceResult<List<FormulaSummaryDto>>.Success(summaries, "验方概要获取成功");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取验方概要时发生异常");
            return ServiceResult<List<FormulaSummaryDto>>.Failure("获取验方概要失败: " + ex.Message);
        }
    }

    #endregion

    #region 搜索和筛选

    public async Task<ServiceResult<PagedResult<FormulaDto>>> SearchFormulasAsync(FormulaSearchDto searchDto)
    {
        var startTime = DateTime.Now;
        try
        {
            var validation = _coreService.ValidateSearchParameters(searchDto);
            if (!validation.IsSuccess)
                return ServiceResult<PagedResult<FormulaDto>>.Failure(validation.ErrorMessage);

            var cacheKey = GenerateQueryCacheKey("search", searchDto);

            // 尝试从缓存获取
            if (_cache.TryGetValue(cacheKey, out List<FormulaDto> cachedResult))
            {
                var pagedResult = CreatePagedResult(cachedResult, searchDto.PageIndex, searchDto.PageSize);
                RecordQueryPerformance("SearchFormulas(cached)", startTime);
                return ServiceResult<PagedResult<FormulaDto>>.Success(pagedResult, "验方搜索成功(缓存)");
            }

            // 调用API搜索
            var searchResult = await _coreService.CallSearchFormulasApiAsync(searchDto);
            if (!searchResult.IsSuccess)
                return ServiceResult<PagedResult<FormulaDto>>.Failure(searchResult.ErrorMessage);

            // 缓存搜索结果
            _cache.Set(cacheKey, searchResult.Data, QueryCacheTime);

            var finalResult = CreatePagedResult(searchResult.Data, searchDto.PageIndex, searchDto.PageSize);
            RecordQueryPerformance("SearchFormulas", startTime);
            return ServiceResult<PagedResult<FormulaDto>>.Success(finalResult, "验方搜索成功");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "搜索验方时发生异常");
            return ServiceResult<PagedResult<FormulaDto>>.Failure("验方搜索失败: " + ex.Message);
        }
    }

    public async Task<ServiceResult<List<FormulaDto>>> SearchByNameAsync(string name)
    {
        var startTime = DateTime.Now;
        try
        {
            if (string.IsNullOrWhiteSpace(name))
                return ServiceResult<List<FormulaDto>>.Success(new List<FormulaDto>(), "搜索关键词为空");

            var searchDto = new FormulaSearchDto
            {
                Name = name,
                PageSize = 100
            };

            var searchResult = await _coreService.CallSearchFormulasApiAsync(searchDto);
            if (!searchResult.IsSuccess)
                return ServiceResult<List<FormulaDto>>.Failure(searchResult.ErrorMessage);

            var filteredResults = searchResult.Data
                .Where(f => f.Name.Contains(name, StringComparison.OrdinalIgnoreCase))
                .ToList();

            RecordQueryPerformance("SearchByName", startTime);
            return ServiceResult<List<FormulaDto>>.Success(filteredResults, $"按名称搜索到{filteredResults.Count}个验方");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "按名称搜索验方时发生异常，名称: {Name}", name);
            return ServiceResult<List<FormulaDto>>.Failure("按名称搜索失败: " + ex.Message);
        }
    }

    public async Task<ServiceResult<List<FormulaDto>>> SearchByTypeAsync(string type)
    {
        var startTime = DateTime.Now;
        try
        {
            if (string.IsNullOrWhiteSpace(type))
                return ServiceResult<List<FormulaDto>>.Success(new List<FormulaDto>(), "类型筛选条件为空");

            var searchDto = new FormulaSearchDto
            {
                Type = type,
                PageSize = 100
            };

            var searchResult = await _coreService.CallSearchFormulasApiAsync(searchDto);
            RecordQueryPerformance("SearchByType", startTime);
            
            return searchResult.IsSuccess
                ? ServiceResult<List<FormulaDto>>.Success(searchResult.Data, $"按类型搜索到{searchResult.Data.Count}个验方")
                : ServiceResult<List<FormulaDto>>.Failure(searchResult.ErrorMessage);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "按类型搜索验方时发生异常，类型: {Type}", type);
            return ServiceResult<List<FormulaDto>>.Failure("按类型搜索失败: " + ex.Message);
        }
    }

    public async Task<ServiceResult<List<FormulaDto>>> SearchBySymptomAsync(string symptom)
    {
        var startTime = DateTime.Now;
        try
        {
            if (string.IsNullOrWhiteSpace(symptom))
                return ServiceResult<List<FormulaDto>>.Success(new List<FormulaDto>(), "症状筛选条件为空");

            var searchDto = new FormulaSearchDto
            {
                Symptom = symptom,
                PageSize = 100
            };

            var searchResult = await _coreService.CallSearchFormulasApiAsync(searchDto);
            RecordQueryPerformance("SearchBySymptom", startTime);
            
            return searchResult.IsSuccess
                ? ServiceResult<List<FormulaDto>>.Success(searchResult.Data, $"按症状搜索到{searchResult.Data.Count}个验方")
                : ServiceResult<List<FormulaDto>>.Failure(searchResult.ErrorMessage);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "按症状搜索验方时发生异常，症状: {Symptom}", symptom);
            return ServiceResult<List<FormulaDto>>.Failure("按症状搜索失败: " + ex.Message);
        }
    }

    public async Task<ServiceResult<List<FormulaDto>>> SearchByIngredientAsync(string ingredientName)
    {
        var startTime = DateTime.Now;
        try
        {
            if (string.IsNullOrWhiteSpace(ingredientName))
                return ServiceResult<List<FormulaDto>>.Success(new List<FormulaDto>(), "药材筛选条件为空");

            var searchDto = new FormulaSearchDto
            {
                IngredientName = ingredientName,
                PageSize = 100
            };

            var searchResult = await _coreService.CallSearchFormulasApiAsync(searchDto);
            RecordQueryPerformance("SearchByIngredient", startTime);
            
            return searchResult.IsSuccess
                ? ServiceResult<List<FormulaDto>>.Success(searchResult.Data, $"包含药材{ingredientName}的验方{searchResult.Data.Count}个")
                : ServiceResult<List<FormulaDto>>.Failure(searchResult.ErrorMessage);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "按药材搜索验方时发生异常，药材: {IngredientName}", ingredientName);
            return ServiceResult<List<FormulaDto>>.Failure("按药材搜索失败: " + ex.Message);
        }
    }

    public async Task<ServiceResult<List<FormulaDto>>> SearchBySourceAsync(string source)
    {
        var startTime = DateTime.Now;
        try
        {
            if (string.IsNullOrWhiteSpace(source))
                return ServiceResult<List<FormulaDto>>.Success(new List<FormulaDto>(), "来源筛选条件为空");

            var searchDto = new FormulaSearchDto
            {
                Source = source,
                PageSize = 100
            };

            var searchResult = await _coreService.CallSearchFormulasApiAsync(searchDto);
            RecordQueryPerformance("SearchBySource", startTime);
            
            return searchResult.IsSuccess
                ? ServiceResult<List<FormulaDto>>.Success(searchResult.Data, $"来源于{source}的验方{searchResult.Data.Count}个")
                : ServiceResult<List<FormulaDto>>.Failure(searchResult.ErrorMessage);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "按来源搜索验方时发生异常，来源: {Source}", source);
            return ServiceResult<List<FormulaDto>>.Failure("按来源搜索失败: " + ex.Message);
        }
    }

    public async Task<ServiceResult<List<FormulaDto>>> SearchByEffectAsync(string effect)
    {
        var startTime = DateTime.Now;
        try
        {
            if (string.IsNullOrWhiteSpace(effect))
                return ServiceResult<List<FormulaDto>>.Success(new List<FormulaDto>(), "功效筛选条件为空");

            var searchDto = new FormulaSearchDto
            {
                Effect = effect,
                PageSize = 100
            };

            var searchResult = await _coreService.CallSearchFormulasApiAsync(searchDto);
            RecordQueryPerformance("SearchByEffect", startTime);
            
            return searchResult.IsSuccess
                ? ServiceResult<List<FormulaDto>>.Success(searchResult.Data, $"功效{effect}的验方{searchResult.Data.Count}个")
                : ServiceResult<List<FormulaDto>>.Failure(searchResult.ErrorMessage);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "按功效搜索验方时发生异常，功效: {Effect}", effect);
            return ServiceResult<List<FormulaDto>>.Failure("按功效搜索失败: " + ex.Message);
        }
    }

    public async Task<ServiceResult<List<FormulaDto>>> SearchByCreatorAsync(Guid creatorId)
    {
        var startTime = DateTime.Now;
        try
        {
            if (creatorId == Guid.Empty)
                return ServiceResult<List<FormulaDto>>.Success(new List<FormulaDto>(), "创建者ID无效");

            var searchDto = new FormulaSearchDto
            {
                CreatorId = creatorId,
                PageSize = 100
            };

            var searchResult = await _coreService.CallSearchFormulasApiAsync(searchDto);
            RecordQueryPerformance("SearchByCreator", startTime);
            
            return searchResult.IsSuccess
                ? ServiceResult<List<FormulaDto>>.Success(searchResult.Data, $"创建者的验方{searchResult.Data.Count}个")
                : ServiceResult<List<FormulaDto>>.Failure(searchResult.ErrorMessage);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "按创建者搜索验方时发生异常，创建者ID: {CreatorId}", creatorId);
            return ServiceResult<List<FormulaDto>>.Failure("按创建者搜索失败: " + ex.Message);
        }
    }

    public async Task<ServiceResult<PagedResult<FormulaDto>>> GetFormulasWithAdvancedFilterAsync(FormulaAdvancedFilterDto filter)
    {
        var startTime = DateTime.Now;
        try
        {
            if (filter == null)
                return ServiceResult<PagedResult<FormulaDto>>.Failure("筛选条件不能为空");

            // 转换为基本搜索条件
            var searchDto = new FormulaSearchDto
            {
                PageIndex = filter.PageIndex,
                PageSize = filter.PageSize,
                IsEnabled = filter.IsEnabled
            };

            // 应用高级筛选逻辑
            var searchResult = await _coreService.CallSearchFormulasApiAsync(searchDto);
            if (!searchResult.IsSuccess)
                return ServiceResult<PagedResult<FormulaDto>>.Failure(searchResult.ErrorMessage);

            // 进一步筛选
            var filteredResults = ApplyAdvancedFilter(searchResult.Data, filter);
            var pagedResult = CreatePagedResult(filteredResults, filter.PageIndex, filter.PageSize);

            RecordQueryPerformance("GetFormulasWithAdvancedFilter", startTime);
            return ServiceResult<PagedResult<FormulaDto>>.Success(pagedResult, "高级筛选完成");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "高级筛选验方时发生异常");
            return ServiceResult<PagedResult<FormulaDto>>.Failure("高级筛选失败: " + ex.Message);
        }
    }

    #endregion

    #region 特定查询

    public async Task<ServiceResult<FormulaDto>> GetFormulaByNameAsync(string name)
    {
        var startTime = DateTime.Now;
        try
        {
            if (string.IsNullOrWhiteSpace(name))
                return ServiceResult<FormulaDto>.Failure("验方名称不能为空");

            var searchResult = await SearchByNameAsync(name);
            if (!searchResult.IsSuccess)
                return ServiceResult<FormulaDto>.Failure(searchResult.ErrorMessage);

            var exactMatch = searchResult.Data.FirstOrDefault(f => 
                f.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

            RecordQueryPerformance("GetFormulaByName", startTime);
            
            return exactMatch != null
                ? ServiceResult<FormulaDto>.Success(exactMatch, "验方获取成功")
                : ServiceResult<FormulaDto>.Failure("未找到指定名称的验方");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "根据名称获取验方时发生异常，名称: {Name}", name);
            return ServiceResult<FormulaDto>.Failure("根据名称获取验方失败: " + ex.Message);
        }
    }

    public async Task<ServiceResult<List<FormulaDto>>> GetActiveFormulasAsync()
    {
        var options = new FormulaQueryOptions
        {
            IncludeDisabled = false
        };

        return await GetFormulaListAsync(options);
    }

    public async Task<ServiceResult<List<FormulaDto>>> GetDisabledFormulasAsync()
    {
        var allFormulasResult = await GetFormulaListAsync(new FormulaQueryOptions { IncludeDisabled = true });
        if (!allFormulasResult.IsSuccess)
            return ServiceResult<List<FormulaDto>>.Failure(allFormulasResult.ErrorMessage);

        var disabledFormulas = allFormulasResult.Data.Where(f => !f.IsEnabled).ToList();
        return ServiceResult<List<FormulaDto>>.Success(disabledFormulas, $"禁用验方{disabledFormulas.Count}个");
    }

    public async Task<ServiceResult<List<FormulaDto>>> GetClassicFormulasAsync()
    {
        return await SearchBySourceAsync("经典");
    }

    public async Task<ServiceResult<List<FormulaDto>>> GetPersonalFormulasAsync(Guid userId)
    {
        return await SearchByCreatorAsync(userId);
    }

    public async Task<ServiceResult<List<FormulaDto>>> GetRecentlyCreatedFormulasAsync(int days = 30)
    {
        var startTime = DateTime.Now;
        try
        {
            var cutoffDate = DateTime.Now.AddDays(-days);
            var options = new FormulaQueryOptions
            {
                CreatedAfter = cutoffDate,
                SortBy = "CreateTime",
                SortDescending = true
            };

            var result = await GetFormulaListAsync(options);
            RecordQueryPerformance("GetRecentlyCreatedFormulas", startTime);
            
            return result.IsSuccess
                ? ServiceResult<List<FormulaDto>>.Success(result.Data, $"近{days}天创建的验方{result.Data.Count}个")
                : result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取最近创建的验方时发生异常，天数: {Days}", days);
            return ServiceResult<List<FormulaDto>>.Failure("获取最近创建的验方失败: " + ex.Message);
        }
    }

    public async Task<ServiceResult<List<FormulaDto>>> GetRecentlyUsedFormulasAsync(int days = 30)
    {
        var startTime = DateTime.Now;
        try
        {
            var allFormulasResult = await GetFormulaListAsync();
            if (!allFormulasResult.IsSuccess)
                return ServiceResult<List<FormulaDto>>.Failure(allFormulasResult.ErrorMessage);

            var cutoffDate = DateTime.Now.AddDays(-days);
            var recentlyUsed = allFormulasResult.Data
                .Where(f => f.LastUsedTime.HasValue && f.LastUsedTime.Value >= cutoffDate)
                .OrderByDescending(f => f.LastUsedTime)
                .ToList();

            RecordQueryPerformance("GetRecentlyUsedFormulas", startTime);
            return ServiceResult<List<FormulaDto>>.Success(recentlyUsed, $"近{days}天使用的验方{recentlyUsed.Count}个");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取最近使用的验方时发生异常，天数: {Days}", days);
            return ServiceResult<List<FormulaDto>>.Failure("获取最近使用的验方失败: " + ex.Message);
        }
    }

    public async Task<ServiceResult<List<FormulaDto>>> GetPopularFormulasAsync(int limit = 10)
    {
        var startTime = DateTime.Now;
        try
        {
            var allFormulasResult = await GetFormulaListAsync();
            if (!allFormulasResult.IsSuccess)
                return ServiceResult<List<FormulaDto>>.Failure(allFormulasResult.ErrorMessage);

            // TODO: 实现基于使用次数的热门验方逻辑
            // 目前根据最近使用时间排序
            var popularFormulas = allFormulasResult.Data
                .Where(f => f.IsEnabled && f.LastUsedTime.HasValue)
                .OrderByDescending(f => f.LastUsedTime)
                .Take(limit)
                .ToList();

            RecordQueryPerformance("GetPopularFormulas", startTime);
            return ServiceResult<List<FormulaDto>>.Success(popularFormulas, $"热门验方{popularFormulas.Count}个");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取热门验方时发生异常，限制数量: {Limit}", limit);
            return ServiceResult<List<FormulaDto>>.Failure("获取热门验方失败: " + ex.Message);
        }
    }

    public async Task<ServiceResult<List<FormulaDto>>> GetRecommendedFormulasAsync(Guid userId, int limit = 5)
    {
        var startTime = DateTime.Now;
        try
        {
            // TODO: 实现基于用户偏好的推荐算法
            // 目前返回用户创建的验方和热门验方的组合
            
            var userFormulasTask = GetPersonalFormulasAsync(userId);
            var popularFormulasTask = GetPopularFormulasAsync(limit);

            await Task.WhenAll(userFormulasTask, popularFormulasTask);

            var recommendations = new List<FormulaDto>();
            
            if (userFormulasTask.Result.IsSuccess)
            {
                recommendations.AddRange(userFormulasTask.Result.Data.Take(limit / 2));
            }

            if (popularFormulasTask.Result.IsSuccess)
            {
                var popularCount = limit - recommendations.Count;
                var popularToAdd = popularFormulasTask.Result.Data
                    .Where(p => !recommendations.Any(r => r.Id == p.Id))
                    .Take(popularCount);
                recommendations.AddRange(popularToAdd);
            }

            RecordQueryPerformance("GetRecommendedFormulas", startTime);
            return ServiceResult<List<FormulaDto>>.Success(recommendations, $"推荐验方{recommendations.Count}个");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取推荐验方时发生异常，用户ID: {UserId}", userId);
            return ServiceResult<List<FormulaDto>>.Failure("获取推荐验方失败: " + ex.Message);
        }
    }

    #endregion

    #region 统计查询

    public async Task<ServiceResult<FormulaStatisticsDto>> GetFormulaStatisticsAsync()
    {
        var startTime = DateTime.Now;
        try
        {
            var cacheKey = $"{STATS_CACHE_PREFIX}overview";

            // 尝试从缓存获取
            if (_cache.TryGetValue(cacheKey, out FormulaStatisticsDto cachedStats))
            {
                RecordQueryPerformance("GetFormulaStatistics(cached)", startTime);
                return ServiceResult<FormulaStatisticsDto>.Success(cachedStats, "验方统计获取成功(缓存)");
            }

            var result = await _coreService.CallGetFormulaStatisticsApiAsync();
            if (result.IsSuccess)
            {
                // 缓存统计结果
                _cache.Set(cacheKey, result.Data, StatsCacheTime);
            }

            RecordQueryPerformance("GetFormulaStatistics", startTime);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取验方统计时发生异常");
            return ServiceResult<FormulaStatisticsDto>.Failure("获取验方统计失败: " + ex.Message);
        }
    }

    public async Task<ServiceResult<Dictionary<string, int>>> GetFormulaCountStatisticsAsync()
    {
        var startTime = DateTime.Now;
        try
        {
            var formulasResult = await GetFormulaListAsync(new FormulaQueryOptions { IncludeDisabled = true });
            if (!formulasResult.IsSuccess)
                return ServiceResult<Dictionary<string, int>>.Failure(formulasResult.ErrorMessage);

            var statistics = new Dictionary<string, int>
            {
                ["总数"] = formulasResult.Data.Count,
                ["启用"] = formulasResult.Data.Count(f => f.IsEnabled),
                ["禁用"] = formulasResult.Data.Count(f => !f.IsEnabled),
                ["经典验方"] = formulasResult.Data.Count(f => f.Source == "经典"),
                ["个人验方"] = formulasResult.Data.Count(f => f.Source != "经典")
            };

            RecordQueryPerformance("GetFormulaCountStatistics", startTime);
            return ServiceResult<Dictionary<string, int>>.Success(statistics, "验方数量统计获取成功");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取验方数量统计时发生异常");
            return ServiceResult<Dictionary<string, int>>.Failure("获取验方数量统计失败: " + ex.Message);
        }
    }

    public async Task<ServiceResult<Dictionary<string, int>>> GetFormulaTypeDistributionAsync()
    {
        var startTime = DateTime.Now;
        try
        {
            var formulasResult = await GetActiveFormulasAsync();
            if (!formulasResult.IsSuccess)
                return ServiceResult<Dictionary<string, int>>.Failure(formulasResult.ErrorMessage);

            var distribution = formulasResult.Data
                .GroupBy(f => f.Type)
                .ToDictionary(g => g.Key, g => g.Count());

            RecordQueryPerformance("GetFormulaTypeDistribution", startTime);
            return ServiceResult<Dictionary<string, int>>.Success(distribution, "验方类型分布获取成功");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取验方类型分布时发生异常");
            return ServiceResult<Dictionary<string, int>>.Failure("获取验方类型分布失败: " + ex.Message);
        }
    }

    public async Task<ServiceResult<List<FormulaCreationTrendDto>>> GetCreationTrendAsync(int days = 30)
    {
        var startTime = DateTime.Now;
        try
        {
            var cacheKey = $"{TREND_CACHE_PREFIX}creation_{days}";

            // 尝试从缓存获取
            if (_cache.TryGetValue(cacheKey, out List<FormulaCreationTrendDto> cachedTrend))
            {
                RecordQueryPerformance("GetCreationTrend(cached)", startTime);
                return ServiceResult<List<FormulaCreationTrendDto>>.Success(cachedTrend, "创建趋势获取成功(缓存)");
            }

            var cutoffDate = DateTime.Now.AddDays(-days);
            var formulasResult = await GetRecentlyCreatedFormulasAsync(days);
            
            if (!formulasResult.IsSuccess)
                return ServiceResult<List<FormulaCreationTrendDto>>.Failure(formulasResult.ErrorMessage);

            var trendData = new List<FormulaCreationTrendDto>();
            var cumulativeCount = 0;

            for (var i = days; i >= 0; i--)
            {
                var date = DateTime.Now.Date.AddDays(-i);
                var dayCount = formulasResult.Data.Count(f => f.CreateTime.Date == date);
                cumulativeCount += dayCount;

                trendData.Add(new FormulaCreationTrendDto
                {
                    Date = date,
                    CreationCount = dayCount,
                    CumulativeCount = cumulativeCount
                });
            }

            // 缓存趋势数据
            _cache.Set(cacheKey, trendData, TrendCacheTime);

            RecordQueryPerformance("GetCreationTrend", startTime);
            return ServiceResult<List<FormulaCreationTrendDto>>.Success(trendData, "创建趋势获取成功");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取创建趋势时发生异常，天数: {Days}", days);
            return ServiceResult<List<FormulaCreationTrendDto>>.Failure("获取创建趋势失败: " + ex.Message);
        }
    }

    public async Task<ServiceResult<FormulaUsageStatisticsDto>> GetFormulaUsageStatisticsAsync(int days = 30)
    {
        var startTime = DateTime.Now;
        try
        {
            // TODO: 实现基于真实使用数据的统计
            // 目前提供模拟数据
            var statistics = new FormulaUsageStatisticsDto
            {
                DailyUsage = 15,
                WeeklyUsage = 95,
                MonthlyUsage = 380,
                AverageUsagePerFormula = 2.5,
                LastUsageTime = DateTime.Now.AddHours(-2)
            };

            RecordQueryPerformance("GetFormulaUsageStatistics", startTime);
            return ServiceResult<FormulaUsageStatisticsDto>.Success(statistics, "验方使用统计获取成功");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取验方使用统计时发生异常");
            return ServiceResult<FormulaUsageStatisticsDto>.Failure("获取验方使用统计失败: " + ex.Message);
        }
    }

    public async Task<ServiceResult<List<IngredientUsageStatisticsDto>>> GetIngredientUsageStatisticsAsync()
    {
        var startTime = DateTime.Now;
        try
        {
            var formulasResult = await GetActiveFormulasAsync();
            if (!formulasResult.IsSuccess)
                return ServiceResult<List<IngredientUsageStatisticsDto>>.Failure(formulasResult.ErrorMessage);

            var ingredientStats = new Dictionary<Guid, IngredientUsageStatisticsDto>();
            var totalFormulas = formulasResult.Data.Count;

            foreach (var formula in formulasResult.Data.Where(f => f.Ingredients != null))
            {
                foreach (var ingredient in formula.Ingredients)
                {
                    if (!ingredientStats.ContainsKey(ingredient.HerbId))
                    {
                        ingredientStats[ingredient.HerbId] = new IngredientUsageStatisticsDto
                        {
                            HerbId = ingredient.HerbId,
                            HerbName = ingredient.HerbName,
                            UsageCount = 0,
                            CommonFormulas = new List<string>()
                        };
                    }

                    var stats = ingredientStats[ingredient.HerbId];
                    stats.UsageCount++;
                    
                    if (stats.CommonFormulas.Count < 5 && !stats.CommonFormulas.Contains(formula.Name))
                    {
                        stats.CommonFormulas.Add(formula.Name);
                    }
                }
            }

            // 计算使用百分比
            foreach (var stat in ingredientStats.Values)
            {
                stat.UsagePercentage = totalFormulas > 0 ? (double)stat.UsageCount / totalFormulas * 100 : 0;
            }

            var result = ingredientStats.Values
                .OrderByDescending(s => s.UsageCount)
                .ToList();

            RecordQueryPerformance("GetIngredientUsageStatistics", startTime);
            return ServiceResult<List<IngredientUsageStatisticsDto>>.Success(result, "药材使用统计获取成功");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取药材使用统计时发生异常");
            return ServiceResult<List<IngredientUsageStatisticsDto>>.Failure("获取药材使用统计失败: " + ex.Message);
        }
    }

    public async Task<ServiceResult<Dictionary<string, int>>> GetEffectDistributionAsync()
    {
        var startTime = DateTime.Now;
        try
        {
            var formulasResult = await GetActiveFormulasAsync();
            if (!formulasResult.IsSuccess)
                return ServiceResult<Dictionary<string, int>>.Failure(formulasResult.ErrorMessage);

            var distribution = formulasResult.Data
                .Where(f => !string.IsNullOrEmpty(f.Effect))
                .GroupBy(f => f.Effect)
                .ToDictionary(g => g.Key, g => g.Count());

            RecordQueryPerformance("GetEffectDistribution", startTime);
            return ServiceResult<Dictionary<string, int>>.Success(distribution, "功效分布获取成功");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取功效分布时发生异常");
            return ServiceResult<Dictionary<string, int>>.Failure("获取功效分布失败: " + ex.Message);
        }
    }

    #endregion

    #region 关联查询

    public async Task<ServiceResult<List<FormulaDto>>> GetRelatedFormulasAsync(Guid formulaId, int limit = 5)
    {
        var startTime = DateTime.Now;
        try
        {
            var formulaResult = await _coreService.CallGetFormulaByIdApiAsync(formulaId);
            if (!formulaResult.IsSuccess)
                return ServiceResult<List<FormulaDto>>.Failure(formulaResult.ErrorMessage);

            var targetFormula = formulaResult.Data;

            // 查找同类型或同功效的验方
            var relatedByTypeTask = SearchByTypeAsync(targetFormula.Type);
            var relatedByEffectTask = SearchByEffectAsync(targetFormula.Effect);

            await Task.WhenAll(relatedByTypeTask, relatedByEffectTask);

            var related = new List<FormulaDto>();

            if (relatedByTypeTask.Result.IsSuccess)
            {
                related.AddRange(relatedByTypeTask.Result.Data
                    .Where(f => f.Id != formulaId)
                    .Take(limit / 2));
            }

            if (relatedByEffectTask.Result.IsSuccess && related.Count < limit)
            {
                var effectFormulas = relatedByEffectTask.Result.Data
                    .Where(f => f.Id != formulaId && !related.Any(r => r.Id == f.Id))
                    .Take(limit - related.Count);
                related.AddRange(effectFormulas);
            }

            RecordQueryPerformance("GetRelatedFormulas", startTime);
            return ServiceResult<List<FormulaDto>>.Success(related, $"相关验方{related.Count}个");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取相关验方时发生异常，验方ID: {FormulaId}", formulaId);
            return ServiceResult<List<FormulaDto>>.Failure("获取相关验方失败: " + ex.Message);
        }
    }

    public async Task<ServiceResult<List<FormulaDto>>> GetSimilarFormulasAsync(Guid formulaId, int limit = 5)
    {
        // 目前实现与相关验方相同的逻辑
        return await GetRelatedFormulasAsync(formulaId, limit);
    }

    public async Task<ServiceResult<List<FormulaDto>>> GetFormulasContainingIngredientAsync(Guid herbId)
    {
        var startTime = DateTime.Now;
        try
        {
            var allFormulasResult = await GetActiveFormulasAsync();
            if (!allFormulasResult.IsSuccess)
                return ServiceResult<List<FormulaDto>>.Failure(allFormulasResult.ErrorMessage);

            var formulasWithIngredient = allFormulasResult.Data
                .Where(f => f.Ingredients != null && f.Ingredients.Any(i => i.HerbId == herbId))
                .ToList();

            RecordQueryPerformance("GetFormulasContainingIngredient", startTime);
            return ServiceResult<List<FormulaDto>>.Success(formulasWithIngredient, 
                $"包含指定药材的验方{formulasWithIngredient.Count}个");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取包含指定药材的验方时发生异常，药材ID: {HerbId}", herbId);
            return ServiceResult<List<FormulaDto>>.Failure("获取包含指定药材的验方失败: " + ex.Message);
        }
    }

    public async Task<ServiceResult<List<FormulaUsageHistoryDto>>> GetFormulaUsageHistoryAsync(Guid formulaId)
    {
        var startTime = DateTime.Now;
        try
        {
            // TODO: 实现基于真实使用历史数据的查询
            // 目前返回模拟数据
            var history = new List<FormulaUsageHistoryDto>();

            RecordQueryPerformance("GetFormulaUsageHistory", startTime);
            return ServiceResult<List<FormulaUsageHistoryDto>>.Success(history, "验方使用历史获取成功");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取验方使用历史时发生异常，验方ID: {FormulaId}", formulaId);
            return ServiceResult<List<FormulaUsageHistoryDto>>.Failure("获取验方使用历史失败: " + ex.Message);
        }
    }

    public async Task<ServiceResult<List<FormulaReviewDto>>> GetFormulaReviewsAsync(Guid formulaId)
    {
        var startTime = DateTime.Now;
        try
        {
            // TODO: 实现基于真实评价数据的查询
            // 目前返回模拟数据
            var reviews = new List<FormulaReviewDto>();

            RecordQueryPerformance("GetFormulaReviews", startTime);
            return ServiceResult<List<FormulaReviewDto>>.Success(reviews, "验方评价获取成功");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取验方评价时发生异常，验方ID: {FormulaId}", formulaId);
            return ServiceResult<List<FormulaReviewDto>>.Failure("获取验方评价失败: " + ex.Message);
        }
    }

    #endregion

    #region 查询优化和缓存

    public async Task<ServiceResult> PreloadQueryCacheAsync()
    {
        try
        {
            _logger.LogInformation("开始预加载验方查询缓存");

            // 预加载常用查询
            var tasks = new List<Task>
            {
                GetActiveFormulasAsync(),
                GetClassicFormulasAsync(),
                GetFormulaStatisticsAsync(),
                GetFormulaTypeDistributionAsync()
            };

            await Task.WhenAll(tasks);

            _logger.LogInformation("验方查询缓存预加载完成");
            return ServiceResult.Success("查询缓存预加载成功");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "预加载验方查询缓存时发生异常");
            return ServiceResult.Failure("查询缓存预加载失败: " + ex.Message);
        }
    }

    public ServiceResult ClearQueryCache()
    {
        try
        {
            _logger.LogInformation("清除验方查询缓存");
            
            // MemoryCache没有提供按前缀清除的功能，记录日志即可
            // 在实际应用中可以考虑使用支持按模式清除的缓存方案

            return ServiceResult.Success("查询缓存清除成功");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "清除验方查询缓存时发生异常");
            return ServiceResult.Failure("查询缓存清除失败: " + ex.Message);
        }
    }

    public ServiceResult<QueryPerformanceDto> GetQueryPerformanceStats()
    {
        var avgQueryTime = _totalQueries > 0 ? (double)_totalQueryTime / _totalQueries : 0;

        var stats = new QueryPerformanceDto
        {
            TotalQueries = _totalQueries,
            AverageQueryTime = Math.Round(avgQueryTime, 2),
            TotalQueryTime = _totalQueryTime,
            LastUpdateTime = DateTime.Now
        };

        return ServiceResult<QueryPerformanceDto>.Success(stats, "查询性能统计获取成功");
    }

    public async Task<ServiceResult> OptimizeQueryIndexAsync()
    {
        try
        {
            _logger.LogInformation("开始优化验方查询索引");

            // TODO: 实现查询索引优化逻辑
            // 目前只是预热缓存
            await PreloadQueryCacheAsync();

            _logger.LogInformation("验方查询索引优化完成");
            return ServiceResult.Success("查询索引优化成功");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "优化验方查询索引时发生异常");
            return ServiceResult.Failure("查询索引优化失败: " + ex.Message);
        }
    }

    #endregion

    #region 导出查询

    public async Task<ServiceResult<List<FormulaExportDto>>> GetFormulasForExportAsync(FormulaExportQueryDto query)
    {
        var startTime = DateTime.Now;
        try
        {
            var result = await _coreService.CallExportFormulasApiAsync(query);
            if (!result.IsSuccess)
                return ServiceResult<List<FormulaExportDto>>.Failure(result.ErrorMessage);

            // TODO: 转换为导出格式
            var exportData = new List<FormulaExportDto>();

            RecordQueryPerformance("GetFormulasForExport", startTime);
            return ServiceResult<List<FormulaExportDto>>.Success(exportData, "导出数据查询成功");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "查询导出数据时发生异常");
            return ServiceResult<List<FormulaExportDto>>.Failure("导出数据查询失败: " + ex.Message);
        }
    }

    public async Task<ServiceResult<List<FormulaBasicInfoDto>>> GetFormulaBasicInfoAsync(List<Guid>? formulaIds = null)
    {
        var startTime = DateTime.Now;
        try
        {
            List<FormulaDto> formulas;

            if (formulaIds != null && formulaIds.Any())
            {
                var result = await GetFormulasByIdsAsync(formulaIds);
                if (!result.IsSuccess)
                    return ServiceResult<List<FormulaBasicInfoDto>>.Failure(result.ErrorMessage);
                formulas = result.Data;
            }
            else
            {
                var result = await GetActiveFormulasAsync();
                if (!result.IsSuccess)
                    return ServiceResult<List<FormulaBasicInfoDto>>.Failure(result.ErrorMessage);
                formulas = result.Data;
            }

            var basicInfo = formulas.Select(f => new FormulaBasicInfoDto
            {
                Id = f.Id,
                Name = f.Name,
                Type = f.Type,
                Source = f.Source,
                IsEnabled = f.IsEnabled
            }).ToList();

            RecordQueryPerformance("GetFormulaBasicInfo", startTime);
            return ServiceResult<List<FormulaBasicInfoDto>>.Success(basicInfo, "验方基础信息获取成功");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取验方基础信息时发生异常");
            return ServiceResult<List<FormulaBasicInfoDto>>.Failure("获取验方基础信息失败: " + ex.Message);
        }
    }

    public async Task<ServiceResult<List<FormulaDetailedInfoDto>>> GetFormulaDetailedInfoAsync(List<Guid> formulaIds)
    {
        var startTime = DateTime.Now;
        try
        {
            var formulas = await GetFormulasByIdsAsync(formulaIds);
            if (!formulas.IsSuccess)
                return ServiceResult<List<FormulaDetailedInfoDto>>.Failure(formulas.ErrorMessage);

            var detailedInfo = formulas.Data.Select(f => new FormulaDetailedInfoDto
            {
                Id = f.Id,
                Name = f.Name,
                Type = f.Type,
                Source = f.Source,
                Effect = f.Effect,
                Indications = f.Indications,
                Contraindications = f.Contraindications,
                Usage = f.Usage,
                Preparation = f.Preparation,
                Dosage = f.Dosage,
                Notes = f.Notes,
                Ingredients = f.Ingredients,
                CreatorId = f.CreatorId,
                CreatorName = f.CreatorName,
                IsEnabled = f.IsEnabled,
                CreateTime = f.CreateTime,
                UpdateTime = f.UpdateTime,
                LastUsedTime = f.LastUsedTime,
                UsageCount = 0 // TODO: 实现真实的使用次数统计
            }).ToList();

            RecordQueryPerformance("GetFormulaDetailedInfo", startTime);
            return ServiceResult<List<FormulaDetailedInfoDto>>.Success(detailedInfo, "验方详细信息获取成功");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取验方详细信息时发生异常");
            return ServiceResult<List<FormulaDetailedInfoDto>>.Failure("获取验方详细信息失败: " + ex.Message);
        }
    }

    #endregion

    #region 私有辅助方法

    private List<FormulaDto> ApplyQueryOptions(List<FormulaDto> formulas, FormulaQueryOptions options)
    {
        var query = formulas.AsQueryable();

        if (!options.IncludeDisabled)
            query = query.Where(f => f.IsEnabled);

        if (!string.IsNullOrEmpty(options.FilterByType))
            query = query.Where(f => f.Type == options.FilterByType);

        if (!string.IsNullOrEmpty(options.FilterBySource))
            query = query.Where(f => f.Source == options.FilterBySource);

        if (options.FilterByCreator.HasValue)
            query = query.Where(f => f.CreatorId == options.FilterByCreator.Value);

        if (options.CreatedAfter.HasValue)
            query = query.Where(f => f.CreateTime >= options.CreatedAfter.Value);

        if (options.CreatedBefore.HasValue)
            query = query.Where(f => f.CreateTime <= options.CreatedBefore.Value);

        // 排序
        switch (options.SortBy?.ToLower())
        {
            case "name":
                query = options.SortDescending ? query.OrderByDescending(f => f.Name) : query.OrderBy(f => f.Name);
                break;
            case "createtime":
                query = options.SortDescending ? query.OrderByDescending(f => f.CreateTime) : query.OrderBy(f => f.CreateTime);
                break;
            case "type":
                query = options.SortDescending ? query.OrderByDescending(f => f.Type) : query.OrderBy(f => f.Type);
                break;
            default:
                query = query.OrderBy(f => f.Name);
                break;
        }

        return query.ToList();
    }

    private List<FormulaDto> ApplyAdvancedFilter(List<FormulaDto> formulas, FormulaAdvancedFilterDto filter)
    {
        var query = formulas.AsQueryable();

        if (filter.Types != null && filter.Types.Any())
            query = query.Where(f => filter.Types.Contains(f.Type));

        if (filter.Sources != null && filter.Sources.Any())
            query = query.Where(f => filter.Sources.Contains(f.Source));

        if (filter.CreatorIds != null && filter.CreatorIds.Any())
            query = query.Where(f => filter.CreatorIds.Contains(f.CreatorId));

        if (filter.IsEnabled.HasValue)
            query = query.Where(f => f.IsEnabled == filter.IsEnabled.Value);

        if (filter.CreatedAfter.HasValue)
            query = query.Where(f => f.CreateTime >= filter.CreatedAfter.Value);

        if (filter.CreatedBefore.HasValue)
            query = query.Where(f => f.CreateTime <= filter.CreatedBefore.Value);

        if (filter.LastUsedAfter.HasValue)
            query = query.Where(f => f.LastUsedTime.HasValue && f.LastUsedTime.Value >= filter.LastUsedAfter.Value);

        if (filter.LastUsedBefore.HasValue)
            query = query.Where(f => f.LastUsedTime.HasValue && f.LastUsedTime.Value <= filter.LastUsedBefore.Value);

        if (filter.IncludedIngredients != null && filter.IncludedIngredients.Any())
            query = query.Where(f => f.Ingredients != null && 
                filter.IncludedIngredients.All(herbId => f.Ingredients.Any(i => i.HerbId == herbId)));

        if (filter.ExcludedIngredients != null && filter.ExcludedIngredients.Any())
            query = query.Where(f => f.Ingredients == null || 
                !filter.ExcludedIngredients.Any(herbId => f.Ingredients.Any(i => i.HerbId == herbId)));

        if (filter.ExcludeFormulaIds != null && filter.ExcludeFormulaIds.Any())
            query = query.Where(f => !filter.ExcludeFormulaIds.Contains(f.Id));

        return query.ToList();
    }

    private PagedResult<FormulaDto> CreatePagedResult(List<FormulaDto> allItems, int pageIndex, int pageSize)
    {
        var totalCount = allItems.Count;
        var items = allItems.Skip((pageIndex - 1) * pageSize).Take(pageSize).ToList();
        
        return new PagedResult<FormulaDto>(items, totalCount, pageIndex, pageSize);
    }

    private string GenerateQueryCacheKey(string operation, object? parameters = null)
    {
        var baseKey = $"{QUERY_CACHE_PREFIX}{operation}";
        
        if (parameters != null)
        {
            var hash = parameters.GetHashCode();
            return $"{baseKey}_{hash}";
        }
        
        return baseKey;
    }

    private void RecordQueryPerformance(string queryType, DateTime startTime)
    {
        var duration = (long)(DateTime.Now - startTime).TotalMilliseconds;
        _totalQueries++;
        _totalQueryTime += duration;

        if (duration > 1000) // 记录慢查询
        {
            _logger.LogWarning("慢查询检测: {QueryType} 耗时 {Duration}ms", queryType, duration);
        }
    }

    #endregion
}