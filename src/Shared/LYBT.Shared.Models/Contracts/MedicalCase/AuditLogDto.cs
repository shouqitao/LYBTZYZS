namespace LYBT.Shared.Models.Contracts.MedicalCase;

public class AuditLogDto
{
    public DateTime Timestamp { get; set; }
    public string Action { get; set; } = string.Empty;
    public string? PerformedBy { get; set; }
    public string? Details { get; set; }
}
