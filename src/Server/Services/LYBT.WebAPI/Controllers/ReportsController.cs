using Asp.Versioning;
using LYBT.Infrastructure.Constants;
using LYBT.Infrastructure.Web;
using LYBT.Module.Reports.Application.Queries;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Reports;
using MediatR;
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
    private readonly ISender _sender;

    public ReportsController(
        ISender sender,
        ILogger<ReportsController> logger)
        : base(logger)
    {
        _sender = sender;
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
        var result = await _sender.Send(new GetDailyIncomeQuery(startDate, endDate), cancellationToken);
        if (!result.IsSuccess)
            return BusinessFail(result.Error ?? "查询失败");

        return Success(result.Value!, "查询成功");
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
        var result = await _sender.Send(new GetDailyConsultationsQuery(startDate, endDate), cancellationToken);
        if (!result.IsSuccess)
            return BusinessFail(result.Error ?? "查询失败");

        return Success(result.Value!, "查询成功");
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
        var result = await _sender.Send(new GetDailyHerbUsageQuery(startDate, endDate), cancellationToken);
        if (!result.IsSuccess)
            return BusinessFail(result.Error ?? "查询失败");

        return Success(result.Value!, "查询成功");
    }
}


