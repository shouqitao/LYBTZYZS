using LYBT.Desktop.Contracts.Api;
using LYBT.Desktop.Formula.Interfaces;
using LYBT.Desktop.Infrastructure.Repositories;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Formula;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Formula.Repositories
{
    /// <summary>
    /// 验方数据仓储实现 - RESTful设计
    /// List返回轻量ListDto，Detail返回完整DetailDto
    /// </summary>
    public class FormulaRepository : RepositoryBase<FormulaDetailDto, FormulaListDto, FormulaInputDto, FormulaInputDto, IFormulaApi>, IFormulaRepository
    {
        public FormulaRepository(
            IFormulaApi formulaApi,
            ILogger<FormulaRepository> logger)
            : base(formulaApi, logger)
        {
        }

        #region RepositoryBase抽象方法实现

        protected override Task<ApiResponse<FormulaDetailDto>> CallApiGetByIdAsync(Guid id)
        {
            return _api.GetFormulaByIdAsync(id);
        }

        protected override Task<ApiResponse<PagedResult<FormulaListDto>>> CallApiGetPagedAsync(int page, int pageSize, string? keyword)
        {
            return _api.GetFormulasAsync(page, pageSize, keyword);
        }

        protected override Task<ApiResponse<FormulaDetailDto>> CallApiCreateAsync(FormulaInputDto dto)
        {
            return _api.CreateFormulaAsync(dto);
        }

        protected override Task<ApiResponse<FormulaDetailDto>> CallApiUpdateAsync(Guid id, FormulaInputDto dto)
        {
            return _api.UpdateFormulaAsync(id, dto);
        }

        protected override Task<ApiResponse> CallApiDeleteAsync(Guid id)
        {
            return _api.DeleteFormulaAsync(id);
        }

        protected override Guid? GetIdFromUpdateDto(FormulaInputDto dto)
        {
            return dto?.Id;
        }

        #endregion

        #region 扩展方法 - 支持分类过滤

        /// <summary>
        /// 分页查询验方列表（支持分类过滤）
        /// </summary>
        public async Task<PagedResult<FormulaListDto>> GetPagedAsync(int page = 1, int pageSize = 20, string? keyword = null, string? category = null)
        {
            try
            {
                var response = await _api.GetFormulasAsync(page, pageSize, keyword, category);
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

        #endregion

        #region 验方专用方法

        /// <summary>
        /// 克隆验方
        /// </summary>
        public async Task<FormulaDetailDto> CloneFormulaAsync(Guid formulaId)
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
        public async Task<List<FormulaDetailDto>> GetPendingValidationFormulasAsync()
        {
            try
            {
                var response = await _api.GetPendingValidationFormulasAsync();
                return response.Data ?? new List<FormulaDetailDto>();
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

        #endregion

        #region 状态切换、恢复和批量操作

        /// <summary>
        /// 切换验方状态（启用/禁用）
        /// </summary>
        public async Task<FormulaDetailDto?> ToggleStatusAsync(Guid id)
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
        public async Task<FormulaDetailDto?> RestoreAsync(Guid id)
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

        /// <inheritdoc />
        public async Task<BatchOperationResultDto?> BatchDeleteAsync(List<Guid> ids)
        {
            try
            {
                _logger.LogInformation("批量删除验方：{Count}个", ids.Count);
                var request = new BatchDeleteInputDto { Ids = ids };
                var response = await _api.BatchDeleteAsync(request);

                if (!response.Success || response.Data == null)
                {
                    _logger.LogError("批量删除验方失败：{Message}", response.Message);
                    return null;
                }

                _logger.LogInformation("批量删除验方完成：成功{Success}个，失败{Failure}个",
                    response.Data.SuccessCount, response.Data.FailureCount);
                return response.Data;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "批量删除验方时发生异常");
                return null;
            }
        }

        /// <inheritdoc />
        public async Task<BatchOperationResultDto?> BatchEnableAsync(List<Guid> ids)
        {
            try
            {
                _logger.LogInformation("批量启用验方：{Count}个", ids.Count);
                var request = new BatchDeleteInputDto { Ids = ids };
                var response = await _api.BatchEnableAsync(request);

                if (!response.Success || response.Data == null)
                {
                    _logger.LogError("批量启用验方失败：{Message}", response.Message);
                    return null;
                }

                _logger.LogInformation("批量启用验方完成：成功{Success}个，失败{Failure}个",
                    response.Data.SuccessCount, response.Data.FailureCount);
                return response.Data;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "批量启用验方时发生异常");
                return null;
            }
        }

        /// <inheritdoc />
        public async Task<BatchOperationResultDto?> BatchDisableAsync(List<Guid> ids)
        {
            try
            {
                _logger.LogInformation("批量禁用验方：{Count}个", ids.Count);
                var request = new BatchDeleteInputDto { Ids = ids };
                var response = await _api.BatchDisableAsync(request);

                if (!response.Success || response.Data == null)
                {
                    _logger.LogError("批量禁用验方失败：{Message}", response.Message);
                    return null;
                }

                _logger.LogInformation("批量禁用验方完成：成功{Success}个，失败{Failure}个",
                    response.Data.SuccessCount, response.Data.FailureCount);
                return response.Data;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "批量禁用验方时发生异常");
                return null;
            }
        }

        #endregion
    }
}
