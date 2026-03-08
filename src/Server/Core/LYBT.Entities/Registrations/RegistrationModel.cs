using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using LYBT.Entities.Common;
using LYBT.Shared.Models.Enums;

namespace LYBT.Entities.Registrations
{
    /// <summary>
    /// 挂号记录实体
    /// Design: registration-module-design.md (D1: 独立实体, D2: 双模式入口)
    /// PRD: registration.md US-REG-001~007
    /// </summary>
    [Table("Registrations")]
    public class Registration : BaseEntity
    {
        /// <summary>
        /// 关联患者 ID
        /// </summary>
        [Required]
        public Guid PatientId { get; set; }

        /// <summary>
        /// 患者姓名 (冗余字段，列表展示用，避免跨表查询)
        /// </summary>
        [Required]
        [StringLength(100)]
        public string PatientName { get; set; } = string.Empty;

        /// <summary>
        /// 指派医生 ID
        /// </summary>
        [Required]
        public Guid DoctorId { get; set; }

        /// <summary>
        /// 医生姓名 (冗余字段)
        /// </summary>
        [Required]
        [StringLength(100)]
        public string DoctorName { get; set; } = string.Empty;

        /// <summary>
        /// 关联医案 ID (Waiting 状态时为 null，接诊后填入)
        /// </summary>
        public Guid? MedicalCaseId { get; set; }

        /// <summary>
        /// 挂号来源 -- 决定状态流转规则
        /// Receptionist: 前台创建，经 Waiting -> InProgress
        /// Doctor: 医生直接看诊，跳过 Waiting 直接 InProgress
        /// </summary>
        [Required]
        public RegistrationSource Source { get; set; }

        /// <summary>
        /// 挂号状态
        /// 状态机: Waiting -> InProgress -> Completed/Cancelled
        /// </summary>
        [Required]
        public RegistrationStatus Status { get; set; }

        /// <summary>
        /// 备注
        /// </summary>
        [StringLength(500)]
        public string? Remark { get; set; }
    }
}
