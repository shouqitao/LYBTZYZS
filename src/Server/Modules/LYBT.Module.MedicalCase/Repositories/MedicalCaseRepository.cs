using LYBT.Infrastructure.Data;
using LYBT.Infrastructure.Repositories;
using LYBT.Module.MedicalCase.Interfaces;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Enums;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace LYBT.Module.MedicalCase.Repositories;

/// <summary>
/// 医疗案例仓储实现 - 数据层统一化重构
/// 继承BaseRepository获得通用CRUD功能，覆盖部分方法以支持Include
/// </summary>
public class MedicalCaseRepository : BaseRepository<LYBT.Entities.MedicalCase.MedicalCase>, IMedicalCaseRepository
{
    public MedicalCaseRepository(AppDbContext context) : base(context)
    {
    }

    // 覆盖基类方法以支持Include
    public override async Task<LYBT.Entities.MedicalCase.MedicalCase?> GetByIdAsync(Guid id)
    {
        return await _dbSet
            .Include(m => m.Consultation)
            .FirstOrDefaultAsync(m => m.Id == id);
    }

    public override async Task<IEnumerable<LYBT.Entities.MedicalCase.MedicalCase>> GetAllAsync()
    {
        return await _dbSet
            .Include(m => m.Consultation)
            .ToListAsync();
    }

    // 覆盖基类方法以支持Include和默认排序
    public override async Task<PagedResult<LYBT.Entities.MedicalCase.MedicalCase>> GetPagedAsync(
        Expression<Func<LYBT.Entities.MedicalCase.MedicalCase, bool>>? predicate,
        int pageNumber,
        int pageSize,
        Expression<Func<LYBT.Entities.MedicalCase.MedicalCase, object>>? orderBy = null,
        bool ascending = true)
    {
        var query = _dbSet
            .Include(m => m.Consultation)
            .AsQueryable();

        if (predicate != null)
        {
            query = query.Where(predicate);
        }

        // 使用默认排序（按创建时间降序）如果没有指定排序
        if (orderBy == null)
        {
            query = query.OrderByDescending(m => m.ConsultationDate);
        }
        else
        {
            query = ascending ? query.OrderBy(orderBy) : query.OrderByDescending(orderBy);
        }

        var totalCount = await query.CountAsync();
        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<LYBT.Entities.MedicalCase.MedicalCase>
        {
            Items = items,
            TotalCount = totalCount,
            CurrentPage = pageNumber,
            PageSize = pageSize
        };
    }

    // 注意：基础CRUD方法（AddAsync, UpdateAsync, DeleteAsync）由BaseRepository提供

    // 医疗案例特有的业务方法
    public async Task<List<LYBT.Entities.MedicalCase.MedicalCase>> GetByPatientIdAsync(Guid patientId)
    {
        return await _dbSet
            .Include(m => m.Consultation)
            .Where(m => m.PatientId == patientId)
            .OrderByDescending(m => m.ConsultationDate)
            .ToListAsync();
    }

    public async Task<List<LYBT.Entities.MedicalCase.MedicalCase>> GetByUserIdAsync(Guid userId)
    {
        return await _dbSet
            .Include(m => m.Consultation)
            .Where(m => m.DoctorId == userId)
            .OrderByDescending(m => m.ConsultationDate)
            .ToListAsync();
    }

    public async Task<List<LYBT.Entities.MedicalCase.MedicalCase>> GetByStatusAsync(MedicalCaseStatus status)
    {
        return await _dbSet
            .Include(m => m.Consultation)
            .Where(m => m.Status == status)
            .OrderByDescending(m => m.ConsultationDate)
            .ToListAsync();
    }

    public async Task<List<LYBT.Entities.MedicalCase.MedicalCase>> GetByDateRangeAsync(DateTime startDate, DateTime endDate)
    {
        return await _dbSet
            .Include(m => m.Consultation)
            .Where(m => m.ConsultationDate >= startDate && m.ConsultationDate <= endDate)
            .OrderByDescending(m => m.ConsultationDate)
            .ToListAsync();
    }

    public async Task<LYBT.Entities.MedicalCase.MedicalCase?> GetLatestByPatientIdAsync(Guid patientId)
    {
        return await _dbSet
            .Include(m => m.Consultation)
            .Where(m => m.PatientId == patientId)
            .OrderByDescending(m => m.ConsultationDate)
            .FirstOrDefaultAsync();
    }

    // AddAsync和GetListAsync由BaseRepository提供，无需重复实现
}