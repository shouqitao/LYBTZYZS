using System.Threading.Tasks;
using System.Linq;
using System;
using AutoMapper;
using LYBT.Infrastructure.Data;
using LYBT.Entities.Herbs;
using LYBT.Shared.Interfaces.Services;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Enums;
using LYBT.Module.Herbs.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace LYBT.Module.Herbs.Services
{

    /// <summary>
    /// 药材业务服务实现类（简化版）
    /// 只提供基础的药材信息维护功能，不包含库存管理
    /// </summary>
    public class HerbService : IHerbService
    {
        private readonly IHerbRepository _repository;
        private readonly IMapper _mapper;
        private readonly AppDbContext _context;

        /// <summary>
        /// 构造方法，注入仓储与对象映射
        /// </summary>
        public HerbService(IHerbRepository repository, IMapper mapper, AppDbContext context)
        {
            _repository = repository;
            _mapper = mapper;
            _context = context;
        }

        /// <summary>
        /// 简单的拼音码生成方法
        /// </summary>
        private static string GetSimplePinyinCode(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return string.Empty;
            // 简化实现：只取第一个字符的大写
            return name.Substring(0, Math.Min(name.Length, 1)).ToUpperInvariant();
        }

        /// <summary>
        /// 获取药材详情
        /// </summary>
        public async Task<ServiceResult<HerbDto>> GetByIdAsync(Guid id)
        {
            try
            {
                var model = await _repository.GetByIdAsync(id);
                if (model == null)
                    return ServiceResult<HerbDto>.Failure("药材不存在");

                var dto = _mapper.Map<HerbDto>(model);
                return ServiceResult<HerbDto>.Success(dto);
            }
            catch (Exception ex)
            {
                return ServiceResult<HerbDto>.Failure("获取药材详情失败", ex);
            }
        }

        /// <summary>
        /// 获取所有药材列表
        /// </summary>
        public async Task<ServiceResult<List<HerbDto>>> GetAllAsync()
        {
            try
            {
                var list = await _repository.GetAllAsync();
                var dtos = _mapper.Map<List<HerbDto>>(list);
                return ServiceResult<List<HerbDto>>.Success(dtos);
            }
            catch (Exception ex)
            {
                return ServiceResult<List<HerbDto>>.Failure("获取药材列表失败", ex);
            }
        }

        /// <summary>
        /// 分页查询药材
        /// </summary>
        public async Task<ServiceResult<PagedResult<HerbDto>>> GetPagedAsync(HerbPagedQueryDto query)
        {
            try
            {
                // 构建查询条件
            var dbQuery = _context.Herbs.AsQueryable();

            // 药材名称搜索
            if (!string.IsNullOrWhiteSpace(query.Name))
            {
                var keyword = query.Name.Trim();
                dbQuery = dbQuery.Where(h => h.Name.Contains(keyword));
            }

            // 拼音码搜索
            if (!string.IsNullOrWhiteSpace(query.PinYinCode))
            {
                var pinyin = query.PinYinCode.Trim().ToUpperInvariant();
                dbQuery = dbQuery.Where(h => h.PinYinCode != null && h.PinYinCode.Contains(pinyin));
            }

            // 产地搜索
            if (!string.IsNullOrWhiteSpace(query.Origin))
            {
                var origin = query.Origin.Trim();
                dbQuery = dbQuery.Where(h => h.Origin != null && h.Origin.Contains(origin));
            }

            // 规格搜索
            if (!string.IsNullOrWhiteSpace(query.Spec))
            {
                var spec = query.Spec.Trim();
                dbQuery = dbQuery.Where(h => h.Spec != null && h.Spec.Contains(spec));
            }

            // 价格范围筛选
            if (query.MinPrice.HasValue)
            {
                dbQuery = dbQuery.Where(h => h.Price >= query.MinPrice.Value);
            }
            if (query.MaxPrice.HasValue)
            {
                dbQuery = dbQuery.Where(h => h.Price <= query.MaxPrice.Value);
            }

            // 状态筛选
            if (query.Status.HasValue)
            {
                dbQuery = dbQuery.Where(h => h.Status == query.Status.Value);
            }
            else
            {
                // 默认只显示启用的药材
                dbQuery = dbQuery.Where(h => h.Status == CommonStatus.Enabled);
            }

            // 获取总数
            var total = await dbQuery.CountAsync();

            // 分页查询
            var models = await dbQuery
                .OrderBy(h => h.Name)
                .Skip((query.PageIndex - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToListAsync();

            var dtos = _mapper.Map<List<HerbDto>>(models);

            var result = new PagedResult<HerbDto>
            {
                TotalCount = total,
                Items = dtos,
                CurrentPage = query.PageIndex,
                PageSize = query.PageSize
            };
            
            return ServiceResult<PagedResult<HerbDto>>.Success(result);
            }
            catch (Exception ex)
            {
                return ServiceResult<PagedResult<HerbDto>>.Failure("分页查询药材失败", ex);
            }
        }

        /// <summary>
        /// 新增药材
        /// </summary>
        public async Task<ServiceResult<HerbDto>> CreateAsync(HerbCreateDto dto)
        {
            try
            {
                var model = _mapper.Map<HerbModel>(dto);
                model.Id = Guid.NewGuid();
                model.PinYinCode = string.IsNullOrWhiteSpace(dto.PinYinCode)
                    ? GetSimplePinyinCode(model.Name)
                    : dto.PinYinCode;
                model.Status = CommonStatus.Enabled; // 新增药材默认启用

                var result = await _repository.AddAsync(model);
                if (result == null)
                {
                    return ServiceResult<HerbDto>.Failure("新增药材失败");
                }

                var herbDto = _mapper.Map<HerbDto>(model);
                return ServiceResult<HerbDto>.Success(herbDto);
            }
            catch (Exception ex)
            {
                return ServiceResult<HerbDto>.Failure("新增药材失败", ex);
            }
        }

        /// <summary>
        /// 编辑药材信息
        /// </summary>
        public async Task<ServiceResult<HerbDto>> UpdateAsync(Guid id, HerbUpdateDto dto)
        {
            try
            {
                var model = await _repository.GetByIdAsync(id);
                if (model == null)
                    return ServiceResult<HerbDto>.Failure("药材不存在");

                // 更新基础信息
                model.Name = dto.Name;
                model.PinYinCode = string.IsNullOrWhiteSpace(dto.PinYinCode)
                    ? GetSimplePinyinCode(dto.Name)
                    : dto.PinYinCode;
                model.Origin = dto.Origin;
                model.Spec = dto.Spec;
                model.Unit = dto.Unit;
                model.Price = dto.Price;
                model.Effect = dto.Effect;
                model.Usage = dto.Usage;
                model.Remark = dto.Remark;
                model.Status = dto.Status;

                var result = await _repository.UpdateAsync(model);
                if (result == null)
                    return ServiceResult<HerbDto>.Failure("更新药材失败");

                var herbDto = _mapper.Map<HerbDto>(result);
                return ServiceResult<HerbDto>.Success(herbDto);
            }
            catch (Exception ex)
            {
                return ServiceResult<HerbDto>.Failure("更新药材失败", ex);
            }
        }

        /// <summary>
        /// 删除药材（软删除，设置IsActive=false）
        /// </summary>
        public async Task<ServiceResult<bool>> DeleteAsync(Guid id)
        {
            try
            {
                var model = await _repository.GetByIdAsync(id);
                if (model == null)
                    return ServiceResult<bool>.Failure("药材不存在");

                model.Status = CommonStatus.Disabled;

                var result = await _repository.UpdateAsync(model);
                return ServiceResult<bool>.Success(result != null);
            }
            catch (Exception ex)
            {
                return ServiceResult<bool>.Failure("删除药材失败", ex);
            }
        }

        /// <summary>
        /// 搜索药材（根据名称、拼音码）
        /// </summary>
        public async Task<ServiceResult<List<HerbDto>>> SearchAsync(string keyword)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(keyword))
                {
                    return ServiceResult<List<HerbDto>>.Success(new List<HerbDto>());
                }

                keyword = keyword.ToLower();
                var models = await _context.Herbs
                    .Where(h => h.Status == CommonStatus.Enabled && (
                        h.Name.ToLower().Contains(keyword) ||
                        (h.PinYinCode != null && h.PinYinCode.ToLower().Contains(keyword))
                    ))
                    .OrderBy(h => h.Name)
                    .Take(20)
                    .ToListAsync();

                var dtos = _mapper.Map<List<HerbDto>>(models);
                return ServiceResult<List<HerbDto>>.Success(dtos);
            }
            catch (Exception ex)
            {
                return ServiceResult<List<HerbDto>>.Failure("搜索药材失败", ex);
            }
        }

        /// <summary>
        /// 获取可用药材列表（状态为启用）
        /// </summary>
        public async Task<ServiceResult<List<HerbDto>>> GetAvailableHerbsAsync()
        {
            try
            {
                var models = await _context.Herbs
                    .Where(h => h.Status == CommonStatus.Enabled)
                    .OrderBy(h => h.Name)
                    .ToListAsync();

                var dtos = _mapper.Map<List<HerbDto>>(models);
                return ServiceResult<List<HerbDto>>.Success(dtos);
            }
            catch (Exception ex)
            {
                return ServiceResult<List<HerbDto>>.Failure("获取可用药材列表失败", ex);
            }
        }

        /// <summary>
        /// 根据ID列表获取药材
        /// </summary>
        public async Task<ServiceResult<List<HerbDto>>> GetByIdsAsync(List<Guid> ids)
        {
            try
            {
                var models = await _context.Herbs
                    .Where(h => ids.Contains(h.Id))
                    .ToListAsync();
                
                var dtos = _mapper.Map<List<HerbDto>>(models);
                return ServiceResult<List<HerbDto>>.Success(dtos);
            }
            catch (Exception ex)
            {
                return ServiceResult<List<HerbDto>>.Failure("批量获取药材失败", ex);
            }
        }

        /// <summary>
        /// 获取药材列表（兼容方法）
        /// </summary>
        public async Task<ServiceResult<List<HerbDto>>> GetHerbsAsync()
        {
            return await GetAllAsync();
        }

        /// <summary>
        /// 获取药材列表（带可选查询参数）
        /// </summary>
        public async Task<ServiceResult<List<HerbDto>>> GetListAsync(HerbPagedQueryDto? query = null)
        {
            try
            {
                if (query == null)
                {
                    return await GetAllAsync();
                }
                
                var pagedResult = await GetPagedAsync(query);
                if (pagedResult.IsSuccess && pagedResult.Data != null)
                {
                    return ServiceResult<List<HerbDto>>.Success(pagedResult.Data.Items);
                }
                
                return ServiceResult<List<HerbDto>>.Failure(pagedResult.ErrorMessage ?? "查询失败");
            }
            catch (Exception ex)
            {
                return ServiceResult<List<HerbDto>>.Failure("获取药材列表失败", ex);
            }
        }

        /// <summary>
        /// 按名称搜索药材
        /// </summary>
        public async Task<ServiceResult<List<HerbDto>>> SearchByNameAsync(string name)
        {
            return await SearchAsync(name);
        }

        /// <summary>
        /// 设置药材启用/禁用状态
        /// </summary>
        public async Task<bool> SetStatusAsync(Guid id, bool isActive)
        {
            var model = await _repository.GetByIdAsync(id);
            if (model == null)
            {
                return false;
            }

            model.Status = isActive ? CommonStatus.Enabled : CommonStatus.Disabled;

            var result = await _repository.UpdateAsync(model);
            return result != null;
        }

        /// <summary>
        /// 批量导入药材
        /// </summary>
        public async Task<ServiceResult<int>> ImportHerbsAsync(List<HerbImportDto> herbs)
        {
            try
            {
                var models = new List<HerbModel>();
                foreach (var dto in herbs)
                {
                    var model = _mapper.Map<HerbModel>(dto);
                    model.Id = Guid.NewGuid();
                    model.PinYinCode = GetSimplePinyinCode(model.Name);
                    model.Status = CommonStatus.Enabled; // 导入的药材默认启用
                    models.Add(model);
                }

                var result = await _repository.AddRangeAsync(models);
                var count = result ? models.Count : 0;
                return ServiceResult<int>.Success(count);
            }
            catch (Exception ex)
            {
                return ServiceResult<int>.Failure("导入药材失败", ex);
            }
        }

        /// <summary>
        /// 导出药材数据
        /// </summary>
        public async Task<ServiceResult<List<HerbDto>>> ExportHerbsAsync()
        {
            try
            {
                var list = await _repository.GetAllAsync();
                var dtos = _mapper.Map<List<HerbDto>>(list);
                return ServiceResult<List<HerbDto>>.Success(dtos);
            }
            catch (Exception ex)
            {
                return ServiceResult<List<HerbDto>>.Failure("导出药材数据失败", ex);
            }
        }

        /// <summary>
        /// 更新库存（实现Shared接口，已禁用功能）
        /// </summary>
        public async Task<ServiceResult<bool>> UpdateStockAsync(Guid id, HerbStockUpdateDto dto)
        {
            try
            {
                var herb = await _repository.GetByIdAsync(id);
                if (herb == null)
                {
                    return ServiceResult<bool>.Failure("药材不存在");
                }

                // 库存字段已删除，仅更新时间戳（向后兼容）
                var result = await _repository.UpdateAsync(herb);
                return ServiceResult<bool>.Success(result != null);
            }
            catch (Exception ex)
            {
                return ServiceResult<bool>.Failure("更新库存失败", ex);
            }
        }

        /// <summary>
        /// 获取缺货药材列表（已禁用 - 不再支持库存管理）
        /// </summary>
        public async Task<ServiceResult<List<HerbDto>>> GetOutOfStockHerbsAsync()
        {
            try
            {
                // 库存字段已删除，返回空列表
                await Task.CompletedTask;
                return ServiceResult<List<HerbDto>>.Success(new List<HerbDto>());
            }
            catch (Exception ex)
            {
                return ServiceResult<List<HerbDto>>.Failure("获取缺货药材失败", ex);
            }
        }

        /// <summary>
        /// 获取即将过期的药材（已禁用 - 不再支持过期日期管理）
        /// </summary>
        public async Task<ServiceResult<List<HerbDto>>> GetExpiringHerbsAsync(int days = 30)
        {
            try
            {
                // 过期日期字段已删除，返回空列表
                await Task.CompletedTask;
                return ServiceResult<List<HerbDto>>.Success(new List<HerbDto>());
            }
            catch (Exception ex)
            {
                return ServiceResult<List<HerbDto>>.Failure("获取即将过期药材失败", ex);
            }
        }

        /// <summary>
        /// 批量更新状态
        /// </summary>
        public async Task<ServiceResult<bool>> BatchUpdateStatusAsync(BatchStatusUpdateDto dto)
        {
            try
            {
                var successCount = 0;
                foreach (var id in dto.Ids)
                {
                    var herb = await _repository.GetByIdAsync(id);
                    if (herb != null)
                    {
                        herb.Status = dto.Status ? CommonStatus.Enabled : CommonStatus.Disabled;
                        var result = await _repository.UpdateAsync(herb);
                        if (result != null)
                        {
                            successCount++;
                        }
                    }
                }
                
                return ServiceResult<bool>.Success(successCount > 0);
            }
            catch (Exception ex)
            {
                return ServiceResult<bool>.Failure("批量更新状态失败", ex);
            }
        }

        /// <summary>
        /// 获取统计数据
        /// </summary>
        public async Task<ServiceResult<Dictionary<int, int>>> GetStatisticsAsync()
        {
            try
            {
                var herbs = await _context.Herbs.Where(h => h.Status == CommonStatus.Enabled).ToListAsync();
                var stats = new Dictionary<int, int>
                {
                    { 0, herbs.Count }, // 总数
                    { 1, 0 },          // 缺货数（库存已删除）
                    { 2, herbs.Count }, // 充足数（默认所有都充足）
                    { 3, 0 }           // 过期数（过期字段已删除）
                };
                
                return ServiceResult<Dictionary<int, int>>.Success(stats);
            }
            catch (Exception ex)
            {
                return ServiceResult<Dictionary<int, int>>.Failure("获取统计数据失败", ex);
            }
        }

        #region 库存管理功能

        /// <summary>
        /// 获取库存预警药材列表（已禁用 - 不再支持库存管理）
        /// </summary>
        public async Task<List<HerbStockWarningDto>> GetStockWarningListAsync()
        {
            // 库存字段已删除，返回空列表
            await Task.CompletedTask;
            return new List<HerbStockWarningDto>();
        }

        /// <summary>
        /// 获取库存统计信息（已禁用 - 不再支持库存管理）
        /// </summary>
        public async Task<ServiceResult<HerbStockStatisticsDto>> GetStockStatisticsAsync()
        {
            try
            {
                var herbs = await _context.Herbs.Where(h => h.Status == CommonStatus.Enabled).ToListAsync();

                var stats = new HerbStockStatisticsDto
                {
                    TotalCount = herbs.Count,
                    OutOfStockCount = 0, // 库存字段已删除
                    WarningCount = 0, // 库存字段已删除
                    SufficientCount = herbs.Count, // 默认所有药材都充足
                    TotalStockValue = 0, // 库存字段已删除，无法计算
                    ExpiringCount = 0, // 过期日期字段已删除
                    ExpiredCount = 0 // 过期日期字段已删除
                };

                return ServiceResult<HerbStockStatisticsDto>.Success(stats);
            }
            catch (Exception ex)
            {
                return ServiceResult<HerbStockStatisticsDto>.Failure("获取库存统计失败", ex);
            }
        }

        /// <summary>
        /// 更新药材库存量（已禁用 - 不再支持库存管理）
        /// </summary>
        public async Task<bool> UpdateStockAsync(Guid id, decimal quantity, bool isIncrease)
        {
            // 库存字段已删除，直接返回成功（向后兼容）
            var herb = await _repository.GetByIdAsync(id);
            if (herb == null)
            {
                return false;
            }

            // 仅更新时间戳，不再处理库存
            var result = await _repository.UpdateAsync(herb);
            return result != null;
        }

        /// <summary>
        /// 批量更新库存量（已禁用 - 不再支持库存管理）
        /// </summary>
        public async Task<int> BatchUpdateStockAsync(List<HerbStockUpdateDto> updates)
        {
            var successCount = 0;
            foreach (var update in updates)
            {
                var herb = await _repository.GetByIdAsync(update.Id);
                if (herb != null)
                {
                    // 仅更新时间戳，不再处理库存
                    var result = await _repository.UpdateAsync(herb);
                    if (result != null)
                    {
                        successCount++;
                    }
                }
            }
            return successCount;
        }

        /// <summary>
        /// 设置库存预警值（已禁用 - 不再支持库存管理）
        /// </summary>
        public async Task<bool> SetStockWarningLevelAsync(Guid id, decimal warningLevel, decimal maxStock)
        {
            var herb = await _repository.GetByIdAsync(id);
            if (herb == null)
            {
                return false;
            }

            // 库存预警字段已删除，仅更新时间戳（向后兼容）
            var result = await _repository.UpdateAsync(herb);
            return result != null;
        }

        /// <summary>
        /// 获取即将过期的药材（已禁用 - 不再支持过期日期管理）
        /// </summary>
        public async Task<List<HerbExpiryWarningDto>> GetExpiryWarningListAsync(int days = 30)
        {
            // 过期日期字段已删除，返回空列表
            await Task.CompletedTask;
            return new List<HerbExpiryWarningDto>();
        }

        #endregion

        #region 价格管理功能

        /// <summary>
        /// 更新药材价格
        /// </summary>
        public async Task<ServiceResult<bool>> UpdatePriceAsync(Guid id, HerbPriceUpdateDto dto)
        {
            try
            {
                var herb = await _repository.GetByIdAsync(id);
                if (herb == null)
                {
                    return ServiceResult<bool>.Failure("药材不存在");
                }

                // 记录价格历史（这里简化处理，实际应该保存到价格历史表）
                if (dto.CostPrice.HasValue)
                {
                    herb.CostPrice = dto.CostPrice.Value;
                }
                if (dto.Price.HasValue)
                {
                    herb.Price = dto.Price.Value;
                }
                // MemberPrice 字段已删除，跳过会员价格设置
                // if (dto.MemberPrice.HasValue) {
                //     herb.MemberPrice = dto.MemberPrice.Value;
                // }

                var result = await _repository.UpdateAsync(herb);
                return ServiceResult<bool>.Success(result != null);
            }
            catch (Exception ex)
            {
                return ServiceResult<bool>.Failure("更新价格失败", ex);
            }
        }

        /// <summary>
        /// 批量更新价格
        /// </summary>
        public async Task<int> BatchUpdatePriceAsync(List<HerbPriceUpdateDto> updates)
        {
            var successCount = 0;
            foreach (var update in updates)
            {
                var result = await UpdatePriceAsync(update.Id, update);
                if (result.IsSuccess)
                {
                    successCount++;
                }
            }
            return successCount;
        }

        /// <summary>
        /// 设置特价促销（已禁用 - 不再支持特价功能）
        /// </summary>
        public async Task<bool> SetSpecialPriceAsync(Guid id, decimal specialPrice, DateTime startTime, DateTime endTime)
        {
            var herb = await _repository.GetByIdAsync(id);
            if (herb == null)
            {
                return false;
            }

            // 特价字段已删除，仅更新时间戳（向后兼容）
            var result = await _repository.UpdateAsync(herb);
            return result != null;
        }

        /// <summary>
        /// 取消特价促销（已禁用 - 不再支持特价功能）
        /// </summary>
        public async Task<bool> CancelSpecialPriceAsync(Guid id)
        {
            var herb = await _repository.GetByIdAsync(id);
            if (herb == null)
            {
                return false;
            }

            // 特价字段已删除，仅更新时间戳（向后兼容）
            var result = await _repository.UpdateAsync(herb);
            return result != null;
        }

        /// <summary>
        /// 获取当前特价药材列表（已禁用 - 不再支持特价功能）
        /// </summary>
        public async Task<List<HerbDto>> GetSpecialPriceHerbsAsync()
        {
            // 特价字段已删除，返回空列表
            await Task.CompletedTask;
            return new List<HerbDto>();
        }

        /// <summary>
        /// 获取价格历史记录（简化实现，实际应从价格历史表查询）
        /// </summary>
        public async Task<List<HerbPriceHistoryDto>> GetPriceHistoryAsync(Guid id)
        {
            // 这里应该从价格历史表查询，暂时返回空列表
            await Task.CompletedTask;
            return new List<HerbPriceHistoryDto>();
        }

        /// <summary>
        /// 按价格区间查询药材
        /// </summary>
        public async Task<List<HerbDto>> GetByPriceRangeAsync(decimal minPrice, decimal maxPrice)
        {
            var herbs = await _context.Herbs
                .Where(h => h.Status == CommonStatus.Enabled && h.Price >= minPrice && h.Price <= maxPrice)
                .OrderBy(h => h.Price)
                .ToListAsync();

            return _mapper.Map<List<HerbDto>>(herbs);
        }

        #endregion
    }
}