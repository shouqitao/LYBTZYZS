using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LYBT.Models.Consultation
{
    /// <summary>
    /// 看诊实体 - 替代原DiagnosisTreatmentModel
    /// </summary>
    [Table("Consultations")]
    public class ConsultationModel
    {
        /// <summary>看诊ID</summary>
        [Key]
        [DisplayName("看诊ID")]
        public Guid Id { get; set; }

        /// <summary>医疗案例ID</summary>
        [Required]
        [DisplayName("医疗案例ID")]
        public Guid MedicalCaseId { get; set; }

        /// <summary>患者ID</summary>
        [Required]
        [DisplayName("患者ID")]
        public Guid PatientId { get; set; }

        /// <summary>医生ID</summary>
        [Required]
        [DisplayName("医生ID")]
        public Guid DoctorId { get; set; }

        /// <summary>主诉</summary>
        [StringLength(500)]
        [DisplayName("主诉")]
        public string? ChiefComplaint { get; set; }

        /// <summary>现病史</summary>
        [StringLength(1000)]
        [DisplayName("现病史")]
        public string? PresentIllness { get; set; }

        /// <summary>既往史</summary>
        [StringLength(500)]
        [DisplayName("既往史")]
        public string? PastHistory { get; set; }

        /// <summary>过敏史</summary>
        [StringLength(200)]
        [DisplayName("过敏史")]
        public string? AllergyHistory { get; set; }

        /// <summary>体格检查</summary>
        [StringLength(1000)]
        [DisplayName("体格检查")]
        public string? PhysicalExamination { get; set; }

        /// <summary>舌诊</summary>
        [StringLength(200)]
        [DisplayName("舌诊")]
        public string? TongueInspection { get; set; }

        /// <summary>脉诊</summary>
        [StringLength(200)]
        [DisplayName("脉诊")]
        public string? PulseCondition { get; set; }

        /// <summary>中医辨证</summary>
        [StringLength(500)]
        [DisplayName("中医辨证")]
        public string? TCMDiagnosis { get; set; }

        /// <summary>西医诊断</summary>
        [StringLength(500)]
        [DisplayName("西医诊断")]
        public string? WesternDiagnosis { get; set; }

        /// <summary>诊断（综合）</summary>
        [Required]
        [StringLength(500)]
        [DisplayName("诊断")]
        public string Diagnosis { get; set; } = string.Empty;

        /// <summary>诊断类型ID（原DiagnosisCatalogId）</summary>
        [DisplayName("诊断类型ID")]
        public Guid? DiagnosisCatalogId { get; set; }

        /// <summary>治疗原则</summary>
        [StringLength(200)]
        [DisplayName("治疗原则")]
        public string? TreatmentPrinciple { get; set; }

        /// <summary>医嘱</summary>
        [StringLength(500)]
        [DisplayName("医嘱")]
        public string? MedicalAdvice { get; set; }

        /// <summary>看诊时间</summary>
        [DisplayName("看诊时间")]
        public DateTime ConsultationTime { get; set; } = DateTime.Now;

        /// <summary>看诊时长（分钟）</summary>
        [DisplayName("看诊时长")]
        public int? Duration { get; set; }

        /// <summary>创建时间</summary>
        [DisplayName("创建时间")]
        public DateTime CreateTime { get; set; } = DateTime.Now;

        /// <summary>更新时间</summary>
        [DisplayName("更新时间")]
        public DateTime? UpdateTime { get; set; }

        /// <summary>是否有效</summary>
        [DisplayName("是否有效")]
        public bool IsActive { get; set; } = true;
    }
}