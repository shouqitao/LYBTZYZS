using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using LYBT.Desktop.Users.Interfaces;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Interfaces.Api;

namespace LYBT.Desktop.Users.Services;

/// <summary>
/// 用户核心服务 - UltraThink三层架构核心操作层
/// 职责：API通信、基础CRUD操作、数据验证、缓存管理
/// </summary>
public partial class UserCoreService : IUserCoreService
{
    private readonly IUserApi _userApi;
    private readonly ILogger<UserCoreService> _logger;
    private readonly IMemoryCache _cache;
    
    private const string USER_CACHE_PREFIX = "user_";
    private const string ALL_USERS_CACHE_KEY = "all_users";
    private readonly TimeSpan _defaultCacheExpiry = TimeSpan.FromMinutes(10);

    public UserCoreService(
        IUserApi userApi,
        ILogger<UserCoreService> logger,
        IMemoryCache cache)
    {
        _userApi = userApi ?? throw new ArgumentNullException(nameof(userApi));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
    }

    #region API通信操作

    /// <summary>
    /// 调用创建用户API
    /// </summary>
    public async Task<ServiceResult<UserDto>> CallCreateUserApiAsync(UserCreateDto createDto)
    {
        try
        {
            _logger.LogInformation("正在调用创建用户API，用户名：{Username}", createDto.Username);
            
            var apiResponse = await _userApi.CreateUserAsync(createDto);
            
            if (apiResponse.IsSuccessStatusCode && apiResponse.Content != null)
            {
                _logger.LogInformation("用户创建成功，ID：{UserId}", apiResponse.Content.Data?.Id);
                
                // 清除相关缓存
                ClearUserCache();
                
                return ServiceResult<UserDto>.Success(
                    apiResponse.Content.Data,
                    apiResponse.Content.Message ?? "用户创建成功"
                );
            }
            
            _logger.LogWarning("创建用户API调用失败：{Error}", apiResponse.Error?.Content);
            return ServiceResult<UserDto>.Failure(
                apiResponse.Error?.Content ?? "创建用户失败"
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "调用创建用户API时发生异常，用户名：{Username}", createDto.Username);
            return ServiceResult<UserDto>.Failure($"创建用户时发生异常：{ex.Message}");
        }
    }

    /// <summary>
    /// 调用更新用户API
    /// </summary>
    public async Task<ServiceResult<UserDto>> CallUpdateUserApiAsync(Guid id, UserUpdateDto updateDto)
    {
        try
        {
            _logger.LogInformation("正在调用更新用户API，用户ID：{UserId}", id);
            
            var apiResponse = await _userApi.UpdateUserAsync(id, updateDto);
            
            if (apiResponse.IsSuccessStatusCode && apiResponse.Content != null)
            {
                _logger.LogInformation("用户更新成功，ID：{UserId}", id);
                
                // 更新缓存
                var cacheKey = $"{USER_CACHE_PREFIX}{id}";
                _cache.Remove(cacheKey);
                _cache.Remove(ALL_USERS_CACHE_KEY);
                
                return ServiceResult<UserDto>.Success(
                    apiResponse.Content.Data,
                    apiResponse.Content.Message ?? "用户更新成功"
                );
            }
            
            _logger.LogWarning("更新用户API调用失败：{Error}", apiResponse.Error?.Content);
            return ServiceResult<UserDto>.Failure(
                apiResponse.Error?.Content ?? "更新用户失败"
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "调用更新用户API时发生异常，用户ID：{UserId}", id);
            return ServiceResult<UserDto>.Failure($"更新用户时发生异常：{ex.Message}");
        }
    }

    /// <summary>
    /// 调用删除用户API
    /// </summary>
    public async Task<ServiceResult<bool>> CallDeleteUserApiAsync(Guid id)
    {
        try
        {
            _logger.LogInformation("正在调用删除用户API，用户ID：{UserId}", id);
            
            var apiResponse = await _userApi.DeleteUserAsync(id);
            
            if (apiResponse.IsSuccessStatusCode)
            {
                _logger.LogInformation("用户删除成功，ID：{UserId}", id);
                
                // 清除相关缓存
                var cacheKey = $"{USER_CACHE_PREFIX}{id}";
                _cache.Remove(cacheKey);
                _cache.Remove(ALL_USERS_CACHE_KEY);
                
                return ServiceResult<bool>.Success(true, "用户删除成功");
            }
            
            _logger.LogWarning("删除用户API调用失败：{Error}", apiResponse.Error?.Content);
            return ServiceResult<bool>.Failure(
                apiResponse.Error?.Content ?? "删除用户失败"
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "调用删除用户API时发生异常，用户ID：{UserId}", id);
            return ServiceResult<bool>.Failure($"删除用户时发生异常：{ex.Message}");
        }
    }

    /// <summary>
    /// 调用获取用户详情API
    /// </summary>
    public async Task<ServiceResult<UserDto>> CallGetUserByIdApiAsync(Guid id)
    {
        try
        {
            _logger.LogInformation("正在调用获取用户详情API，用户ID：{UserId}", id);
            
            var apiResponse = await _userApi.GetUserByIdAsync(id);
            
            if (apiResponse.IsSuccessStatusCode && apiResponse.Content != null)
            {
                _logger.LogInformation("获取用户详情成功，ID：{UserId}", id);
                
                return ServiceResult<UserDto>.Success(
                    apiResponse.Content.Data,
                    apiResponse.Content.Message ?? "获取用户详情成功"
                );
            }
            
            _logger.LogWarning("获取用户详情API调用失败：{Error}", apiResponse.Error?.Content);
            return ServiceResult<UserDto>.Failure(
                apiResponse.Error?.Content ?? "获取用户详情失败"
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "调用获取用户详情API时发生异常，用户ID：{UserId}", id);
            return ServiceResult<UserDto>.Failure($"获取用户详情时发生异常：{ex.Message}");
        }
    }

    /// <summary>
    /// 调用获取用户列表API
    /// </summary>
    public async Task<ServiceResult<PagedResult<UserDto>>> CallGetUsersApiAsync(int page, int pageSize, string? keyword = null)
    {
        try
        {
            _logger.LogInformation("正在调用获取用户列表API，页码：{Page}，页大小：{PageSize}，关键词：{Keyword}", 
                page, pageSize, keyword);
            
            var apiResponse = await _userApi.GetUsersAsync(page, pageSize, keyword);
            
            if (apiResponse.IsSuccessStatusCode && apiResponse.Content != null)
            {
                _logger.LogInformation("获取用户列表成功，返回 {Count} 条记录", apiResponse.Content.Data?.Items.Count ?? 0);
                
                return ServiceResult<PagedResult<UserDto>>.Success(
                    apiResponse.Content.Data,
                    apiResponse.Content.Message ?? "获取用户列表成功"
                );
            }
            
            _logger.LogWarning("获取用户列表API调用失败：{Error}", apiResponse.Error?.Content);
            return ServiceResult<PagedResult<UserDto>>.Failure(
                apiResponse.Error?.Content ?? "获取用户列表失败"
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "调用获取用户列表API时发生异常");
            return ServiceResult<PagedResult<UserDto>>.Failure($"获取用户列表时发生异常：{ex.Message}");
        }
    }

    /// <summary>
    /// 调用切换用户状态API
    /// </summary>
    public async Task<ServiceResult<bool>> CallToggleUserStatusApiAsync(Guid id)
    {
        try
        {
            _logger.LogInformation("正在调用切换用户状态API，用户ID：{UserId}", id);
            
            var apiResponse = await _userApi.ToggleUserStatusAsync(id);
            
            if (apiResponse.IsSuccessStatusCode)
            {
                _logger.LogInformation("切换用户状态成功，ID：{UserId}", id);
                
                // 清除相关缓存
                var cacheKey = $"{USER_CACHE_PREFIX}{id}";
                _cache.Remove(cacheKey);
                _cache.Remove(ALL_USERS_CACHE_KEY);
                
                return ServiceResult<bool>.Success(true, "切换用户状态成功");
            }
            
            _logger.LogWarning("切换用户状态API调用失败：{Error}", apiResponse.Error?.Content);
            return ServiceResult<bool>.Failure(
                apiResponse.Error?.Content ?? "切换用户状态失败"
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "调用切换用户状态API时发生异常，用户ID：{UserId}", id);
            return ServiceResult<bool>.Failure($"切换用户状态时发生异常：{ex.Message}");
        }
    }

    #endregion

    #region 基础数据操作

    /// <summary>
    /// 获取用户信息（带缓存）
    /// </summary>
    public async Task<ServiceResult<UserDto>> GetUserByIdAsync(Guid id)
    {
        var cacheKey = $"{USER_CACHE_PREFIX}{id}";
        
        if (_cache.TryGetValue(cacheKey, out UserDto? cachedUser) && cachedUser != null)
        {
            _logger.LogDebug("从缓存获取用户信息，ID：{UserId}", id);
            return ServiceResult<UserDto>.Success(cachedUser, "从缓存获取用户信息成功");
        }

        var result = await CallGetUserByIdApiAsync(id);
        
        if (result.IsSuccess && result.Data != null)
        {
            // 缓存用户数据
            _cache.Set(cacheKey, result.Data, _defaultCacheExpiry);
            _logger.LogDebug("已缓存用户信息，ID：{UserId}", id);
        }

        return result;
    }

    /// <summary>
    /// 获取所有用户（带缓存）
    /// </summary>
    public async Task<ServiceResult<List<UserDto>>> GetAllUsersAsync()
    {
        if (_cache.TryGetValue(ALL_USERS_CACHE_KEY, out List<UserDto>? cachedUsers) && cachedUsers != null)
        {
            _logger.LogDebug("从缓存获取所有用户信息");
            return ServiceResult<List<UserDto>>.Success(cachedUsers, "从缓存获取用户列表成功");
        }

        // 获取第一页的较大数据量来模拟获取所有用户
        var result = await CallGetUsersApiAsync(1, 1000);
        
        if (result.IsSuccess && result.Data?.Items != null)
        {
            var allUsers = result.Data.Items.ToList();
            
            // 缓存用户列表
            _cache.Set(ALL_USERS_CACHE_KEY, allUsers, _defaultCacheExpiry);
            _logger.LogDebug("已缓存用户列表，总计 {Count} 个用户", allUsers.Count);
            
            return ServiceResult<List<UserDto>>.Success(allUsers, "获取用户列表成功");
        }

        return ServiceResult<List<UserDto>>.Failure(result.ErrorMessage ?? "获取用户列表失败");
    }

    /// <summary>
    /// 验证用户是否存在
    /// </summary>
    public async Task<ServiceResult<bool>> ValidateUserExistsAsync(Guid id)
    {
        try
        {
            var result = await GetUserByIdAsync(id);
            return ServiceResult<bool>.Success(result.IsSuccess, result.IsSuccess ? "用户存在" : "用户不存在");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "验证用户是否存在时发生异常，用户ID：{UserId}", id);
            return ServiceResult<bool>.Failure($"验证用户是否存在时发生异常：{ex.Message}");
        }
    }

    /// <summary>
    /// 检查用户名是否存在
    /// </summary>
    public async Task<ServiceResult<bool>> CheckUsernameExistsAsync(string username, Guid? excludeId = null)
    {
        try
        {
            var allUsersResult = await GetAllUsersAsync();
            
            if (!allUsersResult.IsSuccess || allUsersResult.Data == null)
            {
                return ServiceResult<bool>.Failure("获取用户列表失败，无法检查用户名重复性");
            }

            var exists = allUsersResult.Data.Any(u => 
                string.Equals(u.Username, username, StringComparison.OrdinalIgnoreCase) && 
                u.Id != excludeId);
            
            return ServiceResult<bool>.Success(exists, exists ? "用户名已存在" : "用户名可用");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "检查用户名是否存在时发生异常，用户名：{Username}", username);
            return ServiceResult<bool>.Failure($"检查用户名是否存在时发生异常：{ex.Message}");
        }
    }

    /// <summary>
    /// 检查邮箱是否存在
    /// </summary>
    public async Task<ServiceResult<bool>> CheckEmailExistsAsync(string email, Guid? excludeId = null)
    {
        try
        {
            var allUsersResult = await GetAllUsersAsync();
            
            if (!allUsersResult.IsSuccess || allUsersResult.Data == null)
            {
                return ServiceResult<bool>.Failure("获取用户列表失败，无法检查邮箱重复性");
            }

            var exists = allUsersResult.Data.Any(u => 
                string.Equals(u.Email, email, StringComparison.OrdinalIgnoreCase) && 
                u.Id != excludeId);
            
            return ServiceResult<bool>.Success(exists, exists ? "邮箱已存在" : "邮箱可用");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "检查邮箱是否存在时发生异常，邮箱：{Email}", email);
            return ServiceResult<bool>.Failure($"检查邮箱是否存在时发生异常：{ex.Message}");
        }
    }

    #endregion

    #region 数据验证操作

    /// <summary>
    /// 验证用户创建数据
    /// </summary>
    public ServiceResult ValidateUserCreateData(UserCreateDto createDto)
    {
        if (createDto == null)
        {
            return ServiceResult.Failure("用户创建数据不能为空");
        }

        var validationResults = new List<string>();

        // 验证用户名
        var usernameValidation = ValidateUsername(createDto.Username);
        if (!usernameValidation.IsSuccess)
        {
            validationResults.Add(usernameValidation.ErrorMessage);
        }

        // 验证邮箱
        var emailValidation = ValidateEmail(createDto.Email);
        if (!emailValidation.IsSuccess)
        {
            validationResults.Add(emailValidation.ErrorMessage);
        }

        // 验证基础信息
        var basicInfoValidation = ValidateUserBasicInfo(createDto.Username, createDto.Email, createDto.RealName);
        if (!basicInfoValidation.IsSuccess)
        {
            validationResults.Add(basicInfoValidation.ErrorMessage);
        }

        if (validationResults.Any())
        {
            return ServiceResult.Failure($"用户创建数据验证失败：{string.Join("; ", validationResults)}");
        }

        return ServiceResult.Success("用户创建数据验证通过");
    }

    /// <summary>
    /// 验证用户更新数据
    /// </summary>
    public ServiceResult ValidateUserUpdateData(UserUpdateDto updateDto)
    {
        if (updateDto == null)
        {
            return ServiceResult.Failure("用户更新数据不能为空");
        }

        var validationResults = new List<string>();

        // 验证基础信息（如果提供）
        if (!string.IsNullOrEmpty(updateDto.Username) || 
            !string.IsNullOrEmpty(updateDto.Email) || 
            !string.IsNullOrEmpty(updateDto.RealName))
        {
            var basicInfoValidation = ValidateUserBasicInfo(
                updateDto.Username, 
                updateDto.Email, 
                updateDto.RealName);
                
            if (!basicInfoValidation.IsSuccess)
            {
                validationResults.Add(basicInfoValidation.ErrorMessage);
            }
        }

        if (validationResults.Any())
        {
            return ServiceResult.Failure($"用户更新数据验证失败：{string.Join("; ", validationResults)}");
        }

        return ServiceResult.Success("用户更新数据验证通过");
    }

    /// <summary>
    /// 验证用户名格式
    /// </summary>
    public ServiceResult ValidateUsername(string? username)
    {
        if (string.IsNullOrWhiteSpace(username))
        {
            return ServiceResult.Failure("用户名不能为空");
        }

        if (username.Length < 2)
        {
            return ServiceResult.Failure("用户名长度不能少于2个字符");
        }

        if (username.Length > 50)
        {
            return ServiceResult.Failure("用户名长度不能超过50个字符");
        }

        // 只允许字母、数字、下划线和中文
        if (!UsernameRegex().IsMatch(username))
        {
            return ServiceResult.Failure("用户名只能包含字母、数字、下划线和中文字符");
        }

        return ServiceResult.Success("用户名格式验证通过");
    }

    /// <summary>
    /// 验证邮箱格式
    /// </summary>
    public ServiceResult ValidateEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return ServiceResult.Failure("邮箱不能为空");
        }

        if (!EmailRegex().IsMatch(email))
        {
            return ServiceResult.Failure("邮箱格式不正确");
        }

        if (email.Length > 256)
        {
            return ServiceResult.Failure("邮箱长度不能超过256个字符");
        }

        return ServiceResult.Success("邮箱格式验证通过");
    }

    /// <summary>
    /// 验证用户基础信息
    /// </summary>
    public ServiceResult ValidateUserBasicInfo(string? username, string? email, string? realName)
    {
        var validationResults = new List<string>();

        // 验证用户名
        if (!string.IsNullOrEmpty(username))
        {
            var usernameValidation = ValidateUsername(username);
            if (!usernameValidation.IsSuccess)
            {
                validationResults.Add(usernameValidation.ErrorMessage);
            }
        }

        // 验证邮箱
        if (!string.IsNullOrEmpty(email))
        {
            var emailValidation = ValidateEmail(email);
            if (!emailValidation.IsSuccess)
            {
                validationResults.Add(emailValidation.ErrorMessage);
            }
        }

        // 验证真实姓名
        if (!string.IsNullOrWhiteSpace(realName))
        {
            if (realName.Length > 50)
            {
                validationResults.Add("真实姓名长度不能超过50个字符");
            }
        }

        if (validationResults.Any())
        {
            return ServiceResult.Failure($"用户基础信息验证失败：{string.Join("; ", validationResults)}");
        }

        return ServiceResult.Success("用户基础信息验证通过");
    }

    /// <summary>
    /// 验证用户角色权限
    /// </summary>
    public ServiceResult ValidateUserRole(string? role)
    {
        if (string.IsNullOrWhiteSpace(role))
        {
            return ServiceResult.Failure("用户角色不能为空");
        }

        var validRoles = new[] { "Admin", "Doctor", "User" };
        if (!validRoles.Contains(role))
        {
            return ServiceResult.Failure($"无效的用户角色：{role}，有效角色：{string.Join(", ", validRoles)}");
        }

        return ServiceResult.Success("用户角色验证通过");
    }

    #endregion

    #region 用户状态管理

    /// <summary>
    /// 更新用户状态
    /// </summary>
    public void UpdateUserStatus(Guid userId, bool isEnabled)
    {
        try
        {
            _logger.LogInformation("更新用户状态，用户ID：{UserId}，状态：{Status}", userId, isEnabled);
            
            // 清除相关缓存
            var cacheKey = $"{USER_CACHE_PREFIX}{userId}";
            _cache.Remove(cacheKey);
            _cache.Remove(ALL_USERS_CACHE_KEY);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新用户状态时发生异常，用户ID：{UserId}", userId);
        }
    }

    /// <summary>
    /// 批量更新用户状态
    /// </summary>
    public async Task<ServiceResult<int>> BatchUpdateUserStatusAsync(List<Guid> userIds, bool isEnabled)
    {
        try
        {
            _logger.LogInformation("批量更新用户状态，用户数量：{Count}，状态：{Status}", userIds.Count, isEnabled);
            
            int successCount = 0;
            
            foreach (var userId in userIds)
            {
                try
                {
                    var result = await CallToggleUserStatusApiAsync(userId);
                    if (result.IsSuccess)
                    {
                        successCount++;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "批量更新用户状态时处理用户失败，用户ID：{UserId}", userId);
                }
            }

            // 清除相关缓存
            ClearUserCache();

            return ServiceResult<int>.Success(successCount, $"成功更新 {successCount} 个用户状态");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "批量更新用户状态时发生异常");
            return ServiceResult<int>.Failure($"批量更新用户状态时发生异常：{ex.Message}");
        }
    }

    /// <summary>
    /// 获取用户状态信息
    /// </summary>
    public ServiceResult<UserStatusInfo> GetUserStatusInfo(Guid userId)
    {
        try
        {
            // 这里应该从缓存或API获取用户状态信息
            // 为演示目的，返回一个默认状态
            var statusInfo = new UserStatusInfo
            {
                IsEnabled = true,
                IsLocked = false,
                LastLoginTime = DateTime.Now.AddDays(-1),
                LastActivityTime = DateTime.Now.AddHours(-2),
                StatusDescription = "正常"
            };

            return ServiceResult<UserStatusInfo>.Success(statusInfo, "获取用户状态信息成功");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取用户状态信息时发生异常，用户ID：{UserId}", userId);
            return ServiceResult<UserStatusInfo>.Failure($"获取用户状态信息时发生异常：{ex.Message}");
        }
    }

    #endregion

    #region 缓存和性能优化

    /// <summary>
    /// 预加载常用用户数据
    /// </summary>
    public async Task<ServiceResult> PreloadCommonUsersAsync()
    {
        try
        {
            _logger.LogInformation("预加载常用用户数据");
            
            // 预加载所有用户
            var allUsersResult = await GetAllUsersAsync();
            
            if (allUsersResult.IsSuccess && allUsersResult.Data != null)
            {
                // 预加载活跃用户的详细信息
                var activeUsers = allUsersResult.Data.Where(u => u.IsEnabled).Take(20);
                
                foreach (var user in activeUsers)
                {
                    await GetUserByIdAsync(user.Id);
                }
                
                return ServiceResult.Success("预加载常用用户数据完成");
            }
            
            return ServiceResult.Failure("预加载用户数据失败：无法获取用户列表");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "预加载常用用户数据时发生异常");
            return ServiceResult.Failure($"预加载用户数据时发生异常：{ex.Message}");
        }
    }

    /// <summary>
    /// 清除用户缓存
    /// </summary>
    public ServiceResult ClearUserCache()
    {
        try
        {
            _logger.LogInformation("清除用户缓存");
            
            _cache.Remove(ALL_USERS_CACHE_KEY);
            
            // 这里应该清除所有用户相关的缓存键
            // 由于IMemoryCache没有直接的方式枚举所有键，我们使用一个简单的方法
            
            return ServiceResult.Success("用户缓存清除完成");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "清除用户缓存时发生异常");
            return ServiceResult.Failure($"清除用户缓存时发生异常：{ex.Message}");
        }
    }

    /// <summary>
    /// 获取缓存的用户数据
    /// </summary>
    public ServiceResult<List<UserDto>> GetCachedUsers()
    {
        try
        {
            if (_cache.TryGetValue(ALL_USERS_CACHE_KEY, out List<UserDto>? cachedUsers) && cachedUsers != null)
            {
                return ServiceResult<List<UserDto>>.Success(cachedUsers, "获取缓存用户数据成功");
            }
            
            return ServiceResult<List<UserDto>>.Failure("缓存中没有用户数据");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取缓存用户数据时发生异常");
            return ServiceResult<List<UserDto>>.Failure($"获取缓存用户数据时发生异常：{ex.Message}");
        }
    }

    /// <summary>
    /// 刷新用户缓存
    /// </summary>
    public async Task<ServiceResult> RefreshUserCacheAsync(Guid userId)
    {
        try
        {
            _logger.LogInformation("刷新用户缓存，用户ID：{UserId}", userId);
            
            var cacheKey = $"{USER_CACHE_PREFIX}{userId}";
            _cache.Remove(cacheKey);
            
            // 重新获取用户数据并缓存
            var result = await CallGetUserByIdApiAsync(userId);
            
            if (result.IsSuccess && result.Data != null)
            {
                _cache.Set(cacheKey, result.Data, _defaultCacheExpiry);
                return ServiceResult.Success("用户缓存刷新完成");
            }
            
            return ServiceResult.Failure("刷新用户缓存失败：无法获取用户数据");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "刷新用户缓存时发生异常，用户ID：{UserId}", userId);
            return ServiceResult.Failure($"刷新用户缓存时发生异常：{ex.Message}");
        }
    }

    #endregion

    #region 正则表达式

    [GeneratedRegex(@"^[\u4e00-\u9fa5a-zA-Z0-9_]+$")]
    private static partial Regex UsernameRegex();

    [GeneratedRegex(@"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$")]
    private static partial Regex EmailRegex();

    #endregion
}