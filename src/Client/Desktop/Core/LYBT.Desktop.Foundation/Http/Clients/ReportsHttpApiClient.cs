// ---------------------------------------------------------------------------
// ReportsHttpApiClient — HttpClient adapter for IApiClientReports
// ---------------------------------------------------------------------------
// LocalWebAPI mode implementation of IApiClientReports (split from HttpClientApiClient).
// Part of the IApiClient unified abstraction layer.
// ---------------------------------------------------------------------------

using LYBT.Desktop.Contracts.ApiClient;
using Microsoft.Extensions.Logging;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Reports;

namespace LYBT.Desktop.Foundation.Http.Clients;

/// <summary>本地模式报表 API 客户端（拆分自 HttpClientApiClient）</summary>
internal sealed class ReportsHttpApiClient : HttpApiClientBase, IApiClientReports
{
    public ReportsHttpApiClient(IHttpClientFactory httpClientFactory, ILogger logger) : base(httpClientFactory, logger) { }

    public Task<ApiResponse<DailyIncomeDto>> GetDailyIncomeAsync(DateTime? startDate, DateTime? endDate)
        => GetAndWrapAsync<DailyIncomeDto>($"/api/v1/reports/daily/income?startDate={startDate:yyyy-MM-dd}&endDate={endDate:yyyy-MM-dd}");

    public Task<ApiResponse<DailyConsultationDto>> GetDailyConsultationsAsync(DateTime? startDate, DateTime? endDate)
        => GetAndWrapAsync<DailyConsultationDto>($"/api/v1/reports/daily/consultations?startDate={startDate:yyyy-MM-dd}&endDate={endDate:yyyy-MM-dd}");

    public Task<ApiResponse<DailyHerbUsageDto>> GetDailyHerbUsageAsync(DateTime? startDate, DateTime? endDate)
        => GetAndWrapAsync<DailyHerbUsageDto>($"/api/v1/reports/daily/herbs?startDate={startDate:yyyy-MM-dd}&endDate={endDate:yyyy-MM-dd}");
}
