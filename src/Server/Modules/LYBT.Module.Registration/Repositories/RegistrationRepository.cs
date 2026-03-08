using LYBT.Infrastructure.Data;
using LYBT.Infrastructure.Repositories;
using LYBT.Module.Registration.Interfaces;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RegistrationEntity = LYBT.Entities.Registrations.Registration;

namespace LYBT.Module.Registration.Repositories;

/// <summary>
/// 挂号仓储实现 -- 继承 BaseRepository，复用标准 CRUD
/// </summary>
internal class RegistrationRepository : BaseRepository<RegistrationEntity>, IRegistrationRepository
{
    public RegistrationRepository(AppDbContext context, ILogger<RegistrationRepository> logger)
        : base(context, logger)
    {
    }

    /// <summary>
    /// 关键字过滤: 患者姓名、医生姓名
    /// </summary>
    protected override IQueryable<RegistrationEntity> ApplyKeywordFilter(
        IQueryable<RegistrationEntity> query, string keyword)
    {
        return query.Where(r =>
            r.PatientName.Contains(keyword) ||
            r.DoctorName.Contains(keyword));
    }

    /// <summary>
    /// 默认排序: 按创建时间降序 (最新挂号在前)
    /// </summary>
    protected override IQueryable<RegistrationEntity> ApplyDefaultOrdering(
        IQueryable<RegistrationEntity> query)
    {
        return query.OrderByDescending(r => r.CreatedAt);
    }

    public async Task<List<RegistrationEntity>> GetWaitingQueueAsync(Guid? doctorId = null)
    {
        var query = _dbSet.AsNoTracking()
            .Where(r => !r.IsDeleted && r.Status == RegistrationStatus.Waiting);

        if (doctorId.HasValue)
        {
            query = query.Where(r => r.DoctorId == doctorId.Value);
        }

        return await query.OrderBy(r => r.CreatedAt).ToListAsync();
    }

    public async Task<List<RegistrationEntity>> GetByStatusAsync(
        RegistrationStatus status, Guid? doctorId = null)
    {
        var query = _dbSet.AsNoTracking()
            .Where(r => !r.IsDeleted && r.Status == status);

        if (doctorId.HasValue)
        {
            query = query.Where(r => r.DoctorId == doctorId.Value);
        }

        return await query.OrderByDescending(r => r.CreatedAt).ToListAsync();
    }

    public async Task<bool> HasWaitingRegistrationAsync(Guid patientId)
    {
        return await _dbSet.AnyAsync(r =>
            !r.IsDeleted &&
            r.PatientId == patientId &&
            r.Status == RegistrationStatus.Waiting);
    }

    public async Task<RegistrationEntity?> GetByMedicalCaseIdAsync(Guid medicalCaseId)
    {
        return await _dbSet.FirstOrDefaultAsync(r =>
            !r.IsDeleted &&
            r.MedicalCaseId == medicalCaseId);
    }

    /// <summary>
    /// 分页查询挂号记录 (带高级过滤)
    /// US-REG-007: 日期范围、患者、医生过滤
    /// </summary>
    public async Task<PagedResult<RegistrationEntity>> GetPagedAsync(
        int page, int pageSize, string? keyword,
        DateTime? startDate, DateTime? endDate,
        Guid? patientId, Guid? doctorId)
    {
        var query = _dbSet.AsNoTracking().Where(r => !r.IsDeleted);

        // 关键字过滤
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var term = keyword.Trim();
            query = query.Where(r =>
                r.PatientName.Contains(term) ||
                r.DoctorName.Contains(term));
        }

        // 日期范围过滤
        if (startDate.HasValue)
        {
            query = query.Where(r => r.CreatedAt >= startDate.Value);
        }
        if (endDate.HasValue)
        {
            query = query.Where(r => r.CreatedAt < endDate.Value);
        }

        // 患者过滤
        if (patientId.HasValue)
        {
            query = query.Where(r => r.PatientId == patientId.Value);
        }

        // 医生过滤
        if (doctorId.HasValue)
        {
            query = query.Where(r => r.DoctorId == doctorId.Value);
        }

        // 默认排序: 最新在前
        query = query.OrderByDescending(r => r.CreatedAt);

        var totalCount = await query.CountAsync();
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<RegistrationEntity>(items, totalCount, page, pageSize);
    }
}
