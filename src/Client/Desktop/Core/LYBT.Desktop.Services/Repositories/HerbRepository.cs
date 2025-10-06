using LYBT.Desktop.Services.Http;
using LYBT.Desktop.Services.Repositories.Interfaces;
using LYBT.Shared.Models.Contracts.Herbs;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Services.Repositories
{
    /// <summary>
    /// 草药数据仓储实现 - API集成 - UltraThink架构
    /// </summary>
    public class HerbRepository : BaseApiRepository<HerbDto>, IHerbRepository
    {
        public HerbRepository(
            IApiService apiService,
            ILogger<HerbRepository> logger)
            : base(apiService, logger, "api/Herbs")
        {
        }

        public override Task<List<HerbDto>> GetAllAsync()
        {
            return base.GetAllAsync();
        }

        public override Task<HerbDto> GetByIdAsync(Guid id)
        {
            return base.GetByIdAsync(id);
        }

        public override Task<HerbDto> CreateAsync(HerbDto herb)
        {
            return base.CreateAsync(herb);
        }

        public Task<HerbDto> UpdateAsync(HerbDto herb)
        {
            if (herb?.Id == null)
            {
                _logger.LogError("Cannot update herb with null or invalid id");
                return Task.FromResult<HerbDto>(null!);
            }
            return base.UpdateAsync(herb.Id, herb);
        }

        public override Task<bool> DeleteAsync(Guid id)
        {
            return base.DeleteAsync(id);
        }

        public override Task<List<HerbDto>> SearchAsync(string keyword)
        {
            return base.SearchAsync(keyword);
        }

        public async Task<List<HerbDto>> GetByCategoryAsync(string category)
        {
            try
            {
                var query = new { category };
                var result = await _apiService.GetAsync<List<HerbDto>>($"{_endpoint}/category", query);
                return result ?? new List<HerbDto>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting herbs by category: {category}");
                return new List<HerbDto>();
            }
        }

        public async Task<List<HerbDto>> GetFrequentlyUsedAsync(int limit = 10)
        {
            try
            {
                var query = new { limit };
                var result = await _apiService.GetAsync<List<HerbDto>>($"{_endpoint}/frequent", query);
                return result ?? new List<HerbDto>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting frequently used herbs with limit: {limit}");
                return new List<HerbDto>();
            }
        }
    }
}
