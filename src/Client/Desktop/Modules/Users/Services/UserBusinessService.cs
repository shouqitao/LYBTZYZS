using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using LYBT.Desktop.Users.Interfaces;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Interfaces.Api;

namespace LYBT.Desktop.Users.Services;

/// <summary>
/// 用户业务服务实现 - UltraThink双层架构统一API版
/// 职责：基础业务操作，使用统一API客户端管理器
/// </summary>
public class UserBusinessService(
    ILogger<UserBusinessService> logger,
    IUserApi userApi) : IUserBusinessService
{
    private readonly ILogger<UserBusinessService> _logger = logger;
    private readonly IUserApi _userApi = userApi;

    #region 基础用户业务操作 - 简化实现

    /// <summary>
    /// 创建用户 - 与后端UserBusinessService.CreateUserAsync保持一致
    /// </summary>
    public async Task<ServiceResult<UserDto>> CreateUserAsync(UserMutationDto createDto)
    {
        try
        {
            var refitResponse = await _userApi.CreateUserAsync(createDto);
            
            if (refitResponse.IsSuccessStatusCode && refitResponse.Content != null)
            {
                var apiResponse = refitResponse.Content;
                if (apiResponse.Success && apiResponse.Data != null)
                {
                    _logger.LogInformation("用户创建成功: {Username}", createDto.Username);
                    return ServiceResult<UserDto>.Success(apiResponse.Data);
                }
                
                _logger.LogWarning("用户创建失败: {Username}, 消息: {Message}", 
                    createDto.Username, apiResponse.Message);
                return ServiceResult<UserDto>.Failure(apiResponse.Message ?? "创建用户失败");
            }
            
            _logger.LogWarning("用户创建HTTP请求失败: {Username}, 状态码: {StatusCode}", 
                createDto.Username, refitResponse.StatusCode);
            return ServiceResult<UserDto>.Failure("创建用户请求失败");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "用户创建异常: {Username}", createDto.Username);
            return ServiceResult<UserDto>.Failure($"创建用户过程发生错误: {ex.Message}");
        }
    }

    /// <summary>
    /// 保持向后兼容性的别名方法
    /// </summary>
    [Obsolete("使用CreateUserAsync替代，保持前后端契约一致性")]
    public async Task<ServiceResult<UserDto>> CreateAsync(UserMutationDto createDto)
        => await CreateUserAsync(createDto);

    /// <summary>
    /// 更新用户信息 - 与后端UserBusinessService.UpdateUserAsync保持一致
    /// </summary>
    public async Task<ServiceResult<UserDto>> UpdateUserAsync(Guid id, UserMutationDto updateDto)
    {
        try
        {
            var refitResponse = await _userApi.UpdateUserAsync(id, updateDto);
            
            if (refitResponse.IsSuccessStatusCode && refitResponse.Content != null)
            {
                var apiResponse = refitResponse.Content;
                if (apiResponse.Success && apiResponse.Data != null)
                {
                    _logger.LogInformation("用户更新成功: {UserId}", id);
                    return ServiceResult<UserDto>.Success(apiResponse.Data);
                }
                
                _logger.LogWarning("用户更新失败: {UserId}, 消息: {Message}", 
                    id, apiResponse.Message);
                return ServiceResult<UserDto>.Failure(apiResponse.Message ?? "更新用户失败");
            }
            
            _logger.LogWarning("用户更新HTTP请求失败: {UserId}, 状态码: {StatusCode}", 
                id, refitResponse.StatusCode);
            return ServiceResult<UserDto>.Failure("更新用户请求失败");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "用户更新异常: {UserId}", id);
            return ServiceResult<UserDto>.Failure($"更新用户过程发生错误: {ex.Message}");
        }
    }

    /// <summary>
    /// 保持向后兼容性的别名方法
    /// </summary>
    [Obsolete("使用UpdateUserAsync替代，保持前后端契约一致性")]
    public async Task<ServiceResult<UserDto>> UpdateAsync(Guid id, UserMutationDto updateDto)
        => await UpdateUserAsync(id, updateDto);

    /// <summary>
    /// 启用用户
    /// </summary>
    public Task<ServiceResult<bool>> EnableAsync(Guid userId)
    {
        return Task.FromResult(ServiceResult<bool>.Success(false));
    }

    /// <summary>
    /// 禁用用户
    /// </summary>
    public Task<ServiceResult<bool>> DisableAsync(Guid userId)
    {
        return Task.FromResult(ServiceResult<bool>.Success(false));
    }

    /// <summary>
    /// 重置用户密码
    /// </summary>
    public Task<ServiceResult<bool>> ResetPasswordAsync(Guid userId, string defaultPassword)
    {
        return Task.FromResult(ServiceResult<bool>.Success(false));
    }

    /// <summary>
    /// 修改用户密码
    /// </summary>
    public Task<ServiceResult<bool>> ChangeUserPasswordAsync(string oldPassword, string newPassword)
    {
        return Task.FromResult(ServiceResult<bool>.Success(false));
    }

    /// <summary>
    /// 修改个人信息
    /// </summary>
    public Task<ServiceResult<bool>> ChangeProfileAsync(ChangeProfileDto profileDto)
    {
        return Task.FromResult(ServiceResult<bool>.Success(false));
    }

    /// <summary>
    /// 删除用户 - 与后端UserBusinessService.DeleteUserAsync保持一致
    /// </summary>
    public Task<ServiceResult<bool>> DeleteUserAsync(Guid userId)
    {
        return Task.FromResult(ServiceResult<bool>.Failure("简单诊所版本暂不支持删除用户"));
    }

    /// <summary>
    /// 修改用户密码 - 管理员操作指定用户
    /// </summary>
    public Task<ServiceResult<bool>> ChangePasswordAsync(Guid id, string oldPassword, string newPassword)
    {
        return Task.FromResult(ServiceResult<bool>.Success(false));
    }

    /// <summary>
    /// 批量启用用户
    /// </summary>
    public Task<ServiceResult<int>> BatchEnableAsync(List<Guid> ids)
    {
        return Task.FromResult(ServiceResult<int>.Success(0));
    }

    /// <summary>
    /// 批量禁用用户
    /// </summary>
    public Task<ServiceResult<int>> BatchDisableAsync(List<Guid> ids)
    {
        return Task.FromResult(ServiceResult<int>.Success(0));
    }

    #endregion
}