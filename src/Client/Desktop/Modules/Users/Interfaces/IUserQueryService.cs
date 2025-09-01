using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Enums;

namespace LYBT.Desktop.Users.Interfaces;

/// <summary>
/// 用户查询服务接口 - UltraThink三层架构查询专业层
/// 职责：复杂查询、搜索、筛选、统计、报表查询
/// </summary>
public interface IUserQueryService
{
    #region 分页和列表查询
    
    /// <summary>
    /// 分页查询用户
    /// </summary>
    Task<ServiceResult<PagedResult<UserDto>>> GetPagedAsync(UserPagedQueryDto query);
    
    /// <summary>
    /// 获取用户列表（无分页）
    /// </summary>
    Task<ServiceResult<List<UserDto>>> GetUserListAsync(UserQueryOptions? options = null);
    
    /// <summary>
    /// 根据ID列表批量获取用户
    /// </summary>
    Task<ServiceResult<List<UserDto>>> GetUsersByIdsAsync(List<Guid> userIds);
    
    /// <summary>
    /// 获取用户概要信息
    /// </summary>
    Task<ServiceResult<List<UserSummaryDto>>> GetUserSummariesAsync(UserQueryOptions? options = null);
    
    #endregion
    
    #region 搜索和筛选
    
    /// <summary>
    /// 搜索用户（关键词搜索）
    /// </summary>
    Task<ServiceResult<PagedResult<UserDto>>> SearchUsersAsync(UserSearchDto searchDto);
    
    /// <summary>
    /// 按用户名搜索
    /// </summary>
    Task<ServiceResult<List<UserDto>>> SearchByUsernameAsync(string username);
    
    /// <summary>
    /// 按邮箱搜索
    /// </summary>
    Task<ServiceResult<List<UserDto>>> SearchByEmailAsync(string email);
    
    /// <summary>
    /// 按真实姓名搜索
    /// </summary>
    Task<ServiceResult<List<UserDto>>> SearchByRealNameAsync(string realName);
    
    /// <summary>
    /// 按角色筛选用户
    /// </summary>
    Task<ServiceResult<List<UserDto>>> GetUsersByRoleAsync(UserRole role);
    
    /// <summary>
    /// 按状态筛选用户
    /// </summary>
    Task<ServiceResult<List<UserDto>>> GetUsersByStatusAsync(bool isEnabled);
    
    /// <summary>
    /// 高级筛选用户
    /// </summary>
    Task<ServiceResult<PagedResult<UserDto>>> GetUsersWithAdvancedFilterAsync(UserAdvancedFilterDto filter);
    
    #endregion
    
    #region 特定查询
    
    /// <summary>
    /// 根据用户名获取用户
    /// </summary>
    Task<ServiceResult<UserDto>> GetUserByUsernameAsync(string username);
    
    /// <summary>
    /// 根据邮箱获取用户
    /// </summary>
    Task<ServiceResult<UserDto>> GetUserByEmailAsync(string email);
    
    /// <summary>
    /// 获取活跃用户
    /// </summary>
    Task<ServiceResult<List<UserDto>>> GetActiveUsersAsync();
    
    /// <summary>
    /// 获取禁用用户
    /// </summary>
    Task<ServiceResult<List<UserDto>>> GetDisabledUsersAsync();
    
    /// <summary>
    /// 获取最近注册的用户
    /// </summary>
    Task<ServiceResult<List<UserDto>>> GetRecentlyRegisteredUsersAsync(int days = 30);
    
    /// <summary>
    /// 获取长时间未登录的用户
    /// </summary>
    Task<ServiceResult<List<UserDto>>> GetInactiveUsersAsync(int days = 90);
    
    #endregion
    
    #region 统计查询
    
    /// <summary>
    /// 获取用户统计信息
    /// </summary>
    Task<ServiceResult<UserStatisticsDto>> GetUserStatisticsAsync();
    
    /// <summary>
    /// 获取用户数量统计
    /// </summary>
    Task<ServiceResult<Dictionary<string, int>>> GetUserCountStatisticsAsync();
    
    /// <summary>
    /// 获取角色分布统计
    /// </summary>
    Task<ServiceResult<Dictionary<UserRole, int>>> GetRoleDistributionAsync();
    
    /// <summary>
    /// 获取用户状态分布
    /// </summary>
    Task<ServiceResult<Dictionary<string, int>>> GetStatusDistributionAsync();
    
    /// <summary>
    /// 获取注册趋势数据
    /// </summary>
    Task<ServiceResult<List<UserRegistrationTrendDto>>> GetRegistrationTrendAsync(int days = 30);
    
    /// <summary>
    /// 获取用户活跃度统计
    /// </summary>
    Task<ServiceResult<UserActivityStatisticsDto>> GetUserActivityStatisticsAsync(int days = 30);
    
    #endregion
    
    #region 查询优化和缓存
    
    /// <summary>
    /// 预加载查询缓存
    /// </summary>
    Task<ServiceResult> PreloadQueryCacheAsync();
    
    /// <summary>
    /// 清除查询缓存
    /// </summary>
    ServiceResult ClearQueryCache();
    
    /// <summary>
    /// 获取查询性能统计
    /// </summary>
    ServiceResult<QueryPerformanceDto> GetQueryPerformanceStats();
    
    /// <summary>
    /// 优化查询索引
    /// </summary>
    Task<ServiceResult> OptimizeQueryIndexAsync();
    
    #endregion
    
    #region 导出查询
    
    /// <summary>
    /// 查询用户数据用于导出
    /// </summary>
    Task<ServiceResult<List<UserExportDto>>> GetUsersForExportAsync(UserExportQueryDto query);
    
    /// <summary>
    /// 获取用户基础信息（轻量级）
    /// </summary>
    Task<ServiceResult<List<UserBasicInfoDto>>> GetUserBasicInfoAsync(List<Guid>? userIds = null);
    
    /// <summary>
    /// 获取用户详细信息（完整数据）
    /// </summary>
    Task<ServiceResult<List<UserDetailedInfoDto>>> GetUserDetailedInfoAsync(List<Guid> userIds);
    
    #endregion
}

/// <summary>
/// 用户查询选项
/// </summary>
public class UserQueryOptions
{
    public bool IncludeDisabled { get; set; } = false;
    public UserRole? FilterByRole { get; set; }
    public DateTime? CreatedAfter { get; set; }
    public DateTime? CreatedBefore { get; set; }
    public string? SortBy { get; set; } = "Username";
    public bool SortDescending { get; set; } = false;
}

/// <summary>
/// 用户搜索DTO
/// </summary>
public class UserSearchDto : PagedQueryBaseDto
{
    public string? Username { get; set; }
    public string? Email { get; set; }
    public string? RealName { get; set; }
    public UserRole? Role { get; set; }
    public bool? IsEnabled { get; set; }
}

/// <summary>
/// 用户高级筛选DTO
/// </summary>
public class UserAdvancedFilterDto : PagedQueryBaseDto
{
    public List<UserRole>? Roles { get; set; }
    public bool? IsEnabled { get; set; }
    public DateTime? CreatedAfter { get; set; }
    public DateTime? CreatedBefore { get; set; }
    public DateTime? LastLoginAfter { get; set; }
    public DateTime? LastLoginBefore { get; set; }
    public List<Guid>? ExcludeUserIds { get; set; }
}

/// <summary>
/// 用户统计DTO
/// </summary>
public class UserStatisticsDto
{
    public int TotalUsers { get; set; }
    public int ActiveUsers { get; set; }
    public int DisabledUsers { get; set; }
    public int AdminUsers { get; set; }
    public int DoctorUsers { get; set; }
    public int RecentRegistrations { get; set; }
    public int InactiveUsers { get; set; }
}

/// <summary>
/// 用户注册趋势DTO
/// </summary>
public class UserRegistrationTrendDto
{
    public DateTime Date { get; set; }
    public int RegistrationCount { get; set; }
    public int CumulativeCount { get; set; }
}

/// <summary>
/// 用户活跃度统计DTO
/// </summary>
public class UserActivityStatisticsDto
{
    public int DailyActiveUsers { get; set; }
    public int WeeklyActiveUsers { get; set; }
    public int MonthlyActiveUsers { get; set; }
    public double AverageSessionDuration { get; set; }
    public DateTime LastActiveTime { get; set; }
}

/// <summary>
/// 查询性能DTO
/// </summary>
public class QueryPerformanceDto
{
    public double AverageQueryTime { get; set; }
    public int CacheHitRate { get; set; }
    public int TotalQueries { get; set; }
    public int SlowQueries { get; set; }
}

/// <summary>
/// 用户概要DTO
/// </summary>
public class UserSummaryDto
{
    public Guid Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string RealName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public bool IsEnabled { get; set; }
    public DateTime CreateTime { get; set; }
}

/// <summary>
/// 用户导出查询DTO
/// </summary>
public class UserExportQueryDto
{
    public List<Guid>? UserIds { get; set; }
    public UserRole? Role { get; set; }
    public bool? IsEnabled { get; set; }
    public DateTime? CreatedAfter { get; set; }
    public DateTime? CreatedBefore { get; set; }
    public bool IncludePersonalInfo { get; set; } = true;
    public bool IncludeSystemInfo { get; set; } = false;
}

/// <summary>
/// 用户导出DTO
/// </summary>
public class UserExportDto
{
    public string Username { get; set; } = string.Empty;
    public string RealName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime CreateTime { get; set; }
    public DateTime? LastLoginTime { get; set; }
}

/// <summary>
/// 用户基础信息DTO
/// </summary>
public class UserBasicInfoDto
{
    public Guid Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string RealName { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public bool IsEnabled { get; set; }
}

/// <summary>
/// 用户详细信息DTO
/// </summary>
public class UserDetailedInfoDto : UserBasicInfoDto
{
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public DateTime CreateTime { get; set; }
    public DateTime UpdateTime { get; set; }
    public DateTime? LastLoginTime { get; set; }
    public string Description { get; set; } = string.Empty;
}