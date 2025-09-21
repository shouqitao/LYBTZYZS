using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using LYBT.Entities.Prescriptions;
using LYBT.Shared.Models.Enums;

namespace LYBT.Entities.MedicalCase
{

    /// <summary>
    /// 医疗案例实体 - 根据20250920文档要求重构
    /// 作为聚合根，管理完整诊疗流程
    /// 一病案一诊断，一病案至多一处方
    /// </summary>
    [Table("MedicalCases")]
    public class MedicalCase
    {

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

        /// <summary>创建时间（用于同日编辑判定）</summary>
        [Required]
        [DisplayName("创建时间")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        /// <summary>创建人ID（医生用户ID）</summary>
        [Required]
        [DisplayName("创建人")]
        public Guid CreatedBy { get; set; }

        /// <summary>看诊时间（兼容旧字段）</summary>
        [DisplayName("看诊时间")]
        public DateTime ConsultationDate { get; set; } = DateTime.Now;

        /// <summary>状态</summary>
        [DisplayName("状态")]
        public MedicalCaseStatus Status { get; set; } = MedicalCaseStatus.Active;

        /// <summary>备注</summary>
        [StringLength(500)]
        [DisplayName("备注")]
        public string? Remark { get; set; }

        /// <summary>并发控制字段 - 乐观并发控制</summary>
        [Timestamp]
        [DisplayName("版本")]
        public byte[] RowVersion { get; set; } = new byte[8];

        // 导航属性 - 根据文档要求：1:1关系

        /// <summary>看诊记录（导航属性）- 一个医疗案例对应一次看诊 (1:1关系)</summary>
        [DisplayName("看诊记录")]
        public virtual LYBT.Entities.Consultation.Consultation? Consultation { get; set; }

        /// <summary>处方信息（导航属性）- 一个医疗案例至多一张处方 (0..1关系)</summary>
        [DisplayName("处方信息")]
        public virtual Prescription? Prescription { get; set; }
    }
}
