using LYBT.Shared.Models.Contracts.Reports;

namespace LYBT.Module.Reports.Interfaces;

/// <summary>
/// 报表服务接口 — 封装报表只读聚合查询。
/// </summary>
public interface IReportService
{
    Task<DailyIncomeDto> GetDailyIncomeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);
    Task<DailyConsultationDto> GetDailyConsultationsAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);
    Task<DailyHerbUsageDto> GetDailyHerbUsageAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);
}
