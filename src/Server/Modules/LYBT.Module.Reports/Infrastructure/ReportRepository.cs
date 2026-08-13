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

        // 合并为单次查询：以医案为驱动表，挂号/药费/处方数先按 MedicalCaseId 预聚合（每医案一行），
        // 再 LEFT JOIN 到医案，最后 GroupBy DoctorName 一次得出全部指标——
        // 避免多表直接 JOIN 的行数放大（fan-out）导致求和/计数失真。
        var query =
            from mc in cases
            join rg in (
                from r in _context.Registrations
                where !r.IsDeleted
                group r by r.MedicalCaseId into g
                select new
                {
                    MedicalCaseId = g.Key,
                    RegistrationFeeTotal = g.Sum(x => x.RegistrationFee),
                }
            )
                on mc.Id equals rg.MedicalCaseId
                into registrationJoin
            from rg in registrationJoin.DefaultIfEmpty()
            join mg in (
                from p in _context.Prescriptions
                where !p.IsDeleted
                join pi in _context.PrescriptionItems on p.Id equals pi.PrescriptionId
                group pi by p.MedicalCaseId into g
                select new
                {
                    MedicalCaseId = g.Key,
                    MedicineFeeTotal = g.Sum(x => x.UnitPrice * x.Dosage),
                }
            )
                on mc.Id equals mg.MedicalCaseId
                into medicineJoin
            from mg in medicineJoin.DefaultIfEmpty()
            join pc in (
                from p in _context.Prescriptions
                where !p.IsDeleted
                group p by p.MedicalCaseId into g
                select new { MedicalCaseId = g.Key, PrescriptionCount = g.Count() }
            )
                on mc.Id equals pc.MedicalCaseId
                into prescriptionJoin
            from pc in prescriptionJoin.DefaultIfEmpty()
            group new
            {
                mc,
                rg,
                mg,
                pc,
            } by mc.DoctorName into g
            select new DoctorPerformancePointDto(
                g.Key,
                g.Count(),
                g.Sum(x => x.rg != null ? x.rg.RegistrationFeeTotal : 0),
                g.Sum(x => x.mg != null ? x.mg.MedicineFeeTotal : 0),
                g.Sum(x => x.pc != null ? x.pc.PrescriptionCount : 0)
            );

        // 单次查询（P1-2 2026-08-14）: 预聚合子查询（每医案一行）LEFT JOIN 避免多表 fan-out 失真。
        // 按医生分组聚合一次得出全部指标；排序在内存（分组后行数 = 医生数，量小）。
        var result = await query.ToListAsync(cancellationToken);
        return result.OrderByDescending(d => d.ConsultationCount).ToList();
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
