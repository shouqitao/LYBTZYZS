using System.Threading;
using LYBT.Desktop.Contracts.ApiClient;
using LYBT.Desktop.Contracts.Repositories;
using LYBT.Desktop.Foundation.Repositories;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Patients;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Patients.Repositories;

/// <summary>
/// 患者仓储——所有调用均通过 IApiClient 路由。
/// </summary>
public sealed class PatientRepository
    : EntityApiClientRepositoryBase<PatientListDto, PatientDetailDto, PatientInputDto>,
      IPatientRepository
{
    private readonly IApiClientPatients _patients;

    protected override string LogPrefix => "Patient";

    public PatientRepository(
        IApiClientPatients patients,
        ILogger<PatientRepository> logger)
        : base(logger, patients)
    {
        _patients = patients ?? throw new ArgumentNullException(nameof(patients));
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
            var response = await _patients.GetPatientsAsync(1, 100, keyword);
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
            var response = await _patients.GetPatientsAsync(1, 100, idNumber);
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
        ArgumentNullException.ThrowIfNull(request);
        return await ExecuteImportAsync(
            () => _patients.BatchImportAsync(request),
            "BatchImport",
            request.Patients.Count,
            ct);
    }

    public async Task<byte[]?> ExportTemplateAsync(CancellationToken ct = default)
    {
        try
        {
            var response = await _patients.ExportTemplateAsync();
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
            var response = await _patients.ExportPatientsAsync(keyword);
            return response.IsSuccessStatusCode ? await response.Content.ReadAsByteArrayAsync() : null;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "[REPO] Patient.ExportPatients failed");
            return null;
        }
    }

    #endregion

    #region Restore

    public async Task<PatientDetailDto?> RestoreAsync(Guid id, CancellationToken ct = default)
    {
        return await ExecuteAsync(
            async () =>
            {
                var response = await _patients.RestoreAsync(id);
                if (!response.Success || response.Data == null)
                    throw new InvalidOperationException(response.Message ?? "恢复患者失败");

                Logger.LogInformation("[REPO] Patient.Restore completed - Id={Id}", id);
                return response.Data;
            },
            "Restore",
            LogLevel.Information);
    }

    #endregion

    #region Batch operations

    public async Task<BatchOperationResultDto?> BatchDeleteAsync(List<Guid> ids, CancellationToken ct = default)
    {
        return await ExecuteBatchDeleteAsync(
            () => _patients.BatchDeleteAsync(new BatchDeleteInputDto { Ids = ids }),
            "BatchDelete",
            "批量删除患者失败",
            ids.Count);
    }

    #endregion

}
