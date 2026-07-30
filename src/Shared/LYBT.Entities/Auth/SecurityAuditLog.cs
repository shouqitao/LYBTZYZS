using LYBT.Entities.Common;

namespace LYBT.Entities.Auth;

public class SecurityAuditLog : BaseEntity
{
    public Guid? UserId { get; set; }
    public string? UserName { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public string? Details { get; set; }
    public bool IsSuccess { get; set; }
    public string? FailureReason { get; set; }
}
