using LYBT.Infrastructure.Data;
using LYBT.Module.Reports.Interfaces;
using LYBT.Shared.Models.Contracts.Reports;
using LYBT.Shared.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace LYBT.Module.Reports.Infrastructure;

/// <summary>
/// 报表仓储实现。封装报表只读数据访问逻辑。
/// </summary>
public class ReportRepository : IReportRepository
{
    private readonly AppDbContext _context;

    public ReportRepository(AppDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    /// <inheritdoc/>
    public async Task<decimal> GetRegistrationFeeTotalAsync(
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default
    )
    {
        return await _context
            .Registrations.Where(r =>
                !r.IsDeleted && r.CreatedAt >= startDate && r.CreatedAt < endDate.AddDays(1)
            )
            .SumAsync(r => r.RegistrationFee, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<decimal> GetMedicineFeeTotalAsync(
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default
    )
    {
        var caseIds = await _context
            .MedicalCases.Where(mc =>
                !mc.IsDeleted
                && mc.CreatedAt >= startDate
                && mc.CreatedAt < endDate.AddDays(1)
                && mc.CaseStatus == MedicalCaseStatus.Completed
            )
            .Select(mc => mc.Id)
            .ToListAsync(cancellationToken);

        if (caseIds.Count == 0)
            return 0;

        return await _context
            .Prescriptions.Where(p => caseIds.Contains(p.MedicalCaseId) && !p.IsDeleted)
            .SelectMany(p => p.Items)
            .SumAsync(pi => pi.UnitPrice * pi.Dosage, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<int> GetConsultationCountAsync(
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default
    )
    {
        return await _context.MedicalCases.CountAsync(
            mc =>
                !mc.IsDeleted
                && mc.CreatedAt >= startDate
                && mc.CreatedAt < endDate.AddDays(1)
                && mc.CaseStatus == MedicalCaseStatus.Completed,
            cancellationToken
        );
    }

    /// <inheritdoc/>
    public async Task<List<DoctorCountDto>> GetConsultationsByDoctorAsync(
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default
    )
    {
        return await _context
            .MedicalCases.Where(mc =>
                !mc.IsDeleted
                && mc.CreatedAt >= startDate
                && mc.CreatedAt < endDate.AddDays(1)
                && mc.CaseStatus == MedicalCaseStatus.Completed
            )
            .GroupBy(mc => mc.DoctorName)
            .Select(g => new DoctorCountDto { DoctorName = g.Key, Count = g.Count() })
            .OrderByDescending(d => d.Count)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<List<HerbUsageItemDto>> GetHerbUsageAsync(
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default
    )
    {
        return await HerbUsageQuery(startDate, endDate)
            .OrderByDescending(h => h.UsageCount)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<List<ReportDayValueDto>> GetRegistrationFeeByDayAsync(
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default
    )
    {
        return await _context
            .Registrations.Where(r =>
                !r.IsDeleted && r.CreatedAt >= startDate && r.CreatedAt < endDate.AddDays(1)
            )
            .GroupBy(r => r.CreatedAt.Date)
            .Select(g => new ReportDayValueDto(g.Key, g.Sum(r => r.RegistrationFee)))
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<List<ReportDayValueDto>> GetMedicineFeeByDayAsync(
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default
    )
    {
        return await (
            from mc in _context.MedicalCases
            where
                !mc.IsDeleted
                && mc.CreatedAt >= startDate
                && mc.CreatedAt < endDate.AddDays(1)
                && mc.CaseStatus == MedicalCaseStatus.Completed
            join p in _context.Prescriptions on mc.Id equals p.MedicalCaseId
            where !p.IsDeleted
            join pi in _context.PrescriptionItems on p.Id equals pi.PrescriptionId
            group pi by mc.CreatedAt.Date into g
            select new ReportDayValueDto(g.Key, g.Sum(x => x.UnitPrice * x.Dosage))
        ).ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<List<ReportDayCountDto>> GetConsultationCountByDayAsync(
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default
    )
    {
        return await _context
            .MedicalCases.Where(mc =>
                !mc.IsDeleted
                && mc.CreatedAt >= startDate
                && mc.CreatedAt < endDate.AddDays(1)
                && mc.CaseStatus == MedicalCaseStatus.Completed
            )
            .GroupBy(mc => mc.CreatedAt.Date)
            .Select(g => new ReportDayCountDto(g.Key, g.Count()))
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<List<DoctorPerformancePointDto>> GetDoctorPerformanceAsync(
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default
    )
    {
        var cases = _context.MedicalCases.Where(mc =>
            !mc.IsDeleted
            && mc.CreatedAt >= startDate
            && mc.CreatedAt < endDate.AddDays(1)
            && mc.CaseStatus == MedicalCaseStatus.Completed
        );

        // 单次查询（P1-2 2026-08-14）: 以医案为驱动按医生分组，各指标用相关子查询聚合（挂号费/药费/处方数）——
        // 一次 SQL 完成全部统计，避免 4 次独立查询；相关子查询无多表 fan-out 失真。
        // 注: 不用 LEFT JOIN 预聚合子查询——EF InMemory 对子查询聚合 + LEFT JOIN 外层 Sum 翻译缺陷
        //（Nullable must have value）；相关子查询模式 InMemory/SQL Server 均可翻译。
        var query =
            from mc in cases
            group mc by mc.DoctorName into g
            select new
            {
                DoctorName = g.Key,
                ConsultationCount = g.Count(),
                RegistrationFeeTotal = g.SelectMany(c =>
                        _context.Registrations.Where(r => r.MedicalCaseId == c.Id && !r.IsDeleted)
                    )
                    .Sum(r => (decimal?)r.RegistrationFee)
                    ?? 0m,
                MedicineFeeTotal = g.SelectMany(c =>
                        _context.Prescriptions.Where(p => p.MedicalCaseId == c.Id && !p.IsDeleted)
                    )
                    .SelectMany(p =>
                        _context.PrescriptionItems.Where(pi => pi.PrescriptionId == p.Id)
                    )
                    .Sum(pi => (decimal?)(pi.UnitPrice * pi.Dosage))
                    ?? 0m,
                PrescriptionCount = g.SelectMany(c =>
                        _context.Prescriptions.Where(p => p.MedicalCaseId == c.Id && !p.IsDeleted)
                    )
                    .Count(),
            };

        var result = await query.ToListAsync(cancellationToken);
        return result
            .Select(x => new DoctorPerformancePointDto(
                x.DoctorName,
                x.ConsultationCount,
                x.RegistrationFeeTotal,
                x.MedicineFeeTotal,
                x.PrescriptionCount
            ))
            .OrderByDescending(d => d.ConsultationCount)
            .ToList();
    }

    /// <inheritdoc/>
    public async Task<List<HerbUsageItemDto>> GetHerbRankingAsync(
        DateTime startDate,
        DateTime endDate,
        int top,
        CancellationToken cancellationToken = default
    )
    {
        return await HerbUsageQuery(startDate, endDate)
            .OrderByDescending(h => h.UsageCount)
            .Take(top)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<List<PatientFlowPointDto>> GetPatientFlowByDayAsync(
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default
    )
    {
        var patientDays = await _context
            .MedicalCases.Where(mc =>
                !mc.IsDeleted
                && mc.CreatedAt >= startDate
                && mc.CreatedAt < endDate.AddDays(1)
                && mc.CaseStatus == MedicalCaseStatus.Completed
            )
            .Select(mc => new { mc.PatientId, Day = mc.CreatedAt.Date })
            .Distinct()
            .ToListAsync(cancellationToken);

        if (patientDays.Count == 0)
            return [];

        var patientIds = patientDays.Select(x => x.PatientId).Distinct().ToList();
        var firstDayByPatient = await _context
            .MedicalCases.Where(mc =>
                !mc.IsDeleted
                && mc.CaseStatus == MedicalCaseStatus.Completed
                && patientIds.Contains(mc.PatientId)
            )
            .GroupBy(mc => mc.PatientId)
            .Select(g => new { PatientId = g.Key, FirstDay = g.Min(mc => mc.CreatedAt).Date })
            .ToDictionaryAsync(x => x.PatientId, x => x.FirstDay, cancellationToken);

        return patientDays
            .GroupBy(x => x.Day)
            .OrderBy(g => g.Key)
            .Select(g => new PatientFlowPointDto(
                g.Key,
                g.Count(x => firstDayByPatient[x.PatientId] == g.Key),
                g.Count(x => firstDayByPatient[x.PatientId] < g.Key)
            ))
            .ToList();
    }

    /// <summary>
    /// 药材使用聚合公共查询（排行/日统计共用，聚合下推数据库执行）。
    /// </summary>
    private IQueryable<HerbUsageItemDto> HerbUsageQuery(DateTime startDate, DateTime endDate)
    {
        return from mc in _context.MedicalCases
            where
                !mc.IsDeleted
                && mc.CreatedAt >= startDate
                && mc.CreatedAt < endDate.AddDays(1)
                && mc.CaseStatus == MedicalCaseStatus.Completed
            join p in _context.Prescriptions on mc.Id equals p.MedicalCaseId
            where !p.IsDeleted
            join pi in _context.PrescriptionItems on p.Id equals pi.PrescriptionId
            group pi by pi.HerbName into g
            select new HerbUsageItemDto
            {
                HerbName = g.Key,
                UsageCount = g.Count(),
                TotalDosage = g.Sum(x => x.Dosage),
            };
    }
}
