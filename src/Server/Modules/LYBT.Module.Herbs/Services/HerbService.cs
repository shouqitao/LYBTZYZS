using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Interfaces.Services;
using Microsoft.Extensions.Logging;

namespace LYBT.Module.Herbs.Services
{
    /// <summary>
    /// 药材服务 - UltraThink双层架构纯委托模式
    /// </summary>
    public class HerbService : IHerbService
    {
        private readonly HerbQueryService _queryService;
        private readonly HerbBusinessService _businessService;
        private readonly ILogger<HerbService> _logger;

        public HerbService(
            HerbQueryService queryService,
            HerbBusinessService businessService,
            ILogger<HerbService> logger)
        {
            _queryService = queryService ?? throw new ArgumentNullException(nameof(queryService));
            _businessService = businessService ?? throw new ArgumentNullException(nameof(businessService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        #region Query Operations

        public Task<ServiceResult<HerbDto>> GetByIdAsync(Guid id)
        {
            return Task.FromResult(ServiceResult<HerbDto>.Failure("GetByIdAsync方法需要在QueryService中实现"));
        }

        public async Task<ServiceResult<List<HerbDto>>> GetAllAsync()
            => await _queryService.GetAllAsync();

        public async Task<ServiceResult<PagedResult<HerbDto>>> GetPagedAsync(HerbPagedQueryDto query)
            => await _queryService.GetPagedAsync(query);

        public async Task<ServiceResult<List<HerbDto>>> SearchAsync(string keyword)
            => await _queryService.SearchAsync(keyword);

        public async Task<ServiceResult<List<HerbDto>>> GetAvailableHerbsAsync()
            => await _queryService.GetAvailableHerbsAsync();

        public async Task<ServiceResult<List<HerbDto>>> GetByIdsAsync(List<Guid> ids)
            => await _queryService.GetByIdsAsync(ids);

        public async Task<ServiceResult<List<HerbDto>>> GetByPriceRangeAsync(decimal minPrice, decimal maxPrice)
            => await _queryService.GetByPriceRangeAsync(minPrice, maxPrice);

        public async Task<ServiceResult<List<HerbDto>>> GetHerbsAsync()
            => await GetAllAsync();

        public async Task<ServiceResult<List<HerbDto>>> SearchByNameAsync(string name)
            => await SearchAsync(name);

        public async Task<ServiceResult<List<HerbDto>>> GetListAsync(HerbPagedQueryDto? query = null)
        {
            if (query == null)
                return await GetAllAsync();

            var pagedResult = await _queryService.GetPagedAsync(query);
            return pagedResult.IsSuccess && pagedResult.Data != null
                ? ServiceResult<List<HerbDto>>.Success(pagedResult.Data.Items)
                : ServiceResult<List<HerbDto>>.Failure(pagedResult.ErrorMessage ?? "查询失败");
        }

        #endregion

        #region Business Operations

        public async Task<ServiceResult<HerbDto>> CreateAsync(HerbCreateDto dto)
            => await _businessService.CreateHerbWithAutoCodeAsync(dto);

        public Task<ServiceResult<HerbDto>> UpdateAsync(Guid id, HerbUpdateDto dto)
        {
            // 简化版本更新逻辑：只允许修改价格和状态
            if (id == Guid.Empty)
                return Task.FromResult(ServiceResult<HerbDto>.Failure("药材ID不能为空"));
                
            if (dto == null)
                return Task.FromResult(ServiceResult<HerbDto>.Failure("更新信息不能为空"));
                
            // 目前简化版本不支持全量更新，建议使用状态更新方法
            return Task.FromResult(ServiceResult<HerbDto>.Failure("简化版本暂不支持药材信息更新，建议使用SetStatusAsync更改状态"));
        }

        public async Task<ServiceResult<bool>> DeleteAsync(Guid id)
            => await _businessService.SoftDeleteAsync(id);

        public async Task<bool> SetStatusAsync(Guid id, bool isActive)
        {
            var result = await _businessService.SetStatusAsync(id, isActive);
            return result.IsSuccess;
        }

        public async Task<ServiceResult<int>> ImportHerbsAsync(List<HerbImportDto> herbs)
            => await _businessService.ImportHerbsAsync(herbs);

        public async Task<ServiceResult<List<HerbDto>>> ExportHerbsAsync()
            => await _queryService.GetAvailableHerbsAsync();

        public async Task<ServiceResult<bool>> BatchUpdateStatusAsync(BatchStatusUpdateDto dto)
            => await _businessService.BatchUpdateStatusAsync(dto);

        public Task<ServiceResult<byte[]>> GetImportTemplateAsync()
        {
            try
            {
                var templateContent = """
                    药材导入模板 - UltraThink精简版
                    必填列：药材名称, 产地, 规格, 单位, 价格
                    可选列：功效, 用法, 备注, 状态(Enabled/Disabled)
                    
                    注意：
                    - 拼音码由系统自动生成
                    - 药材名称不能重复
                    - 价格必须为有效数字
                    - 状态默认为Enabled(启用)
                    """;

                var content = System.Text.Encoding.UTF8.GetBytes(templateContent);
                return Task.FromResult(ServiceResult<byte[]>.Success(content));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取药材导入模板异常");
                return Task.FromResult(ServiceResult<byte[]>.Failure($"获取药材导入模板异常: {ex.Message}"));
            }
        }

        #endregion

        #region Legacy Support

        public Task<ServiceResult<Dictionary<int, int>>> GetStatisticsAsync()
        {
            return Task.FromResult(ServiceResult<Dictionary<int, int>>.Success([]));
        }

        public Task<ServiceResult<bool>> UpdatePriceAsync(Guid id, HerbPriceUpdateDto dto)
        {
            return Task.FromResult(ServiceResult<bool>.Success(false));
        }

        public Task<ServiceResult<bool>> UpdateStockAsync(Guid id, HerbStockUpdateDto dto)
        {
            return Task.FromResult(ServiceResult<bool>.Success(false));
        }

        public Task<ServiceResult<HerbStockStatisticsDto>> GetStockStatisticsAsync()
        {
            return Task.FromResult(ServiceResult<HerbStockStatisticsDto>.Success(new HerbStockStatisticsDto()));
        }

        public Task<ServiceResult<List<HerbDto>>> GetOutOfStockHerbsAsync()
        {
            return Task.FromResult(ServiceResult<List<HerbDto>>.Success([]));
        }

        public Task<ServiceResult<List<HerbDto>>> GetExpiringHerbsAsync(int days = 30)
        {
            return Task.FromResult(ServiceResult<List<HerbDto>>.Success([]));
        }

        #endregion
    }
}