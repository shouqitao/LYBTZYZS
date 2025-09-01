using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Users;

namespace LYBT.Desktop.Users.Interfaces;

/// <summary>
/// 用户核心服务接口 - UltraThink三层架构核心操作层
/// 职责：API通信、基础CRUD操作、数据验证、缓存管理
/// </summary>
public interface IUserCoreService
{
    #region API通信操作
    
    /// <summary>
    /// 调用创建用户API
    /// </summary>
    Task<ServiceResult<UserDto>> CallCreateUserApiAsync(UserCreateDto createDto);
    
    /// <summary>
    /// 调用更新用户API
    /// </summary>
    Task<ServiceResult<UserDto>> CallUpdateUserApiAsync(Guid id, UserUpdateDto updateDto);
    
    /// <summary>
    /// 调用删除用户API
    /// </summary>
    Task<ServiceResult<bool>> CallDeleteUserApiAsync(Guid id);
    
    /// <summary>
    /// 调用获取用户详情API
    /// </summary>
    Task<ServiceResult<UserDto>> CallGetUserByIdApiAsync(Guid id);
    
    /// <summary>
    /// 调用获取用户列表API
    /// </summary>
    Task<ServiceResult<PagedResult<UserDto>>> CallGetUsersApiAsync(int page, int pageSize, string? keyword = null);
    
    /// <summary>
    /// 调用切换用户状态API
    /// </summary>
    Task<ServiceResult<bool>> CallToggleUserStatusApiAsync(Guid id);
    
    #endregion
    
    #region 基础数据操作
    
    /// <summary>
    /// 获取用户信息（带缓存）
    /// </summary>
    Task<ServiceResult<UserDto>> GetUserByIdAsync(Guid id);
    
    /// <summary>
    /// 获取所有用户（带缓存）
    /// </summary>
    Task<ServiceResult<List<UserDto>>> GetAllUsersAsync();
    
    /// <summary>
    /// 验证用户是否存在
    /// </summary>
    Task<ServiceResult<bool>> ValidateUserExistsAsync(Guid id);
    
    /// <summary>
    /// 检查用户名是否存在
    /// </summary>
    Task<ServiceResult<bool>> CheckUsernameExistsAsync(string username, Guid? excludeId = null);
    
    /// <summary>
    /// 检查邮箱是否存在
    /// </summary>
    Task<ServiceResult<bool>> CheckEmailExistsAsync(string email, Guid? excludeId = null);
    
    #endregion
    
    #region 数据验证操作
    
    /// <summary>
    /// 验证用户创建数据
    /// </summary>
    ServiceResult ValidateUserCreateData(UserCreateDto createDto);
    
    /// <summary>
    /// 验证用户更新数据
    /// </summary>
    ServiceResult ValidateUserUpdateData(UserUpdateDto updateDto);
    
    /// <summary>
    /// 验证用户名格式
    /// </summary>
    ServiceResult ValidateUsername(string? username);
    
    /// <summary>
    /// 验证邮箱格式
    /// </summary>
    ServiceResult ValidateEmail(string? email);
    
    /// <summary>
    /// 验证用户基础信息
    /// </summary>
    ServiceResult ValidateUserBasicInfo(string? username, string? email, string? realName);
    
    /// <summary>
    /// 验证用户角色权限
    /// </summary>
    ServiceResult ValidateUserRole(string? role);
    
    #endregion
    
    #region 用户状态管理
    
    /// <summary>
    /// 更新用户状态
    /// </summary>
    void UpdateUserStatus(Guid userId, bool isEnabled);
    
    /// <summary>
    /// 批量更新用户状态
    /// </summary>
    Task<ServiceResult<int>> BatchUpdateUserStatusAsync(List<Guid> userIds, bool isEnabled);
    
    /// <summary>
    /// 获取用户状态信息
    /// </summary>
    ServiceResult<UserStatusInfo> GetUserStatusInfo(Guid userId);
    
    #endregion
    
    #region 缓存和性能优化
    
    /// <summary>
    /// 预加载常用用户数据
    /// </summary>
    Task<ServiceResult> PreloadCommonUsersAsync();
    
    /// <summary>
    /// 清除用户缓存
    /// </summary>
    ServiceResult ClearUserCache();
    
    /// <summary>
    /// 获取缓存的用户数据
    /// </summary>
    ServiceResult<List<UserDto>> GetCachedUsers();
    
    /// <summary>
    /// 刷新用户缓存
    /// </summary>
    Task<ServiceResult> RefreshUserCacheAsync(Guid userId);
    
    #endregion
}

/// <summary>
/// 用户状态信息
/// </summary>
public class UserStatusInfo
{
    public bool IsEnabled { get; set; }
    public bool IsLocked { get; set; }
    public DateTime LastLoginTime { get; set; }
    public DateTime LastActivityTime { get; set; }
    public string StatusDescription { get; set; } = string.Empty;
}