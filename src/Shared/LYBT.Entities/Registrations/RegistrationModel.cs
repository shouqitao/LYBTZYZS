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
    /// 当日顺序号 (创建时自动生成: 当天最大号+1)
    /// </summary>
    public int QueueNumber { get; set; }

    /// <summary>
    /// 挂号费 (元)
    /// </summary>
    [Column(TypeName = "decimal(10,2)")]
    public decimal RegistrationFee { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    [StringLength(500)]
    public string? Remark { get; set; }

    // ==== 领域方法 ====

    /// <summary>
    /// 接诊：从队列选中患者，Registration -> InProgress
    /// </summary>
    public void StartVisit()
    {
        if (Status != RegistrationStatus.Waiting)
            throw new InvalidOperationException("只有等待中的挂号可以接诊");

        Status = RegistrationStatus.InProgress;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// 完成就诊 — 仅 InProgress → Completed（P0-10）
    /// </summary>
    public void Complete()
    {
        if (Status != RegistrationStatus.InProgress)
            throw new InvalidOperationException("只有进行中的挂号可以完成");

        Status = RegistrationStatus.Completed;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// 取消挂号（T5-1 #9: 服务端守卫——原无条件置 Cancelled，校验全在 Desktop UI，绕过 UI 可取消任意挂号）
    /// </summary>
    public void Cancel()
    {
        // REG-BR-008: 仅等待中的挂号可取消
        if (Status != RegistrationStatus.Waiting)
            throw new InvalidOperationException("只有等待中的挂号可以取消");

        // REG-BR-001: 已关联医案的挂号不可取消（应通过医案取消联动）
        if (MedicalCaseId.HasValue)
            throw new InvalidOperationException("该挂号已关联医案，请通过医案操作取消");

        Status = RegistrationStatus.Cancelled;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// 关联医案
    /// </summary>
    public void AssignMedicalCase(Guid medicalCaseId)
    {
        MedicalCaseId = medicalCaseId;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// 恢复到等待状态
    /// </summary>
    public void RevertToWaiting()
    {
        Status = RegistrationStatus.Waiting;
        MedicalCaseId = null;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// 医案取消联动 — 医生来源 InProgress → Cancelled（闭环）
    /// 允许 InProgress 状态，清空关联医案，仅供 HandleMedicalCaseCancelledAsync 调用
    /// </summary>
    public void CancelFromMedicalCase()
    {
        Status = RegistrationStatus.Cancelled;
        MedicalCaseId = null;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// 软删除挂号记录
    /// </summary>
    public void SoftDelete(Guid deletedBy)
    {
        IsDeleted = true;
        UpdatedBy = deletedBy;
        UpdatedAt = DateTime.UtcNow;
    }
}
}


