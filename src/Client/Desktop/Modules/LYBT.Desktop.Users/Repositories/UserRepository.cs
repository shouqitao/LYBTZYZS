using LYBT.Desktop.Contracts.Api;
using LYBT.Desktop.Contracts.Repositories;
using LYBT.Shared.ExceptionHandling.Mappers;
using LYBT.Shared.Models.Contracts.Auth;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Users.Repositories;

/// <summary>
/// 用户仓储 - 远程模式实现 (SYNC-D02)
/// 通过 Refit IUserApi 访问 WebAPI，不再依赖 IUserDataSource 中间层。
/// DI 工厂根据 IConnectionModeProvider 在远程模式下选择此实现。
/// </summary>
public sealed class UserRepository : IUserRepository
{
    private readonly IUserApi _api;
    private readonly ILogger<UserRepository> _logger;

    public UserRepository(
        IUserApi api,
        ILogger<UserRepository> logger)
    {
        _api = api ?? throw new ArgumentNullException(nameof(api));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    #region 标准 CRUD 操作

    public async Task<PagedResult<UserListDto>> GetPagedAsync(int page = 1, int pageSize = 20, string? keyword = null)
    {
        try
        {
            _logger.LogDebug("[REPO:Remote] User.GetPaged - Page={Page} PageSize={PageSize} Keyword={Keyword}",
                page, pageSize, keyword);

            var response = await _api.GetUsersAsync(page, pageSize, keyword);
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
            _logger.LogError(ex, "[REPO:Remote] User.GetPaged failed");
            throw;
        }
    }

    public async Task<UserDetailDto?> GetByIdAsync(Guid id)
    {
        try
        {
            _logger.LogDebug("[REPO:Remote] User.GetById - Id={Id}", id);

            var response = await _api.GetUserByIdAsync(id);
            return response.Data;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[REPO:Remote] User.GetById failed - Id={Id}", id);
            throw;
        }
    }

    public async Task<UserDetailDto> CreateAsync(UserInputDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        try
        {
            _logger.LogInformation("[REPO:Remote] User.Create - UserName={UserName}", dto.UserName);

            var response = await _api.CreateUserAsync(dto);
            if (!response.Success || response.Data == null)
                throw new InvalidOperationException(response.Message ?? "创建用户失败");

            _logger.LogInformation("[REPO:Remote] User.Create completed - Id={Id}", response.Data.Id);
            return response.Data;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[REPO:Remote] User.Create failed - UserName={UserName}", dto.UserName);
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
            _logger.LogInformation("[REPO:Remote] User.Update - Id={Id}", dto.Id);

            var response = await _api.UpdateUserAsync(dto.Id.Value, dto);
            if (!response.Success || response.Data == null)
                throw new InvalidOperationException(response.Message ?? "更新用户失败");

            _logger.LogInformation("[REPO:Remote] User.Update completed - Id={Id}", response.Data.Id);
            return response.Data;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[REPO:Remote] User.Update failed - Id={Id}", dto.Id);
            throw;
        }
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        try
        {
            _logger.LogInformation("[REPO:Remote] User.Delete - Id={Id}", id);

            var response = await _api.DeleteUserAsync(id);
            if (response.Success)
                _logger.LogInformation("[REPO:Remote] User.Delete completed - Id={Id}", id);
            else
                _logger.LogWarning("[REPO:Remote] User.Delete failed - Id={Id}", id);

            return response.Success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[REPO:Remote] User.Delete failed - Id={Id}", id);
            return false;
        }
    }

    public async Task<List<UserListDto>> SearchAsync(string keyword)
    {
        try
        {
            _logger.LogDebug("[REPO:Remote] User.Search - Keyword={Keyword}", keyword);

            var response = await _api.GetUsersAsync(1, 100, keyword);
            if (response.Data == null)
                return [];

            return response.Data.Items.ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[REPO:Remote] User.Search failed");
            throw;
        }
    }

    #endregion

    #region 用户专用方法

    public async Task<UserDetailDto> GetByUsernameAsync(string username)
    {
        try
        {
            _logger.LogDebug("[REPO:Remote] User.GetByUsername - Username={Username}", username);

            // 远程模式: 通过搜索找到匹配的用户
            var response = await _api.GetUsersAsync(1, 100, username);
            if (response.Data == null)
                throw new InvalidOperationException($"用户 {username} 不存在");

            // 从搜索结果中精确匹配用户名
            var match = response.Data.Items.FirstOrDefault(u =>
                u.UserName.Equals(username, StringComparison.OrdinalIgnoreCase));

            if (match == null)
                throw new InvalidOperationException($"用户 {username} 不存在");

            // 获取完整详情
            var detail = await GetByIdAsync(match.Id);
            return detail ?? throw new InvalidOperationException($"用户 {username} 不存在");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[REPO:Remote] User.GetByUsername failed - Username={Username}", username);
            throw;
        }
    }

    public async Task<List<UserListDto>> GetDoctorsAsync()
    {
        try
        {
            _logger.LogDebug("[REPO:Remote] User.GetDoctors started");

            var response = await _api.GetUsersAsync(1, 100, null);
            if (response.Data?.Items == null)
            {
                _logger.LogWarning("[REPO:Remote] User.GetDoctors -> Empty result");
                return [];
            }

            // 筛选: 角色=医生 && 状态=启用
            var doctors = response.Data.Items
                .Where(u => u.Role == UserRole.Doctor && u.Status == CommonStatus.Enabled)
                .ToList();

            _logger.LogInformation("[REPO:Remote] User.GetDoctors completed - Count={Count}", doctors.Count);
            return doctors;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[REPO:Remote] User.GetDoctors failed");
            return [];
        }
    }

    public async Task<UserDetailDto> ChangeProfileAsync(Guid userId, ChangeProfileDto dto)
    {
        try
        {
            _logger.LogInformation("[REPO:Remote] User.ChangeProfile - UserId={UserId}", userId);

            var response = await _api.ChangeProfileAsync(userId, dto);
            if (response.Success && response.Data != null)
            {
                _logger.LogInformation("[REPO:Remote] User.ChangeProfile completed - UserId={UserId}", userId);
                return response.Data;
            }

            var errorMsg = response.Message ?? "修改个人资料失败";
            _logger.LogWarning("[REPO:Remote] User.ChangeProfile failed - {Message}", errorMsg);
            throw new InvalidOperationException(errorMsg);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[REPO:Remote] User.ChangeProfile failed - UserId={UserId}", userId);
            throw;
        }
    }

    public async Task<ServiceResult> ChangePasswordAsync(Guid userId, ChangePasswordRequest request)
    {
        try
        {
            _logger.LogInformation("[REPO:Remote] User.ChangePassword - UserId={UserId}", userId);

            var response = await _api.ChangePasswordAsync(userId, request);
            if (response.Success)
            {
                _logger.LogInformation("[REPO:Remote] User.ChangePassword completed - UserId={UserId}", userId);
                return ServiceResult.Success();
            }

            var errorMsg = response.Message ?? "修改密码失败";
            _logger.LogWarning("[REPO:Remote] User.ChangePassword failed - {Message}", errorMsg);
            return ServiceResult.Failure(errorMsg);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[REPO:Remote] User.ChangePassword failed - UserId={UserId}", userId);
            return ServiceResult.Failure(ClientErrorMessageMapper.GetSafeOperationFailureMessage("修改密码", ex));
        }
    }

    public async Task<ServiceResult<ResetPasswordResponseDto>> ResetPasswordAsync(
        Guid userId,
        ResetPasswordRequestDto request)
    {
        try
        {
            _logger.LogDebug("[REPO:Remote] User.ResetPassword - UserId={UserId}", userId);

            var apiResponse = await _api.ResetPasswordAsync(userId, request);
            if (apiResponse.Success && apiResponse.Data != null)
            {
                _logger.LogInformation("[REPO:Remote] User.ResetPassword completed - UserId={UserId}", userId);
                return ServiceResult<ResetPasswordResponseDto>.Success(apiResponse.Data);
            }

            _logger.LogWarning("[REPO:Remote] User.ResetPassword failed - UserId={UserId}, Message={Message}",
                userId, apiResponse.Message);
            return ServiceResult<ResetPasswordResponseDto>.Failure(
                apiResponse.Message ?? "重置密码失败");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[REPO:Remote] User.ResetPassword failed - UserId={UserId}", userId);
            return ServiceResult<ResetPasswordResponseDto>.Failure(
                ClientErrorMessageMapper.GetSafeOperationFailureMessage("重置密码", ex));
        }
    }

    public async Task<UserBatchImportResultDto?> BatchImportAsync(UserBatchImportInputDto request)
    {
        try
        {
            _logger.LogInformation("[REPO:Remote] User.BatchImport");
            var response = await _api.BatchImportAsync(request);
            _logger.LogInformation("[REPO:Remote] User.BatchImport completed");
            return response.Data;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[REPO:Remote] User.BatchImport failed");
            return null;
        }
    }

    #endregion

    #region 状态切换、恢复和批量操作

    public async Task<UserDetailDto?> ToggleStatusAsync(Guid id)
    {
        try
        {
            _logger.LogInformation("[REPO:Remote] User.ToggleStatus - Id={Id}", id);

            var response = await _api.ToggleStatusAsync(id);
            if (!response.Success || response.Data == null)
            {
                _logger.LogWarning("[REPO:Remote] User.ToggleStatus failed: {Message}", response.Message);
                return null;
            }

            _logger.LogInformation("[REPO:Remote] User.ToggleStatus completed - Id={Id}, Status={Status}",
                id, response.Data.Status);
            return response.Data;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[REPO:Remote] User.ToggleStatus failed - Id={Id}", id);
            return null;
        }
    }

    public async Task<UserDetailDto?> RestoreAsync(Guid id)
    {
        try
        {
            _logger.LogInformation("[REPO:Remote] User.Restore - Id={Id}", id);

            var response = await _api.RestoreAsync(id);
            if (!response.Success || response.Data == null)
            {
                _logger.LogWarning("[REPO:Remote] User.Restore failed: {Message}", response.Message);
                return null;
            }

            _logger.LogInformation("[REPO:Remote] User.Restore completed - Id={Id}", id);
            return response.Data;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[REPO:Remote] User.Restore failed - Id={Id}", id);
            return null;
        }
    }

    public async Task<BatchOperationResultDto?> BatchDeleteAsync(List<Guid> ids)
    {
        try
        {
            _logger.LogInformation("[REPO:Remote] User.BatchDelete - Count={Count}", ids.Count);

            var response = await _api.BatchDeleteAsync(new BatchDeleteInputDto { Ids = ids });
            if (!response.Success || response.Data == null)
            {
                _logger.LogError("[REPO:Remote] User.BatchDelete failed - {Message}", response.Message);
                return null;
            }

            _logger.LogInformation("[REPO:Remote] User.BatchDelete completed - Success={Success}, Failure={Failure}",
                response.Data.SuccessCount, response.Data.FailureCount);
            return response.Data;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[REPO:Remote] User.BatchDelete failed");
            return null;
        }
    }

    public async Task<BatchOperationResultDto?> BatchEnableAsync(List<Guid> ids)
    {
        try
        {
            _logger.LogInformation("[REPO:Remote] User.BatchEnable - Count={Count}", ids.Count);

            var response = await _api.BatchEnableAsync(new BatchDeleteInputDto { Ids = ids });
            if (!response.Success || response.Data == null)
            {
                _logger.LogError("[REPO:Remote] User.BatchEnable failed - {Message}", response.Message);
                return null;
            }

            _logger.LogInformation("[REPO:Remote] User.BatchEnable completed - Success={Success}, Failure={Failure}",
                response.Data.SuccessCount, response.Data.FailureCount);
            return response.Data;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[REPO:Remote] User.BatchEnable failed");
            return null;
        }
    }

    public async Task<BatchOperationResultDto?> BatchDisableAsync(List<Guid> ids)
    {
        try
        {
            _logger.LogInformation("[REPO:Remote] User.BatchDisable - Count={Count}", ids.Count);

            var response = await _api.BatchDisableAsync(new BatchDeleteInputDto { Ids = ids });
            if (!response.Success || response.Data == null)
            {
                _logger.LogError("[REPO:Remote] User.BatchDisable failed - {Message}", response.Message);
                return null;
            }

            _logger.LogInformation("[REPO:Remote] User.BatchDisable completed - Success={Success}, Failure={Failure}",
                response.Data.SuccessCount, response.Data.FailureCount);
            return response.Data;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[REPO:Remote] User.BatchDisable failed");
            return null;
        }
    }

    #endregion
}
