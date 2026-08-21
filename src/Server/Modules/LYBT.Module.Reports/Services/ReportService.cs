using LYBT.Module.Reports.Infrastructure;
using LYBT.Module.Reports.Interfaces;
using LYBT.Shared.Models.Contracts.Reports;
using LYBT.Shared.Models.Enums;

namespace LYBT.Module.Reports.Services;

/// <summary>
/// 报表服务实现 — 聚合仓库查询并组装报表 DTO。
/// </summary>
public class ReportService : IReportService
{
    private readonly IReportRepository _reportRepository;

    public ReportService(IReportRepository reportRepository)
    {
        _reportRepository = reportRepository ?? throw new ArgumentNullException(nameof(reportRepository));
    }

    public async Task<DailyIncomeDto> GetDailyIncomeAsync(DateTime startDate, DateTime endDate, Guid? doctorIdFilter = null, CancellationToken cancellationToken = default)
    {
        var registrationFeeTotal = await _reportRepository.GetRegistrationFeeTotalAsync(startDate, endDate, doctorIdFilter, cancellationToken);
        var medicineFeeTotal = await _reportRepository.GetMedicineFeeTotalAsync(startDate, endDate, doctorIdFilter, cancellationToken);

        return new DailyIncomeDto
        {
            TotalIncome = registrationFeeTotal + medicineFeeTotal,
            RegistrationFeeTotal = registrationFeeTotal,
            MedicineFeeTotal = medicineFeeTotal
        };
    }

    public async Task<DailyConsultationDto> GetDailyConsultationsAsync(DateTime startDate, DateTime endDate, Guid? doctorIdFilter = null, CancellationToken cancellationToken = default)
    {
        var totalCount = await _reportRepository.GetConsultationCountAsync(startDate, endDate, doctorIdFilter, cancellationToken);
        var byDoctor = await _reportRepository.GetConsultationsByDoctorAsync(startDate, endDate, doctorIdFilter, cancellationToken);

        return new DailyConsultationDto
        {
            TotalCount = totalCount,
            ByDoctor = byDoctor
        };
    }

    public async Task<DailyHerbUsageDto> GetDailyHerbUsageAsync(DateTime startDate, DateTime endDate, Guid? doctorIdFilter = null, CancellationToken cancellationToken = default)
    {
        var items = await _reportRepository.GetHerbUsageAsync(startDate, endDate, doctorIdFilter, cancellationToken);

        return new DailyHerbUsageDto { Items = items };
    }

    public async Task<IncomeTrendDto> GetIncomeTrendAsync(DateTime startDate, DateTime endDate, ReportGranularity granularity, Guid? doctorIdFilter = null, CancellationToken cancellationToken = default)
    {
        var registrationByDay = await _reportRepository.GetRegistrationFeeByDayAsync(startDate, endDate, doctorIdFilter, cancellationToken);
        var medicineByDay = await _reportRepository.GetMedicineFeeByDayAsync(startDate, endDate, doctorIdFilter, cancellationToken);
        var buckets = ReportTimeBuckets.Build(startDate, endDate, granularity);

        var registration = Rollup(registrationByDay, buckets);
        var medicine = Rollup(medicineByDay, buckets);

        return new IncomeTrendDto
        {
            Labels = buckets.Select(b => b.Label).ToList(),
            Registration = registration,
            Medicine = medicine,
            Total = registration.Zip(medicine).Select(x => x.First + x.Second).ToList()
        };
    }

    public async Task<ConsultationTrendDto> GetConsultationTrendAsync(DateTime startDate, DateTime endDate, ReportGranularity granularity, Guid? doctorIdFilter = null, CancellationToken cancellationToken = default)
    {
        var countsByDay = await _reportRepository.GetConsultationCountByDayAsync(startDate, endDate, doctorIdFilter, cancellationToken);
        var buckets = ReportTimeBuckets.Build(startDate, endDate, granularity);

        return new ConsultationTrendDto
        {
            Labels = buckets.Select(b => b.Label).ToList(),
            Counts = buckets.Select(b => countsByDay.Where(d => d.Date >= b.Start && d.Date < b.EndExclusive).Sum(d => d.Count)).ToList()
        };
    }

    public async Task<List<DoctorPerformanceDto>> GetDoctorPerformanceAsync(DateTime startDate, DateTime endDate, Guid? doctorIdFilter = null, CancellationToken cancellationToken = default)
    {
        // 仅show当医生时：严格视为目标医生维度——Doctor 看本人时 Filter 会命中其所有医案，绩效点天然聚合
        var points = await _reportRepository.GetDoctorPerformanceAsync(startDate, endDate, doctorIdFilter, cancellationToken);

        return points
            .Select(p => new DoctorPerformanceDto
            {
                DoctorName = p.DoctorName,
                ConsultationCount = p.ConsultationCount,
                RegistrationFeeTotal = p.RegistrationFeeTotal,
                MedicineFeeTotal = p.MedicineFeeTotal,
                AveragePrescriptionPrice = p.PrescriptionCount > 0 ? Math.Round(p.MedicineFeeTotal / p.PrescriptionCount, 2) : 0
            })
            .ToList();
    }

    public async Task<List<HerbUsageItemDto>> GetHerbRankingAsync(DateTime startDate, DateTime endDate, int top, Guid? doctorIdFilter = null, CancellationToken cancellationToken = default)
    {
        return await _reportRepository.GetHerbRankingAsync(startDate, endDate, top, doctorIdFilter, cancellationToken);
    }

    public async Task<PatientFlowDto> GetPatientFlowAsync(DateTime startDate, DateTime endDate, ReportGranularity granularity, Guid? doctorIdFilter = null, CancellationToken cancellationToken = default)
    {
        var byDay = await _reportRepository.GetPatientFlowByDayAsync(startDate, endDate, doctorIdFilter, cancellationToken);
        var buckets = ReportTimeBuckets.Build(startDate, endDate, granularity);

        return new PatientFlowDto
        {
            Labels = buckets.Select(b => b.Label).ToList(),
            NewPatients = buckets.Select(b => byDay.Where(d => d.Date >= b.Start && d.Date < b.EndExclusive).Sum(d => d.NewPatients)).ToList(),
            ReturningPatients = buckets.Select(b => byDay.Where(d => d.Date >= b.Start && d.Date < b.EndExclusive).Sum(d => d.ReturningPatients)).ToList()
        };
    }

    /// <summary>
    /// 按时间桶汇总日级数值（仓库已按日聚合，桶汇总仅在少量日行上进行）。
    /// </summary>
    private static List<decimal> Rollup(List<ReportDayValueDto> days, List<ReportBucket> buckets)
    {
        return buckets.Select(b => days.Where(d => d.Date >= b.Start && d.Date < b.EndExclusive).Sum(d => d.Value)).ToList();
    }
}
