using LYBT.Infrastructure.Data;
using LYBT.Models.MedicalCase;
using LYBT.Module.MedicalCase.Interfaces;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Enums;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace LYBT.Module.MedicalCase.Repositories;

public class MedicalCaseRepository : IMedicalCaseRepository
{
    private readonly AppDbContext _context;

    public MedicalCaseRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<MedicalCaseModel?> GetByIdAsync(Guid id)
    {
        return await _context.MedicalCases
            .Include(m => m.Consultation)
            .FirstOrDefaultAsync(m => m.Id == id);
    }

    public async Task<List<MedicalCaseModel>> GetAllAsync()
    {
        return await _context.MedicalCases
            .Include(m => m.Consultation)
            .ToListAsync();
    }

    public async Task<PaginatedResult<MedicalCaseModel>> GetPagedAsync(int page, int pageSize,
        Expression<Func<MedicalCaseModel, bool>>? predicate = null)
    {
        var query = _context.MedicalCases
            .Include(m => m.Consultation)
            .AsQueryable();

        if (predicate != null)
        {
            query = query.Where(predicate);
        }

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderByDescending(m => m.CreateTime)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PaginatedResult<MedicalCaseModel>
        {
            Items = items,
            TotalCount = totalCount,
            CurrentPage = page,
            PageSize = pageSize
        };
    }

    public async Task<MedicalCaseModel> CreateAsync(MedicalCaseModel entity)
    {
        _context.MedicalCases.Add(entity);
        await _context.SaveChangesAsync();
        return entity;
    }

    public async Task<bool> UpdateAsync(MedicalCaseModel entity)
    {
        _context.MedicalCases.Update(entity);
        return await _context.SaveChangesAsync() > 0;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var entity = await GetByIdAsync(id);
        if (entity == null) return false;

        _context.MedicalCases.Remove(entity);
        return await _context.SaveChangesAsync() > 0;
    }

    public async Task<List<MedicalCaseModel>> GetByPatientIdAsync(Guid patientId)
    {
        return await _context.MedicalCases
            .Include(m => m.Consultation)
            .Where(m => m.PatientId == patientId)
            .OrderByDescending(m => m.CreateTime)
            .ToListAsync();
    }

    public async Task<List<MedicalCaseModel>> GetByDoctorIdAsync(Guid doctorId)
    {
        return await _context.MedicalCases
            .Include(m => m.Consultation)
            .Where(m => m.UserId == doctorId)
            .OrderByDescending(m => m.CreateTime)
            .ToListAsync();
    }

    public async Task<List<MedicalCaseModel>> GetByStatusAsync(MedicalCaseStatus status)
    {
        return await _context.MedicalCases
            .Include(m => m.Consultation)
            .Where(m => m.Status == status)
            .OrderByDescending(m => m.CreateTime)
            .ToListAsync();
    }

    public async Task<List<MedicalCaseModel>> GetByDateRangeAsync(DateTime startDate, DateTime endDate)
    {
        return await _context.MedicalCases
            .Include(m => m.Consultation)
            .Where(m => m.CreateTime >= startDate && m.CreateTime <= endDate)
            .OrderByDescending(m => m.CreateTime)
            .ToListAsync();
    }

    public async Task<MedicalCaseModel?> GetLatestByPatientIdAsync(Guid patientId)
    {
        return await _context.MedicalCases
            .Include(m => m.Consultation)
            .Where(m => m.PatientId == patientId)
            .OrderByDescending(m => m.CreateTime)
            .FirstOrDefaultAsync();
    }

    public async Task<MedicalCaseModel> AddAsync(MedicalCaseModel entity)
    {
        return await CreateAsync(entity);
    }

    public async Task<List<MedicalCaseModel>> GetListAsync()
    {
        return await GetAllAsync();
    }

    public async Task<List<MedicalCaseModel>> GetByUserIdAsync(Guid userId)
    {
        return await GetByDoctorIdAsync(userId);
    }
}