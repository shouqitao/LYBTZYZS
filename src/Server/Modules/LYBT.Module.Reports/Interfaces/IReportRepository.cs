using LYBT.Module.Reports.Infrastructure;
using LYBT.Shared.Models.Contracts.Reports;

namespace LYBT.Module.Reports.Interfaces;

public interface IReportRepository
{
    Task<decimal> GetRegistrationFeeTotalAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);
    Task<decimal> GetMedicineFeeTotalAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);
    Task<int> GetConsultationCountAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);
    Task<List<DoctorCountDto>> GetConsultationsByDoctorAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);
    Task<List<HerbUsageItemDto>> GetHerbUsageAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);

    /// <summary>
    /// 按日汇总挂号费收入。
    /// </summary>
    Task<List<ReportDayValueDto>> GetRegistrationFeeByDayAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);

    /// <summary>
    /// 按日汇总药费收入（完成的医案处方金额）。
    /// </summary>
    Task<List<ReportDayValueDto>> GetMedicineFeeByDayAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);

    /// <summary>
    /// 按日统计问诊数（完成的医案）。
    /// </summary>
    Task<List<ReportDayCountDto>> GetConsultationCountByDayAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);

    /// <summary>
    /// 医生绩效聚合（问诊数/挂号费/药费/处方数）。
    /// </summary>
    Task<List<DoctorPerformancePointDto>> GetDoctorPerformanceAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);

    /// <summary>
    /// 热门药材排行（按使用次数降序取前 top 名）。
    /// </summary>
    Task<List<HerbUsageItemDto>> GetHerbRankingAsync(DateTime startDate, DateTime endDate, int top, CancellationToken cancellationToken = default);

    /// <summary>
    /// 按日统计新/回头患者流量。
    /// </summary>
    Task<List<PatientFlowPointDto>> GetPatientFlowByDayAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);
}
