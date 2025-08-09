using LYBT.Infrastructure.Data;
using LYBT.Infrastructure.Repositories;
using LYBT.Models.MedicalCase;
using LYBT.Module.MedicalCase.Interfaces;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Enums;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace LYBT.Module.MedicalCase.Repositories;

/// <summary>
/// 医疗案例仓储实现 - 数据层统一化重构
/// 继承BaseRepository获得通用CRUD功能，覆盖部分方法以支持Include
/// </summary>
public class MedicalCaseRepository : BaseRepository<MedicalCaseModel>, IMedicalCaseRepository
{
    public MedicalCaseRepository(AppDbContext context) : base(context)
    {
    }

    // 覆盖基类方法以支持Include
    public override async Task<MedicalCaseModel?> GetByIdAsync(Guid id)
    {
        return await _dbSet
            .Include(m => m.Consultation)
            .FirstOrDefaultAsync(m => m.Id == id);
    }

    public override async Task<IEnumerable<MedicalCaseModel>> GetAllAsync()
    {
        return await _dbSet
            .Include(m => m.Consultation)
            .ToListAsync();
    }

    // 覆盖基类方法以支持Include和默认排序
    public override async Task<PaginatedResult<MedicalCaseModel>> GetPagedAsync(
        Expression<Func<MedicalCaseModel, bool>>? predicate,
        int pageNumber,
        int pageSize,
        Expression<Func<MedicalCaseModel, object>>? orderBy = null,
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
            query = query.OrderByDescending(m => m.CreateTime);
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

        return new PaginatedResult<MedicalCaseModel>
        {
            Items = items,
            TotalCount = totalCount,
            CurrentPage = pageNumber,
            PageSize = pageSize
        };
    }

    // 注意：基础CRUD方法（AddAsync, UpdateAsync, DeleteAsync）由BaseRepository提供

    // 医疗案例特有的业务方法
    public async Task<List<MedicalCaseModel>> GetByPatientIdAsync(Guid patientId)
    {
        return await _dbSet
            .Include(m => m.Consultation)
            .Where(m => m.PatientId == patientId)
            .OrderByDescending(m => m.CreateTime)
            .ToListAsync();
    }

    public async Task<List<MedicalCaseModel>> GetByUserIdAsync(Guid userId)
    {
        return await _dbSet
            .Include(m => m.Consultation)
            .Where(m => m.UserId == userId)
            .OrderByDescending(m => m.CreateTime)
            .ToListAsync();
    }

    public async Task<List<MedicalCaseModel>> GetByStatusAsync(MedicalCaseStatus status)
    {
        return await _dbSet
            .Include(m => m.Consultation)
            .Where(m => m.Status == status)
            .OrderByDescending(m => m.CreateTime)
            .ToListAsync();
    }

    public async Task<List<MedicalCaseModel>> GetByDateRangeAsync(DateTime startDate, DateTime endDate)
    {
        return await _dbSet
            .Include(m => m.Consultation)
            .Where(m => m.CreateTime >= startDate && m.CreateTime <= endDate)
            .OrderByDescending(m => m.CreateTime)
            .ToListAsync();
    }

    public async Task<MedicalCaseModel?> GetLatestByPatientIdAsync(Guid patientId)
    {
        return await _dbSet
            .Include(m => m.Consultation)
            .Where(m => m.PatientId == patientId)
            .OrderByDescending(m => m.CreateTime)
            .FirstOrDefaultAsync();
    }

    // AddAsync和GetListAsync由BaseRepository提供，无需重复实现
}