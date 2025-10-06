using LYBT.Desktop.Services.Http;
using LYBT.Desktop.Services.Repositories.Interfaces;
using LYBT.Shared.Models.Contracts.Formula;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Services.Repositories
{
    /// <summary>
    /// 配方数据仓储实现 - API集成 - UltraThink架构
    /// </summary>
    public class FormulaRepository : BaseApiRepository<FormulaDto>, IFormulaRepository
    {
        public FormulaRepository(
            IApiService apiService,
            ILogger<FormulaRepository> logger)
            : base(apiService, logger, "api/Formulas")
        {
        }

        public override Task<List<FormulaDto>> GetAllAsync()
        {
            return base.GetAllAsync();
        }

        public override Task<FormulaDto> GetByIdAsync(Guid id)
        {
            return base.GetByIdAsync(id);
        }

        public override Task<FormulaDto> CreateAsync(FormulaDto formula)
        {
            return base.CreateAsync(formula);
        }

        public Task<FormulaDto> UpdateAsync(FormulaDto formula)
        {
            if (formula?.Id == null)
            {
                _logger.LogError("Cannot update formula with null or invalid id");
                return Task.FromResult<FormulaDto>(null!);
            }
            return base.UpdateAsync(formula.Id, formula);
        }

        public override Task<bool> DeleteAsync(Guid id)
        {
            return base.DeleteAsync(id);
        }

        public override Task<List<FormulaDto>> SearchAsync(string keyword)
        {
            return base.SearchAsync(keyword);
        }

        public async Task<List<FormulaDto>> GetByCategoryAsync(string category)
        {
            try
            {
                var query = new { category };
                var result = await _apiService.GetAsync<List<FormulaDto>>($"{_endpoint}/category", query);
                return result ?? new List<FormulaDto>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting formulas by category: {category}");
                return new List<FormulaDto>();
            }
        }

        public async Task<FormulaDto> DuplicateFormulaAsync(Guid formulaId)
        {
            try
            {
                return (await _apiService.PostAsync<object, FormulaDto>($"{_endpoint}/{formulaId}/duplicate", null!))!;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"复制配方失败: {formulaId}");
                throw;
            }
        }

        public async Task<List<FormulaDto>> GetTemplatesAsync()
        {
            try
            {
                var result = await _apiService.GetAsync<List<FormulaDto>>($"{_endpoint}/templates");
                return result ?? new List<FormulaDto>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting formula templates");
                return new List<FormulaDto>();
            }
        }
    }
}
