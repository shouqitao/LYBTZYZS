using LYBT.Desktop.Contracts.ApiClient;
using LYBT.Desktop.Contracts.Repositories;
using LYBT.Desktop.Foundation.Repositories;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Formula;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Formula.Repositories;

/// <summary>
/// 验方仓储 — routes all calls through IApiClient.
/// </summary>
public sealed class FormulaRepository : ApiClientRepositoryBase<FormulaListDto, FormulaDetailDto>, IFormulaRepository
{
    private readonly IApiClient _apiClient;

    public FormulaRepository(
        IApiClient apiClient,
        ILogger<FormulaRepository> logger)
        : base(logger)
    {
        _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
    }

    protected override string LogPrefix => "Formula";

    #region 标准 CRUD 操作

    public async Task<PagedResult<FormulaListDto>> GetPagedAsync(int page = 1, int pageSize = 20, string? keyword = null, string? category = null, CancellationToken ct = default)
    {
        return await ExecuteAsync(
            async () =>
            {
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
            },
            "GetPaged",
            "[REPO] Formula.GetPaged - Page={Page} PageSize={PageSize} Keyword={Keyword} Category={Category}",
            [page, pageSize, keyword, category]);
    }

    public async Task<FormulaDetailDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await ExecuteAsync(
            async () =>
            {
                var response = await _apiClient.Formulas.GetFormulaByIdAsync(id);
                return response.Data;
            },
            "GetById");
    }

    public async Task<FormulaDetailDto> CreateAsync(FormulaInputDto dto, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(dto);

        return await ExecuteAsync(
            async () =>
            {
                var response = await _apiClient.Formulas.CreateFormulaAsync(dto);
                if (!response.Success || response.Data == null)
                    throw new InvalidOperationException(response.Message ?? "创建验方失败");

                Logger.LogInformation("[REPO] Formula.Create completed - Id={Id}", response.Data.Id);
                return response.Data;
            },
            "Create",
            LogLevel.Information);
    }

    public async Task<FormulaDetailDto> UpdateAsync(FormulaInputDto dto, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(dto);
        if (dto.Id is null || dto.Id == Guid.Empty)
            throw new ArgumentException("更新DTO必须包含有效的ID", nameof(dto));

        return await ExecuteAsync(
            async () =>
            {
                var response = await _apiClient.Formulas.UpdateFormulaAsync(dto.Id.Value, dto);
                if (!response.Success || response.Data == null)
                    throw new InvalidOperationException(response.Message ?? "更新验方失败");

                Logger.LogInformation("[REPO] Formula.Update completed - Id={Id}", response.Data.Id);
                return response.Data;
            },
            "Update",
            LogLevel.Information);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        await ExecuteAsync(
            async () =>
            {
                var response = await _apiClient.Formulas.DeleteFormulaAsync(id);
                if (!response.Success)
                    throw new InvalidOperationException(response.Message ?? "删除验方失败");

                Logger.LogInformation("[REPO] Formula.Delete completed - Id={Id}", id);
            },
            "Delete",
            LogLevel.Information);
    }

    public async Task<List<FormulaListDto>> SearchAsync(string keyword, CancellationToken ct = default)
    {
        return await ExecuteAsync(
            async () =>
            {
                var response = await _apiClient.Formulas.GetFormulasAsync(1, 100, keyword, null);
                if (response.Data == null)
                    return [];

                return response.Data.Items.ToList();
            },
            "Search");
    }

    #endregion

    #region 验方专用方法

    public async Task<FormulaDetailDto> CloneFormulaAsync(Guid formulaId, CancellationToken ct = default)
    {
        return await ExecuteAsync(
            async () =>
            {
                var response = await _apiClient.Formulas.CloneFormulaAsync(formulaId);
                if (!response.Success || response.Data == null)
                    throw new InvalidOperationException(response.Message ?? $"克隆验方失败，ID: {formulaId}");

                Logger.LogInformation("[REPO] Formula.Clone completed - OriginalId={OriginalId} ClonedId={ClonedId}",
                    formulaId, response.Data.Id);
                return response.Data;
            },
            "Clone",
            LogLevel.Information);
    }

    #endregion

    #region 状态切换、恢复和批量操作

    public async Task<FormulaDetailDto?> ToggleStatusAsync(Guid id, CancellationToken ct = default)
    {
        return await ExecuteAsync(
            async () =>
            {
                var response = await _apiClient.Formulas.ToggleStatusAsync(id);
                if (!response.Success || response.Data == null)
                    throw new InvalidOperationException(response.Message ?? "切换验方状态失败");

                Logger.LogInformation("[REPO] Formula.ToggleStatus completed - Status={Status}", response.Data.Status);
                return response.Data;
            },
            "ToggleStatus",
            LogLevel.Information);
    }

    public async Task<BatchOperationResultDto?> BatchDeleteAsync(List<Guid> ids, CancellationToken ct = default)
    {
        // Returns failure DTO on exception instead of rethrowing — keep manual try/catch.
        try
        {
            Logger.LogInformation("[REPO] Formula.BatchDelete - Count={Count}", ids.Count);

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
            Logger.LogError(ex, "[REPO] Formula.BatchDelete failed");
            return new BatchOperationResultDto
            {
                TotalCount = ids.Count,
                FailureCount = ids.Count,
                IsSuccess = false,
                Message = ex.Message
            };
        }
    }

    #endregion

    #region 批量导入/导出

    public async Task<FormulaBatchImportResultDto?> BatchImportAsync(FormulaBatchImportInputDto request, CancellationToken ct = default)
    {
        // Returns null on failure instead of rethrowing — keep manual try/catch.
        try
        {
            Logger.LogInformation("[REPO] Formula.BatchImport started");

            var response = await _apiClient.Formulas.BatchImportAsync(request);
            if (!response.Success || response.Data == null)
            {
                Logger.LogWarning("[REPO] Formula.BatchImport failed: {Message}", response.Message);
                return null;
            }

            Logger.LogInformation("[REPO] Formula.BatchImport completed - Success={Success}, Failed={Failed}",
                response.Data.SuccessCount, response.Data.FailureCount);
            return response.Data;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "[REPO] Formula.BatchImport failed");
            return null;
        }
    }

    public async Task<byte[]?> ExportFormulasAsync(string? category = null, CancellationToken ct = default)
    {
        // Returns null on failure instead of rethrowing — keep manual try/catch.
        try
        {
            Logger.LogInformation("[REPO] Formula.ExportFormulas - Category={Category}", category);

            var response = await _apiClient.Formulas.ExportFormulasAsync(category);
            if (!response.IsSuccessStatusCode)
            {
                Logger.LogWarning("[REPO] Formula.ExportFormulas failed: StatusCode={StatusCode}", response.StatusCode);
                return null;
            }

            var data = await response.Content.ReadAsByteArrayAsync(ct);
            Logger.LogInformation("[REPO] Formula.ExportFormulas completed - Size={Size} bytes", data.Length);
            return data;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "[REPO] Formula.ExportFormulas failed");
            return null;
        }
    }

    public async Task<byte[]?> ExportTemplateAsync(CancellationToken ct = default)
    {
        // Returns null on failure instead of rethrowing — keep manual try/catch.
        try
        {
            Logger.LogInformation("[REPO] Formula.ExportTemplate started");

            var response = await _apiClient.Formulas.ExportTemplateAsync();
            if (!response.IsSuccessStatusCode)
            {
                Logger.LogWarning("[REPO] Formula.ExportTemplate failed: StatusCode={StatusCode}", response.StatusCode);
                return null;
            }

            var data = await response.Content.ReadAsByteArrayAsync(ct);
            Logger.LogInformation("[REPO] Formula.ExportTemplate completed - Size={Size} bytes", data.Length);
            return data;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "[REPO] Formula.ExportTemplate failed");
            return null;
        }
    }

    #endregion
}
