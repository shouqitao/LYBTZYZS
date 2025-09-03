using System;
using System.Threading.Tasks;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Users;

namespace LYBT.Desktop.Users.Interfaces;

/// <summary>
/// 用户业务服务接口 - UltraThink双层架构简化版
/// 职责：基础业务操作
/// </summary>
public interface IUserBusinessService
{
    #region 基础用户业务操作

    /// <summary>
    /// 创建用户 - 与后端保持一致的新方法
    /// </summary>
    Task<ServiceResult<UserDto>> CreateUserAsync(UserMutationDto createDto);

    /// <summary>
    /// 创建用户 - 向后兼容别名
    /// </summary>
    [Obsolete("使用CreateUserAsync替代")]
    Task<ServiceResult<UserDto>> CreateAsync(UserMutationDto createDto);

    /// <summary>
    /// 更新用户信息 - 与后端保持一致的新方法
    /// </summary>
    Task<ServiceResult<UserDto>> UpdateUserAsync(Guid id, UserMutationDto updateDto);

    /// <summary>
    /// 更新用户信息 - 向后兼容别名
    /// </summary>
    [Obsolete("使用UpdateUserAsync替代")]
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
    /// 修改用户密码 - 个人密码修改(当前用户)
    /// </summary>
    Task<ServiceResult<bool>> ChangeUserPasswordAsync(string oldPassword, string newPassword);

    /// <summary>
    /// 修改用户密码 - 管理员操作(指定用户) - 与共享接口对齐
    /// </summary>
    Task<ServiceResult<bool>> ChangePasswordAsync(Guid id, string oldPassword, string newPassword);

    /// <summary>
    /// 修改个人信息
    /// </summary>
    Task<ServiceResult<bool>> ChangeProfileAsync(ChangeProfileDto profileDto);

    /// <summary>
    /// 删除用户 - 与后端保持一致
    /// </summary>
    Task<ServiceResult<bool>> DeleteUserAsync(Guid userId);

    /// <summary>
    /// 批量启用用户 - 新增方法与共享接口对齐
    /// </summary>
    Task<ServiceResult<int>> BatchEnableAsync(List<Guid> ids);

    /// <summary>
    /// 批量禁用用户 - 新增方法与共享接口对齐
    /// </summary>
    Task<ServiceResult<int>> BatchDisableAsync(List<Guid> ids);

    #endregion
}