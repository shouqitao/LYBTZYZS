using LYBT.Entities.Consultations;
using LYBT.Entities.MedicalCases;
using LYBT.Entities.Prescriptions;
using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Contracts.Prescriptions;

namespace LYBT.Module.MedicalCases.Application.Mappers;

public static class MedicalCaseMapper
{
    public static MedicalCaseListDto ToListDto(MedicalCase entity) => new()
    {
        Id = entity.Id,
        PatientId = entity.PatientId,
        PatientName = entity.PatientName,
        UserId = entity.UserId,
        DoctorName = entity.DoctorName,
        CaseNumber = entity.CaseNumber,
        CaseStatus = entity.CaseStatus,
        NeedsPrescription = entity.NeedsPrescription,
        CompletedAt = entity.CompletedAt,
        HasConsultation = entity.Consultation != null,
        HasPrescription = entity.Prescription != null && !entity.Prescription.IsDeleted,
        CreatedAt = entity.CreatedAt
    };

    public static MedicalCaseDetailDto ToDetailDto(MedicalCase entity) => new()
    {
        Id = entity.Id,
        PatientId = entity.PatientId,
        PatientName = entity.PatientName,
        UserId = entity.UserId,
        DoctorName = entity.DoctorName,
        CaseNumber = entity.CaseNumber,
        CaseStatus = entity.CaseStatus,
        NeedsPrescription = entity.NeedsPrescription,
        CompletedAt = entity.CompletedAt,
        CreatedAt = entity.CreatedAt,
        UpdatedAt = entity.UpdatedAt,
        CreatedBy = entity.CreatedBy,
        ConsultationId = entity.Consultation != null ? entity.Id : null,
        PrescriptionId = entity.Prescription != null && !entity.Prescription.IsDeleted ? entity.Prescription.Id : null,
        Diagnosis = entity.Consultation?.TcmDiagnosis,
        PresentIllness = entity.Consultation?.PresentIllness,
        Consultation = entity.Consultation != null ? MapConsultation(entity) : null,
        Prescription = entity.Prescription != null && !entity.Prescription.IsDeleted ? MapPrescription(entity) : null
    };

    private static ConsultationDetailDto MapConsultation(MedicalCase entity)
    {
        var c = entity.Consultation!;
        return new ConsultationDetailDto
        {
            MedicalCaseId = entity.Id,
            PatientId = entity.PatientId,
            UserId = entity.UserId,
            PatientName = entity.PatientName,
            DoctorName = entity.DoctorName,
            PresentIllness = c.PresentIllness,
            TongueDiagnosis = c.TongueDiagnosis,
            PulseDiagnosis = c.PulseDiagnosis,
            TcmDiagnosis = c.TcmDiagnosis
        };
    }

    private static PrescriptionDetailDto MapPrescription(MedicalCase entity)
    {
        var p = entity.Prescription!;
        return new PrescriptionDetailDto
        {
            Id = p.Id,
            MedicalCaseId = entity.Id,
            PrescriptionNumber = p.PrescriptionNumber,
            DosageCount = p.DosageCount,
            Discount = p.Discount,
            Usage = p.Usage,
            Advice = p.Advice,
            ReferencedFormulas = p.ReferencedFormulas,
            Remark = p.Remark,
            Items = p.Items.Select(MapPrescriptionItem).ToList(),
            SingleDosePrice = p.Items.Sum(x => x.Amount),
            TotalPrice = p.Items.Sum(x => x.Amount) * p.DosageCount * p.Discount,
            TotalWeight = p.Items.Sum(x => x.Dosage)
        };
    }

    private static PrescriptionItemDto MapPrescriptionItem(PrescriptionItem item) => new()
    {
        Id = item.Id,
        PrescriptionId = item.PrescriptionId,
        HerbId = item.HerbId,
        HerbName = item.HerbName,
        Dosage = item.Dosage,
        Unit = item.Unit,
        DecocteMethod = item.DecocteMethod,
        UnitPrice = item.UnitPrice,
        Usage = item.Usage,
        Remark = item.Remark
    };
}


