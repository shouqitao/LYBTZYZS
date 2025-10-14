using LYBT.Desktop.Formula.Interfaces;
using LYBT.Desktop.Contracts.Api;
using LYBT.Desktop.Infrastructure.Repositories;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Formula;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Formula.Repositories
{
    /// <summary>
    /// 验方数据仓储实现 - RepositoryBase统一架构
    /// Project Standardization 3.0 - 迁移到统一RepositoryBase
    /// </summary>
    public class FormulaRepository : RepositoryBase<FormulaDto, FormulaCreateDto, FormulaUpdateDto, IFormulaApi>, IFormulaRepository
    {
        public FormulaRepository(
            IFormulaApi formulaApi,
            ILogger<FormulaRepository> logger)
            : base(formulaApi, logger)
        {
        }

        /// <summary>
        /// 克隆验方
        /// </summary>
        public async Task<FormulaDto> CloneFormulaAsync(Guid formulaId)
        {
            try
            {
                var response = await _api.CloneFormulaAsync(formulaId);
                return response.Content ?? throw new InvalidOperationException($"克隆验方失败，ID: {formulaId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "克隆验方失败，ID: {Id}", formulaId);
                throw;
            }
        }

        #region RepositoryBase抽象方法实现

        protected override Task<Refit.ApiResponse<FormulaDto>> CallApiGetByIdAsync(Guid id)
        {
            return _api.GetFormulaByIdAsync(id);
        }

        protected override Task<Refit.ApiResponse<PagedResult<FormulaDto>>> CallApiGetPagedAsync(int page, int pageSize, string? keyword)
        {
            return _api.GetFormulasAsync(page, pageSize, keyword);
        }

        protected override Task<Refit.ApiResponse<FormulaDto>> CallApiCreateAsync(FormulaCreateDto dto)
        {
            return _api.CreateFormulaAsync(dto);
        }

        protected override Task<Refit.ApiResponse<FormulaDto>> CallApiUpdateAsync(Guid id, FormulaUpdateDto dto)
        {
            return _api.UpdateFormulaAsync(id, dto);
        }

        protected override Task<Refit.ApiResponse<ApiResponse>> CallApiDeleteAsync(Guid id)
        {
            return _api.DeleteFormulaAsync(id);
        }

        protected override Guid? GetIdFromUpdateDto(FormulaUpdateDto dto)
        {
            return dto?.Id;
        }

        #endregion
    }
}