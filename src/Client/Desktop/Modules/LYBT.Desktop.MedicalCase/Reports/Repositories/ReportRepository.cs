using LYBT.Desktop.Contracts.ApiClient;
using LYBT.Desktop.Contracts.Repositories;
using LYBT.Desktop.Foundation.Repositories;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Reports;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.MedicalCase.Reports.Repositories;

/// <summary>
/// 报表数据仓储 — 封装 IApiClient.Reports 报表域端点（L2 层，P0-2）。
/// 只读聚合，无自有表；无状态，Singleton 注册安全。
/// 继承 ApiClientRepositoryBase 以复用统一 try/catch + 日志模板（架构守卫 A02）。
/// </summary>
public sealed class ReportRepository : ApiClientRepositoryBase<object, object>, IReportRepository
{
    private readonly IApiClient _apiClient;

    public ReportRepository(IApiClient apiClient, ILogger<ReportRepository> logger)
        : base(logger)
    {
        _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
    }

    /// <inheritdoc />
    public Task<ApiResponse<DailyIncomeDto>> GetDailyIncomeAsync(DateTime? startDate = null, DateTime? endDate = null, CancellationToken ct = default)
        => _apiClient.Reports.GetDailyIncomeAsync(startDate, endDate, ct);

    /// <inheritdoc />
    public Task<ApiResponse<DailyConsultationDto>> GetDailyConsultationsAsync(DateTime? startDate = null, DateTime? endDate = null, CancellationToken ct = default)
        => _apiClient.Reports.GetDailyConsultationsAsync(startDate, endDate, ct);

    /// <inheritdoc />
    public Task<ApiResponse<DailyHerbUsageDto>> GetDailyHerbUsageAsync(DateTime? startDate = null, DateTime? endDate = null, CancellationToken ct = default)
        => _apiClient.Reports.GetDailyHerbUsageAsync(startDate, endDate, ct);
}
