using LYBT.Entities.Formulas;
using LYBT.Entities.Herbs;
using LYBT.Entities.MedicalCases;
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

    // -- Metadata queries --

    public Task<List<Herb>> GetAllHerbsIncludingDeletedAsync(CancellationToken ct = default)
        => _context.Herbs.IgnoreQueryFilters().AsNoTracking().ToListAsync(ct);

    public Task<List<Patient>> GetAllPatientsIncludingDeletedAsync(CancellationToken ct = default)
        => _context.Patients.IgnoreQueryFilters().AsNoTracking().ToListAsync(ct);

    public Task<List<Formula>> GetAllFormulasWithHerbsIncludingDeletedAsync(CancellationToken ct = default)
        => _context.Formulas.Include(f => f.Herbs).IgnoreQueryFilters().AsNoTracking().ToListAsync(ct);

    public Task<List<MedicalCase>> GetAllMedicalCasesIncludingDeletedAsync(CancellationToken ct = default)
        => _context.MedicalCases
            .Include(mc => mc.Consultation)
            .Include(mc => mc.Prescription)
                .ThenInclude(p => p!.Items)
            .IgnoreQueryFilters()
            .AsNoTracking()
            .ToListAsync(ct);

    // -- Upload: Find tracked entities --

    public Task<Herb?> FindHerbAsync(Guid id)
        => _context.Herbs.IgnoreQueryFilters().FirstOrDefaultAsync(h => h.Id == id);

    public Task<Patient?> FindPatientAsync(Guid id)
        => _context.Patients.IgnoreQueryFilters().FirstOrDefaultAsync(p => p.Id == id);

    public Task<Formula?> FindFormulaWithHerbsAsync(Guid id)
        => _context.Formulas.Include(f => f.Herbs).IgnoreQueryFilters().FirstOrDefaultAsync(f => f.Id == id);

    public Task<MedicalCase?> FindMedicalCaseWithIncludesAsync(Guid id)
        => _context.MedicalCases
            .Include(mc => mc.Consultation)
            .Include(mc => mc.Prescription)
                .ThenInclude(p => p!.Items)
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(mc => mc.Id == id);

    // -- Upload: Value overwrite --

    public void UpdateHerbValues(Herb existing, Herb incoming)
        => _context.Entry(existing).CurrentValues.SetValues(incoming);

    public void UpdatePatientValues(Patient existing, Patient incoming)
        => _context.Entry(existing).CurrentValues.SetValues(incoming);

    public void UpdateFormulaValues(Formula existing, Formula incoming)
        => _context.Entry(existing).CurrentValues.SetValues(incoming);

    public void UpdateMedicalCaseValues(MedicalCase existing, MedicalCase incoming)
    {
        _context.Entry(existing).CurrentValues.SetValues(incoming);

        if (incoming.Consultation != null)
        {
            if (existing.Consultation != null)
            {
                _context.Entry(existing.Consultation).CurrentValues.SetValues(incoming.Consultation);
            }
            else
            {
                existing.Consultation = incoming.Consultation;
            }
        }

        if (incoming.Prescription != null)
        {
            if (existing.Prescription != null)
            {
                _context.Entry(existing.Prescription).CurrentValues.SetValues(incoming.Prescription);

                if (existing.Prescription.Items != null)
                {
                    _context.RemoveRange(existing.Prescription.Items);
                }
                if (incoming.Prescription.Items != null)
                {
                    foreach (var item in incoming.Prescription.Items)
                    {
                        item.PrescriptionId = existing.Prescription.Id;
                        _context.Add(item);
                    }
                }
            }
            else
            {
                incoming.Prescription.MedicalCaseId = existing.Id;
                existing.Prescription = incoming.Prescription;
            }
        }
        else if (existing.Prescription != null)
        {
            if (existing.Prescription.Items != null)
            {
                _context.RemoveRange(existing.Prescription.Items);
            }
            _context.Remove(existing.Prescription);
            existing.Prescription = null;
        }
    }

    // -- Upload: Add new --

    public void AddHerb(Herb herb) => _context.Herbs.Add(herb);

    public void AddPatient(Patient patient) => _context.Patients.Add(patient);

    public void AddFormula(Formula formula) => _context.Formulas.Add(formula);

    public void AddMedicalCase(MedicalCase medicalCase) => _context.MedicalCases.Add(medicalCase);

    public void RemoveFormulaHerbs(ICollection<FormulaHerbItem> herbs)
        => _context.RemoveRange(herbs);

    public void AddFormulaHerbItem(FormulaHerbItem item) => _context.Add(item);

    // -- Download (AsNoTracking) --

    public Task<Herb?> GetHerbByIdNoTrackingAsync(Guid id, CancellationToken ct = default)
        => _context.Herbs.AsNoTracking().FirstOrDefaultAsync(h => h.Id == id, ct);

    public Task<Patient?> GetPatientByIdNoTrackingAsync(Guid id, CancellationToken ct = default)
        => _context.Patients.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id, ct);

    public Task<Formula?> GetFormulaWithHerbsByIdNoTrackingAsync(Guid id, CancellationToken ct = default)
        => _context.Formulas.Include(f => f.Herbs).AsNoTracking().FirstOrDefaultAsync(f => f.Id == id, ct);

    public Task<MedicalCase?> GetMedicalCaseWithIncludesByIdNoTrackingAsync(Guid id, CancellationToken ct = default)
        => _context.MedicalCases
            .Include(mc => mc.Consultation)
            .Include(mc => mc.Prescription)
                .ThenInclude(p => p!.Items)
            .AsNoTracking()
            .FirstOrDefaultAsync(mc => mc.Id == id, ct);

    // -- Soft delete (IgnoreQueryFilters) --

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

    public async Task<bool> SoftDeleteMedicalCaseAsync(Guid id, CancellationToken ct = default)
    {
        var entity = await _context.MedicalCases.IgnoreQueryFilters().FirstOrDefaultAsync(mc => mc.Id == id, ct);
        if (entity is null) return false;
        entity.IsDeleted = true;
        return true;
    }

    public override Task<int> SaveChangesAsync(CancellationToken ct = default)
        => _context.SaveChangesAsync(ct);
}
