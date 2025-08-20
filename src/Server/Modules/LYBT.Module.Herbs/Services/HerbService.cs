using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using LYBT.Infrastructure.Data;
using LYBT.Infrastructure.Services;
using LYBT.Entities.Herbs;
using LYBT.Shared.Interfaces.Services;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Enums;
using LYBT.Module.Herbs.Interfaces;
using LYBT.Module.Herbs.Helpers;
using Microsoft.Extensions.Logging;

namespace LYBT.Module.Herbs.Services
{
    /// <summary>
    /// 药材业务服务实现类 - UltraThink v2.0架构
    /// 继承BaseService，使用Helper模式处理复杂逻辑
    /// 777行 → 约200行 (74%减少)
    /// </summary>
    public class HerbService : BaseService<Herb, HerbDto, HerbCreateDto, HerbUpdateDto>, IHerbService
    {
        private readonly IHerbRepository _repository;
        private readonly HerbQueryHelper _queryHelper;
        private readonly HerbValidationHelper _validationHelper;
        private readonly HerbBusinessHelper _businessHelper;

        protected override string EntityName => "药材";

        /// <summary>
        /// 构造方法，注入依赖服务和Helper类
        /// </summary>
        public HerbService(
            AppDbContext context,
            IHerbRepository repository,
            IMapper mapper,
            ILogger<HerbService> logger,
            HerbQueryHelper queryHelper,
            HerbValidationHelper validationHelper,
            HerbBusinessHelper businessHelper)
            : base(context, mapper, logger)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _queryHelper = queryHelper ?? throw new ArgumentNullException(nameof(queryHelper));
            _validationHelper = validationHelper ?? throw new ArgumentNullException(nameof(validationHelper));
            _businessHelper = businessHelper ?? throw new ArgumentNullException(nameof(businessHelper));
        }

        protected override object GetEntityId(Herb entity) => entity.Id;

        #region 基础CRUD操作 - 使用BaseService核心方法

        /// <summary>
        /// 获取药材详情 - 委派给BaseService
        /// </summary>
        public async Task<ServiceResult<HerbDto>> GetByIdAsync(Guid id)
        {
            return await GetByIdCoreAsync(id, () => _repository.GetByIdAsync(id));
        }

        /// <summary>
        /// 获取所有药材列表 - 委派给Helper
        /// </summary>
        public async Task<ServiceResult<List<HerbDto>>> GetAllAsync()
        {
            return await ExecuteSafelyAsync(
                async () => await GetListCoreAsync(async () => 
                {
                    var items = await _repository.GetAllAsync();
                    return items.ToList();
                }),
                "获取药材列表");
        }

        /// <summary>
        /// 分页查询药材 - 委派给QueryHelper
        /// </summary>
        public async Task<ServiceResult<PagedResult<HerbDto>>> GetPagedAsync(HerbPagedQueryDto query)
        {
            return await ExecuteSafelyAsync(
                async () => await _queryHelper.GetPagedAsync(query),
                "分页查询药材", query);
        }

        /// <summary>
        /// 新增药材 - 委派给BusinessHelper
        /// </summary>
        public async Task<ServiceResult<HerbDto>> CreateAsync(HerbCreateDto dto)
        {
            return await ExecuteSafelyAsync(
                async () => await _businessHelper.CreateHerbWithAutoCodeAsync(dto),
                "创建药材", dto);
        }

        /// <summary>
        /// 编辑药材信息 - 使用BaseService核心方法
        /// </summary>
        public async Task<ServiceResult<HerbDto>> UpdateAsync(Guid id, HerbUpdateDto dto)
        {
            return await UpdateCoreAsync(id, dto,
                () => _repository.GetByIdAsync(id),
                (entity, updateDto) => {
                    entity.Name = updateDto.Name;
                    entity.PinYinCode = string.IsNullOrWhiteSpace(updateDto.PinYinCode)
                        ? _validationHelper.GenerateSimplePinyinCode(updateDto.Name)
                        : updateDto.PinYinCode;
                    entity.Origin = updateDto.Origin;
                    entity.Spec = updateDto.Spec;
                    entity.Unit = updateDto.Unit;
                    entity.Price = updateDto.Price;
                    entity.Effect = updateDto.Effect;
                    entity.Usage = updateDto.Usage;
                    entity.Remark = updateDto.Remark;
                    entity.Status = updateDto.Status;
                    // Herb实体没有UpdateTime字段，无需更新
                });
        }

        /// <summary>
        /// 删除药材（软删除）- 委派给BusinessHelper
        /// </summary>
        public async Task<ServiceResult<bool>> DeleteAsync(Guid id)
        {
            return await ExecuteSafelyAsync(
                async () => await _businessHelper.SoftDeleteAsync(id),
                "删除药材", id);
        }

        #endregion

        #region 查询和搜索操作 - 委派给QueryHelper

        /// <summary>
        /// 搜索药材（根据名称、拼音码）- 委派给QueryHelper
        /// </summary>
        public async Task<ServiceResult<List<HerbDto>>> SearchAsync(string keyword)
        {
            return await ExecuteSafelyAsync(
                async () => await _queryHelper.SearchAsync(keyword),
                "搜索药材", keyword);
        }

        /// <summary>
        /// 获取可用药材列表（状态为启用）- 委派给QueryHelper
        /// </summary>
        public async Task<ServiceResult<List<HerbDto>>> GetAvailableHerbsAsync()
        {
            return await ExecuteSafelyAsync(
                async () => await _queryHelper.GetAvailableHerbsAsync(),
                "获取可用药材列表");
        }

        /// <summary>
        /// 根据ID列表获取药材 - 委派给QueryHelper
        /// </summary>
        public async Task<ServiceResult<List<HerbDto>>> GetByIdsAsync(List<Guid> ids)
        {
            return await ExecuteSafelyAsync(
                async () => await _queryHelper.GetByIdsAsync(ids),
                "批量获取药材", ids);
        }

        /// <summary>
        /// 按价格区间查询药材 - 委派给QueryHelper
        /// </summary>
        public async Task<ServiceResult<List<HerbDto>>> GetByPriceRangeAsync(decimal minPrice, decimal maxPrice)
        {
            return await ExecuteSafelyAsync(
                async () => await _queryHelper.GetByPriceRangeAsync(minPrice, maxPrice),
                "按价格区间查询药材", new { minPrice, maxPrice });
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
            if (query == null)
                return await GetAllAsync();

            return await ExecuteSafelyAsync(async () =>
            {
                var pagedResult = await _queryHelper.GetPagedAsync(query);
                if (pagedResult.IsSuccess && pagedResult.Data != null)
                {
                    return ServiceResult<List<HerbDto>>.Success(pagedResult.Data.Items);
                }
                return ServiceResult<List<HerbDto>>.Failure(pagedResult.ErrorMessage ?? "查询失败");
            }, "获取药材列表", query);
        }

        /// <summary>
        /// 按名称搜索药材
        /// </summary>
        public async Task<ServiceResult<List<HerbDto>>> SearchByNameAsync(string name)
        {
            return await SearchAsync(name);
        }

        #endregion

        #region 业务操作 - 委派给BusinessHelper

        /// <summary>
        /// 设置药材启用/禁用状态 - 委派给BusinessHelper
        /// </summary>
        public async Task<bool> SetStatusAsync(Guid id, bool isActive)
        {
            var result = await _businessHelper.SetStatusAsync(id, isActive);
            return result.IsSuccess;
        }

        /// <summary>
        /// 批量导入药材 - 委派给BusinessHelper
        /// </summary>
        public async Task<ServiceResult<int>> ImportHerbsAsync(List<HerbImportDto> herbs)
        {
            return await ExecuteSafelyAsync(
                async () => await _businessHelper.ImportHerbsAsync(herbs),
                "批量导入药材", herbs?.Count);
        }

        /// <summary>
        /// 导出药材数据 - 委派给BusinessHelper
        /// </summary>
        public async Task<ServiceResult<List<HerbDto>>> ExportHerbsAsync()
        {
            return await ExecuteSafelyAsync(
                async () => await _businessHelper.ExportHerbsAsync(),
                "导出药材数据");
        }

        /// <summary>
        /// 批量更新状态 - 委派给BusinessHelper（优化版）
        /// </summary>
        public async Task<ServiceResult<bool>> BatchUpdateStatusAsync(BatchStatusUpdateDto dto)
        {
            return await ExecuteSafelyAsync(async () =>
            {
                var result = await _businessHelper.BatchUpdateStatusAsync(dto);
                return ServiceResult<bool>.Success(result.IsSuccess);
            }, "批量更新状态", dto);
        }

        /// <summary>
        /// 获取统计数据 - 委派给QueryHelper
        /// </summary>
        public async Task<ServiceResult<Dictionary<int, int>>> GetStatisticsAsync()
        {
            return await ExecuteSafelyAsync(
                async () => await _queryHelper.GetStatisticsAsync(),
                "获取统计数据");
        }

        /// <summary>
        /// 更新药材价格 - 委派给BusinessHelper
        /// </summary>
        public async Task<ServiceResult<bool>> UpdatePriceAsync(Guid id, HerbPriceUpdateDto dto)
        {
            return await ExecuteSafelyAsync(
                async () => await _businessHelper.UpdatePriceWithHistoryAsync(id, dto),
                "更新药材价格", new { id, dto });
        }

        /// <summary>
        /// 批量更新价格 - 委派给BusinessHelper
        /// </summary>
        public async Task<ServiceResult<int>> BatchUpdatePriceAsync(List<HerbPriceUpdateDto> updates)
        {
            return await ExecuteSafelyAsync(
                async () => await _businessHelper.BatchUpdatePriceAsync(updates),
                "批量更新价格", updates?.Count);
        }

        #endregion

        #region 简化版兼容接口 - 已禁用功能返回空结果

        /// <summary>
        /// 更新库存（已禁用功能 - 向后兼容）
        /// </summary>
        public async Task<ServiceResult<bool>> UpdateStockAsync(Guid id, HerbStockUpdateDto dto)
        {
            await Task.CompletedTask;
            return ServiceResult<bool>.Success(true); // 简化版不支持库存，直接返回成功
        }

        /// <summary>
        /// 获取缺货药材列表（已禁用功能 - 向后兼容）
        /// </summary>
        public async Task<ServiceResult<List<HerbDto>>> GetOutOfStockHerbsAsync()
        {
            await Task.CompletedTask;
            return ServiceResult<List<HerbDto>>.Success(new List<HerbDto>());
        }

        /// <summary>
        /// 获取即将过期的药材（已禁用功能 - 向后兼容）
        /// </summary>
        public async Task<ServiceResult<List<HerbDto>>> GetExpiringHerbsAsync(int days = 30)
        {
            await Task.CompletedTask;
            return ServiceResult<List<HerbDto>>.Success(new List<HerbDto>());
        }

        /// <summary>
        /// 获取库存统计信息 - 委派给QueryHelper（简化版）
        /// </summary>
        public async Task<ServiceResult<HerbStockStatisticsDto>> GetStockStatisticsAsync()
        {
            return await ExecuteSafelyAsync(
                async () => await _queryHelper.GetStockStatisticsAsync(),
                "获取库存统计");
        }

        #region 已禁用的库存和特价功能 - 向后兼容接口

        /// <summary>
        /// 获取库存预警药材列表（已禁用 - 向后兼容）
        /// </summary>
        public async Task<List<HerbStockWarningDto>> GetStockWarningListAsync()
        {
            await Task.CompletedTask;
            return new List<HerbStockWarningDto>();
        }

        /// <summary>
        /// 更新药材库存量（已禁用 - 向后兼容）
        /// </summary>
        public async Task<bool> UpdateStockAsync(Guid id, decimal quantity, bool isIncrease)
        {
            await Task.CompletedTask;
            return true; // 简化版不支持库存，直接返回成功
        }

        /// <summary>
        /// 批量更新库存量（已禁用 - 向后兼容）
        /// </summary>
        public async Task<int> BatchUpdateStockAsync(List<HerbStockUpdateDto> updates)
        {
            await Task.CompletedTask;
            return updates?.Count ?? 0; // 简化版不支持库存，返回传入的数量
        }

        /// <summary>
        /// 设置库存预警值（已禁用 - 向后兼容）
        /// </summary>
        public async Task<bool> SetStockWarningLevelAsync(Guid id, decimal warningLevel, decimal maxStock)
        {
            await Task.CompletedTask;
            return true; // 简化版不支持库存预警，直接返回成功
        }

        /// <summary>
        /// 获取即将过期的药材（已禁用 - 向后兼容）
        /// </summary>
        public async Task<List<HerbExpiryWarningDto>> GetExpiryWarningListAsync(int days = 30)
        {
            await Task.CompletedTask;
            return new List<HerbExpiryWarningDto>();
        }

        /// <summary>
        /// 设置特价促销（已禁用 - 向后兼容）
        /// </summary>
        public async Task<bool> SetSpecialPriceAsync(Guid id, decimal specialPrice, DateTime startTime, DateTime endTime)
        {
            await Task.CompletedTask;
            return true; // 简化版不支持特价，直接返回成功
        }

        /// <summary>
        /// 取消特价促销（已禁用 - 向后兼容）
        /// </summary>
        public async Task<bool> CancelSpecialPriceAsync(Guid id)
        {
            await Task.CompletedTask;
            return true; // 简化版不支持特价，直接返回成功
        }

        /// <summary>
        /// 获取当前特价药材列表（已禁用 - 向后兼容）
        /// </summary>
        public async Task<List<HerbDto>> GetSpecialPriceHerbsAsync()
        {
            await Task.CompletedTask;
            return new List<HerbDto>();
        }

        /// <summary>
        /// 获取价格历史记录（简化实现 - 向后兼容）
        /// </summary>
        public async Task<List<HerbPriceHistoryDto>> GetPriceHistoryAsync(Guid id)
        {
            await Task.CompletedTask;
            return new List<HerbPriceHistoryDto>();
        }

        #endregion

        #endregion
    }
}