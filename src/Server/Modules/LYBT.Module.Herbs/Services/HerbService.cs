using AutoMapper;
using LYBT.Entities.Herbs;
using LYBT.Infrastructure.Data;
using LYBT.Module.Herbs.Interfaces;
using LYBT.Shared.Interfaces.Services;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.Shared.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LYBT.Module.Herbs.Services
{
    /// <summary>
    /// 药材服务 - 统一服务实现
    /// 合并查询和业务逻辑，简化架构
    /// </summary>
    public class HerbService : IHerbService
    {
        private readonly IHerbRepository _repository;
        private readonly AppDbContext _context;
        private readonly IMapper _mapper;
        private readonly ILogger<HerbService> _logger;

        public HerbService(
            IHerbRepository repository,
            AppDbContext context,
            IMapper mapper,
            ILogger<HerbService> logger)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        #region Query Operations

        /// <inheritdoc/>
        public async Task<ServiceResult<HerbDto>> GetByIdAsync(Guid id)
        {
            try
            {
                var herb = await _repository.GetByIdAsync(id);
                if (herb == null)
                {
                    return ServiceResult<HerbDto>.Failure($"药材不存在: {id}");
                }

                var dto = _mapper.Map<HerbDto>(herb);
                return ServiceResult<HerbDto>.Success(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取药材详情失败: {HerbId}", id);
                return ServiceResult<HerbDto>.Failure($"获取药材详情失败: {ex.Message}");
            }
        }

        /// <inheritdoc/>
        public async Task<ServiceResult<PagedResult<HerbDto>>> GetPagedAsync(HerbSearchDto query)
        {
            try
            {
                var result = await _repository.GetPagedAsync(query);
                var dtos = _mapper.Map<List<HerbDto>>(result.Items);
                var pagedDtos = new PagedResult<HerbDto>(dtos, result.TotalCount, query.PageIndex, query.PageSize);
                return ServiceResult<PagedResult<HerbDto>>.Success(pagedDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "分页查询药材失败");
                return ServiceResult<PagedResult<HerbDto>>.Failure($"查询失败: {ex.Message}");
            }
        }

        /// <inheritdoc/>
        public async Task<ServiceResult<List<HerbDto>>> SearchAsync(string keyword)
        {
            try
            {
                var herbs = await _repository.SearchAsync(keyword, 50);
                var dtos = _mapper.Map<List<HerbDto>>(herbs);
                return ServiceResult<List<HerbDto>>.Success(dtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "搜索药材失败: {Keyword}", keyword);
                return ServiceResult<List<HerbDto>>.Failure($"搜索失败: {ex.Message}");
            }
        }

        /// <inheritdoc/>
        public async Task<ServiceResult<List<HerbDto>>> GetByIdsAsync(List<Guid> ids)
        {
            try
            {
                var herbs = await _repository.GetByIdsAsync(ids);
                var dtos = _mapper.Map<List<HerbDto>>(herbs);
                return ServiceResult<List<HerbDto>>.Success(dtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "批量获取药材失败");
                return ServiceResult<List<HerbDto>>.Failure($"批量获取失败: {ex.Message}");
            }
        }

        /// <inheritdoc/>
        public async Task<ServiceResult<List<HerbDto>>> GetByCategoryAsync(string category)
        {
            try
            {
                var herbs = await _repository.GetByCategoryAsync(category);
                var dtos = _mapper.Map<List<HerbDto>>(herbs);
                return ServiceResult<List<HerbDto>>.Success(dtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "按分类查询药材失败: {Category}", category);
                return ServiceResult<List<HerbDto>>.Failure($"查询失败: {ex.Message}");
            }
        }

        #endregion

        #region Business Operations

        /// <inheritdoc/>
        public async Task<ServiceResult<HerbDto>> CreateAsync(HerbCreateDto dto)
        {
            try
            {
                // 检查药材名称是否重复
                var exists = await _repository.ExistsByNameAsync(dto.Name);
                if (exists)
                {
                    return ServiceResult<HerbDto>.Failure($"药材名称已存在: {dto.Name}");
                }

                var herb = _mapper.Map<Herb>(dto);
                herb.Id = Guid.NewGuid();
                herb.Status = CommonStatus.Enabled;
                herb.CreatedAt = DateTime.Now;
                herb.UpdatedAt = DateTime.Now;

                await _repository.AddAsync(herb);

                var resultDto = _mapper.Map<HerbDto>(herb);
                return ServiceResult<HerbDto>.Success(resultDto, "创建药材成功");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "创建药材失败");
                return ServiceResult<HerbDto>.Failure($"创建失败: {ex.Message}");
            }
        }

        /// <inheritdoc/>
        public async Task<ServiceResult<HerbDto>> UpdateAsync(Guid id, HerbUpdateDto dto)
        {
            try
            {
                var herb = await _repository.GetByIdAsync(id);
                if (herb == null)
                {
                    return ServiceResult<HerbDto>.Failure($"药材不存在: {id}");
                }

                // 检查药材名称是否重复（排除自身）
                if (!string.IsNullOrEmpty(dto.Name) && dto.Name != herb.Name)
                {
                    var exists = await _repository.ExistsByNameAsync(dto.Name);
                    if (exists)
                    {
                        return ServiceResult<HerbDto>.Failure($"药材名称已存在: {dto.Name}");
                    }
                }

                _mapper.Map(dto, herb);
                herb.UpdatedAt = DateTime.Now;

                await _repository.UpdateAsync(herb);

                var resultDto = _mapper.Map<HerbDto>(herb);
                return ServiceResult<HerbDto>.Success(resultDto, "更新药材成功");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新药材失败: {HerbId}", id);
                return ServiceResult<HerbDto>.Failure($"更新失败: {ex.Message}");
            }
        }

        /// <inheritdoc/>
        public async Task<ServiceResult<bool>> DeleteAsync(Guid id)
        {
            try
            {
                var herb = await _repository.GetByIdAsync(id);
                if (herb == null)
                {
                    return ServiceResult<bool>.Failure($"药材不存在: {id}");
                }

                // 软删除
                herb.IsDeleted = true;
                herb.UpdatedAt = DateTime.Now;

                await _repository.UpdateAsync(herb);

                return ServiceResult<bool>.Success(true, "删除药材成功");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "删除药材失败: {HerbId}", id);
                return ServiceResult<bool>.Failure($"删除失败: {ex.Message}");
            }
        }

        /// <inheritdoc/>
        public async Task<ServiceResult> EnableAsync(Guid id)
        {
            try
            {
                var herb = await _repository.GetByIdAsync(id);
                if (herb == null)
                {
                    return ServiceResult.Failure($"药材不存在: {id}");
                }

                herb.Status = CommonStatus.Enabled;
                herb.UpdatedAt = DateTime.Now;

                await _repository.UpdateAsync(herb);

                return ServiceResult.Success("启用药材成功");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "启用药材失败: {HerbId}", id);
                return ServiceResult.Failure($"启用失败: {ex.Message}");
            }
        }

        /// <inheritdoc/>
        public async Task<ServiceResult> DisableAsync(Guid id)
        {
            try
            {
                var herb = await _repository.GetByIdAsync(id);
                if (herb == null)
                {
                    return ServiceResult.Failure($"药材不存在: {id}");
                }

                herb.Status = CommonStatus.Disabled;
                herb.UpdatedAt = DateTime.Now;

                await _repository.UpdateAsync(herb);

                return ServiceResult.Success("禁用药材成功");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "禁用药材失败: {HerbId}", id);
                return ServiceResult.Failure($"禁用失败: {ex.Message}");
            }
        }

        #endregion

        #region Batch Operations

        /// <inheritdoc/>
        public async Task<ServiceResult<object>> ImportHerbsAsync(List<HerbCreateDto> herbs)
        {
            try
            {
                var successCount = 0;
                var failedItems = new List<string>();

                foreach (var dto in herbs)
                {
                    try
                    {
                        // 检查是否已存在
                        var exists = await _repository.ExistsByNameAsync(dto.Name);
                        if (exists)
                        {
                            failedItems.Add($"{dto.Name} (已存在)");
                            continue;
                        }

                        var herb = _mapper.Map<Herb>(dto);
                        herb.Id = Guid.NewGuid();
                        herb.Status = CommonStatus.Enabled;
                        herb.CreatedAt = DateTime.Now;
                        herb.UpdatedAt = DateTime.Now;

                        await _repository.AddAsync(herb);
                        successCount++;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "导入药材失败: {HerbName}", dto.Name);
                        failedItems.Add($"{dto.Name} ({ex.Message})");
                    }
                }

                var result = new
                {
                    SuccessCount = successCount,
                    FailedCount = failedItems.Count,
                    FailedItems = failedItems
                };

                return successCount > 0
                    ? ServiceResult<object>.Success(result, $"导入完成，成功: {successCount}, 失败: {failedItems.Count}")
                    : ServiceResult<object>.Failure("导入失败，没有成功导入任何药材");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "批量导入药材失败");
                return ServiceResult<object>.Failure($"导入失败: {ex.Message}");
            }
        }

        /// <inheritdoc/>
        public async Task<ServiceResult<List<HerbDto>>> BatchCreateAsync(List<HerbCreateDto> dtos)
        {
            try
            {
                var createdHerbs = new List<Herb>();

                foreach (var dto in dtos)
                {
                    // 检查药材名称是否重复
                    var exists = await _repository.ExistsByNameAsync(dto.Name);
                    if (exists)
                    {
                        return ServiceResult<List<HerbDto>>.Failure($"药材名称已存在: {dto.Name}");
                    }

                    var herb = _mapper.Map<Herb>(dto);
                    herb.Id = Guid.NewGuid();
                    herb.Status = CommonStatus.Enabled;
                    herb.CreatedAt = DateTime.Now;
                    herb.UpdatedAt = DateTime.Now;

                    createdHerbs.Add(herb);
                }

                foreach (var herb in createdHerbs)
                {
                    await _repository.AddAsync(herb);
                }

                var resultDtos = _mapper.Map<List<HerbDto>>(createdHerbs);
                return ServiceResult<List<HerbDto>>.Success(resultDtos, $"批量创建 {createdHerbs.Count} 个药材成功");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "批量创建药材失败");
                return ServiceResult<List<HerbDto>>.Failure($"批量创建失败: {ex.Message}");
            }
        }

        /// <inheritdoc/>
        public async Task<ServiceResult<bool>> UpdatePriceAsync(Guid id, decimal price)
        {
            try
            {
                var herb = await _repository.GetByIdAsync(id);
                if (herb == null)
                {
                    return ServiceResult<bool>.Failure($"药材不存在: {id}");
                }

                herb.Price = price;
                herb.UpdatedAt = DateTime.Now;

                await _repository.UpdateAsync(herb);

                return ServiceResult<bool>.Success(true, $"药材价格更新为: {price:C}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新药材价格失败: {HerbId}", id);
                return ServiceResult<bool>.Failure($"更新价格失败: {ex.Message}");
            }
        }

        /// <inheritdoc/>
        public async Task<ServiceResult<byte[]>> ExportHerbsAsync(PagedQueryBaseDto query)
        {
            try
            {
                // 简单实现：返回CSV格式数据
                var herbs = await _repository.GetAllAsync();
                var csv = "名称,拼音码,功效,价格,单位,状态\n";

                foreach (var herb in herbs)
                {
                    csv += $"{herb.Name},{herb.PinYinCode},{herb.Effect}," +
                           $"{herb.Price},{herb.Unit},{herb.Status}\n";
                }

                var bytes = System.Text.Encoding.UTF8.GetBytes(csv);
                return ServiceResult<byte[]>.Success(bytes, "导出药材数据成功");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "导出药材数据失败");
                return ServiceResult<byte[]>.Failure($"导出失败: {ex.Message}");
            }
        }

        #endregion
    }
}