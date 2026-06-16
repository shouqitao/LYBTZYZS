using LYBT.Module.Reports.Interfaces;
using LYBT.Shared.Models.Contracts.Reports;

namespace LYBT.Module.Reports.Services;

internal class ReportService : IReportService
{
    private readonly IReportRepository _reportRepository;

    public ReportService(IReportRepository reportRepository)
    {
        _reportRepository = reportRepository;
    }

    public async Task<DailyIncomeDto> GetDailyIncomeAsync(CancellationToken cancellationToken = default)
    {
        var registrationFeeTotal = await _reportRepository.GetTodayRegistrationFeeTotalAsync(cancellationToken);
        var medicineFeeTotal = await _reportRepository.GetTodayMedicineFeeTotalAsync(cancellationToken);

        return new DailyIncomeDto
        {
            RegistrationFeeTotal = registrationFeeTotal,
            MedicineFeeTotal = medicineFeeTotal,
            TotalIncome = registrationFeeTotal + medicineFeeTotal
        };
    }

    public async Task<DailyConsultationDto> GetDailyConsultationsAsync(CancellationToken cancellationToken = default)
    {
        var totalCount = await _reportRepository.GetTodayConsultationCountAsync(cancellationToken);
        var byDoctor = await _reportRepository.GetTodayConsultationsByDoctorAsync(cancellationToken);

        return new DailyConsultationDto
        {
            TotalCount = totalCount,
            ByDoctor = byDoctor
        };
    }

    public async Task<DailyHerbUsageDto> GetDailyHerbUsageAsync(CancellationToken cancellationToken = default)
    {
        var items = await _reportRepository.GetTodayHerbUsageAsync(cancellationToken);

        return new DailyHerbUsageDto
        {
            Items = items
        };
    }
}
