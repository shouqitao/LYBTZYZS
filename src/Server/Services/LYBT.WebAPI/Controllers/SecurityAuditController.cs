using Asp.Versioning;
using LYBT.Infrastructure.Constants;
using LYBT.Infrastructure.Web;
using LYBT.Module.Identity.Interfaces;
using LYBT.Shared.Models.Contracts.Auth;
using LYBT.Shared.Models.Contracts.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LYBT.WebAPI.Controllers;

/// <summary>
/// 安全审计日志查询 API（US-SHELL-014）
/// 权限：仅 SuperAdmin（SysAdminOnly）；只读，无写/删/导出
/// </summary>
[ApiController]
[ApiVersion("1")]
[Route("api/v{version:apiVersion}/security-audit")]
[Authorize(Policy = PolicyConstants.SysAdminOnly)]
public class SecurityAuditController : BaseApiController
{
    private readonly ISecurityAuditService _auditService;

    public SecurityAuditController(ISecurityAuditService auditService, ILogger<SecurityAuditController> logger)
        : base(logger)
    {
        _auditService = auditService;
    }

    /// <summary>
    /// 分页查询安全审计日志（CreatedAt 倒序）
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<SecurityAuditLogDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetLogs(
        [FromQuery] string? eventType,
        [FromQuery] string? userName,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await _auditService.GetLogsAsync(new SecurityAuditLogQueryDto
        {
            EventType = eventType,
            UserName = userName,
            From = from,
            To = to,
            Page = page,
            PageSize = pageSize
        }, ct);

        if (!result.IsSuccess)
            return BusinessFail(result.ErrorMessage ?? "查询安全审计日志失败");

        return SuccessPaged(result.Data!, "查询成功");
    }
}
