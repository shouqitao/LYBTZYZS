using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
// UltraThink v2.0: 移除Info模型引用，直接使用DTO
using LYBT.Desktop.Core.Models.Common;
using LYBT.Desktop.Modules.Herbs.Api;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.Shared.Models.Enums;

namespace LYBT.Desktop.Herbs.Services
{
    /// <summary>
    /// Herb模块核心业务服务实现
    /// UltraThink v2.0架构：直接使用DTO，专注处方用药管理，删除库存管理功能
    /// </summary>
    public class HerbModule : LYBT.Shared.Interfaces.Services.IHerbService
    {
        private readonly IHerbApi _apiService;
        private readonly IMapper _mapper;
        
        public HerbModule(IHerbApi apiService, IMapper mapper)
        {
            _apiService = apiService ?? throw new ArgumentNullException(nameof(apiService));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        }
        
        #region 基础CRUD操作
        
        public async Task<ServiceResult<PagedResult<HerbDto>>> GetPagedAsync(HerbPagedQueryDto query)
        {
            try
            {
                // UltraThink v2.0: 调用Refit API客户端
                var apiResponse = await _apiService.GetHerbsAsync(
                    query.PageIndex,
                    query.PageSize,
                    query.Keyword);
                    
                if (!apiResponse.IsSuccessStatusCode || apiResponse.Content == null)
                {
                    return ServiceResult<PagedResult<HerbDto>>.Failure("获取中药材列表失败");
                }
                
                // 转换Refit响应为标准格式
                var pagedData = apiResponse.Content;
                var result = new PagedResult<HerbDto>(
                    pagedData.Items.ToList(),
                    pagedData.TotalCount,
                    pagedData.CurrentPage,
                    pagedData.PageSize);
                
                return ServiceResult<PagedResult<HerbDto>>.Success(result);
            }
            catch (Exception ex)
            {
                return ServiceResult<PagedResult<HerbDto>>.Failure($"获取中药材列表异常: {ex.Message}");
            }
        }
        
        public async Task<ServiceResult<HerbDto>> GetByIdAsync(Guid id)
        {
            try
            {
                if (id == Guid.Empty)
                {
                    return ServiceResult<HerbDto>.Failure("中药材ID不能为空");
                }
                
                // UltraThink v2.0：API调用获取DTO
                var apiResult = await _apiService.GetHerbByIdAsync(id);
                if (!apiResult.IsSuccessStatusCode || apiResult.Content == null)
                {
                    return ServiceResult<HerbDto>.Failure(
                        apiResult.Error?.Message ?? "获取中药材详情失败");
                }
                
                // UltraThink v2.0: 使用AutoMapper转换HerbDetailDto -> HerbDto
                var herbDto = _mapper.Map<HerbDto>(apiResult.Content);
                return ServiceResult<HerbDto>.Success(herbDto);
            }
            catch (Exception ex)
            {
                return ServiceResult<HerbDto>.Failure($"获取中药材详情异常: {ex.Message}");
            }
        }
        
        public async Task<ServiceResult<HerbDto>> CreateAsync(HerbCreateDto createDto)
        {
            try
            {
                // UltraThink v2.0: 直接使用CreateDto进行业务验证
                var validationResult = await ValidateCreateDtoAsync(createDto);
                if (!validationResult.IsSuccess)
                {
                    return ServiceResult<HerbDto>.Failure(validationResult.ErrorMessage);
                }
                
                // 检查中药材名称是否已存在
                var nameExistsResult = await IsNameExistsAsync(createDto.Name);
                if (nameExistsResult.IsSuccess && nameExistsResult.Data)
                {
                    return ServiceResult<HerbDto>.Failure("该中药材名称已被使用");
                }
                
                // API调用
                var apiResult = await _apiService.CreateHerbAsync(createDto);
                if (!apiResult.IsSuccessStatusCode || apiResult.Content == null)
                {
                    return ServiceResult<HerbDto>.Failure(
                        apiResult.Error?.Message ?? "创建中药材失败");
                }
                
                // UltraThink v2.0: 直接返回DTO
                return ServiceResult<HerbDto>.Success(apiResult.Content);
            }
            catch (Exception ex)
            {
                return ServiceResult<HerbDto>.Failure($"创建中药材异常: {ex.Message}");
            }
        }
        
        public async Task<ServiceResult<HerbDto>> UpdateAsync(Guid id, HerbUpdateDto updateDto)
        {
            try
            {
                // UltraThink v2.0: 直接使用UpdateDto进行业务验证
                var validationResult = await ValidateUpdateDtoAsync(updateDto);
                if (!validationResult.IsSuccess)
                {
                    return ServiceResult<HerbDto>.Failure(validationResult.ErrorMessage);
                }
                
                // 检查中药材名称是否已被其他中药材使用
                var nameExistsResult = await IsNameExistsAsync(updateDto.Name, updateDto.Id);
                if (nameExistsResult.IsSuccess && nameExistsResult.Data)
                {
                    return ServiceResult<HerbDto>.Failure("该中药材名称已被其他中药材使用");
                }
                
                // API调用
                var apiResult = await _apiService.UpdateHerbAsync(updateDto.Id, updateDto);
                if (!apiResult.IsSuccessStatusCode || apiResult.Content == null)
                {
                    return ServiceResult<HerbDto>.Failure(
                        apiResult.Error?.Message ?? "更新中药材失败");
                }
                
                // UltraThink v2.0: 直接返回DTO
                return ServiceResult<HerbDto>.Success(apiResult.Content);
            }
            catch (Exception ex)
            {
                return ServiceResult<HerbDto>.Failure($"更新中药材异常: {ex.Message}");
            }
        }
        
        public async Task<ServiceResult<bool>> DeleteAsync(Guid id)
        {
            try
            {
                if (id == Guid.Empty)
                {
                    return ServiceResult<bool>.Failure("中药材ID不能为空");
                }
                
                // UltraThink v2.0: 使用状态切换代替硬删除
                var apiResult = await _apiService.ToggleStatusAsync(id);
                if (!apiResult.IsSuccessStatusCode)
                {
                    return ServiceResult<bool>.Failure(apiResult.Error?.Message ?? "删除中药材失败");
                }
                
                return ServiceResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                return ServiceResult<bool>.Failure($"删除中药材异常: {ex.Message}");
            }
        }
        
        #endregion
        
        #region 业务特定操作
        
        public async Task<ServiceResult<PagedResult<HerbDto>>> SearchHerbsAsync(HerbPagedQueryDto request)
        {
            try
            {
                // 使用GetPagedAsync实现搜索功能
                return await GetPagedAsync(request);
            }
            catch (Exception ex)
            {
                return ServiceResult<PagedResult<HerbDto>>.Failure($"搜索中药材异常: {ex.Message}");
            }
        }
        
        public async Task<ServiceResult<HerbDto>> GetByNameAsync(string name)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(name))
                {
                    return ServiceResult<HerbDto>.Failure("中药材名称不能为空");
                }
                
                var query = new HerbPagedQueryDto
                {
                    PageIndex = 1,
                    PageSize = 1,
                    Keyword = name
                };
                
                var result = await GetPagedAsync(query);
                if (!result.IsSuccess)
                {
                    return ServiceResult<HerbDto>.Failure(result.ErrorMessage);
                }
                
                var herb = result.Data.Items.FirstOrDefault(h => h.Name == name);
                if (herb == null)
                {
                    return ServiceResult<HerbDto>.Failure("未找到指定中药材");
                }
                
                return ServiceResult<HerbDto>.Success(herb);
            }
            catch (Exception ex)
            {
                return ServiceResult<HerbDto>.Failure($"根据名称获取中药材异常: {ex.Message}");
            }
        }
        
        // UltraThink v2.0: 为CreateDto和UpdateDto创建单独的验证方法
        public async Task<ServiceResult> ValidateCreateDtoAsync(HerbCreateDto createDto)
        {
            try
            {
                if (createDto == null)
                {
                    return ServiceResult.Failure("创建中药材信息不能为空");
                }
                
                if (string.IsNullOrWhiteSpace(createDto.Name))
                {
                    return ServiceResult.Failure("中药材名称不能为空");
                }
                
                if (createDto.Name.Length > 100)
                {
                    return ServiceResult.Failure("中药材名称长度不能超过100个字符");
                }
                
                if (string.IsNullOrWhiteSpace(createDto.Unit))
                {
                    return ServiceResult.Failure("单位不能为空");
                }
                
                if (createDto.Unit.Length > 10)
                {
                    return ServiceResult.Failure("单位长度不能超过10个字符");
                }
                
                if (createDto.Price <= 0)
                {
                    return ServiceResult.Failure("单价必须大于0");
                }
                
                if (createDto.Price > 9999.99m)
                {
                    return ServiceResult.Failure("单价不能超过9999.99");
                }
                
                // TODO: 根据实际HerbCreateDto结构验证其他属性
                
                return ServiceResult.Success();
            }
            catch (Exception ex)
            {
                return ServiceResult.Failure($"验证创建中药材异常: {ex.Message}");
            }
        }
        
        public async Task<ServiceResult> ValidateUpdateDtoAsync(HerbUpdateDto updateDto)
        {
            try
            {
                if (updateDto == null)
                {
                    return ServiceResult.Failure("更新中药材信息不能为空");
                }
                
                if (string.IsNullOrWhiteSpace(updateDto.Name))
                {
                    return ServiceResult.Failure("中药材名称不能为空");
                }
                
                if (updateDto.Name.Length > 100)
                {
                    return ServiceResult.Failure("中药材名称长度不能超过100个字符");
                }
                
                // TODO: 根据实际HerbUpdateDto结构验证其他属性
                
                return ServiceResult.Success();
            }
            catch (Exception ex)
            {
                return ServiceResult.Failure($"验证更新中药材异常: {ex.Message}");
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
                
                var apiResult = await _apiService.ToggleStatusAsync(id);
                if (!apiResult.IsSuccessStatusCode)
                {
                    return ServiceResult.Failure(apiResult.Error?.Message ?? "启用中药材失败");
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
                
                var apiResult = await _apiService.ToggleStatusAsync(id);
                if (!apiResult.IsSuccessStatusCode)
                {
                    return ServiceResult.Failure(apiResult.Error?.Message ?? "禁用中药材失败");
                }
                
                return ServiceResult.Success();
            }
            catch (Exception ex)
            {
                return ServiceResult.Failure($"禁用中药材异常: {ex.Message}");
            }
        }
        
        // UltraThink v2.0: 删除批量状态更新功能 - 20人以下小诊所不需要复杂的批量操作
        // 小诊所的药材数量有限，通过单个状态切换即可满足需求
        
        #endregion
        
        // UltraThink v2.0: 移除库存管理功能 - 根据架构要求，Herbs模块只管理药材信息和单价，不涉及库存
        
        // UltraThink v2.0: 移除分类和统计功能 - 删除过度设计的分类统计功能，简化药材管理职责
        
        #region 基础数据导入导出功能 - UltraThink精简版保留
        
        /// <summary>
        /// 批量导入药材数据 - 基础数据功能保留
        /// </summary>
        public async Task<ServiceResult<int>> ImportHerbsAsync(List<HerbImportDto> herbs)
        {
            try
            {
                if (herbs == null || !herbs.Any())
                {
                    return ServiceResult<int>.Failure("导入药材列表不能为空");
                }

                // API调用批量导入
                var apiResponse = await _apiService.ImportHerbsAsync(herbs);
                if (!apiResponse.IsSuccessStatusCode || apiResponse.Content == null)
                {
                    return ServiceResult<int>.Failure("批量导入药材失败");
                }

                return ServiceResult<int>.Success(apiResponse.Content);
            }
            catch (Exception ex)
            {
                return ServiceResult<int>.Failure($"批量导入药材异常: {ex.Message}");
            }
        }

        /// <summary>
        /// 导出药材数据 - 基础数据功能保留
        /// </summary>
        public async Task<ServiceResult<List<HerbDto>>> ExportHerbsAsync()
        {
            try
            {
                // API调用导出
                var apiResponse = await _apiService.ExportHerbsAsync();
                if (!apiResponse.IsSuccessStatusCode || apiResponse.Content == null)
                {
                    return ServiceResult<List<HerbDto>>.Failure("导出药材数据失败");
                }

                // 使用AutoMapper将HerbDetailDto转换为HerbDto
                var herbDtos = _mapper.Map<List<HerbDto>>(apiResponse.Content);
                return ServiceResult<List<HerbDto>>.Success(herbDtos);
            }
            catch (Exception ex)
            {
                return ServiceResult<List<HerbDto>>.Failure($"导出药材数据异常: {ex.Message}");
            }
        }

        /// <summary>
        /// 获取药材导入模板 - 基础数据功能保留 (拼音码自动生成)
        /// </summary>
        public async Task<ServiceResult<byte[]>> GetImportTemplateAsync()
        {
            try
            {
                // API调用获取导入模板
                var apiResponse = await _apiService.GetImportTemplateAsync();
                if (!apiResponse.IsSuccessStatusCode || apiResponse.Content == null)
                {
                    return ServiceResult<byte[]>.Failure("获取药材导入模板失败");
                }

                return ServiceResult<byte[]>.Success(apiResponse.Content);
            }
            catch (Exception ex)
            {
                return ServiceResult<byte[]>.Failure($"获取药材导入模板异常: {ex.Message}");
            }
        }
        
        #endregion
        
        #region IHerbService接口实现 - 缺失方法补充
        
        /// <summary>
        /// 获取所有中药材
        /// </summary>
        public async Task<ServiceResult<List<HerbDto>>> GetAllAsync()
        {
            try
            {
                var query = new HerbPagedQueryDto { PageSize = int.MaxValue, PageIndex = 1 };
                var result = await GetPagedAsync(query);
                if (!result.IsSuccess)
                {
                    return ServiceResult<List<HerbDto>>.Failure(result.ErrorMessage);
                }
                
                return ServiceResult<List<HerbDto>>.Success(result.Data.Items);
            }
            catch (Exception ex)
            {
                return ServiceResult<List<HerbDto>>.Failure($"获取所有中药材异常: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 根据ID列表获取中药材
        /// </summary>
        public async Task<ServiceResult<List<HerbDto>>> GetByIdsAsync(List<Guid> ids)
        {
            try
            {
                if (ids == null || !ids.Any())
                {
                    return ServiceResult<List<HerbDto>>.Success(new List<HerbDto>());
                }
                
                var allHerbs = await GetAllAsync();
                if (!allHerbs.IsSuccess)
                {
                    return ServiceResult<List<HerbDto>>.Failure(allHerbs.ErrorMessage);
                }
                
                var filteredHerbs = allHerbs.Data.Where(h => ids.Contains(h.Id)).ToList();
                return ServiceResult<List<HerbDto>>.Success(filteredHerbs);
            }
            catch (Exception ex)
            {
                return ServiceResult<List<HerbDto>>.Failure($"根据ID列表获取中药材异常: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 更新库存 - UltraThink v2.0暂不支持库存管理
        /// </summary>
        public async Task<ServiceResult<bool>> UpdateStockAsync(Guid id, HerbStockUpdateDto dto)
        {
            return ServiceResult<bool>.Failure("UltraThink v2.0版本暂不支持库存管理功能");
        }
        
        /// <summary>
        /// 更新价格
        /// </summary>
        public async Task<ServiceResult<bool>> UpdatePriceAsync(Guid id, HerbPriceUpdateDto dto)
        {
            try
            {
                if (id == Guid.Empty)
                {
                    return ServiceResult<bool>.Failure("中药材ID不能为空");
                }
                
                if (dto.Price <= 0)
                {
                    return ServiceResult<bool>.Failure("价格必须大于0");
                }
                
                // 获取现有药材信息
                var herbResult = await GetByIdAsync(id);
                if (!herbResult.IsSuccess)
                {
                    return ServiceResult<bool>.Failure("获取中药材信息失败");
                }
                
                // 创建更新DTO
                var updateDto = new HerbUpdateDto
                {
                    Id = id,
                    Name = herbResult.Data.Name,
                    Price = dto.Price ?? 0m,
                    Unit = herbResult.Data.Unit
                };
                
                var updateResult = await UpdateAsync(id, updateDto);
                return ServiceResult<bool>.Success(updateResult.IsSuccess);
            }
            catch (Exception ex)
            {
                return ServiceResult<bool>.Failure($"更新价格异常: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 获取库存统计 - UltraThink v2.0暂不支持库存管理
        /// </summary>
        public async Task<ServiceResult<HerbStockStatisticsDto>> GetStockStatisticsAsync()
        {
            return ServiceResult<HerbStockStatisticsDto>.Failure("UltraThink v2.0版本暂不支持库存管理功能");
        }
        
        /// <summary>
        /// 搜索中药材
        /// </summary>
        public async Task<ServiceResult<List<HerbDto>>> SearchAsync(string keyword)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(keyword))
                {
                    return ServiceResult<List<HerbDto>>.Success(new List<HerbDto>());
                }
                
                var query = new HerbPagedQueryDto 
                { 
                    PageSize = int.MaxValue, 
                    PageIndex = 1,
                    Keyword = keyword 
                };
                
                var result = await GetPagedAsync(query);
                if (!result.IsSuccess)
                {
                    return ServiceResult<List<HerbDto>>.Failure(result.ErrorMessage);
                }
                
                return ServiceResult<List<HerbDto>>.Success(result.Data.Items);
            }
            catch (Exception ex)
            {
                return ServiceResult<List<HerbDto>>.Failure($"搜索中药材异常: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 批量更新状态 - UltraThink v2.0简化版不支持
        /// </summary>
        public async Task<ServiceResult<bool>> BatchUpdateStatusAsync(BatchStatusUpdateDto dto)
        {
            return ServiceResult<bool>.Failure("UltraThink v2.0版本暂不支持批量状态更新功能");
        }
        
        /// <summary>
        /// 获取中药材列表
        /// </summary>
        public async Task<ServiceResult<List<HerbDto>>> GetHerbsAsync()
        {
            return await GetAllAsync();
        }
        
        /// <summary>
        /// 获取列表（可选查询条件）
        /// </summary>
        public async Task<ServiceResult<List<HerbDto>>> GetListAsync(HerbPagedQueryDto? query = null)
        {
            try
            {
                if (query == null)
                {
                    return await GetAllAsync();
                }
                
                var result = await GetPagedAsync(query);
                if (!result.IsSuccess)
                {
                    return ServiceResult<List<HerbDto>>.Failure(result.ErrorMessage);
                }
                
                return ServiceResult<List<HerbDto>>.Success(result.Data.Items);
            }
            catch (Exception ex)
            {
                return ServiceResult<List<HerbDto>>.Failure($"获取中药材列表异常: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 获取可用中药材
        /// </summary>
        public async Task<ServiceResult<List<HerbDto>>> GetAvailableHerbsAsync()
        {
            try
            {
                var query = new HerbPagedQueryDto 
                { 
                    PageSize = int.MaxValue, 
                    PageIndex = 1
                };
                
                var result = await GetPagedAsync(query);
                if (!result.IsSuccess)
                {
                    return ServiceResult<List<HerbDto>>.Failure(result.ErrorMessage);
                }
                
                var availableHerbs = result.Data.Items
                    .Where(h => h.IsEnabled && h.Status == CommonStatus.Enabled)
                    .ToList();
                
                return ServiceResult<List<HerbDto>>.Success(availableHerbs);
            }
            catch (Exception ex)
            {
                return ServiceResult<List<HerbDto>>.Failure($"获取可用中药材异常: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 获取库存不足药材 - UltraThink v2.0暂不支持库存管理
        /// </summary>
        public async Task<ServiceResult<List<HerbDto>>> GetOutOfStockHerbsAsync()
        {
            return ServiceResult<List<HerbDto>>.Success(new List<HerbDto>());
        }
        
        /// <summary>
        /// 获取即将过期药材 - UltraThink v2.0暂不支持过期管理
        /// </summary>
        public async Task<ServiceResult<List<HerbDto>>> GetExpiringHerbsAsync(int days = 30)
        {
            return ServiceResult<List<HerbDto>>.Success(new List<HerbDto>());
        }
        
        /// <summary>
        /// 获取统计信息
        /// </summary>
        public async Task<ServiceResult<Dictionary<int, int>>> GetStatisticsAsync()
        {
            try
            {
                var allHerbs = await GetAllAsync();
                if (!allHerbs.IsSuccess)
                {
                    return ServiceResult<Dictionary<int, int>>.Failure(allHerbs.ErrorMessage);
                }
                
                var stats = new Dictionary<int, int>
                {
                    { 1, allHerbs.Data.Count }, // 总数
                    { 2, allHerbs.Data.Count(h => h.IsEnabled) }, // 可用数量
                    { 3, allHerbs.Data.Count(h => !h.IsEnabled) } // 禁用数量
                };
                
                return ServiceResult<Dictionary<int, int>>.Success(stats);
            }
            catch (Exception ex)
            {
                return ServiceResult<Dictionary<int, int>>.Failure($"获取统计信息异常: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 根据名称搜索中药材
        /// </summary>
        public async Task<ServiceResult<List<HerbDto>>> SearchByNameAsync(string name)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(name))
                {
                    return ServiceResult<List<HerbDto>>.Success(new List<HerbDto>());
                }
                
                var query = new HerbPagedQueryDto 
                { 
                    PageSize = int.MaxValue, 
                    PageIndex = 1,
                    Name = name
                };
                
                var result = await GetPagedAsync(query);
                if (!result.IsSuccess)
                {
                    return ServiceResult<List<HerbDto>>.Failure(result.ErrorMessage);
                }
                
                return ServiceResult<List<HerbDto>>.Success(result.Data.Items);
            }
            catch (Exception ex)
            {
                return ServiceResult<List<HerbDto>>.Failure($"根据名称搜索中药材异常: {ex.Message}");
            }
        }
        
        #endregion
    }
}