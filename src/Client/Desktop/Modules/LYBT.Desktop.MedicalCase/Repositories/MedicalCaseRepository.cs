using LYBT.Desktop.Contracts.ApiClient;
using LYBT.Desktop.Contracts.Repositories;
using LYBT.Desktop.Foundation.Repositories;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Shared.Models.Contracts.MedicalCase;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.MedicalCase.Repositories;

/// <summary>
/// 医案仓储 — routes all calls through IApiClient.
/// </summary>
public sealed class MedicalCaseRepository : ApiClientRepositoryBase<MedicalCaseListDto, MedicalCaseDetailDto, MedicalCaseInputDto, MedicalCaseInputDto>, IMedicalCaseRepository
{
    private readonly IApiClient _apiClient;

    public MedicalCaseRepository(
        IApiClient apiClient,
        ILogger<MedicalCaseRepository> logger)
        : base(logger)
    {
        _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
    }

    protected override string LogPrefix => "MedicalCase";

    #region 标准 CRUD 操作

    public async Task<PagedResult<MedicalCaseListDto>> GetPagedAsync(int page = 1, int pageSize = 20, string? keyword = null, CancellationToken ct = default)
    {
        return await ExecuteAsync(
            async () =>
            {
                var response = await _apiClient.MedicalCases.GetMedicalCasesAsync(page, pageSize, keyword);
                if (response.Data == null)
                    return new PagedResult<MedicalCaseListDto> { Items = [], TotalCount = 0, CurrentPage = page, PageSize = pageSize };

                return new PagedResult<MedicalCaseListDto>
                {
                    Items = response.Data.Items.ToList(),
                    TotalCount = response.Data.TotalCount,
                    CurrentPage = page,
                    PageSize = pageSize
                };
            },
            "GetPaged",
            "[REPO] MedicalCase.GetPaged - Page={Page} PageSize={PageSize} Keyword={Keyword}",
            [page, pageSize, keyword]);
    }

    public async Task<MedicalCaseDetailDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await ExecuteAsync(
            async () =>
            {
                var response = await _apiClient.MedicalCases.GetMedicalCaseByIdAsync(id);
                return response.Data;
            },
            "GetById");
    }

    public async Task<MedicalCaseDetailDto> CreateAsync(MedicalCaseInputDto dto, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(dto);

        return await ExecuteAsync(
            async () =>
            {
                var response = await _apiClient.MedicalCases.CreateMedicalCaseAsync(dto);
                if (!response.Success || response.Data == null)
                    throw new InvalidOperationException(response.Message ?? "创建医案失败");

                Logger.LogInformation("[REPO] MedicalCase.Create completed - Id={Id}", response.Data.Id);
                return response.Data;
            },
            "Create",
            LogLevel.Information);
    }

    public async Task<MedicalCaseDetailDto> UpdateAsync(MedicalCaseInputDto dto, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(dto);
        if (dto.Id is null || dto.Id == Guid.Empty)
            throw new ArgumentException("更新DTO必须包含有效的ID", nameof(dto));

        return await ExecuteAsync(
            async () =>
            {
                var response = await _apiClient.MedicalCases.SaveAsync(dto.Id.Value, dto);
                if (!response.Success || response.Data == null)
                    throw new InvalidOperationException(response.Message ?? "更新医案失败");

                Logger.LogInformation("[REPO] MedicalCase.Update completed - Id={Id}", response.Data.Id);
                return response.Data;
            },
            "Update",
            LogLevel.Information);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        // DeleteAsync returns false on exception instead of rethrowing,
        // which differs from ExecuteAsync's always-rethrow behavior.
        try
        {
            Logger.LogInformation("[REPO] MedicalCase.Delete - Id={Id}", id);

            var response = await _apiClient.MedicalCases.DeleteMedicalCaseAsync(id);
            if (response.Success)
                Logger.LogInformation("[REPO] MedicalCase.Delete completed - Id={Id}", id);
            else
                Logger.LogWarning("[REPO] MedicalCase.Delete failed - Id={Id}", id);

            return response.Success;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "[REPO] MedicalCase.Delete failed - Id={Id}", id);
            return false;
        }
    }

    #endregion

    #region 高级查询

    public async Task<PagedResult<MedicalCaseDetailDto>> SearchAsync(
        string? patientName = null,
        string? diagnosisKeyword = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        int page = 1,
        int pageSize = 20,
        CancellationToken ct = default)
    {
        return await ExecuteAsync(
            async () =>
            {
                var response = await _apiClient.MedicalCases.SearchMedicalCasesAsync(
                    patientName, diagnosisKeyword, startDate, endDate, page, pageSize);
                return response.Data ?? new PagedResult<MedicalCaseDetailDto>
                {
                    Items = [],
                    TotalCount = 0,
                    CurrentPage = page,
                    PageSize = pageSize
                };
            },
            "Search",
            "[REPO] MedicalCase.Search - PatientName={PatientName} DiagnosisKeyword={DiagnosisKeyword}",
            [patientName ?? "无", diagnosisKeyword ?? "无"]);
    }

    public async Task<PagedResult<MedicalCaseListDto>> QueryAsync(MedicalCaseQueryDto query, CancellationToken ct = default)
    {
        return await ExecuteAsync(
            async () =>
            {
                var response = await _apiClient.MedicalCases.QueryMedicalCasesAsync(
                    queryType: query.QueryType,
                    patientId: query.PatientId,
                    doctorId: query.DoctorId,
                    keyword: query.Keyword,
                    pageIndex: query.PageIndex,
                    pageSize: query.PageSize,
                    includeAllDoctors: query.IncludeAllDoctors,
                    limit: query.Limit);
                return response.Data ?? new PagedResult<MedicalCaseListDto>();
            },
            "Query",
            "[REPO] MedicalCase.Query - QueryType={QueryType}",
            [query.QueryType]);
    }

    #endregion

    #region 生命周期操作

    public async Task<MedicalCaseDetailDto?> CloseCaseAsync(Guid medicalCaseId, CancellationToken ct = default)
    {
        if (medicalCaseId == Guid.Empty)
            throw new ArgumentException("医案ID不能为空", nameof(medicalCaseId));

        // Returns null on failure instead of rethrowing — keep manual try/catch.
        try
        {
            Logger.LogInformation("[REPO] MedicalCase.CloseCase - Id={Id}", medicalCaseId);

            var response = await _apiClient.MedicalCases.CloseCaseAsync(medicalCaseId);
            if (response.Success)
            {
                Logger.LogInformation("[REPO] MedicalCase.CloseCase completed - Id={Id}", medicalCaseId);
                return response.Data;
            }

            Logger.LogWarning("[REPO] MedicalCase.CloseCase failed - Id={Id}, Message={Message}",
                medicalCaseId, response.Message);
            return null;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "[REPO] MedicalCase.CloseCase failed - Id={Id}", medicalCaseId);
            throw;
        }
    }

    public async Task<MedicalCaseDetailDto?> CancelMedicalCaseAsync(Guid id, CancelMedicalCaseRequestDto? request, CancellationToken ct = default)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("医案ID不能为空", nameof(id));

        // Always returns null regardless of outcome — keep manual try/catch.
        try
        {
            Logger.LogInformation("[REPO] MedicalCase.Cancel - Id={Id}, Reason={Reason}",
                id, request?.Reason ?? "无");

            var response = await _apiClient.MedicalCases.CancelMedicalCaseAsync(id, request);
            if (response.Success)
                Logger.LogInformation("[REPO] MedicalCase.Cancel completed - Id={Id}", id);
            else
                Logger.LogWarning("[REPO] MedicalCase.Cancel failed - Id={Id}, Message={Message}",
                    id, response.Message);

            return null;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "[REPO] MedicalCase.Cancel failed - Id={Id}", id);
            throw;
        }
    }

    public async Task<MedicalCaseDetailDto?> SuspendAsync(Guid id, ConsultationInputDto? request, CancellationToken ct = default)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("医案ID不能为空", nameof(id));

        // Returns null on failure instead of rethrowing — keep manual try/catch.
        try
        {
            Logger.LogInformation("[REPO] MedicalCase.Suspend - Id={Id}", id);

            var response = await _apiClient.MedicalCases.SuspendAsync(id, request);
            if (response.Success)
            {
                Logger.LogInformation("[REPO] MedicalCase.Suspend completed - Id={Id}", id);
                return response.Data;
            }

            Logger.LogWarning("[REPO] MedicalCase.Suspend failed - Id={Id}, Message={Message}",
                id, response.Message);
            return null;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "[REPO] MedicalCase.Suspend failed - Id={Id}", id);
            throw;
        }
    }

    public async Task<MedicalCaseDetailDto?> UpdateStatusAsync(Guid id, MedicalCaseStatusInputDto request, CancellationToken ct = default)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("医案ID不能为空", nameof(id));
        ArgumentNullException.ThrowIfNull(request);

        // Returns null on failure instead of rethrowing — keep manual try/catch.
        try
        {
            Logger.LogInformation("[REPO] MedicalCase.UpdateStatus - Id={Id}, Status={Status}",
                id, request.Status);

            var response = await _apiClient.MedicalCases.UpdateStatusAsync(id, request);
            if (response.Success)
            {
                Logger.LogInformation("[REPO] MedicalCase.UpdateStatus completed - Id={Id}", id);
                return response.Data;
            }

            Logger.LogWarning("[REPO] MedicalCase.UpdateStatus failed - Id={Id}, Message={Message}",
                id, response.Message);
            return null;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "[REPO] MedicalCase.UpdateStatus failed - Id={Id}", id);
            throw;
        }
    }

    #endregion

    #region 聚合保存

    public async Task<MedicalCaseDetailDto> SaveAsync(Guid medicalCaseId, MedicalCaseInputDto dto, CancellationToken ct = default)
    {
        if (medicalCaseId == Guid.Empty)
            throw new ArgumentException("医案ID不能为空", nameof(medicalCaseId));
        ArgumentNullException.ThrowIfNull(dto);

        return await ExecuteAsync(
            async () =>
            {
                var response = await _apiClient.MedicalCases.SaveAsync(medicalCaseId, dto);
                if (!response.Success || response.Data == null)
                    throw new InvalidOperationException(response.Message ?? "聚合保存医案失败");

                Logger.LogInformation("[REPO] MedicalCase.Save completed - Id={Id}", medicalCaseId);
                return response.Data;
            },
            "Save",
            LogLevel.Information);
    }

    #endregion

    #region 处方标志

    public async Task<MedicalCaseDetailDto?> SetPrescriptionFlagAsync(Guid id, SetPrescriptionFlagRequest request, CancellationToken ct = default)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("医案ID不能为空", nameof(id));
        ArgumentNullException.ThrowIfNull(request);

        // Returns null on failure instead of rethrowing — keep manual try/catch.
        try
        {
            Logger.LogInformation("[REPO] MedicalCase.SetPrescriptionFlag - Id={Id}, NeedsPrescription={NeedsPrescription}",
                id, request.NeedsPrescription);

            var response = await _apiClient.MedicalCases.SetPrescriptionFlagAsync(id, request);
            if (response.Success)
            {
                Logger.LogInformation("[REPO] MedicalCase.SetPrescriptionFlag completed - Id={Id}", id);
                return response.Data;
            }

            Logger.LogWarning("[REPO] MedicalCase.SetPrescriptionFlag failed - Id={Id}, Message={Message}",
                id, response.Message);
            return null;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "[REPO] MedicalCase.SetPrescriptionFlag failed - Id={Id}", id);
            throw;
        }
    }

    #endregion

    #region 批量操作

    public async Task<BatchOperationResultDto?> BatchDeleteAsync(List<Guid> ids, CancellationToken ct = default)
    {
        // Returns failure DTO on exception instead of rethrowing — keep manual try/catch.
        try
        {
            Logger.LogInformation("[REPO] MedicalCase.BatchDelete - Count={Count}", ids.Count);

            var response = await _apiClient.MedicalCases.BatchDeleteAsync(new BatchDeleteInputDto { Ids = ids });
            if (!response.Success || response.Data == null)
            {
                return new BatchOperationResultDto
                {
                    TotalCount = ids.Count,
                    FailureCount = ids.Count,
                    IsSuccess = false,
                    Message = response.Message ?? "批量删除失败"
                };
            }

            return response.Data;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "[REPO] MedicalCase.BatchDelete failed");
            return new BatchOperationResultDto
            {
                TotalCount = ids.Count,
                FailureCount = ids.Count,
                IsSuccess = false,
                Message = ex.Message
            };
        }
    }

    #endregion
}
