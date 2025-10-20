using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Shared.Models.Contracts.Prescriptions;

namespace LYBT.Desktop.MedicalCase.Models
{
    /// <summary>
    /// 医案流程草稿状态（Issue #1502 - 自动保存草稿功能）
    /// </summary>
    public class FlowDraftState
    {
        /// <summary>
        /// 当前流程步骤
        /// </summary>
        public FlowStep CurrentStep { get; set; }

        /// <summary>
        /// 患者ID
        /// </summary>
        public Guid? PatientId { get; set; }

        /// <summary>
        /// 医案ID
        /// </summary>
        public Guid? MedicalCaseId { get; set; }

        /// <summary>
        /// 诊断表单数据
        /// </summary>
        public ConsultationFormData? Consultation { get; set; }

        /// <summary>
        /// 处方明细列表
        /// </summary>
        public List<PrescriptionItemDto>? PrescriptionItems { get; set; }

        /// <summary>
        /// 草稿保存时间
        /// </summary>
        public DateTime SavedAt { get; set; }
    }

    /// <summary>
    /// 诊断表单数据（用于草稿）
    /// </summary>
    public class ConsultationFormData
    {
        /// <summary>主诉</summary>
        public string ChiefComplaint { get; set; } = string.Empty;

        /// <summary>现病史</summary>
        public string PresentIllness { get; set; } = string.Empty;

        /// <summary>中医诊断</summary>
        public string TCMDiagnosis { get; set; } = string.Empty;

        /// <summary>治疗原则</summary>
        public string TreatmentPrinciple { get; set; } = string.Empty;

        /// <summary>望诊</summary>
        public string Inspection { get; set; } = string.Empty;

        /// <summary>闻诊</summary>
        public string AuscultationOlfaction { get; set; } = string.Empty;

        /// <summary>问诊</summary>
        public string Inquiry { get; set; } = string.Empty;

        /// <summary>切诊</summary>
        public string Palpation { get; set; } = string.Empty;

        /// <summary>备注</summary>
        public string Remarks { get; set; } = string.Empty;
    }
}
