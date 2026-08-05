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
/// 患者仓储——所有调用均通过 IApiClient 路由。
/// </summary>
public sealed class PatientRepository
    : EntityApiClientRepositoryBase<PatientListDto, PatientDetailDto, PatientInputDto>,
      IPatientRepository
{
    private readonly IApiClient _apiClient;

    protected override string LogPrefix => "Patient";

    public PatientRepository(
        IApiClient apiClient,
        ILogger<PatientRepository> logger)
        : base(logger, apiClient.Patients)
    {
        _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
    }

    /// <summary>
    /// 分页查询患者列表（接口无 category 参数，转发基类标准实现）。
    /// </summary>
    public Task<PagedResult<PatientListDto>> GetPagedAsync(int page = 1, int pageSize = 20, string? keyword = null, CancellationToken ct = default)
        => base.GetPagedAsync(page, pageSize, keyword, null, ct);

    #region Search

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
        return await ExecuteBatchDeleteAsync(
            () => _apiClient.Patients.BatchDeleteAsync(new BatchDeleteInputDto { Ids = ids }),
            "BatchDelete",
            "批量删除患者失败",
            ids.Count);
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
