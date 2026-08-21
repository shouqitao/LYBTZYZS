using LYBT.Shared.Models.Contracts.Reports;
using LYBT.Shared.Models.Enums;

namespace LYBT.Module.Reports.Interfaces;

/// <summary>
/// 报表服务接口 — 封装报表只读聚合查询。
/// </summary>
public interface IReportService
{
    // P1-23: doctorIdFilter 可为 null（Admin 全量）或当前医生 ID（Doctor 仅本人）
    Task<DailyIncomeDto> GetDailyIncomeAsync(DateTime startDate, DateTime endDate, Guid? doctorIdFilter = null, CancellationToken cancellationToken = default);
    Task<DailyConsultationDto> GetDailyConsultationsAsync(DateTime startDate, DateTime endDate, Guid? doctorIdFilter = null, CancellationToken cancellationToken = default);
    Task<DailyHerbUsageDto> GetDailyHerbUsageAsync(DateTime startDate, DateTime endDate, Guid? doctorIdFilter = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 收入趋势（挂号费/药费/合计折线数据）。
    /// </summary>
    Task<IncomeTrendDto> GetIncomeTrendAsync(DateTime startDate, DateTime endDate, ReportGranularity granularity, Guid? doctorIdFilter = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 问诊趋势（折线数据）。
    /// </summary>
    Task<ConsultationTrendDto> GetConsultationTrendAsync(DateTime startDate, DateTime endDate, ReportGranularity granularity, Guid? doctorIdFilter = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 医生绩效（问诊数/挂号费/药费/平均处方金额）。
    /// </summary>
    Task<List<DoctorPerformanceDto>> GetDoctorPerformanceAsync(DateTime startDate, DateTime endDate, Guid? doctorIdFilter = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 热门药材排行（按使用次数降序取前 top 名）。
    /// </summary>
    Task<List<HerbUsageItemDto>> GetHerbRankingAsync(DateTime startDate, DateTime endDate, int top = 10, Guid? doctorIdFilter = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 患者流量（新患者/回头患者折线数据）。
    /// </summary>
    Task<PatientFlowDto> GetPatientFlowAsync(DateTime startDate, DateTime endDate, ReportGranularity granularity, Guid? doctorIdFilter = null, CancellationToken cancellationToken = default);
}
