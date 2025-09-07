using LYBT.Desktop.Core.Services.Exceptions;
using LYBT.Desktop.Users.Interfaces;
using LYBT.Shared.Interfaces.Api;
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

    public Task<ServiceResult<bool>> DeleteAsync(Guid userId)
    {
        _logger.LogWarning("简单诊所版本暂不支持删除用户功能: {UserId}", userId);
        return Task.FromResult(ServiceResult<bool>.Failure("简单诊所版本暂不支持删除用户，确保历史数据完整性"));
    }

    #endregion 标准CRUD操作

    #region 状态管理操作

    public Task<ServiceResult<bool>> EnableAsync(Guid userId)
    {
        _logger.LogWarning("简单诊所版本暂不支持启用用户功能: {UserId}", userId);
        return Task.FromResult(ServiceResult<bool>.Failure("简单诊所版本暂不支持用户状态管理"));
    }

    public Task<ServiceResult<bool>> DisableAsync(Guid userId)
    {
        _logger.LogWarning("简单诊所版本暂不支持禁用用户功能: {UserId}", userId);
        return Task.FromResult(ServiceResult<bool>.Failure("简单诊所版本暂不支持用户状态管理"));
    }

    public Task<ServiceResult<int>> BatchEnableAsync(List<Guid> ids, CancellationToken cancellationToken = default)
{
    _logger.LogWarning("简单诊所版本暂不支持批量启用用户功能，请求用户数: {Count}", ids?.Count ?? 0);
    return Task.FromResult(ServiceResult<int>.Failure("简单诊所版本暂不支持批量启用操作"));
}

    public Task<ServiceResult<int>> BatchDisableAsync(List<Guid> ids, CancellationToken cancellationToken = default)
{
    _logger.LogWarning("简单诊所版本暂不支持批量禁用用户功能，请求用户数: {Count}", ids?.Count ?? 0);
    return Task.FromResult(ServiceResult<int>.Failure("简单诊所版本暂不支持批量禁用操作"));
}

    #endregion 状态管理操作

    #region 密码管理操作

    public Task<ServiceResult<bool>> ResetPasswordAsync(Guid userId, string newPassword, CancellationToken cancellationToken = default)
{
    _logger.LogWarning("简单诊所版本暂不支持重置密码功能: {UserId}", userId);
    return Task.FromResult(ServiceResult<bool>.Failure("简单诊所版本暂不支持密码重置功能"));
}

    public Task<ServiceResult<bool>> ChangePasswordAsync(Guid id, string oldPassword, string newPassword, CancellationToken cancellationToken = default)
{
    _logger.LogWarning("简单诊所版本暂不支持修改密码功能: {UserId}", id);
    return Task.FromResult(ServiceResult<bool>.Failure("简单诊所版本暂不支持密码修改功能"));
}

    public Task<ServiceResult<bool>> ChangeProfileAsync(ChangeProfileDto profileDto, CancellationToken cancellationToken = default)
{
    _logger.LogWarning("简单诊所版本暂不支持修改个人信息功能: {UserId}", profileDto?.UserId);
    return Task.FromResult(ServiceResult<bool>.Failure("简单诊所版本暂不支持个人信息修改功能"));
}

    #endregion 密码管理操作
}
