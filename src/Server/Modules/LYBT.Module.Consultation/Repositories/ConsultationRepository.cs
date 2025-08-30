using LYBT.Infrastructure.Data;
using LYBT.Infrastructure.Repositories;
using LYBT.Module.Consultation.Interfaces;
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
public class ConsultationRepository : BaseRepository<LYBT.Entities.Consultation.Consultation>, IConsultationRepository
{
    public ConsultationRepository(AppDbContext context) : base(context)
    {
    }

    // 注意：基础CRUD方法由BaseRepository提供
    // GetByIdAsync, GetAllAsync, GetPagedAsync, AddAsync, UpdateAsync, DeleteAsync等都由基类实现

    public async Task<List<LYBT.Entities.Consultation.Consultation>> GetByPatientIdAsync(Guid patientId)
    {
        // TODO: UltraThink v2.0 Refactor - ConsultationTime属性已删除
        return await _context.Consultations
            .Where(c => c.PatientId == patientId)
            .OrderByDescending(c => c.Id)
            .ToListAsync();
    }

    public async Task<List<LYBT.Entities.Consultation.Consultation>> GetByDoctorIdAsync(Guid doctorId)
    {
        // TODO: UltraThink v2.0 Refactor - ConsultationTime属性已删除
        return await _context.Consultations
            .Where(c => c.UserId == doctorId)
            .OrderByDescending(c => c.Id)
            .ToListAsync();
    }

    public async Task<List<LYBT.Entities.Consultation.Consultation>> GetByStatusAsync(ConsultationStatus status)
    {
        // Consultation实际使用CommonStatus，需要转换
        var commonStatus = status == ConsultationStatus.InProgress ? CommonStatus.Enabled : CommonStatus.Disabled;
        // TODO: UltraThink v2.0 Refactor - ConsultationTime属性已删除
        return await _context.Consultations
            .Where(c => c.Status == commonStatus)
            .OrderByDescending(c => c.Id)
            .ToListAsync();
    }

#if false
    // TODO: UltraThink v2.0 Refactor - ConsultationTime属性已删除
    public async Task<List<LYBT.Entities.Consultation.Consultation>> GetByDateRangeAsync_Original(DateTime startDate, DateTime endDate)
    {
        return await _context.Consultations
            .Where(c => c.ConsultationTime >= startDate && c.ConsultationTime <= endDate)
            .OrderByDescending(c => c.ConsultationTime)
            .ToListAsync();
    }
#endif

    public async Task<List<LYBT.Entities.Consultation.Consultation>> GetByDateRangeAsync(DateTime startDate, DateTime endDate)
    {
        // TODO: UltraThink v2.0 Refactor - 暂时返回所有记录，无法按日期范围过滤
        return await _context.Consultations
            .OrderByDescending(c => c.Id)
            .ToListAsync();
    }

    public async Task<LYBT.Entities.Consultation.Consultation?> GetByMedicalCaseIdAsync(Guid medicalCaseId)
    {
        return await _context.Consultations
            .FirstOrDefaultAsync(c => c.MedicalCaseId == medicalCaseId);
    }

    // AddAsync和GetListAsync由BaseRepository提供，无需重复实现
}