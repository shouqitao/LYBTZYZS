using AutoMapper;
using AutoMapper.QueryableExtensions;
using LYBT.Infrastructure.Data;
using LYBT.Infrastructure.Repositories;
using LYBT.Module.Prescriptions.Interfaces;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Prescriptions;
using LYBT.Shared.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace LYBT.Module.Prescriptions.Repositories
{
    /// <summary>
    /// 处方只读仓储实现 - 专门为QueryService提供数据访问
    /// 继承ReadOnlyRepository获得缓存优化，实现处方特定的查询方法
    /// 使用AutoMapper ProjectTo进行高效的DTO映射
    /// </summary>
    public class PrescriptionReadRepository : ReadOnlyRepository<LYBT.Entities.Prescriptions.Prescription>, IPrescriptionReadRepository
    {
        public PrescriptionReadRepository(
            AppDbContext context,
            IMapper mapper,
            ILogger<PrescriptionReadRepository> logger,
            IMemoryCache cache) : base(context, mapper, logger, cache)
        {
        }

        /// <summary>
        /// 应用全局过滤器 - 排除软删除记录
        /// </summary>
        protected override IQueryable<LYBT.Entities.Prescriptions.Prescription> ApplyGlobalFilters(
            IQueryable<LYBT.Entities.Prescriptions.Prescription> query)
        {
            // 应用软删除过滤 - 排除标记为删除的处方
            return query.Where(p => !p.IsDeleted && 
                               (p.Remark == null || !p.Remark.Contains("处方已删除")));
        }

        public async Task<PrescriptionDto?> GetPrescriptionDtoByIdAsync(Guid id)
        {
            if (id == Guid.Empty)
            {
                return null;
            }

            var cacheKey = $"{CacheKeyPrefix}detail:{id}";

            if (_cache.TryGetValue<PrescriptionDto?>(cacheKey, out var cached))
            {
                _logger.LogDebug("从缓存获取处方详情 {Id}", id);
                return cached;
            }

            var prescriptionDto = await BuildOptimizedQuery()
                .Include(p => p.Items) // 包含处方项目
                .Where(p => p.Id == id)
                .ProjectTo<PrescriptionDto>(_mapper.ConfigurationProvider)
                .FirstOrDefaultAsync();

            SetCacheSafely(cacheKey, prescriptionDto, DefaultCacheDuration);
            return prescriptionDto;
        }

        public async Task<PagedResult<PrescriptionDto>> GetPagedPrescriptionDtosAsync(PrescriptionQueryDto query)
        {
            var cacheKey = GenerateCacheKey("paged_prescriptions", 
                query.Keyword, query.PatientId, query.DoctorId, query.Status, 
                query.StartDate, query.EndDate, query.PageIndex, query.PageSize);

            if (_cache.TryGetValue<PagedResult<PrescriptionDto>>(cacheKey, out var cached))
            {
                _logger.LogDebug("从缓存获取分页处方记录 Page:{PageIndex}", query.PageIndex);
                return cached!;
            }

            IQueryable<LYBT.Entities.Prescriptions.Prescription> queryable = BuildOptimizedQuery().Include(p => p.Items);

            // 应用搜索条件
            if (!string.IsNullOrWhiteSpace(query.Keyword))
            {
                var keyword = query.Keyword.Trim();
                queryable = queryable.Where(p =>
                    (p.Indication != null && p.Indication.Contains(keyword)) ||
                    (p.Remark != null && p.Remark.Contains(keyword)) ||
                    (p.Advice != null && p.Advice.Contains(keyword)));
            }

            // 患者筛选
            if (query.PatientId.HasValue)
            {
                queryable = queryable.Where(p => p.PatientId == query.PatientId.Value);
            }

            // 医生筛选
            if (query.DoctorId.HasValue)
            {
                queryable = queryable.Where(p => p.UserId == query.DoctorId.Value);
            }

            // 状态筛选
            if (query.Status.HasValue)
            {
                // 将查询DTO的状态转换为实体状态
                var prescriptionStatus = query.Status.Value == 0 ? PrescriptionStatus.Draft : PrescriptionStatus.Completed;
                queryable = queryable.Where(p => p.Status == prescriptionStatus);
            }

            // 日期范围筛选
            if (query.StartDate.HasValue)
            {
                queryable = queryable.Where(p => p.CreatedAt >= query.StartDate.Value);
            }

            if (query.EndDate.HasValue)
            {
                queryable = queryable.Where(p => p.CreatedAt <= query.EndDate.Value);
            }

            // 排序：按创建时间降序
            queryable = queryable.OrderByDescending(p => p.CreatedAt);

            // 执行分页查询并映射为DTO
            var totalCount = await queryable.CountAsync();
            var prescriptionDtos = await queryable
                .Skip((query.PageIndex - 1) * query.PageSize)
                .Take(query.PageSize)
                .ProjectTo<PrescriptionDto>(_mapper.ConfigurationProvider)
                .ToListAsync();

            var result = new PagedResult<PrescriptionDto>(
                prescriptionDtos,
                totalCount,
                query.PageIndex,
                query.PageSize);

            SetCacheSafely(cacheKey, result, DefaultCacheDuration);
            return result;
        }

        public async Task<List<PrescriptionDto>> GetPrescriptionDtosByPatientIdAsync(Guid patientId)
        {
            if (patientId == Guid.Empty)
            {
                return new List<PrescriptionDto>();
            }

            var cacheKey = $"{CacheKeyPrefix}patient_prescriptions:{patientId}";

            if (_cache.TryGetValue<List<PrescriptionDto>>(cacheKey, out var cached))
            {
                _logger.LogDebug("从缓存获取患者处方记录 {PatientId}", patientId);
                return cached!;
            }

            var prescriptionDtos = await BuildOptimizedQuery()
                .Include(p => p.Items)
                .Where(p => p.PatientId == patientId)
                .OrderByDescending(p => p.CreatedAt)
                .ProjectTo<PrescriptionDto>(_mapper.ConfigurationProvider)
                .ToListAsync();

            SetCacheSafely(cacheKey, prescriptionDtos, DefaultCacheDuration);
            return prescriptionDtos;
        }

        public async Task<List<PrescriptionDto>> GetPrescriptionDtosByMedicalCaseIdAsync(Guid medicalCaseId)
        {
            if (medicalCaseId == Guid.Empty)
            {
                return new List<PrescriptionDto>();
            }

            var cacheKey = $"{CacheKeyPrefix}medicalcase_prescriptions:{medicalCaseId}";

            if (_cache.TryGetValue<List<PrescriptionDto>>(cacheKey, out var cached))
            {
                _logger.LogDebug("从缓存获取医疗案例处方记录 {MedicalCaseId}", medicalCaseId);
                return cached!;
            }

            var prescriptionDtos = await BuildOptimizedQuery()
                .Include(p => p.Items)
                .Where(p => p.MedicalCaseId == medicalCaseId)
                .OrderByDescending(p => p.CreatedAt)
                .ProjectTo<PrescriptionDto>(_mapper.ConfigurationProvider)
                .ToListAsync();

            SetCacheSafely(cacheKey, prescriptionDtos, DefaultCacheDuration);
            return prescriptionDtos;
        }

        public async Task<List<PrescriptionDto>> SearchPrescriptionDtosAsync(string keyword, int maxResults = 50)
        {
            if (string.IsNullOrWhiteSpace(keyword))
            {
                return new List<PrescriptionDto>();
            }

            var cacheKey = GenerateCacheKey("search_prescriptions", keyword, maxResults);

            if (_cache.TryGetValue<List<PrescriptionDto>>(cacheKey, out var cached))
            {
                _logger.LogDebug("从缓存获取搜索结果 Keyword:{Keyword}", keyword);
                return cached!;
            }

            var searchTerm = keyword.Trim();
            var prescriptionDtos = await BuildOptimizedQuery()
                .Include(p => p.Items)
                .Where(p => (p.Indication != null && p.Indication.Contains(searchTerm)) ||
                           (p.Advice != null && p.Advice.Contains(searchTerm)) ||
                           (p.Remark != null && p.Remark.Contains(searchTerm)))
                .OrderByDescending(p => p.CreatedAt)
                .Take(maxResults)
                .ProjectTo<PrescriptionDto>(_mapper.ConfigurationProvider)
                .ToListAsync();

            SetCacheSafely(cacheKey, prescriptionDtos, TimeSpan.FromMinutes(2)); // 搜索结果较短缓存时间
            return prescriptionDtos;
        }

        public async Task<List<PrescriptionDto>> GetAllPrescriptionDtosAsync()
        {
            var cacheKey = $"{CacheKeyPrefix}all_prescriptions";

            if (_cache.TryGetValue<List<PrescriptionDto>>(cacheKey, out var cached))
            {
                _logger.LogDebug("从缓存获取所有处方记录");
                return cached!;
            }

            var prescriptionDtos = await BuildOptimizedQuery()
                .Include(p => p.Items)
                .OrderByDescending(p => p.CreatedAt)
                .ProjectTo<PrescriptionDto>(_mapper.ConfigurationProvider)
                .ToListAsync();

            SetCacheSafely(cacheKey, prescriptionDtos, DefaultCacheDuration);
            return prescriptionDtos;
        }

        public async Task<List<PrescriptionDto>> GetDoctorPrescriptionDtosAsync(Guid doctorId)
        {
            if (doctorId == Guid.Empty)
            {
                return new List<PrescriptionDto>();
            }

            var cacheKey = $"{CacheKeyPrefix}doctor_prescriptions:{doctorId}";

            if (_cache.TryGetValue<List<PrescriptionDto>>(cacheKey, out var cached))
            {
                _logger.LogDebug("从缓存获取医生处方记录 {DoctorId}", doctorId);
                return cached!;
            }

            var prescriptionDtos = await BuildOptimizedQuery()
                .Include(p => p.Items)
                .Where(p => p.UserId == doctorId)
                .OrderByDescending(p => p.CreatedAt)
                .ProjectTo<PrescriptionDto>(_mapper.ConfigurationProvider)
                .ToListAsync();

            SetCacheSafely(cacheKey, prescriptionDtos, DefaultCacheDuration);
            return prescriptionDtos;
        }

        public async Task<PrescriptionStatsDto> GetPrescriptionStatsAsync()
        {
            var cacheKey = $"{CacheKeyPrefix}stats";

            if (_cache.TryGetValue<PrescriptionStatsDto>(cacheKey, out var cached))
            {
                _logger.LogDebug("从缓存获取处方统计信息");
                return cached!;
            }

            var query = BuildOptimizedQuery();

            var stats = new PrescriptionStatsDto
            {
                TotalCount = await query.CountAsync(),
                DraftCount = await query.CountAsync(p => p.Status == PrescriptionStatus.Draft),
                CompletedCount = await query.CountAsync(p => p.Status == PrescriptionStatus.Completed)
            };

            SetCacheSafely(cacheKey, stats, TimeSpan.FromMinutes(10)); // 统计信息缓存10分钟
            return stats;
        }

        /// <summary>
        /// 获取医生今日处方DTO列表
        /// </summary>
        public async Task<List<PrescriptionDto>> GetDoctorTodayPrescriptionDtosAsync(Guid doctorId)
        {
            if (doctorId == Guid.Empty)
            {
                return new List<PrescriptionDto>();
            }

            var today = DateTime.Today;
            var tomorrow = today.AddDays(1);

            var cacheKey = $"{CacheKeyPrefix}doctor_today_prescriptions:{doctorId}:{today:yyyy-MM-dd}";

            if (_cache.TryGetValue<List<PrescriptionDto>>(cacheKey, out var cached))
            {
                _logger.LogDebug("从缓存获取医生今日处方记录 {DoctorId}", doctorId);
                return cached!;
            }

            var prescriptionDtos = await BuildOptimizedQuery()
                .Include(p => p.Items)
                .Where(p => p.UserId == doctorId && p.CreatedAt >= today && p.CreatedAt < tomorrow)
                .OrderByDescending(p => p.CreatedAt)
                .ProjectTo<PrescriptionDto>(_mapper.ConfigurationProvider)
                .ToListAsync();

            // 今日处方缓存较短时间，确保数据实时性
            SetCacheSafely(cacheKey, prescriptionDtos, TimeSpan.FromMinutes(5)); 
            return prescriptionDtos;
        }
    }
}