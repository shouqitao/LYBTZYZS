using LYBT.Entities.Auth;
using LYBT.Module.Identity.Interfaces;
using LYBT.Shared.Models.Contracts.Auth;
using LYBT.Shared.Models.Contracts.Common;
using Microsoft.Extensions.Logging;

namespace LYBT.Module.Identity.Services;

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

    public async Task<Result<PagedResult<SecurityAuditLogDto>>> GetLogsAsync(
        SecurityAuditLogQueryDto query,
        CancellationToken ct = default)
    {
        var (items, totalCount) = await _repository.GetPagedAsync(
            query.EventType,
            query.UserName,
            query.From,
            query.To,
            query.Page,
            query.PageSize,
            ct);

        var dtos = items.Select(x => new SecurityAuditLogDto
        {
            Id = x.Id,
            CreatedAt = x.CreatedAt,
            UserId = x.UserId,
            UserName = x.UserName,
            EventType = x.EventType,
            IpAddress = x.IpAddress,
            UserAgent = x.UserAgent,
            Details = x.Details,
            IsSuccess = x.IsSuccess,
            FailureReason = x.FailureReason
        }).ToList();

        var page = query.Page < 1 ? 1 : query.Page;
        var pageSize = query.PageSize < 1 ? 20 : Math.Min(query.PageSize, 100);
        return Result<PagedResult<SecurityAuditLogDto>>.Success(
            new PagedResult<SecurityAuditLogDto>(dtos, totalCount, page, pageSize));
    }
}
