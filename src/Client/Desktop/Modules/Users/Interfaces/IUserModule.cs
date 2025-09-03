using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Users;

namespace LYBT.Desktop.Users.Interfaces;

/// <summary>
/// 用户模块接口 - UltraThink双层架构简化版
/// 职责：统一服务入口，纯委托模式
/// </summary>
public interface IUserModule : IDisposable
{
    #region 基础查询操作 - 简化版本

    /// <summary>
    /// 分页查询用户
    /// </summary>
    Task<ServiceResult<PagedResult<UserDto>>> GetPagedAsync(UserPagedQueryDto query);

    /// <summary>
    /// 根据ID获取用户
    /// </summary>
    Task<ServiceResult<UserDto>> GetByIdAsync(Guid id);

    /// <summary>
    /// 获取当前用户个人信息
    /// </summary>
    Task<ServiceResult<UserDto>> GetProfileAsync();

    /// <summary>
    /// 获取所有角色列表 - 更新返回类型与共享接口对齐
    /// </summary>
    Task<ServiceResult<List<object>>> GetRolesAsync();

    /// <summary>
    /// 获取启用用户列表 - 更新返回类型与共享接口对齐
    /// </summary>
    Task<ServiceResult<List<UserDto>>> GetActiveUsersAsync();

    /// <summary>
    /// 获取用户基础统计
    /// </summary>
    Task<ServiceResult<UserStatisticsDto>> GetBasicStatisticsAsync();

    #endregion

    #region 基础业务操作 - 简化版本

    /// <summary>
    /// 创建用户
    /// </summary>
    Task<ServiceResult<UserDto>> CreateAsync(UserMutationDto createDto);

    /// <summary>
    /// 更新用户信息
    /// </summary>
    Task<ServiceResult<UserDto>> UpdateAsync(Guid id, UserMutationDto updateDto);

    /// <summary>
    /// 启用用户
    /// </summary>
    Task<ServiceResult<bool>> EnableAsync(Guid userId);

    /// <summary>
    /// 禁用用户
    /// </summary>
    Task<ServiceResult<bool>> DisableAsync(Guid userId);

    /// <summary>
    /// 重置用户密码
    /// </summary>
    Task<ServiceResult<bool>> ResetPasswordAsync(Guid userId, string defaultPassword);

    /// <summary>
    /// 修改用户密码
    /// </summary>
    Task<ServiceResult<bool>> ChangeUserPasswordAsync(string oldPassword, string newPassword);

    /// <summary>
    /// 修改个人信息
    /// </summary>
    Task<ServiceResult<bool>> ChangeProfileAsync(ChangeProfileDto profileDto);

    #endregion
}