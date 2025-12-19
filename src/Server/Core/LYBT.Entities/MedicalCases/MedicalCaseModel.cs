using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using LYBT.Entities.Common;
using LYBT.Entities.Consultations;
using LYBT.Entities.Prescriptions;
using LYBT.Shared.Models.Enums;

namespace LYBT.Entities.MedicalCases
{

    /// <summary>
    /// 医案实体 - 聚合根
    /// OpenSpec: simplify-medicalcase-dataflow
    /// 管理完整诊疗流程：一病案一诊断，一病案至多一处方
    /// </summary>
    [Table("MedicalCases")]
    public class MedicalCase : BaseEntity
    {
        // ========== 跨聚合引用 (仅ID，符合DDD原则) ==========

        /// <summary>患者ID</summary>
        [Required]
        [DisplayName("患者ID")]
        public Guid PatientId { get; set; }

        /// <summary>患者姓名（冗余-读优化）</summary>
        [Required]
        [StringLength(50)]
        [DisplayName("患者姓名")]
        public string PatientName { get; set; } = string.Empty;

        /// <summary>医生ID（主治医生）- 重命名自DoctorId</summary>
        [Required]
        [DisplayName("医生ID")]
        public Guid UserId { get; set; }

        /// <summary>医生姓名（冗余-读优化）</summary>
        [Required]
        [StringLength(50)]
        [DisplayName("医生姓名")]
        public string DoctorName { get; set; } = string.Empty;

        // ========== 业务字段 ==========

        /// <summary>医案编号（业务编号，如MC20251219001）</summary>
        [StringLength(50)]
        [DisplayName("医案编号")]
        public string? CaseNumber { get; set; }

        /// <summary>业务流程状态</summary>
        [DisplayName("医案状态")]
        public MedicalCaseStatus CaseStatus { get; set; } = MedicalCaseStatus.Active;

        /// <summary>
        /// 是否需要开处方
        /// null: 未标记（用户还未做决策）
        /// true: 需要开处方
        /// false: 不需要开处方
        /// </summary>
        [DisplayName("是否需要开处方")]
        public bool? NeedsPrescription { get; set; }

        /// <summary>完成时间（用于锁定判断）</summary>
        [DisplayName("完成时间")]
        public DateTime? CompletedAt { get; set; }

        /// <summary>备注</summary>
        [StringLength(500)]
        [DisplayName("备注")]
        public string? Remark { get; set; }

        // ConsultationDate已删除，用BaseEntity.CreatedAt代替

        // ========== 同聚合导航属性 ==========

        /// <summary>诊断记录（1:1关系）</summary>
        [DisplayName("诊断记录")]
        public virtual Consultation? Consultation { get; set; }

        /// <summary>处方信息（1:0..1关系）</summary>
        [DisplayName("处方信息")]
        public virtual Prescription? Prescription { get; set; }

        // ========== 计算属性 ==========

        /// <summary>
        /// 是否已锁定
        /// 锁定条件：已完成 或 非当天创建
        /// </summary>
        public bool IsLocked => CompletedAt.HasValue || CreatedAt.Date < DateTime.Today;

        /// <summary>
        /// 是否活跃（可编辑状态）
        /// </summary>
        public bool IsActive => CaseStatus == MedicalCaseStatus.Draft || CaseStatus == MedicalCaseStatus.Active;

        /// <summary>
        /// 是否已完成
        /// </summary>
        public bool IsCompleted => CaseStatus == MedicalCaseStatus.Completed;

        // CanEdit()方法已删除，权限判断移到MedicalCasePermissionService
    }
}
