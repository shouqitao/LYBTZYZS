using LYBT.Infrastructure.Data;
using LYBT.Module.Consultation.Interfaces;
using LYBT.Models.Consultation;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Shared.Models.Enums;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace LYBT.Module.Consultation.Repositories;

public class ConsultationRepository : IConsultationRepository {
    private readonly AppDbContext _context;

    public ConsultationRepository(AppDbContext context) {
        _context = context;
    }

    public async Task<ConsultationModel?> GetByIdAsync(Guid id) {
        return await _context.Consultations
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<List<ConsultationModel>> GetAllAsync() {
        return await _context.Consultations
            .ToListAsync();
    }

    public async Task<PaginatedResult<ConsultationModel>> GetPagedAsync(int page, int pageSize, 
        Expression<Func<ConsultationModel, bool>>? predicate = null) {
        var query = _context.Consultations
            .AsQueryable();

        if (predicate != null) {
            query = query.Where(predicate);
        }

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderByDescending(c => c.CreateTime)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PaginatedResult<ConsultationModel> {
            Items = items,
            TotalCount = totalCount,
            CurrentPage = page,
            PageSize = pageSize
        };
    }

    public async Task<ConsultationModel> CreateAsync(ConsultationModel entity) {
        _context.Consultations.Add(entity);
        await _context.SaveChangesAsync();
        return entity;
    }

    public async Task<bool> UpdateAsync(ConsultationModel entity) {
        _context.Consultations.Update(entity);
        return await _context.SaveChangesAsync() > 0;
    }

    public async Task<bool> DeleteAsync(Guid id) {
        var entity = await GetByIdAsync(id);
        if (entity == null) return false;
        
        _context.Consultations.Remove(entity);
        return await _context.SaveChangesAsync() > 0;
    }

    public async Task<List<ConsultationModel>> GetByPatientIdAsync(Guid patientId) {
        return await _context.Consultations
            .Where(c => c.PatientId == patientId)
            .OrderByDescending(c => c.CreateTime)
            .ToListAsync();
    }

    public async Task<List<ConsultationModel>> GetByDoctorIdAsync(Guid doctorId) {
        return await _context.Consultations
            .Where(c => c.UserId == doctorId)
            .OrderByDescending(c => c.CreateTime)
            .ToListAsync();
    }

    public async Task<List<ConsultationModel>> GetByStatusAsync(ConsultationStatus status) {
        // ConsultationModel实际使用CommonStatus，需要转换
        var commonStatus = status == ConsultationStatus.InProgress ? CommonStatus.Enabled : CommonStatus.Disabled;
        return await _context.Consultations
            .Where(c => c.Status == commonStatus)
            .OrderByDescending(c => c.CreateTime)
            .ToListAsync();
    }

    public async Task<List<ConsultationModel>> GetByDateRangeAsync(DateTime startDate, DateTime endDate) {
        return await _context.Consultations
            .Where(c => c.CreateTime >= startDate && c.CreateTime <= endDate)
            .OrderByDescending(c => c.CreateTime)
            .ToListAsync();
    }

    public async Task<ConsultationModel?> GetByMedicalCaseIdAsync(Guid medicalCaseId) {
        return await _context.Consultations
            .FirstOrDefaultAsync(c => c.MedicalCaseId == medicalCaseId);
    }

    public async Task<ConsultationModel> AddAsync(ConsultationModel entity) {
        return await CreateAsync(entity);
    }

    public async Task<List<ConsultationModel>> GetListAsync() {
        return await GetAllAsync();
    }
}