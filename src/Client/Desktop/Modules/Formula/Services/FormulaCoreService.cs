using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using LYBT.Desktop.Formula.Interfaces;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Formulas;
using LYBT.Shared.Interfaces.Api;

namespace LYBT.Desktop.Formula.Services;

/// <summary>
/// 验方核心服务 - UltraThink三层架构核心层
/// 职责：API通信、数据验证、缓存管理、基础操作
/// </summary>
public class FormulaCoreService : IFormulaCoreService
{
    private readonly IFormulaApi _formulaApi;
    private readonly IMemoryCache _cache;
    private readonly ILogger<FormulaCoreService> _logger;

    // 缓存键常量
    private const string FORMULA_CACHE_KEY = "formula_";
    private const string FORMULAS_LIST_CACHE_KEY = "formulas_list_";
    private const string FORMULA_STATS_CACHE_KEY = "formula_stats";

    // 缓存时间
    private static readonly TimeSpan DefaultCacheTime = TimeSpan.FromMinutes(10);
    private static readonly TimeSpan StatsCacheTime = TimeSpan.FromMinutes(5);

    // 缓存统计
    private int _cacheHits = 0;
    private int _cacheMisses = 0;

    // 验证正则表达式
    [GeneratedRegex(@"^[\u4e00-\u9fa5\w\s]{2,50}$")]
    private static partial Regex FormulaNameRegex();

    [GeneratedRegex(@"^[\u4e00-\u9fa5\w\s]{1,20}$")]
    private static partial Regex FormulaTypeRegex();

    public FormulaCoreService(
        IFormulaApi formulaApi,
        IMemoryCache cache,
        ILogger<FormulaCoreService> logger)
    {
        _formulaApi = formulaApi ?? throw new ArgumentNullException(nameof(formulaApi));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    #region API通信层

    public async Task<ServiceResult<FormulaDto>> CallCreateFormulaApiAsync(FormulaCreateDto createDto)
    {
        try
        {
            // TODO: API通信应该移至公共模块 - 统一API客户端管理
            var apiResponse = await _formulaApi.CreateFormulaAsync(createDto);

            if (apiResponse.IsSuccessStatusCode && apiResponse.Content != null)
            {
                _logger.LogInformation("验方创建成功: {FormulaName}, ID: {FormulaId}", 
                    apiResponse.Content.Name, apiResponse.Content.Id);

                // 清除相关缓存
                ClearFormulaCache();

                await LogFormulaOperationAsync("Create", apiResponse.Content.Id, createDto.CreatorId, createDto);
                
                return ServiceResult<FormulaDto>.Success(apiResponse.Content, "验方创建成功");
            }

            _logger.LogWarning("验方创建API调用失败: {Error}", apiResponse.Error?.Content);
            return ServiceResult<FormulaDto>.Failure("验方创建失败: " + apiResponse.Error?.Content);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "调用验方创建API时发生异常");
            return ServiceResult<FormulaDto>.Failure("验方创建异常: " + ex.Message);
        }
    }

    public async Task<ServiceResult<FormulaDto>> CallUpdateFormulaApiAsync(Guid id, FormulaUpdateDto updateDto)
    {
        try
        {
            // TODO: API通信应该移至公共模块 - 统一API客户端管理
            var apiResponse = await _formulaApi.UpdateFormulaAsync(id, updateDto);

            if (apiResponse.IsSuccessStatusCode && apiResponse.Content != null)
            {
                _logger.LogInformation("验方更新成功: {FormulaId}", id);

                // 清除相关缓存
                ClearFormulaCache(id);

                await LogFormulaOperationAsync("Update", id, updateDto.UpdaterId, updateDto);
                
                return ServiceResult<FormulaDto>.Success(apiResponse.Content, "验方更新成功");
            }

            _logger.LogWarning("验方更新API调用失败: {Error}", apiResponse.Error?.Content);
            return ServiceResult<FormulaDto>.Failure("验方更新失败: " + apiResponse.Error?.Content);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "调用验方更新API时发生异常, ID: {FormulaId}", id);
            return ServiceResult<FormulaDto>.Failure("验方更新异常: " + ex.Message);
        }
    }

    public async Task<ServiceResult<bool>> CallDeleteFormulaApiAsync(Guid id)
    {
        try
        {
            // TODO: API通信应该移至公共模块 - 统一API客户端管理
            var apiResponse = await _formulaApi.DeleteFormulaAsync(id);

            if (apiResponse.IsSuccessStatusCode)
            {
                _logger.LogInformation("验方删除成功: {FormulaId}", id);

                // 清除相关缓存
                ClearFormulaCache(id);

                await LogFormulaOperationAsync("Delete", id);
                
                return ServiceResult<bool>.Success(true, "验方删除成功");
            }

            _logger.LogWarning("验方删除API调用失败: {Error}", apiResponse.Error?.Content);
            return ServiceResult<bool>.Failure("验方删除失败: " + apiResponse.Error?.Content);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "调用验方删除API时发生异常, ID: {FormulaId}", id);
            return ServiceResult<bool>.Failure("验方删除异常: " + ex.Message);
        }
    }

    public async Task<ServiceResult<FormulaDto>> CallGetFormulaByIdApiAsync(Guid id)
    {
        try
        {
            // 先检查缓存
            var cachedResult = await GetCachedFormulaAsync(id);
            if (cachedResult.IsSuccess && cachedResult.Data != null)
            {
                _cacheHits++;
                return ServiceResult<FormulaDto>.Success(cachedResult.Data, "验方获取成功(缓存)");
            }

            _cacheMisses++;

            // TODO: API通信应该移至公共模块 - 统一API客户端管理
            var apiResponse = await _formulaApi.GetFormulaByIdAsync(id);

            if (apiResponse.IsSuccessStatusCode && apiResponse.Content != null)
            {
                // 缓存结果
                await SetFormulaCacheAsync(id, apiResponse.Content);

                await LogFormulaOperationAsync("Get", id);
                
                return ServiceResult<FormulaDto>.Success(apiResponse.Content, "验方获取成功");
            }

            _logger.LogWarning("验方获取API调用失败: {Error}", apiResponse.Error?.Content);
            return ServiceResult<FormulaDto>.Failure("验方获取失败: " + apiResponse.Error?.Content);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "调用验方获取API时发生异常, ID: {FormulaId}", id);
            return ServiceResult<FormulaDto>.Failure("验方获取异常: " + ex.Message);
        }
    }

    public async Task<ServiceResult<List<FormulaDto>>> CallGetFormulasApiAsync()
    {
        try
        {
            const string cacheKey = FORMULAS_LIST_CACHE_KEY + "all";
            
            // 先检查缓存
            var cachedResult = await GetCachedFormulasAsync(cacheKey);
            if (cachedResult.IsSuccess && cachedResult.Data != null)
            {
                _cacheHits++;
                return ServiceResult<List<FormulaDto>>.Success(cachedResult.Data, "验方列表获取成功(缓存)");
            }

            _cacheMisses++;

            // TODO: API通信应该移至公共模块 - 统一API客户端管理
            var apiResponse = await _formulaApi.GetFormulasAsync();

            if (apiResponse.IsSuccessStatusCode && apiResponse.Content != null)
            {
                // 缓存结果
                await SetFormulasCacheAsync(cacheKey, apiResponse.Content);

                await LogFormulaOperationAsync("GetList", Guid.Empty);
                
                return ServiceResult<List<FormulaDto>>.Success(apiResponse.Content, "验方列表获取成功");
            }

            _logger.LogWarning("验方列表获取API调用失败: {Error}", apiResponse.Error?.Content);
            return ServiceResult<List<FormulaDto>>.Failure("验方列表获取失败: " + apiResponse.Error?.Content);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "调用验方列表获取API时发生异常");
            return ServiceResult<List<FormulaDto>>.Failure("验方列表获取异常: " + ex.Message);
        }
    }

    public async Task<ServiceResult<PagedResult<FormulaDto>>> CallGetPagedFormulasApiAsync(FormulaPagedQueryDto query)
    {
        try
        {
            var cacheKey = GenerateFormulasListCacheKey("paged", query);
            
            // 先检查缓存
            var cachedResult = await GetCachedFormulasAsync(cacheKey);
            if (cachedResult.IsSuccess && cachedResult.Data != null)
            {
                _cacheHits++;
                var pagedResult = new PagedResult<FormulaDto>(cachedResult.Data, cachedResult.Data.Count, query.PageIndex, query.PageSize);
                return ServiceResult<PagedResult<FormulaDto>>.Success(pagedResult, "分页验方获取成功(缓存)");
            }

            _cacheMisses++;

            // TODO: API通信应该移至公共模块 - 统一API客户端管理
            var apiResponse = await _formulaApi.GetPagedFormulasAsync(query);

            if (apiResponse.IsSuccessStatusCode && apiResponse.Content != null)
            {
                // 缓存结果
                await SetFormulasCacheAsync(cacheKey, apiResponse.Content.Items);

                await LogFormulaOperationAsync("GetPaged", Guid.Empty, parameters: query);
                
                return ServiceResult<PagedResult<FormulaDto>>.Success(apiResponse.Content, "分页验方获取成功");
            }

            _logger.LogWarning("分页验方获取API调用失败: {Error}", apiResponse.Error?.Content);
            return ServiceResult<PagedResult<FormulaDto>>.Failure("分页验方获取失败: " + apiResponse.Error?.Content);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "调用分页验方获取API时发生异常");
            return ServiceResult<PagedResult<FormulaDto>>.Failure("分页验方获取异常: " + ex.Message);
        }
    }

    public async Task<ServiceResult<List<FormulaDto>>> CallSearchFormulasApiAsync(FormulaSearchDto searchDto)
    {
        try
        {
            var cacheKey = GenerateFormulasListCacheKey("search", searchDto);
            
            // 先检查缓存
            var cachedResult = await GetCachedFormulasAsync(cacheKey);
            if (cachedResult.IsSuccess && cachedResult.Data != null)
            {
                _cacheHits++;
                return ServiceResult<List<FormulaDto>>.Success(cachedResult.Data, "验方搜索成功(缓存)");
            }

            _cacheMisses++;

            // TODO: API通信应该移至公共模块 - 统一API客户端管理
            var apiResponse = await _formulaApi.SearchFormulasAsync(searchDto);

            if (apiResponse.IsSuccessStatusCode && apiResponse.Content != null)
            {
                // 缓存结果
                await SetFormulasCacheAsync(cacheKey, apiResponse.Content);

                await LogFormulaOperationAsync("Search", Guid.Empty, parameters: searchDto);
                
                return ServiceResult<List<FormulaDto>>.Success(apiResponse.Content, "验方搜索成功");
            }

            _logger.LogWarning("验方搜索API调用失败: {Error}", apiResponse.Error?.Content);
            return ServiceResult<List<FormulaDto>>.Failure("验方搜索失败: " + apiResponse.Error?.Content);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "调用验方搜索API时发生异常");
            return ServiceResult<List<FormulaDto>>.Failure("验方搜索异常: " + ex.Message);
        }
    }

    public async Task<ServiceResult<bool>> CallUpdateFormulaStatusApiAsync(Guid id, bool isEnabled)
    {
        try
        {
            // TODO: API通信应该移至公共模块 - 统一API客户端管理
            var apiResponse = await _formulaApi.UpdateFormulaStatusAsync(id, isEnabled);

            if (apiResponse.IsSuccessStatusCode)
            {
                _logger.LogInformation("验方状态更新成功: {FormulaId}, 状态: {Status}", id, isEnabled ? "启用" : "禁用");

                // 清除相关缓存
                ClearFormulaCache(id);

                await LogFormulaOperationAsync(isEnabled ? "Enable" : "Disable", id);
                
                return ServiceResult<bool>.Success(true, "验方状态更新成功");
            }

            _logger.LogWarning("验方状态更新API调用失败: {Error}", apiResponse.Error?.Content);
            return ServiceResult<bool>.Failure("验方状态更新失败: " + apiResponse.Error?.Content);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "调用验方状态更新API时发生异常, ID: {FormulaId}", id);
            return ServiceResult<bool>.Failure("验方状态更新异常: " + ex.Message);
        }
    }

    public async Task<ServiceResult<FormulaBatchOperationResultDto>> CallBatchOperateFormulasApiAsync(FormulaBatchOperationDto operationDto)
    {
        try
        {
            // TODO: API通信应该移至公共模块 - 统一API客户端管理
            var apiResponse = await _formulaApi.BatchOperateFormulasAsync(operationDto);

            if (apiResponse.IsSuccessStatusCode && apiResponse.Content != null)
            {
                _logger.LogInformation("验方批量操作成功: {Operation}, 影响数量: {Count}", 
                    operationDto.Operation, apiResponse.Content.SuccessCount);

                // 清除所有相关缓存
                ClearAllFormulaCache();

                await LogFormulaOperationAsync($"Batch{operationDto.Operation}", Guid.Empty, 
                    operationDto.OperatorId, operationDto);
                
                return ServiceResult<FormulaBatchOperationResultDto>.Success(apiResponse.Content, "批量操作成功");
            }

            _logger.LogWarning("验方批量操作API调用失败: {Error}", apiResponse.Error?.Content);
            return ServiceResult<FormulaBatchOperationResultDto>.Failure("批量操作失败: " + apiResponse.Error?.Content);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "调用验方批量操作API时发生异常");
            return ServiceResult<FormulaBatchOperationResultDto>.Failure("批量操作异常: " + ex.Message);
        }
    }

    public async Task<ServiceResult<FormulaStatisticsDto>> CallGetFormulaStatisticsApiAsync()
    {
        try
        {
            const string cacheKey = FORMULA_STATS_CACHE_KEY;
            
            // 先检查缓存
            if (_cache.TryGetValue(cacheKey, out FormulaStatisticsDto cachedStats))
            {
                _cacheHits++;
                return ServiceResult<FormulaStatisticsDto>.Success(cachedStats, "验方统计获取成功(缓存)");
            }

            _cacheMisses++;

            // TODO: API通信应该移至公共模块 - 统一API客户端管理
            var apiResponse = await _formulaApi.GetFormulaStatisticsAsync();

            if (apiResponse.IsSuccessStatusCode && apiResponse.Content != null)
            {
                // 缓存统计结果
                _cache.Set(cacheKey, apiResponse.Content, StatsCacheTime);

                await LogFormulaOperationAsync("GetStatistics", Guid.Empty);
                
                return ServiceResult<FormulaStatisticsDto>.Success(apiResponse.Content, "验方统计获取成功");
            }

            _logger.LogWarning("验方统计获取API调用失败: {Error}", apiResponse.Error?.Content);
            return ServiceResult<FormulaStatisticsDto>.Failure("验方统计获取失败: " + apiResponse.Error?.Content);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "调用验方统计获取API时发生异常");
            return ServiceResult<FormulaStatisticsDto>.Failure("验方统计获取异常: " + ex.Message);
        }
    }

    public async Task<ServiceResult<FormulaImportResultDto>> CallImportFormulasApiAsync(FormulaImportDto importDto)
    {
        try
        {
            // TODO: API通信应该移至公共模块 - 统一API客户端管理
            var apiResponse = await _formulaApi.ImportFormulasAsync(importDto);

            if (apiResponse.IsSuccessStatusCode && apiResponse.Content != null)
            {
                _logger.LogInformation("验方导入成功: 成功 {SuccessCount}, 失败 {FailureCount}", 
                    apiResponse.Content.SuccessCount, apiResponse.Content.FailureCount);

                // 清除所有相关缓存
                ClearAllFormulaCache();

                await LogFormulaOperationAsync("Import", Guid.Empty, parameters: importDto);
                
                return ServiceResult<FormulaImportResultDto>.Success(apiResponse.Content, "验方导入完成");
            }

            _logger.LogWarning("验方导入API调用失败: {Error}", apiResponse.Error?.Content);
            return ServiceResult<FormulaImportResultDto>.Failure("验方导入失败: " + apiResponse.Error?.Content);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "调用验方导入API时发生异常");
            return ServiceResult<FormulaImportResultDto>.Failure("验方导入异常: " + ex.Message);
        }
    }

    public async Task<ServiceResult<FormulaExportResultDto>> CallExportFormulasApiAsync(FormulaExportQueryDto exportQuery)
    {
        try
        {
            // TODO: API通信应该移至公共模块 - 统一API客户端管理
            var apiResponse = await _formulaApi.ExportFormulasAsync(exportQuery);

            if (apiResponse.IsSuccessStatusCode && apiResponse.Content != null)
            {
                _logger.LogInformation("验方导出成功: 数量 {Count}", apiResponse.Content.ExportedCount);

                await LogFormulaOperationAsync("Export", Guid.Empty, parameters: exportQuery);
                
                return ServiceResult<FormulaExportResultDto>.Success(apiResponse.Content, "验方导出成功");
            }

            _logger.LogWarning("验方导出API调用失败: {Error}", apiResponse.Error?.Content);
            return ServiceResult<FormulaExportResultDto>.Failure("验方导出失败: " + apiResponse.Error?.Content);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "调用验方导出API时发生异常");
            return ServiceResult<FormulaExportResultDto>.Failure("验方导出异常: " + ex.Message);
        }
    }

    #endregion

    #region 数据验证层

    public ServiceResult ValidateFormulaCreateData(FormulaCreateDto createDto)
    {
        if (createDto == null)
            return ServiceResult.Failure("创建数据不能为空");

        if (string.IsNullOrWhiteSpace(createDto.Name))
            return ServiceResult.Failure("验方名称不能为空");

        if (!FormulaNameRegex().IsMatch(createDto.Name))
            return ServiceResult.Failure("验方名称格式不正确（2-50个中文、数字或字母）");

        if (string.IsNullOrWhiteSpace(createDto.Type))
            return ServiceResult.Failure("验方类型不能为空");

        if (!FormulaTypeRegex().IsMatch(createDto.Type))
            return ServiceResult.Failure("验方类型格式不正确");

        if (createDto.CreatorId == Guid.Empty)
            return ServiceResult.Failure("创建者ID不能为空");

        if (createDto.Ingredients == null || !createDto.Ingredients.Any())
            return ServiceResult.Failure("验方必须包含至少一味药材");

        // 验证药材列表
        var ingredientValidation = ValidateFormulaIngredients(createDto.Ingredients);
        if (!ingredientValidation.IsSuccess)
            return ingredientValidation;

        return ServiceResult.Success("验证通过");
    }

    public ServiceResult ValidateFormulaUpdateData(FormulaUpdateDto updateDto)
    {
        if (updateDto == null)
            return ServiceResult.Failure("更新数据不能为空");

        if (string.IsNullOrWhiteSpace(updateDto.Name))
            return ServiceResult.Failure("验方名称不能为空");

        if (!FormulaNameRegex().IsMatch(updateDto.Name))
            return ServiceResult.Failure("验方名称格式不正确（2-50个中文、数字或字母）");

        if (!string.IsNullOrWhiteSpace(updateDto.Type) && !FormulaTypeRegex().IsMatch(updateDto.Type))
            return ServiceResult.Failure("验方类型格式不正确");

        if (updateDto.UpdaterId == Guid.Empty)
            return ServiceResult.Failure("更新者ID不能为空");

        // 验证药材列表（如果提供）
        if (updateDto.Ingredients != null && updateDto.Ingredients.Any())
        {
            var ingredientValidation = ValidateFormulaIngredients(updateDto.Ingredients);
            if (!ingredientValidation.IsSuccess)
                return ingredientValidation;
        }

        return ServiceResult.Success("验证通过");
    }

    public ServiceResult ValidateFormulaId(Guid formulaId)
    {
        if (formulaId == Guid.Empty)
            return ServiceResult.Failure("验方ID不能为空");

        return ServiceResult.Success("验证通过");
    }

    public ServiceResult ValidateFormulaName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return ServiceResult.Failure("验方名称不能为空");

        if (!FormulaNameRegex().IsMatch(name))
            return ServiceResult.Failure("验方名称格式不正确（2-50个中文、数字或字母）");

        return ServiceResult.Success("验证通过");
    }

    public ServiceResult ValidateFormulaType(string type)
    {
        if (string.IsNullOrWhiteSpace(type))
            return ServiceResult.Failure("验方类型不能为空");

        if (!FormulaTypeRegex().IsMatch(type))
            return ServiceResult.Failure("验方类型格式不正确");

        return ServiceResult.Success("验证通过");
    }

    public ServiceResult ValidateFormulaIngredients(List<FormulaIngredientDto> ingredients)
    {
        if (ingredients == null || !ingredients.Any())
            return ServiceResult.Failure("药材列表不能为空");

        if (ingredients.Count > 50)
            return ServiceResult.Failure("药材数量不能超过50味");

        for (int i = 0; i < ingredients.Count; i++)
        {
            var ingredient = ingredients[i];
            
            if (ingredient.HerbId == Guid.Empty)
                return ServiceResult.Failure($"第{i + 1}味药材ID不能为空");

            if (string.IsNullOrWhiteSpace(ingredient.HerbName))
                return ServiceResult.Failure($"第{i + 1}味药材名称不能为空");

            if (ingredient.Dosage <= 0)
                return ServiceResult.Failure($"第{i + 1}味药材({ingredient.HerbName})剂量必须大于0");

            if (ingredient.Dosage > 1000)
                return ServiceResult.Failure($"第{i + 1}味药材({ingredient.HerbName})剂量不能超过1000克");
        }

        // 检查是否有重复药材
        var duplicateHerbs = ingredients.GroupBy(x => x.HerbId).Where(g => g.Count() > 1).ToList();
        if (duplicateHerbs.Any())
        {
            var herbNames = string.Join("、", duplicateHerbs.Select(g => g.First().HerbName));
            return ServiceResult.Failure($"存在重复药材：{herbNames}");
        }

        return ServiceResult.Success("验证通过");
    }

    public ServiceResult ValidateFormulaCompatibility(FormulaDto formula)
    {
        if (formula == null)
            return ServiceResult.Failure("验方不能为空");

        if (formula.Ingredients == null || !formula.Ingredients.Any())
            return ServiceResult.Failure("验方药材列表为空，无法检查配伍");

        // TODO: 实现配伍禁忌检查逻辑
        // 这里应该实现具体的中药配伍禁忌检查逻辑
        // 目前返回基础验证通过
        
        return ServiceResult.Success("配伍检查通过");
    }

    public ServiceResult ValidateBatchOperationData(List<Guid> formulaIds, string operation)
    {
        if (formulaIds == null || !formulaIds.Any())
            return ServiceResult.Failure("验方ID列表不能为空");

        if (formulaIds.Count > 100)
            return ServiceResult.Failure("批量操作数量不能超过100个");

        if (formulaIds.Any(id => id == Guid.Empty))
            return ServiceResult.Failure("存在无效的验方ID");

        if (string.IsNullOrWhiteSpace(operation))
            return ServiceResult.Failure("操作类型不能为空");

        var validOperations = new[] { "enable", "disable", "delete", "transfer", "clone" };
        if (!validOperations.Contains(operation.ToLower()))
            return ServiceResult.Failure($"不支持的操作类型：{operation}");

        return ServiceResult.Success("验证通过");
    }

    public ServiceResult ValidateSearchParameters(FormulaSearchDto searchDto)
    {
        if (searchDto == null)
            return ServiceResult.Failure("搜索参数不能为空");

        if (searchDto.PageIndex < 1)
            searchDto.PageIndex = 1;

        if (searchDto.PageSize < 1)
            searchDto.PageSize = 20;

        if (searchDto.PageSize > 100)
            searchDto.PageSize = 100;

        return ServiceResult.Success("验证通过");
    }

    public ServiceResult ValidatePagedQueryParameters(FormulaPagedQueryDto query)
    {
        if (query == null)
            return ServiceResult.Failure("查询参数不能为空");

        if (query.PageIndex < 1)
            query.PageIndex = 1;

        if (query.PageSize < 1)
            query.PageSize = 20;

        if (query.PageSize > 100)
            query.PageSize = 100;

        return ServiceResult.Success("验证通过");
    }

    #endregion

    #region 缓存管理层

    public async Task<ServiceResult<FormulaDto?>> GetCachedFormulaAsync(Guid formulaId)
    {
        var cacheKey = GenerateFormulaCacheKey(formulaId);
        
        if (_cache.TryGetValue(cacheKey, out FormulaDto cachedFormula))
        {
            return ServiceResult<FormulaDto?>.Success(cachedFormula, "缓存命中");
        }

        return ServiceResult<FormulaDto?>.Success(null, "缓存未命中");
    }

    public async Task SetFormulaCacheAsync(Guid formulaId, FormulaDto formula)
    {
        var cacheKey = GenerateFormulaCacheKey(formulaId);
        _cache.Set(cacheKey, formula, DefaultCacheTime);
        
        _logger.LogDebug("验方已缓存: {FormulaId} -> {CacheKey}", formulaId, cacheKey);
        await Task.CompletedTask;
    }

    public async Task<ServiceResult<List<FormulaDto>?>> GetCachedFormulasAsync(string cacheKey)
    {
        if (_cache.TryGetValue(cacheKey, out List<FormulaDto> cachedFormulas))
        {
            return ServiceResult<List<FormulaDto>?>.Success(cachedFormulas, "缓存命中");
        }

        return ServiceResult<List<FormulaDto>?>.Success(null, "缓存未命中");
    }

    public async Task SetFormulasCacheAsync(string cacheKey, List<FormulaDto> formulas)
    {
        _cache.Set(cacheKey, formulas, DefaultCacheTime);
        
        _logger.LogDebug("验方列表已缓存: {Count}条记录 -> {CacheKey}", formulas.Count, cacheKey);
        await Task.CompletedTask;
    }

    public void ClearFormulaCache(Guid? formulaId = null)
    {
        if (formulaId.HasValue)
        {
            var cacheKey = GenerateFormulaCacheKey(formulaId.Value);
            _cache.Remove(cacheKey);
            _logger.LogDebug("已清除验方缓存: {CacheKey}", cacheKey);
        }
        else
        {
            // 清除所有验方相关缓存
            ClearAllFormulaCache();
        }
    }

    public void ClearAllFormulaCache()
    {
        // 由于MemoryCache没有提供清除特定前缀缓存的方法，这里记录日志
        // 在实际应用中可以考虑使用更高级的缓存策略或第三方缓存库
        _logger.LogInformation("请求清除所有验方相关缓存");
        
        // 清除统计缓存
        _cache.Remove(FORMULA_STATS_CACHE_KEY);
    }

    public ServiceResult<FormulaCacheStatsDto> GetCacheStats()
    {
        var totalRequests = _cacheHits + _cacheMisses;
        var hitRate = totalRequests > 0 ? (double)_cacheHits / totalRequests * 100 : 0;

        var stats = new FormulaCacheStatsDto
        {
            TotalCachedItems = 0, // MemoryCache无法直接获取项目数量
            HitRate = Math.Round(hitRate, 2),
            TotalHits = _cacheHits,
            TotalMisses = _cacheMisses,
            LastUpdate = DateTime.Now
        };

        return ServiceResult<FormulaCacheStatsDto>.Success(stats, "缓存统计获取成功");
    }

    public async Task<ServiceResult> WarmupCacheAsync()
    {
        try
        {
            _logger.LogInformation("开始预热验方缓存");

            // 预热常用验方列表
            await CallGetFormulasApiAsync();
            
            // 预热统计数据
            await CallGetFormulaStatisticsApiAsync();

            _logger.LogInformation("验方缓存预热完成");
            return ServiceResult.Success("缓存预热成功");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "验方缓存预热失败");
            return ServiceResult.Failure("缓存预热失败: " + ex.Message);
        }
    }

    #endregion

    #region 基础操作层

    public async Task<ServiceResult<FormulaDto>> GetFormulaBasicInfoAsync(Guid formulaId)
    {
        var validation = ValidateFormulaId(formulaId);
        if (!validation.IsSuccess)
            return ServiceResult<FormulaDto>.Failure(validation.ErrorMessage);

        return await CallGetFormulaByIdApiAsync(formulaId);
    }

    public async Task<ServiceResult<FormulaDetailDto>> GetFormulaDetailAsync(Guid formulaId)
    {
        var validation = ValidateFormulaId(formulaId);
        if (!validation.IsSuccess)
            return ServiceResult<FormulaDetailDto>.Failure(validation.ErrorMessage);

        var formulaResult = await CallGetFormulaByIdApiAsync(formulaId);
        if (!formulaResult.IsSuccess)
            return ServiceResult<FormulaDetailDto>.Failure(formulaResult.ErrorMessage);

        // 转换为详细信息DTO
        var detail = new FormulaDetailDto
        {
            Id = formulaResult.Data.Id,
            Name = formulaResult.Data.Name,
            Type = formulaResult.Data.Type,
            Source = formulaResult.Data.Source,
            Effect = formulaResult.Data.Effect,
            Indications = formulaResult.Data.Indications,
            Contraindications = formulaResult.Data.Contraindications,
            Usage = formulaResult.Data.Usage,
            Preparation = formulaResult.Data.Preparation,
            Dosage = formulaResult.Data.Dosage,
            Notes = formulaResult.Data.Notes,
            Ingredients = formulaResult.Data.Ingredients,
            CreatorId = formulaResult.Data.CreatorId,
            CreatorName = formulaResult.Data.CreatorName,
            IsEnabled = formulaResult.Data.IsEnabled,
            CreateTime = formulaResult.Data.CreateTime,
            UpdateTime = formulaResult.Data.UpdateTime
        };

        return ServiceResult<FormulaDetailDto>.Success(detail, "验方详情获取成功");
    }

    public async Task<ServiceResult<bool>> CheckFormulaExistsAsync(Guid formulaId)
    {
        try
        {
            var result = await CallGetFormulaByIdApiAsync(formulaId);
            return ServiceResult<bool>.Success(result.IsSuccess, 
                result.IsSuccess ? "验方存在" : "验方不存在");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "检查验方是否存在时发生异常, ID: {FormulaId}", formulaId);
            return ServiceResult<bool>.Failure("检查验方存在性异常: " + ex.Message);
        }
    }

    public async Task<ServiceResult<bool>> CheckFormulaNameAvailableAsync(string name, Guid? excludeFormulaId = null)
    {
        try
        {
            var searchDto = new FormulaSearchDto { Name = name };
            var searchResult = await CallSearchFormulasApiAsync(searchDto);
            
            if (!searchResult.IsSuccess)
                return ServiceResult<bool>.Failure("检查名称可用性失败: " + searchResult.ErrorMessage);

            var existingFormulas = searchResult.Data
                .Where(f => f.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

            if (excludeFormulaId.HasValue)
                existingFormulas = existingFormulas.Where(f => f.Id != excludeFormulaId.Value);

            var isAvailable = !existingFormulas.Any();
            return ServiceResult<bool>.Success(isAvailable, 
                isAvailable ? "名称可用" : "名称已存在");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "检查验方名称可用性时发生异常, 名称: {Name}", name);
            return ServiceResult<bool>.Failure("检查名称可用性异常: " + ex.Message);
        }
    }

    public async Task<ServiceResult<List<FormulaIngredientDto>>> GetFormulaIngredientsAsync(Guid formulaId)
    {
        var formulaResult = await CallGetFormulaByIdApiAsync(formulaId);
        if (!formulaResult.IsSuccess)
            return ServiceResult<List<FormulaIngredientDto>>.Failure(formulaResult.ErrorMessage);

        return ServiceResult<List<FormulaIngredientDto>>.Success(
            formulaResult.Data.Ingredients, "验方药材获取成功");
    }

    public async Task<ServiceResult<bool>> CheckFormulaPermissionAsync(Guid formulaId, Guid userId, string operation)
    {
        // TODO: 实现具体的权限检查逻辑
        // 目前返回基础检查
        await Task.CompletedTask;
        
        if (formulaId == Guid.Empty || userId == Guid.Empty || string.IsNullOrWhiteSpace(operation))
            return ServiceResult<bool>.Success(false, "参数无效");

        return ServiceResult<bool>.Success(true, "权限检查通过");
    }

    public async Task LogFormulaOperationAsync(string operation, Guid formulaId, Guid? userId = null, object? additionalData = null)
    {
        try
        {
            _logger.LogInformation("验方操作记录: {Operation}, FormulaId: {FormulaId}, UserId: {UserId}, Data: {@Data}",
                operation, formulaId, userId, additionalData);
            
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "记录验方操作日志时发生异常");
        }
    }

    public string GenerateFormulaCacheKey(Guid formulaId)
    {
        return $"{FORMULA_CACHE_KEY}{formulaId}";
    }

    public string GenerateFormulasListCacheKey(string operation, object? parameters = null)
    {
        var baseKey = $"{FORMULAS_LIST_CACHE_KEY}{operation}";
        
        if (parameters != null)
        {
            var hash = parameters.GetHashCode();
            return $"{baseKey}_{hash}";
        }
        
        return baseKey;
    }

    #endregion
}