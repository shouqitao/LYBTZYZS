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
    /// 管理完整诊疗流程：一医案一诊断，一医案至多一处方
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

        // ========== 打印管理字段 ==========

        /// <summary>当前打印版本号</summary>
        [DisplayName("打印版本号")]
        public int PrintVersion { get; set; } = 1;

        /// <summary>最后打印时间</summary>
        [DisplayName("最后打印时间")]
        public DateTime? LastPrintedAt { get; set; }

        /// <summary>打印次数</summary>
        [DisplayName("打印次数")]
        public int PrintCount { get; set; } = 0;

        /// <summary>是否已打印</summary>
        [DisplayName("是否已打印")]
        public bool IsPrinted { get; set; } = false;

        // ========== 同聚合导航属性 ==========

        /// <summary>诊断记录（1:1关系）</summary>
        [DisplayName("诊断记录")]
        public virtual Consultation? Consultation { get; set; }

        /// <summary>处方信息（1:0..1关系）</summary>
        [DisplayName("处方信息")]
        public virtual Prescription? Prescription { get; set; }

        /// <summary>打印日志记录</summary>
        public virtual ICollection<MedicalCasePrintLog> PrintLogs { get; set; } = new List<MedicalCasePrintLog>();

        // ========== 计算属性 ==========

        /// <summary>
        /// 是否已锁定
        /// 锁定条件：已完成状态 且 非当天（完成或创建）
        /// Suspended/Active状态不受跨日限制，可随时编辑
        /// </summary>
        public bool IsLocked => IsCompleted && (CompletedAt ?? CreatedAt).Date < DateTime.Today;

        /// <summary>
        /// 是否活跃（可编辑状态）
        /// </summary>
        public bool IsActive => CaseStatus == MedicalCaseStatus.Suspended || CaseStatus == MedicalCaseStatus.Active;

        /// <summary>
        /// 是否已完成
        /// </summary>
        public bool IsCompleted => CaseStatus == MedicalCaseStatus.Completed;

        // CanEdit()方法已删除，权限判断移到MedicalCasePermissionService

        // ========== DDD 聚合根域方法 ==========

        /// <summary>
        /// 完成医案 -- 设置状态为 Completed + CompletedAt
        /// </summary>
        public void Complete()
        {
            CaseStatus = MedicalCaseStatus.Completed;
            CompletedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// 挂起医案 -- 设置状态为 Suspended
        /// </summary>
        public void Suspend()
        {
            CaseStatus = MedicalCaseStatus.Suspended;
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// 软删除医案 -- 设置 IsDeleted = true
        /// </summary>
        public void SoftDelete()
        {
            IsDeleted = true;
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// 更新诊断信息（4 个核心字段）
        /// </summary>
        public void UpdateConsultation(string? presentIllness, string? tongueDiagnosis, string? pulseDiagnosis, string? tcmDiagnosis)
        {
            if (Consultation == null) return;

            Consultation.PresentIllness = presentIllness;
            Consultation.TongueDiagnosis = tongueDiagnosis;
            Consultation.PulseDiagnosis = pulseDiagnosis;
            Consultation.TcmDiagnosis = tcmDiagnosis;
            Consultation.UpdatedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
        }
    }
}
