using LYBT.Infrastructure.Web;
using LYBT.Infrastructure.Constants;
using LYBT.Module.Reports.Interfaces;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Reports;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LYBT.LocalWebAPI.Controllers;

/// <summary>
/// 统计报表 API - 收入、问诊、药材使用统计
/// API 单一职能（P0-1 2026-08-14）：注入 IReportService（与 Remote 一致）——DTO 组装在 Service 层，
/// Controller 不直连 Repository（原 Local 直注 IReportRepository 跳过 Service 层——分层违规）
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Authorize(Policy = PolicyConstants.DoctorOrAdmin)]
public class ReportsController : BaseApiController
{
    private readonly IReportService _reportService;

    public ReportsController(IReportService reportService, ILogger<ReportsController> logger)
        : base(logger)
    {
        _reportService = reportService;
    }

    /// <summary>
    /// 获取日收入统计
    /// </summary>
    [HttpGet("daily/income")]
    [ProducesResponseType(typeof(ApiResponse<DailyIncomeDto>), 200)]
    public async Task<IActionResult> GetDailyIncome(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        CancellationToken cancellationToken = default)
    {
        var start = startDate ?? DateTime.Today;
        var end = endDate ?? start;
        if (start > end)
            return BadRequest("开始日期不能晚于结束日期");

        var dto = await _reportService.GetDailyIncomeAsync(start, end, cancellationToken);

        return Success(dto, "查询成功");
    }

    /// <summary>
    /// 获取日问诊统计
    /// </summary>
    [HttpGet("daily/consultations")]
    [ProducesResponseType(typeof(ApiResponse<DailyConsultationDto>), 200)]
    public async Task<IActionResult> GetDailyConsultations(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        CancellationToken cancellationToken = default)
    {
        var start = startDate ?? DateTime.Today;
        var end = endDate ?? start;
        if (start > end)
            return BadRequest("开始日期不能晚于结束日期");

        var dto = await _reportService.GetDailyConsultationsAsync(start, end, cancellationToken);

        return Success(dto, "查询成功");
    }

    /// <summary>
    /// 获取日药材使用统计
    /// </summary>
    [HttpGet("daily/herbs")]
    [ProducesResponseType(typeof(ApiResponse<DailyHerbUsageDto>), 200)]
    public async Task<IActionResult> GetDailyHerbs(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        CancellationToken cancellationToken = default)
    {
        var start = startDate ?? DateTime.Today;
        var end = endDate ?? start;
        if (start > end)
            return BadRequest("开始日期不能晚于结束日期");

        var dto = await _reportService.GetDailyHerbUsageAsync(start, end, cancellationToken);

        return Success(dto, "查询成功");
    }
}
