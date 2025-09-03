using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Users;

namespace LYBT.Desktop.Users.Interfaces;

/// <summary>
/// 用户查询服务接口 - UltraThink双层架构简化版
/// 职责：查询和搜索操作（仅保留核心查询功能）
/// </summary>
public interface IUserQueryService
{
    #region 核心查询操作

    /// <summary>
    /// 分页查询用户
    /// </summary>
    Task<ServiceResult<PagedResult<UserDto>>> GetPagedAsync(UserPagedQueryDto query);

    /// <summary>
    /// 根据ID获取用户
    /// </summary>
    Task<ServiceResult<UserDto>> GetByIdAsync(Guid id);

    /// <summary>
    /// 根据用户名获取用户
    /// </summary>
    Task<ServiceResult<UserDto>> GetByUsernameAsync(string username);

    /// <summary>
    /// 搜索用户
    /// </summary>
    Task<ServiceResult<List<UserDto>>> SearchAsync(string keyword);

    /// <summary>
    /// 获取启用用户列表
    /// </summary>
    Task<ServiceResult<List<UserDto>>> GetActiveUsersAsync();

    /// <summary>
    /// 获取所有角色列表
    /// </summary>
    Task<ServiceResult<List<object>>> GetRolesAsync();

    /// <summary>
    /// 验证用户名是否可用
    /// </summary>
    Task<ServiceResult<bool>> ValidateUsernameAsync(string username);

    #endregion
}