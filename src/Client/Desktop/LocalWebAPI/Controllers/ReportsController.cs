using LYBT.Infrastructure.Web;
using LYBT.Infrastructure.Constants;
using LYBT.Module.Reports.Application.Queries;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Reports;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LYBT.LocalWebAPI.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize(Policy = PolicyConstants.DoctorOrAdminOrReceptionist)]
public class ReportsController : BaseApiController
{
    private readonly ISender _sender;

    public ReportsController(ISender sender, ILogger<ReportsController> logger) : base(logger)
    {
        _sender = sender;
    }

    [HttpGet("daily/income")]
    public async Task<IActionResult> GetDailyIncome(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _sender.Send(new GetDailyIncomeQuery(startDate, endDate), cancellationToken);
        if (!result.IsSuccess)
            return BusinessFail(result.Error ?? "查询失败");
        return Success(result.Value!, "查询成功");
    }

    [HttpGet("daily/consultations")]
    public async Task<IActionResult> GetDailyConsultations(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _sender.Send(new GetDailyConsultationsQuery(startDate, endDate), cancellationToken);
        if (!result.IsSuccess)
            return BusinessFail(result.Error ?? "查询失败");
        return Success(result.Value!, "查询成功");
    }

    [HttpGet("daily/herbs")]
    public async Task<IActionResult> GetDailyHerbs(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _sender.Send(new GetDailyHerbUsageQuery(startDate, endDate), cancellationToken);
        if (!result.IsSuccess)
            return BusinessFail(result.Error ?? "查询失败");
        return Success(result.Value!, "查询成功");
    }
}
