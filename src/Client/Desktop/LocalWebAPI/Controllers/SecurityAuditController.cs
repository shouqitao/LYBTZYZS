using LYBT.Infrastructure.Constants;
using LYBT.Infrastructure.Web;
using LYBT.Shared.Models.Contracts.Auth;
using LYBT.Shared.Models.Contracts.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LYBT.LocalWebAPI.Controllers;

/// <summary>
/// 安全审计查询（本地模式）——按 05-dual-mode：本地不记安全审计，查询返回空页。
/// 路由/权限与远程 SecurityAuditController 对齐（SysAdminOnly）。
/// </summary>
[ApiController]
[Route("api/v1/security-audit")]
[Authorize(Policy = PolicyConstants.SysAdminOnly)]
public class SecurityAuditController : BaseApiController
{
    public SecurityAuditController(ILogger<SecurityAuditController> logger)
        : base(logger)
    {
    }

    /// <summary>本地模式无安全审计数据，返回空分页（US-SHELL-014）</summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<SecurityAuditLogDto>>), StatusCodes.Status200OK)]
    public IActionResult GetLogs(
        [FromQuery] string? eventType,
        [FromQuery] string? userName,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var empty = new PagedResult<SecurityAuditLogDto>(
            new List<SecurityAuditLogDto>(),
            0,
            page < 1 ? 1 : page,
            pageSize < 1 ? 20 : Math.Min(pageSize, 100));
        return SuccessPaged(empty, "本地模式无安全审计数据");
    }
}
