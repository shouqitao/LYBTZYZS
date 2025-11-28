using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using LYBT.Shared.Models.Enums;

namespace LYBT.Entities.Common
{
    /// <summary>
    /// 通用实体审计日志
    /// OpenSpec: add-global-audit-system
    /// 记录所有业务实体的变更历史，包括创建、更新和删除操作
    /// </summary>
    [Table("EntityAuditLogs")]
    public class EntityAuditLog
    {
        /// <summary>唯一标识</summary>
        [Key]
        [DisplayName("唯一标识")]
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>实体类型（Patient, Prescription, Herb, Formula, User, Consultation等）</summary>
        [Required]
        [StringLength(100)]
        [DisplayName("实体类型")]
        public string EntityType { get; set; } = string.Empty;

        /// <summary>实体ID</summary>
        [Required]
        [DisplayName("实体ID")]
        public Guid EntityId { get; set; }

        /// <summary>操作者ID</summary>
        [Required]
        [DisplayName("操作者ID")]
        public Guid OperatorId { get; set; }

        /// <summary>操作者姓名</summary>
        [Required]
        [StringLength(50)]
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

        /// <summary>修改原因</summary>
        [StringLength(500)]
        [DisplayName("修改原因")]
        public string? Reason { get; set; }

        /// <summary>创建时间</summary>
        [DisplayName("创建时间")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
