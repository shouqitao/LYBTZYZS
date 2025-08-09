using LYBT.Infrastructure.Data;
using LYBT.Infrastructure.Repositories;
using LYBT.Module.Consultation.Interfaces;
using LYBT.Models.Consultation;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Shared.Models.Enums;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace LYBT.Module.Consultation.Repositories;

/// <summary>
/// 看诊仓储实现 - 数据层统一化重构
/// 继承BaseRepository获得通用CRUD功能，只实现看诊特有业务方法
/// </summary>
public class ConsultationRepository : BaseRepository<ConsultationModel>, IConsultationRepository
{
    public ConsultationRepository(AppDbContext context) : base(context)
    {
    }

    // 注意：基础CRUD方法由BaseRepository提供
    // GetByIdAsync, GetAllAsync, GetPagedAsync, AddAsync, UpdateAsync, DeleteAsync等都由基类实现

    public async Task<List<ConsultationModel>> GetByPatientIdAsync(Guid patientId)
    {
        return await _context.Consultations
            .Where(c => c.PatientId == patientId)
            .OrderByDescending(c => c.CreateTime)
            .ToListAsync();
    }

    public async Task<List<ConsultationModel>> GetByDoctorIdAsync(Guid doctorId)
    {
        return await _context.Consultations
            .Where(c => c.UserId == doctorId)
            .OrderByDescending(c => c.CreateTime)
            .ToListAsync();
    }

    public async Task<List<ConsultationModel>> GetByStatusAsync(ConsultationStatus status)
    {
        // ConsultationModel实际使用CommonStatus，需要转换
        var commonStatus = status == ConsultationStatus.InProgress ? CommonStatus.Enabled : CommonStatus.Disabled;
        return await _context.Consultations
            .Where(c => c.Status == commonStatus)
            .OrderByDescending(c => c.CreateTime)
            .ToListAsync();
    }

    public async Task<List<ConsultationModel>> GetByDateRangeAsync(DateTime startDate, DateTime endDate)
    {
        return await _context.Consultations
            .Where(c => c.CreateTime >= startDate && c.CreateTime <= endDate)
            .OrderByDescending(c => c.CreateTime)
            .ToListAsync();
    }

    public async Task<ConsultationModel?> GetByMedicalCaseIdAsync(Guid medicalCaseId)
    {
        return await _context.Consultations
            .FirstOrDefaultAsync(c => c.MedicalCaseId == medicalCaseId);
    }

    // AddAsync和GetListAsync由BaseRepository提供，无需重复实现
}