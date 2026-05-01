using System.Threading;
using LYBT.Desktop.Contracts.Api;
using LYBT.Desktop.Contracts.Repositories;
using LYBT.Desktop.Contracts.Services;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Patients;
using Microsoft.Extensions.Logging;
using Riok.Mapperly.Abstractions;

namespace LYBT.Desktop.Patients.Repositories;

/// <summary>
/// Patient repository with dual-path support.
/// Routes to remote IPatientApi or local ILocalPatientApi based on IApiRouter state.
/// </summary>
public sealed class PatientRepository : IPatientRepository
{
    private readonly IPatientApi _api;
    private readonly ILocalPatientApi _localApi;
    private readonly IApiRouter _apiRouter;
    private readonly ILogger<PatientRepository> _logger;
    private readonly PatientListToDetailMapper _listMapper = new();

    public PatientRepository(
        IPatientApi api,
        ILocalPatientApi localApi,
        IApiRouter apiRouter,
        ILogger<PatientRepository> logger)
    {
        _api = api ?? throw new ArgumentNullException(nameof(api));
        _localApi = localApi ?? throw new ArgumentNullException(nameof(localApi));
        _apiRouter = apiRouter ?? throw new ArgumentNullException(nameof(apiRouter));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    private bool IsOffline => _apiRouter.IsOffline;

    #region Standard CRUD

    public async Task<PagedResult<PatientListDto>> GetPagedAsync(int page = 1, int pageSize = 20, string? keyword = null, CancellationToken ct = default)
    {
        try
        {
            if (IsOffline)
            {
                _logger.LogDebug("[REPO:Local] Patient.GetPaged - Page={Page} PageSize={PageSize}", page, pageSize);
                var patients = await _localApi.GetPatientsAsync(keyword, page, pageSize);
                var listDtos = patients.Select(p => new PatientListDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    PinYinCode = p.PinYinCode,
                    Gender = p.Gender,
                    PhoneNumber = p.PhoneNumber,
                    Status = p.Status,
                    CreatedAt = p.CreatedAt,
                    UpdatedAt = p.UpdatedAt
                }).ToList();
                return new PagedResult<PatientListDto>
                {
                    Items = listDtos,
                    TotalCount = listDtos.Count,
                    CurrentPage = page,
                    PageSize = pageSize
                };
            }

            _logger.LogDebug("[REPO:Remote] Patient.GetPaged - Page={Page} PageSize={PageSize} Keyword={Keyword}",
                page, pageSize, keyword);

            var response = await _api.GetPatientsAsync(page, pageSize, keyword);
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
            _logger.LogError(ex, "[REPO:{Mode}] Patient.GetPaged failed", IsOffline ? "Local" : "Remote");
            throw;
        }
    }

    public async Task<PatientDetailDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            if (IsOffline)
            {
                _logger.LogDebug("[REPO:Local] Patient.GetById - Id={Id}", id);
                var p = await _localApi.GetPatientByIdAsync(id);
                return MapToDetailDto(p);
            }

            _logger.LogDebug("[REPO:Remote] Patient.GetById - Id={Id}", id);
            var response = await _api.GetPatientByIdAsync(id);
            return response.Data;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[REPO:{Mode}] Patient.GetById failed - Id={Id}", IsOffline ? "Local" : "Remote", id);
            throw;
        }
    }

    public async Task<PatientDetailDto> CreateAsync(PatientInputDto patient, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(patient);

        try
        {
            if (IsOffline)
            {
                _logger.LogInformation("[REPO:Local] Patient.Create");
                var created = await _localApi.CreatePatientAsync(patient);
                return MapToDetailDto(created);
            }

            _logger.LogInformation("[REPO:Remote] Patient.Create");
            var response = await _api.CreatePatientAsync(patient);
            if (!response.Success || response.Data == null)
                throw new InvalidOperationException(response.Message ?? "Create patient failed");
            return response.Data;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[REPO:{Mode}] Patient.Create failed", IsOffline ? "Local" : "Remote");
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
            if (IsOffline)
            {
                _logger.LogInformation("[REPO:Local] Patient.Update - Id={Id}", patient.Id);
                var updated = await _localApi.UpdatePatientAsync(patient.Id.Value, patient);
                return MapToDetailDto(updated);
            }

            _logger.LogInformation("[REPO:Remote] Patient.Update - Id={Id}", patient.Id);
            var response = await _api.UpdatePatientAsync(patient.Id.Value, patient);
            if (!response.Success || response.Data == null)
                throw new InvalidOperationException(response.Message ?? "Update patient failed");
            return response.Data;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[REPO:{Mode}] Patient.Update failed - Id={Id}", IsOffline ? "Local" : "Remote", patient.Id);
            throw;
        }
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            if (IsOffline)
            {
                _logger.LogInformation("[REPO:Local] Patient.Delete - Id={Id}", id);
                await _localApi.DeletePatientAsync(id);
                return true;
            }

            _logger.LogInformation("[REPO:Remote] Patient.Delete - Id={Id}", id);
            var response = await _api.DeletePatientAsync(id);
            return response.Success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[REPO:{Mode}] Patient.Delete failed - Id={Id}", IsOffline ? "Local" : "Remote", id);
            return false;
        }
    }

    public async Task<List<PatientListDto>> SearchAsync(string keyword, CancellationToken ct = default)
    {
        try
        {
            if (IsOffline)
            {
                _logger.LogDebug("[REPO:Local] Patient.Search - Keyword={Keyword}", keyword);
                var patients = await _localApi.GetPatientsAsync(keyword, 1, 100);
                return patients.Select(p => new PatientListDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    PinYinCode = p.PinYinCode,
                    Gender = p.Gender,
                    PhoneNumber = p.PhoneNumber,
                    Status = p.Status,
                    CreatedAt = p.CreatedAt,
                    UpdatedAt = p.UpdatedAt
                }).ToList();
            }

            _logger.LogDebug("[REPO:Remote] Patient.Search - Keyword={Keyword}", keyword);
            var response = await _api.GetPatientsAsync(1, 100, keyword);
            if (response.Data == null)
                return [];
            return response.Data.Items.ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[REPO:{Mode}] Patient.Search failed", IsOffline ? "Local" : "Remote");
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
            if (IsOffline)
            {
                _logger.LogInformation("[REPO:Local] Patient.GetByIdNumber");
                var patients = await _localApi.GetPatientsAsync(idNumber, 1, 100);
                var match = patients.FirstOrDefault(p =>
                    p.IdNumber?.Equals(idNumber, StringComparison.OrdinalIgnoreCase) == true);
                return match != null ? MapToDetailDto(match) : null;
            }

            _logger.LogInformation("[REPO:Remote] Patient.GetByIdNumber");
            var response = await _api.GetPatientsAsync(1, 100, idNumber);
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
            _logger.LogError(ex, "[REPO:{Mode}] Patient.GetByIdNumber failed", IsOffline ? "Local" : "Remote");
            return null;
        }
    }

    #endregion

    #region Batch import/export (remote only)

    public async Task<PatientBatchImportResultDto?> BatchImportAsync(PatientBatchImportInputDto request, CancellationToken ct = default)
    {
        if (IsOffline)
        {
            _logger.LogWarning("[REPO:Local] Patient.BatchImport not supported in offline mode");
            return null;
        }

        try
        {
            var response = await _api.BatchImportAsync(request);
            return response.Data;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[REPO:Remote] Patient.BatchImport failed");
            return null;
        }
    }

    public async Task<byte[]?> ExportTemplateAsync(CancellationToken ct = default)
    {
        if (IsOffline)
        {
            _logger.LogWarning("[REPO:Local] Patient.ExportTemplate not supported in offline mode");
            return null;
        }

        try
        {
            var response = await _api.ExportTemplateAsync();
            return response.IsSuccessStatusCode ? await response.Content.ReadAsByteArrayAsync() : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[REPO:Remote] Patient.ExportTemplate failed");
            return null;
        }
    }

    public async Task<byte[]?> ExportPatientsAsync(string? keyword = null, CancellationToken ct = default)
    {
        if (IsOffline)
        {
            _logger.LogWarning("[REPO:Local] Patient.ExportPatients not supported in offline mode");
            return null;
        }

        try
        {
            var response = await _api.ExportPatientsAsync(keyword);
            return response.IsSuccessStatusCode ? await response.Content.ReadAsByteArrayAsync() : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[REPO:Remote] Patient.ExportPatients failed");
            return null;
        }
    }

    #endregion

    #region Restore and batch operations (remote only)

    public async Task<PatientDetailDto?> RestoreAsync(Guid id, CancellationToken ct = default)
    {
        if (IsOffline)
        {
            _logger.LogWarning("[REPO:Local] Patient.Restore not supported in offline mode");
            return null;
        }

        try
        {
            var response = await _api.RestoreAsync(id);
            return response.Data;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[REPO:Remote] Patient.Restore failed - Id={Id}", id);
            return null;
        }
    }

    public async Task<BatchOperationResultDto?> BatchDeleteAsync(List<Guid> ids, CancellationToken ct = default)
    {
        if (IsOffline)
        {
            _logger.LogWarning("[REPO:Local] Patient.BatchDelete not supported in offline mode");
            return new BatchOperationResultDto
            {
                TotalCount = ids.Count,
                FailureCount = ids.Count,
                IsSuccess = false,
                Message = "Batch delete not supported in offline mode"
            };
        }

        try
        {
            var response = await _api.BatchDeleteAsync(new BatchDeleteInputDto { Ids = ids });
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
            _logger.LogError(ex, "[REPO:Remote] Patient.BatchDelete failed");
            return new BatchOperationResultDto { TotalCount = ids.Count, FailureCount = ids.Count, IsSuccess = false, Message = ex.Message };
        }
    }

    #endregion

    #region Mapping helpers

    private static PatientDetailDto MapToDetailDto(Entities.Patients.Patient p)
    {
        return new PatientDetailDto
        {
            Id = p.Id,
            Name = p.Name,
            PinYinCode = p.PinYinCode,
            Gender = p.Gender,
            BirthDate = p.BirthDate,
            IdNumber = p.IdNumber,
            PhoneNumber = p.PhoneNumber,
            Address = p.Address,
            AllergyHistory = p.AllergyHistory,
            MedicalHistory = p.MedicalHistory,
            Status = p.Status,
            DisableReason = p.DisableReason,
            CreatedAt = p.CreatedAt,
            UpdatedAt = p.UpdatedAt
        };
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
