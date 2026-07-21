using LYBT.Desktop.Contracts.ApiClient;
using LYBT.Desktop.Contracts.Repositories;
using LYBT.Desktop.Foundation.Repositories;
using LYBT.Desktop.Foundation.ExceptionHandling;
using LYBT.Shared.Models.Contracts.Auth;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Users.Repositories;

/// <summary>
/// 用户仓储 — routes all calls through IApiClient.
/// </summary>
public sealed class UserRepository : ApiClientRepositoryBase<UserListDto, UserDetailDto, UserInputDto, UserInputDto>, IUserRepository
{
    private readonly IApiClient _apiClient;

    public UserRepository(
        IApiClient apiClient,
        ILogger<UserRepository> logger)
        : base(logger)
    {
        _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
    }

    protected override string LogPrefix => "User";

    #region 标准 CRUD 操作

    public async Task<PagedResult<UserListDto>> GetPagedAsync(int page = 1, int pageSize = 20, string? keyword = null, CancellationToken ct = default)
    {
        return await ExecuteAsync(
            async () =>
            {
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
            },
            "GetPaged",
            "[REPO] User.GetPaged - Page={Page} PageSize={PageSize} Keyword={Keyword}",
            [page, pageSize, keyword]);
    }

    public async Task<UserDetailDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await ExecuteAsync(
            async () =>
            {
                var response = await _apiClient.Users.GetUserByIdAsync(id);
                return response.Data;
            },
            "GetById");
    }

    public async Task<UserDetailDto> CreateAsync(UserInputDto dto, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(dto);

        return await ExecuteAsync(
            async () =>
            {
                var response = await _apiClient.Users.CreateUserAsync(dto);
                if (!response.Success || response.Data == null)
                    throw new InvalidOperationException(response.Message ?? "创建用户失败");

                Logger.LogInformation("[REPO] User.Create completed - Id={Id}", response.Data.Id);
                return response.Data;
            },
            "Create",
            LogLevel.Information);
    }

    public async Task<UserDetailDto> UpdateAsync(UserInputDto dto, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(dto);
        if (dto.Id is null || dto.Id == Guid.Empty)
            throw new ArgumentException("更新DTO必须包含有效的ID", nameof(dto));

        return await ExecuteAsync(
            async () =>
            {
                var response = await _apiClient.Users.UpdateUserAsync(dto.Id.Value, dto);
                if (!response.Success || response.Data == null)
                    throw new InvalidOperationException(response.Message ?? "更新用户失败");

                Logger.LogInformation("[REPO] User.Update completed - Id={Id}", response.Data.Id);
                return response.Data;
            },
            "Update",
            LogLevel.Information);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        await ExecuteAsync(
            async () =>
            {
                var response = await _apiClient.Users.DeleteUserAsync(id);
                if (!response.Success)
                    throw new InvalidOperationException(response.Message ?? "删除用户失败");

                Logger.LogInformation("[REPO] User.Delete completed - Id={Id}", id);
            },
            "Delete",
            LogLevel.Information);
    }

    public async Task<List<UserListDto>> SearchAsync(string keyword, CancellationToken ct = default)
    {
        return await ExecuteAsync(
            async () =>
            {
                var response = await _apiClient.Users.GetUsersAsync(1, 100, keyword);
                if (response.Data == null)
                    return [];

                return response.Data.Items.ToList();
            },
            "Search");
    }

    #endregion

    #region 用户专用方法

    public async Task<UserDetailDto> GetByUsernameAsync(string username, CancellationToken ct = default)
    {
        return await ExecuteAsync(
            async () =>
            {
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
                var detail = await GetByIdAsync(remoteMatch.Id, ct);
                return detail ?? throw new InvalidOperationException($"用户 {username} 不存在");
            },
            "GetByUsername");
    }

    public async Task<List<UserListDto>> GetDoctorsAsync(CancellationToken ct = default)
    {
        // Returns empty list on exception instead of rethrowing — keep manual try/catch.
        try
        {
            Logger.LogDebug("[REPO] User.GetDoctors started");

            var response = await _apiClient.Users.GetUsersAsync(1, 100, null);
            if (response.Data?.Items == null)
            {
                Logger.LogWarning("[REPO] User.GetDoctors -> Empty result");
                return [];
            }

            // 筛选: 角色=医生 && 状态=启用
            var remoteDoctors = response.Data.Items
                .Where(u => u.Role == UserRole.Doctor && u.Status == CommonStatus.Enabled)
                .ToList();

            Logger.LogInformation("[REPO] User.GetDoctors completed - Count={Count}", remoteDoctors.Count);
            return remoteDoctors;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "[REPO] User.GetDoctors failed");
            return [];
        }
    }

    public async Task<UserDetailDto> ChangeProfileAsync(Guid userId, ChangeProfileDto dto, CancellationToken ct = default)
    {
        return await ExecuteAsync(
            async () =>
            {
                var response = await _apiClient.Users.ChangeProfileAsync(userId, dto);
                if (response.Success && response.Data != null)
                {
                    Logger.LogInformation("[REPO] User.ChangeProfile completed - UserId={UserId}", userId);
                    return response.Data;
                }

                var errorMsg = response.Message ?? "修改个人资料失败";
                Logger.LogWarning("[REPO] User.ChangeProfile failed - {Message}", errorMsg);
                throw new InvalidOperationException(errorMsg);
            },
            "ChangeProfile",
            LogLevel.Information);
    }

    public async Task<Result> ChangePasswordAsync(Guid userId, ChangePasswordRequest request, CancellationToken ct = default)
    {
        // Returns Result.Failure on exception instead of rethrowing — keep manual try/catch.
        try
        {
            Logger.LogInformation("[REPO] User.ChangePassword - UserId={UserId}", userId);

            var response = await _apiClient.Users.ChangePasswordAsync(userId, request);
            if (response.Success)
            {
                Logger.LogInformation("[REPO] User.ChangePassword completed - UserId={UserId}", userId);
                return Result.Success();
            }

            var errorMsg = response.Message ?? "修改密码失败";
            Logger.LogWarning("[REPO] User.ChangePassword failed - {Message}", errorMsg);
            return Result.Failure(errorMsg);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "[REPO] User.ChangePassword failed - UserId={UserId}", userId);
            return Result.Failure(ClientErrorMessageMapper.GetSafeOperationFailureMessage("修改密码", ex));
        }
    }

    public async Task<Result<ResetPasswordResponseDto>> ResetPasswordAsync(
        Guid userId,
        ResetPasswordRequestDto request,
        CancellationToken ct = default)
    {
        // Returns Result.Failure on exception instead of rethrowing — keep manual try/catch.
        try
        {
            Logger.LogDebug("[REPO] User.ResetPassword - UserId={UserId}", userId);

            var apiResponse = await _apiClient.Users.ResetPasswordAsync(userId, request);
            if (apiResponse.Success && apiResponse.Data != null)
            {
                Logger.LogInformation("[REPO] User.ResetPassword completed - UserId={UserId}", userId);
                return Result<ResetPasswordResponseDto>.Success(apiResponse.Data);
            }

            Logger.LogWarning("[REPO] User.ResetPassword failed - UserId={UserId}, Message={Message}",
                userId, apiResponse.Message);
            return Result<ResetPasswordResponseDto>.Failure(
                apiResponse.Message ?? "重置密码失败");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "[REPO] User.ResetPassword failed - UserId={UserId}", userId);
            return Result<ResetPasswordResponseDto>.Failure(
                ClientErrorMessageMapper.GetSafeOperationFailureMessage("重置密码", ex));
        }
    }

    #endregion

    #region 状态切换、恢复和批量操作

    public async Task<UserDetailDto?> ToggleStatusAsync(Guid id, CancellationToken ct = default)
    {
        return await ExecuteAsync(
            async () =>
            {
                var response = await _apiClient.Users.ToggleStatusAsync(id);
                if (!response.Success || response.Data == null)
                    throw new InvalidOperationException(response.Message ?? "切换用户状态失败");

                Logger.LogInformation("[REPO] User.ToggleStatus completed - Id={Id}, Status={Status}",
                    id, response.Data.Status);
                return response.Data;
            },
            "ToggleStatus",
            LogLevel.Information);
    }

    public async Task<BatchOperationResultDto?> BatchDeleteAsync(List<Guid> ids, CancellationToken ct = default)
    {
        // Returns failure DTO on exception instead of rethrowing — keep manual try/catch.
        try
        {
            Logger.LogInformation("[REPO] User.BatchDelete - Count={Count}", ids.Count);

            var response = await _apiClient.Users.BatchDeleteAsync(new BatchDeleteInputDto { Ids = ids });
            if (!response.Success || response.Data == null)
            {
                return new BatchOperationResultDto
                {
                    TotalCount = ids.Count,
                    FailureCount = ids.Count,
                    IsSuccess = false,
                    Message = response.Message ?? "批量删除失败"
                };
            }

            return response.Data;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "[REPO] User.BatchDelete failed");
            return new BatchOperationResultDto
            {
                TotalCount = ids.Count,
                FailureCount = ids.Count,
                IsSuccess = false,
                Message = ex.Message
            };
        }
    }

    #endregion
}
