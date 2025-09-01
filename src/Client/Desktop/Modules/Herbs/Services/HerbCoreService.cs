using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using LYBT.Desktop.Herbs.Interfaces;
using LYBT.Shared.Interfaces.Api;
using LYBT.Shared.Models;
using LYBT.Shared.Models.Herbs;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Herbs.Services;

/// <summary>
/// 中药材核心服务实现 - UltraThink三层架构核心操作层
/// 职责：API通信、基础CRUD操作、数据验证、缓存管理
/// </summary>
public class HerbCoreService : IHerbCoreService
{
    private readonly IHerbApi _herbApi;
    private readonly IMapper _mapper;
    private readonly IMemoryCache _cache;
    private readonly ILogger<HerbCoreService> _logger;
    
    private const string CACHE_KEY_ALL_HERBS = "herbs_all";
    private const string CACHE_KEY_HERB_PREFIX = "herb_";
    private readonly TimeSpan _cacheExpiry = TimeSpan.FromMinutes(30);
    
    public HerbCoreService(
        IHerbApi herbApi,
        IMapper mapper,
        IMemoryCache cache,
        ILogger<HerbCoreService> logger)
    {
        _herbApi = herbApi ?? throw new ArgumentNullException(nameof(herbApi));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }
    
    #region API通信操作
    
    public async Task<ServiceResult<HerbDto>> CallCreateHerbApiAsync(HerbCreateDto createDto)
    {
        try
        {
            _logger.LogInformation("调用创建中药材API: {HerbName}", createDto.Name);
            
            var apiResponse = await _herbApi.CreateHerbAsync(createDto);
            if (!apiResponse.IsSuccessStatusCode || apiResponse.Content == null)
            {
                var errorMessage = apiResponse.Error?.Message ?? "创建中药材失败";
                _logger.LogWarning("创建中药材API调用失败: {Error}", errorMessage);
                return ServiceResult<HerbDto>.Failure(errorMessage);
            }
            
            var herbDto = _mapper.Map<HerbDto>(apiResponse.Content);
            
            // 清除相关缓存
            ClearHerbCache();
            
            _logger.LogInformation("中药材创建成功: {HerbId}", herbDto.Id);
            return ServiceResult<HerbDto>.Success(herbDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "调用创建中药材API异常: {HerbName}", createDto.Name);
            return ServiceResult<HerbDto>.Failure($"创建中药材异常: {ex.Message}");
        }
    }
    
    public async Task<ServiceResult<HerbDto>> CallUpdateHerbApiAsync(Guid id, HerbUpdateDto updateDto)
    {
        try
        {
            _logger.LogInformation("调用更新中药材API: {HerbId}", id);
            
            var apiResponse = await _herbApi.UpdateHerbAsync(id, updateDto);
            if (!apiResponse.IsSuccessStatusCode || apiResponse.Content == null)
            {
                var errorMessage = apiResponse.Error?.Message ?? "更新中药材失败";
                _logger.LogWarning("更新中药材API调用失败: {Error}", errorMessage);
                return ServiceResult<HerbDto>.Failure(errorMessage);
            }
            
            var herbDto = _mapper.Map<HerbDto>(apiResponse.Content);
            
            // 清除相关缓存
            ClearHerbCache();
            _cache.Remove($"{CACHE_KEY_HERB_PREFIX}{id}");
            
            _logger.LogInformation("中药材更新成功: {HerbId}", id);
            return ServiceResult<HerbDto>.Success(herbDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "调用更新中药材API异常: {HerbId}", id);
            return ServiceResult<HerbDto>.Failure($"更新中药材异常: {ex.Message}");
        }
    }
    
    public async Task<ServiceResult<bool>> CallDeleteHerbApiAsync(Guid id)
    {
        try
        {
            _logger.LogInformation("调用删除中药材API: {HerbId}", id);
            
            var apiResponse = await _herbApi.DeleteHerbAsync(id);
            if (!apiResponse.IsSuccessStatusCode)
            {
                var errorMessage = apiResponse.Error?.Message ?? "删除中药材失败";
                _logger.LogWarning("删除中药材API调用失败: {Error}", errorMessage);
                return ServiceResult<bool>.Failure(errorMessage);
            }
            
            // 清除相关缓存
            ClearHerbCache();
            _cache.Remove($"{CACHE_KEY_HERB_PREFIX}{id}");
            
            _logger.LogInformation("中药材删除成功: {HerbId}", id);
            return ServiceResult<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "调用删除中药材API异常: {HerbId}", id);
            return ServiceResult<bool>.Failure($"删除中药材异常: {ex.Message}");
        }
    }
    
    public async Task<ServiceResult<HerbDto>> CallGetHerbByIdApiAsync(Guid id)
    {
        try
        {
            _logger.LogInformation("调用获取中药材详情API: {HerbId}", id);
            
            var apiResponse = await _herbApi.GetHerbByIdAsync(id);
            if (!apiResponse.IsSuccessStatusCode || apiResponse.Content == null)
            {
                var errorMessage = apiResponse.Error?.Message ?? "获取中药材详情失败";
                _logger.LogWarning("获取中药材详情API调用失败: {Error}", errorMessage);
                return ServiceResult<HerbDto>.Failure(errorMessage);
            }
            
            var herbDto = _mapper.Map<HerbDto>(apiResponse.Content);
            
            _logger.LogInformation("获取中药材详情成功: {HerbId}", id);
            return ServiceResult<HerbDto>.Success(herbDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "调用获取中药材详情API异常: {HerbId}", id);
            return ServiceResult<HerbDto>.Failure($"获取中药材详情异常: {ex.Message}");
        }
    }
    
    public async Task<ServiceResult<List<HerbDto>>> CallGetAllHerbsApiAsync()
    {
        try
        {
            _logger.LogInformation("调用获取所有中药材API");
            
            var apiResponse = await _herbApi.GetHerbsAsync(1, 1000); // 假设最多1000条记录
            if (!apiResponse.IsSuccessStatusCode || apiResponse.Content == null)
            {
                var errorMessage = apiResponse.Error?.Message ?? "获取中药材列表失败";
                _logger.LogWarning("获取中药材列表API调用失败: {Error}", errorMessage);
                return ServiceResult<List<HerbDto>>.Failure(errorMessage);
            }
            
            var herbDtos = apiResponse.Content.Items.ToList();
            
            _logger.LogInformation("获取中药材列表成功，共 {Count} 条记录", herbDtos.Count);
            return ServiceResult<List<HerbDto>>.Success(herbDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "调用获取所有中药材API异常");
            return ServiceResult<List<HerbDto>>.Failure($"获取中药材列表异常: {ex.Message}");
        }
    }
    
    #endregion
    
    #region 基础数据操作
    
    public async Task<ServiceResult<HerbDto>> GetHerbByIdAsync(Guid id)
    {
        try
        {
            // 先检查缓存
            var cacheKey = $"{CACHE_KEY_HERB_PREFIX}{id}";
            if (_cache.TryGetValue(cacheKey, out HerbDto cachedHerb))
            {
                return ServiceResult<HerbDto>.Success(cachedHerb);
            }
            
            // 缓存未命中，调用API
            var apiResult = await CallGetHerbByIdApiAsync(id);
            if (apiResult.IsSuccess && apiResult.Data != null)
            {
                // 添加到缓存
                _cache.Set(cacheKey, apiResult.Data, _cacheExpiry);
            }
            
            return apiResult;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取中药材信息异常: {HerbId}", id);
            return ServiceResult<HerbDto>.Failure($"获取中药材信息异常: {ex.Message}");
        }
    }
    
    public async Task<ServiceResult<List<HerbDto>>> GetAllHerbsAsync()
    {
        try
        {
            // 先检查缓存
            if (_cache.TryGetValue(CACHE_KEY_ALL_HERBS, out List<HerbDto> cachedHerbs))
            {
                return ServiceResult<List<HerbDto>>.Success(cachedHerbs);
            }
            
            // 缓存未命中，调用API
            var apiResult = await CallGetAllHerbsApiAsync();
            if (apiResult.IsSuccess && apiResult.Data != null)
            {
                // 添加到缓存
                _cache.Set(CACHE_KEY_ALL_HERBS, apiResult.Data, _cacheExpiry);
            }
            
            return apiResult;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取所有中药材信息异常");
            return ServiceResult<List<HerbDto>>.Failure($"获取所有中药材信息异常: {ex.Message}");
        }
    }
    
    public async Task<ServiceResult<bool>> ValidateHerbExistsAsync(Guid id)
    {
        try
        {
            var result = await GetHerbByIdAsync(id);
            return ServiceResult<bool>.Success(result.IsSuccess);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "验证中药材存在性异常: {HerbId}", id);
            return ServiceResult<bool>.Failure($"验证中药材存在性异常: {ex.Message}");
        }
    }
    
    public async Task<ServiceResult<bool>> CheckHerbNameExistsAsync(string name, Guid? excludeId = null)
    {
        try
        {
            var allHerbsResult = await GetAllHerbsAsync();
            if (!allHerbsResult.IsSuccess || allHerbsResult.Data == null)
            {
                return ServiceResult<bool>.Failure("获取中药材列表失败");
            }
            
            var exists = allHerbsResult.Data.Any(h => 
                h.Name.Equals(name, StringComparison.OrdinalIgnoreCase) && 
                (!excludeId.HasValue || h.Id != excludeId.Value));
            
            return ServiceResult<bool>.Success(exists);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "检查中药材名称重复异常: {HerbName}", name);
            return ServiceResult<bool>.Failure($"检查中药材名称重复异常: {ex.Message}");
        }
    }
    
    #endregion
    
    #region 数据验证操作
    
    public ServiceResult ValidateHerbCreateData(HerbCreateDto createDto)
    {
        if (createDto == null)
            return ServiceResult.Failure("创建数据不能为空");
            
        if (string.IsNullOrWhiteSpace(createDto.Name))
            return ServiceResult.Failure("中药材名称不能为空");
            
        if (createDto.Name.Length > 50)
            return ServiceResult.Failure("中药材名称长度不能超过50字符");
            
        if (string.IsNullOrWhiteSpace(createDto.Category))
            return ServiceResult.Failure("中药材分类不能为空");
            
        if (createDto.Price < 0)
            return ServiceResult.Failure("中药材价格不能为负数");
            
        return ServiceResult.Success();
    }
    
    public ServiceResult ValidateHerbUpdateData(HerbUpdateDto updateDto)
    {
        if (updateDto == null)
            return ServiceResult.Failure("更新数据不能为空");
            
        if (!string.IsNullOrWhiteSpace(updateDto.Name) && updateDto.Name.Length > 50)
            return ServiceResult.Failure("中药材名称长度不能超过50字符");
            
        if (updateDto.Price.HasValue && updateDto.Price < 0)
            return ServiceResult.Failure("中药材价格不能为负数");
            
        return ServiceResult.Success();
    }
    
    public ServiceResult ValidatePriceData(decimal price)
    {
        if (price < 0)
            return ServiceResult.Failure("价格不能为负数");
            
        if (price > 10000)
            return ServiceResult.Failure("价格过高，请检查输入");
            
        return ServiceResult.Success();
    }
    
    public ServiceResult ValidateHerbBasicInfo(string name, string category, string properties)
    {
        if (string.IsNullOrWhiteSpace(name))
            return ServiceResult.Failure("中药材名称不能为空");
            
        if (string.IsNullOrWhiteSpace(category))
            return ServiceResult.Failure("中药材分类不能为空");
            
        if (string.IsNullOrWhiteSpace(properties))
            return ServiceResult.Failure("中药材性味不能为空");
            
        return ServiceResult.Success();
    }
    
    #endregion
    
    #region 缓存和性能优化
    
    public async Task<ServiceResult> PreloadCommonHerbsAsync()
    {
        try
        {
            _logger.LogInformation("开始预加载常用中药材");
            
            // 预加载所有中药材到缓存
            await GetAllHerbsAsync();
            
            _logger.LogInformation("预加载常用中药材完成");
            return ServiceResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "预加载常用中药材异常");
            return ServiceResult.Failure($"预加载常用中药材异常: {ex.Message}");
        }
    }
    
    public ServiceResult ClearHerbCache()
    {
        try
        {
            _cache.Remove(CACHE_KEY_ALL_HERBS);
            
            // 清除所有以CACHE_KEY_HERB_PREFIX开头的缓存项
            // 注意：IMemoryCache没有直接方法获取所有键，这里简化处理
            _logger.LogInformation("中药材缓存已清除");
            return ServiceResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "清除中药材缓存异常");
            return ServiceResult.Failure($"清除中药材缓存异常: {ex.Message}");
        }
    }
    
    public ServiceResult<List<HerbDto>> GetCachedHerbs()
    {
        try
        {
            if (_cache.TryGetValue(CACHE_KEY_ALL_HERBS, out List<HerbDto> cachedHerbs))
            {
                return ServiceResult<List<HerbDto>>.Success(cachedHerbs);
            }
            
            return ServiceResult<List<HerbDto>>.Failure("缓存中没有中药材数据");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取缓存的中药材数据异常");
            return ServiceResult<List<HerbDto>>.Failure($"获取缓存的中药材数据异常: {ex.Message}");
        }
    }
    
    #endregion
}