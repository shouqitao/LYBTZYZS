using LYBT.Desktop.Herbs.Interfaces;
using LYBT.Desktop.Contracts.Api;
using LYBT.Desktop.Infrastructure.Repositories;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Herbs;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Herbs.Repositories
{
    /// <summary>
    /// 药材数据仓储实现 - RepositoryBase统一架构
    /// Project Standardization 3.0 - 迁移到统一RepositoryBase
    /// </summary>
    public class HerbRepository : RepositoryBase<HerbDto, HerbCreateDto, HerbUpdateDto, IHerbApi>, IHerbRepository
    {
        public HerbRepository(
            IHerbApi herbApi,
            ILogger<HerbRepository> logger)
            : base(herbApi, logger)
        {
        }

        /// <summary>
        /// 获取所有草药列表（不分页，用于兼容旧代码）
        /// </summary>
        public async Task<List<HerbDto>> GetAllAsync()
        {
            try
            {
                // 获取第一页，大页数
                var pagedResult = await GetPagedAsync(1, 1000);
                return pagedResult.Items ?? new List<HerbDto>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取草药列表失败");
                throw;
            }
        }

        #region RepositoryBase抽象方法实现

        protected override Task<Refit.ApiResponse<HerbDto>> CallApiGetByIdAsync(Guid id)
        {
            return _api.GetHerbByIdAsync(id);
        }

        protected override Task<Refit.ApiResponse<PagedResult<HerbDto>>> CallApiGetPagedAsync(int page, int pageSize, string? keyword)
        {
            return _api.GetHerbsAsync(page, pageSize, keyword);
        }

        protected override Task<Refit.ApiResponse<HerbDto>> CallApiCreateAsync(HerbCreateDto dto)
        {
            return _api.CreateHerbAsync(dto);
        }

        protected override Task<Refit.ApiResponse<HerbDto>> CallApiUpdateAsync(Guid id, HerbUpdateDto dto)
        {
            return _api.UpdateHerbAsync(id, dto);
        }

        protected override Task<Refit.ApiResponse<ApiResponse>> CallApiDeleteAsync(Guid id)
        {
            return _api.DeleteHerbAsync(id);
        }

        protected override Guid? GetIdFromUpdateDto(HerbUpdateDto dto)
        {
            return dto?.Id;
        }

        #endregion
    }
}