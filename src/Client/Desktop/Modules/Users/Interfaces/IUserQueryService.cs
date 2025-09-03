using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Users;

namespace LYBT.Desktop.Users.Interfaces;

/// <summary>
/// 用户查询服务接口 - UltraThink双层架构简化版
/// 职责：查询和搜索操作
/// </summary>
public interface IUserQueryService
{
    #region 基础查询操作 - 简化实现

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

    /// <summary>
    /// 根据用户名获取用户 - 新增方法与共享接口对齐
    /// </summary>
    Task<ServiceResult<UserDto>> GetByUsernameAsync(string username);

    /// <summary>
    /// 搜索用户 - 新增方法与共享接口对齐
    /// </summary>
    Task<ServiceResult<List<UserDto>>> SearchAsync(string keyword);

    /// <summary>
    /// 验证用户名是否可用 - 新增方法与共享接口对齐
    /// </summary>
    Task<ServiceResult<bool>> ValidateUsernameAsync(string username);

    /// <summary>
    /// 获取用户操作日志 - 新增方法与共享接口对齐
    /// </summary>
    Task<ServiceResult<PagedResult<object>>> GetOperationLogsAsync(Guid userId, PagedQueryBaseDto query);

    #endregion
}