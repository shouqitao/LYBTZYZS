using LYBT.Desktop.Contracts.Api;
using LYBT.Desktop.Contracts.ApiClient;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Reports;

namespace LYBT.Desktop.Foundation.Http.Clients;

internal sealed class ReportsApiClient : IApiClientReports
{
    private readonly IReportsApi _api;

    public ReportsApiClient(IReportsApi api)
    {
        _api = api ?? throw new ArgumentNullException(nameof(api));
    }

    public Task<ApiResponse<DailyIncomeDto>> GetDailyIncomeAsync(DateTime? startDate = null, DateTime? endDate = null)
        => _api.GetDailyIncomeAsync(startDate, endDate);

    public Task<ApiResponse<DailyConsultationDto>> GetDailyConsultationsAsync(DateTime? startDate = null, DateTime? endDate = null)
        => _api.GetDailyConsultationsAsync(startDate, endDate);

    public Task<ApiResponse<DailyHerbUsageDto>> GetDailyHerbUsageAsync(DateTime? startDate = null, DateTime? endDate = null)
        => _api.GetDailyHerbUsageAsync(startDate, endDate);
}
