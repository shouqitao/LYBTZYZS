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
                return response.Data ?? throw new InvalidOperationException($"克隆验方失败，ID: {formulaId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "克隆验方失败，ID: {Id}", formulaId);
                throw;
            }
        }

        /// <summary>
        /// 获取待校验的验方列表 (Issue #1349)
        /// </summary>
        public async Task<List<FormulaDto>> GetPendingValidationFormulasAsync()
        {
            try
            {
                var response = await _api.GetPendingValidationFormulasAsync();
                return response.Data ?? new List<FormulaDto>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取待校验验方列表失败");
                throw;
            }
        }

        /// <summary>
        /// 验证验方药材 - 手动绑定药材到系统药材库 (Issue #1348)
        /// </summary>
        public async Task<bool> ValidateFormulaHerbAsync(Guid formulaId, Guid herbItemId, Guid selectedHerbId)
        {
            try
            {
                var response = await _api.ValidateFormulaHerbAsync(formulaId, herbItemId, selectedHerbId);
                return response.Success;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "验证验方药材失败，验方ID: {FormulaId}, 药材项ID: {HerbItemId}, 系统药材ID: {SelectedHerbId}",
                    formulaId, herbItemId, selectedHerbId);
                throw;
            }
        }

        #region RepositoryBase抽象方法实现

        protected override Task<ApiResponse<FormulaDto>> CallApiGetByIdAsync(Guid id)
        {
            return _api.GetFormulaByIdAsync(id);
        }

        protected override Task<ApiResponse<PagedResult<FormulaDto>>> CallApiGetPagedAsync(int page, int pageSize, string? keyword)
        {
            return _api.GetFormulasAsync(page, pageSize, keyword);
        }

        protected override Task<ApiResponse<FormulaDto>> CallApiCreateAsync(FormulaCreateDto dto)
        {
            return _api.CreateFormulaAsync(dto);
        }

        protected override Task<ApiResponse<FormulaDto>> CallApiUpdateAsync(Guid id, FormulaUpdateDto dto)
        {
            return _api.UpdateFormulaAsync(id, dto);
        }

        protected override Task<ApiResponse<ApiResponse>> CallApiDeleteAsync(Guid id)
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