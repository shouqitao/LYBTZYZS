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
    IUserApi userApi) : IUserBusinessService {
    private readonly ILogger<UserBusinessService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly IUserApi _userApi = userApi ?? throw new ArgumentNullException(nameof(userApi));

    #region 标准CRUD操作

    public async Task<ServiceResult<UserDto>> CreateAsync(UserMutationDto createDto) {
        ArgumentNullException.ThrowIfNull(createDto, nameof(createDto));

        try {
            _logger.LogInformation("开始处理用户创建: {Username}", createDto.Username);

            var refitResponse = await _userApi.CreateUserAsync(createDto);

            if (refitResponse.IsSuccessStatusCode && refitResponse.Content != null) {
                var apiResponse = refitResponse.Content;
                if (apiResponse.Success && apiResponse.Data != null) {
                    _logger.LogInformation("用户创建成功: {Username}", apiResponse.Data.Username);
                    return ServiceResult<UserDto>.Success(apiResponse.Data);
                }

                _logger.LogWarning("用户创建业务失败: {Username}, 错误: {Message}",
                    createDto.Username, apiResponse.Message);
                return ServiceResult<UserDto>.Failure(apiResponse.Message ?? "创建用户失败，请检查输入信息");
            }

            _logger.LogWarning("用户创建HTTP请求失败: {Username}, 状态码: {StatusCode}",
                createDto.Username, refitResponse.StatusCode);
            return ServiceResult<UserDto>.Failure("创建用户网络请求失败，请检查网络连接");
        } catch (Exception ex) {
            _logger.LogError(ex, "用户创建过程发生异常: {Username}", createDto.Username);
            return ServiceResult<UserDto>.Failure($"创建用户过程发生错误: {ex.Message}");
        }
    }

    public async Task<ServiceResult<UserDto>> UpdateAsync(UserMutationDto updateDto) {
        ArgumentNullException.ThrowIfNull(updateDto, nameof(updateDto));

        try {
            _logger.LogInformation("开始处理用户更新: {UserId}", updateDto.Id);

            var refitResponse = await _userApi.UpdateUserAsync(updateDto.Id, updateDto);

            if (refitResponse.IsSuccessStatusCode && refitResponse.Content != null) {
                var apiResponse = refitResponse.Content;
                if (apiResponse.Success && apiResponse.Data != null) {
                    _logger.LogInformation("用户更新成功: {UserId}", updateDto.Id);
                    return ServiceResult<UserDto>.Success(apiResponse.Data);
                }

                _logger.LogWarning("用户更新业务失败: {UserId}, 错误: {Message}",
                    updateDto.Id, apiResponse.Message);
                return ServiceResult<UserDto>.Failure(apiResponse.Message ?? "更新用户失败，请检查输入信息");
            }

            _logger.LogWarning("用户更新HTTP请求失败: {UserId}, 状态码: {StatusCode}",
                updateDto.Id, refitResponse.StatusCode);
            return ServiceResult<UserDto>.Failure("更新用户网络请求失败，请检查网络连接");
        } catch (Exception ex) {
            _logger.LogError(ex, "用户更新过程发生异常: {UserId}", updateDto.Id);
            return ServiceResult<UserDto>.Failure($"更新用户过程发生错误: {ex.Message}");
        }
    }

    public Task<ServiceResult<bool>> DeleteAsync(Guid userId) {
        _logger.LogWarning("简单诊所版本暂不支持删除用户功能: {UserId}", userId);
        return Task.FromResult(ServiceResult<bool>.Failure("简单诊所版本暂不支持删除用户，确保历史数据完整性"));
    }

    #endregion 标准CRUD操作

    #region 状态管理操作

    public Task<ServiceResult<bool>> EnableAsync(Guid userId) {
        _logger.LogWarning("简单诊所版本暂不支持启用用户功能: {UserId}", userId);
        return Task.FromResult(ServiceResult<bool>.Failure("简单诊所版本暂不支持用户状态管理"));
    }

    public Task<ServiceResult<bool>> DisableAsync(Guid userId) {
        _logger.LogWarning("简单诊所版本暂不支持禁用用户功能: {UserId}", userId);
        return Task.FromResult(ServiceResult<bool>.Failure("简单诊所版本暂不支持用户状态管理"));
    }

    public Task<ServiceResult<int>> BatchEnableAsync(List<Guid> ids) {
        _logger.LogWarning("简单诊所版本暂不支持批量启用用户功能，请求用户数: {Count}", ids?.Count ?? 0);
        return Task.FromResult(ServiceResult<int>.Failure("简单诊所版本暂不支持批量启用操作"));
    }

    public Task<ServiceResult<int>> BatchDisableAsync(List<Guid> ids) {
        _logger.LogWarning("简单诊所版本暂不支持批量禁用用户功能，请求用户数: {Count}", ids?.Count ?? 0);
        return Task.FromResult(ServiceResult<int>.Failure("简单诊所版本暂不支持批量禁用操作"));
    }

    #endregion 状态管理操作

    #region 密码管理操作

    public Task<ServiceResult<bool>> ResetPasswordAsync(Guid userId, string newPassword) {
        _logger.LogWarning("简单诊所版本暂不支持重置密码功能: {UserId}", userId);
        return Task.FromResult(ServiceResult<bool>.Failure("简单诊所版本暂不支持密码重置功能"));
    }

    public Task<ServiceResult<bool>> ChangePasswordAsync(Guid id, string oldPassword, string newPassword) {
        _logger.LogWarning("简单诊所版本暂不支持修改密码功能: {UserId}", id);
        return Task.FromResult(ServiceResult<bool>.Failure("简单诊所版本暂不支持密码修改功能"));
    }

    public Task<ServiceResult<bool>> ChangeProfileAsync(ChangeProfileDto profileDto) {
        _logger.LogWarning("简单诊所版本暂不支持修改个人信息功能: {UserId}", profileDto?.UserId);
        return Task.FromResult(ServiceResult<bool>.Failure("简单诊所版本暂不支持个人信息修改功能"));
    }

    #endregion 密码管理操作
}
