using LYBT.Desktop.Foundation.Http;
using LYBT.Desktop.Foundation.Repositories;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Herbs;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Herbs.Repositories
{
    /// <summary>
    /// 药材数据仓储实现 - Phase 2模块化架构
    /// Issue #1114 - 支持CreateDto和UpdateDto
    /// </summary>
    public class HerbRepository : BaseApiRepository<HerbDto>, IHerbRepository
    {
        public HerbRepository(
            IApiService apiService,
            ILogger<HerbRepository> logger)
            : base(apiService, logger, "api/v1/herbs")
        {
        }

        public override Task<PagedResult<HerbDto>> GetPagedAsync(int page = 1, int pageSize = 20, string? keyword = null)
        {
            return base.GetPagedAsync(page, pageSize, keyword);
        }

        public override Task<HerbDto> GetByIdAsync(Guid id)
        {
            return base.GetByIdAsync(id);
        }

        public async Task<HerbDto> CreateAsync(HerbCreateDto dto)
        {
            if (dto == null)
                throw new ArgumentNullException(nameof(dto));

            return (await _apiService.PostAsync<HerbCreateDto, HerbDto>(_endpoint, dto))!;
        }

        public async Task<HerbDto> UpdateAsync(HerbUpdateDto dto)
        {
            if (dto?.Id == null || dto.Id == Guid.Empty)
            {
                _logger.LogError("Cannot update herb with null or invalid id");
                throw new ArgumentException("Herb ID is required", nameof(dto));
            }

            return (await _apiService.PutAsync<HerbUpdateDto, HerbDto>($"{_endpoint}/{dto.Id}", dto))!;
        }

        public override Task<bool> DeleteAsync(Guid id)
        {
            return base.DeleteAsync(id);
        }

        public override Task<List<HerbDto>> SearchAsync(string keyword)
        {
            return base.SearchAsync(keyword);
        }
    }
}
