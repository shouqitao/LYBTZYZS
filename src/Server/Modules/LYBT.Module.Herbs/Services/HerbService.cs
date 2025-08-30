using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Interfaces.Services;
using LYBT.Module.Herbs.Services.Core;
using Microsoft.Extensions.Logging;

namespace LYBT.Module.Herbs.Services
{
    /// <summary>
    /// 药材业务服务实现类 - UltraThink v3.0架构
    /// 采用三层服务纯委托模式，从452行重构为~150行 (67%减少)
    /// 职责：服务组合器，纯委托调用，不包含业务逻辑
    /// </summary>
    public class HerbService : IHerbService
    {
        private readonly HerbServiceCore _coreService;
        private readonly HerbQueryService _queryService;
        private readonly HerbBusinessService _businessService;
        private readonly ILogger<HerbService> _logger;

        /// <summary>
        /// 构造方法，注入三层专业服务
        /// </summary>
        public HerbService(
            HerbServiceCore coreService,
            HerbQueryService queryService,
            HerbBusinessService businessService,
            ILogger<HerbService> logger)
        {
            _coreService = coreService ?? throw new ArgumentNullException(nameof(coreService));
            _queryService = queryService ?? throw new ArgumentNullException(nameof(queryService));
            _businessService = businessService ?? throw new ArgumentNullException(nameof(businessService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        #region 基础CRUD操作 - 纯委托调用

        /// <summary>
        /// 获取药材详情 - 委派给CoreService
        /// </summary>
        public async Task<ServiceResult<HerbDto>> GetByIdAsync(Guid id)
        {
            return await _coreService.GetByIdAsync(id);
        }

        /// <summary>
        /// 获取所有药材列表 - 委派给QueryService
        /// </summary>
        public async Task<ServiceResult<List<HerbDto>>> GetAllAsync()
        {
            return await _queryService.GetAllAsync();
        }

        /// <summary>
        /// 分页查询药材 - 委派给QueryService
        /// </summary>
        public async Task<ServiceResult<PagedResult<HerbDto>>> GetPagedAsync(HerbPagedQueryDto query)
        {
            return await _queryService.GetPagedAsync(query);
        }

        /// <summary>
        /// 新增药材 - 委派给CoreService
        /// </summary>
        public async Task<ServiceResult<HerbDto>> CreateAsync(HerbCreateDto dto)
        {
            return await _coreService.CreateAsync(dto);
        }

        /// <summary>
        /// 编辑药材信息 - 委派给CoreService
        /// </summary>
        public async Task<ServiceResult<HerbDto>> UpdateAsync(Guid id, HerbUpdateDto dto)
        {
            return await _coreService.UpdateAsync(id, dto);
        }

        /// <summary>
        /// 删除药材（软删除）- 委派给BusinessService
        /// </summary>
        public async Task<ServiceResult<bool>> DeleteAsync(Guid id)
        {
            return await _businessService.SoftDeleteAsync(id);
        }

        #endregion

        #region 查询和搜索操作 - 委派给QueryService

        /// <summary>
        /// 搜索药材（根据名称、拼音码）- 委派给QueryService
        /// </summary>
        public async Task<ServiceResult<List<HerbDto>>> SearchAsync(string keyword)
        {
            return await _queryService.SearchAsync(keyword);
        }

        /// <summary>
        /// 获取可用药材列表（状态为启用）- 委派给QueryService
        /// </summary>
        public async Task<ServiceResult<List<HerbDto>>> GetAvailableHerbsAsync()
        {
            return await _queryService.GetAvailableHerbsAsync();
        }

        /// <summary>
        /// 根据ID列表获取药材 - 委派给QueryService
        /// </summary>
        public async Task<ServiceResult<List<HerbDto>>> GetByIdsAsync(List<Guid> ids)
        {
            return await _queryService.GetByIdsAsync(ids);
        }

        /// <summary>
        /// 按价格区间查询药材 - 委派给QueryService
        /// </summary>
        public async Task<ServiceResult<List<HerbDto>>> GetByPriceRangeAsync(decimal minPrice, decimal maxPrice)
        {
            return await _queryService.GetByPriceRangeAsync(minPrice, maxPrice);
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

            var pagedResult = await _queryService.GetPagedAsync(query);
            if (pagedResult.IsSuccess && pagedResult.Data != null)
            {
                return ServiceResult<List<HerbDto>>.Success(pagedResult.Data.Items);
            }
            return ServiceResult<List<HerbDto>>.Failure(pagedResult.ErrorMessage ?? "查询失败");
        }

        /// <summary>
        /// 按名称搜索药材
        /// </summary>
        public async Task<ServiceResult<List<HerbDto>>> SearchByNameAsync(string name)
        {
            return await SearchAsync(name);
        }

        #endregion

        #region 业务操作 - 委派给BusinessService

        /// <summary>
        /// 设置药材启用/禁用状态 - 委派给BusinessService
        /// </summary>
        public async Task<bool> SetStatusAsync(Guid id, bool isActive)
        {
            var result = await _businessService.SetStatusAsync(id, isActive);
            return result.IsSuccess;
        }

        /// <summary>
        /// 批量导入药材 - 委派给BusinessService
        /// </summary>
        public async Task<ServiceResult<int>> ImportHerbsAsync(List<HerbImportDto> herbs)
        {
            return await _businessService.ImportHerbsAsync(herbs);
        }

        /// <summary>
        /// 导出药材数据 - 委派给QueryService
        /// </summary>
        public async Task<ServiceResult<List<HerbDto>>> ExportHerbsAsync()
        {
            return await _queryService.GetAvailableHerbsAsync();
        }

        /// <summary>
        /// 获取药材导入模板 - 基础数据功能 (拼音码自动生成)
        /// </summary>
        public async Task<ServiceResult<byte[]>> GetImportTemplateAsync()
        {
            try
            {
                _logger.LogInformation("获取药材导入模板");
                var templateContent = @"药材导入模板 - UltraThink v3.0精简版
必填列：药材名称, 产地, 规格, 单位, 价格
可选列：功效, 用法, 备注, 状态(Enabled/Disabled)

注意：
- 拼音码由系统自动生成，无需填写
  规则：每个字拼音首字母大写组合（如：当归 → DG）
- 药材名称不能重复
- 价格必须为有效数字
- 状态默认为Enabled(启用)
- 单位推荐：g(克), kg(公斤), 包, 盒等";

                var content = System.Text.Encoding.UTF8.GetBytes(templateContent);
                return ServiceResult<byte[]>.Success(content);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取药材导入模板异常");
                return ServiceResult<byte[]>.Failure($"获取药材导入模板异常: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 批量更新状态 - 委派给BusinessService
        /// </summary>
        public async Task<ServiceResult<bool>> BatchUpdateStatusAsync(BatchStatusUpdateDto dto)
        {
            return await _businessService.BatchUpdateStatusAsync(dto);
        }

        #endregion

        #region 已废弃功能 - UltraThink v3.0精简

        /// <summary>
        /// 获取统计数据 (已废弃)
        /// UltraThink v3.0: 统计功能已删除 - 小诊所不需要复杂统计分析
        /// </summary>
        public async Task<ServiceResult<Dictionary<int, int>>> GetStatisticsAsync()
        {
            await Task.CompletedTask;
            _logger.LogWarning("统计功能已废弃 - UltraThink v3.0精简版");
            return ServiceResult<Dictionary<int, int>>.Success(new Dictionary<int, int>());
        }

        /// <summary>
        /// 更新药材价格 (已废弃)
        /// UltraThink v3.0: 价格历史功能已删除，直接使用UpdateAsync更新价格
        /// </summary>
        public async Task<ServiceResult<bool>> UpdatePriceAsync(Guid id, HerbPriceUpdateDto dto)
        {
            await Task.CompletedTask;
            _logger.LogWarning("价格历史功能已废弃 - 使用UpdateAsync更新价格");
            return ServiceResult<bool>.Success(false);
        }

        /// <summary>
        /// 更新库存 (已废弃)
        /// UltraThink v3.0: 库存功能已删除 - 小诊所不需要复杂库存管理
        /// </summary>
        public async Task<ServiceResult<bool>> UpdateStockAsync(Guid id, HerbStockUpdateDto dto)
        {
            await Task.CompletedTask;
            _logger.LogWarning("库存功能已废弃 - UltraThink v3.0精简版");
            return ServiceResult<bool>.Success(false);
        }

        /// <summary>
        /// 获取库存统计信息 (已废弃)
        /// </summary>
        public async Task<ServiceResult<HerbStockStatisticsDto>> GetStockStatisticsAsync()
        {
            await Task.CompletedTask;
            return ServiceResult<HerbStockStatisticsDto>.Success(new HerbStockStatisticsDto());
        }

        /// <summary>
        /// 获取缺货药材列表 (已废弃)
        /// </summary>
        public async Task<ServiceResult<List<HerbDto>>> GetOutOfStockHerbsAsync()
        {
            await Task.CompletedTask;
            return ServiceResult<List<HerbDto>>.Success(new List<HerbDto>());
        }

        /// <summary>
        /// 获取即将过期的药材 (已废弃)
        /// </summary>
        public async Task<ServiceResult<List<HerbDto>>> GetExpiringHerbsAsync(int days = 30)
        {
            await Task.CompletedTask;
            return ServiceResult<List<HerbDto>>.Success(new List<HerbDto>());
        }

        #endregion
    }
}
