using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Reports;

namespace LYBT.Desktop.Contracts.Api;

internal interface IReportsApi
{
    [Refit.Get("/api/v1/reports/daily/income")]
    Task<ApiResponse<DailyIncomeDto>> GetDailyIncomeAsync(
        [Refit.Query] DateTime? startDate = null,
        [Refit.Query] DateTime? endDate = null,
        CancellationToken ct = default);

    [Refit.Get("/api/v1/reports/daily/consultations")]
    Task<ApiResponse<DailyConsultationDto>> GetDailyConsultationsAsync(
        [Refit.Query] DateTime? startDate = null,
        [Refit.Query] DateTime? endDate = null,
        CancellationToken ct = default);

    [Refit.Get("/api/v1/reports/daily/herbs")]
    Task<ApiResponse<DailyHerbUsageDto>> GetDailyHerbUsageAsync(
        [Refit.Query] DateTime? startDate = null,
        [Refit.Query] DateTime? endDate = null,
        CancellationToken ct = default);
}
