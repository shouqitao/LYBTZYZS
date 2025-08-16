using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LYBT.Desktop.Core.Models;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Users;

namespace LYBT.Desktop.Shared
{
    /// <summary>
    /// 共享用户服务接口
    /// 提供跨工作台的用户管理功能
    /// </summary>
    public interface ISharedUserService
    {
        /// <summary>
        /// 创建新用户
        /// </summary>
        /// <param name="dto">用户信息</param>
        /// <returns>创建的用户信息</returns>
        Task<ServiceResult<UserDto>> CreateUserAsync(UserCreateDto dto);

        /// <summary>
        /// 根据ID获取用户信息
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <returns>用户详细信息</returns>
        Task<ServiceResult<UserDto>> GetUserByIdAsync(Guid userId);

        /// <summary>
        /// 更新用户信息
        /// </summary>
        /// <param name="dto">更新的用户信息</param>
        /// <returns>更新结果</returns>
        Task<ServiceResult> UpdateUserAsync(Guid id, UserUpdateDto dto);

        /// <summary>
        /// 启用用户账号
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <returns>操作结果</returns>
        Task<ServiceResult> EnableUserAsync(Guid userId);

        /// <summary>
        /// 禁用用户账号
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <returns>操作结果</returns>
        Task<ServiceResult> DisableUserAsync(Guid userId);

        /// <summary>
        /// 重置用户密码
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <param name="newPassword">新密码</param>
        /// <returns>重置结果</returns>
        Task<ServiceResult> ResetPasswordAsync(Guid userId, string newPassword);

        /// <summary>
        /// 更改用户密码
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <param name="oldPassword">旧密码</param>
        /// <param name="newPassword">新密码</param>
        /// <returns>更改结果</returns>
        Task<ServiceResult> ChangePasswordAsync(Guid userId, string oldPassword, string newPassword);

        /// <summary>
        /// 搜索用户
        /// </summary>
        /// <param name="keyword">搜索关键词</param>
        /// <returns>匹配的用户列表</returns>
        Task<ServiceResult<List<UserDto>>> SearchUsersAsync(string keyword);

        /// <summary>
        /// 获取用户角色列表
        /// </summary>
        /// <returns>角色列表</returns>
        Task<ServiceResult<List<string>>> GetRolesAsync();

        /// <summary>
        /// 分配用户角色
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <param name="roles">角色列表</param>
        /// <returns>分配结果</returns>
        Task<ServiceResult> AssignRolesAsync(Guid userId, List<string> roles);

        /// <summary>
        /// 获取在线用户列表
        /// </summary>
        /// <returns>在线用户列表</returns>
        Task<ServiceResult<List<UserDto>>> GetOnlineUsersAsync();

        /// <summary>
        /// 验证用户权限
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <param name="permission">权限名称</param>
        /// <returns>是否有权限</returns>
        /// <summary>
        /// 分页查询用户列表
        /// </summary>
        /// <param name="queryParams">查询参数</param>
        /// <returns>分页用户列表</returns>
        Task<ServiceResult<PagedResult<UserDto>>> GetUsersAsync(Dictionary<string, object> queryParams);

        /// <summary>
        /// 根据ID获取单个用户信息
        /// </summary>
        /// <param name="id">用户ID</param>
        /// <returns>用户详细信息</returns>
        Task<ServiceResult<UserDto>> GetUserAsync(Guid id);

        /// <summary>
        /// 删除用户
        /// </summary>
        /// <param name="id">用户ID</param>
        /// <returns>删除结果</returns>
        Task<ServiceResult> DeleteUserAsync(Guid id);

        /// <summary>
        /// 获取用户统计信息
        /// </summary>
        /// <returns>统计信息</returns>
        Task<ServiceResult<UserStatisticsDto>> GetUserStatisticsAsync();

        /// <summary>
        /// 切换用户状态（启用/禁用）
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <returns>切换结果</returns>
        Task<ServiceResult> ToggleUserStatusAsync(Guid userId);

        /// <summary>
        /// 验证用户权限
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <param name="permission">权限名称</param>
        /// <returns>是否有权限</returns>
        Task<ServiceResult<bool>> CheckPermissionAsync(Guid userId, string permission);
    }
}