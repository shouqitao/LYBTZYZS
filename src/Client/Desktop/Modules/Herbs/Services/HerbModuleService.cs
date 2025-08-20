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
    public class HerbModuleService
    {
        private readonly IHerbApi _apiService;
        private readonly IMapper _mapper;
        
        public HerbModuleService(IHerbApi apiService, IMapper mapper)
        {
            _apiService = apiService ?? throw new ArgumentNullException(nameof(apiService));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        }
        
        #region 基础CRUD操作
        
        public async Task<ServiceResult<PagedResult<HerbDto>>> GetPagedAsync(PagedQueryBaseDto query)
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
        
        public async Task<ServiceResult<HerbDto>> UpdateAsync(HerbUpdateDto updateDto)
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
        
        public async Task<ServiceResult> DeleteAsync(Guid id)
        {
            try
            {
                if (id == Guid.Empty)
                {
                    return ServiceResult.Failure("中药材ID不能为空");
                }
                
                // UltraThink v2.0: 使用状态切换代替硬删除
                var apiResult = await _apiService.ToggleStatusAsync(id);
                if (!apiResult.IsSuccessStatusCode)
                {
                    return ServiceResult.Failure(apiResult.Error?.Message ?? "删除中药材失败");
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
        
        public async Task<ServiceResult<PagedResult<HerbDto>>> SearchHerbsAsync(PagedQueryBaseDto request)
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
                
                var query = new PagedQueryBaseDto
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
        
        public async Task<ServiceResult<int>> BatchUpdateStatusAsync(IEnumerable<Guid> ids, bool isEnabled, string reason = "")
        {
            try
            {
                if (ids == null || !ids.Any())
                {
                    return ServiceResult<int>.Failure("中药材ID列表不能为空");
                }
                
                // UltraThink v2.0: 由于CommonStatusUpdateDto只支持单个ID，需要循环处理
                var successCount = 0;
                var failedCount = 0;
                var errors = new List<string>();

                foreach (var id in ids)
                {
                    try
                    {
                        var statusUpdateDto = new CommonStatusUpdateDto
                        {
                            Id = id,
                            Status = isEnabled ? CommonStatus.Enabled : CommonStatus.Disabled,
                            IsEnabled = isEnabled,
                            Reason = reason
                        };
                        
                        var apiResult = await _apiService.UpdateStatusAsync(statusUpdateDto);
                        if (apiResult.IsSuccessStatusCode)
                        {
                            successCount++;
                        }
                        else
                        {
                            failedCount++;
                            errors.Add($"ID {id}: {apiResult.Error?.Message}");
                        }
                    }
                    catch (Exception ex)
                    {
                        failedCount++;
                        errors.Add($"ID {id}: {ex.Message}");
                    }
                }
                
                if (failedCount > 0 && successCount == 0)
                {
                    return ServiceResult<int>.Failure($"批量更新全部失败: {string.Join("; ", errors)}");
                }
                
                if (failedCount > 0)
                {
                    return ServiceResult<int>.Failure($"部分更新失败（成功: {successCount}, 失败: {failedCount}）: {string.Join("; ", errors)}");
                }
                
                return ServiceResult<int>.Success(successCount);
            }
            catch (Exception ex)
            {
                return ServiceResult<int>.Failure($"批量更新状态异常: {ex.Message}");
            }
        }
        
        #endregion
        
        // UltraThink v2.0: 移除库存管理功能 - 根据架构要求，Herbs模块只管理药材信息和单价，不涉及库存
        
                // UltraThink v2.0: 移除分类和统计功能 - 删除过度设计的分类统计功能，简化药材管理职责
        
                // UltraThink v2.0: 移除导入导出功能 - 删除过度设计的导入导出功能
    }
}