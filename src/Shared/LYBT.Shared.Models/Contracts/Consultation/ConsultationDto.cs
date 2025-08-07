using System.ComponentModel;

namespace LYBT.Shared.Models.Contracts.Consultation
{
    /// <summary>
    /// 看诊信息DTO（列表用）
    /// </summary>
    public class ConsultationDto
    {
        /// <summary>看诊ID</summary>
        [DisplayName("看诊ID")]
        public Guid Id { get; set; }

        /// <summary>医疗案例ID</summary>
        [DisplayName("医疗案例ID")]
        public Guid MedicalCaseId { get; set; }

        /// <summary>患者ID</summary>
        [DisplayName("患者ID")]
        public Guid PatientId { get; set; }

        /// <summary>患者姓名</summary>
        [DisplayName("患者姓名")]
        public string PatientName { get; set; } = string.Empty;

        /// <summary>用户ID（医生）</summary>
        [DisplayName("用户ID")]
        public Guid UserId { get; set; }

        /// <summary>医生姓名</summary>
        [DisplayName("医生姓名")]
        public string DoctorName { get; set; } = string.Empty;

        /// <summary>诊断</summary>
        [DisplayName("诊断")]
        public string Diagnosis { get; set; } = string.Empty;

        /// <summary>看诊时间</summary>
        [DisplayName("看诊时间")]
        public DateTime ConsultationTime { get; set; }

        /// <summary>状态</summary>
        [DisplayName("状态")]
        public string Status { get; set; } = string.Empty;
    }
}