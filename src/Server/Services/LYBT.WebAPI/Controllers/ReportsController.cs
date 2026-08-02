using Asp.Versioning;
using LYBT.Infrastructure.Constants;
using LYBT.Infrastructure.Web;
using LYBT.Module.Reports.Interfaces;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Reports;
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
    private readonly IReportRepository _reportRepository;

    public ReportsController(
        IReportRepository reportRepository,
        ILogger<ReportsController> logger)
        : base(logger)
    {
        _reportRepository = reportRepository;
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
        var end = endDate ?? DateTime.Today;

        var items = await _reportRepository.GetHerbUsageAsync(start, end, cancellationToken);

        var dto = new DailyHerbUsageDto { Items = items };

        return Success(dto, "查询成功");
    }
}
