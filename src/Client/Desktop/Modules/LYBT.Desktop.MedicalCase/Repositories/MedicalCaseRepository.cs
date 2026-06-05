using LYBT.Desktop.Contracts.ApiClient;
using LYBT.Desktop.Contracts.Repositories;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Shared.Models.Contracts.MedicalCase;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.MedicalCase.Repositories;

/// <summary>
/// 医案仓储 — routes all calls through IApiClient.
/// </summary>
public sealed class MedicalCaseRepository : IMedicalCaseRepository
{
    private readonly IApiClient _apiClient;
    private readonly ILogger<MedicalCaseRepository> _logger;

    public MedicalCaseRepository(
        IApiClient apiClient,
        ILogger<MedicalCaseRepository> logger)
    {
        _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    #region 标准 CRUD 操作

    public async Task<PagedResult<MedicalCaseListDto>> GetPagedAsync(int page = 1, int pageSize = 20, string? keyword = null)
    {
        try
        {
            _logger.LogDebug("[REPO] MedicalCase.GetPaged - Page={Page} PageSize={PageSize} Keyword={Keyword}",
                page, pageSize, keyword);

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
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[REPO] MedicalCase.GetPaged failed");
            throw;
        }
    }

    public async Task<MedicalCaseDetailDto?> GetByIdAsync(Guid id)
    {
        try
        {
            _logger.LogDebug("[REPO] MedicalCase.GetById - Id={Id}", id);
            var response = await _apiClient.MedicalCases.GetMedicalCaseByIdAsync(id);
            return response.Data;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[REPO] MedicalCase.GetById failed - Id={Id}", id);
            throw;
        }
    }

    public async Task<MedicalCaseDetailDto> CreateAsync(MedicalCaseInputDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        try
        {
            _logger.LogInformation("[REPO] MedicalCase.Create - PatientId={PatientId}", dto.PatientId);

            var response = await _apiClient.MedicalCases.CreateMedicalCaseAsync(dto);
            if (!response.Success || response.Data == null)
                throw new InvalidOperationException(response.Message ?? "创建医案失败");

            _logger.LogInformation("[REPO] MedicalCase.Create completed - Id={Id}", response.Data.Id);
            return response.Data;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[REPO] MedicalCase.Create failed");
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
            _logger.LogInformation("[REPO] MedicalCase.Update - Id={Id}", dto.Id);

            var response = await _apiClient.MedicalCases.SaveAsync(dto.Id.Value, dto);
            if (!response.Success || response.Data == null)
                throw new InvalidOperationException(response.Message ?? "更新医案失败");

            _logger.LogInformation("[REPO] MedicalCase.Update completed - Id={Id}", response.Data.Id);
            return response.Data;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[REPO] MedicalCase.Update failed - Id={Id}", dto.Id);
            throw;
        }
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        try
        {
            _logger.LogInformation("[REPO] MedicalCase.Delete - Id={Id}", id);

            var response = await _apiClient.MedicalCases.DeleteMedicalCaseAsync(id);
            if (response.Success)
                _logger.LogInformation("[REPO] MedicalCase.Delete completed - Id={Id}", id);
            else
                _logger.LogWarning("[REPO] MedicalCase.Delete failed - Id={Id}", id);

            return response.Success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[REPO] MedicalCase.Delete failed - Id={Id}", id);
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
            _logger.LogDebug("[REPO] MedicalCase.Search - PatientName={PatientName} DiagnosisKeyword={DiagnosisKeyword}",
                patientName ?? "无", diagnosisKeyword ?? "无");

            var response = await _apiClient.MedicalCases.SearchMedicalCasesAsync(
                patientName, diagnosisKeyword, startDate, endDate, page, pageSize);
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
            _logger.LogError(ex, "[REPO] MedicalCase.Search failed");
            throw;
        }
    }

    public async Task<PagedResult<MedicalCaseListDto>> QueryAsync(MedicalCaseQueryDto query)
    {
        try
        {
            _logger.LogDebug("[REPO] MedicalCase.Query - QueryType={QueryType}", query.QueryType);

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
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[REPO] MedicalCase.Query failed - QueryType={QueryType}", query.QueryType);
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
            _logger.LogInformation("[REPO] MedicalCase.CloseCase - Id={Id}", medicalCaseId);

            var response = await _apiClient.MedicalCases.CloseCaseAsync(medicalCaseId);
            if (response.Success)
            {
                _logger.LogInformation("[REPO] MedicalCase.CloseCase completed - Id={Id}", medicalCaseId);
                return response.Data;
            }

            _logger.LogWarning("[REPO] MedicalCase.CloseCase failed - Id={Id}, Message={Message}",
                medicalCaseId, response.Message);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[REPO] MedicalCase.CloseCase failed - Id={Id}", medicalCaseId);
            throw;
        }
    }

    public async Task<MedicalCaseDetailDto?> CancelMedicalCaseAsync(Guid id, CancelMedicalCaseRequestDto? request)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("医案ID不能为空", nameof(id));

        try
        {
            _logger.LogInformation("[REPO] MedicalCase.Cancel - Id={Id}, Reason={Reason}",
                id, request?.Reason ?? "无");

            var response = await _apiClient.MedicalCases.CancelMedicalCaseAsync(id, request);
            if (response.Success)
            {
                _logger.LogInformation("[REPO] MedicalCase.Cancel completed - Id={Id}", id);
                return null;
            }

            _logger.LogWarning("[REPO] MedicalCase.Cancel failed - Id={Id}, Message={Message}",
                id, response.Message);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[REPO] MedicalCase.Cancel failed - Id={Id}", id);
            throw;
        }
    }

    public async Task<MedicalCaseDetailDto?> SuspendAsync(Guid id, ConsultationInputDto? request)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("医案ID不能为空", nameof(id));

        try
        {
            _logger.LogInformation("[REPO] MedicalCase.Suspend - Id={Id}", id);

            var response = await _apiClient.MedicalCases.SuspendAsync(id, request);
            if (response.Success)
            {
                _logger.LogInformation("[REPO] MedicalCase.Suspend completed - Id={Id}", id);
                return response.Data;
            }

            _logger.LogWarning("[REPO] MedicalCase.Suspend failed - Id={Id}, Message={Message}",
                id, response.Message);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[REPO] MedicalCase.Suspend failed - Id={Id}", id);
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
            _logger.LogInformation("[REPO] MedicalCase.UpdateStatus - Id={Id}, Status={Status}",
                id, request.Status);

            var response = await _apiClient.MedicalCases.UpdateStatusAsync(id, request);
            if (response.Success)
            {
                _logger.LogInformation("[REPO] MedicalCase.UpdateStatus completed - Id={Id}", id);
                return response.Data;
            }

            _logger.LogWarning("[REPO] MedicalCase.UpdateStatus failed - Id={Id}, Message={Message}",
                id, response.Message);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[REPO] MedicalCase.UpdateStatus failed - Id={Id}", id);
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
            _logger.LogDebug("[REPO] MedicalCase.GetPermissions - Id={Id}", medicalCaseId);

            var response = await _apiClient.MedicalCases.GetPermissionsAsync(medicalCaseId);
            if (response.Success && response.Data != null)
            {
                _logger.LogDebug("[REPO] MedicalCase.GetPermissions completed - Id={Id}, CanEdit={CanEdit}",
                    medicalCaseId, response.Data.CanEdit);
                return response.Data;
            }

            _logger.LogWarning("[REPO] MedicalCase.GetPermissions failed - Id={Id}, Message={Message}",
                medicalCaseId, response.Message);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[REPO] MedicalCase.GetPermissions failed - Id={Id}", medicalCaseId);
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
            _logger.LogInformation("[REPO] MedicalCase.Save - Id={Id}, HasConsultation={HasConsultation}, HasPrescription={HasPrescription}",
                medicalCaseId, dto.Consultation != null, dto.Prescription != null);

            var response = await _apiClient.MedicalCases.SaveAsync(medicalCaseId, dto);
            if (!response.Success || response.Data == null)
                throw new InvalidOperationException(response.Message ?? "聚合保存医案失败");

            _logger.LogInformation("[REPO] MedicalCase.Save completed - Id={Id}", medicalCaseId);
            return response.Data;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[REPO] MedicalCase.Save failed - Id={Id}", medicalCaseId);
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
            _logger.LogInformation("[REPO] MedicalCase.SetPrescriptionFlag - Id={Id}, NeedsPrescription={NeedsPrescription}",
                id, request.NeedsPrescription);

            var response = await _apiClient.MedicalCases.SetPrescriptionFlagAsync(id, request);
            if (response.Success)
            {
                _logger.LogInformation("[REPO] MedicalCase.SetPrescriptionFlag completed - Id={Id}", id);
                return response.Data;
            }

            _logger.LogWarning("[REPO] MedicalCase.SetPrescriptionFlag failed - Id={Id}, Message={Message}",
                id, response.Message);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[REPO] MedicalCase.SetPrescriptionFlag failed - Id={Id}", id);
            throw;
        }
    }

    public async Task<MedicalCaseDetailDto?> RecordPrintCompletedAsync(Guid medicalCaseId, PrintCompletedRequest request)
    {
        if (medicalCaseId == Guid.Empty)
            throw new ArgumentException("医案ID不能为空", nameof(medicalCaseId));

        try
        {
            _logger.LogInformation("[REPO] MedicalCase.RecordPrintCompleted - Id={Id}", medicalCaseId);

            var response = await _apiClient.MedicalCases.RecordPrintCompletedAsync(medicalCaseId, request);
            if (response.Success)
            {
                _logger.LogInformation("[REPO] MedicalCase.RecordPrintCompleted completed - Id={Id}", medicalCaseId);
                return response.Data;
            }

            _logger.LogWarning("[REPO] MedicalCase.RecordPrintCompleted failed - Id={Id}, Message={Message}",
                medicalCaseId, response.Message);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[REPO] MedicalCase.RecordPrintCompleted failed - Id={Id}", medicalCaseId);
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
            _logger.LogInformation("[REPO] MedicalCase.GetBatchDetails - Count={Count}", ids.Count);

            var request = new BatchDetailQueryDto { Ids = ids };
            var response = await _apiClient.MedicalCases.GetBatchDetailsAsync(request);

            if (response.Success && response.Data != null)
            {
                _logger.LogInformation("[REPO] MedicalCase.GetBatchDetails completed - Count={Count}", response.Data.Count);
                return response.Data;
            }

            _logger.LogWarning("[REPO] MedicalCase.GetBatchDetails failed - Message={Message}", response.Message);
            return [];
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[REPO] MedicalCase.GetBatchDetails failed - Count={Count}", ids.Count);
            throw;
        }
    }

    public async Task<BatchOperationResultDto?> BatchDeleteAsync(List<Guid> ids)
    {
        try
        {
            _logger.LogInformation("[REPO] MedicalCase.BatchDelete - Count={Count}", ids.Count);

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
            _logger.LogError(ex, "[REPO] MedicalCase.BatchDelete failed");
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
