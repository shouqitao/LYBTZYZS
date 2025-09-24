using AutoMapper;
using AutoMapper.QueryableExtensions;
using LYBT.Infrastructure.Data;
using LYBT.Infrastructure.Repositories;
using LYBT.Module.Patients.Interfaces;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Shared.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace LYBT.Module.Patients.Repositories
{
    /// <summary>
    /// 患者只读仓储实现 - 专门为QueryService提供数据访问
    /// 继承ReadOnlyRepository获得缓存优化，实现患者特定的查询方法
    /// 使用AutoMapper ProjectTo进行高效的DTO映射
    /// </summary>
    public class PatientReadRepository : ReadOnlyRepository<LYBT.Entities.Patients.Patient>, IPatientReadRepository
    {
        public PatientReadRepository(
            AppDbContext context,
            IMapper mapper,
            ILogger<PatientReadRepository> logger,
            IMemoryCache cache) : base(context, mapper, logger, cache)
        {
        }

        /// <summary>
        /// 应用全局过滤器 - 排除软删除记录
        /// </summary>
        protected override IQueryable<LYBT.Entities.Patients.Patient> ApplyGlobalFilters(
            IQueryable<LYBT.Entities.Patients.Patient> query)
        {
            // 应用软删除过滤
            return query.Where(p => !p.IsDeleted);
        }

        public async Task<PagedResult<PatientDto>> GetPagedPatientDtosAsync(PagedQueryBaseDto query)
        {
            var cacheKey = GenerateCacheKey("paged_patients", query.Keyword, query.PageIndex, query.PageSize);

            if (_cache.TryGetValue<PagedResult<PatientDto>>(cacheKey, out var cached))
            {
                _logger.LogDebug("从缓存获取分页患者记录 Page:{PageIndex}", query.PageIndex);
                return cached!;
            }

            var queryable = BuildOptimizedQuery()
                .Where(p => p.Status == CommonStatus.Enabled);

            // 基础关键词搜索
            if (!string.IsNullOrWhiteSpace(query.Keyword))
            {
                var keyword = query.Keyword.Trim();
                queryable = queryable.Where(p =>
                    (p.Name != null && p.Name.Contains(keyword)) ||
                    (p.PhoneNumber != null && p.PhoneNumber.Contains(keyword)) ||
                    (p.PinYinCode != null && p.PinYinCode.Contains(keyword.ToUpper())));
            }

            // 排序：按创建时间降序
            queryable = queryable.OrderByDescending(p => p.CreatedAt);

            // 执行分页查询并映射为DTO
            var totalCount = await queryable.CountAsync();
            var patientDtos = await queryable
                .Skip((query.PageIndex - 1) * query.PageSize)
                .Take(query.PageSize)
                .ProjectTo<PatientDto>(_mapper.ConfigurationProvider)
                .ToListAsync();

            var result = new PagedResult<PatientDto>(
                patientDtos,
                totalCount,
                query.PageIndex,
                query.PageSize);

            SetCacheSafely(cacheKey, result, DefaultCacheDuration);
            return result;
        }

        public async Task<PatientDto?> GetPatientDtoByIdAsync(Guid patientId)
        {
            if (patientId == Guid.Empty)
            {
                return null;
            }

            var cacheKey = $"{CacheKeyPrefix}detail:{patientId}";

            if (_cache.TryGetValue<PatientDto?>(cacheKey, out var cached))
            {
                _logger.LogDebug("从缓存获取患者详情 {Id}", patientId);
                return cached;
            }

            var patientDto = await BuildOptimizedQuery()
                .Where(p => p.Id == patientId)
                .ProjectTo<PatientDto>(_mapper.ConfigurationProvider)
                .FirstOrDefaultAsync();

            SetCacheSafely(cacheKey, patientDto, DefaultCacheDuration);
            return patientDto;
        }

        public async Task<List<PatientDto>> GetAllPatientDtosAsync()
        {
            var cacheKey = $"{CacheKeyPrefix}all_patients";

            if (_cache.TryGetValue<List<PatientDto>>(cacheKey, out var cached))
            {
                _logger.LogDebug("从缓存获取所有患者列表");
                return cached!;
            }

            var patientDtos = await BuildOptimizedQuery()
                .OrderBy(p => p.Name)
                .ProjectTo<PatientDto>(_mapper.ConfigurationProvider)
                .ToListAsync();

            SetCacheSafely(cacheKey, patientDtos, DefaultCacheDuration);
            return patientDtos;
        }

        public async Task<List<PatientDto>> GetActivePatientDtosAsync()
        {
            var cacheKey = $"{CacheKeyPrefix}active_patients";

            if (_cache.TryGetValue<List<PatientDto>>(cacheKey, out var cached))
            {
                _logger.LogDebug("从缓存获取活跃患者列表");
                return cached!;
            }

            var patientDtos = await BuildOptimizedQuery()
                .Where(p => p.Status == CommonStatus.Enabled)
                .OrderBy(p => p.Name)
                .ProjectTo<PatientDto>(_mapper.ConfigurationProvider)
                .ToListAsync();

            SetCacheSafely(cacheKey, patientDtos, DefaultCacheDuration);
            return patientDtos;
        }

        public async Task<PatientDto?> GetPatientDtoByIdNumberAsync(string idNumber)
        {
            if (string.IsNullOrWhiteSpace(idNumber))
            {
                return null;
            }

            var cacheKey = $"{CacheKeyPrefix}idnumber:{idNumber}";

            if (_cache.TryGetValue<PatientDto?>(cacheKey, out var cached))
            {
                _logger.LogDebug("从缓存获取患者信息 IdNumber:{IdNumber}", idNumber);
                return cached;
            }

            var patientDto = await BuildOptimizedQuery()
                .Where(p => p.IdNumber == idNumber)
                .ProjectTo<PatientDto>(_mapper.ConfigurationProvider)
                .FirstOrDefaultAsync();

            SetCacheSafely(cacheKey, patientDto, DefaultCacheDuration);
            return patientDto;
        }

        public async Task<PatientDto?> GetPatientDtoByPhoneNumberAsync(string phoneNumber)
        {
            if (string.IsNullOrWhiteSpace(phoneNumber))
            {
                return null;
            }

            var cacheKey = $"{CacheKeyPrefix}phone:{phoneNumber}";

            if (_cache.TryGetValue<PatientDto?>(cacheKey, out var cached))
            {
                _logger.LogDebug("从缓存获取患者信息 PhoneNumber:{PhoneNumber}", phoneNumber);
                return cached;
            }

            var patientDto = await BuildOptimizedQuery()
                .Where(p => p.PhoneNumber == phoneNumber)
                .ProjectTo<PatientDto>(_mapper.ConfigurationProvider)
                .FirstOrDefaultAsync();

            SetCacheSafely(cacheKey, patientDto, DefaultCacheDuration);
            return patientDto;
        }

        public async Task<PatientDto?> GetPatientDtoByIdCardAsync(string idCard)
        {
            if (string.IsNullOrWhiteSpace(idCard))
            {
                return null;
            }

            var cacheKey = $"{CacheKeyPrefix}idcard:{idCard}";

            if (_cache.TryGetValue<PatientDto?>(cacheKey, out var cached))
            {
                _logger.LogDebug("从缓存获取患者信息 IdCard:{IdCard}", idCard);
                return cached;
            }

            var patientDto = await BuildOptimizedQuery()
                .Where(p => p.IdNumber == idCard)
                .ProjectTo<PatientDto>(_mapper.ConfigurationProvider)
                .FirstOrDefaultAsync();

            SetCacheSafely(cacheKey, patientDto, DefaultCacheDuration);
            return patientDto;
        }

        public async Task<List<PatientDto>> GetPatientDtosByPhoneAsync(string phone)
        {
            if (string.IsNullOrWhiteSpace(phone))
            {
                return new List<PatientDto>();
            }

            var cacheKey = $"{CacheKeyPrefix}phone_list:{phone}";

            if (_cache.TryGetValue<List<PatientDto>>(cacheKey, out var cached))
            {
                _logger.LogDebug("从缓存获取患者列表 Phone:{Phone}", phone);
                return cached!;
            }

            var patientDtos = await BuildOptimizedQuery()
                .Where(p => p.PhoneNumber != null && p.PhoneNumber.Contains(phone))
                .OrderBy(p => p.Name)
                .ProjectTo<PatientDto>(_mapper.ConfigurationProvider)
                .ToListAsync();

            SetCacheSafely(cacheKey, patientDtos, DefaultCacheDuration);
            return patientDtos;
        }

        public async Task<List<PatientDto>> SearchPatientDtosAsync(string keyword, int maxResults = 20)
        {
            if (string.IsNullOrWhiteSpace(keyword))
            {
                return new List<PatientDto>();
            }

            var cacheKey = GenerateCacheKey("search_patients", keyword, maxResults);

            if (_cache.TryGetValue<List<PatientDto>>(cacheKey, out var cached))
            {
                _logger.LogDebug("从缓存获取搜索结果 Keyword:{Keyword}", keyword);
                return cached!;
            }

            var searchTerm = keyword.Trim();
            var patientDtos = await BuildOptimizedQuery()
                .Where(p => p.Status == CommonStatus.Enabled && (
                    (p.Name != null && p.Name.Contains(searchTerm)) ||
                    (p.PhoneNumber != null && p.PhoneNumber.Contains(searchTerm)) ||
                    (p.IdNumber != null && p.IdNumber.Contains(searchTerm)) ||
                    (p.PinYinCode != null && p.PinYinCode.Contains(searchTerm.ToUpper()))))
                .OrderBy(p => p.Name)
                .Take(maxResults)
                .ProjectTo<PatientDto>(_mapper.ConfigurationProvider)
                .ToListAsync();

            SetCacheSafely(cacheKey, patientDtos, TimeSpan.FromMinutes(2)); // 搜索结果较短缓存时间
            return patientDtos;
        }

        public async Task<PagedResult<PatientDto>> AdvancedSearchPatientDtosAsync(PatientSearchDto searchDto)
        {
            var cacheKey = GenerateCacheKey("advanced_search_patients",
                searchDto.Keyword, searchDto.Name, searchDto.PhoneNumber, 
                searchDto.MinAge, searchDto.MaxAge, searchDto.Gender,
                searchDto.PageIndex, searchDto.PageSize);

            if (_cache.TryGetValue<PagedResult<PatientDto>>(cacheKey, out var cached))
            {
                _logger.LogDebug("从缓存获取高级搜索结果 Page:{PageIndex}", searchDto.PageIndex);
                return cached!;
            }

            var queryable = BuildOptimizedQuery()
                .Where(p => p.Status == CommonStatus.Enabled);

            // 基础关键词搜索
            if (!string.IsNullOrWhiteSpace(searchDto.Keyword))
            {
                var keyword = searchDto.Keyword.Trim();
                queryable = queryable.Where(p =>
                    (p.Name != null && p.Name.Contains(keyword)) ||
                    (p.PhoneNumber != null && p.PhoneNumber.Contains(keyword)) ||
                    (p.IdNumber != null && p.IdNumber.Contains(keyword)));
            }

            // 姓名搜索
            if (!string.IsNullOrWhiteSpace(searchDto.Name))
            {
                queryable = queryable.Where(p => p.Name != null && p.Name.Contains(searchDto.Name));
            }

            // 手机号搜索
            if (!string.IsNullOrWhiteSpace(searchDto.PhoneNumber))
            {
                queryable = queryable.Where(p => p.PhoneNumber != null && p.PhoneNumber.Contains(searchDto.PhoneNumber));
            }

            // 年龄范围搜索
            if (searchDto.MinAge.HasValue || searchDto.MaxAge.HasValue)
            {
                var today = DateTime.Today;

                if (searchDto.MinAge.HasValue)
                {
                    var maxBirthDate = today.AddYears(-searchDto.MinAge.Value);
                    queryable = queryable.Where(p => p.BirthDate <= maxBirthDate);
                }

                if (searchDto.MaxAge.HasValue)
                {
                    var minBirthDate = today.AddYears(-searchDto.MaxAge.Value - 1);
                    queryable = queryable.Where(p => p.BirthDate >= minBirthDate);
                }
            }

            // 性别搜索
            if (searchDto.Gender.HasValue)
            {
                queryable = queryable.Where(p => p.Gender == searchDto.Gender.Value);
            }

            // 排序：按创建时间降序
            queryable = queryable.OrderByDescending(p => p.CreatedAt);

            // 执行分页查询并映射为DTO
            var totalCount = await queryable.CountAsync();
            var patientDtos = await queryable
                .Skip((searchDto.PageIndex - 1) * searchDto.PageSize)
                .Take(searchDto.PageSize)
                .ProjectTo<PatientDto>(_mapper.ConfigurationProvider)
                .ToListAsync();

            var result = new PagedResult<PatientDto>(
                patientDtos,
                totalCount,
                searchDto.PageIndex,
                searchDto.PageSize);

            SetCacheSafely(cacheKey, result, TimeSpan.FromMinutes(5)); // 高级搜索结果缓存5分钟
            return result;
        }

        public async Task<List<PatientDto>> CheckDuplicatePatientDtosAsync(PatientCreateDto createDto)
        {
            var cacheKey = GenerateCacheKey("duplicate_patients", createDto.PhoneNumber, createDto.IdNumber);

            if (_cache.TryGetValue<List<PatientDto>>(cacheKey, out var cached))
            {
                _logger.LogDebug("从缓存获取重复患者检查结果");
                return cached!;
            }

            var queryable = BuildOptimizedQuery();
            var hasDuplicateCondition = false;

            // 检查手机号重复
            if (!string.IsNullOrWhiteSpace(createDto.PhoneNumber))
            {
                queryable = queryable.Where(p => p.PhoneNumber == createDto.PhoneNumber);
                hasDuplicateCondition = true;
            }

            // 检查身份证号重复
            if (!string.IsNullOrWhiteSpace(createDto.IdNumber))
            {
                if (hasDuplicateCondition)
                {
                    queryable = BuildOptimizedQuery()
                        .Where(p => p.PhoneNumber == createDto.PhoneNumber || p.IdNumber == createDto.IdNumber);
                }
                else
                {
                    queryable = queryable.Where(p => p.IdNumber == createDto.IdNumber);
                    hasDuplicateCondition = true;
                }
            }

            if (!hasDuplicateCondition)
            {
                return new List<PatientDto>();
            }

            var duplicatePatientDtos = await queryable
                .ProjectTo<PatientDto>(_mapper.ConfigurationProvider)
                .ToListAsync();

            SetCacheSafely(cacheKey, duplicatePatientDtos, TimeSpan.FromMinutes(1)); // 重复检查结果缓存1分钟
            return duplicatePatientDtos;
        }
    }
}