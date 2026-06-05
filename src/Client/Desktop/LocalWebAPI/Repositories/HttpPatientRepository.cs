using Microsoft.Extensions.Logging;
using LYBT.Desktop.Contracts.ApiClient;
using LYBT.Desktop.Contracts.Repositories;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Patients;

namespace LYBT.LocalWebAPI.Repositories;

public class HttpPatientRepository : IPatientRepository
{
    private readonly IApiClient _apiClient;
    private readonly ILogger<HttpPatientRepository> _logger;

    public HttpPatientRepository(IApiClient apiClient, ILogger<HttpPatientRepository> logger)
    {
        _apiClient = apiClient;
        _logger = logger;
    }

    public async Task<PagedResult<PatientListDto>> GetPagedAsync(int page = 1, int pageSize = 20, string? keyword = null, CancellationToken ct = default)
    {
        var response = await _apiClient.Patients.GetPatientsAsync(page, pageSize, keyword);
        if (response.Data == null)
            return new PagedResult<PatientListDto>();
        return new PagedResult<PatientListDto>
        {
            Items = response.Data.Items.ToList(),
            TotalCount = response.Data.TotalCount,
            CurrentPage = page,
            PageSize = pageSize
        };
    }

    public async Task<PatientDetailDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var response = await _apiClient.Patients.GetPatientByIdAsync(id);
        return response.Data;
    }

    public async Task<PatientDetailDto> CreateAsync(PatientInputDto patient, CancellationToken ct = default)
    {
        var response = await _apiClient.Patients.CreatePatientAsync(patient);
        if (!response.Success || response.Data == null)
            throw new InvalidOperationException(response.Message ?? "Create patient failed");
        return response.Data;
    }

    public async Task<PatientDetailDto> UpdateAsync(PatientInputDto patient, CancellationToken ct = default)
    {
        var response = await _apiClient.Patients.UpdatePatientAsync(patient.Id!.Value, patient);
        if (!response.Success || response.Data == null)
            throw new InvalidOperationException(response.Message ?? "Update patient failed");
        return response.Data;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var response = await _apiClient.Patients.DeletePatientAsync(id);
        return response.Success;
    }

    public async Task<List<PatientListDto>> SearchAsync(string keyword, CancellationToken ct = default)
    {
        var response = await _apiClient.Patients.GetPatientsAsync(1, 100, keyword);
        if (response.Data == null)
            return [];
        return response.Data.Items.ToList();
    }

    public async Task<PatientDetailDto?> GetByIdNumberAsync(string idNumber, CancellationToken ct = default)
    {
        var response = await _apiClient.Patients.GetPatientsAsync(1, 100, idNumber);
        if (response.Data == null)
            return null;

        foreach (var candidate in response.Data.Items)
        {
            var detail = await GetByIdAsync(candidate.Id, ct);
            if (detail?.IdNumber?.Equals(idNumber, StringComparison.OrdinalIgnoreCase) == true)
                return detail;
        }
        return null;
    }

    public async Task<BatchOperationResultDto?> BatchDeleteAsync(List<Guid> ids, CancellationToken ct = default)
    {
        var response = await _apiClient.Patients.BatchDeleteAsync(new BatchDeleteInputDto { Ids = ids });
        return response.Data;
    }

    public async Task<PatientDetailDto?> RestoreAsync(Guid id, CancellationToken ct = default)
    {
        var response = await _apiClient.Patients.RestoreAsync(id);
        return response.Data;
    }

    public async Task<PatientBatchImportResultDto?> BatchImportAsync(PatientBatchImportInputDto request, CancellationToken ct = default)
    {
        var response = await _apiClient.Patients.BatchImportAsync(request);
        return response.Data;
    }

    public async Task<byte[]?> ExportTemplateAsync(CancellationToken ct = default)
    {
        var response = await _apiClient.Patients.ExportTemplateAsync();
        return response.IsSuccessStatusCode ? await response.Content.ReadAsByteArrayAsync() : null;
    }

    public async Task<byte[]?> ExportPatientsAsync(string? keyword = null, CancellationToken ct = default)
    {
        var response = await _apiClient.Patients.ExportPatientsAsync(keyword);
        return response.IsSuccessStatusCode ? await response.Content.ReadAsByteArrayAsync() : null;
    }
}
