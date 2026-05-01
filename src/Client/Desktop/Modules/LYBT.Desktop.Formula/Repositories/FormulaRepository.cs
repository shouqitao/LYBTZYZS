using LYBT.Desktop.Contracts.Api;
using LYBT.Desktop.Contracts.Repositories;
using LYBT.Desktop.Contracts.Services;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Formula;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Formula.Repositories;

/// <summary>
/// 验方仓储 - 通过 Refit IFormulaApi 访问 WebAPI。
/// </summary>
public sealed class FormulaRepository : IFormulaRepository
{
    private readonly IFormulaApi _api;
    private readonly ILocalFormulaApi _localApi;
    private readonly IApiRouter _apiRouter;
    private readonly ILogger<FormulaRepository> _logger;

    private bool IsOffline => _apiRouter.IsOffline;

    public FormulaRepository(
        IFormulaApi api,
        ILocalFormulaApi localApi,
        IApiRouter apiRouter,
        ILogger<FormulaRepository> logger)
    {
        _api = api ?? throw new ArgumentNullException(nameof(api));
        _localApi = localApi ?? throw new ArgumentNullException(nameof(localApi));
        _apiRouter = apiRouter ?? throw new ArgumentNullException(nameof(apiRouter));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    #region 标准 CRUD 操作

    public async Task<PagedResult<FormulaListDto>> GetPagedAsync(int page = 1, int pageSize = 20, string? keyword = null, string? category = null)
    {
        try
        {
            _logger.LogDebug("[REPO:Remote] Formula.GetPaged - Page={Page} PageSize={PageSize} Keyword={Keyword} Category={Category}",
                page, pageSize, keyword, category);

            var response = await _api.GetFormulasAsync(page, pageSize, keyword, category);
            if (response.Data == null)
                return new PagedResult<FormulaListDto> { Items = [], TotalCount = 0, CurrentPage = page, PageSize = pageSize };

            return new PagedResult<FormulaListDto>
            {
                Items = response.Data.Items.ToList(),
                TotalCount = response.Data.TotalCount,
                CurrentPage = page,
                PageSize = pageSize
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[REPO:Remote] Formula.GetPaged failed");
            throw;
        }
    }

    public async Task<FormulaDetailDto?> GetByIdAsync(Guid id)
    {
        try
        {
            _logger.LogDebug("[REPO:Remote] Formula.GetById - Id={Id}", id);

            var response = await _api.GetFormulaByIdAsync(id);
            return response.Data;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[REPO:Remote] Formula.GetById failed - Id={Id}", id);
            throw;
        }
    }

    public async Task<FormulaDetailDto> CreateAsync(FormulaInputDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        try
        {
            _logger.LogInformation("[REPO:Remote] Formula.Create started - Name={Name}", dto.Name);

            var response = await _api.CreateFormulaAsync(dto);
            if (!response.Success || response.Data == null)
                throw new InvalidOperationException(response.Message ?? "创建验方失败");

            _logger.LogInformation("[REPO:Remote] Formula.Create completed - Id={Id}", response.Data.Id);
            return response.Data;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[REPO:Remote] Formula.Create failed - Name={Name}", dto.Name);
            throw;
        }
    }

    public async Task<FormulaDetailDto> UpdateAsync(FormulaInputDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);
        if (dto.Id is null || dto.Id == Guid.Empty)
            throw new ArgumentException("更新DTO必须包含有效的ID", nameof(dto));

        try
        {
            _logger.LogInformation("[REPO:Remote] Formula.Update - Id={Id}", dto.Id);

            var response = await _api.UpdateFormulaAsync(dto.Id.Value, dto);
            if (!response.Success || response.Data == null)
                throw new InvalidOperationException(response.Message ?? "更新验方失败");

            _logger.LogInformation("[REPO:Remote] Formula.Update completed - Id={Id}", response.Data.Id);
            return response.Data;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[REPO:Remote] Formula.Update failed - Id={Id}", dto.Id);
            throw;
        }
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        try
        {
            _logger.LogInformation("[REPO:Remote] Formula.Delete - Id={Id}", id);

            var response = await _api.DeleteFormulaAsync(id);
            if (response.Success)
                _logger.LogInformation("[REPO:Remote] Formula.Delete completed - Id={Id}", id);
            else
                _logger.LogWarning("[REPO:Remote] Formula.Delete failed - Id={Id}", id);

            return response.Success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[REPO:Remote] Formula.Delete failed - Id={Id}", id);
            return false;
        }
    }

    public async Task<List<FormulaListDto>> SearchAsync(string keyword)
    {
        try
        {
            _logger.LogDebug("[REPO:Remote] Formula.Search - Keyword={Keyword}", keyword);

            var response = await _api.GetFormulasAsync(1, 100, keyword, null);
            if (response.Data == null)
                return [];

            return response.Data.Items.ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[REPO:Remote] Formula.Search failed");
            throw;
        }
    }

    #endregion

    #region 验方专用方法

    public async Task<FormulaDetailDto> CloneFormulaAsync(Guid formulaId)
    {
        try
        {
            _logger.LogInformation("[REPO:Remote] Formula.Clone - Id={Id}", formulaId);

            var response = await _api.CloneFormulaAsync(formulaId);
            if (!response.Success || response.Data == null)
                throw new InvalidOperationException(response.Message ?? $"克隆验方失败，ID: {formulaId}");

            _logger.LogInformation("[REPO:Remote] Formula.Clone completed - OriginalId={OriginalId} ClonedId={ClonedId}",
                formulaId, response.Data.Id);
            return response.Data;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[REPO:Remote] Formula.Clone failed - Id={Id}", formulaId);
            throw;
        }
    }

    // OpenSpec: cleanup-formula-dead-code - 已删除 GetPendingValidationFormulasAsync/ValidateFormulaHerbAsync

    #endregion

    #region 状态切换、恢复和批量操作

    public async Task<FormulaDetailDto?> ToggleStatusAsync(Guid id)
    {
        try
        {
            _logger.LogInformation("[REPO:Remote] Formula.ToggleStatus - Id={Id}", id);

            var response = await _api.ToggleStatusAsync(id);
            if (!response.Success || response.Data == null)
            {
                _logger.LogWarning("[REPO:Remote] Formula.ToggleStatus failed: {Message}", response.Message);
                return null;
            }

            _logger.LogInformation("[REPO:Remote] Formula.ToggleStatus completed - Status={Status}", response.Data.Status);
            return response.Data;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[REPO:Remote] Formula.ToggleStatus failed - Id={Id}", id);
            return null;
        }
    }

    public async Task<FormulaDetailDto?> RestoreAsync(Guid id)
    {
        try
        {
            _logger.LogInformation("[REPO:Remote] Formula.Restore - Id={Id}", id);

            var response = await _api.RestoreAsync(id);
            if (!response.Success || response.Data == null)
            {
                _logger.LogWarning("[REPO:Remote] Formula.Restore failed: {Message}", response.Message);
                return null;
            }

            return response.Data;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[REPO:Remote] Formula.Restore failed - Id={Id}", id);
            return null;
        }
    }

    public async Task<BatchOperationResultDto?> BatchDeleteAsync(List<Guid> ids)
    {
        try
        {
            _logger.LogInformation("[REPO:Remote] Formula.BatchDelete - Count={Count}", ids.Count);

            var response = await _api.BatchDeleteAsync(new BatchDeleteInputDto { Ids = ids });
            if (!response.Success || response.Data == null)
            {
                return new BatchOperationResultDto
                {
                    TotalCount = ids.Count,
                    FailureCount = ids.Count,
                    IsSuccess = false,
                    Message = response.Message ?? "批量删除失败"
                };
            }

            return response.Data;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[REPO:Remote] Formula.BatchDelete failed");
            return new BatchOperationResultDto
            {
                TotalCount = ids.Count,
                FailureCount = ids.Count,
                IsSuccess = false,
                Message = ex.Message
            };
        }
    }

    public async Task<BatchOperationResultDto?> BatchEnableAsync(List<Guid> ids)
    {
        try
        {
            _logger.LogInformation("[REPO:Remote] Formula.BatchEnable - Count={Count}", ids.Count);

            var response = await _api.BatchEnableAsync(new BatchDeleteInputDto { Ids = ids });
            if (!response.Success || response.Data == null)
            {
                _logger.LogWarning("[REPO:Remote] Formula.BatchEnable failed: {Message}", response.Message);
                return null;
            }

            return response.Data;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[REPO:Remote] Formula.BatchEnable failed");
            return null;
        }
    }

    public async Task<BatchOperationResultDto?> BatchDisableAsync(List<Guid> ids)
    {
        try
        {
            _logger.LogInformation("[REPO:Remote] Formula.BatchDisable - Count={Count}", ids.Count);

            var response = await _api.BatchDisableAsync(new BatchDeleteInputDto { Ids = ids });
            if (!response.Success || response.Data == null)
            {
                _logger.LogWarning("[REPO:Remote] Formula.BatchDisable failed: {Message}", response.Message);
                return null;
            }

            return response.Data;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[REPO:Remote] Formula.BatchDisable failed");
            return null;
        }
    }

    #endregion

    #region 批量导入/导出

    public async Task<FormulaBatchImportResultDto?> BatchImportAsync(FormulaBatchImportInputDto request, CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("[REPO:Remote] Formula.BatchImport started");

            var response = await _api.BatchImportAsync(request);
            if (!response.Success || response.Data == null)
            {
                _logger.LogWarning("[REPO:Remote] Formula.BatchImport failed: {Message}", response.Message);
                return null;
            }

            _logger.LogInformation("[REPO:Remote] Formula.BatchImport completed - Success={Success}, Failed={Failed}",
                response.Data.SuccessCount, response.Data.FailureCount);
            return response.Data;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[REPO:Remote] Formula.BatchImport failed");
            return null;
        }
    }

    public async Task<byte[]?> ExportFormulasAsync(string? category = null, CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("[REPO:Remote] Formula.ExportFormulas - Category={Category}", category);

            var response = await _api.ExportFormulasAsync(category);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("[REPO:Remote] Formula.ExportFormulas failed: StatusCode={StatusCode}", response.StatusCode);
                return null;
            }

            var data = await response.Content.ReadAsByteArrayAsync(ct);
            _logger.LogInformation("[REPO:Remote] Formula.ExportFormulas completed - Size={Size} bytes", data.Length);
            return data;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[REPO:Remote] Formula.ExportFormulas failed");
            return null;
        }
    }

    public async Task<byte[]?> ExportTemplateAsync(CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("[REPO:Remote] Formula.ExportTemplate started");

            var response = await _api.ExportTemplateAsync();
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("[REPO:Remote] Formula.ExportTemplate failed: StatusCode={StatusCode}", response.StatusCode);
                return null;
            }

            var data = await response.Content.ReadAsByteArrayAsync(ct);
            _logger.LogInformation("[REPO:Remote] Formula.ExportTemplate completed - Size={Size} bytes", data.Length);
            return data;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[REPO:Remote] Formula.ExportTemplate failed");
            return null;
        }
    }

    #endregion
}
