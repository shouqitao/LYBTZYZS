using System.ComponentModel;
using LYBT.Shared.Models.Enums;

namespace LYBT.Shared.Models.Contracts.MedicalCase
{
    /// <summary>
    /// 医案审计日志DTO
    /// OpenSpec: refactor-medicalcase-management (LIFECYCLE-008)
    /// 用于前后端传递医案的修改历史记录
    /// </summary>
    public class MedicalCaseAuditLogDto
    {
        /// <summary>唯一标识</summary>
        [DisplayName("唯一标识")]
        public Guid Id { get; set; }

        /// <summary>医案ID</summary>
        [DisplayName("医案ID")]
        public Guid MedicalCaseId { get; set; }

        /// <summary>操作者ID</summary>
        [DisplayName("操作者ID")]
        public Guid OperatorId { get; set; }

        /// <summary>操作者姓名</summary>
        [DisplayName("操作者姓名")]
        public string OperatorName { get; set; } = string.Empty;

        /// <summary>操作者角色</summary>
        [DisplayName("操作者角色")]
        public UserRole OperatorRole { get; set; }

        /// <summary>操作类型</summary>
        [DisplayName("操作类型")]
        public AuditOperationType OperationType { get; set; }

        /// <summary>操作类型显示名称</summary>
        [DisplayName("操作类型名称")]
        public string OperationTypeName => OperationType switch
        {
            AuditOperationType.Create => "创建",
            AuditOperationType.Update => "更新",
            AuditOperationType.StatusChange => "状态变更",
            AuditOperationType.SoftDelete => "软删除",
            _ => "未知"
        };

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
        [DisplayName("修改原因")]
        public string? Reason { get; set; }

        /// <summary>创建时间</summary>
        [DisplayName("创建时间")]
        public DateTime CreatedAt { get; set; }
    }
}
