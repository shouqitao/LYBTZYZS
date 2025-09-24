using AutoMapper;
using AutoMapper.QueryableExtensions;
using LYBT.Infrastructure.Data;
using LYBT.Infrastructure.Repositories;
using LYBT.Module.MedicalCase.Interfaces;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace LYBT.Module.MedicalCase.Repositories
{
    /// <summary>
    /// 医疗案例只读仓储实现 - 专门为QueryService提供数据访问
    /// 继承ReadOnlyRepository获得缓存优化，实现医疗案例特定的查询方法
    /// 使用AutoMapper ProjectTo进行高效的DTO映射
    /// </summary>
    public class MedicalCaseReadRepository : ReadOnlyRepository<LYBT.Entities.MedicalCase.MedicalCase>, IMedicalCaseReadRepository
    {
        public MedicalCaseReadRepository(
            AppDbContext context,
            IMapper mapper,
            ILogger<MedicalCaseReadRepository> logger,
            IMemoryCache cache) : base(context, mapper, logger, cache)
        {
        }

        /// <summary>
        /// 应用全局过滤器 - 排除软删除记录
        /// </summary>
        protected override IQueryable<LYBT.Entities.MedicalCase.MedicalCase> ApplyGlobalFilters(
            IQueryable<LYBT.Entities.MedicalCase.MedicalCase> query)
        {
            // 应用软删除过滤
            return query.Where(mc => !mc.IsDeleted);
        }

        public async Task<MedicalCaseDto?> GetMedicalCaseDtoByIdAsync(Guid caseId)
        {
            if (caseId == Guid.Empty)
            {
                return null;
            }

            var cacheKey = $"{CacheKeyPrefix}detail:{caseId}";

            if (_cache.TryGetValue<MedicalCaseDto?>(cacheKey, out var cached))
            {
                _logger.LogDebug("从缓存获取医疗案例详情 {Id}", caseId);
                return cached;
            }

            var medicalCaseDto = await BuildOptimizedQuery()
                .Where(mc => mc.Id == caseId)
                .ProjectTo<MedicalCaseDto>(_mapper.ConfigurationProvider)
                .FirstOrDefaultAsync();

            SetCacheSafely(cacheKey, medicalCaseDto, DefaultCacheDuration);
            return medicalCaseDto;
        }

        public async Task<PagedResult<MedicalCaseDto>> GetPagedMedicalCaseDtosAsync(PagedQueryBaseDto query)
        {
            var cacheKey = GenerateCacheKey("paged_medicalcases", 
                query.Keyword, query.PageIndex, query.PageSize);

            if (_cache.TryGetValue<PagedResult<MedicalCaseDto>>(cacheKey, out var cached))
            {
                _logger.LogDebug("从缓存获取分页医疗案例记录 Page:{PageIndex}", query.PageIndex);
                return cached!;
            }

            var queryable = BuildOptimizedQuery();

            // 基础筛选 - 排除已删除/关闭的案例
            queryable = queryable.Where(mc => mc.Status != MedicalCaseStatus.Closed);

            // 应用搜索条件
            if (!string.IsNullOrWhiteSpace(query.Keyword))
            {
                var keyword = query.Keyword.Trim();
                queryable = queryable.Where(mc =>
                    mc.PatientName.Contains(keyword) ||
                    mc.DoctorName.Contains(keyword) ||
                    (mc.Remark != null && mc.Remark.Contains(keyword)));
            }

            // 排序：按诊疗时间降序
            queryable = queryable.OrderByDescending(mc => mc.ConsultationDate);

            // 执行分页查询并映射为DTO
            var totalCount = await queryable.CountAsync();
            var medicalCaseDtos = await queryable
                .Skip((query.PageIndex - 1) * query.PageSize)
                .Take(query.PageSize)
                .ProjectTo<MedicalCaseDto>(_mapper.ConfigurationProvider)
                .ToListAsync();

            var result = new PagedResult<MedicalCaseDto>(
                medicalCaseDtos,
                totalCount,
                query.PageIndex,
                query.PageSize);

            SetCacheSafely(cacheKey, result, DefaultCacheDuration);
            return result;
        }

        public async Task<List<MedicalCaseDto>> GetMedicalCaseDtosByPatientIdAsync(Guid patientId)
        {
            if (patientId == Guid.Empty)
            {
                return new List<MedicalCaseDto>();
            }

            var cacheKey = $"{CacheKeyPrefix}patient_cases:{patientId}";

            if (_cache.TryGetValue<List<MedicalCaseDto>>(cacheKey, out var cached))
            {
                _logger.LogDebug("从缓存获取患者医疗案例 {PatientId}", patientId);
                return cached!;
            }

            var medicalCaseDtos = await BuildOptimizedQuery()
                .Where(mc => mc.PatientId == patientId && mc.Status != MedicalCaseStatus.Closed)
                .OrderByDescending(mc => mc.ConsultationDate)
                .ProjectTo<MedicalCaseDto>(_mapper.ConfigurationProvider)
                .ToListAsync();

            SetCacheSafely(cacheKey, medicalCaseDtos, DefaultCacheDuration);
            return medicalCaseDtos;
        }

        public async Task<MedicalCaseDto?> GetActiveMedicalCaseDtoByPatientIdAsync(Guid patientId)
        {
            if (patientId == Guid.Empty)
            {
                return null;
            }

            var cacheKey = $"{CacheKeyPrefix}active_case:{patientId}";

            if (_cache.TryGetValue<MedicalCaseDto?>(cacheKey, out var cached))
            {
                _logger.LogDebug("从缓存获取患者活跃医疗案例 {PatientId}", patientId);
                return cached;
            }

            var activeCaseDto = await BuildOptimizedQuery()
                .Where(mc => mc.PatientId == patientId && mc.Status == MedicalCaseStatus.Active)
                .ProjectTo<MedicalCaseDto>(_mapper.ConfigurationProvider)
                .FirstOrDefaultAsync();

            SetCacheSafely(cacheKey, activeCaseDto, DefaultCacheDuration);
            return activeCaseDto;
        }

        public async Task<List<MedicalCaseDto>> SearchMedicalCaseDtosAsync(string keyword, int maxResults = 50)
        {
            if (string.IsNullOrWhiteSpace(keyword))
            {
                return new List<MedicalCaseDto>();
            }

            var cacheKey = GenerateCacheKey("search_medicalcases", keyword, maxResults);

            if (_cache.TryGetValue<List<MedicalCaseDto>>(cacheKey, out var cached))
            {
                _logger.LogDebug("从缓存获取搜索结果 Keyword:{Keyword}", keyword);
                return cached!;
            }

            var searchTerm = keyword.Trim();
            var medicalCaseDtos = await BuildOptimizedQuery()
                .Where(mc => mc.Status != MedicalCaseStatus.Closed &&
                           (mc.PatientName.Contains(searchTerm) ||
                            mc.DoctorName.Contains(searchTerm) ||
                            (mc.Remark != null && mc.Remark.Contains(searchTerm))))
                .OrderByDescending(mc => mc.ConsultationDate)
                .Take(maxResults)
                .ProjectTo<MedicalCaseDto>(_mapper.ConfigurationProvider)
                .ToListAsync();

            SetCacheSafely(cacheKey, medicalCaseDtos, TimeSpan.FromMinutes(2)); // 搜索结果较短缓存时间
            return medicalCaseDtos;
        }

        public async Task<bool> HasActiveCaseAsync(Guid patientId)
        {
            if (patientId == Guid.Empty)
            {
                return false;
            }

            var cacheKey = $"{CacheKeyPrefix}has_active_case:{patientId}";

            if (_cache.TryGetValue<bool>(cacheKey, out var cached))
            {
                _logger.LogDebug("从缓存检查患者活跃案例 {PatientId}", patientId);
                return cached;
            }

            var hasActiveCase = await BuildOptimizedQuery()
                .AnyAsync(mc => mc.PatientId == patientId && mc.Status == MedicalCaseStatus.Active);

            SetCacheSafely(cacheKey, hasActiveCase, TimeSpan.FromMinutes(5)); // 活跃案例状态缓存5分钟
            return hasActiveCase;
        }

        public async Task<List<MedicalCaseDto>> GetHistoryMedicalCaseDtosAsync(Guid patientId)
        {
            if (patientId == Guid.Empty)
            {
                return new List<MedicalCaseDto>();
            }

            var cacheKey = $"{CacheKeyPrefix}history_cases:{patientId}";

            if (_cache.TryGetValue<List<MedicalCaseDto>>(cacheKey, out var cached))
            {
                _logger.LogDebug("从缓存获取患者历史医疗案例 {PatientId}", patientId);
                return cached!;
            }

            // 获取患者的已完成案例作为历史记录
            var historyCaseDtos = await BuildOptimizedQuery()
                .Where(mc => mc.PatientId == patientId && mc.Status == MedicalCaseStatus.Closed)
                .OrderByDescending(mc => mc.ConsultationDate)
                .ProjectTo<MedicalCaseDto>(_mapper.ConfigurationProvider)
                .ToListAsync();

            SetCacheSafely(cacheKey, historyCaseDtos, DefaultCacheDuration);
            return historyCaseDtos;
        }

        public async Task<List<MedicalCaseDto>> GetMedicalCaseDtosByDoctorIdAsync(Guid doctorId)
        {
            if (doctorId == Guid.Empty)
            {
                return new List<MedicalCaseDto>();
            }

            var cacheKey = $"{CacheKeyPrefix}doctor_cases:{doctorId}";

            if (_cache.TryGetValue<List<MedicalCaseDto>>(cacheKey, out var cached))
            {
                _logger.LogDebug("从缓存获取医生医疗案例 {DoctorId}", doctorId);
                return cached!;
            }

            var medicalCaseDtos = await BuildOptimizedQuery()
                .Where(mc => mc.DoctorId == doctorId && mc.Status != MedicalCaseStatus.Closed)
                .OrderByDescending(mc => mc.ConsultationDate)
                .ProjectTo<MedicalCaseDto>(_mapper.ConfigurationProvider)
                .ToListAsync();

            SetCacheSafely(cacheKey, medicalCaseDtos, DefaultCacheDuration);
            return medicalCaseDtos;
        }

        public async Task<List<MedicalCaseDto>> GetMedicalCaseDtosByStatusAsync(MedicalCaseStatus status)
        {
            var cacheKey = $"{CacheKeyPrefix}status_cases:{status}";

            if (_cache.TryGetValue<List<MedicalCaseDto>>(cacheKey, out var cached))
            {
                _logger.LogDebug("从缓存获取状态医疗案例 Status:{Status}", status);
                return cached!;
            }

            var medicalCaseDtos = await BuildOptimizedQuery()
                .Where(mc => mc.Status == status)
                .OrderByDescending(mc => mc.ConsultationDate)
                .ProjectTo<MedicalCaseDto>(_mapper.ConfigurationProvider)
                .ToListAsync();

            SetCacheSafely(cacheKey, medicalCaseDtos, DefaultCacheDuration);
            return medicalCaseDtos;
        }

        public async Task<object> GetMedicalCaseStatisticsAsync()
        {
            var cacheKey = $"{CacheKeyPrefix}statistics";

            if (_cache.TryGetValue<object>(cacheKey, out var cached))
            {
                _logger.LogDebug("从缓存获取医疗案例统计信息");
                return cached!;
            }

            // Record-Only模式：极简统计，仅业务运行必需
            var statistics = new
            {
                Message = "统计功能在简化版本中暂不提供",
                GeneratedAt = DateTime.Now
            };

            await Task.CompletedTask; // 保持异步签名
            SetCacheSafely(cacheKey, statistics, TimeSpan.FromMinutes(10)); // 统计信息缓存10分钟

            return statistics;
        }
    }
}