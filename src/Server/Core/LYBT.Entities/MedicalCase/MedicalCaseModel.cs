using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using LYBT.Entities.Common;
using LYBT.Entities.Prescriptions;
using LYBT.Shared.Models.Enums;

namespace LYBT.Entities.MedicalCase
{

    /// <summary>
    /// 医疗案例实体 - 根据20250920文档要求重构
    /// 作为聚合根，管理完整诊疗流程
    /// 一病案一诊断，一病案至多一处方
    /// 继承BaseEntity实现审计字段自动化
    /// </summary>
    [Table("MedicalCases")]
    public class MedicalCase : BaseEntity
    {

        // Id字段继承自BaseEntity

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

        // 审计字段（CreatedAt、CreatedBy等）继承自BaseEntity

        /// <summary>诊疗时间（兼容旧字段）</summary>
        [DisplayName("诊疗时间")]
        public DateTime ConsultationDate { get; set; } = DateTime.Now;

        /// <summary>状态</summary>
        [DisplayName("状态")]
        public MedicalCaseStatus Status { get; set; } = MedicalCaseStatus.Active;

        /// <summary>备注</summary>
        [StringLength(500)]
        [DisplayName("备注")]
        public string? Remark { get; set; }

        // RowVersion、IsDeleted等字段继承自BaseEntity

        // 导航属性 - 根据文档要求：1:1关系

        /// <summary>诊疗记录（导航属性）- 一个医疗案例对应一次诊疗 (1:1关系)</summary>
        [DisplayName("诊疗记录")]
        public virtual LYBT.Entities.Consultation.Consultation? Consultation { get; set; }

        /// <summary>处方信息（导航属性）- 一个医疗案例至多一张处方 (0..1关系)</summary>
        [DisplayName("处方信息")]
        public virtual Prescription? Prescription { get; set; }
    }
}
