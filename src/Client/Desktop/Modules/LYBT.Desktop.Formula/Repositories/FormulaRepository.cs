using LYBT.Desktop.Contracts.Api;
using LYBT.Desktop.Formula.Interfaces;
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
    public class FormulaRepository : RepositoryBase<FormulaDto, FormulaInputDto, FormulaInputDto, IFormulaApi>, IFormulaRepository
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

        /// <summary>
        /// 获取验方列表（返回FormulaListDto，用于列表视图）
        /// OpenSpec: optimize-entity-data-flow - 增量API方法
        /// </summary>
        public async Task<PagedResult<FormulaListDto>> GetPagedListAsync(int page = 1, int pageSize = 20, string? keyword = null, string? category = null)
        {
            try
            {
                var response = await _api.GetFormulasListAsync(page, pageSize, keyword, category);
                return response.Data ?? new PagedResult<FormulaListDto>
                {
                    Items = new List<FormulaListDto>(),
                    TotalCount = 0,
                    CurrentPage = page,
                    PageSize = pageSize
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取验方列表失败");
                throw;
            }
        }

        protected override Task<ApiResponse<FormulaDto>> CallApiCreateAsync(FormulaInputDto dto)
        {
            return _api.CreateFormulaAsync(dto);
        }

        protected override Task<ApiResponse<FormulaDto>> CallApiUpdateAsync(Guid id, FormulaInputDto dto)
        {
            return _api.UpdateFormulaAsync(id, dto);
        }

        protected override Task<ApiResponse<ApiResponse>> CallApiDeleteAsync(Guid id)
        {
            return _api.DeleteFormulaAsync(id);
        }

        protected override Guid? GetIdFromUpdateDto(FormulaInputDto dto)
        {
            return dto?.Id;
        }

        #endregion

        #region OpenSpec: optimize-module-list-ui - 状态切换和恢复

        /// <summary>
        /// 切换验方状态（启用/禁用）
        /// </summary>
        public async Task<FormulaDto?> ToggleStatusAsync(Guid id)
        {
            try
            {
                _logger.LogInformation("切换验方状态：{Id}", id);
                var response = await _api.ToggleStatusAsync(id);

                if (!response.Success || response.Data == null)
                {
                    _logger.LogError("切换验方状态失败：{Message}", response.Message);
                    return null;
                }

                _logger.LogInformation("验方状态已切换为：{Status}", response.Data.Status);
                return response.Data;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "切换验方状态时发生异常：{Id}", id);
                return null;
            }
        }

        /// <summary>
        /// 恢复已删除的验方
        /// </summary>
        public async Task<FormulaDto?> RestoreAsync(Guid id)
        {
            try
            {
                _logger.LogInformation("恢复验方：{Id}", id);
                var response = await _api.RestoreAsync(id);

                if (!response.Success || response.Data == null)
                {
                    _logger.LogError("恢复验方失败：{Message}", response.Message);
                    return null;
                }

                _logger.LogInformation("验方已恢复：{Id}", id);
                return response.Data;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "恢复验方时发生异常：{Id}", id);
                return null;
            }
        }

        #endregion
    }
}
