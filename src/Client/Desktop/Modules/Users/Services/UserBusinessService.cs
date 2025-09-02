using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using LYBT.Desktop.Users.Interfaces;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Users;

namespace LYBT.Desktop.Users.Services;

/// <summary>
/// 用户业务服务实现 - UltraThink双层架构简化版
/// 职责：基础业务操作
/// </summary>
public class UserBusinessService(ILogger<UserBusinessService> logger) : IUserBusinessService
{
    private readonly ILogger<UserBusinessService> _logger = logger;

    #region 基础用户业务操作 - 简化实现

    /// <summary>
    /// 创建用户
    /// </summary>
    public async Task<ServiceResult<UserDto>> CreateAsync(UserMutationDto createDto)
    {
        return ServiceResult<UserDto>.Failure("简单诊所版本暂不支持创建用户");
    }

    /// <summary>
    /// 更新用户信息
    /// </summary>
    public async Task<ServiceResult<UserDto>> UpdateAsync(Guid id, UserMutationDto updateDto)
    {
        return ServiceResult<UserDto>.Failure("简单诊所版本暂不支持更新用户信息");
    }

    /// <summary>
    /// 启用用户
    /// </summary>
    public async Task<ServiceResult<bool>> EnableAsync(Guid userId)
    {
        return ServiceResult<bool>.Success(false);
    }

    /// <summary>
    /// 禁用用户
    /// </summary>
    public async Task<ServiceResult<bool>> DisableAsync(Guid userId)
    {
        return ServiceResult<bool>.Success(false);
    }

    /// <summary>
    /// 重置用户密码
    /// </summary>
    public async Task<ServiceResult<bool>> ResetPasswordAsync(Guid userId, string defaultPassword)
    {
        return ServiceResult<bool>.Success(false);
    }

    /// <summary>
    /// 修改用户密码
    /// </summary>
    public async Task<ServiceResult<bool>> ChangeUserPasswordAsync(string oldPassword, string newPassword)
    {
        return ServiceResult<bool>.Success(false);
    }

    /// <summary>
    /// 修改个人信息
    /// </summary>
    public async Task<ServiceResult<bool>> ChangeProfileAsync(ChangeProfileDto profileDto)
    {
        return ServiceResult<bool>.Success(false);
    }

    #endregion
}