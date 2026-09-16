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
/// 验方管理 API 客户端——包装 IFormulaApi（Refit）以实现 IApiClientFormulas。
/// </summary>
internal sealed class FormulaApiClient : IApiClientFormulas
{
    private readonly IFormulaApi _api;

    /// <summary>
    /// 初始化 <see cref="FormulaApiClient"/> 类的新实例。
    /// </summary>
    /// <param name="api">Refit-generated formula API client.</param>
    public FormulaApiClient(IFormulaApi api)
    {
        _api = api ?? throw new ArgumentNullException(nameof(api));
    }

    /// <inheritdoc />
    public Task<ApiResponse<PagedResult<FormulaListDto>>> GetFormulasAsync(
        int page = 1, int pageSize = 20, string? keyword = null, string? category = null,
        CancellationToken ct = default)
        => _api.GetFormulasAsync(page, pageSize, keyword, category, ct);

    /// <inheritdoc />
    public Task<ApiResponse<FormulaDetailDto>> GetFormulaByIdAsync(Guid id, CancellationToken ct = default)
        => _api.GetFormulaByIdAsync(id, ct);

    /// <inheritdoc />
    public Task<ApiResponse<FormulaDetailDto>> CreateFormulaAsync(FormulaInputDto request, CancellationToken ct = default)
        => _api.CreateFormulaAsync(request, ct);

    /// <inheritdoc />
    public Task<ApiResponse<FormulaDetailDto>> UpdateFormulaAsync(Guid id, FormulaInputDto request, CancellationToken ct = default)
        => _api.UpdateFormulaAsync(id, request, ct);

    /// <inheritdoc />
    public Task<ApiResponse> DeleteFormulaAsync(Guid id, CancellationToken ct = default)
        => _api.DeleteFormulaAsync(id, ct);

    /// <inheritdoc />
    public Task<ApiResponse<FormulaDetailDto>> CloneFormulaAsync(Guid id, CancellationToken ct = default)
        => _api.CloneFormulaAsync(id, ct);

    /// <inheritdoc />
    public Task<ApiResponse<FormulaDetailDto>> ToggleStatusAsync(Guid id, CancellationToken ct = default)
        => _api.ToggleStatusAsync(id, ct);

    /// <inheritdoc />
    public Task<ApiResponse<BatchOperationResultDto>> BatchDeleteAsync(BatchDeleteInputDto request, CancellationToken ct = default)
        => _api.BatchDeleteAsync(request, ct);

    /// <inheritdoc />
    public Task<ApiResponse<FormulaBatchImportResultDto>> BatchImportAsync(FormulaBatchImportInputDto request, CancellationToken ct = default)
        => _api.BatchImportAsync(request, ct);

    /// <inheritdoc />
    public Task<HttpResponseMessage> ExportFormulasAsync(string? category = null, CancellationToken ct = default)
        => _api.ExportFormulasAsync(category, ct);

    /// <inheritdoc />
    public Task<HttpResponseMessage> ExportTemplateAsync(CancellationToken ct = default)
        => _api.ExportTemplateAsync(ct);

    /// <inheritdoc />
    public Task<ApiResponse<FormulaDetailDto>> RestoreAsync(Guid id, CancellationToken ct = default)
        => _api.RestoreAsync(id, ct);

    /// <inheritdoc />
    public Task<ApiResponse<BatchOperationResultDto>> BatchEnableAsync(BatchDeleteInputDto request, CancellationToken ct = default)
        => _api.BatchEnableAsync(request, ct);

    /// <inheritdoc />
    public Task<ApiResponse<BatchOperationResultDto>> BatchDisableAsync(BatchDeleteInputDto request, CancellationToken ct = default)
        => _api.BatchDisableAsync(request, ct);

    /// <inheritdoc />
    public Task<ApiResponse<PagedResult<FormulaDetailDto>>> GetPendingValidationAsync(int page = 1, int pageSize = 20, CancellationToken ct = default)
        => _api.GetPendingValidationAsync(page, pageSize, ct);

    /// <inheritdoc />
    public Task<ApiResponse> ValidateHerbAsync(
        Guid formulaId, Guid herbItemId, ValidateFormulaHerbInputDto request,
        CancellationToken ct = default)
        => _api.ValidateHerbAsync(formulaId, herbItemId, request, ct);
}
