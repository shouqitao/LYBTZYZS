using LYBT.Desktop.Contracts.Api;
using LYBT.Desktop.Contracts.Repositories;
using LYBT.Desktop.Contracts.Services;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Shared.Models.Contracts.MedicalCase;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.MedicalCase.Repositories;

/// <summary>
/// 医案仓储 - 通过 Refit IMedicalCaseApi / ILocalMedicalCaseApi 双路径访问。
/// </summary>
public sealed class MedicalCaseRepository : IMedicalCaseRepository
{
    private readonly IMedicalCaseApi _api;
    private readonly ILocalMedicalCaseApi _localApi;
    private readonly IApiRouter _apiRouter;
    private readonly ILogger<MedicalCaseRepository> _logger;

    private bool IsOffline => _apiRouter.IsOffline;

    public MedicalCaseRepository(
        IMedicalCaseApi api,
        ILocalMedicalCaseApi localApi,
        IApiRouter apiRouter,
        ILogger<MedicalCaseRepository> logger)
    {
        _api = api ?? throw new ArgumentNullException(nameof(api));
        _localApi = localApi ?? throw new ArgumentNullException(nameof(localApi));
        _apiRouter = apiRouter ?? throw new ArgumentNullException(nameof(apiRouter));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    #region 标准 CRUD 操作

    public async Task<PagedResult<MedicalCaseListDto>> GetPagedAsync(int page = 1, int pageSize = 20, string? keyword = null)
    {
        try
        {
            if (IsOffline)
            {
                _logger.LogDebug("[REPO:Local] MedicalCase.GetPaged - Page={Page} PageSize={PageSize} Keyword={Keyword}",
                    page, pageSize, keyword);
                var items = await _localApi.GetMedicalCasesAsync(patientId: null);
                return new PagedResult<MedicalCaseListDto>
                {
                    Items = items,
                    TotalCount = items.Count,
                    CurrentPage = page,
                    PageSize = pageSize
                };
            }

            _logger.LogDebug("[REPO:Remote] MedicalCase.GetPaged - Page={Page} PageSize={PageSize} Keyword={Keyword}",
                page, pageSize, keyword);

            var response = await _api.GetMedicalCasesAsync(page, pageSize, keyword);
            if (response.Data == null)
                return new PagedResult<MedicalCaseListDto> { Items = [], TotalCount = 0, CurrentPage = page, PageSize = pageSize };

            return new PagedResult<MedicalCaseListDto>
            {
                Items = response.Data.Items.ToList(),
                TotalCount = response.Data.TotalCount,
                CurrentPage = page,
                PageSize = pageSize
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[REPO:{Mode}] MedicalCase.GetPaged failed", IsOffline ? "Local" : "Remote");
            throw;
        }
    }

    public async Task<MedicalCaseDetailDto?> GetByIdAsync(Guid id)
    {
        try
        {
            if (IsOffline)
            {
                _logger.LogDebug("[REPO:Local] MedicalCase.GetById - Id={Id}", id);
                return await _localApi.GetMedicalCaseByIdAsync(id);
            }

            _logger.LogDebug("[REPO:Remote] MedicalCase.GetById - Id={Id}", id);
            var response = await _api.GetMedicalCaseByIdAsync(id);
            return response.Data;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[REPO:{Mode}] MedicalCase.GetById failed - Id={Id}", IsOffline ? "Local" : "Remote", id);
            throw;
        }
    }

    public async Task<MedicalCaseDetailDto> CreateAsync(MedicalCaseInputDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        try
        {
            if (IsOffline)
            {
                _logger.LogInformation("[REPO:Local] MedicalCase.Create - PatientId={PatientId}", dto.PatientId);
                return await _localApi.CreateMedicalCaseAsync(dto);
            }

            _logger.LogInformation("[REPO:Remote] MedicalCase.Create - PatientId={PatientId}", dto.PatientId);

            var response = await _api.CreateMedicalCaseAsync(dto);
            if (!response.Success || response.Data == null)
                throw new InvalidOperationException(response.Message ?? "创建医案失败");

            _logger.LogInformation("[REPO:Remote] MedicalCase.Create completed - Id={Id}", response.Data.Id);
            return response.Data;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[REPO:{Mode}] MedicalCase.Create failed", IsOffline ? "Local" : "Remote");
            throw;
        }
    }

    public async Task<MedicalCaseDetailDto> UpdateAsync(MedicalCaseInputDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);
        if (dto.Id is null || dto.Id == Guid.Empty)
            throw new ArgumentException("更新DTO必须包含有效的ID", nameof(dto));

        try
        {
            if (IsOffline)
            {
                _logger.LogInformation("[REPO:Local] MedicalCase.Update - Id={Id}", dto.Id);
                return await _localApi.SaveAsync(dto.Id.Value, dto);
            }

            _logger.LogInformation("[REPO:Remote] MedicalCase.Update - Id={Id}", dto.Id);

            var response = await _api.SaveAsync(dto.Id.Value, dto);
            if (!response.Success || response.Data == null)
                throw new InvalidOperationException(response.Message ?? "更新医案失败");

            _logger.LogInformation("[REPO:Remote] MedicalCase.Update completed - Id={Id}", response.Data.Id);
            return response.Data;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[REPO:{Mode}] MedicalCase.Update failed - Id={Id}", IsOffline ? "Local" : "Remote", dto.Id);
            throw;
        }
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        try
        {
            if (IsOffline)
            {
                _logger.LogInformation("[REPO:Local] MedicalCase.Delete - Id={Id}", id);
                await _localApi.DeleteMedicalCaseAsync(id);
                return true;
            }

            _logger.LogInformation("[REPO:Remote] MedicalCase.Delete - Id={Id}", id);

            var response = await _api.DeleteMedicalCaseAsync(id);
            if (response.Success)
                _logger.LogInformation("[REPO:Remote] MedicalCase.Delete completed - Id={Id}", id);
            else
                _logger.LogWarning("[REPO:Remote] MedicalCase.Delete failed - Id={Id}", id);

            return response.Success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[REPO:{Mode}] MedicalCase.Delete failed - Id={Id}", IsOffline ? "Local" : "Remote", id);
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
        int pageSize = 20)
    {
        try
        {
            if (IsOffline)
            {
                _logger.LogDebug("[REPO:Local] MedicalCase.Search - PatientName={PatientName} DiagnosisKeyword={DiagnosisKeyword}",
                    patientName ?? "无", diagnosisKeyword ?? "无");
                return await _localApi.SearchMedicalCasesAsync(patientName, diagnosisKeyword, startDate, endDate, page, pageSize);
            }

            _logger.LogDebug("[REPO:Remote] MedicalCase.Search - PatientName={PatientName} DiagnosisKeyword={DiagnosisKeyword}",
                patientName ?? "无", diagnosisKeyword ?? "无");

            var response = await _api.SearchMedicalCasesAsync(patientName, diagnosisKeyword, startDate, endDate, page, pageSize);
            return response.Data ?? new PagedResult<MedicalCaseDetailDto>
            {
                Items = [],
                TotalCount = 0,
                CurrentPage = page,
                PageSize = pageSize
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[REPO:{Mode}] MedicalCase.Search failed", IsOffline ? "Local" : "Remote");
            throw;
        }
    }

    public async Task<PagedResult<MedicalCaseListDto>> QueryAsync(MedicalCaseQueryDto query)
    {
        try
        {
            if (IsOffline)
            {
                _logger.LogDebug("[REPO:Local] MedicalCase.Query - QueryType={QueryType}", query.QueryType);
                return await _localApi.QueryMedicalCasesAsync(
                    query.QueryType,
                    query.PatientId,
                    query.DoctorId,
                    query.Keyword,
                    query.PageIndex,
                    query.PageSize,
                    query.IncludeAllDoctors,
                    query.Limit);
            }

            _logger.LogDebug("[REPO:Remote] MedicalCase.Query - QueryType={QueryType}", query.QueryType);

            var response = await _api.QueryMedicalCasesAsync(
                queryType: query.QueryType,
                patientId: query.PatientId,
                doctorId: query.DoctorId,
                keyword: query.Keyword,
                pageIndex: query.PageIndex,
                pageSize: query.PageSize,
                includeAllDoctors: query.IncludeAllDoctors,
                limit: query.Limit);
            return response.Data ?? new PagedResult<MedicalCaseListDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[REPO:{Mode}] MedicalCase.Query failed - QueryType={QueryType}", IsOffline ? "Local" : "Remote", query.QueryType);
            throw;
        }
    }

    #endregion

    #region 生命周期操作

    public async Task<MedicalCaseDetailDto?> CloseCaseAsync(Guid medicalCaseId)
    {
        if (medicalCaseId == Guid.Empty)
            throw new ArgumentException("医案ID不能为空", nameof(medicalCaseId));

        try
        {
            if (IsOffline)
            {
                _logger.LogInformation("[REPO:Local] MedicalCase.CloseCase - Id={Id}", medicalCaseId);
                return await _localApi.CloseCaseAsync(medicalCaseId);
            }

            _logger.LogInformation("[REPO:Remote] MedicalCase.CloseCase - Id={Id}", medicalCaseId);

            var response = await _api.CloseCaseAsync(medicalCaseId);
            if (response.Success)
            {
                _logger.LogInformation("[REPO:Remote] MedicalCase.CloseCase completed - Id={Id}", medicalCaseId);
                return response.Data;
            }

            _logger.LogWarning("[REPO:Remote] MedicalCase.CloseCase failed - Id={Id}, Message={Message}",
                medicalCaseId, response.Message);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[REPO:{Mode}] MedicalCase.CloseCase failed - Id={Id}", IsOffline ? "Local" : "Remote", medicalCaseId);
            throw;
        }
    }

    public async Task<MedicalCaseDetailDto?> CancelMedicalCaseAsync(Guid id, CancelMedicalCaseRequestDto? request)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("医案ID不能为空", nameof(id));

        try
        {
            if (IsOffline)
            {
                _logger.LogInformation("[REPO:Local] MedicalCase.Cancel - Id={Id}", id);
                await _localApi.CancelMedicalCaseAsync(id, request);
                return null;
            }

            _logger.LogInformation("[REPO:Remote] MedicalCase.Cancel - Id={Id}, Reason={Reason}",
                id, request?.Reason ?? "无");

            var response = await _api.CancelMedicalCaseAsync(id, request);
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("[REPO:Remote] MedicalCase.Cancel completed - Id={Id}", id);
                return null;
            }

            _logger.LogWarning("[REPO:Remote] MedicalCase.Cancel failed - Id={Id}, StatusCode={StatusCode}",
                id, response.StatusCode);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[REPO:{Mode}] MedicalCase.Cancel failed - Id={Id}", IsOffline ? "Local" : "Remote", id);
            throw;
        }
    }

    public async Task<MedicalCaseDetailDto?> SuspendAsync(Guid id, ConsultationInputDto? request)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("医案ID不能为空", nameof(id));

        try
        {
            if (IsOffline)
            {
                _logger.LogInformation("[REPO:Local] MedicalCase.Suspend - Id={Id}", id);
                return await _localApi.SuspendAsync(id, request);
            }

            _logger.LogInformation("[REPO:Remote] MedicalCase.Suspend - Id={Id}", id);

            var response = await _api.SuspendAsync(id, request);
            if (response.Success)
            {
                _logger.LogInformation("[REPO:Remote] MedicalCase.Suspend completed - Id={Id}", id);
                return response.Data;
            }

            _logger.LogWarning("[REPO:Remote] MedicalCase.Suspend failed - Id={Id}, Message={Message}",
                id, response.Message);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[REPO:{Mode}] MedicalCase.Suspend failed - Id={Id}", IsOffline ? "Local" : "Remote", id);
            throw;
        }
    }

    public async Task<MedicalCaseDetailDto?> UpdateStatusAsync(Guid id, MedicalCaseStatusInputDto request)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("医案ID不能为空", nameof(id));
        ArgumentNullException.ThrowIfNull(request);

        try
        {
            if (IsOffline)
            {
                _logger.LogInformation("[REPO:Local] MedicalCase.UpdateStatus - Id={Id}, Status={Status}", id, request.Status);
                return await _localApi.UpdateStatusAsync(id, request);
            }

            _logger.LogInformation("[REPO:Remote] MedicalCase.UpdateStatus - Id={Id}, Status={Status}",
                id, request.Status);

            var response = await _api.UpdateStatusAsync(id, request);
            if (response.Success)
            {
                _logger.LogInformation("[REPO:Remote] MedicalCase.UpdateStatus completed - Id={Id}", id);
                return response.Data;
            }

            _logger.LogWarning("[REPO:Remote] MedicalCase.UpdateStatus failed - Id={Id}, Message={Message}",
                id, response.Message);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[REPO:{Mode}] MedicalCase.UpdateStatus failed - Id={Id}", IsOffline ? "Local" : "Remote", id);
            throw;
        }
    }

    #endregion

    #region 权限与聚合保存

    public async Task<MedicalCasePermissionDto?> GetPermissionsAsync(Guid medicalCaseId)
    {
        if (medicalCaseId == Guid.Empty)
            throw new ArgumentException("医案ID不能为空", nameof(medicalCaseId));

        try
        {
            if (IsOffline)
            {
                _logger.LogDebug("[REPO:Local] MedicalCase.GetPermissions - Id={Id}", medicalCaseId);
                return await _localApi.GetPermissionsAsync(medicalCaseId);
            }

            _logger.LogDebug("[REPO:Remote] MedicalCase.GetPermissions - Id={Id}", medicalCaseId);

            var response = await _api.GetPermissionsAsync(medicalCaseId);
            if (response.Success && response.Data != null)
            {
                _logger.LogDebug("[REPO:Remote] MedicalCase.GetPermissions completed - Id={Id}, CanEdit={CanEdit}",
                    medicalCaseId, response.Data.CanEdit);
                return response.Data;
            }

            _logger.LogWarning("[REPO:Remote] MedicalCase.GetPermissions failed - Id={Id}, Message={Message}",
                medicalCaseId, response.Message);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[REPO:{Mode}] MedicalCase.GetPermissions failed - Id={Id}", IsOffline ? "Local" : "Remote", medicalCaseId);
            throw;
        }
    }

    public async Task<MedicalCaseDetailDto> SaveAsync(Guid medicalCaseId, MedicalCaseInputDto dto)
    {
        if (medicalCaseId == Guid.Empty)
            throw new ArgumentException("医案ID不能为空", nameof(medicalCaseId));
        ArgumentNullException.ThrowIfNull(dto);

        try
        {
            if (IsOffline)
            {
                _logger.LogInformation("[REPO:Local] MedicalCase.Save - Id={Id}, HasConsultation={HasConsultation}, HasPrescription={HasPrescription}",
                    medicalCaseId, dto.Consultation != null, dto.Prescription != null);
                return await _localApi.SaveAsync(medicalCaseId, dto);
            }

            _logger.LogInformation("[REPO:Remote] MedicalCase.Save - Id={Id}, HasConsultation={HasConsultation}, HasPrescription={HasPrescription}",
                medicalCaseId, dto.Consultation != null, dto.Prescription != null);

            var response = await _api.SaveAsync(medicalCaseId, dto);
            if (!response.Success || response.Data == null)
                throw new InvalidOperationException(response.Message ?? "聚合保存医案失败");

            _logger.LogInformation("[REPO:Remote] MedicalCase.Save completed - Id={Id}", medicalCaseId);
            return response.Data;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[REPO:{Mode}] MedicalCase.Save failed - Id={Id}", IsOffline ? "Local" : "Remote", medicalCaseId);
            throw;
        }
    }

    #endregion

    #region 处方标志与打印

    public async Task<MedicalCaseDetailDto?> SetPrescriptionFlagAsync(Guid id, SetPrescriptionFlagRequest request)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("医案ID不能为空", nameof(id));
        ArgumentNullException.ThrowIfNull(request);

        try
        {
            if (IsOffline)
            {
                _logger.LogInformation("[REPO:Local] MedicalCase.SetPrescriptionFlag - Id={Id}, NeedsPrescription={NeedsPrescription}",
                    id, request.NeedsPrescription);
                return await _localApi.SetPrescriptionFlagAsync(id, request);
            }

            _logger.LogInformation("[REPO:Remote] MedicalCase.SetPrescriptionFlag - Id={Id}, NeedsPrescription={NeedsPrescription}",
                id, request.NeedsPrescription);

            var response = await _api.SetPrescriptionFlagAsync(id, request);
            if (response.Success)
            {
                _logger.LogInformation("[REPO:Remote] MedicalCase.SetPrescriptionFlag completed - Id={Id}", id);
                return response.Data;
            }

            _logger.LogWarning("[REPO:Remote] MedicalCase.SetPrescriptionFlag failed - Id={Id}, Message={Message}",
                id, response.Message);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[REPO:{Mode}] MedicalCase.SetPrescriptionFlag failed - Id={Id}", IsOffline ? "Local" : "Remote", id);
            throw;
        }
    }

    public async Task<MedicalCaseDetailDto?> RecordPrintCompletedAsync(Guid medicalCaseId, PrintCompletedRequest request)
    {
        if (medicalCaseId == Guid.Empty)
            throw new ArgumentException("医案ID不能为空", nameof(medicalCaseId));

        try
        {
            if (IsOffline)
            {
                _logger.LogInformation("[REPO:Local] MedicalCase.RecordPrintCompleted - Id={Id}", medicalCaseId);
                return await _localApi.RecordPrintCompletedAsync(medicalCaseId, request);
            }

            _logger.LogInformation("[REPO:Remote] MedicalCase.RecordPrintCompleted - Id={Id}", medicalCaseId);

            var response = await _api.RecordPrintCompletedAsync(medicalCaseId, request);
            if (response.Success)
            {
                _logger.LogInformation("[REPO:Remote] MedicalCase.RecordPrintCompleted completed - Id={Id}", medicalCaseId);
                return response.Data;
            }

            _logger.LogWarning("[REPO:Remote] MedicalCase.RecordPrintCompleted failed - Id={Id}, Message={Message}",
                medicalCaseId, response.Message);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[REPO:{Mode}] MedicalCase.RecordPrintCompleted failed - Id={Id}", IsOffline ? "Local" : "Remote", medicalCaseId);
            throw;
        }
    }

    #endregion

    #region 批量操作

    public async Task<List<MedicalCaseDetailDto>> GetBatchDetailsAsync(List<Guid> ids)
    {
        if (ids == null || ids.Count == 0)
            return [];

        if (ids.Count > 50)
            throw new ArgumentException("单次最多查询50个医案", nameof(ids));

        try
        {
            if (IsOffline)
            {
                _logger.LogInformation("[REPO:Local] MedicalCase.GetBatchDetails - Count={Count}", ids.Count);
                return await _localApi.GetBatchDetailsAsync(new BatchDetailQueryDto { Ids = ids });
            }

            _logger.LogInformation("[REPO:Remote] MedicalCase.GetBatchDetails - Count={Count}", ids.Count);

            var request = new BatchDetailQueryDto { Ids = ids };
            var response = await _api.GetBatchDetailsAsync(request);

            if (response.Success && response.Data != null)
            {
                _logger.LogInformation("[REPO:Remote] MedicalCase.GetBatchDetails completed - Count={Count}", response.Data.Count);
                return response.Data;
            }

            _logger.LogWarning("[REPO:Remote] MedicalCase.GetBatchDetails failed - Message={Message}", response.Message);
            return [];
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[REPO:{Mode}] MedicalCase.GetBatchDetails failed - Count={Count}", IsOffline ? "Local" : "Remote", ids.Count);
            throw;
        }
    }

    public async Task<BatchOperationResultDto?> BatchDeleteAsync(List<Guid> ids)
    {
        try
        {
            if (IsOffline)
            {
                _logger.LogInformation("[REPO:Local] MedicalCase.BatchDelete - Count={Count}", ids.Count);
                return await _localApi.BatchDeleteAsync(new BatchDeleteInputDto { Ids = ids });
            }

            _logger.LogInformation("[REPO:Remote] MedicalCase.BatchDelete - Count={Count}", ids.Count);

            var response = await _api.BatchDeleteAsync(new BatchDeleteInputDto { Ids = ids });
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
            _logger.LogError(ex, "[REPO:{Mode}] MedicalCase.BatchDelete failed", IsOffline ? "Local" : "Remote");
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
