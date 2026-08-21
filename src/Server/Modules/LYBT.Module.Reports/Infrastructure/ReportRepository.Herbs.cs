using LYBT.Infrastructure.Data;
using LYBT.Module.Reports.Interfaces;
using LYBT.Shared.Models.Contracts.Reports;
using LYBT.Shared.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace LYBT.Module.Reports.Infrastructure;

/// <summary>
/// 报表仓储 — 药材主题 partial（T4.3：从 ReportRepository 拆出，含排行/日统计共用聚合查询）。
/// </summary>
public partial class ReportRepository
{
    /// <inheritdoc/>
    public async Task<List<HerbUsageItemDto>> GetHerbUsageAsync(
        DateTime startDate,
        DateTime endDate,
        Guid? doctorIdFilter = null,
        CancellationToken cancellationToken = default
    )
    {
        return await HerbUsageQuery(startDate, endDate, doctorIdFilter)
            .OrderByDescending(h => h.UsageCount)
            .ToListAsync(cancellationToken);
    }


    /// <inheritdoc/>
    public async Task<List<HerbUsageItemDto>> GetHerbRankingAsync(
        DateTime startDate,
        DateTime endDate,
        int top,
        Guid? doctorIdFilter = null,
        CancellationToken cancellationToken = default
    )
    {
        return await HerbUsageQuery(startDate, endDate, doctorIdFilter)
            .OrderByDescending(h => h.UsageCount)
            .Take(top)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<List<PatientFlowPointDto>> GetPatientFlowByDayAsync(
        DateTime startDate,
        DateTime endDate,
        Guid? doctorIdFilter = null,
        CancellationToken cancellationToken = default
    )
    {
        var patientDays = await _context
            .MedicalCases.Where(mc =>
                !mc.IsDeleted
                && mc.CreatedAt >= startDate
                && mc.CreatedAt < endDate.AddDays(1)
                && mc.CaseStatus == MedicalCaseStatus.Completed
                && (!doctorIdFilter.HasValue || mc.UserId == doctorIdFilter.Value)
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
    private IQueryable<HerbUsageItemDto> HerbUsageQuery(DateTime startDate, DateTime endDate, Guid? doctorIdFilter = null)
    {
        return from mc in _context.MedicalCases
            where
                !mc.IsDeleted
                && mc.CreatedAt >= startDate
                && mc.CreatedAt < endDate.AddDays(1)
                && mc.CaseStatus == MedicalCaseStatus.Completed
                && (!doctorIdFilter.HasValue || mc.UserId == doctorIdFilter.Value)
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
