using LYBT.Entities.MedicalCases;
using LYBT.Shared.Models.Contracts.MedicalCase;

namespace LYBT.Module.MedicalCases.Services;

public partial class MedicalCaseCommandService
{
    private void ValidateEditReason(MedicalCase medicalCase, MedicalCaseInputDto request, Guid currentUserId)
        => _stateGuard.EnsureCanEdit(medicalCase, request.EditReason, currentUserId);

    private static Dictionary<string, string?> CaptureSnapshot(MedicalCase medicalCase)
        => new()
        {
            ["PresentIllness"] = medicalCase.Consultation?.PresentIllness,
            ["TongueDiagnosis"] = medicalCase.Consultation?.TongueDiagnosis,
            ["PulseDiagnosis"] = medicalCase.Consultation?.PulseDiagnosis,
            ["TcmDiagnosis"] = medicalCase.Consultation?.TcmDiagnosis,
            ["PrescriptionItems"] = medicalCase.Prescription?.Items?.Count.ToString() ?? "0"
        };

    private async Task WriteUpdateAuditAsync(
        MedicalCase medicalCase,
        MedicalCaseInputDto request,
        Guid currentUserId,
        Dictionary<string, string?> before,
        CancellationToken cancellationToken)
    {
        var changed = new Dictionary<string, (string? Old, string? New)>();
        void Compare(string field, string? oldVal, string? newVal)
        {
            if (!string.Equals(oldVal, newVal))
                changed[field] = (oldVal, newVal);
        }
        Compare("PresentIllness", before["PresentIllness"], medicalCase.Consultation?.PresentIllness);
        Compare("TongueDiagnosis", before["TongueDiagnosis"], medicalCase.Consultation?.TongueDiagnosis);
        Compare("PulseDiagnosis", before["PulseDiagnosis"], medicalCase.Consultation?.PulseDiagnosis);
        Compare("TcmDiagnosis", before["TcmDiagnosis"], medicalCase.Consultation?.TcmDiagnosis);
        Compare("PrescriptionItems", before["PrescriptionItems"], medicalCase.Prescription?.Items?.Count.ToString() ?? "0");
        if (changed.Count == 0)
            return;
        var operatorInfo = await _userCrossModule.GetUserBasicInfoAsync(currentUserId, cancellationToken);
        await _repository.AddAuditLogAsync(new MedicalCaseAuditLog
        {
            Id = Guid.NewGuid(),
            MedicalCaseId = medicalCase.Id,
            CreatedBy = currentUserId,
            OperatorId = currentUserId,
            OperatorName = operatorInfo?.UserName ?? string.Empty,
            OperatorRole = operatorInfo?.Role != null ? (int)operatorInfo.Role : 0,
            OperationType = 1,
            Reason = request.EditReason,
            ChangedFields = string.Join(",", changed.Keys),
            OldValues = System.Text.Json.JsonSerializer.Serialize(changed.ToDictionary(k => k.Key, v => v.Value.Old)),
            NewValues = System.Text.Json.JsonSerializer.Serialize(changed.ToDictionary(k => k.Key, v => v.Value.New)),
            CreatedAt = DateTime.UtcNow
        }, saveChanges: false, cancellationToken);
    }

    private void ValidateEditPermission(MedicalCase medicalCase, Guid currentUserId, bool isAdmin)
        => MedicalCaseServiceHelper.EnsureCanEdit(medicalCase, currentUserId, isAdmin, "Save", _logger);

    private static void UpdateMedicalCaseBasicFields(MedicalCase medicalCase, MedicalCaseInputDto request)
    {
        medicalCase.UpdatedAt = DateTime.UtcNow;
    }

    private static void UpdateConsultationFields(LYBT.Entities.Consultations.Consultation consultation, LYBT.Shared.Models.Contracts.Consultation.ConsultationInputDto dto)
    {
        consultation.PresentIllness = dto.PresentIllness;
        consultation.TongueDiagnosis = dto.TongueDiagnosis;
        consultation.PulseDiagnosis = dto.PulseDiagnosis;
        consultation.TcmDiagnosis = dto.TcmDiagnosis;
        consultation.UpdatedAt = DateTime.UtcNow;
    }
}
