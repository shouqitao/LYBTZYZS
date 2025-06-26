using LYBT.Models.DiagnosisTreatment;

namespace LYBT.Models.Records {

    /// <summary>
    /// 病历实体模型
    /// </summary>
    public class RecordModel {

        /// <summary>记录ID</summary>
        public Guid RecordId { get; set; }

        /// <summary>患者ID</summary>
        public Guid PatientId { get; set; }

        /// <summary>医生ID</summary>
        public Guid DoctorId { get; set; }

        /// <summary>主诉</summary>
        public string ChiefComplaint { get; set; } = string.Empty;

        /// <summary>诊断文本</summary>
        public string DiagnosisText { get; set; } = string.Empty;

        /// <summary>处方摘要</summary>
        public string PrescriptionSummary { get; set; } = string.Empty;

        /// <summary>治疗摘要</summary>
        public string TreatmentSummary { get; set; } = string.Empty;

        /// <summary>是否共享</summary>
        public bool IsShared { get; set; }

        /// <summary>就诊时间</summary>
        public DateTime VisitTime { get; set; }

        /// <summary>主键ID</summary>
        public Guid Id { get; set; }

        /// <summary>病历记录时间</summary>
        public DateTime RecordTime { get; set; }

        /// <summary>诊断</summary>
        public string Diagnosis { get; set; } = string.Empty;

        /// <summary>现病史</summary>
        public string? PresentIllness { get; set; }

        /// <summary>治疗建议</summary>
        public string? TreatmentAdvice { get; set; }

        /// <summary>处方ID</summary>
        public Guid? PrescriptionId { get; set; }

        /// <summary>辩证结果列表</summary>
        public List<string> DiagnosisResults { get; set; } = new();

        /// <summary>药材组成</summary>
        public List<HerbItemModel>? HerbalFormula { get; set; }

        /// <summary>辅助治疗方案</summary>
        public List<TreatmentItemModel>? TreatmentPlans { get; set; }

        /// <summary>共享给医生ID列表</summary>
        public List<string> SharedToDoctorIds { get; set; } = new();

        /// <summary>创建医生ID</summary>
        public string? CreatedBy { get; set; }

        /// <summary>创建时间</summary>
        public DateTime CreatedTime { get; set; } = DateTime.Now;
    }
}