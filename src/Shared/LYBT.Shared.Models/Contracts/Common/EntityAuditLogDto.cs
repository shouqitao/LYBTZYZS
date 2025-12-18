using System.Text.Json.Serialization;
using LYBT.Shared.Models.Enums;

namespace LYBT.Shared.Models.Contracts.Common
{
    /// <summary>
    /// 通用实体审计日志DTO
    /// OpenSpec: add-global-audit-system
    /// 用于API响应和前端展示
    /// </summary>
    public class EntityAuditLogDto
    {
        /// <summary>唯一标识</summary>
        [JsonPropertyName("id")]
        public Guid Id { get; set; }

        /// <summary>实体类型（Patient, Prescription, Herb, Formula, User, Consultation等）</summary>
        [JsonPropertyName("entityType")]
        public string EntityType { get; set; } = string.Empty;

        /// <summary>实体ID</summary>
        [JsonPropertyName("entityId")]
        public Guid EntityId { get; set; }

        /// <summary>操作者ID</summary>
        [JsonPropertyName("operatorId")]
        public Guid OperatorId { get; set; }

        /// <summary>操作者姓名</summary>
        [JsonPropertyName("operatorName")]
        public string OperatorName { get; set; } = string.Empty;

        /// <summary>操作者角色</summary>
        [JsonPropertyName("operatorRole")]
        public UserRole OperatorRole { get; set; }

        /// <summary>操作者角色显示名称</summary>
        [JsonPropertyName("operatorRoleDisplay")]
        public string OperatorRoleDisplay => OperatorRole switch
        {
            UserRole.SuperAdmin => "超级管理员",
            UserRole.Admin => "管理员",
            UserRole.Doctor => "医生",
            _ => "未知"
        };

        /// <summary>操作类型</summary>
        [JsonPropertyName("operationType")]
        public AuditOperationType OperationType { get; set; }

        /// <summary>操作类型显示名称</summary>
        [JsonPropertyName("operationTypeDisplay")]
        public string OperationTypeDisplay => OperationType switch
        {
            AuditOperationType.Create => "创建",
            AuditOperationType.Update => "修改",
            AuditOperationType.SoftDelete => "删除",
            AuditOperationType.StatusChange => "状态变更",
            _ => "未知操作"
        };

        /// <summary>变更的字段列表（JSON格式）</summary>
        [JsonPropertyName("changedFields")]
        public string? ChangedFields { get; set; }

        /// <summary>变更前的值（JSON格式）</summary>
        [JsonPropertyName("oldValues")]
        public string? OldValues { get; set; }

        /// <summary>变更后的值（JSON格式）</summary>
        [JsonPropertyName("newValues")]
        public string? NewValues { get; set; }

        /// <summary>修改原因</summary>
        [JsonPropertyName("reason")]
        public string? Reason { get; set; }

        /// <summary>创建时间</summary>
        [JsonPropertyName("createdAt")]
        public DateTime CreatedAt { get; set; }

        /// <summary>格式化的创建时间</summary>
        [JsonPropertyName("createdAtDisplay")]
        public string CreatedAtDisplay => CreatedAt.ToString("yyyy-MM-dd HH:mm:ss");
    }
}
