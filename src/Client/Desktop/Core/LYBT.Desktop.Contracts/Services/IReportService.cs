using LYBT.Desktop.Contracts.Results;
using LYBT.Shared.Models.Contracts.Reports;
using System.Threading;

namespace LYBT.Desktop.Contracts.Services;

/// <summary>
/// 报表Service接口 - Desktop 端报表数据访问
/// 封装 IApiClient.Reports，VM 只注入本接口不直连 API 客户端
/// </summary>
public interface IReportService
{
    /// <summary>日收入报表</summary>
    Task<CommandResult<DailyIncomeDto>> GetDailyIncomeAsync(DateTime? startDate = null, DateTime? endDate = null, CancellationToken ct = default);

    /// <summary>日问诊报表</summary>
    Task<CommandResult<DailyConsultationDto>> GetDailyConsultationsAsync(DateTime? startDate = null, DateTime? endDate = null, CancellationToken ct = default);

    /// <summary>日药材用量报表</summary>
    Task<CommandResult<DailyHerbUsageDto>> GetDailyHerbUsageAsync(DateTime? startDate = null, DateTime? endDate = null, CancellationToken ct = default);
}
