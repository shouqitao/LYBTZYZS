using LYBT.Desktop.Contracts.Api;
using LYBT.Desktop.Contracts.ApiClient;
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

    public Task<ApiResponse<DailyIncomeDto>> GetDailyIncomeAsync()
        => _api.GetDailyIncomeAsync();

    public Task<ApiResponse<DailyConsultationDto>> GetDailyConsultationsAsync()
        => _api.GetDailyConsultationsAsync();

    public Task<ApiResponse<DailyHerbUsageDto>> GetDailyHerbUsageAsync()
        => _api.GetDailyHerbUsageAsync();
}
