using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using LYBT.Entities.Patients;
using LYBT.Entities.Users;
using LYBT.Shared.Models.Enums;

namespace LYBT.Entities.Consultation
{

    /// <summary>
    /// 诊疗实体 - UltraThink v2.0架构简化版
    /// 合并了原BaseConsultation和ConsultationModel
    /// 专注于中医诊疗，包含中医四诊和辨证论治
    /// </summary>
    [Table("Consultations")]
    public class Consultation
    {

        /// <summary>诊疗ID</summary>
        [Key]
        [DisplayName("诊疗ID")]
        public Guid Id { get; set; }

        /// <summary>医疗案例ID</summary>
        [Required]
        [DisplayName("医疗案例ID")]
        public Guid MedicalCaseId { get; set; }

        /// <summary>患者ID</summary>
        [Required]
        [DisplayName("患者ID")]
        public Guid PatientId { get; set; }

        /// <summary>关联用户ID（医生）</summary>
        [Required]
        [DisplayName("关联用户ID")]
        public Guid UserId { get; set; }

        /// <summary>创建人ID（医生用户ID）</summary>
        [Required]
        [DisplayName("创建人")]
        public Guid CreatedBy { get; set; }

        /// <summary>主诉</summary>
        [StringLength(500)]
        [DisplayName("主诉")]
        public string? ChiefComplaint { get; set; }

        /// <summary>现病史</summary>
        [StringLength(1000)]
        [DisplayName("现病史")]
        public string? PresentIllness { get; set; }

        // 中医四诊

        /// <summary>望诊</summary>
        [StringLength(500)]
        [DisplayName("望诊")]
        public string? Inspection { get; set; }

        /// <summary>闻诊</summary>
        [StringLength(500)]
        [DisplayName("闻诊")]
        public string? AuscultationOlfaction { get; set; }

        /// <summary>问诊</summary>
        [StringLength(500)]
        [DisplayName("问诊")]
        public string? Inquiry { get; set; }

        /// <summary>切诊（包含脉诊、舌诊等）</summary>
        [StringLength(500)]
        [DisplayName("切诊")]
        public string? Palpation { get; set; }

        // 中医诊断结果

        /// <summary>中医辨证</summary>
        [Required]
        [StringLength(500)]
        [DisplayName("中医辨证")]
        public string TCMDiagnosis { get; set; } = string.Empty;

        /// <summary>治疗原则</summary>
        [StringLength(500)]
        [DisplayName("治疗原则")]
        public string? TreatmentPrinciple { get; set; }

        /// <summary>医嘱</summary>
        [StringLength(1000)]
        [DisplayName("医嘱")]
        public string? MedicalAdvice { get; set; }

        /// <summary>状态</summary>
        [DisplayName("状态")]
        public CommonStatus Status { get; set; } = CommonStatus.Enabled;

        /// <summary>备注信息</summary>
        [StringLength(500)]
        [DisplayName("备注")]
        public string? Remark { get; set; }

        /// <summary>并发控制字段 - 乐观并发控制</summary>
        [Timestamp]
        [DisplayName("版本")]
        public byte[] RowVersion { get; set; } = new byte[8];

        // 导航属性

        /// <summary>
        /// 患者信息
        /// </summary>
        public virtual Patient? Patient { get; set; }

        /// <summary>
        /// 医生信息
        /// </summary>
        public virtual User? User { get; set; }

        /// <summary>
        /// 医疗案例
        /// </summary>
        public virtual MedicalCase.MedicalCase? MedicalCase { get; set; }
    }
}
