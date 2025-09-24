using AutoMapper;
using AutoMapper.QueryableExtensions;
using LYBT.Infrastructure.Data;
using LYBT.Infrastructure.Repositories;
using LYBT.Module.Consultation.Interfaces;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Shared.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace LYBT.Module.Consultation.Repositories
{
    /// <summary>
    /// 诊疗只读仓储实现 - 专门为QueryService提供数据访问
    /// 继承ReadOnlyRepository获得缓存优化，实现诊疗特定的查询方法
    /// 使用AutoMapper ProjectTo进行高效的DTO映射
    /// </summary>
    public class ConsultationReadRepository : ReadOnlyRepository<LYBT.Entities.Consultation.Consultation>, IConsultationReadRepository
    {
        public ConsultationReadRepository(
            AppDbContext context,
            IMapper mapper,
            ILogger<ConsultationReadRepository> logger,
            IMemoryCache cache) : base(context, mapper, logger, cache)
        {
        }

        /// <summary>
        /// 应用全局过滤器 - 排除软删除记录
        /// </summary>
        protected override IQueryable<LYBT.Entities.Consultation.Consultation> ApplyGlobalFilters(
            IQueryable<LYBT.Entities.Consultation.Consultation> query)
        {
            // 应用软删除过滤
            return query.Where(c => !c.IsDeleted);
        }

        public async Task<PagedResult<ConsultationDto>> GetPagedConsultationDtosAsync(ConsultationQueryDto query)
        {
            var cacheKey = GenerateCacheKey("paged_consultations", query.Keyword, query.PageIndex, query.PageSize);

            if (_cache.TryGetValue<PagedResult<ConsultationDto>>(cacheKey, out var cached))
            {
                _logger.LogDebug("从缓存获取分页诊疗记录 Page:{PageIndex}", query.PageIndex);
                return cached!;
            }

            var dbQuery = BuildOptimizedQuery();

            // 基础筛选 - 只查询有效状态的诊疗
            dbQuery = dbQuery.Where(c => c.Status == CommonStatus.Enabled);

            // 应用搜索条件
            if (!string.IsNullOrWhiteSpace(query.Keyword))
            {
                var searchTerm = query.Keyword.Trim();
                dbQuery = dbQuery.Where(c =>
                    (c.ChiefComplaint != null && c.ChiefComplaint.Contains(searchTerm)) ||
                    (c.PresentIllness != null && c.PresentIllness.Contains(searchTerm)) ||
                    (c.TCMDiagnosis != null && c.TCMDiagnosis.Contains(searchTerm)));
            }

            // 排序：按创建时间降序
            dbQuery = dbQuery.OrderByDescending(c => c.CreatedAt);

            // 执行分页查询并映射为DTO
            var totalCount = await dbQuery.CountAsync();
            var consultationDtos = await dbQuery
                .Skip((query.PageIndex - 1) * query.PageSize)
                .Take(query.PageSize)
                .ProjectTo<ConsultationDto>(_mapper.ConfigurationProvider)
                .ToListAsync();

            var result = new PagedResult<ConsultationDto>(
                consultationDtos,
                totalCount,
                query.PageIndex,
                query.PageSize);

            SetCacheSafely(cacheKey, result, DefaultCacheDuration);
            return result;
        }

        public async Task<List<ConsultationDto>> GetConsultationDtosByPatientIdAsync(Guid patientId)
        {
            if (patientId == Guid.Empty)
            {
                return new List<ConsultationDto>();
            }

            var cacheKey = $"{CacheKeyPrefix}patient_consultations:{patientId}";

            if (_cache.TryGetValue<List<ConsultationDto>>(cacheKey, out var cached))
            {
                _logger.LogDebug("从缓存获取患者诊疗记录 {PatientId}", patientId);
                return cached!;
            }

            var consultationDtos = await BuildOptimizedQuery()
                .Where(c => c.PatientId == patientId && c.Status == CommonStatus.Enabled)
                .OrderByDescending(c => c.CreatedAt)
                .ProjectTo<ConsultationDto>(_mapper.ConfigurationProvider)
                .ToListAsync();

            SetCacheSafely(cacheKey, consultationDtos, DefaultCacheDuration);
            return consultationDtos;
        }

        public async Task<List<ConsultationDto>> GetConsultationDtosByMedicalCaseIdAsync(Guid medicalCaseId)
        {
            if (medicalCaseId == Guid.Empty)
            {
                return new List<ConsultationDto>();
            }

            var cacheKey = $"{CacheKeyPrefix}medicalcase_consultations:{medicalCaseId}";

            if (_cache.TryGetValue<List<ConsultationDto>>(cacheKey, out var cached))
            {
                _logger.LogDebug("从缓存获取医疗案例诊疗记录 {MedicalCaseId}", medicalCaseId);
                return cached!;
            }

            var consultationDtos = await BuildOptimizedQuery()
                .Where(c => c.MedicalCaseId == medicalCaseId && c.Status == CommonStatus.Enabled)
                .OrderByDescending(c => c.CreatedAt)
                .ProjectTo<ConsultationDto>(_mapper.ConfigurationProvider)
                .ToListAsync();

            SetCacheSafely(cacheKey, consultationDtos, DefaultCacheDuration);
            return consultationDtos;
        }

        public async Task<List<ConsultationDto>> GetConsultationDtosByDoctorIdAsync(Guid doctorId)
        {
            if (doctorId == Guid.Empty)
            {
                return new List<ConsultationDto>();
            }

            var cacheKey = $"{CacheKeyPrefix}doctor_consultations:{doctorId}";

            if (_cache.TryGetValue<List<ConsultationDto>>(cacheKey, out var cached))
            {
                _logger.LogDebug("从缓存获取医生诊疗记录 {DoctorId}", doctorId);
                return cached!;
            }

            var consultationDtos = await BuildOptimizedQuery()
                .Where(c => c.UserId == doctorId && c.Status == CommonStatus.Enabled)
                .OrderByDescending(c => c.CreatedAt)
                .ProjectTo<ConsultationDto>(_mapper.ConfigurationProvider)
                .ToListAsync();

            SetCacheSafely(cacheKey, consultationDtos, DefaultCacheDuration);
            return consultationDtos;
        }

        public async Task<List<ConsultationDto>> SearchConsultationDtosAsync(string keyword, int maxResults = 50)
        {
            if (string.IsNullOrWhiteSpace(keyword))
            {
                return new List<ConsultationDto>();
            }

            var cacheKey = GenerateCacheKey("search_consultations", keyword, maxResults);

            if (_cache.TryGetValue<List<ConsultationDto>>(cacheKey, out var cached))
            {
                _logger.LogDebug("从缓存获取搜索结果 Keyword:{Keyword}", keyword);
                return cached!;
            }

            var searchTerm = keyword.Trim();
            var consultationDtos = await BuildOptimizedQuery()
                .Where(c => c.Status == CommonStatus.Enabled &&
                           ((c.ChiefComplaint != null && c.ChiefComplaint.Contains(searchTerm)) ||
                            (c.PresentIllness != null && c.PresentIllness.Contains(searchTerm)) ||
                            (c.TCMDiagnosis != null && c.TCMDiagnosis.Contains(searchTerm))))
                .OrderByDescending(c => c.CreatedAt)
                .Take(maxResults)
                .ProjectTo<ConsultationDto>(_mapper.ConfigurationProvider)
                .ToListAsync();

            SetCacheSafely(cacheKey, consultationDtos, TimeSpan.FromMinutes(2)); // 搜索结果较短缓存时间
            return consultationDtos;
        }

        public async Task<List<ConsultationDto>> GetPatientHistoryDtosAsync(Guid patientId)
        {
            if (patientId == Guid.Empty)
            {
                return new List<ConsultationDto>();
            }

            var cacheKey = $"{CacheKeyPrefix}patient_history:{patientId}";

            if (_cache.TryGetValue<List<ConsultationDto>>(cacheKey, out var cached))
            {
                _logger.LogDebug("从缓存获取患者历史记录 {PatientId}", patientId);
                return cached!;
            }

            // 历史记录查询已禁用状态的诊疗记录
            var consultationDtos = await BuildOptimizedQuery()
                .Where(c => c.PatientId == patientId && c.Status == CommonStatus.Disabled)
                .OrderByDescending(c => c.CreatedAt)
                .ProjectTo<ConsultationDto>(_mapper.ConfigurationProvider)
                .ToListAsync();

            SetCacheSafely(cacheKey, consultationDtos, DefaultCacheDuration);
            return consultationDtos;
        }

        public async Task<ConsultationDetailDto?> GetConsultationDetailDtoAsync(Guid id)
        {
            if (id == Guid.Empty)
            {
                return null;
            }

            var cacheKey = $"{CacheKeyPrefix}detail:{id}";

            if (_cache.TryGetValue<ConsultationDetailDto?>(cacheKey, out var cached))
            {
                _logger.LogDebug("从缓存获取诊疗详情 {Id}", id);
                return cached;
            }

            var consultationDetailDto = await BuildOptimizedQuery()
                .Where(c => c.Id == id && c.Status == CommonStatus.Enabled)
                .ProjectTo<ConsultationDetailDto>(_mapper.ConfigurationProvider)
                .FirstOrDefaultAsync();

            SetCacheSafely(cacheKey, consultationDetailDto, DefaultCacheDuration);
            return consultationDetailDto;
        }

        /// <summary>
        /// 根据ID获取诊疗详情DTO (别名方法)
        /// </summary>
        public async Task<ConsultationDetailDto?> GetConsultationDetailDtoByIdAsync(Guid id)
        {
            return await GetConsultationDetailDtoAsync(id);
        }

        /// <summary>
        /// 获取患者诊疗历史记录DTO列表 (别名方法)
        /// </summary>
        public async Task<List<ConsultationDto>> GetPatientConsultationHistoryAsync(Guid patientId)
        {
            return await GetPatientHistoryDtosAsync(patientId);
        }
    }
}