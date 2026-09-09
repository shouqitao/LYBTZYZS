namespace LYBT.Shared.Models.Contracts.Auth;

/// <summary>
/// 安全审计日志列表项（US-SHELL-014 查询侧）
/// </summary>
public class SecurityAuditLogDto
{
    public Guid Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public Guid? UserId { get; set; }
    public string? UserName { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public string? Details { get; set; }
    public bool IsSuccess { get; set; }
    public string? FailureReason { get; set; }
}
