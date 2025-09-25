using LYBT.Desktop.Core.Constants;
using LYBT.Desktop.Core.Services.Exceptions;
using LYBT.Desktop.Users.Interfaces;
using LYBT.Shared.Interfaces.Api;
using LYBT.Shared.Interfaces.Services;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Users;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Users.Services;

/// <summary>
/// 用户服务 - 重构后的统一实现
/// 合并原QueryService和BusinessService的所有功能
/// </summary>
public class UserService(
    ILogger<UserService> logger,
    IUserApi userApi,
    IExceptionHandler exceptionHandler) : IUserService
{
    private readonly ILogger<UserService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly IUserApi _userApi = userApi ?? throw new ArgumentNullException(nameof(userApi));
    private readonly IExceptionHandler _exceptionHandler = exceptionHandler ?? throw new ArgumentNullException(nameof(exceptionHandler));

    #region Query Operations

    /// <inheritdoc/>
    public async Task<ServiceResult<PagedResult<UserDto>>> GetPagedAsync(UserSearchDto query)
    {
        return await _exceptionHandler.HandleException<PagedResult<UserDto>>(
            async (ct) =>
            {
                _logger.LogDebug("执行用户分页查询，页码: {PageIndex}, 页大小: {PageSize}", query.PageIndex, query.PageSize);

                var refitResponse = await _userApi.GetUsersAsync(
                    page: query.PageIndex,
                    pageSize: query.PageSize,
                    keyword: query.Keyword).ConfigureAwait(false);

                ct.ThrowIfCancellationRequested();

                if (refitResponse.IsSuccessStatusCode && refitResponse.Content != null)
                {
                    var apiResponse = refitResponse.Content;
                    if (apiResponse.Success && apiResponse.Data != null)
                    {
                        return ServiceResult<PagedResult<UserDto>>.Success(apiResponse.Data);
                    }
                    return ServiceResult<PagedResult<UserDto>>.Failure(apiResponse.Message ?? "查询用户列表失败");
                }

                return ServiceResult<PagedResult<UserDto>>.Failure("查询用户网络请求失败");
            },
            nameof(GetPagedAsync), "用户分页查询", CancellationToken.None);
    }

    /// <inheritdoc/>
    public async Task<ServiceResult<UserDto>> GetByIdAsync(Guid id)
    {
        return await _exceptionHandler.HandleException<UserDto>(
            async (ct) =>
            {
                _logger.LogDebug("查询用户详情: {UserId}", id);

                var refitResponse = await _userApi.GetUserByIdAsync(id).ConfigureAwait(false);
                ct.ThrowIfCancellationRequested();

                if (refitResponse.IsSuccessStatusCode && refitResponse.Content != null)
                {
                    var apiResponse = refitResponse.Content;
                    if (apiResponse.Success && apiResponse.Data != null)
                    {
                        return ServiceResult<UserDto>.Success(apiResponse.Data);
                    }
                    return ServiceResult<UserDto>.Failure(apiResponse.Message ?? "用户不存在");
                }

                return ServiceResult<UserDto>.Failure("查询用户网络请求失败");
            },
            nameof(GetByIdAsync), $"查询用户详情: {id}", CancellationToken.None);
    }

    /// <inheritdoc/>
    public async Task<ServiceResult<UserDto>> GetByUsernameAsync(string username)
    {
        return await _exceptionHandler.HandleException<UserDto>(
            async (ct) =>
            {
                _logger.LogDebug("按用户名查询用户: {Username}", username);

                var refitResponse = await _userApi.GetUsersAsync(
                    page: 1,
                    pageSize: 1,
                    username: username).ConfigureAwait(false);

                ct.ThrowIfCancellationRequested();

                if (refitResponse.IsSuccessStatusCode && refitResponse.Content != null)
                {
                    var apiResponse = refitResponse.Content;
                    if (apiResponse.Success && apiResponse.Data != null && apiResponse.Data.Items.Any())
                    {
                        var user = apiResponse.Data.Items.First();
                        return ServiceResult<UserDto>.Success(user, "用户查询成功");
                    }
                    return ServiceResult<UserDto>.Failure("未找到指定用户名的用户");
                }

                return ServiceResult<UserDto>.Failure("按用户名查询网络请求失败");
            },
            nameof(GetByUsernameAsync), $"按用户名查询: {username}", CancellationToken.None);
    }

    /// <inheritdoc/>
    public async Task<ServiceResult<List<UserDto>>> SearchAsync(string keyword)
    {
        return await _exceptionHandler.HandleException<List<UserDto>>(
            async (ct) =>
            {
                _logger.LogDebug("搜索用户: {Keyword}", keyword);

                if (string.IsNullOrWhiteSpace(keyword))
                {
                    return ServiceResult<List<UserDto>>.Success([]);
                }

                var refitResponse = await _userApi.GetUsersAsync(
                    page: 1,
                    pageSize: 100,
                    keyword: keyword).ConfigureAwait(false);

                ct.ThrowIfCancellationRequested();

                if (refitResponse.IsSuccessStatusCode && refitResponse.Content != null)
                {
                    var apiResponse = refitResponse.Content;
                    if (apiResponse.Success && apiResponse.Data != null)
                    {
                        var users = apiResponse.Data.Items.ToList();
                        _logger.LogDebug("用户搜索成功: {Keyword}, 结果数: {Count}", keyword, users.Count);
                        return ServiceResult<List<UserDto>>.Success(users, "搜索成功");
                    }
                    return ServiceResult<List<UserDto>>.Failure(apiResponse.Message ?? "用户搜索失败");
                }

                return ServiceResult<List<UserDto>>.Success([], "搜索网络请求失败，返回空结果");
            },
            nameof(SearchAsync), $"搜索用户: {keyword}", CancellationToken.None);
    }

    /// <inheritdoc/>
    public async Task<ServiceResult<List<UserDto>>> GetActiveUsersAsync()
    {
        return await _exceptionHandler.HandleException<List<UserDto>>(
            async (ct) =>
            {
                _logger.LogDebug("获取活跃用户列表");

                var refitResponse = await _userApi.GetActiveUsersAsync().ConfigureAwait(false);
                ct.ThrowIfCancellationRequested();

                if (refitResponse.IsSuccessStatusCode && refitResponse.Content != null)
                {
                    var apiResponse = refitResponse.Content;
                    if (apiResponse.Success && apiResponse.Data != null)
                    {
                        var activeUsers = apiResponse.Data.ToList();
                        _logger.LogDebug("获取活跃用户列表成功，用户数: {Count}", activeUsers.Count);
                        return ServiceResult<List<UserDto>>.Success(activeUsers, "获取活跃用户列表成功");
                    }
                    return ServiceResult<List<UserDto>>.Failure(apiResponse.Message ?? "获取活跃用户列表失败");
                }

                return ServiceResult<List<UserDto>>.Failure("获取活跃用户列表网络请求失败");
            },
            nameof(GetActiveUsersAsync), "获取活跃用户列表", CancellationToken.None);
    }

    /// <inheritdoc/>
    public Task<ServiceResult<List<object>>> GetRolesAsync()
    {
        _logger.LogDebug("获取系统角色列表");

        var roles = new List<object>
        {
            new { Id = SystemConstants.AdminRole, Name = "管理员" },
            new { Id = SystemConstants.DoctorRole, Name = "医生" }
        };

        return Task.FromResult(ServiceResult<List<object>>.Success(roles));
    }

    /// <inheritdoc/>
    public async Task<ServiceResult<bool>> ValidateUsernameAsync(string username)
    {
        return await _exceptionHandler.HandleException<bool>(
            async (ct) =>
            {
                _logger.LogDebug("验证用户名可用性: {Username}", username);

                if (string.IsNullOrWhiteSpace(username))
                {
                    return ServiceResult<bool>.Failure("用户名不能为空");
                }

                var userResult = await GetByUsernameAsync(username);
                ct.ThrowIfCancellationRequested();

                if (userResult.IsSuccess && userResult.Data != null)
                {
                    return ServiceResult<bool>.Success(false, "用户名已存在");
                }

                if (userResult.ErrorMessage?.Contains("未找到") == true)
                {
                    return ServiceResult<bool>.Success(true, "用户名可用");
                }

                return ServiceResult<bool>.Failure(userResult.ErrorMessage ?? "用户名验证失败");
            },
            nameof(ValidateUsernameAsync), $"验证用户名: {username}", CancellationToken.None);
    }

    #endregion

    #region Business Operations

    /// <inheritdoc/>
    public async Task<ServiceResult<UserDto>> CreateAsync(UserCreateDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto, nameof(dto));

        return await _exceptionHandler.HandleException<UserDto>(
            async (ct) =>
            {
                _logger.LogInformation("开始处理用户创建: {Username}", dto.Username);

                var refitResponse = await _userApi.CreateUserAsync(dto).ConfigureAwait(false);
                ct.ThrowIfCancellationRequested();

                if (refitResponse.IsSuccessStatusCode && refitResponse.Content != null)
                {
                    var apiResponse = refitResponse.Content;
                    if (apiResponse.Success && apiResponse.Data != null)
                    {
                        _logger.LogInformation("用户创建成功: {Username}", apiResponse.Data.Username);
                        return ServiceResult<UserDto>.Success(apiResponse.Data);
                    }
                    _logger.LogWarning("用户创建业务失败: {Username}, 错误: {Message}", dto.Username, apiResponse.Message);
                    return ServiceResult<UserDto>.Failure(apiResponse.Message ?? "创建用户失败，请检查输入信息");
                }

                _logger.LogWarning("用户创建HTTP请求失败: {Username}, 状态码: {StatusCode}", dto.Username, refitResponse.StatusCode);
                return ServiceResult<UserDto>.Failure("创建用户网络请求失败，请检查网络连接");
            },
            nameof(CreateAsync), $"创建用户: {dto.Username}", CancellationToken.None);
    }

    /// <inheritdoc/>
    public async Task<ServiceResult<UserDto>> UpdateAsync(UserUpdateDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto, nameof(dto));

        return await _exceptionHandler.HandleException<UserDto>(
            async (ct) =>
            {
                _logger.LogInformation("开始处理用户更新: {UserId}", dto.Id);

                var refitResponse = await _userApi.UpdateUserAsync(dto.Id, dto).ConfigureAwait(false);
                ct.ThrowIfCancellationRequested();

                if (refitResponse.IsSuccessStatusCode && refitResponse.Content != null)
                {
                    var apiResponse = refitResponse.Content;
                    if (apiResponse.Success && apiResponse.Data != null)
                    {
                        _logger.LogInformation("用户更新成功: {UserId}", dto.Id);
                        return ServiceResult<UserDto>.Success(apiResponse.Data);
                    }
                    _logger.LogWarning("用户更新业务失败: {UserId}, 错误: {Message}", dto.Id, apiResponse.Message);
                    return ServiceResult<UserDto>.Failure(apiResponse.Message ?? "更新用户失败，请检查输入信息");
                }

                _logger.LogWarning("用户更新HTTP请求失败: {UserId}, 状态码: {StatusCode}", dto.Id, refitResponse.StatusCode);
                return ServiceResult<UserDto>.Failure("更新用户网络请求失败，请检查网络连接");
            },
            nameof(UpdateAsync), $"更新用户: {dto.Id}", CancellationToken.None);
    }

    /// <inheritdoc/>
    public async Task<ServiceResult<bool>> DeleteAsync(Guid id)
    {
        return await _exceptionHandler.HandleException<bool>(
            async (ct) =>
            {
                _logger.LogInformation("删除用户: {UserId}", id);

                // 使用ToggleStatus接口来软删除用户（设为禁用状态）
                var refitResponse = await _userApi.ToggleStatusAsync(id).ConfigureAwait(false);
                ct.ThrowIfCancellationRequested();

                if (refitResponse.IsSuccessStatusCode && refitResponse.Content != null)
                {
                    var apiResponse = refitResponse.Content;
                    if (apiResponse.Success)
                    {
                        _logger.LogInformation("用户删除成功: {UserId}", id);
                        return ServiceResult<bool>.Success(true, "用户已被禁用（软删除）");
                    }
                    _logger.LogWarning("用户删除业务失败: {UserId}, 错误: {Message}", id, apiResponse.Message);
                    return ServiceResult<bool>.Failure(apiResponse.Message ?? "删除用户失败");
                }

                _logger.LogWarning("用户删除HTTP请求失败: {UserId}, 状态码: {StatusCode}", id, refitResponse.StatusCode);
                return ServiceResult<bool>.Failure("删除用户网络请求失败，请检查网络连接");
            },
            nameof(DeleteAsync), $"删除用户: {id}", CancellationToken.None);
    }

    /// <inheritdoc/>
    public async Task<ServiceResult<bool>> EnableAsync(Guid id)
    {
        return await _exceptionHandler.HandleException<bool>(
            async (ct) =>
            {
                _logger.LogInformation("启用用户: {UserId}", id);

                var refitResponse = await _userApi.ToggleStatusAsync(id).ConfigureAwait(false);
                ct.ThrowIfCancellationRequested();

                if (refitResponse.IsSuccessStatusCode && refitResponse.Content != null)
                {
                    var apiResponse = refitResponse.Content;
                    if (apiResponse.Success)
                    {
                        _logger.LogInformation("用户启用成功: {UserId}", id);
                        return ServiceResult<bool>.Success(true, "用户已启用");
                    }
                    _logger.LogWarning("用户启用业务失败: {UserId}, 错误: {Message}", id, apiResponse.Message);
                    return ServiceResult<bool>.Failure(apiResponse.Message ?? "启用用户失败");
                }

                _logger.LogWarning("用户启用HTTP请求失败: {UserId}, 状态码: {StatusCode}", id, refitResponse.StatusCode);
                return ServiceResult<bool>.Failure("启用用户网络请求失败，请检查网络连接");
            },
            nameof(EnableAsync), $"启用用户: {id}", CancellationToken.None);
    }

    /// <inheritdoc/>
    public async Task<ServiceResult<bool>> DisableAsync(Guid id)
    {
        return await _exceptionHandler.HandleException<bool>(
            async (ct) =>
            {
                _logger.LogInformation("禁用用户: {UserId}", id);

                var refitResponse = await _userApi.ToggleStatusAsync(id).ConfigureAwait(false);
                ct.ThrowIfCancellationRequested();

                if (refitResponse.IsSuccessStatusCode && refitResponse.Content != null)
                {
                    var apiResponse = refitResponse.Content;
                    if (apiResponse.Success)
                    {
                        _logger.LogInformation("用户禁用成功: {UserId}", id);
                        return ServiceResult<bool>.Success(true, "用户已禁用");
                    }
                    _logger.LogWarning("用户禁用业务失败: {UserId}, 错误: {Message}", id, apiResponse.Message);
                    return ServiceResult<bool>.Failure(apiResponse.Message ?? "禁用用户失败");
                }

                _logger.LogWarning("用户禁用HTTP请求失败: {UserId}, 状态码: {StatusCode}", id, refitResponse.StatusCode);
                return ServiceResult<bool>.Failure("禁用用户网络请求失败，请检查网络连接");
            },
            nameof(DisableAsync), $"禁用用户: {id}", CancellationToken.None);
    }

    /// <inheritdoc/>
    public async Task<ServiceResult<int>> BatchEnableAsync(List<Guid> ids)
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
            },
            nameof(BatchEnableAsync), $"批量启用用户: {ids.Count}", CancellationToken.None);
    }

    /// <inheritdoc/>
    public async Task<ServiceResult<int>> BatchDisableAsync(List<Guid> ids)
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
            },
            nameof(BatchDisableAsync), $"批量禁用用户: {ids.Count}", CancellationToken.None);
    }

    #endregion

    #region Password Management

    /// <inheritdoc/>
    public async Task<ServiceResult<bool>> ResetPasswordAsync(Guid id, string newPassword)
    {
        return await _exceptionHandler.HandleException<bool>(
            async (ct) =>
            {
                _logger.LogInformation("重置用户密码: {UserId}", id);

                // newPassword参数被忽略，后端使用DefaultPasswordService
                var refitResponse = await _userApi.ResetPasswordAsync(id).ConfigureAwait(false);
                ct.ThrowIfCancellationRequested();

                if (refitResponse.IsSuccessStatusCode && refitResponse.Content != null)
                {
                    var apiResponse = refitResponse.Content;
                    if (apiResponse.Success)
                    {
                        _logger.LogInformation("用户密码重置成功: {UserId}", id);
                        return ServiceResult<bool>.Success(true, "用户密码已重置为默认密码");
                    }
                    _logger.LogWarning("用户密码重置业务失败: {UserId}, 错误: {Message}", id, apiResponse.Message);
                    return ServiceResult<bool>.Failure(apiResponse.Message ?? "密码重置失败");
                }

                _logger.LogWarning("用户密码重置HTTP请求失败: {UserId}, 状态码: {StatusCode}", id, refitResponse.StatusCode);
                return ServiceResult<bool>.Failure("密码重置网络请求失败，请检查网络连接");
            },
            nameof(ResetPasswordAsync), $"重置用户密码: {id}", CancellationToken.None);
    }

    /// <inheritdoc/>
    public async Task<ServiceResult<bool>> ChangePasswordAsync(Guid id, string oldPassword, string newPassword)
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
            },
            nameof(ChangePasswordAsync), $"修改用户密码: {id}", CancellationToken.None);
    }

    /// <inheritdoc/>
    public async Task<ServiceResult<bool>> ChangeProfileAsync(ChangeProfileDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto, nameof(dto));

        return await _exceptionHandler.HandleException<bool>(
            async (ct) =>
            {
                _logger.LogInformation("修改用户个人信息: {UserId}", dto.UserId);

                var refitResponse = await _userApi.ChangeProfileAsync(dto).ConfigureAwait(false);
                ct.ThrowIfCancellationRequested();

                if (refitResponse.IsSuccessStatusCode && refitResponse.Content != null)
                {
                    var apiResponse = refitResponse.Content;
                    if (apiResponse.Success)
                    {
                        _logger.LogInformation("用户个人信息修改成功: {UserId}", dto.UserId);
                        return ServiceResult<bool>.Success(true, "个人信息修改成功");
                    }
                    _logger.LogWarning("用户个人信息修改业务失败: {UserId}, 错误: {Message}", dto.UserId, apiResponse.Message);
                    return ServiceResult<bool>.Failure(apiResponse.Message ?? "个人信息修改失败");
                }

                _logger.LogWarning("用户个人信息修改HTTP请求失败: {UserId}, 状态码: {StatusCode}", dto.UserId, refitResponse.StatusCode);
                return ServiceResult<bool>.Failure("个人信息修改网络请求失败，请检查网络连接");
            },
            nameof(ChangeProfileAsync), $"修改用户个人信息: {dto.UserId}", CancellationToken.None);
    }

    #endregion
}