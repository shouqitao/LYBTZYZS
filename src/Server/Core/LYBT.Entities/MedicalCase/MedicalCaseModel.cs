using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using LYBT.Entities.Prescriptions;
using LYBT.Shared.Models.Enums;

namespace LYBT.Entities.MedicalCase {

    /// <summary>
    /// 医疗案例实体 - UltraThink v2.0架构简化版
    /// 合并了原BaseMedicalCase和MedicalCaseModel
    /// 作为聘合根，管理完整诊疗流程，不包含诊断字段（属于Consultation）
    /// </summary>
    [Table("MedicalCases")]
    public class MedicalCase {

        /// <summary>医疗案例ID</summary>
        [Key]
        [DisplayName("医疗案例ID")]
        public Guid Id { get; set; }

        /// <summary>患者ID</summary>
        [Required]
        [DisplayName("患者ID")]
        public Guid PatientId { get; set; }

        /// <summary>患者姓名（显示用）</summary>
        [Required]
        [StringLength(50)]
        [DisplayName("患者姓名")]
        public string PatientName { get; set; } = string.Empty;

        /// <summary>医生ID（主治医生）</summary>
        [Required]
        [DisplayName("医生ID")]
        public Guid DoctorId { get; set; }

        /// <summary>医生姓名（显示用）</summary>
        [Required]
        [StringLength(50)]
        [DisplayName("医生姓名")]
        public string DoctorName { get; set; } = string.Empty;

        /// <summary>处方ID（关联Prescription，可为空）</summary>
        [DisplayName("处方ID")]
        public Guid? PrescriptionId { get; set; }

        /// <summary>看诊时间（医案创建时间）</summary>
        [DisplayName("看诊时间")]
        public DateTime ConsultationDate { get; set; } = DateTime.Now;

        /// <summary>状态</summary>
        [DisplayName("状态")]
        public MedicalCaseStatus Status { get; set; } = MedicalCaseStatus.Registered;

        /// <summary>备注</summary>
        [StringLength(500)]
        [DisplayName("备注")]
        public string? Remark { get; set; }

        // 导航属性 - UltraThink Phase 7修复：1:1关系
        /// <summary>看诊记录（导航属性）- 一个医疗案例对应一次看诊 (1:1关系)</summary>
        [DisplayName("看诊记录")]
        public virtual LYBT.Entities.Consultation.Consultation? Consultation { get; set; }

        /// <summary>处方信息（导航属性）</summary>
        [DisplayName("处方信息")]
        public virtual Prescription? Prescription { get; set; }
    }
}
