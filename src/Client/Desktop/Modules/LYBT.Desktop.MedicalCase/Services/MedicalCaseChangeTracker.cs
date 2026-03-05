using LYBT.Desktop.MedicalCase.Mappers;
using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Contracts.Prescriptions;

namespace LYBT.Desktop.MedicalCase.Services;

/// <summary>
/// Tracks changes to a MedicalCase aggregate by comparing current state against a deep-copied baseline.
/// Extracted from MedicalCaseService for SRP.
/// </summary>
public class MedicalCaseChangeTracker
{
    private readonly MedicalCaseCloneMapper _cloneMapper = new();
    private MedicalCaseDetailDto? _baseline;

    public void SetBaseline(MedicalCaseDetailDto snapshot)
        => _baseline = _cloneMapper.Clone(snapshot);

    public bool HasChanges(MedicalCaseDetailDto? current)
    {
        if (_baseline == null || current == null) return false;
        return IsMedicalCaseChanged(_baseline, current)
            || IsConsultationChanged(_baseline.Consultation, current.Consultation)
            || IsPrescriptionChanged(_baseline.Prescription, current.Prescription);
    }

    public void ClearBaseline() => _baseline = null;

    // Exact field comparison logic migrated from MedicalCaseService.cs lines 389-410
    private static bool IsMedicalCaseChanged(MedicalCaseDetailDto baseline, MedicalCaseDetailDto current)
        => baseline.CaseNumber != current.CaseNumber
        || baseline.PatientId != current.PatientId
        || baseline.UserId != current.UserId
        || baseline.CaseStatus != current.CaseStatus
        || baseline.Remark != current.Remark;

    private static bool IsConsultationChanged(ConsultationDetailDto? baseline, ConsultationDetailDto? current)
    {
        if (baseline == null && current == null) return false;
        if (baseline == null || current == null) return true;
        return baseline.PresentIllness != current.PresentIllness
            || baseline.TongueDiagnosis != current.TongueDiagnosis
            || baseline.PulseDiagnosis != current.PulseDiagnosis
            || baseline.TcmDiagnosis != current.TcmDiagnosis;
    }

    private static bool IsPrescriptionChanged(PrescriptionDetailDto? baseline, PrescriptionDetailDto? current)
    {
        if (baseline == null && current == null) return false;
        if (baseline == null || current == null) return true;
        return baseline.DosageCount != current.DosageCount
            || baseline.Usage != current.Usage
            || baseline.Discount != current.Discount
            || baseline.Advice != current.Advice
            || baseline.Remark != current.Remark;
    }
}
