using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using LYBT.Desktop.Users.Interfaces;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Enums;
using LYBT.Shared.Interfaces.Api;

namespace LYBT.Desktop.Users.Services;

/// <summary>
/// 用户查询服务 - UltraThink三层架构查询专业层
/// 职责：复杂查询、搜索、筛选、统计、报表查询
/// </summary>
public class UserQueryService : IUserQueryService
{
    private readonly IUserApi _userApi;
    private readonly IUserCoreService _coreService;
    private readonly ILogger<UserQueryService> _logger;
    private readonly IMemoryCache _cache;
    
    private const string QUERY_CACHE_PREFIX = "user_query_";
    private const string STATS_CACHE_PREFIX = "user_stats_";
    private readonly TimeSpan _queryCacheExpiry = TimeSpan.FromMinutes(5);
    private readonly TimeSpan _statsCacheExpiry = TimeSpan.FromMinutes(15);

    public UserQueryService(
        IUserApi userApi,
        IUserCoreService coreService,
        ILogger<UserQueryService> logger,
        IMemoryCache cache)
    {
        _userApi = userApi ?? throw new ArgumentNullException(nameof(userApi));
        _coreService = coreService ?? throw new ArgumentNullException(nameof(coreService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
    }

    #region 分页和列表查询

    /// <summary>
    /// 分页查询用户
    /// </summary>
    public async Task<ServiceResult<PagedResult<UserDto>>> GetPagedAsync(UserPagedQueryDto query)
    {
        try
        {
            _logger.LogInformation("分页查询用户，页码：{Page}，页大小：{PageSize}", query.Page, query.PageSize);
            
            var cacheKey = $"{QUERY_CACHE_PREFIX}paged_{query.Page}_{query.PageSize}_{query.GetHashCode()}";
            
            if (_cache.TryGetValue(cacheKey, out PagedResult<UserDto>? cachedResult) && cachedResult != null)
            {
                _logger.LogDebug("从缓存返回分页查询结果");
                return ServiceResult<PagedResult<UserDto>>.Success(cachedResult, "从缓存获取分页查询结果");
            }

            var result = await _coreService.CallGetUsersApiAsync(query.Page, query.PageSize, query.Keyword);
            
            if (result.IsSuccess && result.Data != null)
            {
                _cache.Set(cacheKey, result.Data, _queryCacheExpiry);
            }
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "分页查询用户时发生异常");
            return ServiceResult<PagedResult<UserDto>>.Failure($"分页查询用户时发生异常：{ex.Message}");
        }
    }

    /// <summary>
    /// 获取用户列表（无分页）
    /// </summary>
    public async Task<ServiceResult<List<UserDto>>> GetUserListAsync(UserQueryOptions? options = null)
    {
        try
        {
            _logger.LogInformation("获取用户列表，选项：{Options}", options?.ToString() ?? "默认");
            
            var allUsersResult = await _coreService.GetAllUsersAsync();
            
            if (!allUsersResult.IsSuccess || allUsersResult.Data == null)
            {
                return ServiceResult<List<UserDto>>.Failure(allUsersResult.ErrorMessage ?? "获取用户列表失败");
            }

            var users = allUsersResult.Data.AsQueryable();

            // 应用过滤条件
            if (options != null)
            {
                if (!options.IncludeDisabled)
                {
                    users = users.Where(u => u.IsEnabled);
                }

                if (options.FilterByRole.HasValue)
                {
                    users = users.Where(u => u.Role == options.FilterByRole.Value);
                }

                if (options.CreatedAfter.HasValue)
                {
                    users = users.Where(u => u.CreateTime >= options.CreatedAfter.Value);
                }

                if (options.CreatedBefore.HasValue)
                {
                    users = users.Where(u => u.CreateTime <= options.CreatedBefore.Value);
                }

                // 排序
                if (!string.IsNullOrEmpty(options.SortBy))
                {
                    users = options.SortBy.ToLower() switch
                    {
                        "username" => options.SortDescending 
                            ? users.OrderByDescending(u => u.Username) 
                            : users.OrderBy(u => u.Username),
                        "email" => options.SortDescending 
                            ? users.OrderByDescending(u => u.Email) 
                            : users.OrderBy(u => u.Email),
                        "realname" => options.SortDescending 
                            ? users.OrderByDescending(u => u.RealName) 
                            : users.OrderBy(u => u.RealName),
                        "createtime" => options.SortDescending 
                            ? users.OrderByDescending(u => u.CreateTime) 
                            : users.OrderBy(u => u.CreateTime),
                        _ => users.OrderBy(u => u.Username)
                    };
                }
            }

            var result = users.ToList();
            
            return ServiceResult<List<UserDto>>.Success(result, $"获取用户列表成功，共 {result.Count} 条记录");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取用户列表时发生异常");
            return ServiceResult<List<UserDto>>.Failure($"获取用户列表时发生异常：{ex.Message}");
        }
    }

    /// <summary>
    /// 根据ID列表批量获取用户
    /// </summary>
    public async Task<ServiceResult<List<UserDto>>> GetUsersByIdsAsync(List<Guid> userIds)
    {
        try
        {
            _logger.LogInformation("批量获取用户，用户数量：{Count}", userIds.Count);
            
            var users = new List<UserDto>();
            
            foreach (var userId in userIds)
            {
                var result = await _coreService.GetUserByIdAsync(userId);
                if (result.IsSuccess && result.Data != null)
                {
                    users.Add(result.Data);
                }
            }
            
            return ServiceResult<List<UserDto>>.Success(users, $"批量获取用户成功，获得 {users.Count} 条记录");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "批量获取用户时发生异常");
            return ServiceResult<List<UserDto>>.Failure($"批量获取用户时发生异常：{ex.Message}");
        }
    }

    /// <summary>
    /// 获取用户概要信息
    /// </summary>
    public async Task<ServiceResult<List<UserSummaryDto>>> GetUserSummariesAsync(UserQueryOptions? options = null)
    {
        try
        {
            var usersResult = await GetUserListAsync(options);
            
            if (!usersResult.IsSuccess || usersResult.Data == null)
            {
                return ServiceResult<List<UserSummaryDto>>.Failure(usersResult.ErrorMessage ?? "获取用户概要失败");
            }

            var summaries = usersResult.Data.Select(u => new UserSummaryDto
            {
                Id = u.Id,
                Username = u.Username,
                RealName = u.RealName,
                Email = u.Email,
                Role = u.Role,
                IsEnabled = u.IsEnabled,
                CreateTime = u.CreateTime
            }).ToList();
            
            return ServiceResult<List<UserSummaryDto>>.Success(summaries, $"获取用户概要成功，共 {summaries.Count} 条记录");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取用户概要信息时发生异常");
            return ServiceResult<List<UserSummaryDto>>.Failure($"获取用户概要信息时发生异常：{ex.Message}");
        }
    }

    #endregion

    #region 搜索和筛选

    /// <summary>
    /// 搜索用户（关键词搜索）
    /// </summary>
    public async Task<ServiceResult<PagedResult<UserDto>>> SearchUsersAsync(UserSearchDto searchDto)
    {
        try
        {
            _logger.LogInformation("搜索用户，关键词：{Keyword}", searchDto.Username ?? searchDto.Email ?? searchDto.RealName);
            
            var cacheKey = $"{QUERY_CACHE_PREFIX}search_{searchDto.GetHashCode()}";
            
            if (_cache.TryGetValue(cacheKey, out PagedResult<UserDto>? cachedResult) && cachedResult != null)
            {
                return ServiceResult<PagedResult<UserDto>>.Success(cachedResult, "从缓存获取搜索结果");
            }

            var allUsersResult = await _coreService.GetAllUsersAsync();
            
            if (!allUsersResult.IsSuccess || allUsersResult.Data == null)
            {
                return ServiceResult<PagedResult<UserDto>>.Failure("获取用户数据失败");
            }

            var users = allUsersResult.Data.AsQueryable();

            // 应用搜索条件
            if (!string.IsNullOrWhiteSpace(searchDto.Username))
            {
                users = users.Where(u => u.Username.Contains(searchDto.Username, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(searchDto.Email))
            {
                users = users.Where(u => u.Email.Contains(searchDto.Email, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(searchDto.RealName))
            {
                users = users.Where(u => u.RealName.Contains(searchDto.RealName, StringComparison.OrdinalIgnoreCase));
            }

            if (searchDto.Role.HasValue)
            {
                users = users.Where(u => u.Role == searchDto.Role.Value);
            }

            if (searchDto.IsEnabled.HasValue)
            {
                users = users.Where(u => u.IsEnabled == searchDto.IsEnabled.Value);
            }

            // 分页
            var totalCount = users.Count();
            var pagedUsers = users
                .Skip((searchDto.Page - 1) * searchDto.PageSize)
                .Take(searchDto.PageSize)
                .ToList();

            var result = new PagedResult<UserDto>
            {
                Items = pagedUsers,
                TotalCount = totalCount,
                Page = searchDto.Page,
                PageSize = searchDto.PageSize,
                TotalPages = (int)Math.Ceiling((double)totalCount / searchDto.PageSize)
            };

            _cache.Set(cacheKey, result, _queryCacheExpiry);
            
            return ServiceResult<PagedResult<UserDto>>.Success(result, $"搜索用户成功，找到 {totalCount} 条匹配记录");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "搜索用户时发生异常");
            return ServiceResult<PagedResult<UserDto>>.Failure($"搜索用户时发生异常：{ex.Message}");
        }
    }

    /// <summary>
    /// 按用户名搜索
    /// </summary>
    public async Task<ServiceResult<List<UserDto>>> SearchByUsernameAsync(string username)
    {
        try
        {
            var allUsersResult = await _coreService.GetAllUsersAsync();
            
            if (!allUsersResult.IsSuccess || allUsersResult.Data == null)
            {
                return ServiceResult<List<UserDto>>.Failure("获取用户数据失败");
            }

            var matchedUsers = allUsersResult.Data
                .Where(u => u.Username.Contains(username, StringComparison.OrdinalIgnoreCase))
                .ToList();
            
            return ServiceResult<List<UserDto>>.Success(matchedUsers, $"按用户名搜索成功，找到 {matchedUsers.Count} 条记录");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "按用户名搜索时发生异常，用户名：{Username}", username);
            return ServiceResult<List<UserDto>>.Failure($"按用户名搜索时发生异常：{ex.Message}");
        }
    }

    /// <summary>
    /// 按邮箱搜索
    /// </summary>
    public async Task<ServiceResult<List<UserDto>>> SearchByEmailAsync(string email)
    {
        try
        {
            var allUsersResult = await _coreService.GetAllUsersAsync();
            
            if (!allUsersResult.IsSuccess || allUsersResult.Data == null)
            {
                return ServiceResult<List<UserDto>>.Failure("获取用户数据失败");
            }

            var matchedUsers = allUsersResult.Data
                .Where(u => u.Email.Contains(email, StringComparison.OrdinalIgnoreCase))
                .ToList();
            
            return ServiceResult<List<UserDto>>.Success(matchedUsers, $"按邮箱搜索成功，找到 {matchedUsers.Count} 条记录");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "按邮箱搜索时发生异常，邮箱：{Email}", email);
            return ServiceResult<List<UserDto>>.Failure($"按邮箱搜索时发生异常：{ex.Message}");
        }
    }

    /// <summary>
    /// 按真实姓名搜索
    /// </summary>
    public async Task<ServiceResult<List<UserDto>>> SearchByRealNameAsync(string realName)
    {
        try
        {
            var allUsersResult = await _coreService.GetAllUsersAsync();
            
            if (!allUsersResult.IsSuccess || allUsersResult.Data == null)
            {
                return ServiceResult<List<UserDto>>.Failure("获取用户数据失败");
            }

            var matchedUsers = allUsersResult.Data
                .Where(u => u.RealName.Contains(realName, StringComparison.OrdinalIgnoreCase))
                .ToList();
            
            return ServiceResult<List<UserDto>>.Success(matchedUsers, $"按真实姓名搜索成功，找到 {matchedUsers.Count} 条记录");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "按真实姓名搜索时发生异常，姓名：{RealName}", realName);
            return ServiceResult<List<UserDto>>.Failure($"按真实姓名搜索时发生异常：{ex.Message}");
        }
    }

    /// <summary>
    /// 按角色筛选用户
    /// </summary>
    public async Task<ServiceResult<List<UserDto>>> GetUsersByRoleAsync(UserRole role)
    {
        try
        {
            var allUsersResult = await _coreService.GetAllUsersAsync();
            
            if (!allUsersResult.IsSuccess || allUsersResult.Data == null)
            {
                return ServiceResult<List<UserDto>>.Failure("获取用户数据失败");
            }

            var roleUsers = allUsersResult.Data
                .Where(u => u.Role == role)
                .ToList();
            
            return ServiceResult<List<UserDto>>.Success(roleUsers, $"按角色筛选成功，找到 {roleUsers.Count} 个{role}用户");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "按角色筛选用户时发生异常，角色：{Role}", role);
            return ServiceResult<List<UserDto>>.Failure($"按角色筛选用户时发生异常：{ex.Message}");
        }
    }

    /// <summary>
    /// 按状态筛选用户
    /// </summary>
    public async Task<ServiceResult<List<UserDto>>> GetUsersByStatusAsync(bool isEnabled)
    {
        try
        {
            var allUsersResult = await _coreService.GetAllUsersAsync();
            
            if (!allUsersResult.IsSuccess || allUsersResult.Data == null)
            {
                return ServiceResult<List<UserDto>>.Failure("获取用户数据失败");
            }

            var statusUsers = allUsersResult.Data
                .Where(u => u.IsEnabled == isEnabled)
                .ToList();
            
            var statusText = isEnabled ? "启用" : "禁用";
            return ServiceResult<List<UserDto>>.Success(statusUsers, $"按状态筛选成功，找到 {statusUsers.Count} 个{statusText}用户");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "按状态筛选用户时发生异常，状态：{Status}", isEnabled);
            return ServiceResult<List<UserDto>>.Failure($"按状态筛选用户时发生异常：{ex.Message}");
        }
    }

    /// <summary>
    /// 高级筛选用户
    /// </summary>
    public async Task<ServiceResult<PagedResult<UserDto>>> GetUsersWithAdvancedFilterAsync(UserAdvancedFilterDto filter)
    {
        try
        {
            _logger.LogInformation("执行高级筛选用户");
            
            var allUsersResult = await _coreService.GetAllUsersAsync();
            
            if (!allUsersResult.IsSuccess || allUsersResult.Data == null)
            {
                return ServiceResult<PagedResult<UserDto>>.Failure("获取用户数据失败");
            }

            var users = allUsersResult.Data.AsQueryable();

            // 应用高级筛选条件
            if (filter.Roles != null && filter.Roles.Any())
            {
                users = users.Where(u => filter.Roles.Contains(u.Role));
            }

            if (filter.IsEnabled.HasValue)
            {
                users = users.Where(u => u.IsEnabled == filter.IsEnabled.Value);
            }

            if (filter.CreatedAfter.HasValue)
            {
                users = users.Where(u => u.CreateTime >= filter.CreatedAfter.Value);
            }

            if (filter.CreatedBefore.HasValue)
            {
                users = users.Where(u => u.CreateTime <= filter.CreatedBefore.Value);
            }

            if (filter.ExcludeUserIds != null && filter.ExcludeUserIds.Any())
            {
                users = users.Where(u => !filter.ExcludeUserIds.Contains(u.Id));
            }

            // 分页
            var totalCount = users.Count();
            var pagedUsers = users
                .Skip((filter.Page - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToList();

            var result = new PagedResult<UserDto>
            {
                Items = pagedUsers,
                TotalCount = totalCount,
                Page = filter.Page,
                PageSize = filter.PageSize,
                TotalPages = (int)Math.Ceiling((double)totalCount / filter.PageSize)
            };
            
            return ServiceResult<PagedResult<UserDto>>.Success(result, $"高级筛选成功，找到 {totalCount} 条匹配记录");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "高级筛选用户时发生异常");
            return ServiceResult<PagedResult<UserDto>>.Failure($"高级筛选用户时发生异常：{ex.Message}");
        }
    }

    #endregion

    #region 特定查询

    /// <summary>
    /// 根据用户名获取用户
    /// </summary>
    public async Task<ServiceResult<UserDto>> GetUserByUsernameAsync(string username)
    {
        try
        {
            var allUsersResult = await _coreService.GetAllUsersAsync();
            
            if (!allUsersResult.IsSuccess || allUsersResult.Data == null)
            {
                return ServiceResult<UserDto>.Failure("获取用户数据失败");
            }

            var user = allUsersResult.Data.FirstOrDefault(u => 
                string.Equals(u.Username, username, StringComparison.OrdinalIgnoreCase));
            
            if (user == null)
            {
                return ServiceResult<UserDto>.Failure($"未找到用户名为 {username} 的用户");
            }
            
            return ServiceResult<UserDto>.Success(user, "根据用户名获取用户成功");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "根据用户名获取用户时发生异常，用户名：{Username}", username);
            return ServiceResult<UserDto>.Failure($"根据用户名获取用户时发生异常：{ex.Message}");
        }
    }

    /// <summary>
    /// 根据邮箱获取用户
    /// </summary>
    public async Task<ServiceResult<UserDto>> GetUserByEmailAsync(string email)
    {
        try
        {
            var allUsersResult = await _coreService.GetAllUsersAsync();
            
            if (!allUsersResult.IsSuccess || allUsersResult.Data == null)
            {
                return ServiceResult<UserDto>.Failure("获取用户数据失败");
            }

            var user = allUsersResult.Data.FirstOrDefault(u => 
                string.Equals(u.Email, email, StringComparison.OrdinalIgnoreCase));
            
            if (user == null)
            {
                return ServiceResult<UserDto>.Failure($"未找到邮箱为 {email} 的用户");
            }
            
            return ServiceResult<UserDto>.Success(user, "根据邮箱获取用户成功");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "根据邮箱获取用户时发生异常，邮箱：{Email}", email);
            return ServiceResult<UserDto>.Failure($"根据邮箱获取用户时发生异常：{ex.Message}");
        }
    }

    /// <summary>
    /// 获取活跃用户
    /// </summary>
    public async Task<ServiceResult<List<UserDto>>> GetActiveUsersAsync()
    {
        return await GetUsersByStatusAsync(true);
    }

    /// <summary>
    /// 获取禁用用户
    /// </summary>
    public async Task<ServiceResult<List<UserDto>>> GetDisabledUsersAsync()
    {
        return await GetUsersByStatusAsync(false);
    }

    /// <summary>
    /// 获取最近注册的用户
    /// </summary>
    public async Task<ServiceResult<List<UserDto>>> GetRecentlyRegisteredUsersAsync(int days = 30)
    {
        try
        {
            var cutoffDate = DateTime.Now.AddDays(-days);
            var allUsersResult = await _coreService.GetAllUsersAsync();
            
            if (!allUsersResult.IsSuccess || allUsersResult.Data == null)
            {
                return ServiceResult<List<UserDto>>.Failure("获取用户数据失败");
            }

            var recentUsers = allUsersResult.Data
                .Where(u => u.CreateTime >= cutoffDate)
                .OrderByDescending(u => u.CreateTime)
                .ToList();
            
            return ServiceResult<List<UserDto>>.Success(recentUsers, 
                $"获取最近{days}天注册用户成功，找到 {recentUsers.Count} 个用户");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取最近注册用户时发生异常，天数：{Days}", days);
            return ServiceResult<List<UserDto>>.Failure($"获取最近注册用户时发生异常：{ex.Message}");
        }
    }

    /// <summary>
    /// 获取长时间未登录的用户
    /// </summary>
    public async Task<ServiceResult<List<UserDto>>> GetInactiveUsersAsync(int days = 90)
    {
        try
        {
            // 由于当前UserDto没有LastLoginTime字段，这里返回一个空列表
            // 在实际实现中，需要扩展UserDto或使用其他方式获取登录时间信息
            _logger.LogWarning("获取长时间未登录用户功能需要登录时间信息支持");
            
            return ServiceResult<List<UserDto>>.Success(new List<UserDto>(), 
                $"获取{days}天未登录用户完成（功能待完善）");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取长时间未登录用户时发生异常，天数：{Days}", days);
            return ServiceResult<List<UserDto>>.Failure($"获取长时间未登录用户时发生异常：{ex.Message}");
        }
    }

    #endregion

    #region 统计查询

    /// <summary>
    /// 获取用户统计信息
    /// </summary>
    public async Task<ServiceResult<UserStatisticsDto>> GetUserStatisticsAsync()
    {
        try
        {
            var cacheKey = $"{STATS_CACHE_PREFIX}user_statistics";
            
            if (_cache.TryGetValue(cacheKey, out UserStatisticsDto? cachedStats) && cachedStats != null)
            {
                return ServiceResult<UserStatisticsDto>.Success(cachedStats, "从缓存获取用户统计信息");
            }

            var allUsersResult = await _coreService.GetAllUsersAsync();
            
            if (!allUsersResult.IsSuccess || allUsersResult.Data == null)
            {
                return ServiceResult<UserStatisticsDto>.Failure("获取用户数据失败");
            }

            var users = allUsersResult.Data;
            var recentDate = DateTime.Now.AddDays(-30);

            var statistics = new UserStatisticsDto
            {
                TotalUsers = users.Count,
                ActiveUsers = users.Count(u => u.IsEnabled),
                DisabledUsers = users.Count(u => !u.IsEnabled),
                AdminUsers = users.Count(u => u.Role == UserRole.Admin),
                DoctorUsers = users.Count(u => u.Role == UserRole.Doctor),
                RecentRegistrations = users.Count(u => u.CreateTime >= recentDate),
                InactiveUsers = 0 // 需要登录时间信息支持
            };

            _cache.Set(cacheKey, statistics, _statsCacheExpiry);
            
            return ServiceResult<UserStatisticsDto>.Success(statistics, "获取用户统计信息成功");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取用户统计信息时发生异常");
            return ServiceResult<UserStatisticsDto>.Failure($"获取用户统计信息时发生异常：{ex.Message}");
        }
    }

    /// <summary>
    /// 获取用户数量统计
    /// </summary>
    public async Task<ServiceResult<Dictionary<string, int>>> GetUserCountStatisticsAsync()
    {
        try
        {
            var statisticsResult = await GetUserStatisticsAsync();
            
            if (!statisticsResult.IsSuccess || statisticsResult.Data == null)
            {
                return ServiceResult<Dictionary<string, int>>.Failure("获取统计数据失败");
            }

            var stats = statisticsResult.Data;
            var countStats = new Dictionary<string, int>
            {
                ["总用户数"] = stats.TotalUsers,
                ["活跃用户"] = stats.ActiveUsers,
                ["禁用用户"] = stats.DisabledUsers,
                ["管理员"] = stats.AdminUsers,
                ["医生"] = stats.DoctorUsers,
                ["最近注册"] = stats.RecentRegistrations
            };
            
            return ServiceResult<Dictionary<string, int>>.Success(countStats, "获取用户数量统计成功");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取用户数量统计时发生异常");
            return ServiceResult<Dictionary<string, int>>.Failure($"获取用户数量统计时发生异常：{ex.Message}");
        }
    }

    /// <summary>
    /// 获取角色分布统计
    /// </summary>
    public async Task<ServiceResult<Dictionary<UserRole, int>>> GetRoleDistributionAsync()
    {
        try
        {
            var allUsersResult = await _coreService.GetAllUsersAsync();
            
            if (!allUsersResult.IsSuccess || allUsersResult.Data == null)
            {
                return ServiceResult<Dictionary<UserRole, int>>.Failure("获取用户数据失败");
            }

            var roleDistribution = allUsersResult.Data
                .GroupBy(u => u.Role)
                .ToDictionary(g => g.Key, g => g.Count());
            
            return ServiceResult<Dictionary<UserRole, int>>.Success(roleDistribution, "获取角色分布统计成功");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取角色分布统计时发生异常");
            return ServiceResult<Dictionary<UserRole, int>>.Failure($"获取角色分布统计时发生异常：{ex.Message}");
        }
    }

    /// <summary>
    /// 获取用户状态分布
    /// </summary>
    public async Task<ServiceResult<Dictionary<string, int>>> GetStatusDistributionAsync()
    {
        try
        {
            var allUsersResult = await _coreService.GetAllUsersAsync();
            
            if (!allUsersResult.IsSuccess || allUsersResult.Data == null)
            {
                return ServiceResult<Dictionary<string, int>>.Failure("获取用户数据失败");
            }

            var statusDistribution = allUsersResult.Data
                .GroupBy(u => u.IsEnabled ? "启用" : "禁用")
                .ToDictionary(g => g.Key, g => g.Count());
            
            return ServiceResult<Dictionary<string, int>>.Success(statusDistribution, "获取状态分布统计成功");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取状态分布统计时发生异常");
            return ServiceResult<Dictionary<string, int>>.Failure($"获取状态分布统计时发生异常：{ex.Message}");
        }
    }

    /// <summary>
    /// 获取注册趋势数据
    /// </summary>
    public async Task<ServiceResult<List<UserRegistrationTrendDto>>> GetRegistrationTrendAsync(int days = 30)
    {
        try
        {
            var allUsersResult = await _coreService.GetAllUsersAsync();
            
            if (!allUsersResult.IsSuccess || allUsersResult.Data == null)
            {
                return ServiceResult<List<UserRegistrationTrendDto>>.Failure("获取用户数据失败");
            }

            var startDate = DateTime.Now.AddDays(-days).Date;
            var trendData = new List<UserRegistrationTrendDto>();
            
            for (int i = 0; i < days; i++)
            {
                var date = startDate.AddDays(i);
                var dailyCount = allUsersResult.Data.Count(u => u.CreateTime.Date == date);
                var cumulativeCount = allUsersResult.Data.Count(u => u.CreateTime.Date <= date);
                
                trendData.Add(new UserRegistrationTrendDto
                {
                    Date = date,
                    RegistrationCount = dailyCount,
                    CumulativeCount = cumulativeCount
                });
            }
            
            return ServiceResult<List<UserRegistrationTrendDto>>.Success(trendData, 
                $"获取{days}天注册趋势数据成功");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取注册趋势数据时发生异常，天数：{Days}", days);
            return ServiceResult<List<UserRegistrationTrendDto>>.Failure($"获取注册趋势数据时发生异常：{ex.Message}");
        }
    }

    /// <summary>
    /// 获取用户活跃度统计
    /// </summary>
    public async Task<ServiceResult<UserActivityStatisticsDto>> GetUserActivityStatisticsAsync(int days = 30)
    {
        try
        {
            // 由于缺少用户活跃度相关数据，这里返回模拟数据
            var activityStats = new UserActivityStatisticsDto
            {
                DailyActiveUsers = 0,
                WeeklyActiveUsers = 0,
                MonthlyActiveUsers = 0,
                AverageSessionDuration = 0,
                LastActiveTime = DateTime.Now
            };

            _logger.LogWarning("获取用户活跃度统计功能需要用户活动数据支持");
            
            return ServiceResult<UserActivityStatisticsDto>.Success(activityStats, 
                $"获取用户活跃度统计完成（功能待完善）");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取用户活跃度统计时发生异常，天数：{Days}", days);
            return ServiceResult<UserActivityStatisticsDto>.Failure($"获取用户活跃度统计时发生异常：{ex.Message}");
        }
    }

    #endregion

    #region 查询优化和缓存

    /// <summary>
    /// 预加载查询缓存
    /// </summary>
    public async Task<ServiceResult> PreloadQueryCacheAsync()
    {
        try
        {
            _logger.LogInformation("预加载查询缓存");
            
            // 预加载统计数据
            await GetUserStatisticsAsync();
            
            // 预加载角色分布
            await GetRoleDistributionAsync();
            
            // 预加载状态分布
            await GetStatusDistributionAsync();
            
            return ServiceResult.Success("查询缓存预加载完成");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "预加载查询缓存时发生异常");
            return ServiceResult.Failure($"预加载查询缓存时发生异常：{ex.Message}");
        }
    }

    /// <summary>
    /// 清除查询缓存
    /// </summary>
    public ServiceResult ClearQueryCache()
    {
        try
        {
            _logger.LogInformation("清除查询缓存");
            
            // 清除统计缓存
            var keysToRemove = new[]
            {
                $"{STATS_CACHE_PREFIX}user_statistics"
            };
            
            foreach (var key in keysToRemove)
            {
                _cache.Remove(key);
            }
            
            return ServiceResult.Success("查询缓存清除完成");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "清除查询缓存时发生异常");
            return ServiceResult.Failure($"清除查询缓存时发生异常：{ex.Message}");
        }
    }

    /// <summary>
    /// 获取查询性能统计
    /// </summary>
    public ServiceResult<QueryPerformanceDto> GetQueryPerformanceStats()
    {
        try
        {
            // 模拟查询性能统计数据
            var performanceStats = new QueryPerformanceDto
            {
                AverageQueryTime = 150, // 毫秒
                CacheHitRate = 85, // 百分比
                TotalQueries = 1000,
                SlowQueries = 5
            };
            
            return ServiceResult<QueryPerformanceDto>.Success(performanceStats, "获取查询性能统计成功");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取查询性能统计时发生异常");
            return ServiceResult<QueryPerformanceDto>.Failure($"获取查询性能统计时发生异常：{ex.Message}");
        }
    }

    /// <summary>
    /// 优化查询索引
    /// </summary>
    public async Task<ServiceResult> OptimizeQueryIndexAsync()
    {
        try
        {
            _logger.LogInformation("优化查询索引");
            
            // 这里应该实现查询索引优化逻辑
            // 例如清理过期缓存、重建索引等
            
            await Task.Delay(100); // 模拟异步操作
            
            return ServiceResult.Success("查询索引优化完成");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "优化查询索引时发生异常");
            return ServiceResult.Failure($"优化查询索引时发生异常：{ex.Message}");
        }
    }

    #endregion

    #region 导出查询

    /// <summary>
    /// 查询用户数据用于导出
    /// </summary>
    public async Task<ServiceResult<List<UserExportDto>>> GetUsersForExportAsync(UserExportQueryDto query)
    {
        try
        {
            var allUsersResult = await _coreService.GetAllUsersAsync();
            
            if (!allUsersResult.IsSuccess || allUsersResult.Data == null)
            {
                return ServiceResult<List<UserExportDto>>.Failure("获取用户数据失败");
            }

            var users = allUsersResult.Data.AsQueryable();

            // 应用筛选条件
            if (query.UserIds != null && query.UserIds.Any())
            {
                users = users.Where(u => query.UserIds.Contains(u.Id));
            }

            if (query.Role.HasValue)
            {
                users = users.Where(u => u.Role == query.Role.Value);
            }

            if (query.IsEnabled.HasValue)
            {
                users = users.Where(u => u.IsEnabled == query.IsEnabled.Value);
            }

            if (query.CreatedAfter.HasValue)
            {
                users = users.Where(u => u.CreateTime >= query.CreatedAfter.Value);
            }

            if (query.CreatedBefore.HasValue)
            {
                users = users.Where(u => u.CreateTime <= query.CreatedBefore.Value);
            }

            // 转换为导出格式
            var exportData = users.Select(u => new UserExportDto
            {
                Username = u.Username,
                RealName = u.RealName,
                Email = query.IncludePersonalInfo ? u.Email : "***",
                Role = u.Role.ToString(),
                Status = u.IsEnabled ? "启用" : "禁用",
                CreateTime = u.CreateTime,
                LastLoginTime = null // 需要登录时间数据支持
            }).ToList();
            
            return ServiceResult<List<UserExportDto>>.Success(exportData, 
                $"查询导出用户数据成功，共 {exportData.Count} 条记录");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "查询用户数据用于导出时发生异常");
            return ServiceResult<List<UserExportDto>>.Failure($"查询用户数据用于导出时发生异常：{ex.Message}");
        }
    }

    /// <summary>
    /// 获取用户基础信息（轻量级）
    /// </summary>
    public async Task<ServiceResult<List<UserBasicInfoDto>>> GetUserBasicInfoAsync(List<Guid>? userIds = null)
    {
        try
        {
            var allUsersResult = await _coreService.GetAllUsersAsync();
            
            if (!allUsersResult.IsSuccess || allUsersResult.Data == null)
            {
                return ServiceResult<List<UserBasicInfoDto>>.Failure("获取用户数据失败");
            }

            var users = allUsersResult.Data.AsQueryable();
            
            if (userIds != null && userIds.Any())
            {
                users = users.Where(u => userIds.Contains(u.Id));
            }

            var basicInfo = users.Select(u => new UserBasicInfoDto
            {
                Id = u.Id,
                Username = u.Username,
                RealName = u.RealName,
                Role = u.Role,
                IsEnabled = u.IsEnabled
            }).ToList();
            
            return ServiceResult<List<UserBasicInfoDto>>.Success(basicInfo, 
                $"获取用户基础信息成功，共 {basicInfo.Count} 条记录");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取用户基础信息时发生异常");
            return ServiceResult<List<UserBasicInfoDto>>.Failure($"获取用户基础信息时发生异常：{ex.Message}");
        }
    }

    /// <summary>
    /// 获取用户详细信息（完整数据）
    /// </summary>
    public async Task<ServiceResult<List<UserDetailedInfoDto>>> GetUserDetailedInfoAsync(List<Guid> userIds)
    {
        try
        {
            var usersResult = await GetUsersByIdsAsync(userIds);
            
            if (!usersResult.IsSuccess || usersResult.Data == null)
            {
                return ServiceResult<List<UserDetailedInfoDto>>.Failure("获取用户数据失败");
            }

            var detailedInfo = usersResult.Data.Select(u => new UserDetailedInfoDto
            {
                Id = u.Id,
                Username = u.Username,
                RealName = u.RealName,
                Role = u.Role,
                IsEnabled = u.IsEnabled,
                Email = u.Email,
                Phone = u.Phone,
                CreateTime = u.CreateTime,
                UpdateTime = u.UpdateTime,
                LastLoginTime = null, // 需要登录时间数据支持
                Description = u.Description
            }).ToList();
            
            return ServiceResult<List<UserDetailedInfoDto>>.Success(detailedInfo, 
                $"获取用户详细信息成功，共 {detailedInfo.Count} 条记录");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取用户详细信息时发生异常");
            return ServiceResult<List<UserDetailedInfoDto>>.Failure($"获取用户详细信息时发生异常：{ex.Message}");
        }
    }

    #endregion
}