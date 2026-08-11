using Asp.Versioning;
using LYBT.Infrastructure.Constants;
using LYBT.Infrastructure.Web;
using LYBT.Module.Reports.Interfaces;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Reports;
using LYBT.Shared.Models.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LYBT.WebAPI.Controllers;

/// <summary>
/// 统计报表 API - 收入、问诊、药材使用统计
/// </summary>
[ApiController]
[ApiVersion("1")]
[Route("api/v{version:apiVersion}/reports")]
[Authorize(Policy = PolicyConstants.DoctorOrAdmin)]
public class ReportsController : BaseApiController
{
    private readonly IReportService _reportService;

    public ReportsController(
        IReportService reportService,
        ILogger<ReportsController> logger)
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
        // T5-2 #17 (US-REPORT-001~003): 仅传 startDate 时 endDate 默认等于 startDate；startDate>endDate → 400
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
        // T5-2 #17 (US-REPORT-001~003): 仅传 startDate 时 endDate 默认等于 startDate；startDate>endDate → 400
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
        // T5-2 #17 (US-REPORT-001~003): 仅传 startDate 时 endDate 默认等于 startDate；startDate>endDate → 400
        var start = startDate ?? DateTime.Today;
        var end = endDate ?? start;
        if (start > end)
            return BadRequest("开始日期不能晚于结束日期");

        var dto = await _reportService.GetDailyHerbUsageAsync(start, end, cancellationToken);

        return Success(dto, "查询成功");
    }

    /// <summary>
    /// 获取收入趋势（挂号费/药费/合计，默认最近 30 天）
    /// </summary>
    [HttpGet("trend/income")]
    [ProducesResponseType(typeof(ApiResponse<IncomeTrendDto>), 200)]
    public async Task<IActionResult> GetIncomeTrend(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] ReportGranularity granularity = ReportGranularity.Day,
        CancellationToken cancellationToken = default)
    {
        var end = endDate ?? DateTime.Today;
        var start = startDate ?? end.AddDays(-29);

        var dto = await _reportService.GetIncomeTrendAsync(start, end, granularity, cancellationToken);

        return Success(dto, "查询成功");
    }

    /// <summary>
    /// 获取问诊趋势（默认最近 30 天）
    /// </summary>
    [HttpGet("trend/consultations")]
    [ProducesResponseType(typeof(ApiResponse<ConsultationTrendDto>), 200)]
    public async Task<IActionResult> GetConsultationTrend(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] ReportGranularity granularity = ReportGranularity.Day,
        CancellationToken cancellationToken = default)
    {
        var end = endDate ?? DateTime.Today;
        var start = startDate ?? end.AddDays(-29);

        var dto = await _reportService.GetConsultationTrendAsync(start, end, granularity, cancellationToken);

        return Success(dto, "查询成功");
    }

    /// <summary>
    /// 获取医生绩效统计
    /// </summary>
    [HttpGet("doctor-performance")]
    [ProducesResponseType(typeof(ApiResponse<List<DoctorPerformanceDto>>), 200)]
    public async Task<IActionResult> GetDoctorPerformance(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        CancellationToken cancellationToken = default)
    {
        // T5-2 #17 (US-REPORT-001~003): 仅传 startDate 时 endDate 默认等于 startDate；startDate>endDate → 400
        var start = startDate ?? DateTime.Today;
        var end = endDate ?? start;
        if (start > end)
            return BadRequest("开始日期不能晚于结束日期");

        var dto = await _reportService.GetDoctorPerformanceAsync(start, end, cancellationToken);

        return Success(dto, "查询成功");
    }

    /// <summary>
    /// 获取热门药材排行
    /// </summary>
    [HttpGet("herbs/ranking")]
    [ProducesResponseType(typeof(ApiResponse<List<HerbUsageItemDto>>), 200)]
    public async Task<IActionResult> GetHerbRanking(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] int top = 10,
        CancellationToken cancellationToken = default)
    {
        // T5-2 #17 (US-REPORT-001~003): 仅传 startDate 时 endDate 默认等于 startDate；startDate>endDate → 400
        var start = startDate ?? DateTime.Today;
        var end = endDate ?? start;
        if (start > end)
            return BadRequest("开始日期不能晚于结束日期");

        var dto = await _reportService.GetHerbRankingAsync(start, end, top, cancellationToken);

        return Success(dto, "查询成功");
    }

    /// <summary>
    /// 获取患者流量（新患者/回头患者，默认最近 30 天）
    /// </summary>
    [HttpGet("patient-flow")]
    [ProducesResponseType(typeof(ApiResponse<PatientFlowDto>), 200)]
    public async Task<IActionResult> GetPatientFlow(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] ReportGranularity granularity = ReportGranularity.Day,
        CancellationToken cancellationToken = default)
    {
        var end = endDate ?? DateTime.Today;
        var start = startDate ?? end.AddDays(-29);

        var dto = await _reportService.GetPatientFlowAsync(start, end, granularity, cancellationToken);

        return Success(dto, "查询成功");
    }
}
