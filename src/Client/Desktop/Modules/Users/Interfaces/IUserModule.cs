using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Users;

namespace LYBT.Desktop.Users.Interfaces;

/// <summary>
/// 用户模块统一接口 - UltraThink三层架构统一入口
/// 继承共享接口以保持向后兼容性
/// </summary>
public interface IUserModule : LYBT.Shared.Interfaces.Services.IUserService
{
    #region 模块特定方法（不在共享接口中）
    
    /// <summary>
    /// 用户名可用性验证
    /// </summary>
    Task<ServiceResult<bool>> ValidateUsernameAsync(string username);
    
    /// <summary>
    /// 根据用户名获取用户
    /// </summary>
    Task<ServiceResult<UserDto>> GetByUsernameAsync(string username);
    
    /// <summary>
    /// 搜索用户
    /// </summary>
    Task<ServiceResult<List<UserDto>>> SearchAsync(string keyword);
    
    /// <summary>
    /// 获取活跃用户列表
    /// </summary>
    Task<ServiceResult<List<UserDto>>> GetActiveUsersAsync();
    
    /// <summary>
    /// 修改用户个人信息
    /// </summary>
    Task<ServiceResult<bool>> ChangeProfileAsync(ChangeProfileDto dto);
    
    /// <summary>
    /// 获取角色列表
    /// </summary>
    Task<ServiceResult<List<object>>> GetRolesAsync();
    
    /// <summary>
    /// 获取操作日志
    /// </summary>
    Task<ServiceResult<PagedResult<object>>> GetOperationLogsAsync(Guid userId, PagedQueryBaseDto query);
    
    /// <summary>
    /// 批量启用用户
    /// </summary>
    Task<ServiceResult<int>> BatchEnableAsync(List<Guid> ids);
    
    /// <summary>
    /// 批量禁用用户
    /// </summary>
    Task<ServiceResult<int>> BatchDisableAsync(List<Guid> ids);
    
    #endregion
}