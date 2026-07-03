using LYBT.Infrastructure.Data;
using LYBT.Module.Reports.Interfaces;
using LYBT.Shared.Models.Contracts.Reports;
using LYBT.Shared.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace LYBT.Module.Reports.Infrastructure;

/// <summary>
/// 报表仓储实现。封装报表只读数据访问逻辑。
/// </summary>
internal class ReportRepository : IReportRepository
{
    private readonly AppDbContext _context;

    public ReportRepository(AppDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    /// <inheritdoc/>
    public async Task<decimal> GetRegistrationFeeTotalAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        return await _context.Registrations
            .Where(r => !r.IsDeleted && r.CreatedAt >= startDate && r.CreatedAt < endDate.AddDays(1))
            .SumAsync(r => r.RegistrationFee, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<decimal> GetMedicineFeeTotalAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        var caseIds = await _context.MedicalCases
            .Where(mc => !mc.IsDeleted && mc.CreatedAt >= startDate && mc.CreatedAt < endDate.AddDays(1) && mc.CaseStatus == MedicalCaseStatus.Completed)
            .Select(mc => mc.Id)
            .ToListAsync(cancellationToken);

        if (caseIds.Count == 0)
            return 0;

        return await _context.Prescriptions
            .Where(p => caseIds.Contains(p.MedicalCaseId) && !p.IsDeleted)
            .SelectMany(p => p.Items)
            .SumAsync(pi => pi.Amount, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<int> GetConsultationCountAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        return await _context.MedicalCases
            .CountAsync(mc => !mc.IsDeleted && mc.CreatedAt >= startDate && mc.CreatedAt < endDate.AddDays(1) && mc.CaseStatus == MedicalCaseStatus.Completed, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<List<DoctorCountDto>> GetConsultationsByDoctorAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        return await _context.MedicalCases
            .Where(mc => !mc.IsDeleted && mc.CreatedAt >= startDate && mc.CreatedAt < endDate.AddDays(1) && mc.CaseStatus == MedicalCaseStatus.Completed)
            .GroupBy(mc => mc.DoctorName)
            .Select(g => new DoctorCountDto
            {
                DoctorName = g.Key,
                Count = g.Count()
            })
            .OrderByDescending(d => d.Count)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<List<HerbUsageItemDto>> GetHerbUsageAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        return await (
            from mc in _context.MedicalCases
            where !mc.IsDeleted && mc.CreatedAt >= startDate && mc.CreatedAt < endDate.AddDays(1) && mc.CaseStatus == MedicalCaseStatus.Completed
            join p in _context.Prescriptions on mc.Id equals p.MedicalCaseId
            where !p.IsDeleted
            from pi in p.Items
            group pi by pi.HerbName into g
            select new HerbUsageItemDto
            {
                HerbName = g.Key,
                UsageCount = g.Count(),
                TotalDosage = g.Sum(x => x.Dosage)
            })
            .OrderByDescending(h => h.UsageCount)
            .ToListAsync(cancellationToken);
    }
}


