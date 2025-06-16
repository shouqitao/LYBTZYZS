using System;

namespace LYBT.Models.Records {
    /// <summary>
    /// 病历实体模型
    /// </summary>
    public class RecordModel {
        public Guid RecordId { get; set; }
        public Guid PatientId { get; set; }
        public Guid DoctorId { get; set; }
        public string ChiefComplaint { get; set; } = string.Empty;
        public string DiagnosisText { get; set; } = string.Empty;
        public string PrescriptionSummary { get; set; } = string.Empty; // 处方摘要
        public string TreatmentSummary { get; set; } = string.Empty;  // 治疗摘要
        public bool IsShared { get; set; }               // 是否共享
        public DateTime VisitTime { get; set; }
        public Guid Id { get; set; }
        public DateTime RecordTime { get; set; }
        public string Diagnosis { get; set; } = string.Empty;
        public string? PresentIllness { get; set; }
        public string? TreatmentAdvice { get; set; }
        public Guid? PrescriptionId { get; set; }
    }
}
