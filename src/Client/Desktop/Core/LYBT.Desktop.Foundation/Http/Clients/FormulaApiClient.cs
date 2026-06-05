// ---------------------------------------------------------------------------
// FormulaApiClient — Refit adapter for IApiClientFormulas
// ---------------------------------------------------------------------------
// Delegates remote calls to IFormulaApi (Refit-generated HTTP client).
// Local-only methods throw NotSupportedException in remote mode.
// ---------------------------------------------------------------------------

using System.Net.Http;
using LYBT.Desktop.Contracts.Api;
using LYBT.Desktop.Contracts.ApiClient;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Formula;

namespace LYBT.Desktop.Foundation.Http.Clients;

/// <summary>
/// Formula management API client — wraps IFormulaApi (Refit) to implement IApiClientFormulas.
/// </summary>
internal sealed class FormulaApiClient : IApiClientFormulas
{
    private readonly IFormulaApi _api;

    /// <summary>
    /// Initializes a new instance of <see cref="FormulaApiClient"/>.
    /// </summary>
    /// <param name="api">Refit-generated formula API client.</param>
    public FormulaApiClient(IFormulaApi api)
    {
        _api = api ?? throw new ArgumentNullException(nameof(api));
    }

    /// <inheritdoc />
    public Task<ApiResponse<PagedResult<FormulaListDto>>> GetFormulasAsync(
        int page = 1, int pageSize = 20, string? keyword = null, string? category = null)
        => _api.GetFormulasAsync(page, pageSize, keyword, category);

    /// <inheritdoc />
    public Task<ApiResponse<FormulaDetailDto>> GetFormulaByIdAsync(Guid id)
        => _api.GetFormulaByIdAsync(id);

    /// <inheritdoc />
    public Task<ApiResponse<FormulaDetailDto>> CreateFormulaAsync(FormulaInputDto request)
        => _api.CreateFormulaAsync(request);

    /// <inheritdoc />
    public Task<ApiResponse<FormulaDetailDto>> UpdateFormulaAsync(Guid id, FormulaInputDto request)
        => _api.UpdateFormulaAsync(id, request);

    /// <inheritdoc />
    public Task<ApiResponse> DeleteFormulaAsync(Guid id)
        => _api.DeleteFormulaAsync(id);

    /// <inheritdoc />
    public Task<ApiResponse<FormulaDetailDto>> CloneFormulaAsync(Guid id)
        => _api.CloneFormulaAsync(id);

    /// <inheritdoc />
    public Task<ApiResponse<FormulaDetailDto>> ToggleStatusAsync(Guid id)
        => _api.ToggleStatusAsync(id);

    /// <inheritdoc />
    public Task<ApiResponse<FormulaDetailDto>> RestoreAsync(Guid id)
        => _api.RestoreAsync(id);

    /// <inheritdoc />
    public Task<ApiResponse<BatchOperationResultDto>> BatchDeleteAsync(BatchDeleteInputDto request)
        => _api.BatchDeleteAsync(request);

    /// <inheritdoc />
    public Task<ApiResponse<BatchOperationResultDto>> BatchEnableAsync(BatchDeleteInputDto request)
        => _api.BatchEnableAsync(request);

    /// <inheritdoc />
    public Task<ApiResponse<BatchOperationResultDto>> BatchDisableAsync(BatchDeleteInputDto request)
        => _api.BatchDisableAsync(request);

    /// <inheritdoc />
    public Task<ApiResponse<FormulaBatchImportResultDto>> BatchImportAsync(FormulaBatchImportInputDto request)
        => _api.BatchImportAsync(request);

    /// <inheritdoc />
    public Task<HttpResponseMessage> ExportFormulasAsync(string? category = null)
        => _api.ExportFormulasAsync(category);

    /// <inheritdoc />
    public Task<HttpResponseMessage> ExportTemplateAsync()
        => _api.ExportTemplateAsync();

    /// <inheritdoc />
    /// <remarks>Local-only method — not available in remote Refit mode.</remarks>
    public Task<List<string>> GetCategoriesAsync()
        => throw new NotSupportedException("GetCategoriesAsync is a local-only method and is not available in remote mode.");
}
