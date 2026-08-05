using LYBT.Module.Reports.Interfaces;
using LYBT.Shared.Models.Contracts.Reports;

namespace LYBT.Module.Reports.Services;

/// <summary>
/// 报表服务实现 — 聚合仓库查询并组装报表 DTO。
/// </summary>
internal class ReportService : IReportService
{
    private readonly IReportRepository _reportRepository;

    public ReportService(IReportRepository reportRepository)
    {
        _reportRepository = reportRepository ?? throw new ArgumentNullException(nameof(reportRepository));
    }

    public async Task<DailyIncomeDto> GetDailyIncomeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        var registrationFeeTotal = await _reportRepository.GetRegistrationFeeTotalAsync(startDate, endDate, cancellationToken);
        var medicineFeeTotal = await _reportRepository.GetMedicineFeeTotalAsync(startDate, endDate, cancellationToken);

        return new DailyIncomeDto
        {
            TotalIncome = registrationFeeTotal + medicineFeeTotal,
            RegistrationFeeTotal = registrationFeeTotal,
            MedicineFeeTotal = medicineFeeTotal
        };
    }

    public async Task<DailyConsultationDto> GetDailyConsultationsAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        var totalCount = await _reportRepository.GetConsultationCountAsync(startDate, endDate, cancellationToken);
        var byDoctor = await _reportRepository.GetConsultationsByDoctorAsync(startDate, endDate, cancellationToken);

        return new DailyConsultationDto
        {
            TotalCount = totalCount,
            ByDoctor = byDoctor
        };
    }

    public async Task<DailyHerbUsageDto> GetDailyHerbUsageAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        var items = await _reportRepository.GetHerbUsageAsync(startDate, endDate, cancellationToken);

        return new DailyHerbUsageDto { Items = items };
    }
}
