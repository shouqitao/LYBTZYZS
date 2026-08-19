using LYBT.Desktop.Contracts.ApiClient;
using LYBT.Desktop.Contracts.Repositories;
using LYBT.Desktop.Foundation.Repositories;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Formula;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Catalog.Repositories;

/// <summary>
/// 验方仓储 — routes all calls through IApiClient.
/// </summary>
public sealed class FormulaRepository : EntityApiClientRepositoryBase<FormulaListDto, FormulaDetailDto, FormulaInputDto>, IFormulaRepository
{
    private readonly IApiClientFormulas _formulas;

    public FormulaRepository(
        IApiClientFormulas formulas,
        ILogger<FormulaRepository> logger)
        : base(logger, formulas)
    {
        _formulas = formulas ?? throw new ArgumentNullException(nameof(formulas));
    }

    protected override string LogPrefix => "Formula";

    #region 搜索

    public async Task<List<FormulaListDto>> SearchAsync(string keyword, CancellationToken ct = default)
    {
        return await ExecuteAsync(
            async () =>
            {
                var response = await _formulas.GetFormulasAsync(1, 100, keyword, null);
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
                var response = await _formulas.CloneFormulaAsync(formulaId);
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
                var response = await _formulas.ToggleStatusAsync(id);
                if (!response.Success || response.Data == null)
                    throw new InvalidOperationException(response.Message ?? "切换验方状态失败");

                Logger.LogInformation("[REPO] Formula.ToggleStatus completed - Status={Status}", response.Data.Status);
                return response.Data;
            },
            "ToggleStatus",
            LogLevel.Information);
    }

    public async Task<FormulaDetailDto?> RestoreAsync(Guid id, CancellationToken ct = default)
    {
        return await ExecuteAsync(
            async () =>
            {
                var response = await _formulas.RestoreAsync(id);
                if (!response.Success || response.Data == null)
                    throw new InvalidOperationException(response.Message ?? "恢复验方失败");

                Logger.LogInformation("[REPO] Formula.Restore completed - Id={Id}", id);
                return response.Data;
            },
            "Restore",
            LogLevel.Information);
    }

    public async Task<BatchOperationResultDto?> BatchDeleteAsync(List<Guid> ids, CancellationToken ct = default)
    {
        return await ExecuteBatchDeleteAsync(
            () => _formulas.BatchDeleteAsync(new BatchDeleteInputDto { Ids = ids }),
            "BatchDelete",
            "批量删除失败",
            ids.Count);
    }

    #endregion

    #region 批量导入/导出

    public async Task<FormulaBatchImportResultDto?> BatchImportAsync(FormulaBatchImportInputDto request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        return await ExecuteImportAsync(
            () => _formulas.BatchImportAsync(request),
            "BatchImport",
            request.Formulas.Count,
            ct);
    }

    public async Task<byte[]?> ExportFormulasAsync(string? category = null, CancellationToken ct = default)
    {
        // Returns null on failure instead of rethrowing — keep manual try/catch.
        try
        {
            Logger.LogInformation("[REPO] Formula.ExportFormulas - Category={Category}", category);

            var response = await _formulas.ExportFormulasAsync(category);
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

            var response = await _formulas.ExportTemplateAsync();
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
