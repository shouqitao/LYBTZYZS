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