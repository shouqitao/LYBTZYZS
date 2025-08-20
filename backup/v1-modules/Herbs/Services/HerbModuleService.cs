using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using LYBT.Desktop.Core.Models.Herbs;
using LYBT.Desktop.Core.Models.Common;
using LYBT.Desktop.Herbs.Services.Interfaces;
using LYBT.Desktop.Services.Interfaces;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.Shared.Models.Enums;

namespace LYBT.Desktop.Herbs.Services
{
    /// <summary>
    /// Herb模块核心业务服务实现
    /// UltraThink模块化架构：封装模块业务逻辑，使用AutoMapper进行DTO↔Info转换
    /// </summary>
    public class HerbModuleService : IHerbModuleService
    {
        private readonly IHerbApiService _apiService;
        private readonly IMapper _mapper;
        
        public HerbModuleService(IHerbApiService apiService, IMapper mapper)
        {
            _apiService = apiService ?? throw new ArgumentNullException(nameof(apiService));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        }
        
        #region 基础CRUD操作
        
        public async Task<ServiceResult<PagedResult<HerbInfo>>> GetPagedAsync(PagedQueryBaseDto query)
        {
            try
            {
                // 转换为中药材专用查询DTO
                var herbQuery = new HerbPagedQueryDto
                {
                    PageIndex = query.PageIndex,
                    PageSize = query.PageSize,
                    Keyword = query.Keyword,
                    SortField = query.SortField,
                    SortDirection = query.SortDirection
                };

                // UltraThink四层架构：API调用获取DTOs
                var apiResult = await _apiService.GetPagedAsync(herbQuery);
                if (!apiResult.IsSuccess || apiResult.Data == null)
                {
                    return ServiceResult<PagedResult<HerbInfo>>.Failure(
                        apiResult.ErrorMessage ?? "获取中药材列表失败");
                }
                
                // UltraThink四层架构：使用AutoMapper转换 DTOs → Infos
                var herbInfos = _mapper.Map<List<HerbInfo>>(apiResult.Data.Items);
                var result = new PagedResult<HerbInfo>(
                    herbInfos,
                    apiResult.Data.TotalCount,
                    apiResult.Data.CurrentPage,
                    apiResult.Data.PageSize);
                
                return ServiceResult<PagedResult<HerbInfo>>.Success(result);
            }
            catch (Exception ex)
            {
                return ServiceResult<PagedResult<HerbInfo>>.Failure($"获取中药材列表异常: {ex.Message}");
            }
        }
        
        public async Task<ServiceResult<HerbInfo>> GetByIdAsync(Guid id)
        {
            try
            {
                if (id == Guid.Empty)
                {
                    return ServiceResult<HerbInfo>.Failure("中药材ID不能为空");
                }
                
                // UltraThink四层架构：API调用获取DTO
                var apiResult = await _apiService.GetByIdAsync(id);
                if (!apiResult.IsSuccess || apiResult.Data == null)
                {
                    return ServiceResult<HerbInfo>.Failure(
                        apiResult.ErrorMessage ?? "获取中药材详情失败");
                }
                
                // UltraThink四层架构：使用AutoMapper转换 DTO → Info
                var herbInfo = _mapper.Map<HerbInfo>(apiResult.Data);
                return ServiceResult<HerbInfo>.Success(herbInfo);
            }
            catch (Exception ex)
            {
                return ServiceResult<HerbInfo>.Failure($"获取中药材详情异常: {ex.Message}");
            }
        }
        
        public async Task<ServiceResult<HerbInfo>> CreateAsync(HerbCreateInfo createInfo)
        {
            try
            {
                // 业务验证
                var validationResult = await ValidateAsync(_mapper.Map<HerbInfo>(createInfo));
                if (!validationResult.IsSuccess)
                {
                    return ServiceResult<HerbInfo>.Failure(validationResult.ErrorMessage);
                }
                
                // 检查中药材名称是否已存在
                var nameExistsResult = await IsNameExistsAsync(createInfo.Name);
                if (nameExistsResult.IsSuccess && nameExistsResult.Data)
                {
                    return ServiceResult<HerbInfo>.Failure("该中药材名称已被使用");
                }
                
                // UltraThink四层架构：使用AutoMapper转换 Info → DTO
                var createDto = _mapper.Map<HerbCreateDto>(createInfo);
                
                // API调用
                var apiResult = await _apiService.CreateAsync(createDto);
                if (!apiResult.IsSuccess || apiResult.Data == null)
                {
                    return ServiceResult<HerbInfo>.Failure(
                        apiResult.ErrorMessage ?? "创建中药材失败");
                }
                
                // UltraThink四层架构：使用AutoMapper转换 DTO → Info
                var herbInfo = _mapper.Map<HerbInfo>(apiResult.Data);
                return ServiceResult<HerbInfo>.Success(herbInfo);
            }
            catch (Exception ex)
            {
                return ServiceResult<HerbInfo>.Failure($"创建中药材异常: {ex.Message}");
            }
        }
        
        public async Task<ServiceResult<HerbInfo>> UpdateAsync(HerbUpdateInfo updateInfo)
        {
            try
            {
                // 业务验证
                var validationResult = await ValidateAsync(_mapper.Map<HerbInfo>(updateInfo));
                if (!validationResult.IsSuccess)
                {
                    return ServiceResult<HerbInfo>.Failure(validationResult.ErrorMessage);
                }
                
                // 检查中药材名称是否已被其他中药材使用
                var nameExistsResult = await IsNameExistsAsync(updateInfo.Name, updateInfo.Id);
                if (nameExistsResult.IsSuccess && nameExistsResult.Data)
                {
                    return ServiceResult<HerbInfo>.Failure("该中药材名称已被其他中药材使用");
                }
                
                // UltraThink四层架构：使用AutoMapper转换 Info → DTO
                var updateDto = _mapper.Map<HerbUpdateDto>(updateInfo);
                
                // API调用
                var apiResult = await _apiService.UpdateAsync(updateDto);
                if (!apiResult.IsSuccess || apiResult.Data == null)
                {
                    return ServiceResult<HerbInfo>.Failure(
                        apiResult.ErrorMessage ?? "更新中药材失败");
                }
                
                // UltraThink四层架构：使用AutoMapper转换 DTO → Info
                var herbInfo = _mapper.Map<HerbInfo>(apiResult.Data);
                return ServiceResult<HerbInfo>.Success(herbInfo);
            }
            catch (Exception ex)
            {
                return ServiceResult<HerbInfo>.Failure($"更新中药材异常: {ex.Message}");
            }
        }
        
        public async Task<ServiceResult> DeleteAsync(Guid id)
        {
            try
            {
                if (id == Guid.Empty)
                {
                    return ServiceResult.Failure("中药材ID不能为空");
                }
                
                var apiResult = await _apiService.DeleteAsync(id);
                if (!apiResult.IsSuccess)
                {
                    return ServiceResult.Failure(apiResult.ErrorMessage ?? "删除中药材失败");
                }
                
                return ServiceResult.Success();
            }
            catch (Exception ex)
            {
                return ServiceResult.Failure($"删除中药材异常: {ex.Message}");
            }
        }
        
        #endregion
        
        #region 业务特定操作
        
        public async Task<ServiceResult<PagedResult<HerbInfo>>> SearchHerbsAsync(PagedQueryBaseDto request)
        {
            try
            {
                // 使用GetPagedAsync实现搜索功能
                return await GetPagedAsync(request);
            }
            catch (Exception ex)
            {
                return ServiceResult<PagedResult<HerbInfo>>.Failure($"搜索中药材异常: {ex.Message}");
            }
        }
        
        public async Task<ServiceResult<HerbInfo>> GetByNameAsync(string name)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(name))
                {
                    return ServiceResult<HerbInfo>.Failure("中药材名称不能为空");
                }
                
                var query = new PagedQueryBaseDto
                {
                    PageIndex = 1,
                    PageSize = 1,
                    Keyword = name
                };
                
                var result = await GetPagedAsync(query);
                if (!result.IsSuccess)
                {
                    return ServiceResult<HerbInfo>.Failure(result.ErrorMessage);
                }
                
                var herb = result.Data.Items.FirstOrDefault(h => h.Name == name);
                if (herb == null)
                {
                    return ServiceResult<HerbInfo>.Failure("未找到指定中药材");
                }
                
                return ServiceResult<HerbInfo>.Success(herb);
            }
            catch (Exception ex)
            {
                return ServiceResult<HerbInfo>.Failure($"根据名称获取中药材异常: {ex.Message}");
            }
        }
        
        public async Task<ServiceResult> ValidateAsync(HerbInfo herbInfo)
        {
            try
            {
                if (herbInfo == null)
                {
                    return ServiceResult.Failure("中药材信息不能为空");
                }
                
                if (string.IsNullOrWhiteSpace(herbInfo.Name))
                {
                    return ServiceResult.Failure("中药材名称不能为空");
                }
                
                if (herbInfo.Name.Length > 100)
                {
                    return ServiceResult.Failure("中药材名称长度不能超过100个字符");
                }
                
                if (string.IsNullOrWhiteSpace(herbInfo.Unit))
                {
                    return ServiceResult.Failure("单位不能为空");
                }
                
                if (herbInfo.Unit.Length > 10)
                {
                    return ServiceResult.Failure("单位长度不能超过10个字符");
                }
                
                if (herbInfo.Price <= 0)
                {
                    return ServiceResult.Failure("单价必须大于0");
                }
                
                if (herbInfo.Price > 9999.99m)
                {
                    return ServiceResult.Failure("单价不能超过9999.99");
                }
                
                if (herbInfo.Stock < 0)
                {
                    return ServiceResult.Failure("库存数量不能为负数");
                }
                
                return ServiceResult.Success();
            }
            catch (Exception ex)
            {
                return ServiceResult.Failure($"验证中药材信息异常: {ex.Message}");
            }
        }
        
        public async Task<ServiceResult<bool>> IsNameExistsAsync(string name, Guid? excludeId = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(name))
                {
                    return ServiceResult<bool>.Success(false);
                }
                
                var herbResult = await GetByNameAsync(name);
                if (!herbResult.IsSuccess)
                {
                    // 如果找不到中药材，说明名称不存在
                    return ServiceResult<bool>.Success(false);
                }
                
                var exists = excludeId == null || herbResult.Data.Id != excludeId.Value;
                return ServiceResult<bool>.Success(exists);
            }
            catch (Exception ex)
            {
                return ServiceResult<bool>.Failure($"检查中药材名称异常: {ex.Message}");
            }
        }
        
        #endregion
        
        #region 状态管理
        
        public async Task<ServiceResult> EnableAsync(Guid id)
        {
            try
            {
                if (id == Guid.Empty)
                {
                    return ServiceResult.Failure("中药材ID不能为空");
                }
                
                var apiResult = await _apiService.EnableAsync(id);
                if (!apiResult.IsSuccess)
                {
                    return ServiceResult.Failure(apiResult.ErrorMessage ?? "启用中药材失败");
                }
                
                return ServiceResult.Success();
            }
            catch (Exception ex)
            {
                return ServiceResult.Failure($"启用中药材异常: {ex.Message}");
            }
        }
        
        public async Task<ServiceResult> DisableAsync(Guid id)
        {
            try
            {
                if (id == Guid.Empty)
                {
                    return ServiceResult.Failure("中药材ID不能为空");
                }
                
                var apiResult = await _apiService.DisableAsync(id);
                if (!apiResult.IsSuccess)
                {
                    return ServiceResult.Failure(apiResult.ErrorMessage ?? "禁用中药材失败");
                }
                
                return ServiceResult.Success();
            }
            catch (Exception ex)
            {
                return ServiceResult.Failure($"禁用中药材异常: {ex.Message}");
            }
        }
        
        public async Task<ServiceResult<int>> BatchUpdateStatusAsync(IEnumerable<Guid> ids, bool isEnabled, string reason = "")
        {
            try
            {
                if (ids == null || !ids.Any())
                {
                    return ServiceResult<int>.Failure("中药材ID列表不能为空");
                }
                
                var apiResult = await _apiService.BatchUpdateStatusAsync(ids, isEnabled, reason);
                if (!apiResult.IsSuccess)
                {
                    return ServiceResult<int>.Failure(apiResult.ErrorMessage ?? "批量更新状态失败");
                }
                
                return ServiceResult<int>.Success(apiResult.Data);
            }
            catch (Exception ex)
            {
                return ServiceResult<int>.Failure($"批量更新状态异常: {ex.Message}");
            }
        }
        
        #endregion
        
        #region 库存管理
        
        public async Task<ServiceResult> UpdateStockAsync(Guid id, decimal newStock, string reason = "")
        {
            try
            {
                if (id == Guid.Empty)
                {
                    return ServiceResult.Failure("中药材ID不能为空");
                }
                
                if (newStock < 0)
                {
                    return ServiceResult.Failure("库存数量不能为负数");
                }
                
                // 这里应该调用API的库存更新接口
                // 目前模拟实现
                return ServiceResult.Success();
            }
            catch (Exception ex)
            {
                return ServiceResult.Failure($"更新库存异常: {ex.Message}");
            }
        }
        
        public async Task<ServiceResult<int>> BatchUpdateStockAsync(IEnumerable<(Guid Id, decimal Stock)> stockUpdates, string reason = "")
        {
            try
            {
                if (stockUpdates == null || !stockUpdates.Any())
                {
                    return ServiceResult<int>.Failure("库存更新列表不能为空");
                }
                
                // 这里应该调用API的批量库存更新接口
                // 目前模拟实现
                return ServiceResult<int>.Success(stockUpdates.Count());
            }
            catch (Exception ex)
            {
                return ServiceResult<int>.Failure($"批量更新库存异常: {ex.Message}");
            }
        }
        
        public async Task<ServiceResult<IEnumerable<HerbInfo>>> GetLowStockHerbsAsync(decimal threshold = 10)
        {
            try
            {
                var query = new PagedQueryBaseDto
                {
                    PageIndex = 1,
                    PageSize = 100 // 获取足够多的数据进行筛选
                };
                
                var result = await GetPagedAsync(query);
                if (!result.IsSuccess)
                {
                    return ServiceResult<IEnumerable<HerbInfo>>.Failure(result.ErrorMessage);
                }
                
                var lowStockHerbs = result.Data.Items.Where(h => h.Stock <= threshold);
                return ServiceResult<IEnumerable<HerbInfo>>.Success(lowStockHerbs);
            }
            catch (Exception ex)
            {
                return ServiceResult<IEnumerable<HerbInfo>>.Failure($"获取库存不足中药材异常: {ex.Message}");
            }
        }
        
        #endregion
        
        #region 分类和统计
        
        public async Task<ServiceResult<IEnumerable<string>>> GetCategoriesAsync()
        {
            try
            {
                // 获取所有中药材进行分类统计
                var allHerbsResult = await GetPagedAsync(new PagedQueryBaseDto 
                { 
                    PageIndex = 1, 
                    PageSize = 10000 // 获取足够多的数据进行统计
                });
                
                if (!allHerbsResult.IsSuccess)
                {
                    return ServiceResult<IEnumerable<string>>.Failure(allHerbsResult.ErrorMessage);
                }
                
                var categories = allHerbsResult.Data.Items
                    .Where(h => !string.IsNullOrEmpty(h.Category))
                    .Select(h => h.Category!)
                    .Distinct()
                    .OrderBy(c => c);
                
                return ServiceResult<IEnumerable<string>>.Success(categories);
            }
            catch (Exception ex)
            {
                return ServiceResult<IEnumerable<string>>.Failure($"获取分类列表异常: {ex.Message}");
            }
        }
        
        public async Task<ServiceResult<PagedResult<HerbInfo>>> GetByCategoryAsync(string category, PagedQueryBaseDto query)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(category))
                {
                    return ServiceResult<PagedResult<HerbInfo>>.Failure("分类不能为空");
                }
                
                // 这里应该调用API的分类查询接口
                // 目前使用简单的搜索实现
                var searchQuery = new PagedQueryBaseDto
                {
                    PageIndex = query.PageIndex,
                    PageSize = query.PageSize,
                    Keyword = category
                };
                
                var result = await GetPagedAsync(searchQuery);
                if (!result.IsSuccess)
                {
                    return ServiceResult<PagedResult<HerbInfo>>.Failure(result.ErrorMessage);
                }
                
                // 过滤出指定分类的中药材
                var categoryHerbs = result.Data.Items.Where(h => h.Category == category).ToList();
                var filteredResult = new PagedResult<HerbInfo>(
                    categoryHerbs,
                    categoryHerbs.Count,
                    query.PageIndex,
                    query.PageSize);
                
                return ServiceResult<PagedResult<HerbInfo>>.Success(filteredResult);
            }
            catch (Exception ex)
            {
                return ServiceResult<PagedResult<HerbInfo>>.Failure($"根据分类获取中药材异常: {ex.Message}");
            }
        }
        
        public async Task<ServiceResult<HerbStatisticsInfo>> GetStatisticsAsync()
        {
            try
            {
                // 获取中药材总数据进行统计
                var allHerbsResult = await GetPagedAsync(new PagedQueryBaseDto 
                { 
                    PageIndex = 1, 
                    PageSize = 10000 // 获取足够多的数据进行统计
                });
                
                if (!allHerbsResult.IsSuccess)
                {
                    return ServiceResult<HerbStatisticsInfo>.Failure(allHerbsResult.ErrorMessage);
                }
                
                var herbs = allHerbsResult.Data.Items;
                
                var statistics = new HerbStatisticsInfo
                {
                    TotalCount = herbs.Count,
                    EnabledCount = herbs.Count(h => h.Status == CommonStatus.Enabled),
                    DisabledCount = herbs.Count(h => h.Status != CommonStatus.Enabled),
                    LowStockCount = herbs.Count(h => h.Stock <= 10),
                    OutOfStockCount = herbs.Count(h => h.Stock <= 0),
                    TotalValue = herbs.Sum(h => h.Price * h.Stock),
                    CategoryCounts = herbs.Where(h => !string.IsNullOrEmpty(h.Category))
                                        .GroupBy(h => h.Category!)
                                        .ToDictionary(g => g.Key, g => g.Count()),
                    CategoryValues = herbs.Where(h => !string.IsNullOrEmpty(h.Category))
                                         .GroupBy(h => h.Category!)
                                         .ToDictionary(g => g.Key, g => g.Sum(h => h.Price * h.Stock)),
                    LastUpdateTime = DateTime.Now,
                    MostExpensiveHerb = herbs.OrderByDescending(h => h.Price).FirstOrDefault()?.Name,
                    MostPopularCategory = herbs.Where(h => !string.IsNullOrEmpty(h.Category))
                                              .GroupBy(h => h.Category!)
                                              .OrderByDescending(g => g.Count())
                                              .FirstOrDefault()?.Key
                };
                
                return ServiceResult<HerbStatisticsInfo>.Success(statistics);
            }
            catch (Exception ex)
            {
                return ServiceResult<HerbStatisticsInfo>.Failure($"获取中药材统计信息异常: {ex.Message}");
            }
        }
        
        public async Task<ServiceResult<IEnumerable<HerbInfo>>> GetPopularHerbsAsync(int count = 10)
        {
            try
            {
                var query = new PagedQueryBaseDto
                {
                    PageIndex = 1,
                    PageSize = count * 2, // 获取更多数据以便筛选
                    SortField = "LastOperationTime",
                    SortDirection = "DESC"
                };
                
                var result = await GetPagedAsync(query);
                if (!result.IsSuccess)
                {
                    return ServiceResult<IEnumerable<HerbInfo>>.Failure(result.ErrorMessage);
                }
                
                var popularHerbs = result.Data.Items
                    .Where(h => h.Status == CommonStatus.Enabled)
                    .OrderByDescending(h => h.LastOperationTime ?? DateTime.MinValue)
                    .Take(count);
                
                return ServiceResult<IEnumerable<HerbInfo>>.Success(popularHerbs);
            }
            catch (Exception ex)
            {
                return ServiceResult<IEnumerable<HerbInfo>>.Failure($"获取热门中药材异常: {ex.Message}");
            }
        }
        
        #endregion
        
        #region 导入导出功能
        
        public async Task<ServiceResult<IEnumerable<HerbInfo>>> ImportAsync(string filePath)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(filePath))
                {
                    return ServiceResult<IEnumerable<HerbInfo>>.Failure("文件路径不能为空");
                }
                
                // TODO: 实现实际的导入逻辑
                // 这里是预留功能，返回空列表表示功能开发中
                return ServiceResult<IEnumerable<HerbInfo>>.Success(new List<HerbInfo>());
            }
            catch (Exception ex)
            {
                return ServiceResult<IEnumerable<HerbInfo>>.Failure($"导入中药材数据异常: {ex.Message}");
            }
        }
        
        public async Task<ServiceResult> ExportAsync(IEnumerable<Guid> herbIds, string filePath)
        {
            try
            {
                if (herbIds == null || !herbIds.Any())
                {
                    return ServiceResult.Failure("导出的中药材ID列表不能为空");
                }
                
                if (string.IsNullOrWhiteSpace(filePath))
                {
                    return ServiceResult.Failure("导出文件路径不能为空");
                }
                
                // TODO: 实现实际的导出逻辑
                // 这里是预留功能，返回成功表示功能开发中
                return ServiceResult.Success();
            }
            catch (Exception ex)
            {
                return ServiceResult.Failure($"导出中药材数据异常: {ex.Message}");
            }
        }
        
        public async Task<ServiceResult> GenerateImportTemplateAsync(string filePath)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(filePath))
                {
                    return ServiceResult.Failure("模板文件路径不能为空");
                }
                
                // TODO: 实现实际的模板生成逻辑
                // 这里是预留功能，返回成功表示功能开发中
                return ServiceResult.Success();
            }
            catch (Exception ex)
            {
                return ServiceResult.Failure($"生成导入模板异常: {ex.Message}");
            }
        }
        
        #endregion
    }
}