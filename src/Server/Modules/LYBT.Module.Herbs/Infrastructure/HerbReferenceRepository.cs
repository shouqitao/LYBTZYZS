using LYBT.Entities.Formulas;
using LYBT.Entities.Herbs;
using LYBT.Infrastructure.Data;
using LYBT.Infrastructure.Repositories;
using LYBT.Module.Herbs.Interfaces;
using LYBT.Shared.Models.Contracts.Herbs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LYBT.Module.Herbs.Infrastructure;

/// <summary>
/// 药材引用仓储实现。用于检查药材在处方和验方中的引用情况。
/// </summary>
internal class HerbReferenceRepository : BaseRepository<Herb>, IHerbReferenceRepository
{
    public HerbReferenceRepository(AppDbContext dbContext, ILogger<HerbReferenceRepository> logger)
        : base(dbContext, logger)
    {
    }

    public async Task<int> GetPrescriptionReferenceCountAsync(Guid herbId, CancellationToken ct = default)
    {
        return await _context.PrescriptionItems
            .CountAsync(pi => pi.HerbId == herbId, ct);
    }

    public async Task<int> GetFormulaReferenceCountAsync(Guid herbId, CancellationToken ct = default)
    {
        return await _context.Set<FormulaHerbItem>()
            .CountAsync(fhi => fhi.HerbId != null && fhi.HerbId == herbId, ct);
    }

    public async Task<List<PrescriptionReferenceDto>> GetRecentPrescriptionReferencesAsync(Guid herbId, int take, CancellationToken ct = default)
    {
        return await (
            from pi in _context.PrescriptionItems
            join p in _context.Prescriptions on pi.PrescriptionId equals p.Id
            join mc in _context.MedicalCases on p.MedicalCaseId equals mc.Id
            join patient in _context.Patients on mc.PatientId equals patient.Id
            where pi.HerbId == herbId
            orderby p.CreatedAt descending
            select new PrescriptionReferenceDto
            {
                PrescriptionId = p.Id,
                PrescriptionNumber = p.PrescriptionNumber ?? string.Empty,
                PatientName = patient.Name,
                CreatedAt = p.CreatedAt,
                Status = "已开具"
            })
            .Take(take)
            .ToListAsync(ct);
    }

    public async Task<Dictionary<Guid, int>> GetBatchPrescriptionReferenceCountsAsync(List<Guid> herbIds, CancellationToken ct = default)
    {
        if (herbIds == null || herbIds.Count == 0)
            return new Dictionary<Guid, int>();

        var counts = await _context.PrescriptionItems
            .Where(pi => herbIds.Contains(pi.HerbId))
            .GroupBy(pi => pi.HerbId)
            .Select(g => new { HerbId = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        return counts.ToDictionary(x => x.HerbId, x => x.Count);
    }

    public async Task<Dictionary<Guid, int>> GetBatchFormulaReferenceCountsAsync(List<Guid> herbIds, CancellationToken ct = default)
    {
        if (herbIds == null || herbIds.Count == 0)
            return new Dictionary<Guid, int>();

        var counts = await _context.Set<FormulaHerbItem>()
            .Where(fhi => fhi.HerbId != null && herbIds.Contains(fhi.HerbId!.Value))
            .GroupBy(fhi => fhi.HerbId!.Value)
            .Select(g => new { HerbId = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        return counts.ToDictionary(x => x.HerbId, x => x.Count);
    }
}
