using LYBT.Models.DiagnosisTreatment;
using System.ComponentModel;

namespace LYBT.Models.Records {

    /// <summary>
    /// 病历实体模型
    /// </summary>
    public class RecordModel {

        /// <summary>记录ID</summary>
        [DisplayName("记录ID")]
/// <summary>
/// RecordId 属性。
/// </summary>
        public Guid RecordId { get; set; }

        /// <summary>患者ID</summary>
        [DisplayName("患者ID")]
/// <summary>
/// PatientId 属性。
/// </summary>
        public Guid PatientId { get; set; }

        /// <summary>医生ID</summary>
        [DisplayName("医生ID")]
/// <summary>
/// DoctorId 属性。
/// </summary>
        public Guid DoctorId { get; set; }

        /// <summary>主诉</summary>
        [DisplayName("主诉")]
/// <summary>
/// ChiefComplaint 属性。
/// </summary>
        public string ChiefComplaint { get; set; } = string.Empty;

        /// <summary>诊断文本</summary>
        [DisplayName("诊断文本")]
/// <summary>
/// DiagnosisText 属性。
/// </summary>
        public string DiagnosisText { get; set; } = string.Empty;

        /// <summary>处方摘要</summary>
        [DisplayName("处方摘要")]
/// <summary>
/// PrescriptionSummary 属性。
/// </summary>
        public string PrescriptionSummary { get; set; } = string.Empty;

        /// <summary>治疗摘要</summary>
        [DisplayName("治疗摘要")]
/// <summary>
/// TreatmentSummary 属性。
/// </summary>
        public string TreatmentSummary { get; set; } = string.Empty;

        /// <summary>是否共享</summary>
        [DisplayName("是否共享")]
/// <summary>
/// IsShared 属性。
/// </summary>
        public bool IsShared { get; set; }

        /// <summary>就诊时间</summary>
        [DisplayName("就诊时间")]
/// <summary>
/// VisitTime 属性。
/// </summary>
        public DateTime VisitTime { get; set; }

        /// <summary>主键ID</summary>
        [DisplayName("主键ID")]
/// <summary>
/// Id 属性。
/// </summary>
        public Guid Id { get; set; }

        /// <summary>病历记录时间</summary>
        [DisplayName("病历记录时间")]
/// <summary>
/// RecordTime 属性。
/// </summary>
        public DateTime RecordTime { get; set; }

        /// <summary>诊断</summary>
        [DisplayName("诊断")]
/// <summary>
/// Diagnosis 属性。
/// </summary>
        public string Diagnosis { get; set; } = string.Empty;

        /// <summary>现病史</summary>
        [DisplayName("现病史")]
/// <summary>
/// PresentIllness 属性。
/// </summary>
        public string? PresentIllness { get; set; }

        /// <summary>治疗建议</summary>
        [DisplayName("治疗建议")]
/// <summary>
/// TreatmentAdvice 属性。
/// </summary>
        public string? TreatmentAdvice { get; set; }

        /// <summary>处方ID</summary>
        [DisplayName("处方ID")]
/// <summary>
/// PrescriptionId 属性。
/// </summary>
        public Guid? PrescriptionId { get; set; }

        /// <summary>辩证结果列表</summary>
        [DisplayName("辩证结果列表")]
/// <summary>
/// DiagnosisResults 属性。
/// </summary>
        public List<string> DiagnosisResults { get; set; } = new();

        /// <summary>药材组成</summary>
        [DisplayName("药材组成")]
/// <summary>
/// HerbalFormula 属性。
/// </summary>
        public List<HerbItemModel>? HerbalFormula { get; set; }

        /// <summary>辅助治疗方案</summary>
        [DisplayName("辅助治疗方案")]
/// <summary>
/// TreatmentPlans 属性。
/// </summary>
        public List<TreatmentItemModel>? TreatmentPlans { get; set; }

        /// <summary>共享给医生ID列表</summary>
        [DisplayName("共享给医生ID列表")]
/// <summary>
/// SharedToDoctorIds 属性。
/// </summary>
        public List<string> SharedToDoctorIds { get; set; } = new();

        /// <summary>创建医生ID</summary>
        [DisplayName("创建医生ID")]
/// <summary>
/// CreatedBy 属性。
/// </summary>
        public string? CreatedBy { get; set; }

        /// <summary>创建时间</summary>
        [DisplayName("创建时间")]
/// <summary>
/// CreatedTime 属性。
/// </summary>
        public DateTime CreatedTime { get; set; } = DateTime.Now;
    }
}
