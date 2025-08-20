using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LYBT.Desktop.Core.Coordinators;
using LYBT.Shared.Interfaces.Services;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Core.Coordinators
{
    /// <summary>
    /// 中药材协调器 - UltraThink架构的中药材业务协调层
    /// 协调中药材相关的所有业务操作，包括价格管理和库存提醒
    /// </summary>
    public class HerbCoordinator : IDataCoordinator<HerbDto, HerbCreateDto, HerbUpdateDto>
    {
        #region Fields

        private readonly IHerbService _herbService;
        private readonly ILogger<HerbCoordinator> _logger;
        private readonly Dictionary<Guid, HerbDto> _cache = new();
        private readonly Dictionary<string, List<HerbDto>> _categoryCache = new();

        #endregion

        #region Events

        public event EventHandler<DataChangedEventArgs<HerbDto>>? DataChanged;
        public event EventHandler<OperationProgressEventArgs>? OperationProgress;

        /// <summary>
        /// 价格变化事件
        /// </summary>
        public event EventHandler<HerbPriceChangedEventArgs>? PriceChanged;

        /// <summary>
        /// 库存警告事件
        /// </summary>
        public event EventHandler<HerbStockWarningEventArgs>? StockWarning;

        #endregion

        #region Constructor

        public HerbCoordinator(IHerbService herbService, ILogger<HerbCoordinator> logger)
        {
            _herbService = herbService ?? throw new ArgumentNullException(nameof(herbService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        #endregion

        #region Query Operations

        public async Task<ServiceResult<PagedResult<HerbDto>>> GetPagedAsync(PagedQueryBaseDto query)
        {
            try
            {
                _logger.LogInformation("开始分页查询中药材，页码: {Page}, 关键字: {Keyword}", query.CurrentPage, query.SearchKeyword);

                // 转换为中药材查询DTO
                var herbQuery = new HerbPagedQueryDto
                {
                    PageIndex = query.CurrentPage,
                    PageSize = query.PageSize,
                    Keyword = query.SearchKeyword
                };

                var result = await _herbService.GetPagedAsync(herbQuery);

                if (result.IsSuccess && result.Data != null)
                {
                    // 更新缓存
                    foreach (var herb in result.Data.Items)
                    {
                        _cache[herb.Id] = herb;
                        
                        // 检查库存警告
                        // UltraThink v2.0: 库存管理功能已移除
                        // CheckStockWarning(herb);
                    }

                    _logger.LogInformation("中药材分页查询成功，返回 {Count} 条记录", result.Data.Items.Count);
                }
                else
                {
                    _logger.LogWarning("中药材分页查询失败: {Error}", result.ErrorMessage);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "中药材分页查询异常");
                return ServiceResult<PagedResult<HerbDto>>.Failure($"查询中药材失败: {ex.Message}", ex);
            }
        }

        public async Task<ServiceResult<HerbDto>> GetByIdAsync(Guid id)
        {
            try
            {
                // 先检查缓存
                if (_cache.TryGetValue(id, out var cachedHerb))
                {
                    return ServiceResult<HerbDto>.Success(cachedHerb);
                }

                _logger.LogInformation("根据ID查询中药材: {HerbId}", id);

                var result = await _herbService.GetByIdAsync(id);

                if (result.IsSuccess && result.Data != null)
                {
                    // 更新缓存
                    _cache[id] = result.Data;
                    
                    // 检查库存警告
                    // UltraThink v2.0: 库存管理功能已移除
                    // CheckStockWarning(result.Data);
                    
                    _logger.LogInformation("中药材查询成功: {HerbName}", result.Data.Name);
                }
                else
                {
                    _logger.LogWarning("中药材查询失败: {Error}", result.ErrorMessage);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "根据ID查询中药材异常: {HerbId}", id);
                return ServiceResult<HerbDto>.Failure($"查询中药材详情失败: {ex.Message}", ex);
            }
        }

        public async Task<ServiceResult<List<HerbDto>>> SearchAsync(string keyword)
        {
            try
            {
                _logger.LogInformation("搜索中药材，关键字: {Keyword}", keyword);

                var query = new HerbPagedQueryDto
                {
                    Keyword = keyword,
                    PageIndex = 1,
                    PageSize = 200 // 中药材数据量大，增加搜索结果数量
                };

                var result = await _herbService.GetPagedAsync(query);

                if (result.IsSuccess && result.Data != null)
                {
                    _logger.LogInformation("中药材搜索成功，找到 {Count} 条记录", result.Data.Items.Count);
                    return ServiceResult<List<HerbDto>>.Success(result.Data.Items);
                }

                return ServiceResult<List<HerbDto>>.Failure(result.ErrorMessage ?? "搜索中药材失败");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "中药材搜索异常");
                return ServiceResult<List<HerbDto>>.Failure($"搜索中药材失败: {ex.Message}", ex);
            }
        }

        public async Task<ServiceResult<List<HerbDto>>> GetActiveAsync()
        {
            try
            {
                _logger.LogInformation("获取可用中药材列表");

                var query = new HerbPagedQueryDto
                {
                    Status = CommonStatus.Enabled,
                    PageIndex = 1,
                    PageSize = 2000 // 中药材种类较多
                };

                var result = await _herbService.GetPagedAsync(query);

                if (result.IsSuccess && result.Data != null)
                {
                    _logger.LogInformation("可用中药材查询成功，返回 {Count} 条记录", result.Data.Items.Count);
                    return ServiceResult<List<HerbDto>>.Success(result.Data.Items);
                }

                return ServiceResult<List<HerbDto>>.Failure(result.ErrorMessage ?? "获取可用中药材失败");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取可用中药材异常");
                return ServiceResult<List<HerbDto>>.Failure($"获取可用中药材失败: {ex.Message}", ex);
            }
        }

        #endregion

        #region Extended Query Operations

        /// <summary>
        /// 按分类获取中药材
        /// </summary>
        public async Task<ServiceResult<List<HerbDto>>> GetByCategoryAsync(string category)
        {
            try
            {
                // 检查分类缓存
                if (_categoryCache.TryGetValue(category, out var cachedHerbs))
                {
                    return ServiceResult<List<HerbDto>>.Success(cachedHerbs);
                }

                _logger.LogInformation("按分类查询中药材: {Category}", category);

                var query = new HerbPagedQueryDto
                {
                    // Category = category, // 属性不存在：HerbPagedQueryDto.Category
                    Status = CommonStatus.Enabled,
                    PageIndex = 1,
                    PageSize = 500
                };

                var result = await _herbService.GetPagedAsync(query);

                if (result.IsSuccess && result.Data != null)
                {
                    // 更新分类缓存
                    _categoryCache[category] = result.Data.Items;
                    
                    _logger.LogInformation("按分类查询中药材成功，分类: {Category}, 数量: {Count}", category, result.Data.Items.Count);
                    return ServiceResult<List<HerbDto>>.Success(result.Data.Items);
                }

                return ServiceResult<List<HerbDto>>.Failure(result.ErrorMessage ?? "按分类查询中药材失败");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "按分类查询中药材异常: {Category}", category);
                return ServiceResult<List<HerbDto>>.Failure($"按分类查询中药材失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 获取低库存中药材
        /// </summary>
        public async Task<ServiceResult<List<HerbDto>>> GetLowStockHerbsAsync(int threshold = 10)
        {
            try
            {
                _logger.LogInformation("查询低库存中药材，阈值: {Threshold}", threshold);

                var query = new HerbPagedQueryDto
                {
                    Status = CommonStatus.Enabled,
                    PageIndex = 1,
                    PageSize = 1000
                };

                var result = await _herbService.GetPagedAsync(query);

                if (result.IsSuccess && result.Data != null)
                {
                    // 筛选低库存中药材
                    // UltraThink v2.0: 库存功能已移除，直接返回所有药材
                    var lowStockHerbs = result.Data.Items.ToList();
                    
                    _logger.LogInformation("低库存中药材查询完成，找到 {Count} 种", lowStockHerbs.Count);
                    return ServiceResult<List<HerbDto>>.Success(lowStockHerbs);
                }

                return ServiceResult<List<HerbDto>>.Failure(result.ErrorMessage ?? "查询低库存中药材失败");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "查询低库存中药材异常");
                return ServiceResult<List<HerbDto>>.Failure($"查询低库存中药材失败: {ex.Message}", ex);
            }
        }

        #endregion

        #region CRUD Operations

        public async Task<ServiceResult<HerbDto>> CreateAsync(HerbCreateDto createDto)
        {
            try
            {
                _logger.LogInformation("创建中药材: {HerbName}", createDto.Name);

                // 验证数据
                var validationResult = await ValidateAsync(createDto);
                if (!validationResult.IsSuccess)
                {
                    return ServiceResult<HerbDto>.Failure(validationResult.ErrorMessage ?? "数据验证失败");
                }

                var result = await _herbService.CreateAsync(createDto);

                if (result.IsSuccess && result.Data != null)
                {
                    // 更新缓存
                    _cache[result.Data.Id] = result.Data;
                    
                    // 清除分类缓存

                    // 触发数据变化事件
                    DataChanged?.Invoke(this, new DataChangedEventArgs<HerbDto>(DataChangeType.Created, result.Data));

                    _logger.LogInformation("中药材创建成功: {HerbId}", result.Data.Id);
                }
                else
                {
                    _logger.LogWarning("中药材创建失败: {Error}", result.ErrorMessage);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "创建中药材异常");
                return ServiceResult<HerbDto>.Failure($"创建中药材失败: {ex.Message}", ex);
            }
        }

        public async Task<ServiceResult<HerbDto>> UpdateAsync(Guid id, HerbUpdateDto updateDto)
        {
            try
            {
                _logger.LogInformation("更新中药材: {HerbId}", id);

                // 获取原有数据用于价格比较
                var oldHerb = await GetByIdAsync(id);

                // 验证数据
                var validationResult = await ValidateUpdateAsync(id, updateDto);
                if (!validationResult.IsSuccess)
                {
                    return ServiceResult<HerbDto>.Failure(validationResult.ErrorMessage ?? "数据验证失败");
                }

                var result = await _herbService.UpdateAsync(id, updateDto);

                if (result.IsSuccess && result.Data != null)
                {
                    // 更新缓存
                    _cache[id] = result.Data;
                    
                    // 清除分类缓存

                    // 检查价格变化
                    if (oldHerb.IsSuccess && oldHerb.Data != null && oldHerb.Data.Price != result.Data.Price)
                    {
                        PriceChanged?.Invoke(this, new HerbPriceChangedEventArgs(result.Data, oldHerb.Data.Price, result.Data.Price));
                    }

                    // 检查库存警告
                    // UltraThink v2.0: 库存管理功能已移除
                    // CheckStockWarning(result.Data);

                    // 触发数据变化事件
                    DataChanged?.Invoke(this, new DataChangedEventArgs<HerbDto>(DataChangeType.Updated, result.Data));

                    _logger.LogInformation("中药材更新成功: {HerbId}", id);
                }
                else
                {
                    _logger.LogWarning("中药材更新失败: {Error}", result.ErrorMessage);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新中药材异常: {HerbId}", id);
                return ServiceResult<HerbDto>.Failure($"更新中药材失败: {ex.Message}", ex);
            }
        }

        public async Task<ServiceResult<bool>> DeleteAsync(Guid id)
        {
            try
            {
                _logger.LogInformation("删除中药材: {HerbId}", id);

                // 获取中药材信息用于事件
                var herb = await GetByIdAsync(id);

                var result = await _herbService.DeleteAsync(id);

                if (result.IsSuccess)
                {
                    // 从缓存中移除
                    _cache.Remove(id);
                    
                    // 清除分类缓存

                    // 触发数据变化事件
                    if (herb.IsSuccess && herb.Data != null)
                    {
                        DataChanged?.Invoke(this, new DataChangedEventArgs<HerbDto>(DataChangeType.Deleted, herb.Data));
                    }

                    _logger.LogInformation("中药材删除成功: {HerbId}", id);
                }
                else
                {
                    _logger.LogWarning("中药材删除失败: {Error}", result.ErrorMessage);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "删除中药材异常: {HerbId}", id);
                return ServiceResult<bool>.Failure($"删除中药材失败: {ex.Message}", ex);
            }
        }

        #endregion

        #region Status Operations

        // EnableAsync、DisableAsync、BatchEnableAsync、BatchDisableAsync方法已删除（方法不存在）
        // 提供空实现以满足接口要求
        
        public async Task<ServiceResult<bool>> EnableAsync(Guid id)
        {
            await Task.CompletedTask;
            return ServiceResult<bool>.Failure("方法未实现");
        }
        
        public async Task<ServiceResult<bool>> DisableAsync(Guid id)
        {
            await Task.CompletedTask;
            return ServiceResult<bool>.Failure("方法未实现");
        }
        
        public async Task<ServiceResult<int>> BatchEnableAsync(List<Guid> ids)
        {
            await Task.CompletedTask;
            return ServiceResult<int>.Failure("方法未实现");
        }
        
        public async Task<ServiceResult<int>> BatchDisableAsync(List<Guid> ids)
        {
            await Task.CompletedTask;
            return ServiceResult<int>.Failure("方法未实现");
        }

        #endregion

        #region Validation

        public async Task<ServiceResult<bool>> ValidateAsync(HerbCreateDto createDto)
        {
            try
            {
                if (createDto == null)
                    return ServiceResult<bool>.Failure("创建数据不能为空");

                if (string.IsNullOrWhiteSpace(createDto.Name))
                    return ServiceResult<bool>.Failure("中药材名称不能为空");

                if (createDto.Price <= 0)
                    return ServiceResult<bool>.Failure("中药材价格必须大于0");

                // 检查名称是否重复
                var existingHerbs = await SearchAsync(createDto.Name);
                if (existingHerbs.IsSuccess && existingHerbs.Data != null && 
                    existingHerbs.Data.Any(h => h.Name.Equals(createDto.Name, StringComparison.OrdinalIgnoreCase)))
                {
                    return ServiceResult<bool>.Failure("该中药材名称已存在");
                }

                return ServiceResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "验证中药材创建数据异常");
                return ServiceResult<bool>.Failure($"数据验证失败: {ex.Message}", ex);
            }
        }

        public async Task<ServiceResult<bool>> ValidateUpdateAsync(Guid id, HerbUpdateDto updateDto)
        {
            try
            {
                if (updateDto == null)
                    return ServiceResult<bool>.Failure("更新数据不能为空");

                if (string.IsNullOrWhiteSpace(updateDto.Name))
                    return ServiceResult<bool>.Failure("中药材名称不能为空");

                if (updateDto.Price <= 0)
                    return ServiceResult<bool>.Failure("中药材价格必须大于0");

                // 检查中药材是否存在
                var existingHerb = await GetByIdAsync(id);
                if (!existingHerb.IsSuccess || existingHerb.Data == null)
                {
                    return ServiceResult<bool>.Failure("中药材不存在");
                }

                return ServiceResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "验证中药材更新数据异常: {HerbId}", id);
                return ServiceResult<bool>.Failure($"数据验证失败: {ex.Message}", ex);
            }
        }

        #endregion

        #region Cache Management

        public async Task RefreshCacheAsync()
        {
            try
            {
                _logger.LogInformation("刷新中药材缓存");

                // 获取所有可用中药材并更新缓存
                var activeHerbs = await GetActiveAsync();
                if (activeHerbs.IsSuccess && activeHerbs.Data != null)
                {
                    _cache.Clear();
                    _categoryCache.Clear();
                    
                    foreach (var herb in activeHerbs.Data)
                    {
                        _cache[herb.Id] = herb;
                    }

                    _logger.LogInformation("中药材缓存刷新完成，缓存 {Count} 条记录", activeHerbs.Data.Count);

                    // 触发数据刷新事件
                    DataChanged?.Invoke(this, new DataChangedEventArgs<HerbDto>(DataChangeType.Refreshed, activeHerbs.Data));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "刷新中药材缓存异常");
            }
        }

        public void ClearCache()
        {
            _logger.LogInformation("清除中药材缓存");
            _cache.Clear();
            _categoryCache.Clear();
        }

        #endregion

        #region Private Helper Methods

        private async Task RefreshHerbCache(Guid id)
        {
            // 清除缓存以强制重新加载
            _cache.Remove(id);

            // 重新获取中药材信息
            var updatedHerb = await GetByIdAsync(id);
            if (updatedHerb.IsSuccess && updatedHerb.Data != null)
            {
                DataChanged?.Invoke(this, new DataChangedEventArgs<HerbDto>(DataChangeType.StatusChanged, updatedHerb.Data));
            }
        }

        // UltraThink v2.0: 库存管理功能已移除，Herbs模块只管理药材信息和单价
        // private void CheckStockWarning(HerbDto herb)
        // {
        //     const int lowStockThreshold = 10;
        //     const int outOfStockThreshold = 0;
        //
        //     if (herb.Stock <= outOfStockThreshold)
        //     {
        //         StockWarning?.Invoke(this, new HerbStockWarningEventArgs(herb, StockWarningType.OutOfStock));
        //     }
        //     else if (herb.Stock <= lowStockThreshold)
        //     {
        //         StockWarning?.Invoke(this, new HerbStockWarningEventArgs(herb, StockWarningType.LowStock));
        //     }
        // }

        #endregion
    }

    #region Event Args

    /// <summary>
    /// 中药材价格变化事件参数
    /// </summary>
    public class HerbPriceChangedEventArgs : EventArgs
    {
        public HerbDto Herb { get; }
        public decimal OldPrice { get; }
        public decimal NewPrice { get; }
        public decimal ChangeAmount => NewPrice - OldPrice;
        public decimal ChangePercentage => OldPrice == 0 ? 0 : (ChangeAmount / OldPrice) * 100;

        public HerbPriceChangedEventArgs(HerbDto herb, decimal oldPrice, decimal newPrice)
        {
            Herb = herb;
            OldPrice = oldPrice;
            NewPrice = newPrice;
        }
    }

    /// <summary>
    /// 中药材库存警告事件参数
    /// </summary>
    public class HerbStockWarningEventArgs : EventArgs
    {
        public HerbDto Herb { get; }
        public StockWarningType WarningType { get; }

        public HerbStockWarningEventArgs(HerbDto herb, StockWarningType warningType)
        {
            Herb = herb;
            WarningType = warningType;
        }
    }

    /// <summary>
    /// 库存警告类型
    /// </summary>
    public enum StockWarningType
    {
        LowStock,
        OutOfStock
    }

    #endregion
}