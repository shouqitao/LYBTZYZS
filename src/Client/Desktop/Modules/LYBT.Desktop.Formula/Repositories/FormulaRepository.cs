using LYBT.Desktop.Contracts.ApiClient;
using LYBT.Desktop.Contracts.Repositories;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Formula;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Formula.Repositories;

/// <summary>
/// 验方仓储 — routes all calls through IApiClient.
/// </summary>
public sealed class FormulaRepository : IFormulaRepository
{
    private readonly IApiClient _apiClient;
    private readonly ILogger<FormulaRepository> _logger;

    public FormulaRepository(
        IApiClient apiClient,
        ILogger<FormulaRepository> logger)
    {
        _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    #region 标准 CRUD 操作

    public async Task<PagedResult<FormulaListDto>> GetPagedAsync(int page = 1, int pageSize = 20, string? keyword = null, string? category = null)
    {
        try
        {
            _logger.LogDebug("[REPO] Formula.GetPaged - Page={Page} PageSize={PageSize} Keyword={Keyword} Category={Category}",
                page, pageSize, keyword, category);

            var response = await _apiClient.Formulas.GetFormulasAsync(page, pageSize, keyword, category);
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
            _logger.LogError(ex, "[REPO] Formula.GetPaged failed");
            throw;
        }
    }

    public async Task<FormulaDetailDto?> GetByIdAsync(Guid id)
    {
        try
        {
            _logger.LogDebug("[REPO] Formula.GetById - Id={Id}", id);
            var response = await _apiClient.Formulas.GetFormulaByIdAsync(id);
            return response.Data;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[REPO] Formula.GetById failed - Id={Id}", id);
            throw;
        }
    }

    public async Task<FormulaDetailDto> CreateAsync(FormulaInputDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        try
        {
            _logger.LogInformation("[REPO] Formula.Create started - Name={Name}", dto.Name);

            var response = await _apiClient.Formulas.CreateFormulaAsync(dto);
            if (!response.Success || response.Data == null)
                throw new InvalidOperationException(response.Message ?? "创建验方失败");

            _logger.LogInformation("[REPO] Formula.Create completed - Id={Id}", response.Data.Id);
            return response.Data;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[REPO] Formula.Create failed - Name={Name}", dto.Name);
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
            _logger.LogInformation("[REPO] Formula.Update - Id={Id}", dto.Id);

            var response = await _apiClient.Formulas.UpdateFormulaAsync(dto.Id.Value, dto);
            if (!response.Success || response.Data == null)
                throw new InvalidOperationException(response.Message ?? "更新验方失败");

            _logger.LogInformation("[REPO] Formula.Update completed - Id={Id}", response.Data.Id);
            return response.Data;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[REPO] Formula.Update failed - Id={Id}", dto.Id);
            throw;
        }
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        try
        {
            _logger.LogInformation("[REPO] Formula.Delete - Id={Id}", id);

            var response = await _apiClient.Formulas.DeleteFormulaAsync(id);
            if (response.Success)
                _logger.LogInformation("[REPO] Formula.Delete completed - Id={Id}", id);
            else
                _logger.LogWarning("[REPO] Formula.Delete failed - Id={Id}", id);

            return response.Success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[REPO] Formula.Delete failed - Id={Id}", id);
            return false;
        }
    }

    public async Task<List<FormulaListDto>> SearchAsync(string keyword)
    {
        try
        {
            _logger.LogDebug("[REPO] Formula.Search - Keyword={Keyword}", keyword);
            var response = await _apiClient.Formulas.GetFormulasAsync(1, 100, keyword, null);
            if (response.Data == null)
                return [];

            return response.Data.Items.ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[REPO] Formula.Search failed");
            throw;
        }
    }

    #endregion

    #region 验方专用方法

    public async Task<FormulaDetailDto> CloneFormulaAsync(Guid formulaId)
    {
        try
        {
            _logger.LogInformation("[REPO] Formula.Clone - Id={Id}", formulaId);

            var response = await _apiClient.Formulas.CloneFormulaAsync(formulaId);
            if (!response.Success || response.Data == null)
                throw new InvalidOperationException(response.Message ?? $"克隆验方失败，ID: {formulaId}");

            _logger.LogInformation("[REPO] Formula.Clone completed - OriginalId={OriginalId} ClonedId={ClonedId}",
                formulaId, response.Data.Id);
            return response.Data;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[REPO] Formula.Clone failed - Id={Id}", formulaId);
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
            _logger.LogInformation("[REPO] Formula.ToggleStatus - Id={Id}", id);

            var response = await _apiClient.Formulas.ToggleStatusAsync(id);
            if (!response.Success || response.Data == null)
            {
                _logger.LogWarning("[REPO] Formula.ToggleStatus failed: {Message}", response.Message);
                return null;
            }

            _logger.LogInformation("[REPO] Formula.ToggleStatus completed - Status={Status}", response.Data.Status);
            return response.Data;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[REPO] Formula.ToggleStatus failed - Id={Id}", id);
            return null;
        }
    }

    public async Task<FormulaDetailDto?> RestoreAsync(Guid id)
    {
        try
        {
            _logger.LogInformation("[REPO] Formula.Restore - Id={Id}", id);

            var response = await _apiClient.Formulas.RestoreAsync(id);
            if (!response.Success || response.Data == null)
            {
                _logger.LogWarning("[REPO] Formula.Restore failed: {Message}", response.Message);
                return null;
            }

            return response.Data;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[REPO] Formula.Restore failed - Id={Id}", id);
            return null;
        }
    }

    public async Task<BatchOperationResultDto?> BatchDeleteAsync(List<Guid> ids)
    {
        try
        {
            _logger.LogInformation("[REPO] Formula.BatchDelete - Count={Count}", ids.Count);

            var response = await _apiClient.Formulas.BatchDeleteAsync(new BatchDeleteInputDto { Ids = ids });
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
            _logger.LogError(ex, "[REPO] Formula.BatchDelete failed");
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
            _logger.LogInformation("[REPO] Formula.BatchEnable - Count={Count}", ids.Count);

            var response = await _apiClient.Formulas.BatchEnableAsync(new BatchDeleteInputDto { Ids = ids });
            if (!response.Success || response.Data == null)
            {
                _logger.LogWarning("[REPO] Formula.BatchEnable failed: {Message}", response.Message);
                return null;
            }

            return response.Data;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[REPO] Formula.BatchEnable failed");
            return null;
        }
    }

    public async Task<BatchOperationResultDto?> BatchDisableAsync(List<Guid> ids)
    {
        try
        {
            _logger.LogInformation("[REPO] Formula.BatchDisable - Count={Count}", ids.Count);

            var response = await _apiClient.Formulas.BatchDisableAsync(new BatchDeleteInputDto { Ids = ids });
            if (!response.Success || response.Data == null)
            {
                _logger.LogWarning("[REPO] Formula.BatchDisable failed: {Message}", response.Message);
                return null;
            }

            return response.Data;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[REPO] Formula.BatchDisable failed");
            return null;
        }
    }

    #endregion

    #region 批量导入/导出

    public async Task<FormulaBatchImportResultDto?> BatchImportAsync(FormulaBatchImportInputDto request, CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("[REPO] Formula.BatchImport started");

            var response = await _apiClient.Formulas.BatchImportAsync(request);
            if (!response.Success || response.Data == null)
            {
                _logger.LogWarning("[REPO] Formula.BatchImport failed: {Message}", response.Message);
                return null;
            }

            _logger.LogInformation("[REPO] Formula.BatchImport completed - Success={Success}, Failed={Failed}",
                response.Data.SuccessCount, response.Data.FailureCount);
            return response.Data;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[REPO] Formula.BatchImport failed");
            return null;
        }
    }

    public async Task<byte[]?> ExportFormulasAsync(string? category = null, CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("[REPO] Formula.ExportFormulas - Category={Category}", category);

            var response = await _apiClient.Formulas.ExportFormulasAsync(category);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("[REPO] Formula.ExportFormulas failed: StatusCode={StatusCode}", response.StatusCode);
                return null;
            }

            var data = await response.Content.ReadAsByteArrayAsync(ct);
            _logger.LogInformation("[REPO] Formula.ExportFormulas completed - Size={Size} bytes", data.Length);
            return data;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[REPO] Formula.ExportFormulas failed");
            return null;
        }
    }

    public async Task<byte[]?> ExportTemplateAsync(CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("[REPO] Formula.ExportTemplate started");

            var response = await _apiClient.Formulas.ExportTemplateAsync();
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("[REPO] Formula.ExportTemplate failed: StatusCode={StatusCode}", response.StatusCode);
                return null;
            }

            var data = await response.Content.ReadAsByteArrayAsync(ct);
            _logger.LogInformation("[REPO] Formula.ExportTemplate completed - Size={Size} bytes", data.Length);
            return data;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[REPO] Formula.ExportTemplate failed");
            return null;
        }
    }

    #endregion
}
