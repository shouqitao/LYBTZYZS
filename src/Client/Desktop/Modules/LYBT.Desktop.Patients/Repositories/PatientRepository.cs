using System.Threading;
using LYBT.Desktop.Contracts.ApiClient;
using LYBT.Desktop.Contracts.Repositories;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Patients;
using Microsoft.Extensions.Logging;
using Riok.Mapperly.Abstractions;

namespace LYBT.Desktop.Patients.Repositories;

/// <summary>
/// Patient repository — routes all calls through IApiClient.
/// </summary>
public sealed class PatientRepository : IPatientRepository
{
    private readonly IApiClient _apiClient;
    private readonly ILogger<PatientRepository> _logger;
    private readonly PatientListToDetailMapper _listMapper = new();

    public PatientRepository(
        IApiClient apiClient,
        ILogger<PatientRepository> logger)
    {
        _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    #region Standard CRUD

    public async Task<PagedResult<PatientListDto>> GetPagedAsync(int page = 1, int pageSize = 20, string? keyword = null, CancellationToken ct = default)
    {
        try
        {
            _logger.LogDebug("[REPO] Patient.GetPaged - Page={Page} PageSize={PageSize} Keyword={Keyword}",
                page, pageSize, keyword);

            var response = await _apiClient.Patients.GetPatientsAsync(page, pageSize, keyword);
            if (response.Data == null)
                return new PagedResult<PatientListDto> { Items = [], TotalCount = 0, CurrentPage = page, PageSize = pageSize };

            return new PagedResult<PatientListDto>
            {
                Items = response.Data.Items.ToList(),
                TotalCount = response.Data.TotalCount,
                CurrentPage = page,
                PageSize = pageSize
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[REPO] Patient.GetPaged failed");
            throw;
        }
    }

    public async Task<PatientDetailDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            _logger.LogDebug("[REPO] Patient.GetById - Id={Id}", id);
            var response = await _apiClient.Patients.GetPatientByIdAsync(id);
            return response.Data;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[REPO] Patient.GetById failed - Id={Id}", id);
            throw;
        }
    }

    public async Task<PatientDetailDto> CreateAsync(PatientInputDto patient, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(patient);

        try
        {
            _logger.LogInformation("[REPO] Patient.Create");
            var response = await _apiClient.Patients.CreatePatientAsync(patient);
            if (!response.Success || response.Data == null)
                throw new InvalidOperationException(response.Message ?? "Create patient failed");
            return response.Data;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[REPO] Patient.Create failed");
            throw;
        }
    }

    public async Task<PatientDetailDto> UpdateAsync(PatientInputDto patient, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(patient);
        if (patient.Id is null || patient.Id == Guid.Empty)
            throw new ArgumentException("Update DTO must contain valid ID", nameof(patient));

        try
        {
            _logger.LogInformation("[REPO] Patient.Update - Id={Id}", patient.Id);
            var response = await _apiClient.Patients.UpdatePatientAsync(patient.Id.Value, patient);
            if (!response.Success || response.Data == null)
                throw new InvalidOperationException(response.Message ?? "Update patient failed");
            return response.Data;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[REPO] Patient.Update failed - Id={Id}", patient.Id);
            throw;
        }
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("[REPO] Patient.Delete - Id={Id}", id);
            var response = await _apiClient.Patients.DeletePatientAsync(id);
            return response.Success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[REPO] Patient.Delete failed - Id={Id}", id);
            return false;
        }
    }

    public async Task<List<PatientListDto>> SearchAsync(string keyword, CancellationToken ct = default)
    {
        try
        {
            _logger.LogDebug("[REPO] Patient.Search - Keyword={Keyword}", keyword);
            var response = await _apiClient.Patients.GetPatientsAsync(1, 100, keyword);
            if (response.Data == null)
                return [];
            return response.Data.Items.ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[REPO] Patient.Search failed");
            throw;
        }
    }

    #endregion

    #region IdNumber query

    public async Task<PatientDetailDto?> GetByIdNumberAsync(string idNumber, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(idNumber))
            return null;

        try
        {
            _logger.LogInformation("[REPO] Patient.GetByIdNumber");
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
        catch (Exception ex)
        {
            _logger.LogError(ex, "[REPO] Patient.GetByIdNumber failed");
            return null;
        }
    }

    #endregion

    #region Batch import/export

    public async Task<PatientBatchImportResultDto?> BatchImportAsync(PatientBatchImportInputDto request, CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("[REPO] Patient.BatchImport - Count={Count}", request.Patients.Count);
            var response = await _apiClient.Patients.BatchImportAsync(request);
            return response.Data;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[REPO] Patient.BatchImport failed");
            return null;
        }
    }

    public async Task<byte[]?> ExportTemplateAsync(CancellationToken ct = default)
    {
        try
        {
            var response = await _apiClient.Patients.ExportTemplateAsync();
            return response.IsSuccessStatusCode ? await response.Content.ReadAsByteArrayAsync() : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[REPO] Patient.ExportTemplate failed");
            return null;
        }
    }

    public async Task<byte[]?> ExportPatientsAsync(string? keyword = null, CancellationToken ct = default)
    {
        try
        {
            var response = await _apiClient.Patients.ExportPatientsAsync(keyword);
            return response.IsSuccessStatusCode ? await response.Content.ReadAsByteArrayAsync() : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[REPO] Patient.ExportPatients failed");
            return null;
        }
    }

    #endregion

    #region Restore and batch operations

    public async Task<PatientDetailDto?> RestoreAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("[REPO] Patient.Restore - Id={Id}", id);
            var response = await _apiClient.Patients.RestoreAsync(id);
            return response.Data;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[REPO] Patient.Restore failed - Id={Id}", id);
            return null;
        }
    }

    public async Task<BatchOperationResultDto?> BatchDeleteAsync(List<Guid> ids, CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("[REPO] Patient.BatchDelete - Count={Count}", ids.Count);
            var response = await _apiClient.Patients.BatchDeleteAsync(new BatchDeleteInputDto { Ids = ids });
            if (!response.Success || response.Data == null)
            {
                return new BatchOperationResultDto
                {
                    TotalCount = ids.Count,
                    FailureCount = ids.Count,
                    IsSuccess = false,
                    Message = response.Message ?? "Batch delete failed"
                };
            }
            return response.Data;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[REPO] Patient.BatchDelete failed");
            return new BatchOperationResultDto { TotalCount = ids.Count, FailureCount = ids.Count, IsSuccess = false, Message = ex.Message };
        }
    }

    #endregion

}

[Mapper]
internal partial class PatientListToDetailMapper
{
    [MapperIgnoreTarget(nameof(PatientDetailDto.BirthDate))]
    [MapperIgnoreTarget(nameof(PatientDetailDto.IdNumber))]
    [MapperIgnoreTarget(nameof(PatientDetailDto.MaritalStatus))]
    [MapperIgnoreTarget(nameof(PatientDetailDto.IdType))]
    [MapperIgnoreTarget(nameof(PatientDetailDto.BloodType))]
    [MapperIgnoreTarget(nameof(PatientDetailDto.AllergyHistory))]
    [MapperIgnoreTarget(nameof(PatientDetailDto.MedicalHistory))]
    [MapperIgnoreTarget(nameof(PatientDetailDto.EmergencyContactName))]
    [MapperIgnoreTarget(nameof(PatientDetailDto.EmergencyContactPhone))]
    [MapperIgnoreTarget(nameof(PatientDetailDto.EmergencyContactRelation))]
    [MapperIgnoreTarget(nameof(PatientDetailDto.DisableReason))]
    [MapperIgnoreTarget(nameof(PatientDetailDto.UpdatedAt))]
    [MapperIgnoreTarget(nameof(PatientDetailDto.CreatedBy))]
    public partial PatientDetailDto ToDetailDto(PatientListDto listDto);
}
