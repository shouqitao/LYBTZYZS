using LYBT.Entities.Auth;
using LYBT.Infrastructure.Data;
using LYBT.Module.Auth.Interfaces;
using LYBT.Module.Auth.Models;
using Microsoft.Extensions.Logging;

namespace LYBT.Module.Auth.Services;

public class SecurityAuditService : ISecurityAuditService
{
    private readonly AppDbContext _context;
    private readonly ILogger<SecurityAuditService> _logger;

    public SecurityAuditService(AppDbContext context, ILogger<SecurityAuditService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task RecordEventAsync(SecurityAuditEvent auditEvent, CancellationToken ct = default)
    {
        try
        {
            var log = new SecurityAuditLog
            {
                UserId = auditEvent.UserId,
                UserName = auditEvent.UserName,
                EventType = auditEvent.EventType,
                IpAddress = auditEvent.IpAddress,
                UserAgent = auditEvent.UserAgent,
                Details = auditEvent.Details,
                IsSuccess = auditEvent.IsSuccess,
                FailureReason = auditEvent.FailureReason,
                CreatedAt = DateTime.UtcNow
            };

            await _context.SecurityAuditLogs.AddAsync(log, ct);
            await _context.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to record security audit event: {EventType}", auditEvent.EventType);
        }
    }
}
