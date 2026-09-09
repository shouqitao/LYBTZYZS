using LYBT.Desktop.Contracts.Results;
using LYBT.Shared.Models.Contracts.Auth;
using LYBT.Shared.Models.Contracts.Common;

namespace LYBT.Desktop.Contracts.Services;

/// <summary>
/// 安全审计日志查询服务（US-SHELL-014）——VM 只注入本接口，不直连 IApiClient（DP10）
/// </summary>
public interface ISecurityAuditQueryService
{
    Task<CommandResult<PagedResult<SecurityAuditLogDto>>> GetLogsAsync(
        int page,
        int pageSize,
        string? eventType = null,
        string? userName = null,
        DateTime? from = null,
        DateTime? to = null,
        CancellationToken ct = default);
}
