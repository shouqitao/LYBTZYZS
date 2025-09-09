using LYBT.Desktop.Core.Services.Exceptions;
using LYBT.Desktop.Users.Interfaces;
using LYBT.Shared.Interfaces.Api;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Users;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Users.Services;

/// <summary>
/// 用户业务服务 - UltraThink双层架构业务逻辑层
/// 职责：处理用户业务逻辑、CRUD操作、状态管理、权限验证（精简版）
/// </summary>
public class UserBusinessService(
    ILogger<UserBusinessService> logger,
    IUserApi userApi,
    IExceptionHandler exceptionHandler) : IUserBusinessService
{
    private readonly ILogger<UserBusinessService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly IUserApi _userApi = userApi ?? throw new ArgumentNullException(nameof(userApi));
    private readonly IExceptionHandler _exceptionHandler = exceptionHandler ?? throw new ArgumentNullException(nameof(exceptionHandler));

    #region 标准CRUD操作

    public async Task<ServiceResult<UserDto>> CreateAsync(UserMutationDto createDto, CancellationToken cancellationToken = default)
{
    ArgumentNullException.ThrowIfNull(createDto, nameof(createDto));

    return await _exceptionHandler.HandleException<UserDto>(
        async (ct) =>
        {
            _logger.LogInformation("开始处理用户创建: {Username}", createDto.Username);

            // 传递取消令牌给API调用
            var refitResponse = await _userApi.CreateUserAsync(createDto).ConfigureAwait(false);
            
            // 检查是否取消
            ct.ThrowIfCancellationRequested();

            if (refitResponse.IsSuccessStatusCode && refitResponse.Content != null)
            {
                var apiResponse = refitResponse.Content;
                if (apiResponse.Success && apiResponse.Data != null)
                {
                    _logger.LogInformation("用户创建成功: {Username}", apiResponse.Data.Username);
                    return ServiceResult<UserDto>.Success(apiResponse.Data);
                }

                _logger.LogWarning(
                    "用户创建业务失败: {Username}, 错误: {Message}",
                    createDto.Username, apiResponse.Message);
                return ServiceResult<UserDto>.Failure(apiResponse.Message ?? "创建用户失败，请检查输入信息");
            }

            _logger.LogWarning(
                "用户创建HTTP请求失败: {Username}, 状态码: {StatusCode}",
                createDto.Username, refitResponse.StatusCode);
            return ServiceResult<UserDto>.Failure("创建用户网络请求失败，请检查网络连接");
        }
        , nameof(CreateAsync), $"创建用户: {createDto.Username}", cancellationToken);
}

    public async Task<ServiceResult<UserDto>> UpdateAsync(UserMutationDto updateDto, CancellationToken cancellationToken = default)
{
    ArgumentNullException.ThrowIfNull(updateDto, nameof(updateDto));

    return await _exceptionHandler.HandleException<UserDto>(
        async (ct) =>
        {
            _logger.LogInformation("开始处理用户更新: {UserId}", updateDto.Id);

            // 传递取消令牌给API调用
            var refitResponse = await _userApi.UpdateUserAsync(updateDto.Id, updateDto).ConfigureAwait(false);
            
            // 检查是否取消
            ct.ThrowIfCancellationRequested();

            if (refitResponse.IsSuccessStatusCode && refitResponse.Content != null)
            {
                var apiResponse = refitResponse.Content;
                if (apiResponse.Success && apiResponse.Data != null)
                {
                    _logger.LogInformation("用户更新成功: {UserId}", updateDto.Id);
                    return ServiceResult<UserDto>.Success(apiResponse.Data);
                }

                _logger.LogWarning(
                    "用户更新业务失败: {UserId}, 错误: {Message}",
                    updateDto.Id, apiResponse.Message);
                return ServiceResult<UserDto>.Failure(apiResponse.Message ?? "更新用户失败，请检查输入信息");
            }

            _logger.LogWarning(
                "用户更新HTTP请求失败: {UserId}, 状态码: {StatusCode}",
                updateDto.Id, refitResponse.StatusCode);
            return ServiceResult<UserDto>.Failure("更新用户网络请求失败，请检查网络连接");
        }
        , nameof(UpdateAsync), $"更新用户: {updateDto.Id}", cancellationToken);
}

    public async Task<ServiceResult<bool>> DeleteAsync(Guid userId)
    {
        return await _exceptionHandler.HandleException<bool>(
            async (ct) =>
            {
                _logger.LogInformation("删除用户: {UserId}", userId);
                
                // 注意：这里使用ToggleStatus接口来软删除用户（设为禁用状态）
                // 这样既实现了"删除"功能，又保持了历史数据完整性
                var refitResponse = await _userApi.ToggleStatusAsync(userId).ConfigureAwait(false);
                
                ct.ThrowIfCancellationRequested();

                if (refitResponse.IsSuccessStatusCode && refitResponse.Content != null)
                {
                    var apiResponse = refitResponse.Content;
                    if (apiResponse.Success)
                    {
                        _logger.LogInformation("用户删除成功: {UserId}", userId);
                        return ServiceResult<bool>.Success(true, "用户已被禁用（软删除）");
                    }

                    _logger.LogWarning("用户删除业务失败: {UserId}, 错误: {Message}", userId, apiResponse.Message);
                    return ServiceResult<bool>.Failure(apiResponse.Message ?? "删除用户失败");
                }

                _logger.LogWarning("用户删除HTTP请求失败: {UserId}, 状态码: {StatusCode}", userId, refitResponse.StatusCode);
                return ServiceResult<bool>.Failure("删除用户网络请求失败，请检查网络连接");
            }
            , nameof(DeleteAsync), $"删除用户: {userId}", CancellationToken.None);
    }

    #endregion 标准CRUD操作

    #region 状态管理操作

    public async Task<ServiceResult<bool>> EnableAsync(Guid userId)
    {
        return await _exceptionHandler.HandleException<bool>(
            async (ct) =>
            {
                _logger.LogInformation("启用用户: {UserId}", userId);
                
                var refitResponse = await _userApi.ToggleStatusAsync(userId).ConfigureAwait(false);
                
                ct.ThrowIfCancellationRequested();

                if (refitResponse.IsSuccessStatusCode && refitResponse.Content != null)
                {
                    var apiResponse = refitResponse.Content;
                    if (apiResponse.Success)
                    {
                        _logger.LogInformation("用户启用成功: {UserId}", userId);
                        return ServiceResult<bool>.Success(true, "用户已启用");
                    }

                    _logger.LogWarning("用户启用业务失败: {UserId}, 错误: {Message}", userId, apiResponse.Message);
                    return ServiceResult<bool>.Failure(apiResponse.Message ?? "启用用户失败");
                }

                _logger.LogWarning("用户启用HTTP请求失败: {UserId}, 状态码: {StatusCode}", userId, refitResponse.StatusCode);
                return ServiceResult<bool>.Failure("启用用户网络请求失败，请检查网络连接");
            }
            , nameof(EnableAsync), $"启用用户: {userId}", CancellationToken.None);
    }

    public async Task<ServiceResult<bool>> DisableAsync(Guid userId)
    {
        return await _exceptionHandler.HandleException<bool>(
            async (ct) =>
            {
                _logger.LogInformation("禁用用户: {UserId}", userId);
                
                var refitResponse = await _userApi.ToggleStatusAsync(userId).ConfigureAwait(false);
                
                ct.ThrowIfCancellationRequested();

                if (refitResponse.IsSuccessStatusCode && refitResponse.Content != null)
                {
                    var apiResponse = refitResponse.Content;
                    if (apiResponse.Success)
                    {
                        _logger.LogInformation("用户禁用成功: {UserId}", userId);
                        return ServiceResult<bool>.Success(true, "用户已禁用");
                    }

                    _logger.LogWarning("用户禁用业务失败: {UserId}, 错误: {Message}", userId, apiResponse.Message);
                    return ServiceResult<bool>.Failure(apiResponse.Message ?? "禁用用户失败");
                }

                _logger.LogWarning("用户禁用HTTP请求失败: {UserId}, 状态码: {StatusCode}", userId, refitResponse.StatusCode);
                return ServiceResult<bool>.Failure("禁用用户网络请求失败，请检查网络连接");
            }
            , nameof(DisableAsync), $"禁用用户: {userId}", CancellationToken.None);
    }

    public async Task<ServiceResult<int>> BatchEnableAsync(List<Guid> ids, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(ids, nameof(ids));
        
        if (!ids.Any())
        {
            return ServiceResult<int>.Failure("用户ID列表不能为空");
        }

        return await _exceptionHandler.HandleException<int>(
            async (ct) =>
            {
                _logger.LogInformation("批量启用用户，用户数: {Count}", ids.Count);
                
                var batchDto = new BatchIdsDto { Ids = ids };
                var refitResponse = await _userApi.BatchEnableAsync(batchDto).ConfigureAwait(false);
                
                ct.ThrowIfCancellationRequested();

                if (refitResponse.IsSuccessStatusCode && refitResponse.Content != null)
                {
                    var apiResponse = refitResponse.Content;
                    if (apiResponse.Success)
                    {
                        _logger.LogInformation("批量启用用户成功，用户数: {Count}", ids.Count);
                        return ServiceResult<int>.Success(ids.Count, $"成功启用 {ids.Count} 个用户");
                    }

                    _logger.LogWarning("批量启用用户业务失败，用户数: {Count}, 错误: {Message}", ids.Count, apiResponse.Message);
                    return ServiceResult<int>.Failure(apiResponse.Message ?? "批量启用用户失败");
                }

                _logger.LogWarning("批量启用用户HTTP请求失败，用户数: {Count}, 状态码: {StatusCode}", ids.Count, refitResponse.StatusCode);
                return ServiceResult<int>.Failure("批量启用用户网络请求失败，请检查网络连接");
            }
            , nameof(BatchEnableAsync), $"批量启用用户: {ids.Count}", cancellationToken);
    }

    public async Task<ServiceResult<int>> BatchDisableAsync(List<Guid> ids, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(ids, nameof(ids));
        
        if (!ids.Any())
        {
            return ServiceResult<int>.Failure("用户ID列表不能为空");
        }

        return await _exceptionHandler.HandleException<int>(
            async (ct) =>
            {
                _logger.LogInformation("批量禁用用户，用户数: {Count}", ids.Count);
                
                var batchDto = new BatchIdsDto { Ids = ids };
                var refitResponse = await _userApi.BatchDisableAsync(batchDto).ConfigureAwait(false);
                
                ct.ThrowIfCancellationRequested();

                if (refitResponse.IsSuccessStatusCode && refitResponse.Content != null)
                {
                    var apiResponse = refitResponse.Content;
                    if (apiResponse.Success)
                    {
                        _logger.LogInformation("批量禁用用户成功，用户数: {Count}", ids.Count);
                        return ServiceResult<int>.Success(ids.Count, $"成功禁用 {ids.Count} 个用户");
                    }

                    _logger.LogWarning("批量禁用用户业务失败，用户数: {Count}, 错误: {Message}", ids.Count, apiResponse.Message);
                    return ServiceResult<int>.Failure(apiResponse.Message ?? "批量禁用用户失败");
                }

                _logger.LogWarning("批量禁用用户HTTP请求失败，用户数: {Count}, 状态码: {StatusCode}", ids.Count, refitResponse.StatusCode);
                return ServiceResult<int>.Failure("批量禁用用户网络请求失败，请检查网络连接");
            }
            , nameof(BatchDisableAsync), $"批量禁用用户: {ids.Count}", cancellationToken);
    }

    #endregion 状态管理操作

    #region 密码管理操作

    public async Task<ServiceResult<bool>> ResetPasswordAsync(Guid userId, string newPassword, CancellationToken cancellationToken = default)
    {
        return await _exceptionHandler.HandleException<bool>(
            async (ct) =>
            {
                _logger.LogInformation("重置用户密码: {UserId}", userId);
                
                // 注意：ResetPasswordAsync API不需要newPassword参数，会重置为系统默认密码
                var refitResponse = await _userApi.ResetPasswordAsync(userId).ConfigureAwait(false);
                
                ct.ThrowIfCancellationRequested();

                if (refitResponse.IsSuccessStatusCode && refitResponse.Content != null)
                {
                    var apiResponse = refitResponse.Content;
                    if (apiResponse.Success)
                    {
                        _logger.LogInformation("用户密码重置成功: {UserId}", userId);
                        return ServiceResult<bool>.Success(true, "用户密码已重置为默认密码");
                    }

                    _logger.LogWarning("用户密码重置业务失败: {UserId}, 错误: {Message}", userId, apiResponse.Message);
                    return ServiceResult<bool>.Failure(apiResponse.Message ?? "密码重置失败");
                }

                _logger.LogWarning("用户密码重置HTTP请求失败: {UserId}, 状态码: {StatusCode}", userId, refitResponse.StatusCode);
                return ServiceResult<bool>.Failure("密码重置网络请求失败，请检查网络连接");
            }
            , nameof(ResetPasswordAsync), $"重置用户密码: {userId}", cancellationToken);
    }

    public async Task<ServiceResult<bool>> ChangePasswordAsync(Guid id, string oldPassword, string newPassword, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(oldPassword, nameof(oldPassword));
        ArgumentException.ThrowIfNullOrWhiteSpace(newPassword, nameof(newPassword));

        return await _exceptionHandler.HandleException<bool>(
            async (ct) =>
            {
                _logger.LogInformation("修改用户密码: {UserId}", id);
                
                var changePasswordDto = new ChangePasswordDto
                {
                    UserId = id,
                    OldPassword = oldPassword,
                    NewPassword = newPassword
                };
                
                var refitResponse = await _userApi.ChangePasswordAsync(changePasswordDto).ConfigureAwait(false);
                
                ct.ThrowIfCancellationRequested();

                if (refitResponse.IsSuccessStatusCode && refitResponse.Content != null)
                {
                    var apiResponse = refitResponse.Content;
                    if (apiResponse.Success)
                    {
                        _logger.LogInformation("用户密码修改成功: {UserId}", id);
                        return ServiceResult<bool>.Success(true, "密码修改成功");
                    }

                    _logger.LogWarning("用户密码修改业务失败: {UserId}, 错误: {Message}", id, apiResponse.Message);
                    return ServiceResult<bool>.Failure(apiResponse.Message ?? "密码修改失败");
                }

                _logger.LogWarning("用户密码修改HTTP请求失败: {UserId}, 状态码: {StatusCode}", id, refitResponse.StatusCode);
                return ServiceResult<bool>.Failure("密码修改网络请求失败，请检查网络连接");
            }
            , nameof(ChangePasswordAsync), $"修改用户密码: {id}", cancellationToken);
    }

    public async Task<ServiceResult<bool>> ChangeProfileAsync(ChangeProfileDto profileDto, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(profileDto, nameof(profileDto));

        return await _exceptionHandler.HandleException<bool>(
            async (ct) =>
            {
                _logger.LogInformation("修改用户个人信息: {UserId}", profileDto.UserId);
                
                var refitResponse = await _userApi.ChangeProfileAsync(profileDto).ConfigureAwait(false);
                
                ct.ThrowIfCancellationRequested();

                if (refitResponse.IsSuccessStatusCode && refitResponse.Content != null)
                {
                    var apiResponse = refitResponse.Content;
                    if (apiResponse.Success)
                    {
                        _logger.LogInformation("用户个人信息修改成功: {UserId}", profileDto.UserId);
                        return ServiceResult<bool>.Success(true, "个人信息修改成功");
                    }

                    _logger.LogWarning("用户个人信息修改业务失败: {UserId}, 错误: {Message}", profileDto.UserId, apiResponse.Message);
                    return ServiceResult<bool>.Failure(apiResponse.Message ?? "个人信息修改失败");
                }

                _logger.LogWarning("用户个人信息修改HTTP请求失败: {UserId}, 状态码: {StatusCode}", profileDto.UserId, refitResponse.StatusCode);
                return ServiceResult<bool>.Failure("个人信息修改网络请求失败，请检查网络连接");
            }
            , nameof(ChangeProfileAsync), $"修改用户个人信息: {profileDto.UserId}", cancellationToken);
    }

    #endregion 密码管理操作
}
