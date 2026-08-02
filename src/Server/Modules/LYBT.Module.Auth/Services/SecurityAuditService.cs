using LYBT.Entities.Auth;
using LYBT.Module.Auth.Interfaces;
using LYBT.Module.Auth.Models;
using Microsoft.Extensions.Logging;

namespace LYBT.Module.Auth.Services;

public class SecurityAuditService : ISecurityAuditService
{
    private readonly ISecurityAuditRepository _repository;
    private readonly ILogger<SecurityAuditService> _logger;

    public SecurityAuditService(ISecurityAuditRepository repository, ILogger<SecurityAuditService> logger)
    {
        _repository = repository;
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

            await _repository.AddAsync(log, ct);
            await _repository.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to record security audit event: {EventType}", auditEvent.EventType);
        }
    }
}
