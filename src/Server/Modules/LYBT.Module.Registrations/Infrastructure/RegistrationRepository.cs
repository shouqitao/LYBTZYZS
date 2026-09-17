using LYBT.Entities.Registrations;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Enums;
using LYBT.Module.Registrations.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace LYBT.Module.Registrations.Infrastructure;

/// <summary>
/// 挂号仓储实现。封装患者数据访问逻辑。
/// ADR-0017: 注入挂号模块自己的 DbContext
/// </summary>
public class RegistrationRepository : IRegistrationRepository
{
    private readonly RegistrationDbContext _context;

    public RegistrationRepository(RegistrationDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    /// <inheritdoc/>
    public async Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Database.BeginTransactionAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<Registration?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Registrations
            .FirstOrDefaultAsync(r => r.Id == id && !r.IsDeleted, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<PagedResult<Registration>> GetPagedAsync(
        int page, int pageSize, string? keyword,
        DateTime? startDate, DateTime? endDate,
        Guid? patientId, Guid? doctorId,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Registrations
            .AsNoTracking()
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

        return new PagedResult<Registration>
        {
            Items = items,
            TotalCount = totalCount,
            CurrentPage = page,
            PageSize = pageSize
        };
    }

    /// <inheritdoc/>
    public async Task<List<Registration>> GetWaitingQueueAsync(
        Guid? doctorId = null, bool onlyToday = false, CancellationToken cancellationToken = default)
    {
        var query = _context.Registrations
            .AsNoTracking()
            .Where(r => !r.IsDeleted && r.Status == RegistrationStatus.Waiting);

        // P1 (US-REG-BR-012): 医生待诊列表仅当天（历史 Waiting 不入队）
        if (onlyToday)
            query = query.Where(r => r.CreatedAt.Date == DateTime.Today);

        if (doctorId.HasValue)
            query = query.Where(r => r.DoctorId == doctorId.Value);

        return await query
            .OrderBy(r => r.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    /// <summary>
    /// 患者当日是否已有待诊挂号（T5-1 #9 US-REG-BR-007）
    /// </summary>
    public async Task<bool> HasSameDayWaitingAsync(Guid patientId, CancellationToken cancellationToken = default)
    {
        var today = DateTime.Today;
        return await _context.Registrations
            .AsNoTracking()
            .AnyAsync(r => !r.IsDeleted
                && r.PatientId == patientId
                && r.Status == RegistrationStatus.Waiting
                && r.CreatedAt.Date == today, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<bool> HasPendingAsync(Guid patientId, CancellationToken cancellationToken = default)
    {
        return await _context.Registrations
            .AsNoTracking()
            .AnyAsync(r => !r.IsDeleted
                && r.PatientId == patientId
                && (r.Status == RegistrationStatus.Waiting || r.Status == RegistrationStatus.InProgress), cancellationToken);
    }

    public async Task<int> GetTodayMaxQueueNumberAsync(CancellationToken cancellationToken = default)
    {
        var today = DateTime.UtcNow.Date;
        var max = await _context.Registrations
            .Where(r => !r.IsDeleted && r.CreatedAt >= today)
            .MaxAsync(r => (int?)r.QueueNumber, cancellationToken);
        return max ?? 0;
    }

    /// <inheritdoc/>
    public async Task<Registration?> GetByMedicalCaseIdAsync(Guid medicalCaseId, CancellationToken cancellationToken = default)
    {
        return await _context.Registrations.FirstOrDefaultAsync(r =>
            !r.IsDeleted &&
            r.MedicalCaseId == medicalCaseId,
            cancellationToken);
    }

    /// <inheritdoc/>
    public async Task AddAsync(Registration registration, CancellationToken cancellationToken = default)
    {
        await _context.Registrations.AddAsync(registration, cancellationToken);
    }

    /// <inheritdoc/>
    public Task UpdateAsync(Registration registration, CancellationToken cancellationToken = default)
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
