using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using LYBT.Entities.Common;

namespace LYBT.Entities.MedicalCases;

[Table("MedicalCaseAuditLogs")]
public class MedicalCaseAuditLog : BaseEntity
{
    [Required]
    [DisplayName("医案ID")]
    public Guid MedicalCaseId { get; set; }

    [Required]
    [DisplayName("操作人ID")]
    public Guid OperatorId { get; set; }

    [Required]
    [StringLength(100)]
    [DisplayName("操作人姓名")]
    public string OperatorName { get; set; } = string.Empty;

    [Required]
    [DisplayName("操作人角色")]
    public int OperatorRole { get; set; }

    [Required]
    [DisplayName("操作类型")]
    public int OperationType { get; set; }

    [StringLength(500)]
    [DisplayName("原因")]
    public string? Reason { get; set; }

    [DisplayName("变更字段(JSON)")]
    public string? ChangedFields { get; set; }

    [DisplayName("旧值(JSON)")]
    public string? OldValues { get; set; }

    [DisplayName("新值(JSON)")]
    public string? NewValues { get; set; }
}
