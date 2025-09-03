using LYBT.Infrastructure.Data;
using LYBT.Infrastructure.Repositories;
using LYBT.Module.MedicalCase.Interfaces;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Memory;
using System.Linq.Expressions;

namespace LYBT.Module.MedicalCase.Repositories;

/// <summary>
/// 医疗案例仓储实现 - 数据层统一化重构
/// 继承OptimizedBaseRepository获得缓存和性能优化，覆盖部分方法以支持Include
/// </summary>
public class MedicalCaseRepository : OptimizedBaseRepository<LYBT.Entities.MedicalCase.MedicalCase>, IMedicalCaseRepository
{
    public MedicalCaseRepository(
        AppDbContext context,
        ILogger<MedicalCaseRepository> logger,
        IMemoryCache cache) : base(context, logger, cache)
    {
    }

    // 覆盖基类方法以支持Include和缓存
    public override async Task<LYBT.Entities.MedicalCase.MedicalCase?> GetByIdAsync(Guid id)
    {
        var cacheKey = $"{CacheKeyPrefix}withConsultation:{id}";
        
        if (_cache.TryGetValue<LYBT.Entities.MedicalCase.MedicalCase?>(cacheKey, out var cached))
        {
            _logger.LogDebug("从缓存获取医案详情 {Id}", id);
            return cached;
        }
        
        var medicalCase = await _dbSet
            .Include(m => m.Consultation)
            .FirstOrDefaultAsync(m => m.Id == id);
            
        _cache.Set(cacheKey, medicalCase, DefaultCacheDuration);
        return medicalCase;
    }

    public override async Task<IEnumerable<LYBT.Entities.MedicalCase.MedicalCase>> GetAllAsync()
    {
        var cacheKey = $"{CacheKeyPrefix}allWithConsultation";
        
        if (_cache.TryGetValue<IEnumerable<LYBT.Entities.MedicalCase.MedicalCase>>(cacheKey, out var cached) && cached != null)
        {
            _logger.LogDebug("从缓存获取所有医案列表");
            return cached;
        }
        
        var medicalCases = await _dbSet
            .Include(m => m.Consultation)
            .ToListAsync();
            
        _cache.Set(cacheKey, medicalCases, DefaultCacheDuration);
        return medicalCases;
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

    // 医疗案例特有的业务方法（带缓存优化）
    public async Task<List<LYBT.Entities.MedicalCase.MedicalCase>> GetByPatientIdAsync(Guid patientId)
    {
        var cacheKey = $"{CacheKeyPrefix}patient:{patientId}";
        
        if (_cache.TryGetValue<List<LYBT.Entities.MedicalCase.MedicalCase>>(cacheKey, out var cached) && cached != null)
        {
            _logger.LogDebug("从缓存获取患者医案记录 {PatientId}", patientId);
            return cached;
        }
        
        var medicalCases = await _dbSet
            .Include(m => m.Consultation)
            .Where(m => m.PatientId == patientId)
            .OrderByDescending(m => m.ConsultationDate)
            .ToListAsync();
            
        _cache.Set(cacheKey, medicalCases, DefaultCacheDuration);
        return medicalCases;
    }

    public async Task<List<LYBT.Entities.MedicalCase.MedicalCase>> GetByUserIdAsync(Guid userId)
    {
        var cacheKey = $"{CacheKeyPrefix}doctor:{userId}";
        
        if (_cache.TryGetValue<List<LYBT.Entities.MedicalCase.MedicalCase>>(cacheKey, out var cached) && cached != null)
        {
            _logger.LogDebug("从缓存获取医生医案记录 {UserId}", userId);
            return cached;
        }
        
        var medicalCases = await _dbSet
            .Include(m => m.Consultation)
            .Where(m => m.DoctorId == userId)
            .OrderByDescending(m => m.ConsultationDate)
            .ToListAsync();
            
        _cache.Set(cacheKey, medicalCases, DefaultCacheDuration);
        return medicalCases;
    }

    public async Task<List<LYBT.Entities.MedicalCase.MedicalCase>> GetByStatusAsync(MedicalCaseStatus status)
    {
        var cacheKey = $"{CacheKeyPrefix}status:{status}";
        
        if (_cache.TryGetValue<List<LYBT.Entities.MedicalCase.MedicalCase>>(cacheKey, out var cached) && cached != null)
        {
            _logger.LogDebug("从缓存获取状态医案记录 {Status}", status);
            return cached;
        }
        
        var medicalCases = await _dbSet
            .Include(m => m.Consultation)
            .Where(m => m.Status == status)
            .OrderByDescending(m => m.ConsultationDate)
            .ToListAsync();
            
        _cache.Set(cacheKey, medicalCases, DefaultCacheDuration);
        return medicalCases;
    }

    public async Task<List<LYBT.Entities.MedicalCase.MedicalCase>> GetByDateRangeAsync(DateTime startDate, DateTime endDate)
    {
        var cacheKey = $"{CacheKeyPrefix}daterange:{startDate:yyyyMMdd}-{endDate:yyyyMMdd}";
        
        if (_cache.TryGetValue<List<LYBT.Entities.MedicalCase.MedicalCase>>(cacheKey, out var cached) && cached != null)
        {
            _logger.LogDebug("从缓存获取日期范围医案记录 {StartDate}-{EndDate}", startDate.Date, endDate.Date);
            return cached;
        }
        
        var medicalCases = await _dbSet
            .Include(m => m.Consultation)
            .Where(m => m.ConsultationDate >= startDate && m.ConsultationDate <= endDate)
            .OrderByDescending(m => m.ConsultationDate)
            .ToListAsync();
            
        _cache.Set(cacheKey, medicalCases, DefaultCacheDuration);
        return medicalCases;
    }

    public async Task<LYBT.Entities.MedicalCase.MedicalCase?> GetLatestByPatientIdAsync(Guid patientId)
    {
        var cacheKey = $"{CacheKeyPrefix}latest:patient:{patientId}";
        
        if (_cache.TryGetValue<LYBT.Entities.MedicalCase.MedicalCase?>(cacheKey, out var cached))
        {
            _logger.LogDebug("从缓存获取患者最新医案 {PatientId}", patientId);
            return cached;
        }
        
        var latestMedicalCase = await _dbSet
            .Include(m => m.Consultation)
            .Where(m => m.PatientId == patientId)
            .OrderByDescending(m => m.ConsultationDate)
            .FirstOrDefaultAsync();
            
        // 短缓存时间，因为这个数据可能经常变化
        _cache.Set(cacheKey, latestMedicalCase, TimeSpan.FromMinutes(2));
        return latestMedicalCase;
    }

    // AddAsync和GetListAsync由BaseRepository提供，无需重复实现
}