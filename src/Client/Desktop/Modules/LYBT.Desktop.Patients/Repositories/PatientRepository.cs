using System.Threading;
using LYBT.Desktop.Contracts.ApiClient;
using LYBT.Desktop.Contracts.Repositories;
using LYBT.Desktop.Foundation.Repositories;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Patients;
using Microsoft.Extensions.Logging;
using Riok.Mapperly.Abstractions;

namespace LYBT.Desktop.Patients.Repositories;

/// <summary>
/// Patient repository — routes all calls through IApiClient.
/// </summary>
public sealed class PatientRepository
    : ApiClientRepositoryBase<PatientListDto, PatientDetailDto, PatientInputDto, PatientInputDto>,
      IPatientRepository
{
    private readonly IApiClient _apiClient;

    protected override string LogPrefix => "Patient";

    public PatientRepository(
        IApiClient apiClient,
        ILogger<PatientRepository> logger)
        : base(logger)
    {
        _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
    }

    #region Standard CRUD

    public async Task<PagedResult<PatientListDto>> GetPagedAsync(int page = 1, int pageSize = 20, string? keyword = null, CancellationToken ct = default)
    {
        return await ExecuteAsync(async () =>
        {
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
        }, nameof(GetPagedAsync), "[REPO] {0}.{1} - Page={2} PageSize={3} Keyword={4}",
           [LogPrefix, nameof(GetPagedAsync), page, pageSize, keyword]);
    }

    public async Task<PatientDetailDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await ExecuteAsync(async () =>
        {
            var response = await _apiClient.Patients.GetPatientByIdAsync(id);
            return response.Data;
        }, nameof(GetByIdAsync), "[REPO] {0}.{1} - Id={2}",
           [LogPrefix, nameof(GetByIdAsync), id]);
    }

    public async Task<PatientDetailDto> CreateAsync(PatientInputDto patient, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(patient);

        return await ExecuteAsync(async () =>
        {
            var response = await _apiClient.Patients.CreatePatientAsync(patient);
            if (!response.Success || response.Data == null)
                throw new InvalidOperationException(response.Message ?? "创建患者失败");
            return response.Data;
        }, nameof(CreateAsync), LogLevel.Information);
    }

    public async Task<PatientDetailDto> UpdateAsync(PatientInputDto patient, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(patient);
        if (patient.Id is null || patient.Id == Guid.Empty)
            throw new ArgumentException("Update DTO must contain valid ID", nameof(patient));

        return await ExecuteAsync(async () =>
        {
            var response = await _apiClient.Patients.UpdatePatientAsync(patient.Id.Value, patient);
            if (!response.Success || response.Data == null)
                throw new InvalidOperationException(response.Message ?? "更新患者失败");
            return response.Data;
        }, nameof(UpdateAsync), LogLevel.Information);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        await ExecuteAsync(
            async () =>
            {
                var response = await _apiClient.Patients.DeletePatientAsync(id);
                if (!response.Success)
                    throw new InvalidOperationException(response.Message ?? "删除患者失败");

                Logger.LogInformation("[REPO] Patient.Delete completed - Id={Id}", id);
            },
            nameof(DeleteAsync),
            LogLevel.Information);
    }

    public async Task<List<PatientListDto>> SearchAsync(string keyword, CancellationToken ct = default)
    {
        return await ExecuteAsync(async () =>
        {
            var response = await _apiClient.Patients.GetPatientsAsync(1, 100, keyword);
            if (response.Data == null)
                return [];
            return response.Data.Items.ToList();
        }, nameof(SearchAsync));
    }

    #endregion

    #region IdNumber query

    public async Task<PatientDetailDto?> GetByIdNumberAsync(string idNumber, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(idNumber))
            return null;

        try
        {
            Logger.LogInformation("[REPO] Patient.GetByIdNumber");
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
            Logger.LogError(ex, "[REPO] Patient.GetByIdNumber failed");
            return null;
        }
    }

    #endregion

    #region Batch import/export

    public async Task<PatientBatchImportResultDto?> BatchImportAsync(PatientBatchImportInputDto request, CancellationToken ct = default)
    {
        try
        {
            Logger.LogInformation("[REPO] Patient.BatchImport - Count={Count}", request.Patients.Count);
            var response = await _apiClient.Patients.BatchImportAsync(request);
            return response.Data;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "[REPO] Patient.BatchImport failed");
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
            Logger.LogError(ex, "[REPO] Patient.ExportTemplate failed");
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
            Logger.LogError(ex, "[REPO] Patient.ExportPatients failed");
            return null;
        }
    }

    #endregion

    #region Batch operations

    public async Task<BatchOperationResultDto?> BatchDeleteAsync(List<Guid> ids, CancellationToken ct = default)
    {
        try
        {
            Logger.LogInformation("[REPO] Patient.BatchDelete - Count={Count}", ids.Count);
            var response = await _apiClient.Patients.BatchDeleteAsync(new BatchDeleteInputDto { Ids = ids });
            if (!response.Success || response.Data == null)
            {
                return new BatchOperationResultDto
                {
                    TotalCount = ids.Count,
                    FailureCount = ids.Count,
                    IsSuccess = false,
                    Message = response.Message ?? "批量删除患者失败"
                };
            }
            return response.Data;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "[REPO] Patient.BatchDelete failed");
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
    [MapperIgnoreTarget(nameof(PatientDetailDto.UpdatedAt))]
    [MapperIgnoreTarget(nameof(PatientDetailDto.CreatedBy))]
    public partial PatientDetailDto ToDetailDto(PatientListDto listDto);
}
