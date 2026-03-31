using LYBT.Entities.Formulas;
using LYBT.Entities.Herbs;
using LYBT.Entities.Patients;
using LYBT.Infrastructure.Data;
using LYBT.Module.Sync.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace LYBT.Module.Sync.Repositories;

internal class SyncRepository : ISyncRepository
{
    private readonly AppDbContext _dbContext;
    public SyncRepository(AppDbContext dbContext) => _dbContext = dbContext;

    public Task<List<Herb>> GetAllHerbsIncludingDeletedAsync(CancellationToken ct = default)
        => _dbContext.Herbs.IgnoreQueryFilters().AsNoTracking().ToListAsync(ct);

    public Task<List<Patient>> GetAllPatientsIncludingDeletedAsync(CancellationToken ct = default)
        => _dbContext.Patients.IgnoreQueryFilters().AsNoTracking().ToListAsync(ct);

    public Task<List<Formula>> GetAllFormulasWithHerbsIncludingDeletedAsync(CancellationToken ct = default)
        => _dbContext.Formulas.Include(f => f.Herbs).IgnoreQueryFilters().AsNoTracking().ToListAsync(ct);

    public Task<Herb?> FindHerbAsync(Guid id)
        => _dbContext.Herbs.IgnoreQueryFilters().FirstOrDefaultAsync(h => h.Id == id);

    public Task<Patient?> FindPatientAsync(Guid id)
        => _dbContext.Patients.IgnoreQueryFilters().FirstOrDefaultAsync(p => p.Id == id);

    public Task<Formula?> FindFormulaWithHerbsAsync(Guid id)
        => _dbContext.Formulas.Include(f => f.Herbs).IgnoreQueryFilters().FirstOrDefaultAsync(f => f.Id == id);

    public void UpdateHerbValues(Herb existing, Herb incoming)
        => _dbContext.Entry(existing).CurrentValues.SetValues(incoming);

    public void UpdatePatientValues(Patient existing, Patient incoming)
        => _dbContext.Entry(existing).CurrentValues.SetValues(incoming);

    public void UpdateFormulaValues(Formula existing, Formula incoming)
        => _dbContext.Entry(existing).CurrentValues.SetValues(incoming);

    public void AddHerb(Herb herb) => _dbContext.Herbs.Add(herb);

    public void AddPatient(Patient patient) => _dbContext.Patients.Add(patient);

    public void AddFormula(Formula formula) => _dbContext.Formulas.Add(formula);

    public void RemoveFormulaHerbs(ICollection<FormulaHerbItem> herbs)
        => _dbContext.RemoveRange(herbs);

    public void AddFormulaHerbItem(FormulaHerbItem item) => _dbContext.Add(item);

    public Task<Herb?> GetHerbByIdNoTrackingAsync(Guid id, CancellationToken ct = default)
        => _dbContext.Herbs.AsNoTracking().FirstOrDefaultAsync(h => h.Id == id, ct);

    public Task<Patient?> GetPatientByIdNoTrackingAsync(Guid id, CancellationToken ct = default)
        => _dbContext.Patients.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id, ct);

    public Task<Formula?> GetFormulaWithHerbsByIdNoTrackingAsync(Guid id, CancellationToken ct = default)
        => _dbContext.Formulas.Include(f => f.Herbs).AsNoTracking().FirstOrDefaultAsync(f => f.Id == id, ct);

    public async Task<bool> SoftDeleteHerbAsync(Guid id, CancellationToken ct = default)
    {
        var entity = await _dbContext.Herbs.IgnoreQueryFilters().FirstOrDefaultAsync(h => h.Id == id, ct);
        if (entity is null) return false;
        entity.IsDeleted = true;
        return true;
    }

    public async Task<bool> SoftDeletePatientAsync(Guid id, CancellationToken ct = default)
    {
        var entity = await _dbContext.Patients.IgnoreQueryFilters().FirstOrDefaultAsync(p => p.Id == id, ct);
        if (entity is null) return false;
        entity.IsDeleted = true;
        return true;
    }

    public async Task<bool> SoftDeleteFormulaAsync(Guid id, CancellationToken ct = default)
    {
        var entity = await _dbContext.Formulas.IgnoreQueryFilters().FirstOrDefaultAsync(f => f.Id == id, ct);
        if (entity is null) return false;
        entity.IsDeleted = true;
        return true;
    }

    public Task<int> SaveChangesAsync(CancellationToken ct = default)
        => _dbContext.SaveChangesAsync(ct);
}
