using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Reports;

namespace LYBT.Desktop.Contracts.Repositories;

/// <summary>
/// 报表数据仓储接口（P0-2：报表 Service 统一走 Repository 层，不直连 IApiClient.Reports）。
/// 只读聚合，镜像 Server 侧 IReportRepository 命名——不同程序集，语义不同不可互换。
/// </summary>
public interface IReportRepository
{
    /// <summary>日收入报表。</summary>
    Task<ApiResponse<DailyIncomeDto>> GetDailyIncomeAsync(DateTime? startDate = null, DateTime? endDate = null);

    /// <summary>日就诊报表。</summary>
    Task<ApiResponse<DailyConsultationDto>> GetDailyConsultationsAsync(DateTime? startDate = null, DateTime? endDate = null);

    /// <summary>日药材用量报表。</summary>
    Task<ApiResponse<DailyHerbUsageDto>> GetDailyHerbUsageAsync(DateTime? startDate = null, DateTime? endDate = null);
}
