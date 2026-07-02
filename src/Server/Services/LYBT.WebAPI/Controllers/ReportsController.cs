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

    [HttpGet("daily/income")]
    [ProducesResponseType(typeof(ApiResponse<DailyIncomeDto>), 200)]
    public async Task<IActionResult> GetDailyIncome(CancellationToken cancellationToken = default)
    {
        var result = await _sender.Send(new GetDailyIncomeQuery(), cancellationToken);
        if (!result.IsSuccess)
            return BusinessFail(result.Error ?? "查询失败");

        var dto = new DailyIncomeDto
        {
            TotalIncome = result.Value!.TotalIncome,
            RegistrationFeeTotal = result.Value.RegistrationFeeTotal,
            MedicineFeeTotal = result.Value.MedicineFeeTotal
        };
        return Success(dto, "查询成功");
    }

    [HttpGet("daily/consultations")]
    [ProducesResponseType(typeof(ApiResponse<DailyConsultationDto>), 200)]
    public async Task<IActionResult> GetDailyConsultations(CancellationToken cancellationToken = default)
    {
        var result = await _sender.Send(new GetDailyConsultationsQuery(), cancellationToken);
        if (!result.IsSuccess)
            return BusinessFail(result.Error ?? "查询失败");

        var byDoctor = result.Value!.ByDoctor
            .Select(d => new DoctorCountDto { DoctorName = d.DoctorName, Count = d.Count })
            .ToList();
        var dto = new DailyConsultationDto
        {
            TotalCount = result.Value.TotalCount,
            ByDoctor = byDoctor
        };
        return Success(dto, "查询成功");
    }

    [HttpGet("daily/herbs")]
    [ProducesResponseType(typeof(ApiResponse<DailyHerbUsageDto>), 200)]
    public async Task<IActionResult> GetDailyHerbs(CancellationToken cancellationToken = default)
    {
        var result = await _sender.Send(new GetDailyHerbUsageQuery(), cancellationToken);
        if (!result.IsSuccess)
            return BusinessFail(result.Error ?? "查询失败");

        var items = result.Value!.Items
            .Select(h => new HerbUsageItemDto { HerbName = h.HerbName, UsageCount = h.UsageCount, TotalDosage = h.TotalDosage })
            .ToList();
        var dto = new DailyHerbUsageDto { Items = items };
        return Success(dto, "查询成功");
    }
}


