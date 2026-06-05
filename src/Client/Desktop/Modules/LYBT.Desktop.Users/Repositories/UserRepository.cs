using LYBT.Desktop.Contracts.ApiClient;
using LYBT.Desktop.Contracts.Repositories;
using LYBT.Shared.ExceptionHandling.Mappers;
using LYBT.Shared.Models.Contracts.Auth;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Users.Repositories;

/// <summary>
/// 用户仓储 — routes all calls through IApiClient.
/// </summary>
public sealed class UserRepository : IUserRepository
{
    private readonly IApiClient _apiClient;
    private readonly ILogger<UserRepository> _logger;

    public UserRepository(
        IApiClient apiClient,
        ILogger<UserRepository> logger)
    {
        _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    #region 标准 CRUD 操作

    public async Task<PagedResult<UserListDto>> GetPagedAsync(int page = 1, int pageSize = 20, string? keyword = null)
    {
        try
        {
            _logger.LogDebug("[REPO] User.GetPaged - Page={Page} PageSize={PageSize} Keyword={Keyword}",
                page, pageSize, keyword);

            var response = await _apiClient.Users.GetUsersAsync(page, pageSize, keyword);
            if (response.Data == null)
                return new PagedResult<UserListDto> { Items = [], TotalCount = 0, CurrentPage = page, PageSize = pageSize };

            return new PagedResult<UserListDto>
            {
                Items = response.Data.Items.ToList(),
                TotalCount = response.Data.TotalCount,
                CurrentPage = page,
                PageSize = pageSize
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[REPO] User.GetPaged failed");
            throw;
        }
    }

    public async Task<UserDetailDto?> GetByIdAsync(Guid id)
    {
        try
        {
            _logger.LogDebug("[REPO] User.GetById - Id={Id}", id);

            var response = await _apiClient.Users.GetUserByIdAsync(id);
            return response.Data;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[REPO] User.GetById failed - Id={Id}", id);
            throw;
        }
    }

    public async Task<UserDetailDto> CreateAsync(UserInputDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        try
        {
            _logger.LogInformation("[REPO] User.Create - UserName={UserName}", dto.UserName);

            var response = await _apiClient.Users.CreateUserAsync(dto);
            if (!response.Success || response.Data == null)
                throw new InvalidOperationException(response.Message ?? "创建用户失败");

            _logger.LogInformation("[REPO] User.Create completed - Id={Id}", response.Data.Id);
            return response.Data;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[REPO] User.Create failed - UserName={UserName}", dto.UserName);
            throw;
        }
    }

    public async Task<UserDetailDto> UpdateAsync(UserInputDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);
        if (dto.Id is null || dto.Id == Guid.Empty)
            throw new ArgumentException("更新DTO必须包含有效的ID", nameof(dto));

        try
        {
            _logger.LogInformation("[REPO] User.Update - Id={Id}", dto.Id);

            var response = await _apiClient.Users.UpdateUserAsync(dto.Id.Value, dto);
            if (!response.Success || response.Data == null)
                throw new InvalidOperationException(response.Message ?? "更新用户失败");

            _logger.LogInformation("[REPO] User.Update completed - Id={Id}", response.Data.Id);
            return response.Data;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[REPO] User.Update failed - Id={Id}", dto.Id);
            throw;
        }
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        try
        {
            _logger.LogInformation("[REPO] User.Delete - Id={Id}", id);

            var response = await _apiClient.Users.DeleteUserAsync(id);
            if (response.Success)
                _logger.LogInformation("[REPO] User.Delete completed - Id={Id}", id);
            else
                _logger.LogWarning("[REPO] User.Delete failed - Id={Id}", id);

            return response.Success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[REPO] User.Delete failed - Id={Id}", id);
            return false;
        }
    }

    public async Task<List<UserListDto>> SearchAsync(string keyword)
    {
        try
        {
            _logger.LogDebug("[REPO] User.Search - Keyword={Keyword}", keyword);

            var response = await _apiClient.Users.GetUsersAsync(1, 100, keyword);
            if (response.Data == null)
                return [];

            return response.Data.Items.ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[REPO] User.Search failed");
            throw;
        }
    }

    #endregion

    #region 用户专用方法

    public async Task<UserDetailDto> GetByUsernameAsync(string username)
    {
        try
        {
            _logger.LogDebug("[REPO] User.GetByUsername - Username={Username}", username);

            // 通过搜索找到匹配的用户
            var response = await _apiClient.Users.GetUsersAsync(1, 100, username);
            if (response.Data == null)
                throw new InvalidOperationException($"用户 {username} 不存在");

            // 从搜索结果中精确匹配用户名
            var remoteMatch = response.Data.Items.FirstOrDefault(u =>
                u.UserName.Equals(username, StringComparison.OrdinalIgnoreCase));

            if (remoteMatch == null)
                throw new InvalidOperationException($"用户 {username} 不存在");

            // 获取完整详情
            var detail = await GetByIdAsync(remoteMatch.Id);
            return detail ?? throw new InvalidOperationException($"用户 {username} 不存在");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[REPO] User.GetByUsername failed - Username={Username}", username);
            throw;
        }
    }

    public async Task<List<UserListDto>> GetDoctorsAsync()
    {
        try
        {
            _logger.LogDebug("[REPO] User.GetDoctors started");

            var response = await _apiClient.Users.GetUsersAsync(1, 100, null);
            if (response.Data?.Items == null)
            {
                _logger.LogWarning("[REPO] User.GetDoctors -> Empty result");
                return [];
            }

            // 筛选: 角色=医生 && 状态=启用
            var remoteDoctors = response.Data.Items
                .Where(u => u.Role == UserRole.Doctor && u.Status == CommonStatus.Enabled)
                .ToList();

            _logger.LogInformation("[REPO] User.GetDoctors completed - Count={Count}", remoteDoctors.Count);
            return remoteDoctors;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[REPO] User.GetDoctors failed");
            return [];
        }
    }

    public async Task<UserDetailDto> ChangeProfileAsync(Guid userId, ChangeProfileDto dto)
    {
        try
        {
            _logger.LogInformation("[REPO] User.ChangeProfile - UserId={UserId}", userId);

            var response = await _apiClient.Users.ChangeProfileAsync(userId, dto);
            if (response.Success && response.Data != null)
            {
                _logger.LogInformation("[REPO] User.ChangeProfile completed - UserId={UserId}", userId);
                return response.Data;
            }

            var errorMsg = response.Message ?? "修改个人资料失败";
            _logger.LogWarning("[REPO] User.ChangeProfile failed - {Message}", errorMsg);
            throw new InvalidOperationException(errorMsg);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[REPO] User.ChangeProfile failed - UserId={UserId}", userId);
            throw;
        }
    }

    public async Task<ServiceResult> ChangePasswordAsync(Guid userId, ChangePasswordRequest request)
    {
        try
        {
            _logger.LogInformation("[REPO] User.ChangePassword - UserId={UserId}", userId);

            var response = await _apiClient.Users.ChangePasswordAsync(userId, request);
            if (response.Success)
            {
                _logger.LogInformation("[REPO] User.ChangePassword completed - UserId={UserId}", userId);
                return ServiceResult.Success();
            }

            var errorMsg = response.Message ?? "修改密码失败";
            _logger.LogWarning("[REPO] User.ChangePassword failed - {Message}", errorMsg);
            return ServiceResult.Failure(errorMsg);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[REPO] User.ChangePassword failed - UserId={UserId}", userId);
            return ServiceResult.Failure(ClientErrorMessageMapper.GetSafeOperationFailureMessage("修改密码", ex));
        }
    }

    public async Task<ServiceResult<ResetPasswordResponseDto>> ResetPasswordAsync(
        Guid userId,
        ResetPasswordRequestDto request)
    {
        try
        {
            _logger.LogDebug("[REPO] User.ResetPassword - UserId={UserId}", userId);

            var apiResponse = await _apiClient.Users.ResetPasswordAsync(userId, request);
            if (apiResponse.Success && apiResponse.Data != null)
            {
                _logger.LogInformation("[REPO] User.ResetPassword completed - UserId={UserId}", userId);
                return ServiceResult<ResetPasswordResponseDto>.Success(apiResponse.Data);
            }

            _logger.LogWarning("[REPO] User.ResetPassword failed - UserId={UserId}, Message={Message}",
                userId, apiResponse.Message);
            return ServiceResult<ResetPasswordResponseDto>.Failure(
                apiResponse.Message ?? "重置密码失败");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[REPO] User.ResetPassword failed - UserId={UserId}", userId);
            return ServiceResult<ResetPasswordResponseDto>.Failure(
                ClientErrorMessageMapper.GetSafeOperationFailureMessage("重置密码", ex));
        }
    }

    public async Task<UserBatchImportResultDto?> BatchImportAsync(UserBatchImportInputDto request)
    {
        try
        {
            _logger.LogInformation("[REPO] User.BatchImport");
            var response = await _apiClient.Users.BatchImportAsync(request);
            _logger.LogInformation("[REPO] User.BatchImport completed");
            return response.Data;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[REPO] User.BatchImport failed");
            return null;
        }
    }

    #endregion

    #region 状态切换、恢复和批量操作

    public async Task<UserDetailDto?> ToggleStatusAsync(Guid id)
    {
        try
        {
            _logger.LogInformation("[REPO] User.ToggleStatus - Id={Id}", id);

            var response = await _apiClient.Users.ToggleStatusAsync(id);
            if (!response.Success || response.Data == null)
            {
                _logger.LogWarning("[REPO] User.ToggleStatus failed: {Message}", response.Message);
                return null;
            }

            _logger.LogInformation("[REPO] User.ToggleStatus completed - Id={Id}, Status={Status}",
                id, response.Data.Status);
            return response.Data;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[REPO] User.ToggleStatus failed - Id={Id}", id);
            return null;
        }
    }

    public async Task<UserDetailDto?> RestoreAsync(Guid id)
    {
        try
        {
            _logger.LogInformation("[REPO] User.Restore - Id={Id}", id);

            var response = await _apiClient.Users.RestoreAsync(id);
            if (!response.Success || response.Data == null)
            {
                _logger.LogWarning("[REPO] User.Restore failed: {Message}", response.Message);
                return null;
            }

            _logger.LogInformation("[REPO] User.Restore completed - Id={Id}", id);
            return response.Data;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[REPO] User.Restore failed - Id={Id}", id);
            return null;
        }
    }

    public async Task<BatchOperationResultDto?> BatchDeleteAsync(List<Guid> ids)
    {
        try
        {
            _logger.LogInformation("[REPO] User.BatchDelete - Count={Count}", ids.Count);

            var response = await _apiClient.Users.BatchDeleteAsync(new BatchDeleteInputDto { Ids = ids });
            if (!response.Success || response.Data == null)
            {
                _logger.LogError("[REPO] User.BatchDelete failed - {Message}", response.Message);
                return null;
            }

            _logger.LogInformation("[REPO] User.BatchDelete completed - Success={Success}, Failure={Failure}",
                response.Data.SuccessCount, response.Data.FailureCount);
            return response.Data;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[REPO] User.BatchDelete failed");
            return null;
        }
    }

    public async Task<BatchOperationResultDto?> BatchEnableAsync(List<Guid> ids)
    {
        try
        {
            _logger.LogInformation("[REPO] User.BatchEnable - Count={Count}", ids.Count);

            var response = await _apiClient.Users.BatchEnableAsync(new BatchDeleteInputDto { Ids = ids });
            if (!response.Success || response.Data == null)
            {
                _logger.LogError("[REPO] User.BatchEnable failed - {Message}", response.Message);
                return null;
            }

            _logger.LogInformation("[REPO] User.BatchEnable completed - Success={Success}, Failure={Failure}",
                response.Data.SuccessCount, response.Data.FailureCount);
            return response.Data;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[REPO] User.BatchEnable failed");
            return null;
        }
    }

    public async Task<BatchOperationResultDto?> BatchDisableAsync(List<Guid> ids)
    {
        try
        {
            _logger.LogInformation("[REPO] User.BatchDisable - Count={Count}", ids.Count);

            var response = await _apiClient.Users.BatchDisableAsync(new BatchDeleteInputDto { Ids = ids });
            if (!response.Success || response.Data == null)
            {
                _logger.LogError("[REPO] User.BatchDisable failed - {Message}", response.Message);
                return null;
            }

            _logger.LogInformation("[REPO] User.BatchDisable completed - Success={Success}, Failure={Failure}",
                response.Data.SuccessCount, response.Data.FailureCount);
            return response.Data;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[REPO] User.BatchDisable failed");
            return null;
        }
    }

    #endregion
}
