using LYBT.Entities.Formulas;
using LYBT.Entities.Herbs;
using LYBT.Entities.Patients;
using LYBT.Infrastructure.Data;
using LYBT.Infrastructure.Repositories;
using LYBT.Module.Sync.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LYBT.Module.Sync.Repositories;

internal class SyncRepository : BaseRepository<Herb>, ISyncRepository
{
    public SyncRepository(AppDbContext dbContext, ILogger<SyncRepository> logger)
        : base(dbContext, logger)
    {
    }

    public Task<List<Herb>> GetAllHerbsIncludingDeletedAsync(CancellationToken ct = default)
        => _context.Herbs.IgnoreQueryFilters().AsNoTracking().ToListAsync(ct);

    public Task<List<Patient>> GetAllPatientsIncludingDeletedAsync(CancellationToken ct = default)
        => _context.Patients.IgnoreQueryFilters().AsNoTracking().ToListAsync(ct);

    public Task<List<Formula>> GetAllFormulasWithHerbsIncludingDeletedAsync(CancellationToken ct = default)
        => _context.Formulas.Include(f => f.Herbs).IgnoreQueryFilters().AsNoTracking().ToListAsync(ct);

    public Task<Herb?> FindHerbAsync(Guid id)
        => _context.Herbs.IgnoreQueryFilters().FirstOrDefaultAsync(h => h.Id == id);

    public Task<Patient?> FindPatientAsync(Guid id)
        => _context.Patients.IgnoreQueryFilters().FirstOrDefaultAsync(p => p.Id == id);

    public Task<Formula?> FindFormulaWithHerbsAsync(Guid id)
        => _context.Formulas.Include(f => f.Herbs).IgnoreQueryFilters().FirstOrDefaultAsync(f => f.Id == id);

    public void UpdateHerbValues(Herb existing, Herb incoming)
        => _context.Entry(existing).CurrentValues.SetValues(incoming);

    public void UpdatePatientValues(Patient existing, Patient incoming)
        => _context.Entry(existing).CurrentValues.SetValues(incoming);

    public void UpdateFormulaValues(Formula existing, Formula incoming)
        => _context.Entry(existing).CurrentValues.SetValues(incoming);

    public void AddHerb(Herb herb) => _context.Herbs.Add(herb);

    public void AddPatient(Patient patient) => _context.Patients.Add(patient);

    public void AddFormula(Formula formula) => _context.Formulas.Add(formula);

    public void RemoveFormulaHerbs(ICollection<FormulaHerbItem> herbs)
        => _context.RemoveRange(herbs);

    public void AddFormulaHerbItem(FormulaHerbItem item) => _context.Add(item);

    public Task<Herb?> GetHerbByIdNoTrackingAsync(Guid id, CancellationToken ct = default)
        => _context.Herbs.AsNoTracking().FirstOrDefaultAsync(h => h.Id == id, ct);

    public Task<Patient?> GetPatientByIdNoTrackingAsync(Guid id, CancellationToken ct = default)
        => _context.Patients.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id, ct);

    public Task<Formula?> GetFormulaWithHerbsByIdNoTrackingAsync(Guid id, CancellationToken ct = default)
        => _context.Formulas.Include(f => f.Herbs).AsNoTracking().FirstOrDefaultAsync(f => f.Id == id, ct);

    public async Task<bool> SoftDeleteHerbAsync(Guid id, CancellationToken ct = default)
    {
        var entity = await _context.Herbs.IgnoreQueryFilters().FirstOrDefaultAsync(h => h.Id == id, ct);
        if (entity is null) return false;
        entity.IsDeleted = true;
        return true;
    }

    public async Task<bool> SoftDeletePatientAsync(Guid id, CancellationToken ct = default)
    {
        var entity = await _context.Patients.IgnoreQueryFilters().FirstOrDefaultAsync(p => p.Id == id, ct);
        if (entity is null) return false;
        entity.IsDeleted = true;
        return true;
    }

    public async Task<bool> SoftDeleteFormulaAsync(Guid id, CancellationToken ct = default)
    {
        var entity = await _context.Formulas.IgnoreQueryFilters().FirstOrDefaultAsync(f => f.Id == id, ct);
        if (entity is null) return false;
        entity.IsDeleted = true;
        return true;
    }

    public override Task<int> SaveChangesAsync(CancellationToken ct = default)
        => _context.SaveChangesAsync(ct);
}
