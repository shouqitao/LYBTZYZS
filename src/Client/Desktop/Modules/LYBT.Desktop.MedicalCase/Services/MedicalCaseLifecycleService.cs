using LYBT.Desktop.Contracts.Repositories;
using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Foundation.ExceptionHandling;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Contracts.Prescriptions;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.MedicalCase.Services;

/// <summary>
/// 医案生命周期服务 - 初始化、状态流转
/// 从 MedicalCaseService 拆分，实现 IMedicalCaseLifecycleService
/// </summary>
internal class MedicalCaseLifecycleService : IMedicalCaseLifecycleService
{
    private readonly IMedicalCaseRepository _repository;
    private readonly MedicalCaseEditContext _context;
    private readonly ILogger<MedicalCaseLifecycleService> _logger;

    public MedicalCaseLifecycleService(
        IMedicalCaseRepository repository,
        MedicalCaseEditContext context,
        ILogger<MedicalCaseLifecycleService> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Guid MedicalCaseId => _context.CurrentDetail?.Id ?? Guid.Empty;
    public ConsultationDetailDto? CurrentConsultation => _context.CurrentDetail?.Consultation;
    public PrescriptionDetailDto? CurrentPrescription => _context.CurrentDetail?.Prescription;

    public async Task InitializeAsync(Guid entityId, CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("[LC] MedicalCase.Initialize started - MedicalCaseId={MedicalCaseId}", entityId);
            var detail = await _repository.GetByIdAsync(entityId);
            if (detail == null) throw new InvalidOperationException($"未找到ID为{entityId}的医案");
            _context.SetCurrent(detail);
            _logger.LogInformation("[LC] MedicalCase.Initialize completed - PatientName={PatientName}", detail.PatientName);
        }
        catch (Exception ex) { _logger.LogError(ex, "[LC] MedicalCase.Initialize failed - MedicalCaseId={MedicalCaseId}", entityId); throw; }
    }

    public async Task ReloadAsync(CancellationToken ct = default)
    {
        if (_context.CurrentDetail != null)
        {
            _logger.LogDebug("[LC] MedicalCase.Reload started - MedicalCaseId={MedicalCaseId}", _context.CurrentDetail.Id);
            await InitializeAsync(_context.CurrentDetail.Id);
        }
    }

    public virtual async Task<(bool success, string? errorMessage)> SuspendAsync(Guid medicalCaseId, CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("[LC] MedicalCase.Suspend started - MedicalCaseId={MedicalCaseId}", medicalCaseId);
            var response = await SuspendViaApiAsync(medicalCaseId);
            if (!response.Success)
            {
                _logger.LogWarning("[LC] MedicalCase.Suspend → Failed - Message={Message}", response.Message);
                return (false, response.Message ?? "挂起医案失败");
            }
            _logger.LogInformation("[LC] MedicalCase.Suspend completed - MedicalCaseId={MedicalCaseId}", medicalCaseId);
            return (true, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[LC] MedicalCase.Suspend failed - MedicalCaseId={MedicalCaseId}", medicalCaseId);
            return (false, ClientErrorMessageMapper.GetSafeOperationFailureMessage("挂起", ex));
        }
    }

    public virtual async Task<(bool success, string? errorMessage)> CancelMedicalCaseAsync(Guid medicalCaseId, string? reason = null, CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("[LC] MedicalCase.Cancel started - MedicalCaseId={MedicalCaseId}", medicalCaseId);
            var request = string.IsNullOrEmpty(reason) ? null : new CancelMedicalCaseRequestDto { Reason = reason };
            var data = await _repository.CancelMedicalCaseAsync(medicalCaseId, request);

            if (data != null)
            {
                _logger.LogInformation("[LC] MedicalCase.Cancel completed - MedicalCaseId={MedicalCaseId}", medicalCaseId);
                return (true, null);
            }
            else
            {
                _logger.LogWarning("[LC] MedicalCase.Cancel failed - MedicalCaseId={MedicalCaseId}", medicalCaseId);
                return (false, "取消医案失败");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[LC] MedicalCase.Cancel failed - MedicalCaseId={MedicalCaseId}", medicalCaseId);
            return (false, ClientErrorMessageMapper.GetSafeOperationFailureMessage("取消", ex));
        }
    }

    public virtual async Task<(bool success, string? errorMessage)> CompleteMedicalCaseAsync(Guid medicalCaseId, CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("[LC] MedicalCase.Complete started - MedicalCaseId={MedicalCaseId}", medicalCaseId);
            var request = new MedicalCaseStatusInputDto
            {
                Status = MedicalCaseStatus.Completed,
                StatusChangeReason = null
            };
            var data = await _repository.UpdateStatusAsync(medicalCaseId, request);

            if (data != null)
            {
                _logger.LogInformation("[LC] MedicalCase.Complete completed - MedicalCaseId={MedicalCaseId}", medicalCaseId);
                return (true, null);
            }
            else
            {
                _logger.LogWarning("[LC] MedicalCase.Complete failed - MedicalCaseId={MedicalCaseId}", medicalCaseId);
                return (false, "完成医案失败");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[LC] MedicalCase.Complete failed - MedicalCaseId={MedicalCaseId}", medicalCaseId);
            return (false, ClientErrorMessageMapper.GetSafeOperationFailureMessage("完成", ex));
        }
    }

    public virtual async Task<(bool success, string? errorMessage)> ResumeSuspendedAsync(Guid medicalCaseId, CancellationToken ct = default)
    {
        try
        {
            _logger.LogDebug("[LC] MedicalCase.ResumeSuspended started - MedicalCaseId={MedicalCaseId}", medicalCaseId);
            var request = new MedicalCaseStatusInputDto
            {
                Status = MedicalCaseStatus.Active,
                StatusChangeReason = null
            };
            var data = await _repository.UpdateStatusAsync(medicalCaseId, request);

            if (data != null)
            {
                _logger.LogInformation("[LC] MedicalCase.ResumeSuspended completed - MedicalCaseId={MedicalCaseId}", medicalCaseId);
                return (true, null);
            }
            else
            {
                _logger.LogWarning("[LC] MedicalCase.ResumeSuspended failed - MedicalCaseId={MedicalCaseId}", medicalCaseId);
                return (false, "恢复医案失败");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[LC] MedicalCase.ResumeSuspended failed - MedicalCaseId={MedicalCaseId}", medicalCaseId);
            return (false, ClientErrorMessageMapper.GetSafeOperationFailureMessage("恢复", ex));
        }
    }

    public virtual async Task<ApiResponse<MedicalCaseDetailDto>> CloseCaseAsync(Guid medicalCaseId, CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("[LC] MedicalCase.CloseCase started - MedicalCaseId={MedicalCaseId}", medicalCaseId);
            var data = await _repository.CloseCaseAsync(medicalCaseId);

            if (data != null)
            {
                _logger.LogInformation("[LC] MedicalCase.CloseCase completed - MedicalCaseId={MedicalCaseId}", medicalCaseId);
                return new ApiResponse<MedicalCaseDetailDto> { Success = true, Data = data };
            }
            else
            {
                _logger.LogWarning("[LC] MedicalCase.CloseCase failed - MedicalCaseId={MedicalCaseId}", medicalCaseId);
                return new ApiResponse<MedicalCaseDetailDto> { Success = false, Message = "关闭医案失败" };
            }
        }
        catch (Exception ex) { _logger.LogError(ex, "[LC] MedicalCase.CloseCase failed - MedicalCaseId={MedicalCaseId}", medicalCaseId); throw; }
    }

    #region 内部 API 调用

    private async Task<ApiResponse<MedicalCaseDetailDto>> SuspendViaApiAsync(Guid medicalCaseId)
    {
        try
        {
            var data = await _repository.SuspendAsync(medicalCaseId, null);
            if (data != null)
                return new ApiResponse<MedicalCaseDetailDto> { Success = true, Data = data };
            return new ApiResponse<MedicalCaseDetailDto> { Success = false, Message = "挂起医案失败" };
        }
        catch (Exception ex) { _logger.LogError(ex, "[LC] MedicalCase.SuspendViaApi failed - MedicalCaseId={MedicalCaseId}", medicalCaseId); throw; }
    }

    #endregion
}
