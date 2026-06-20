using LYBT.Infrastructure.Web;
using LYBT.Module.Reports.Interfaces;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Reports;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LYBT.LocalWebAPI.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class ReportsController : BaseApiController
{
    private readonly IReportService _reportService;

    public ReportsController(IReportService reportService, ILogger<ReportsController> logger) : base(logger)
    {
        _reportService = reportService;
    }

    [HttpGet("daily/income")]
    public async Task<IActionResult> GetDailyIncome(CancellationToken cancellationToken = default)
    {
        var result = await _reportService.GetDailyIncomeAsync(cancellationToken);
        return Success(result, "查询成功");
    }

    [HttpGet("daily/consultations")]
    public async Task<IActionResult> GetDailyConsultations(CancellationToken cancellationToken = default)
    {
        var result = await _reportService.GetDailyConsultationsAsync(cancellationToken);
        return Success(result, "查询成功");
    }

    [HttpGet("daily/herbs")]
    public async Task<IActionResult> GetDailyHerbs(CancellationToken cancellationToken = default)
    {
        var result = await _reportService.GetDailyHerbUsageAsync(cancellationToken);
        return Success(result, "查询成功");
    }
}
