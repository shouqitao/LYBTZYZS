using LYBT.Desktop.Contracts.Repositories;
using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Foundation.ExceptionHandling;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Extensions;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.MedicalCase.Services;

/// <summary>
/// 医案命令服务 - 写操作 + 变更检测
/// 从 MedicalCaseService 拆分，实现 IMedicalCaseCommandService
/// </summary>
internal class MedicalCaseCommandService : IMedicalCaseCommandService
{
    private readonly IMedicalCaseRepository _repository;
    private readonly ISessionManager? _sessionManager;
    private readonly ILogger<MedicalCaseCommandService> _logger;
    private readonly MedicalCaseEditContext _context;

    public MedicalCaseCommandService(
        IMedicalCaseRepository repository,
        MedicalCaseEditContext context,
        ILogger<MedicalCaseCommandService> logger,
        ISessionManager? sessionManager = null)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _sessionManager = sessionManager;
    }

    public MedicalCaseDetailDto? Current => _context.CurrentDetail;

    public bool HasChanges => _context.CurrentDetail != null && _context.OriginalDetail != null &&
        (IsMedicalCaseChanged() || IsConsultationChanged() || IsPrescriptionChanged());

    public virtual async Task<bool> SaveAsync(CancellationToken ct = default)
    {
        if (_context.CurrentDetail == null) { _logger.LogWarning("[CMD] MedicalCase.Save → NoData"); return false; }
        if (!HasChanges) { _logger.LogDebug("[CMD] MedicalCase.Save → NoChanges - MedicalCaseId={MedicalCaseId}", _context.CurrentDetail.Id); return true; }

        try
        {
            _logger.LogInformation("[CMD] MedicalCase.Save started - MedicalCaseId={MedicalCaseId}", _context.CurrentDetail.Id);
            var inputDto = _context.CurrentDetail.ToInputDto();
            var updated = await _repository.SaveAsync(_context.CurrentDetail.Id, inputDto);
            if (updated != null)
            {
                UpdateMedicalCaseFields(_context.CurrentDetail, updated);
                if (updated.Consultation != null) _context.CurrentDetail.Consultation = updated.Consultation;
                if (updated.Prescription != null) _context.CurrentDetail.Prescription = updated.Prescription;
            }
            _context.UpdateOriginal();
            _logger.LogInformation("[CMD] MedicalCase.Save completed - MedicalCaseId={MedicalCaseId}", _context.CurrentDetail.Id);
            return true;
        }
        catch (Exception ex) { _logger.LogError(ex, "[CMD] MedicalCase.Save failed - MedicalCaseId={MedicalCaseId}", _context.CurrentDetail.Id); return false; }
    }

    public virtual async Task<bool> DeleteAsync(CancellationToken ct = default)
    {
        if (_context.CurrentDetail == null) { _logger.LogWarning("[CMD] MedicalCase.Delete → NoData"); return false; }
        try
        {
            _logger.LogInformation("[CMD] MedicalCase.Delete started - MedicalCaseId={MedicalCaseId}", _context.CurrentDetail.Id);
            await _repository.DeleteAsync(_context.CurrentDetail.Id);
            _logger.LogInformation("[CMD] MedicalCase.Delete completed - MedicalCaseId={MedicalCaseId}", _context.CurrentDetail.Id);
            _context.Clear();
            return true;
        }
        catch (Exception ex) { _logger.LogError(ex, "[CMD] MedicalCase.Delete failed - MedicalCaseId={MedicalCaseId}", _context.CurrentDetail?.Id ?? Guid.Empty); return false; }
    }

    public virtual async Task<(bool success, Guid medicalCaseId, string? errorMessage)> CreateMedicalCaseAsync(Guid patientId, Guid? registrationId = null, CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("[CMD] MedicalCase.CreateNew started - PatientId={PatientId} RegistrationId={RegistrationId}", patientId, registrationId);

            if (_sessionManager == null)
            {
                _logger.LogWarning("[CMD] MedicalCase.CreateNew → NullSessionManager");
                return (false, Guid.Empty, "会话管理器未初始化，无法创建医案");
            }
            if (_sessionManager.CurrentUser == null)
            {
                _logger.LogWarning("[CMD] MedicalCase.CreateNew → NullCurrentUser");
                return (false, Guid.Empty, "用户信息丢失，无法创建医案");
            }

            var createDto = new MedicalCaseInputDto
            {
                Id = null,
                PatientId = patientId,
                UserId = _sessionManager.CurrentUser.Id,
                RegistrationId = registrationId
            };
            var createdDto = await _repository.CreateAsync(createDto);
            if (createdDto == null)
            {
                _logger.LogWarning("[CMD] MedicalCase.CreateNew → NullResult");
                return (false, Guid.Empty, "创建医案失败：服务返回空结果");
            }

            _logger.LogInformation("[CMD] MedicalCase.CreateNew completed - MedicalCaseId={MedicalCaseId}", createdDto.Id);
            return (true, createdDto.Id, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[CMD] MedicalCase.CreateNew failed - PatientId={PatientId}", patientId);
            return (false, Guid.Empty, ClientErrorMessageMapper.GetSafeOperationFailureMessage("创建医案", ex));
        }
    }

    #region 变更检测

    private bool IsMedicalCaseChanged() => _context.CurrentDetail != null && _context.OriginalDetail != null &&
        (_context.CurrentDetail.CaseNumber != _context.OriginalDetail.CaseNumber ||
         _context.CurrentDetail.PatientId != _context.OriginalDetail.PatientId ||
         _context.CurrentDetail.UserId != _context.OriginalDetail.UserId ||
         _context.CurrentDetail.CaseStatus != _context.OriginalDetail.CaseStatus);

    private bool IsConsultationChanged()
    {
        if (_context.CurrentDetail?.Consultation == null || _context.OriginalDetail?.Consultation == null) return false;
        var c = _context.CurrentDetail.Consultation; var o = _context.OriginalDetail.Consultation;
        return c.PresentIllness != o.PresentIllness ||
               c.TongueDiagnosis != o.TongueDiagnosis || c.PulseDiagnosis != o.PulseDiagnosis ||
               c.TcmDiagnosis != o.TcmDiagnosis;
    }

    private bool IsPrescriptionChanged()
    {
        if (_context.CurrentDetail?.Prescription == null || _context.OriginalDetail?.Prescription == null) return false;
        var c = _context.CurrentDetail.Prescription; var o = _context.OriginalDetail.Prescription;
        return c.DosageCount != o.DosageCount || c.Usage != o.Usage ||
               c.Discount != o.Discount || c.Advice != o.Advice || c.Remark != o.Remark;
    }

    private static void UpdateMedicalCaseFields(MedicalCaseDetailDto target, MedicalCaseDetailDto source)
    {
        target.CaseNumber = source.CaseNumber;
        target.PatientId = source.PatientId; target.PatientName = source.PatientName;
        target.PatientGender = source.PatientGender; target.PatientAge = source.PatientAge;
        target.UserId = source.UserId; target.DoctorName = source.DoctorName;
        target.ConsultationId = source.ConsultationId; target.PrescriptionId = source.PrescriptionId;
        target.CaseStatus = source.CaseStatus;
        target.UpdatedAt = source.UpdatedAt;
    }

    #endregion
}
