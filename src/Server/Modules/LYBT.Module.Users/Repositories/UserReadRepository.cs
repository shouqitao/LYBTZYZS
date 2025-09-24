using AutoMapper;
using AutoMapper.QueryableExtensions;
using LYBT.Infrastructure.Data;
using LYBT.Infrastructure.Repositories;
using LYBT.Module.Users.Interfaces;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace LYBT.Module.Users.Repositories
{
    /// <summary>
    /// 用户只读仓储实现 - 专门为QueryService提供数据访问
    /// 继承ReadOnlyRepository获得缓存优化，实现用户特定的查询方法
    /// 使用AutoMapper ProjectTo进行高效的DTO映射
    /// </summary>
    public class UserReadRepository : ReadOnlyRepository<LYBT.Entities.Users.User>, IUserReadRepository
    {
        public UserReadRepository(
            AppDbContext context,
            IMapper mapper,
            ILogger<UserReadRepository> logger,
            IMemoryCache cache) : base(context, mapper, logger, cache)
        {
        }

        /// <summary>
        /// 应用全局过滤器 - 排除软删除记录
        /// </summary>
        protected override IQueryable<LYBT.Entities.Users.User> ApplyGlobalFilters(
            IQueryable<LYBT.Entities.Users.User> query)
        {
            // 应用软删除过滤
            return query.Where(u => !u.IsDeleted);
        }

        public async Task<UserDto?> GetUserDtoByIdAsync(Guid id)
        {
            if (id == Guid.Empty)
            {
                return null;
            }

            var cacheKey = $"{CacheKeyPrefix}detail:{id}";

            if (_cache.TryGetValue<UserDto?>(cacheKey, out var cached))
            {
                _logger.LogDebug("从缓存获取用户详情 {Id}", id);
                return cached;
            }

            var userDto = await BuildOptimizedQuery()
                .Where(u => u.Id == id)
                .ProjectTo<UserDto>(_mapper.ConfigurationProvider)
                .FirstOrDefaultAsync();

            SetCacheSafely(cacheKey, userDto, DefaultCacheDuration);
            return userDto;
        }

        public async Task<PagedResult<UserDto>> GetPagedUserDtosAsync(UserSearchDto query)
        {
            var cacheKey = GenerateCacheKey("paged_users",
                query.Keyword, query.Role, query.Status, query.PageIndex, query.PageSize);

            if (_cache.TryGetValue<PagedResult<UserDto>>(cacheKey, out var cached))
            {
                _logger.LogDebug("从缓存获取分页用户记录 Page:{PageIndex}", query.PageIndex);
                return cached!;
            }

            var queryable = BuildOptimizedQuery();

            // 基础筛选 - 排除已删除的用户
            queryable = queryable.Where(u => u.Status != CommonStatus.Disabled);

            // 应用搜索条件
            if (!string.IsNullOrWhiteSpace(query.Keyword))
            {
                var keyword = query.Keyword.Trim();
                queryable = queryable.Where(u =>
                    u.Username.Contains(keyword) ||
                    u.RealName.Contains(keyword) ||
                    (u.PhoneNumber != null && u.PhoneNumber.Contains(keyword)) ||
                    (u.Email != null && u.Email.Contains(keyword)));
            }

            // 角色筛选
            if (query.Role.HasValue)
            {
                queryable = queryable.Where(u => u.Role == query.Role.Value);
            }

            // 状态筛选
            if (query.Status.HasValue)
            {
                queryable = queryable.Where(u => u.Status == query.Status.Value);
            }

            // 排序：按创建时间降序
            queryable = queryable.OrderByDescending(u => u.CreatedAt);

            // 执行分页查询并映射为DTO
            var totalCount = await queryable.CountAsync();
            var userDtos = await queryable
                .Skip((query.PageIndex - 1) * query.PageSize)
                .Take(query.PageSize)
                .ProjectTo<UserDto>(_mapper.ConfigurationProvider)
                .ToListAsync();

            var result = new PagedResult<UserDto>(
                userDtos,
                totalCount,
                query.PageIndex,
                query.PageSize);

            SetCacheSafely(cacheKey, result, DefaultCacheDuration);
            return result;
        }

        public async Task<UserDto?> GetUserDtoByUsernameAsync(string userName)
        {
            if (string.IsNullOrWhiteSpace(userName))
            {
                return null;
            }

            var cacheKey = $"{CacheKeyPrefix}username:{userName}";

            if (_cache.TryGetValue<UserDto?>(cacheKey, out var cached))
            {
                _logger.LogDebug("从缓存获取用户信息 Username:{Username}", userName);
                return cached;
            }

            var userDto = await BuildOptimizedQuery()
                .Where(u => u.Username == userName)
                .ProjectTo<UserDto>(_mapper.ConfigurationProvider)
                .FirstOrDefaultAsync();

            SetCacheSafely(cacheKey, userDto, DefaultCacheDuration);
            return userDto;
        }

        public async Task<List<UserDto>> GetActiveUserDtosAsync()
        {
            var cacheKey = $"{CacheKeyPrefix}active_users";

            if (_cache.TryGetValue<List<UserDto>>(cacheKey, out var cached))
            {
                _logger.LogDebug("从缓存获取活跃用户列表");
                return cached!;
            }

            var userDtos = await BuildOptimizedQuery()
                .Where(u => u.Status == CommonStatus.Enabled)
                .OrderBy(u => u.RealName)
                .ProjectTo<UserDto>(_mapper.ConfigurationProvider)
                .ToListAsync();

            SetCacheSafely(cacheKey, userDtos, DefaultCacheDuration);
            return userDtos;
        }

        public async Task<List<UserDto>> SearchUserDtosAsync(string keyword, int maxResults = 50)
        {
            if (string.IsNullOrWhiteSpace(keyword))
            {
                return new List<UserDto>();
            }

            var cacheKey = GenerateCacheKey("search_users", keyword, maxResults);

            if (_cache.TryGetValue<List<UserDto>>(cacheKey, out var cached))
            {
                _logger.LogDebug("从缓存获取搜索结果 Keyword:{Keyword}", keyword);
                return cached!;
            }

            var searchTerm = keyword.Trim();
            var userDtos = await BuildOptimizedQuery()
                .Where(u => u.Status != CommonStatus.Disabled &&
                           (u.Username.Contains(searchTerm) ||
                            u.RealName.Contains(searchTerm) ||
                            (u.PhoneNumber != null && u.PhoneNumber.Contains(searchTerm)) ||
                            (u.Email != null && u.Email.Contains(searchTerm))))
                .OrderBy(u => u.RealName)
                .Take(maxResults)
                .ProjectTo<UserDto>(_mapper.ConfigurationProvider)
                .ToListAsync();

            SetCacheSafely(cacheKey, userDtos, TimeSpan.FromMinutes(2)); // 搜索结果较短缓存时间
            return userDtos;
        }

        public async Task<bool> IsUsernameAvailableAsync(string userName)
        {
            if (string.IsNullOrWhiteSpace(userName))
            {
                return false;
            }

            var cacheKey = $"{CacheKeyPrefix}username_available:{userName}";

            if (_cache.TryGetValue<bool>(cacheKey, out var cached))
            {
                _logger.LogDebug("从缓存检查用户名可用性 Username:{Username}", userName);
                return cached;
            }

            var exists = await BuildOptimizedQuery()
                .AnyAsync(u => u.Username == userName);

            var isAvailable = !exists;
            SetCacheSafely(cacheKey, isAvailable, TimeSpan.FromMinutes(1)); // 用户名可用性较短缓存

            return isAvailable;
        }

        public async Task<List<UserDto>> GetDoctorDtosAsync()
        {
            var cacheKey = $"{CacheKeyPrefix}doctors";

            if (_cache.TryGetValue<List<UserDto>>(cacheKey, out var cached))
            {
                _logger.LogDebug("从缓存获取医生列表");
                return cached!;
            }

            var doctorDtos = await BuildOptimizedQuery()
                .Where(u => u.Role == UserRole.Doctor && u.Status == CommonStatus.Enabled)
                .OrderBy(u => u.RealName)
                .ProjectTo<UserDto>(_mapper.ConfigurationProvider)
                .ToListAsync();

            SetCacheSafely(cacheKey, doctorDtos, DefaultCacheDuration);
            return doctorDtos;
        }

        public async Task<bool> IsDoctorAvailableAsync(Guid doctorId)
        {
            if (doctorId == Guid.Empty)
            {
                return false;
            }

            var cacheKey = $"{CacheKeyPrefix}doctor_available:{doctorId}";

            if (_cache.TryGetValue<bool>(cacheKey, out var cached))
            {
                _logger.LogDebug("从缓存检查医生可用性 DoctorId:{DoctorId}", doctorId);
                return cached;
            }

            var isAvailable = await BuildOptimizedQuery()
                .AnyAsync(u => u.Id == doctorId && 
                              u.Role == UserRole.Doctor && 
                              u.Status == CommonStatus.Enabled);

            SetCacheSafely(cacheKey, isAvailable, TimeSpan.FromMinutes(5)); // 医生可用性缓存5分钟
            return isAvailable;
        }

        public async Task<List<object>> GetRolesAsync()
        {
            var cacheKey = $"{CacheKeyPrefix}roles";

            if (_cache.TryGetValue<List<object>>(cacheKey, out var cached))
            {
                _logger.LogDebug("从缓存获取角色列表");
                return cached!;
            }

            // 简化角色获取 - 返回枚举值
            var roles = new List<object>
            {
                new { Value = (int)UserRole.Admin, Text = "管理员" },
                new { Value = (int)UserRole.Doctor, Text = "医生" }
            };

            await Task.CompletedTask; // 保持异步签名
            SetCacheSafely(cacheKey, roles, TimeSpan.FromHours(1)); // 角色信息缓存1小时

            return roles;
        }
    }
}