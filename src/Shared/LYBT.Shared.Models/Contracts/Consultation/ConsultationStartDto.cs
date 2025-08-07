using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace LYBT.Shared.Models.Contracts.Consultation
{
    /// <summary>
    /// 开始看诊DTO
    /// </summary>
    public class ConsultationStartDto
    {
        /// <summary>医疗案例ID</summary>
        [Required]
        [DisplayName("医疗案例ID")]
        public Guid MedicalCaseId { get; set; }

        /// <summary>患者ID</summary>
        [Required]
        [DisplayName("患者ID")]
        public Guid PatientId { get; set; }

        /// <summary>用户ID（医生）</summary>
        [Required]
        [DisplayName("用户ID")]
        public Guid UserId { get; set; }

        /// <summary>挂号ID</summary>
        [DisplayName("挂号ID")]
        public Guid? RegistrationId { get; set; }

        /// <summary>备注</summary>
        [DisplayName("备注")]
        public string? Remark { get; set; }
    }
}