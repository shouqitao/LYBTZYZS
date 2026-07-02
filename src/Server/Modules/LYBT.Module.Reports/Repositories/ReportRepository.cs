using LYBT.Entities.MedicalCases;
using LYBT.Infrastructure.Data;
using LYBT.Infrastructure.Repositories;
using LYBT.Module.Reports.Interfaces;
using LYBT.Shared.Models.Contracts.Reports;
using LYBT.Shared.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LYBT.Module.Reports.Repositories;

internal class ReportRepository : BaseRepository<MedicalCase>, IReportRepository
{
    public ReportRepository(AppDbContext dbContext, ILogger<ReportRepository> logger)
        : base(dbContext, logger)
    {
    }

    public async Task<decimal> GetTodayRegistrationFeeTotalAsync(CancellationToken cancellationToken = default)
    {
        var today = DateTime.Today;
        return await _context.Registrations
            .Where(r => !r.IsDeleted && r.CreatedAt.Date == today)
            .SumAsync(r => r.RegistrationFee, cancellationToken);
    }

    public async Task<decimal> GetTodayMedicineFeeTotalAsync(CancellationToken cancellationToken = default)
    {
        var today = DateTime.Today;
        var todayCaseIds = await _context.MedicalCases
            .Where(mc => !mc.IsDeleted && mc.CreatedAt.Date == today && mc.CaseStatus == MedicalCaseStatus.Completed)
            .Select(mc => mc.Id)
            .ToListAsync(cancellationToken);

        if (todayCaseIds.Count == 0)
            return 0;

        return await _context.Prescriptions
            .Where(p => todayCaseIds.Contains(p.MedicalCaseId) && !p.IsDeleted)
            .SelectMany(p => p.Items)
            .SumAsync(pi => pi.Amount, cancellationToken);
    }

    public async Task<int> GetTodayConsultationCountAsync(CancellationToken cancellationToken = default)
    {
        var today = DateTime.Today;
        return await _context.MedicalCases
            .CountAsync(mc => !mc.IsDeleted && mc.CreatedAt.Date == today && mc.CaseStatus == MedicalCaseStatus.Completed, cancellationToken);
    }

    public async Task<List<DoctorCountDto>> GetTodayConsultationsByDoctorAsync(CancellationToken cancellationToken = default)
    {
        var today = DateTime.Today;
        return await _context.MedicalCases
            .Where(mc => !mc.IsDeleted && mc.CreatedAt.Date == today && mc.CaseStatus == MedicalCaseStatus.Completed)
            .GroupBy(mc => mc.DoctorName)
            .Select(g => new DoctorCountDto
            {
                DoctorName = g.Key,
                Count = g.Count()
            })
            .OrderByDescending(d => d.Count)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<HerbUsageItemDto>> GetTodayHerbUsageAsync(CancellationToken cancellationToken = default)
    {
        var today = DateTime.Today;
        return await (
            from mc in _context.MedicalCases
            where !mc.IsDeleted && mc.CreatedAt.Date == today && mc.CaseStatus == MedicalCaseStatus.Completed
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


