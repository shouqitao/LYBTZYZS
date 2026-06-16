using LYBT.Shared.Models.Contracts.Reports;

namespace LYBT.Module.Reports.Interfaces;

public interface IReportService
{
    Task<DailyIncomeDto> GetDailyIncomeAsync(CancellationToken cancellationToken = default);
    Task<DailyConsultationDto> GetDailyConsultationsAsync(CancellationToken cancellationToken = default);
    Task<DailyHerbUsageDto> GetDailyHerbUsageAsync(CancellationToken cancellationToken = default);
}
