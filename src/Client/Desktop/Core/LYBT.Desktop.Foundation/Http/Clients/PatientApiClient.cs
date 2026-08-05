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
        int page = 1, int pageSize = 20, string? keyword = null)
        => _api.GetPatientsAsync(page, pageSize, keyword);

    /// <inheritdoc />
    public Task<ApiResponse<PatientDetailDto>> GetPatientByIdAsync(Guid id)
        => _api.GetPatientByIdAsync(id);

    /// <inheritdoc />
    public Task<ApiResponse<PatientDetailDto>> CreatePatientAsync(PatientInputDto request)
        => _api.CreatePatientAsync(request);

    /// <inheritdoc />
    public Task<ApiResponse<PatientDetailDto>> UpdatePatientAsync(Guid id, PatientInputDto request)
        => _api.UpdatePatientAsync(id, request);

    /// <inheritdoc />
    public Task<ApiResponse> DeletePatientAsync(Guid id)
        => _api.DeletePatientAsync(id);

    /// <inheritdoc />
    public Task<ApiResponse<PatientBatchImportResultDto>> BatchImportAsync(PatientBatchImportInputDto request)
        => _api.BatchImportAsync(request);

    /// <inheritdoc />
    public Task<HttpResponseMessage> ExportTemplateAsync()
        => _api.ExportTemplateAsync();

    /// <inheritdoc />
    public Task<HttpResponseMessage> ExportPatientsAsync(string? keyword = null)
        => _api.ExportPatientsAsync(keyword);

    /// <inheritdoc />
    public Task<ApiResponse<BatchOperationResultDto>> BatchDeleteAsync(BatchDeleteInputDto request)
        => _api.BatchDeleteAsync(request);

    /// <inheritdoc />
    public Task<ApiResponse<PatientDetailDto>> ToggleStatusAsync(Guid id)
        => _api.ToggleStatusAsync(id);

    /// <inheritdoc />
    public Task<ApiResponse<PatientDetailDto>> RestoreAsync(Guid id)
        => _api.RestoreAsync(id);
}
