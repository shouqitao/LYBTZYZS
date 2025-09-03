using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LYBT.Desktop.Users.Interfaces;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Interfaces.Services;

namespace LYBT.Desktop.Users.Services;

/// <summary>
/// 用户模块 - UltraThink双层架构纯委托层
/// 职责：统一服务入口，请求路由分发
/// 现已实现共享IUserService接口，与后端完全对齐
/// </summary>
public class UserModule(
    IUserQueryService queryService,
    IUserBusinessService businessService) : IUserService
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
    public async Task<ServiceResult<List<object>>> GetRolesAsync()
        => await _queryService.GetRolesAsync();

    /// <summary>
    /// 获取启用用户列表
    /// </summary>
    public async Task<ServiceResult<List<UserDto>>> GetActiveUsersAsync()
        => await _queryService.GetActiveUsersAsync();

    /// <summary>
    /// 获取用户基础统计
    /// </summary>
    public async Task<ServiceResult<UserStatisticsDto>> GetBasicStatisticsAsync()
        => await _queryService.GetBasicStatisticsAsync();

    #endregion

    #region 基础业务操作 - 对应简化接口

    /// <summary>
    /// 创建用户 - 更新为与后端一致的方法名
    /// </summary>
    public async Task<ServiceResult<UserDto>> CreateAsync(UserMutationDto createDto)
        => await _businessService.CreateUserAsync(createDto);

    /// <summary>
    /// 更新用户信息 - 共享接口标准方法（DTO包含ID）
    /// </summary>
    public async Task<ServiceResult<UserDto>> UpdateAsync(UserMutationDto dto)
    {
        // 从DTO中提取ID，适配到内部两参数方法
        if (dto.Id == Guid.Empty)
        {
            return ServiceResult<UserDto>.Failure("更新用户时必须提供有效的用户ID");
        }
        return await _businessService.UpdateUserAsync(dto.Id, dto);
    }

    /// <summary>
    /// 更新用户信息 - 内部两参数方法（保持向后兼容）
    /// </summary>
    public async Task<ServiceResult<UserDto>> UpdateUserAsync(Guid id, UserMutationDto updateDto)
        => await _businessService.UpdateUserAsync(id, updateDto);

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

    /// <summary>
    /// 删除用户 - 与后端保持一致
    /// </summary>
    public async Task<ServiceResult<bool>> DeleteAsync(Guid userId)
        => await _businessService.DeleteUserAsync(userId);

    #endregion

    #region 共享接口IUserService额外方法 - 委托给相应服务层

    /// <summary>
    /// 根据用户名获取用户 - 委托给QueryService
    /// </summary>
    public async Task<ServiceResult<UserDto>> GetByUsernameAsync(string username)
        => await _queryService.GetByUsernameAsync(username);

    /// <summary>
    /// 批量启用用户 - 委托给BusinessService
    /// </summary>
    public async Task<ServiceResult<int>> BatchEnableAsync(List<Guid> ids)
        => await _businessService.BatchEnableAsync(ids);

    /// <summary>
    /// 批量禁用用户 - 委托给BusinessService
    /// </summary>
    public async Task<ServiceResult<int>> BatchDisableAsync(List<Guid> ids)
        => await _businessService.BatchDisableAsync(ids);

    /// <summary>
    /// 修改用户密码 - 管理员操作，委托给BusinessService
    /// </summary>
    public async Task<ServiceResult<bool>> ChangePasswordAsync(Guid id, string oldPassword, string newPassword)
        => await _businessService.ChangePasswordAsync(id, oldPassword, newPassword);

    /// <summary>
    /// 搜索用户 - 委托给QueryService
    /// </summary>
    public async Task<ServiceResult<List<UserDto>>> SearchAsync(string keyword)
        => await _queryService.SearchAsync(keyword);

    /// <summary>
    /// 验证用户名是否可用 - 委托给QueryService
    /// </summary>
    public async Task<ServiceResult<bool>> ValidateUsernameAsync(string username)
        => await _queryService.ValidateUsernameAsync(username);

    /// <summary>
    /// 获取用户操作日志 - 委托给QueryService
    /// </summary>
    public async Task<ServiceResult<PagedResult<object>>> GetOperationLogsAsync(Guid userId, PagedQueryBaseDto query)
        => await _queryService.GetOperationLogsAsync(userId, query);

    #endregion

    #region 资源清理

    public void Dispose()
    {
        // 简化版本无需特殊清理
        GC.SuppressFinalize(this);
    }

    #endregion
}