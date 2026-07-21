using LYBT.Desktop.Contracts.Repositories;
using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.MedicalCase.Interfaces;
using LYBT.Desktop.Foundation.ExceptionHandling;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Contracts.Prescriptions;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Logging;
using System.Threading;

namespace LYBT.Desktop.MedicalCase.Services;

/// <summary>
/// 医案Service - 聚合代理
/// 委托 Query/Command/Lifecycle 三个独立服务，自身保留 Coordinator 职责
/// </summary>
public class MedicalCaseService : IMedicalCaseService
{
    private readonly IMedicalCaseRepository _repository;
    private readonly IMedicalCaseQueryService _queryService;
    private readonly IMedicalCaseCommandService _commandService;
    private readonly IMedicalCaseLifecycleService _lifecycleService;
    private readonly MedicalCaseEditContext _context;
    private readonly ISessionManager? _sessionManager;
    private readonly ILogger<MedicalCaseService> _logger;

    public MedicalCaseService(
        IMedicalCaseRepository repository,
        IMedicalCaseQueryService queryService,
        IMedicalCaseCommandService commandService,
        IMedicalCaseLifecycleService lifecycleService,
        MedicalCaseEditContext context,
        ILogger<MedicalCaseService> logger,
        ISessionManager? sessionManager = null)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _queryService = queryService ?? throw new ArgumentNullException(nameof(queryService));
        _commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));
        _lifecycleService = lifecycleService ?? throw new ArgumentNullException(nameof(lifecycleService));
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _sessionManager = sessionManager;
    }

    #region IMedicalCaseQueryService 委托

    public virtual async Task<PagedResult<MedicalCaseListDto>?> GetPagedAsync(int page, int pageSize, string? searchText = null, CancellationToken ct = default)
        => await _queryService.GetPagedAsync(page, pageSize, searchText, ct);

    public virtual async Task<PagedResult<MedicalCaseListDto>?> QueryAsync(MedicalCaseQueryDto query, CancellationToken ct = default)
        => await _queryService.QueryAsync(query, ct);

    public virtual async Task<MedicalCaseDetailDto?> GetUnfinishedCaseByPatientIdAsync(Guid patientId, Guid doctorId, bool checkAllDoctors = false, CancellationToken ct = default)
        => await _queryService.GetUnfinishedCaseByPatientIdAsync(patientId, doctorId, checkAllDoctors, ct);

    #endregion

    #region IMedicalCaseCommandService 委托

    public MedicalCaseDetailDto? Current => _commandService.Current;
    public bool HasChanges => _commandService.HasChanges;

    public virtual async Task<bool> SaveAsync(CancellationToken ct = default)
        => await _commandService.SaveAsync(ct);

    public virtual async Task<bool> DeleteAsync(CancellationToken ct = default)
        => await _commandService.DeleteAsync(ct);

    public virtual async Task<(bool success, Guid medicalCaseId, string? errorMessage)> CreateMedicalCaseAsync(Guid patientId, Guid? registrationId = null, CancellationToken ct = default)
        => await _commandService.CreateMedicalCaseAsync(patientId, registrationId, ct);

    #endregion

    #region IMedicalCaseLifecycleService 委托

    public Guid MedicalCaseId => _lifecycleService.MedicalCaseId;
    public ConsultationDetailDto? CurrentConsultation => _lifecycleService.CurrentConsultation;
    public PrescriptionDetailDto? CurrentPrescription => _lifecycleService.CurrentPrescription;

    public async Task InitializeAsync(Guid entityId, CancellationToken ct = default)
        => await _lifecycleService.InitializeAsync(entityId, ct);

    public virtual async Task ReloadAsync(CancellationToken ct = default)
        => await _lifecycleService.ReloadAsync(ct);

    public virtual async Task<(bool success, string? errorMessage)> SuspendAsync(Guid medicalCaseId, CancellationToken ct = default)
        => await _lifecycleService.SuspendAsync(medicalCaseId, ct);

    public virtual async Task<(bool success, string? errorMessage)> CancelMedicalCaseAsync(Guid medicalCaseId, string? reason = null, CancellationToken ct = default)
        => await _lifecycleService.CancelMedicalCaseAsync(medicalCaseId, reason, ct);

    public virtual async Task<(bool success, string? errorMessage)> CompleteMedicalCaseAsync(Guid medicalCaseId, CancellationToken ct = default)
        => await _lifecycleService.CompleteMedicalCaseAsync(medicalCaseId, ct);

    public virtual async Task<(bool success, string? errorMessage)> ResumeSuspendedAsync(Guid medicalCaseId, CancellationToken ct = default)
        => await _lifecycleService.ResumeSuspendedAsync(medicalCaseId, ct);

    public virtual async Task<ApiResponse<MedicalCaseDetailDto>> CloseCaseAsync(Guid medicalCaseId, CancellationToken ct = default)
        => await _lifecycleService.CloseCaseAsync(medicalCaseId, ct);

    #endregion

    #region IMedicalCaseService 独有成员（Coordinator 职责）

    public MedicalCaseDetailDto? CachedMedicalCase => _context.CachedMedicalCase;
    public ConsultationDetailDto? CachedConsultation => _context.CachedConsultation;
    public PrescriptionDetailDto? CachedPrescription => _context.CachedPrescription;

    public async Task<(bool success, MedicalCaseDetailDto? detail, string? errorMessage)> LoadDetailsAsync(Guid medicalCaseId, CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("[SVC] MedicalCase.LoadDetails started - MedicalCaseId={MedicalCaseId}", medicalCaseId);

            var detail = await _repository.GetByIdAsync(medicalCaseId);
            if (detail == null)
            {
                _logger.LogWarning("[SVC] MedicalCase.LoadDetails → NotFound - MedicalCaseId={MedicalCaseId}", medicalCaseId);
                return (false, null, "未找到医案数据");
            }

            _context.CachedMedicalCase = detail;
            _context.CachedConsultation = detail.Consultation;
            _context.CachedPrescription = detail.Prescription;

            _logger.LogInformation("[SVC] MedicalCase.LoadDetails completed");
            return (true, detail, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[SVC] MedicalCase.LoadDetails failed - MedicalCaseId={MedicalCaseId}", medicalCaseId);
            return (false, null, ClientErrorMessageMapper.GetSafeOperationFailureMessage("加载医案数据", ex));
        }
    }

    public void ClearCache()
    {
        _logger.LogDebug("[SVC] MedicalCase.ClearCache");
        _context.ClearCache();
    }

    public async Task<(bool Success, MedicalCaseDetailDto? Data, string? Error)> AggregateSaveAsync(
        Guid medicalCaseId,
        ConsultationInputDto? consultation,
        PrescriptionInputDto? prescription,
        string? remark = null,
        string? editReason = null,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("[SVC] MedicalCase.AggregateSave started - MedicalCaseId={MedicalCaseId}", medicalCaseId);

            var aggregateDto = new MedicalCaseInputDto
            {
                Id = medicalCaseId,
                EditReason = editReason,
                Consultation = consultation,
                Prescription = prescription
            };

            var result = await _repository.SaveAsync(medicalCaseId, aggregateDto);

            _context.CachedMedicalCase = result;
            _context.CachedConsultation = result?.Consultation;
            _context.CachedPrescription = result?.Prescription;

            _logger.LogInformation("[SVC] MedicalCase.AggregateSave completed - MedicalCaseId={MedicalCaseId}", medicalCaseId);
            return (true, result, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[SVC] MedicalCase.AggregateSave failed - MedicalCaseId={MedicalCaseId}", medicalCaseId);
            return (false, null, ClientErrorMessageMapper.GetSafeOperationFailureMessage("保存", ex));
        }
    }

    public async Task<(bool Success, string? Error)> SaveAndCompleteAsync(
        Guid medicalCaseId,
        ConsultationInputDto? consultation,
        PrescriptionInputDto? prescription,
        IValidatable? consultationValidator,
        IValidatable? prescriptionValidator,
        string? remark = null,
        bool isPrescriptionEnabled = true,
        CancellationToken ct = default)
    {
        if (consultationValidator != null && !consultationValidator.Validate())
            return (false, consultationValidator.ValidationMessage);
        if (isPrescriptionEnabled && prescriptionValidator != null && !prescriptionValidator.Validate())
            return (false, prescriptionValidator.ValidationMessage);

        var (saveOk, _, saveError) = await AggregateSaveAsync(medicalCaseId, consultation, prescription, remark);
        if (!saveOk) return (false, saveError);

        return await CompleteMedicalCaseAsync(medicalCaseId);
    }

    public async Task<(bool Success, string? Error)> SaveAndSuspendAsync(
        Guid medicalCaseId,
        ConsultationInputDto? consultation,
        PrescriptionInputDto? prescription,
        string? remark = null,
        CancellationToken ct = default)
    {
        var (saveOk, _, saveError) = await AggregateSaveAsync(medicalCaseId, consultation, prescription, remark);
        if (!saveOk) return (false, saveError);

        return await SuspendAsync(medicalCaseId);
    }

    public async Task<(bool Success, string? Error)> SaveAndCancelAsync(
        Guid medicalCaseId,
        ConsultationInputDto? consultation,
        PrescriptionInputDto? prescription,
        string? remark = null,
        CancellationToken ct = default)
    {
        try
        {
            await AggregateSaveAsync(medicalCaseId, consultation, prescription, remark);
        }
        catch (Exception saveEx)
        {
            _logger.LogWarning(saveEx, "[SVC] MedicalCase.SaveAndCancel → SaveFailed, proceeding with cancel");
        }

        return await CancelMedicalCaseAsync(medicalCaseId);
    }

    #endregion

    #region 额外业务方法（非接口成员，供 ViewModel 直接调用）

    public virtual async Task<MedicalCaseDetailDto?> GetByIdSimpleAsync(Guid id)
    {
        try
        {
            _logger.LogDebug("[SVC] MedicalCase.GetByIdSimple started - MedicalCaseId={MedicalCaseId}", id);
            var result = await _repository.GetByIdAsync(id);
            if (result == null)
                _logger.LogWarning("[SVC] MedicalCase.GetByIdSimple → NotFound - MedicalCaseId={MedicalCaseId}", id);
            return result;
        }
        catch (Exception ex) { _logger.LogError(ex, "[SVC] MedicalCase.GetByIdSimple failed - MedicalCaseId={MedicalCaseId}", id); return null; }
    }

    public virtual async Task<ApiResponse<MedicalCaseDetailDto>> SetPrescriptionFlagAsync(Guid medicalCaseId, SetPrescriptionFlagRequest request)
    {
        try
        {
            _logger.LogInformation("[SVC] MedicalCase.SetPrescriptionFlag started - MedicalCaseId={MedicalCaseId} NeedsPrescription={NeedsPrescription}",
                medicalCaseId, request.NeedsPrescription);
            var data = await _repository.SetPrescriptionFlagAsync(medicalCaseId, request);

            if (data != null)
            {
                _logger.LogInformation("[SVC] MedicalCase.SetPrescriptionFlag completed - MedicalCaseId={MedicalCaseId}", medicalCaseId);
                return new ApiResponse<MedicalCaseDetailDto> { Success = true, Data = data };
            }
            else
            {
                _logger.LogWarning("[SVC] MedicalCase.SetPrescriptionFlag failed - MedicalCaseId={MedicalCaseId}", medicalCaseId);
                return new ApiResponse<MedicalCaseDetailDto> { Success = false, Message = "设置处方标志失败" };
            }
        }
        catch (Exception ex) { _logger.LogError(ex, "[SVC] MedicalCase.SetPrescriptionFlag failed - MedicalCaseId={MedicalCaseId}", medicalCaseId); throw; }
    }

    public virtual async Task<ApiResponse> DeleteMedicalCaseAsync(Guid medicalCaseId)
    {
        try
        {
            _logger.LogInformation("[SVC] MedicalCase.DeleteViaApi started - MedicalCaseId={MedicalCaseId}", medicalCaseId);
            await _repository.DeleteAsync(medicalCaseId);

            _logger.LogInformation("[SVC] MedicalCase.DeleteViaApi completed - MedicalCaseId={MedicalCaseId}", medicalCaseId);
            return new ApiResponse { Success = true, Message = "医案已取消" };
        }
        catch (Exception ex) { _logger.LogError(ex, "[SVC] MedicalCase.DeleteViaApi failed - MedicalCaseId={MedicalCaseId}", medicalCaseId); return new ApiResponse { Success = false, Message = ClientErrorMessageMapper.GetSafeOperationFailureMessage("删除", ex) }; }
    }

    public virtual async Task<ApiResponse<MedicalCaseDetailDto>> UpdateStatusAsync(Guid medicalCaseId, MedicalCaseStatusInputDto request)
    {
        try
        {
            _logger.LogInformation("[SVC] MedicalCase.UpdateStatus started - MedicalCaseId={MedicalCaseId} Status={Status}", medicalCaseId, request.Status);
            var data = await _repository.UpdateStatusAsync(medicalCaseId, request);

            if (data != null)
            {
                _logger.LogInformation("[SVC] MedicalCase.UpdateStatus completed - MedicalCaseId={MedicalCaseId}", medicalCaseId);
                return new ApiResponse<MedicalCaseDetailDto> { Success = true, Data = data };
            }
            else
            {
                _logger.LogWarning("[SVC] MedicalCase.UpdateStatus failed - MedicalCaseId={MedicalCaseId}", medicalCaseId);
                return new ApiResponse<MedicalCaseDetailDto> { Success = false, Message = "更新状态失败" };
            }
        }
        catch (Exception ex) { _logger.LogError(ex, "[SVC] MedicalCase.UpdateStatus failed - MedicalCaseId={MedicalCaseId}", medicalCaseId); throw; }
    }

    public virtual async Task<ApiResponse<MedicalCaseDetailDto>> SuspendViaApiAsync(Guid medicalCaseId, ConsultationInputDto? consultationData = null)
    {
        try
        {
            _logger.LogInformation("[SVC] MedicalCase.SuspendViaApi started - MedicalCaseId={MedicalCaseId}", medicalCaseId);
            var data = await _repository.SuspendAsync(medicalCaseId, consultationData);

            if (data != null)
            {
                _logger.LogInformation("[SVC] MedicalCase.SuspendViaApi completed - MedicalCaseId={MedicalCaseId}", medicalCaseId);
                return new ApiResponse<MedicalCaseDetailDto> { Success = true, Data = data };
            }
            else
            {
                _logger.LogWarning("[SVC] MedicalCase.SuspendViaApi failed - MedicalCaseId={MedicalCaseId}", medicalCaseId);
                return new ApiResponse<MedicalCaseDetailDto> { Success = false, Message = "挂起医案失败" };
            }
        }
        catch (Exception ex) { _logger.LogError(ex, "[SVC] MedicalCase.SuspendViaApi failed - MedicalCaseId={MedicalCaseId}", medicalCaseId); throw; }
    }

    public virtual async Task<ApiResponse<MedicalCaseDetailDto>> CancelMedicalCaseViaApiAsync(Guid medicalCaseId, string? reason = null)
    {
        try
        {
            _logger.LogInformation("[SVC] MedicalCase.CancelViaApi started - MedicalCaseId={MedicalCaseId}", medicalCaseId);
            var request = string.IsNullOrEmpty(reason) ? null : new CancelMedicalCaseRequestDto { Reason = reason };
            var data = await _repository.CancelMedicalCaseAsync(medicalCaseId, request);

            if (data != null)
            {
                _logger.LogInformation("[SVC] MedicalCase.CancelViaApi completed - MedicalCaseId={MedicalCaseId}", medicalCaseId);
                return new ApiResponse<MedicalCaseDetailDto> { Success = true, Data = data };
            }
            else
            {
                _logger.LogWarning("[SVC] MedicalCase.CancelViaApi failed - MedicalCaseId={MedicalCaseId}", medicalCaseId);
                return new ApiResponse<MedicalCaseDetailDto> { Success = false, Message = "取消医案失败" };
            }
        }
        catch (Exception ex) { _logger.LogError(ex, "[SVC] MedicalCase.CancelViaApi failed - MedicalCaseId={MedicalCaseId}", medicalCaseId); throw; }
    }

    #endregion
}
