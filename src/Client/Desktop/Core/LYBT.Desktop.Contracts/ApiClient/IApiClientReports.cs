using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Reports;

namespace LYBT.Desktop.Contracts.ApiClient;

public interface IApiClientReports
{
    Task<ApiResponse<DailyIncomeDto>> GetDailyIncomeAsync(DateTime? startDate = null, DateTime? endDate = null);

    Task<ApiResponse<DailyConsultationDto>> GetDailyConsultationsAsync(DateTime? startDate = null, DateTime? endDate = null);

    Task<ApiResponse<DailyHerbUsageDto>> GetDailyHerbUsageAsync(DateTime? startDate = null, DateTime? endDate = null);
}
