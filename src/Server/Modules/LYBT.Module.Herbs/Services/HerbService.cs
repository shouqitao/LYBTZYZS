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

        protected override string EntityName => "药材";        /// <summary>
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
                "获取药材列表");        }

        /// <summary>
        /// 分页查询药材 - 委派给QueryHelper
        /// </summary>
        public async Task<ServiceResult<PagedResult<HerbDto>>> GetPagedAsync(HerbPagedQueryDto query)
        {
            return await ExecuteSafelyAsync(
                async () => await _queryHelper.GetPagedAsync(query),
                "分页查询药材", query);        }

        /// <summary>
        /// 新增药材 - 委派给BusinessHelper
        /// </summary>
        public async Task<ServiceResult<HerbDto>> CreateAsync(HerbCreateDto dto)
        {
            return await ExecuteSafelyAsync(
                async () => await _businessHelper.CreateHerbWithAutoCodeAsync(dto),
                "创建药材", dto);        }

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
                "删除药材", id);        }

        #endregion

        #region 查询和搜索操作 - 委派给QueryHelper

        /// <summary>
        /// 搜索药材（根据名称、拼音码）- 委派给QueryHelper
        /// </summary>
        public async Task<ServiceResult<List<HerbDto>>> SearchAsync(string keyword)
        {
            return await ExecuteSafelyAsync(
                async () => await _queryHelper.SearchAsync(keyword),
                "搜索药材", keyword);        }

        /// <summary>
        /// 获取可用药材列表（状态为启用）- 委派给QueryHelper
        /// </summary>
        public async Task<ServiceResult<List<HerbDto>>> GetAvailableHerbsAsync()
        {
            return await ExecuteSafelyAsync(
                async () => await _queryHelper.GetAvailableHerbsAsync(),
                "获取可用药材列表");        }

        /// <summary>
        /// 根据ID列表获取药材 - 委派给QueryHelper
        /// </summary>
        public async Task<ServiceResult<List<HerbDto>>> GetByIdsAsync(List<Guid> ids)
        {
            return await ExecuteSafelyAsync(
                async () => await _queryHelper.GetByIdsAsync(ids),
                "批量获取药材", ids);        }

        /// <summary>
        /// 按价格区间查询药材 - 委派给QueryHelper
        /// </summary>
        public async Task<ServiceResult<List<HerbDto>>> GetByPriceRangeAsync(decimal minPrice, decimal maxPrice)
        {
            return await ExecuteSafelyAsync(
                async () => await _queryHelper.GetByPriceRangeAsync(minPrice, maxPrice),
                "按价格区间查询药材", new { minPrice, maxPrice });        }

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
                return ServiceResult<List<HerbDto>>.Failure(pagedResult.ErrorMessage ?? "查询失败");            }, "获取药材列表", query);        }

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
        /// 批量导入药材 - 委派给BusinessHelper (基础数据功能保留)
        /// </summary>
        public async Task<ServiceResult<int>> ImportHerbsAsync(List<HerbImportDto> herbs)
        {
            return await ExecuteSafelyAsync(
                async () => await _businessHelper.ImportHerbsAsync(herbs),
                "批量导入药材", herbs?.Count);        }

        /// <summary>
        /// 导出药材数据 - 委派给BusinessHelper (基础数据功能保留)
        /// </summary>
        public async Task<ServiceResult<List<HerbDto>>> ExportHerbsAsync()
        {
            return await ExecuteSafelyAsync(
                async () => await _queryHelper.GetAvailableHerbsAsync(),
                "导出药材数据");        }

        /// <summary>
        /// 获取药材导入模板 - 基础数据功能 (拼音码自动生成)
        /// </summary>
        public async Task<ServiceResult<byte[]>> GetImportTemplateAsync()
        {
            try
            {
                _logger.LogInformation("获取药材导入模板");                var templateContent = @"药材导入模板 - UltraThink精简版
必填列：药材名称, 产地, 规格, 单位, 价格
可选列：功效, 用法, 备注, 状态(Enabled/Disabled)

注意：
- 拼音码由系统自动生成，无需填写
  规则：每个字拼音首字母大写组合（如：当归 → DG）
- 药材名称不能重复
- 价格必须为有效数字
- 状态默认为Enabled(启用)
- 单位推荐：g(克), kg(公斤), 包, 盒等";                var content = System.Text.Encoding.UTF8.GetBytes(templateContent);
                return ServiceResult<byte[]>.Success(content);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取药材导入模板异常");                return ServiceResult<byte[]>.Failure($"获取药材导入模板异常: {ex.Message}", ex);            }
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

        #region 已废弃功能 - UltraThink精简

        /// <summary>
        /// 获取统计数据 (已废弃)
        /// UltraThink v2.0: 统计功能已删除 - 小诊所不需要复杂统计分析
        /// </summary>
        public async Task<ServiceResult<Dictionary<int, int>>> GetStatisticsAsync()
        {
            await Task.CompletedTask;
            return ServiceResult<Dictionary<int, int>>.Success(new Dictionary<int, int>());
        }

        /// <summary>
        /// 更新药材价格 (已废弃)
        /// UltraThink v2.0: 价格历史功能已删除，直接使用UpdateAsync更新价格
        /// </summary>
        public async Task<ServiceResult<bool>> UpdatePriceAsync(Guid id, HerbPriceUpdateDto dto)
        {
            await Task.CompletedTask;
            return ServiceResult<bool>.Success(false); // 功能已废弃
        }

        /// <summary>
        /// 更新库存 (已废弃)
        /// UltraThink v2.0: 库存功能已删除 - 小诊所不需要复杂库存管理
        /// </summary>
        public async Task<ServiceResult<bool>> UpdateStockAsync(Guid id, HerbStockUpdateDto dto)
        {
            await Task.CompletedTask;
            return ServiceResult<bool>.Success(false); // 功能已废弃
        }

        /// <summary>
        /// 获取库存统计信息 (已废弃)
        /// UltraThink v2.0: 库存统计功能已删除
        /// </summary>
        public async Task<ServiceResult<HerbStockStatisticsDto>> GetStockStatisticsAsync()
        {
            await Task.CompletedTask;
            return ServiceResult<HerbStockStatisticsDto>.Success(new HerbStockStatisticsDto());
        }

        /// <summary>
        /// 获取缺货药材列表 (已废弃)
        /// UltraThink v2.0: 库存功能已删除
        /// </summary>
        public async Task<ServiceResult<List<HerbDto>>> GetOutOfStockHerbsAsync()
        {
            await Task.CompletedTask;
            return ServiceResult<List<HerbDto>>.Success(new List<HerbDto>());
        }

        /// <summary>
        /// 获取即将过期的药材 (已废弃)
        /// UltraThink v2.0: 过期管理功能已删除
        /// </summary>
        public async Task<ServiceResult<List<HerbDto>>> GetExpiringHerbsAsync(int days = 30)
        {
            await Task.CompletedTask;
            return ServiceResult<List<HerbDto>>.Success(new List<HerbDto>());
        }

        /*
        /// <summary>
        /// 批量更新价格 (已废弃)
        /// </summary>
        public async Task<ServiceResult<int>> BatchUpdatePriceAsync(List<HerbPriceUpdateDto> updates)
        {
            // 批量价格更新功能已删除 - 小诊所逐个更新即可
        }
        */

        #endregion

        /*
        /// <summary>
        /// 更新库存 (已废弃)
        /// </summary>
        public async Task<ServiceResult<bool>> UpdateStockAsync(Guid id, HerbStockUpdateDto dto)
        {
            // 库存功能已删除 - 小诊所不需要复杂库存管理
        }

        /// <summary>
        /// 获取缺货药材列表 (已废弃)
        /// </summary>
        public async Task<ServiceResult<List<HerbDto>>> GetOutOfStockHerbsAsync()
        {
            // 库存功能已删除
        }

        /// <summary>
        /// 获取即将过期的药材 (已废弃)
        /// </summary>
        public async Task<ServiceResult<List<HerbDto>>> GetExpiringHerbsAsync(int days = 30)
        {
            // 过期管理功能已删除
        }

        /// <summary>
        /// 获取库存统计信息 (已废弃)
        /// </summary>
        public async Task<ServiceResult<HerbStockStatisticsDto>> GetStockStatisticsAsync()
        {
            // 库存统计功能已删除
        }
        */

        /*
        /// <summary>
        /// 获取库存预警药材列表 (已废弃)
        /// </summary>
        public async Task<List<HerbStockWarningDto>> GetStockWarningListAsync()
        {
            // 库存预警功能已删除
        }

        /// <summary>
        /// 更新药材库存量 (已废弃)
        /// </summary>
        public async Task<bool> UpdateStockAsync(Guid id, decimal quantity, bool isIncrease)
        {
            // 库存管理功能已删除
        }

        /// <summary>
        /// 批量更新库存量 (已废弃)
        /// </summary>
        public async Task<int> BatchUpdateStockAsync(List<HerbStockUpdateDto> updates)
        {
            // 批量库存更新功能已删除
        }

        /// <summary>
        /// 设置库存预警值 (已废弃)
        /// </summary>
        public async Task<bool> SetStockWarningLevelAsync(Guid id, decimal warningLevel, decimal maxStock)
        {
            // 库存预警功能已删除
        }

        /// <summary>
        /// 获取即将过期的药材 (已废弃)
        /// </summary>
        public async Task<List<HerbExpiryWarningDto>> GetExpiryWarningListAsync(int days = 30)
        {
            // 过期管理功能已删除
        }

        /// <summary>
        /// 设置特价促销 (已废弃)
        /// </summary>
        public async Task<bool> SetSpecialPriceAsync(Guid id, decimal specialPrice, DateTime startTime, DateTime endTime)
        {
            // 特价促销功能已删除
        }

        /// <summary>
        /// 取消特价促销 (已废弃)
        /// </summary>
        public async Task<bool> CancelSpecialPriceAsync(Guid id)
        {
            // 特价促销功能已删除
        }

        /// <summary>
        /// 获取当前特价药材列表 (已废弃)
        /// </summary>
        public async Task<List<HerbDto>> GetSpecialPriceHerbsAsync()
        {
            // 特价促销功能已删除
        }

        /// <summary>
        /// 获取价格历史记录 (已废弃)
        /// </summary>
        public async Task<List<HerbPriceHistoryDto>> GetPriceHistoryAsync(Guid id)
        {
            // 价格历史功能已删除
        }
        */

        #endregion
    }
}
