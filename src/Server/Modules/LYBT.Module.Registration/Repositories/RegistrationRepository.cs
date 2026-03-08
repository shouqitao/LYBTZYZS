using LYBT.Infrastructure.Data;
using LYBT.Infrastructure.Repositories;
using LYBT.Module.Registration.Interfaces;
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
}
