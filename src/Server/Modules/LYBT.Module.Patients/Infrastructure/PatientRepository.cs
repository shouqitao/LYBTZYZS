using LYBT.Entities.Patients;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Enums;
using LYBT.Module.Patients.Interfaces;
using LYBT.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace LYBT.Module.Patients.Infrastructure;

/// <summary>
/// 患者仓储实现。封装患者数据访问逻辑。
/// </summary>
public class PatientRepository : IPatientRepository
{
    private readonly AppDbContext _context;

    public PatientRepository(AppDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    /// <inheritdoc/>
    public async Task<Patient?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Patients
            .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<PagedResult<Patient>> GetPagedAsync(
        int page, int pageSize, string? keyword,
        CancellationToken cancellationToken = default)
    {
        return await GetPagedAsync(page, pageSize, keyword, null, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<PagedResult<Patient>> GetPagedAsync(
        int page, int pageSize, string? keyword, CommonStatus? status,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Patients
            .Where(p => !p.IsDeleted)
            .AsQueryable();

        if (status.HasValue)
            query = query.Where(p => p.Status == status.Value);

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var kw = keyword.ToLower();
            query = query.Where(p =>
                p.Name.ToLower().Contains(kw) ||
                (p.PinYinCode != null && p.PinYinCode.ToLower().Contains(kw)) ||
                (p.PhoneNumber != null && p.PhoneNumber.Contains(kw)));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(p => p.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<Patient>
        {
            Items = items,
            TotalCount = totalCount,
            CurrentPage = page,
            PageSize = pageSize
        };
    }

    /// <inheritdoc/>
    public async Task<bool> ExistsByNameAsync(string name, Guid? excludeId = null, CancellationToken cancellationToken = default)
    {
        var query = _context.Patients
            .Where(p => p.Name == name && !p.IsDeleted);

        if (excludeId.HasValue)
            query = query.Where(p => p.Id != excludeId.Value);

        return await query.AnyAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task AddAsync(Patient patient, CancellationToken cancellationToken = default)
    {
        await _context.Patients.AddAsync(patient, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task UpdateAsync(Patient patient, CancellationToken cancellationToken = default)
    {
        _context.Patients.Update(patient);
        await _context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<Patient?> GetByIdNumberAsync(string idNumber, CancellationToken cancellationToken = default)
    {
        return await _context.Patients
            .FirstOrDefaultAsync(p => p.IdNumber == idNumber && !p.IsDeleted, cancellationToken);
    }
}


