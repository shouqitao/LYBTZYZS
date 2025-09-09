using LYBT.Infrastructure.Data;
using LYBT.Infrastructure.Repositories;
using LYBT.Module.Consultation.Interfaces;
using LYBT.Shared.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace LYBT.Module.Consultation.Repositories;

/// <summary>
/// 看诊仓储实现 - 数据层统一化重构
/// 继承OptimizedBaseRepository获得缓存和性能优化，实现看诊特有业务方法
/// </summary>
public class ConsultationRepository : OptimizedBaseRepository<LYBT.Entities.Consultation.Consultation>, IConsultationRepository
{

    public ConsultationRepository(
        AppDbContext context,
        ILogger<ConsultationRepository> logger,
        IMemoryCache cache) : base(context, logger, cache)
    {
    }

    // 注意：基础CRUD方法由OptimizedBaseRepository提供
    // GetByIdAsync, GetAllAsync, GetPagedAsync, AddAsync, UpdateAsync, DeleteAsync等都由基类实现，带有缓存优化
    public async Task<List<LYBT.Entities.Consultation.Consultation>> GetByPatientIdAsync(Guid patientId)
    {
        var cacheKey = $"{CacheKeyPrefix}patient:{patientId}";

        if (_cache.TryGetValue<List<LYBT.Entities.Consultation.Consultation>>(cacheKey, out var cached) && cached != null)
        {
            _logger.LogDebug("从缓存获取患者看诊记录 {PatientId}", patientId);
            return cached;
        }

        var consultations = await _dbSet
            .Where(c => c.PatientId == patientId)
            .OrderByDescending(c => c.Id)
            .ToListAsync();

        _cache.Set(cacheKey, consultations, DefaultCacheDuration);
        return consultations;
    }

    public async Task<List<LYBT.Entities.Consultation.Consultation>> GetByDoctorIdAsync(Guid doctorId)
    {
        var cacheKey = $"{CacheKeyPrefix}doctor:{doctorId}";

        if (_cache.TryGetValue<List<LYBT.Entities.Consultation.Consultation>>(cacheKey, out var cached) && cached != null)
        {
            _logger.LogDebug("从缓存获取医生看诊记录 {DoctorId}", doctorId);
            return cached;
        }

        var consultations = await _dbSet
            .Where(c => c.UserId == doctorId)
            .OrderByDescending(c => c.Id)
            .ToListAsync();

        _cache.Set(cacheKey, consultations, DefaultCacheDuration);
        return consultations;
    }

    public async Task<List<LYBT.Entities.Consultation.Consultation>> GetByStatusAsync(ConsultationStatus status)
    {
        var cacheKey = $"{CacheKeyPrefix}status:{status}";

        if (_cache.TryGetValue<List<LYBT.Entities.Consultation.Consultation>>(cacheKey, out var cached) && cached != null)
        {
            _logger.LogDebug("从缓存获取状态看诊记录 {Status}", status);
            return cached;
        }

        // Consultation实际使用CommonStatus，需要转换
        var commonStatus = status == ConsultationStatus.InProgress ? CommonStatus.Enabled : CommonStatus.Disabled;

        var consultations = await _dbSet
            .Where(c => c.Status == commonStatus)
            .OrderByDescending(c => c.Id)
            .ToListAsync();

        _cache.Set(cacheKey, consultations, DefaultCacheDuration);
        return consultations;
    }

    public async Task<List<LYBT.Entities.Consultation.Consultation>> GetByDateRangeAsync(DateTime startDate, DateTime endDate)
    {
        var cacheKey = $"{CacheKeyPrefix}daterange:{startDate:yyyyMMdd}-{endDate:yyyyMMdd}";

        if (_cache.TryGetValue<List<LYBT.Entities.Consultation.Consultation>>(cacheKey, out var cached) && cached != null)
        {
            _logger.LogDebug("从缓存获取日期范围看诊记录 {StartDate}-{EndDate}", startDate.Date, endDate.Date);
            return cached;
        }

        // Note: 当前返回所有记录，日期范围过滤功能待v2.0实现
        var consultations = await _dbSet
            .OrderByDescending(c => c.Id)
            .ToListAsync();

        // 短缓存时间，因为缺少日期字段过滤
        _cache.Set(cacheKey, consultations, TimeSpan.FromMinutes(1));
        return consultations;
    }

    public async Task<LYBT.Entities.Consultation.Consultation?> GetByMedicalCaseIdAsync(Guid medicalCaseId)
    {
        var cacheKey = $"{CacheKeyPrefix}medicalcase:{medicalCaseId}";

        if (_cache.TryGetValue<LYBT.Entities.Consultation.Consultation?>(cacheKey, out var cached))
        {
            _logger.LogDebug("从缓存获取医案看诊记录 {MedicalCaseId}", medicalCaseId);
            return cached;
        }

        var consultation = await _dbSet
            .FirstOrDefaultAsync(c => c.MedicalCaseId == medicalCaseId);

        _cache.Set(cacheKey, consultation, DefaultCacheDuration);
        return consultation;
    }

    // AddAsync和GetListAsync由BaseRepository提供，无需重复实现
}
