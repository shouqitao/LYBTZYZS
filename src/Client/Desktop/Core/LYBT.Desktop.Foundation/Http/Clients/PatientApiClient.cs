// ---------------------------------------------------------------------------
// PatientApiClient — Refit adapter for IApiClientPatients
// ---------------------------------------------------------------------------
// Delegates all calls to IPatientApi (Refit-generated HTTP client).
// Part of the IApiClient unified abstraction layer.
// ---------------------------------------------------------------------------

using System.Net.Http;
using LYBT.Desktop.Contracts.Api;
using LYBT.Desktop.Contracts.ApiClient;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Patients;

namespace LYBT.Desktop.Foundation.Http.Clients;

/// <summary>
/// 患者管理 API 客户端——包装 IPatientApi（Refit）以实现 IApiClientPatients。
/// </summary>
internal sealed class PatientApiClient : IApiClientPatients
{
    private readonly IPatientApi _api;

    /// <summary>
    /// 初始化 <see cref="PatientApiClient"/> 类的新实例。
    /// </summary>
    /// <param name="api">Refit-generated patient API client.</param>
    public PatientApiClient(IPatientApi api)
    {
        _api = api ?? throw new ArgumentNullException(nameof(api));
    }

    /// <inheritdoc />
    public Task<ApiResponse<PagedResult<PatientListDto>>> GetPatientsAsync(
        int page = 1, int pageSize = 20, string? keyword = null,
        CancellationToken ct = default)
        => _api.GetPatientsAsync(page, pageSize, keyword, ct);

    /// <inheritdoc />
    public Task<ApiResponse<PatientDetailDto>> GetPatientByIdAsync(Guid id, CancellationToken ct = default)
        => _api.GetPatientByIdAsync(id, ct);

    /// <inheritdoc />
    public Task<ApiResponse<PatientDetailDto>> CreatePatientAsync(PatientInputDto request, CancellationToken ct = default)
        => _api.CreatePatientAsync(request, ct);

    /// <inheritdoc />
    public Task<ApiResponse<PatientDetailDto>> UpdatePatientAsync(Guid id, PatientInputDto request, CancellationToken ct = default)
        => _api.UpdatePatientAsync(id, request, ct);

    /// <inheritdoc />
    public Task<ApiResponse> DeletePatientAsync(Guid id, CancellationToken ct = default)
        => _api.DeletePatientAsync(id, ct);

    /// <inheritdoc />
    public Task<ApiResponse<PatientBatchImportResultDto>> BatchImportAsync(PatientBatchImportInputDto request, CancellationToken ct = default)
        => _api.BatchImportAsync(request, ct);

    /// <inheritdoc />
    public Task<HttpResponseMessage> ExportTemplateAsync(CancellationToken ct = default)
        => _api.ExportTemplateAsync(ct);

    /// <inheritdoc />
    public Task<HttpResponseMessage> ExportPatientsAsync(string? keyword = null, CancellationToken ct = default)
        => _api.ExportPatientsAsync(keyword, ct);

    /// <inheritdoc />
    public Task<ApiResponse<BatchOperationResultDto>> BatchDeleteAsync(BatchDeleteInputDto request, CancellationToken ct = default)
        => _api.BatchDeleteAsync(request, ct);

    /// <inheritdoc />
    public Task<ApiResponse<PatientDetailDto>> ToggleStatusAsync(Guid id, CancellationToken ct = default)
        => _api.ToggleStatusAsync(id, ct);

    /// <inheritdoc />
    public Task<ApiResponse<PatientDetailDto>> RestoreAsync(Guid id, CancellationToken ct = default)
        => _api.RestoreAsync(id, ct);
}
