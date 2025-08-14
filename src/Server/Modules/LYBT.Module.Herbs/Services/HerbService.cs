using System.Threading.Tasks;
using System.Linq;
using System;
using AutoMapper;
using LYBT.Infrastructure.Data;
using LYBT.Entities.Herbs;
using LYBT.Module.Herbs.Interfaces;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.Shared.Models.Enums;
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
        public async Task<HerbDetailDto?> GetByIdAsync(Guid id)
        {
            var model = await _repository.GetByIdAsync(id);
            if (model == null)
                return null;

            var dto = _mapper.Map<HerbDetailDto>(model);
            return dto;
        }

        /// <summary>
        /// 获取所有药材列表
        /// </summary>
        public async Task<List<HerbDto>> GetListAsync()
        {
            var list = await _repository.GetAllAsync();
            var dtos = _mapper.Map<List<HerbDto>>(list);
            return dtos;
        }

        /// <summary>
        /// 分页查询药材
        /// </summary>
        public async Task<PaginatedResult<HerbDto>> GetPagedAsync(HerbPagedQueryDto query)
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

            return new PaginatedResult<HerbDto>
            {
                TotalCount = total,
                Items = dtos,
                CurrentPage = query.PageIndex,
                PageSize = query.PageSize
            };
        }

        /// <summary>
        /// 新增药材
        /// </summary>
        public async Task<HerbDto?> AddAsync(HerbCreateDto dto)
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
                return null;
            }

            return _mapper.Map<HerbDto>(model);
        }

        /// <summary>
        /// 编辑药材信息
        /// </summary>
        public async Task<bool> UpdateAsync(HerbUpdateDto dto)
        {
            var model = await _repository.GetByIdAsync(dto.Id);
            if (model == null)
                return false;

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
            return result != null;
        }

        /// <summary>
        /// 删除药材（软删除，设置IsActive=false）
        /// </summary>
        public async Task<bool> DeleteAsync(Guid id)
        {
            var model = await _repository.GetByIdAsync(id);
            if (model == null)
                return false;

            model.Status = CommonStatus.Disabled;

            var result = await _repository.UpdateAsync(model);
            return result != null;
        }

        /// <summary>
        /// 搜索药材（根据名称、拼音码）
        /// </summary>
        public async Task<List<HerbDto>> SearchAsync(string keyword)
        {
            if (string.IsNullOrWhiteSpace(keyword))
            {
                return new List<HerbDto>();
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

            return _mapper.Map<List<HerbDto>>(models);
        }

        /// <summary>
        /// 获取可用药材列表（状态为启用）
        /// </summary>
        public async Task<List<HerbDto>> GetAvailableHerbsAsync()
        {
            var models = await _context.Herbs
                .Where(h => h.Status == CommonStatus.Enabled)
                .OrderBy(h => h.Name)
                .ToListAsync();

            return _mapper.Map<List<HerbDto>>(models);
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
        public async Task<int> ImportAsync(List<HerbImportDto> dtos)
        {
            var models = new List<HerbModel>();
            foreach (var dto in dtos)
            {
                var model = _mapper.Map<HerbModel>(dto);
                model.Id = Guid.NewGuid();
                model.PinYinCode = GetSimplePinyinCode(model.Name);
                model.Status = CommonStatus.Enabled; // 导入的药材默认启用
                models.Add(model);
            }

            var result = await _repository.AddRangeAsync(models);
            return result ? models.Count : 0;
        }

        /// <summary>
        /// 导出药材数据
        /// </summary>
        public async Task<List<HerbDetailDto>> ExportAsync()
        {
            var list = await _repository.GetAllAsync();
            var dtos = _mapper.Map<List<HerbDetailDto>>(list);
            return dtos;
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
        public async Task<HerbStockStatisticsDto> GetStockStatisticsAsync()
        {
            var herbs = await _context.Herbs.Where(h => h.Status == CommonStatus.Enabled).ToListAsync();

            return new HerbStockStatisticsDto
            {
                TotalCount = herbs.Count,
                OutOfStockCount = 0, // 库存字段已删除
                WarningCount = 0, // 库存字段已删除
                SufficientCount = herbs.Count, // 默认所有药材都充足
                TotalStockValue = 0, // 库存字段已删除，无法计算
                ExpiringCount = 0, // 过期日期字段已删除
                ExpiredCount = 0 // 过期日期字段已删除
            };
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
        public async Task<bool> UpdatePriceAsync(HerbPriceUpdateDto dto)
        {
            var herb = await _repository.GetByIdAsync(dto.Id);
            if (herb == null)
            {
                return false;
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
            return result != null;
        }

        /// <summary>
        /// 批量更新价格
        /// </summary>
        public async Task<int> BatchUpdatePriceAsync(List<HerbPriceUpdateDto> updates)
        {
            var successCount = 0;
            foreach (var update in updates)
            {
                if (await UpdatePriceAsync(update))
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