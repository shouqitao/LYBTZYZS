using System;

namespace LYBT.Models.Records {
    /// <summary>
    /// 病历实体模型
    /// </summary>
    public class RecordModel {
        public string RecordId { get; set; }
        public string PatientId { get; set; }
        public string DoctorId { get; set; }
        public string ChiefComplaint { get; set; }
        public string DiagnosisText { get; set; }
        public string PrescriptionSummary { get; set; }  // 处方摘要
        public string TreatmentSummary { get; set; }     // 治疗摘要
        public bool IsShared { get; set; }               // 是否共享
        public DateTime VisitTime { get; set; }
        public Guid Id { get; set; }
        public DateTime RecordTime { get; set; }
        public string Diagnosis { get; set; }
        public string? PresentIllness { get; set; }
        public string? TreatmentAdvice { get; set; }
        public Guid? PrescriptionId { get; set; }
    }
}
