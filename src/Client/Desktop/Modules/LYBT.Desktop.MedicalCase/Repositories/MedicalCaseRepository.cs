using LYBT.Desktop.Contracts.Api;
using LYBT.Desktop.Contracts.Repositories;
using LYBT.Desktop.Contracts.Services;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Shared.Models.Contracts.MedicalCase;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.MedicalCase.Repositories;

/// <summary>
/// 医案仓储 - 通过 Refit IMedicalCaseApi 访问 WebAPI。
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
            _logger.LogError(ex, "[REPO:Remote] MedicalCase.GetPaged failed");
            throw;
        }
    }

    public async Task<MedicalCaseDetailDto?> GetByIdAsync(Guid id)
    {
        try
        {
            _logger.LogDebug("[REPO:Remote] MedicalCase.GetById - Id={Id}", id);

            var response = await _api.GetMedicalCaseByIdAsync(id);
            return response.Data;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[REPO:Remote] MedicalCase.GetById failed - Id={Id}", id);
            throw;
        }
    }

    public async Task<MedicalCaseDetailDto> CreateAsync(MedicalCaseInputDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        try
        {
            _logger.LogInformation("[REPO:Remote] MedicalCase.Create - PatientId={PatientId}", dto.PatientId);

            var response = await _api.CreateMedicalCaseAsync(dto);
            if (!response.Success || response.Data == null)
                throw new InvalidOperationException(response.Message ?? "创建医案失败");

            _logger.LogInformation("[REPO:Remote] MedicalCase.Create completed - Id={Id}", response.Data.Id);
            return response.Data;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[REPO:Remote] MedicalCase.Create failed");
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
            _logger.LogInformation("[REPO:Remote] MedicalCase.Update - Id={Id}", dto.Id);

            var response = await _api.SaveAsync(dto.Id.Value, dto);
            if (!response.Success || response.Data == null)
                throw new InvalidOperationException(response.Message ?? "更新医案失败");

            _logger.LogInformation("[REPO:Remote] MedicalCase.Update completed - Id={Id}", response.Data.Id);
            return response.Data;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[REPO:Remote] MedicalCase.Update failed - Id={Id}", dto.Id);
            throw;
        }
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        try
        {
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
            _logger.LogError(ex, "[REPO:Remote] MedicalCase.Delete failed - Id={Id}", id);
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
            _logger.LogError(ex, "[REPO:Remote] MedicalCase.Search failed");
            throw;
        }
    }

    public async Task<PagedResult<MedicalCaseListDto>> QueryAsync(MedicalCaseQueryDto query)
    {
        try
        {
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
            _logger.LogError(ex, "[REPO:Remote] MedicalCase.Query failed - QueryType={QueryType}", query.QueryType);
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
            _logger.LogError(ex, "[REPO:Remote] MedicalCase.CloseCase failed - Id={Id}", medicalCaseId);
            throw;
        }
    }

    public async Task<MedicalCaseDetailDto?> CancelMedicalCaseAsync(Guid id, CancelMedicalCaseRequestDto? request)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("医案ID不能为空", nameof(id));

        try
        {
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
            _logger.LogError(ex, "[REPO:Remote] MedicalCase.Cancel failed - Id={Id}", id);
            throw;
        }
    }

    public async Task<MedicalCaseDetailDto?> SuspendAsync(Guid id, ConsultationInputDto? request)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("医案ID不能为空", nameof(id));

        try
        {
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
            _logger.LogError(ex, "[REPO:Remote] MedicalCase.Suspend failed - Id={Id}", id);
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
            _logger.LogError(ex, "[REPO:Remote] MedicalCase.UpdateStatus failed - Id={Id}", id);
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
            _logger.LogError(ex, "[REPO:Remote] MedicalCase.GetPermissions failed - Id={Id}", medicalCaseId);
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
            _logger.LogError(ex, "[REPO:Remote] MedicalCase.Save failed - Id={Id}", medicalCaseId);
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
            _logger.LogError(ex, "[REPO:Remote] MedicalCase.SetPrescriptionFlag failed - Id={Id}", id);
            throw;
        }
    }

    public async Task<MedicalCaseDetailDto?> RecordPrintCompletedAsync(Guid medicalCaseId, PrintCompletedRequest request)
    {
        if (medicalCaseId == Guid.Empty)
            throw new ArgumentException("医案ID不能为空", nameof(medicalCaseId));

        try
        {
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
            _logger.LogError(ex, "[REPO:Remote] MedicalCase.RecordPrintCompleted failed - Id={Id}", medicalCaseId);
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
            _logger.LogError(ex, "[REPO:Remote] MedicalCase.GetBatchDetails failed - Count={Count}", ids.Count);
            throw;
        }
    }

    public async Task<BatchOperationResultDto?> BatchDeleteAsync(List<Guid> ids)
    {
        try
        {
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
            _logger.LogError(ex, "[REPO:Remote] MedicalCase.BatchDelete failed");
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
