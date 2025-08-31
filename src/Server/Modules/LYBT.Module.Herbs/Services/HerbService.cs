using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Interfaces.Services;
using LYBT.Module.Herbs.Services.Core;
using Microsoft.Extensions.Logging;

namespace LYBT.Module.Herbs.Services
{
    /// <summary>
    /// 药材服务 - UltraThink三层架构纯委托模式
    /// </summary>
    public class HerbService : IHerbService
    {
        private readonly HerbServiceCore _coreService;
        private readonly HerbQueryService _queryService;
        private readonly HerbBusinessService _businessService;
        private readonly ILogger<HerbService> _logger;

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

        #region Query Operations

        public async Task<ServiceResult<HerbDto>> GetByIdAsync(Guid id)
            => await _coreService.GetByIdAsync(id);

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

        #region Core Operations

        public async Task<ServiceResult<HerbDto>> CreateAsync(HerbCreateDto dto)
            => await _coreService.CreateAsync(dto);

        public async Task<ServiceResult<HerbDto>> UpdateAsync(Guid id, HerbUpdateDto dto)
            => await _coreService.UpdateAsync(id, dto);

        public async Task<ServiceResult<bool>> DeleteAsync(Guid id)
            => await _businessService.SoftDeleteAsync(id);

        #endregion

        #region Business Operations

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

        public async Task<ServiceResult<Dictionary<int, int>>> GetStatisticsAsync()
        {
            await Task.CompletedTask;
            return ServiceResult<Dictionary<int, int>>.Success([]);
        }

        public async Task<ServiceResult<bool>> UpdatePriceAsync(Guid id, HerbPriceUpdateDto dto)
        {
            await Task.CompletedTask;
            return ServiceResult<bool>.Success(false);
        }

        public async Task<ServiceResult<bool>> UpdateStockAsync(Guid id, HerbStockUpdateDto dto)
        {
            await Task.CompletedTask;
            return ServiceResult<bool>.Success(false);
        }

        public async Task<ServiceResult<HerbStockStatisticsDto>> GetStockStatisticsAsync()
        {
            await Task.CompletedTask;
            return ServiceResult<HerbStockStatisticsDto>.Success(new HerbStockStatisticsDto());
        }

        public async Task<ServiceResult<List<HerbDto>>> GetOutOfStockHerbsAsync()
        {
            await Task.CompletedTask;
            return ServiceResult<List<HerbDto>>.Success([]);
        }

        public async Task<ServiceResult<List<HerbDto>>> GetExpiringHerbsAsync(int days = 30)
        {
            await Task.CompletedTask;
            return ServiceResult<List<HerbDto>>.Success([]);
        }

        #endregion
    }
}