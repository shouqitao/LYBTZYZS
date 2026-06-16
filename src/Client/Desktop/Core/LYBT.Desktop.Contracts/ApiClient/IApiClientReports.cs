using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Reports;

namespace LYBT.Desktop.Contracts.ApiClient;

public interface IApiClientReports
{
    Task<ApiResponse<DailyIncomeDto>> GetDailyIncomeAsync();

    Task<ApiResponse<DailyConsultationDto>> GetDailyConsultationsAsync();

    Task<ApiResponse<DailyHerbUsageDto>> GetDailyHerbUsageAsync();
}
