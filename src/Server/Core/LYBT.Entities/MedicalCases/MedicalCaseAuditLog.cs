using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using LYBT.Entities.Common;
using LYBT.Shared.Models.Enums;

namespace LYBT.Entities.MedicalCases
{
    /// <summary>
    /// 医案审计日志实体
    /// OpenSpec: refactor-medicalcase-management (LIFECYCLE-008)
    /// 记录医案的所有修改历史，包括创建、更新、状态变更和删除
    /// </summary>
    [Table("MedicalCaseAuditLogs")]
    public class MedicalCaseAuditLog : BaseEntity
    {
        /// <summary>医案ID</summary>
        [Required]
        [DisplayName("医案ID")]
        public Guid MedicalCaseId { get; set; }

        /// <summary>操作者ID</summary>
        [Required]
        [DisplayName("操作者ID")]
        public Guid OperatorId { get; set; }

        /// <summary>操作者姓名</summary>
        [Required]
        [StringLength(100)]
        [DisplayName("操作者姓名")]
        public string OperatorName { get; set; } = string.Empty;

        /// <summary>操作者角色</summary>
        [DisplayName("操作者角色")]
        public UserRole OperatorRole { get; set; }

        /// <summary>操作类型</summary>
        [DisplayName("操作类型")]
        public AuditOperationType OperationType { get; set; }

        /// <summary>变更的字段列表（JSON格式）</summary>
        [DisplayName("变更字段")]
        public string? ChangedFields { get; set; }

        /// <summary>变更前的值（JSON格式）</summary>
        [DisplayName("原值")]
        public string? OldValues { get; set; }

        /// <summary>变更后的值（JSON格式）</summary>
        [DisplayName("新值")]
        public string? NewValues { get; set; }

        /// <summary>修改原因（历史医案修改时必填）</summary>
        [StringLength(500)]
        [DisplayName("修改原因")]
        public string? Reason { get; set; }

        // 导航属性
        /// <summary>关联的医案</summary>
        [ForeignKey(nameof(MedicalCaseId))]
        public virtual MedicalCase? MedicalCase { get; set; }
    }
}
