using LYBT.Infrastructure.Web;
using LYBT.Infrastructure.Constants;
using LYBT.Module.Reports.Interfaces;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Reports;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LYBT.LocalWebAPI.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize(Policy = PolicyConstants.DoctorOrAdmin)]
public class ReportsController : BaseApiController
{
    private readonly IReportRepository _reportRepository;

    public ReportsController(IReportRepository reportRepository, ILogger<ReportsController> logger) : base(logger)
    {
        _reportRepository = reportRepository;
    }

    [HttpGet("daily/income")]
    public async Task<IActionResult> GetDailyIncome(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        CancellationToken cancellationToken = default)
    {
        var start = startDate ?? DateTime.Today;
        var end = endDate ?? DateTime.Today;

        var registrationFeeTotal = await _reportRepository.GetRegistrationFeeTotalAsync(start, end, cancellationToken);
        var medicineFeeTotal = await _reportRepository.GetMedicineFeeTotalAsync(start, end, cancellationToken);

        var dto = new DailyIncomeDto
        {
            TotalIncome = registrationFeeTotal + medicineFeeTotal,
            RegistrationFeeTotal = registrationFeeTotal,
            MedicineFeeTotal = medicineFeeTotal
        };

        return Success(dto, "查询成功");
    }

    [HttpGet("daily/consultations")]
    public async Task<IActionResult> GetDailyConsultations(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        CancellationToken cancellationToken = default)
    {
        var start = startDate ?? DateTime.Today;
        var end = endDate ?? DateTime.Today;

        var totalCount = await _reportRepository.GetConsultationCountAsync(start, end, cancellationToken);
        var byDoctor = await _reportRepository.GetConsultationsByDoctorAsync(start, end, cancellationToken);

        var dto = new DailyConsultationDto
        {
            TotalCount = totalCount,
            ByDoctor = byDoctor
        };

        return Success(dto, "查询成功");
    }

    [HttpGet("daily/herbs")]
    public async Task<IActionResult> GetDailyHerbs(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        CancellationToken cancellationToken = default)
    {
        var start = startDate ?? DateTime.Today;
        var end = endDate ?? DateTime.Today;

        var items = await _reportRepository.GetHerbUsageAsync(start, end, cancellationToken);

        var dto = new DailyHerbUsageDto { Items = items };

        return Success(dto, "查询成功");
    }
}
