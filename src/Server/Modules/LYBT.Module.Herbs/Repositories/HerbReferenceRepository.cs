using LYBT.Entities.Formulas;
using LYBT.Infrastructure.Data;
using LYBT.Module.Herbs.Interfaces;
using LYBT.Shared.Models.Contracts.Herbs;
using Microsoft.EntityFrameworkCore;

namespace LYBT.Module.Herbs.Repositories
{
    internal class HerbReferenceRepository : IHerbReferenceRepository
    {
        private readonly AppDbContext _dbContext;

        public HerbReferenceRepository(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<int> GetPrescriptionReferenceCountAsync(Guid herbId, CancellationToken ct = default)
        {
            return await _dbContext.PrescriptionItems
                .CountAsync(pi => pi.HerbId == herbId, ct);
        }

        public async Task<int> GetFormulaReferenceCountAsync(Guid herbId, CancellationToken ct = default)
        {
            return await _dbContext.Set<FormulaHerbItem>()
                .CountAsync(fhi => fhi.HerbId != null && fhi.HerbId == herbId, ct);
        }

        public async Task<List<PrescriptionReferenceDto>> GetRecentPrescriptionReferencesAsync(Guid herbId, int take, CancellationToken ct = default)
        {
            return await (
                from pi in _dbContext.PrescriptionItems
                join p in _dbContext.Prescriptions on pi.PrescriptionId equals p.Id
                join mc in _dbContext.MedicalCases on p.MedicalCaseId equals mc.Id
                join patient in _dbContext.Patients on mc.PatientId equals patient.Id
                where pi.HerbId == herbId
                orderby p.CreatedAt descending
                select new PrescriptionReferenceDto
                {
                    PrescriptionId = p.Id,
                    PrescriptionNumber = p.PrescriptionNumber ?? string.Empty,
                    PatientName = patient.Name,
                    CreatedAt = p.CreatedAt,
                    // T2-X8-09: IsPrinted 已迁移到 MedicalCase 层级
                    Status = mc.IsPrinted ? "已打印" : "未打印"
                })
                .Take(take)
                .ToListAsync(ct);
        }
    }
}
