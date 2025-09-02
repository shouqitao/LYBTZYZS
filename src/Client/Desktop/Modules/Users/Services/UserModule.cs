using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LYBT.Desktop.Users.Interfaces;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Users;

namespace LYBT.Desktop.Users.Services;

/// <summary>
/// 用户模块 - UltraThink双层架构纯委托层
/// 职责：统一服务入口，请求路由分发
/// 简化版：仅支持基础操作
/// </summary>
public class UserModule(
    IUserQueryService queryService,
    IUserBusinessService businessService) : IUserModule, IDisposable
{
    private readonly IUserQueryService _queryService = queryService;
    private readonly IUserBusinessService _businessService = businessService;

    #region 基础查询操作 - 对应简化接口

    /// <summary>
    /// 分页查询用户
    /// </summary>
    public async Task<ServiceResult<PagedResult<UserDto>>> GetPagedAsync(UserPagedQueryDto query)
        => await _queryService.GetPagedAsync(query);

    /// <summary>
    /// 根据ID获取用户
    /// </summary>
    public async Task<ServiceResult<UserDto>> GetByIdAsync(Guid id)
        => await _queryService.GetByIdAsync(id);

    /// <summary>
    /// 获取当前用户个人信息
    /// </summary>
    public async Task<ServiceResult<UserDto>> GetProfileAsync()
        => await _queryService.GetProfileAsync();

    /// <summary>
    /// 获取所有角色列表
    /// </summary>
    public async Task<ServiceResult<IEnumerable<object>>> GetRolesAsync()
        => await _queryService.GetRolesAsync();

    /// <summary>
    /// 获取启用用户列表
    /// </summary>
    public async Task<ServiceResult<IEnumerable<UserDto>>> GetActiveUsersAsync()
        => await _queryService.GetActiveUsersAsync();

    /// <summary>
    /// 获取用户基础统计
    /// </summary>
    public async Task<ServiceResult<UserStatisticsDto>> GetBasicStatisticsAsync()
        => await _queryService.GetBasicStatisticsAsync();

    #endregion

    #region 基础业务操作 - 对应简化接口

    /// <summary>
    /// 创建用户
    /// </summary>
    public async Task<ServiceResult<UserDto>> CreateAsync(UserMutationDto createDto)
        => await _businessService.CreateAsync(createDto);

    /// <summary>
    /// 更新用户信息
    /// </summary>
    public async Task<ServiceResult<UserDto>> UpdateAsync(Guid id, UserMutationDto updateDto)
        => await _businessService.UpdateAsync(id, updateDto);

    /// <summary>
    /// 启用用户
    /// </summary>
    public async Task<ServiceResult<bool>> EnableAsync(Guid userId)
        => await _businessService.EnableAsync(userId);

    /// <summary>
    /// 禁用用户
    /// </summary>
    public async Task<ServiceResult<bool>> DisableAsync(Guid userId)
        => await _businessService.DisableAsync(userId);

    /// <summary>
    /// 重置用户密码
    /// </summary>
    public async Task<ServiceResult<bool>> ResetPasswordAsync(Guid userId, string defaultPassword)
        => await _businessService.ResetPasswordAsync(userId, defaultPassword);

    /// <summary>
    /// 修改用户密码
    /// </summary>
    public async Task<ServiceResult<bool>> ChangeUserPasswordAsync(string oldPassword, string newPassword)
        => await _businessService.ChangeUserPasswordAsync(oldPassword, newPassword);

    /// <summary>
    /// 修改个人信息
    /// </summary>
    public async Task<ServiceResult<bool>> ChangeProfileAsync(ChangeProfileDto profileDto)
        => await _businessService.ChangeProfileAsync(profileDto);

    #endregion

    #region 资源清理

    public void Dispose()
    {
        // 简化版本无需特殊清理
        GC.SuppressFinalize(this);
    }

    #endregion
}