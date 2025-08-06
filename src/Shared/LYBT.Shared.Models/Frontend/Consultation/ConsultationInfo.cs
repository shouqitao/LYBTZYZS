using System;

namespace LYBT.Shared.Models.Frontend.Consultation
{
    /// <summary>
    /// 看诊前端模型（替代DiagnosisTreatmentInfo）
    /// </summary>
    public class ConsultationInfo
    {
        /// <summary>
        /// ID
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// 医疗案例ID
        /// </summary>
        public Guid MedicalCaseId { get; set; }

        /// <summary>
        /// 患者ID
        /// </summary>
        public Guid PatientId { get; set; }

        /// <summary>
        /// 患者姓名
        /// </summary>
        public string PatientName { get; set; } = string.Empty;

        /// <summary>
        /// 医生ID
        /// </summary>
        public Guid DoctorId { get; set; }

        /// <summary>
        /// 医生姓名
        /// </summary>
        public string DoctorName { get; set; } = string.Empty;

        /// <summary>
        /// 看诊时间
        /// </summary>
        public DateTime ConsultationTime { get; set; }

        /// <summary>
        /// 主诉
        /// </summary>
        public string ChiefComplaint { get; set; } = string.Empty;

        /// <summary>
        /// 现病史
        /// </summary>
        public string PresentIllness { get; set; } = string.Empty;

        /// <summary>
        /// 既往史
        /// </summary>
        public string PastHistory { get; set; } = string.Empty;

        /// <summary>
        /// 过敏史
        /// </summary>
        public string AllergyHistory { get; set; } = string.Empty;

        /// <summary>
        /// 体格检查
        /// </summary>
        public string PhysicalExamination { get; set; } = string.Empty;

        /// <summary>
        /// 舌诊
        /// </summary>
        public string TongueInspection { get; set; } = string.Empty;

        /// <summary>
        /// 脉诊
        /// </summary>
        public string PulseCondition { get; set; } = string.Empty;

        /// <summary>
        /// 中医诊断
        /// </summary>
        public string TCMDiagnosis { get; set; } = string.Empty;

        /// <summary>
        /// 西医诊断
        /// </summary>
        public string WesternDiagnosis { get; set; } = string.Empty;

        /// <summary>
        /// 诊断
        /// </summary>
        public string Diagnosis { get; set; } = string.Empty;

        /// <summary>
        /// 治疗原则
        /// </summary>
        public string TreatmentPrinciple { get; set; } = string.Empty;

        /// <summary>
        /// 医嘱
        /// </summary>
        public string MedicalAdvice { get; set; } = string.Empty;

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreateTime { get; set; }

        /// <summary>
        /// 更新时间
        /// </summary>
        public DateTime? UpdateTime { get; set; }

        /// <summary>
        /// 是否有效
        /// </summary>
        public bool IsActive { get; set; } = true;
    }
}