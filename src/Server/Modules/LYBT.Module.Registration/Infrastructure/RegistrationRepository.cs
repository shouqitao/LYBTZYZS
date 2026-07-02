using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Enums;
using LYBT.Module.Registration.Interfaces;
using LYBT.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using RegistrationEntity = LYBT.Entities.Registrations.Registration;

namespace LYBT.Module.Registration.Infrastructure;

/// <summary>
/// 挂号仓储实现。封装患者数据访问逻辑。
/// </summary>
public class RegistrationRepository : IRegistrationRepository
{
    private readonly AppDbContext _context;

    public RegistrationRepository(AppDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    /// <inheritdoc/>
    public async Task<RegistrationEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Registrations
            .FirstOrDefaultAsync(r => r.Id == id && !r.IsDeleted, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<PagedResult<RegistrationEntity>> GetPagedAsync(
        int page, int pageSize, string? keyword,
        DateTime? startDate, DateTime? endDate,
        Guid? patientId, Guid? doctorId,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Registrations
            .Where(r => !r.IsDeleted)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var term = keyword.Trim();
            query = query.Where(r =>
                r.PatientName.Contains(term) ||
                r.DoctorName.Contains(term));
        }

        if (startDate.HasValue)
            query = query.Where(r => r.CreatedAt >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(r => r.CreatedAt < endDate.Value);

        if (patientId.HasValue)
            query = query.Where(r => r.PatientId == patientId.Value);

        if (doctorId.HasValue)
            query = query.Where(r => r.DoctorId == doctorId.Value);

        query = query.OrderByDescending(r => r.CreatedAt);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<RegistrationEntity>
        {
            Items = items,
            TotalCount = totalCount,
            CurrentPage = page,
            PageSize = pageSize
        };
    }

    /// <inheritdoc/>
    public async Task<List<RegistrationEntity>> GetWaitingQueueAsync(
        Guid? doctorId = null, CancellationToken cancellationToken = default)
    {
        var query = _context.Registrations
            .AsNoTracking()
            .Where(r => !r.IsDeleted && r.Status == RegistrationStatus.Waiting);

        if (doctorId.HasValue)
            query = query.Where(r => r.DoctorId == doctorId.Value);

        return await query
            .OrderBy(r => r.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<int> GetTodayMaxQueueNumberAsync(CancellationToken cancellationToken = default)
    {
        var today = DateTime.UtcNow.Date;
        var max = await _context.Registrations
            .Where(r => !r.IsDeleted && r.CreatedAt >= today)
            .MaxAsync(r => (int?)r.QueueNumber, cancellationToken);
        return max ?? 0;
    }

    /// <inheritdoc/>
    public async Task<bool> HasWaitingRegistrationAsync(Guid patientId, CancellationToken cancellationToken = default)
    {
        return await _context.Registrations.AnyAsync(r =>
            !r.IsDeleted &&
            r.PatientId == patientId &&
            r.Status == RegistrationStatus.Waiting,
            cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<RegistrationEntity?> GetByMedicalCaseIdAsync(Guid medicalCaseId, CancellationToken cancellationToken = default)
    {
        return await _context.Registrations.FirstOrDefaultAsync(r =>
            !r.IsDeleted &&
            r.MedicalCaseId == medicalCaseId,
            cancellationToken);
    }

    /// <inheritdoc/>
    public async Task AddAsync(RegistrationEntity registration, CancellationToken cancellationToken = default)
    {
        await _context.Registrations.AddAsync(registration, cancellationToken);
    }

    /// <inheritdoc/>
    public Task UpdateAsync(RegistrationEntity registration, CancellationToken cancellationToken = default)
    {
        _context.Registrations.Update(registration);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }
}
