using System;
using LYBT.Domain.Aggregates.ConsultationAggregate;
using LYBT.Domain.Aggregates.MedicalCaseAggregate;
using LYBT.Domain.ValueObjects;

namespace LYBT.Application.Commands
{
    #region 创建看诊命令

    /// <summary>
    /// 创建看诊命令
    /// </summary>
    public class CreateConsultationCommand
    {
        public Guid PatientId { get; set; }
        public string PatientName { get; set; }
        public Guid DoctorId { get; set; }
        public string DoctorName { get; set; }
        public DateTime? AppointmentTime { get; set; }
        public ConsultationType ConsultationType { get; set; }
        public string ChiefComplaint { get; set; }
        public int? Duration { get; set; }
        public Guid? MedicalCaseId { get; set; }
        public bool IsEmergency { get; set; }
    }

    #endregion

    #region 四诊记录命令

    /// <summary>
    /// 记录四诊信息命令
    /// </summary>
    public class RecordFourDiagnosisCommand
    {
        public Guid ConsultationId { get; set; }
        public Guid? MedicalCaseId { get; set; }
        public InspectionCommand Inspection { get; set; }
        public AuscultationOlfactionCommand AuscultationOlfaction { get; set; }
        public InquiryCommand Inquiry { get; set; }
        public PalpationCommand Palpation { get; set; }
    }

    /// <summary>
    /// 望诊命令
    /// </summary>
    public class InspectionCommand
    {
        public Complexion Complexion { get; set; }
        public Spirit Spirit { get; set; }
        public BodyShape BodyShape { get; set; }
        public TongueCondition TongueCondition { get; set; }
        public string Observations { get; set; }
    }

    /// <summary>
    /// 闻诊命令
    /// </summary>
    public class AuscultationOlfactionCommand
    {
        public Voice Voice { get; set; }
        public Breathing Breathing { get; set; }
        public Cough Cough { get; set; }
        public Odor Odor { get; set; }
        public string Notes { get; set; }
    }

    /// <summary>
    /// 问诊命令
    /// </summary>
    public class InquiryCommand
    {
        public ColdHeat ColdHeat { get; set; }
        public Perspiration Perspiration { get; set; }
        public string HeadBody { get; set; }
        public string Stool { get; set; }
        public string Urine { get; set; }
        public string Appetite { get; set; }
        public string ChestAbdomen { get; set; }
        public Sleep Sleep { get; set; }
        public string Menstruation { get; set; }
        public string OtherSymptoms { get; set; }
    }

    /// <summary>
    /// 切诊命令
    /// </summary>
    public class PalpationCommand
    {
        public PulseCondition PulseCondition { get; set; }
        public string AbdominalPalpation { get; set; }
        public string Notes { get; set; }
    }

    #endregion

    #region 诊断命令

    /// <summary>
    /// 设置中医诊断命令
    /// </summary>
    public class SetTCMDiagnosisCommand
    {
        public Guid ConsultationId { get; set; }
        public Guid? MedicalCaseId { get; set; }
        public string TCMDisease { get; set; }
        public TCMSyndrome TCMSyndrome { get; set; }
        public string SyndromeAnalysis { get; set; }
        public TreatmentPrinciple TreatmentPrinciple { get; set; }
        public string DiseaseCode { get; set; }
    }

    #endregion

    #region 完成看诊命令

    /// <summary>
    /// 完成看诊命令
    /// </summary>
    public class CompleteConsultationCommand
    {
        public Guid ConsultationId { get; set; }
        public Guid? MedicalCaseId { get; set; }
        public Guid? PrescriptionId { get; set; }
        public string PrescriptionNo { get; set; }
        public Money PrescriptionAmount { get; set; }
        public string DoctorAdvice { get; set; }
        public DateTime? NextAppointmentDate { get; set; }
        public string FollowUpNotes { get; set; }
        public string Summary { get; set; }
    }

    #endregion
}