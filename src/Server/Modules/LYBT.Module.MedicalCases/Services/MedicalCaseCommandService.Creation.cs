using FluentValidation;
using LYBT.Entities.Consultations;
using LYBT.Entities.MedicalCases;
using LYBT.Entities.Prescriptions;
using LYBT.Shared.ExceptionHandling.Exceptions;
using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Enums;
using LYBT.Shared.Models.Primitives.ErrorCodes;
using Microsoft.Extensions.Logging;

namespace LYBT.Module.MedicalCases.Services;

public partial class MedicalCaseCommandService
{
    private async Task<MedicalCase?> CreateFromInputDtoAsync(
        MedicalCaseInputDto request,
        Guid currentUserId,
        bool isAdmin = false,
        CancellationToken cancellationToken = default)
    {
        if (request.UserId == Guid.Empty)
            request.UserId = currentUserId;
        var doctorId = request.UserId;
        await _inputValidator.ValidateAndThrowAsync(request, cancellationToken);
        _logger.LogInformation("[SVC] MedicalCase.CreateFromInput started - PatientId={PatientId} UserId={UserId}", request.PatientId, doctorId);
        var (patient, doctor) = await MedicalCaseServiceHelper.ValidateAndFetchCreationContextAsync(
            request.PatientId, doctorId, _patientCrossModule, _userCrossModule, _repository, _logger, cancellationToken);
        var medicalCase = new MedicalCase
        {
            Id = Guid.NewGuid(),
            CaseNumber = await GenerateCaseNumberAsync(cancellationToken),
            PatientId = request.PatientId,
            PatientName = patient.Name,
            CaseStatus = MedicalCaseStatus.Active,
            NeedsPrescription = request.Prescription?.NeedsPrescription,
            UserId = doctorId,
            DoctorName = doctor.RealName,
            CreatedBy = currentUserId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        var consultation = new Consultation
        {
            Id = medicalCase.Id,
            CreatedBy = currentUserId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        if (request.Consultation != null)
        {
            await _consultationValidator.ValidateAndThrowAsync(request.Consultation, cancellationToken);
            if (string.IsNullOrWhiteSpace(request.Consultation.TcmDiagnosis))
            {
                _logger.LogInformation("[SVC] MedicalCase.Create -> TcmDiagnosisEmpty");
                throw new BusinessException(ErrorCode.MedicalCaseMissingDiagnosis, "中医诊断不能为空");
            }
            consultation.PresentIllness = request.Consultation.PresentIllness;
            consultation.TongueDiagnosis = request.Consultation.TongueDiagnosis;
            consultation.PulseDiagnosis = request.Consultation.PulseDiagnosis;
            consultation.TcmDiagnosis = request.Consultation.TcmDiagnosis;
        }
        medicalCase.Consultation = consultation;
        if (request.Prescription != null && request.Prescription.NeedsPrescription)
        {
            await _itemService.CreateNewPrescriptionAsync(medicalCase, request.Prescription, currentUserId, cancellationToken);
        }
        var result = await _repository.AddAsync(medicalCase, cancellationToken);
        if (request.RegistrationId.HasValue)
        {
            await _registrationCrossModule.LinkRegistrationToMedicalCaseAsync(request.RegistrationId.Value, result.Id, cancellationToken);
            _logger.LogInformation("[SVC] MedicalCase.CreateFromInput -> Registration linked - RegistrationId={RegistrationId}, MedicalCaseId={MedicalCaseId}", request.RegistrationId.Value, result.Id);
        }
        await _cacheInvalidation.InvalidateAsync("medicalcases", cancellationToken);
        return result;
    }

    private async Task<string> GenerateCaseNumberAsync(CancellationToken cancellationToken = default)
    {
        var today = DateTime.Today;
        var dateStr = today.ToString("yyyyMMdd");
        var prefix = $"{LYBT.Shared.Configuration.Options.Common.MedicalCaseNumberOptions.DefaultPrefix}{dateStr}";
        var count = await _repository.CountByPrefixAsync(prefix, cancellationToken);
        return $"{prefix}{(count + 1).ToString($"D{LYBT.Shared.Configuration.Options.Common.MedicalCaseNumberOptions.DefaultPadLength}")}";
    }
}
