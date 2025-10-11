using LYBT.Desktop.Foundation.Http;
using LYBT.Desktop.Foundation.Repositories;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Formula;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Formula.Repositories
{
    /// <summary>
    /// 验方数据仓储实现 - Phase 2模块化架构
    /// Issue #1114 - 支持CreateDto和UpdateDto
    /// </summary>
    public class FormulaRepository : BaseApiRepository<FormulaDto>, IFormulaRepository
    {
        public FormulaRepository(
            IApiService apiService,
            ILogger<FormulaRepository> logger)
            : base(apiService, logger, "api/v1/formulas")
        {
        }

        public override Task<PagedResult<FormulaDto>> GetPagedAsync(int page = 1, int pageSize = 20, string? keyword = null)
        {
            return base.GetPagedAsync(page, pageSize, keyword);
        }

        public override Task<FormulaDto> GetByIdAsync(Guid id)
        {
            return base.GetByIdAsync(id);
        }

        public async Task<FormulaDto> CreateAsync(FormulaCreateDto dto)
        {
            if (dto == null)
                throw new ArgumentNullException(nameof(dto));

            return (await _apiService.PostAsync<FormulaCreateDto, FormulaDto>(_endpoint, dto))!;
        }

        public async Task<FormulaDto> UpdateAsync(FormulaUpdateDto dto)
        {
            if (dto?.Id == null || dto.Id == Guid.Empty)
            {
                _logger.LogError("Cannot update formula with null or invalid id");
                throw new ArgumentException("Formula ID is required", nameof(dto));
            }

            return (await _apiService.PutAsync<FormulaUpdateDto, FormulaDto>($"{_endpoint}/{dto.Id}", dto))!;
        }

        public override Task<bool> DeleteAsync(Guid id)
        {
            return base.DeleteAsync(id);
        }

        public override Task<List<FormulaDto>> SearchAsync(string keyword)
        {
            return base.SearchAsync(keyword);
        }

        public async Task<FormulaDto> CloneFormulaAsync(Guid formulaId)
        {
            return (await _apiService.PostAsync<object, FormulaDto>($"{_endpoint}/{formulaId}/clone", new { }))!;
        }
    }
}
