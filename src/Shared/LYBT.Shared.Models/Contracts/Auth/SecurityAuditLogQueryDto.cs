namespace LYBT.Shared.Models.Contracts.Auth;

/// <summary>
/// 安全审计日志查询条件（US-SHELL-014）
/// </summary>
public class SecurityAuditLogQueryDto
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? EventType { get; set; }
    public string? UserName { get; set; }
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
}
