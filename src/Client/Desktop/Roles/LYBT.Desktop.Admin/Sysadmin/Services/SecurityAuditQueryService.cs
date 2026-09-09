using LYBT.Desktop.Contracts.ApiClient;
using LYBT.Desktop.Contracts.Results;
using LYBT.Desktop.Contracts.Services;
using LYBT.Shared.Models.Contracts.Auth;
using LYBT.Shared.Models.Contracts.Common;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Admin.Sysadmin.Services;

/// <summary>
/// 安全审计查询服务——封装 IApiClientIdentity.GetSecurityAuditLogsAsync（D6 DP10 门面）
/// </summary>
public class SecurityAuditQueryService : ISecurityAuditQueryService
{
    private readonly IApiClientIdentity _identity;
    private readonly ILogger<SecurityAuditQueryService> _logger;

    public SecurityAuditQueryService(IApiClientIdentity identity, ILogger<SecurityAuditQueryService> logger)
    {
        _identity = identity;
        _logger = logger;
    }

    public async Task<CommandResult<PagedResult<SecurityAuditLogDto>>> GetLogsAsync(
        int page,
        int pageSize,
        string? eventType = null,
        string? userName = null,
        DateTime? from = null,
        DateTime? to = null,
        CancellationToken ct = default)
    {
        try
        {
            var response = await _identity.GetSecurityAuditLogsAsync(page, pageSize, eventType, userName, from, to);
            if (response.Success && response.Data != null)
                return CommandResult<PagedResult<SecurityAuditLogDto>>.Succeeded(response.Data);
            return CommandResult<PagedResult<SecurityAuditLogDto>>.Failed(
                string.IsNullOrWhiteSpace(response.Message) ? "查询安全审计日志失败" : response.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetSecurityAuditLogs failed page={Page}", page);
            return CommandResult<PagedResult<SecurityAuditLogDto>>.Failed(ex.Message);
        }
    }
}
