using LYBT.Desktop.Contracts.ApiClient;
using LYBT.Desktop.Contracts.Repositories;
using LYBT.Desktop.Contracts.Results;
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
public sealed class UserRepository : EntityApiClientRepositoryBase<UserListDto, UserDetailDto, UserInputDto>, IUserRepository
{
    private readonly IApiClientIdentity _identity;

    public UserRepository(
        IApiClientIdentity identity,
        ILogger<UserRepository> logger)
        : base(logger, identity)
    {
        _identity = identity ?? throw new ArgumentNullException(nameof(identity));
    }

    protected override string LogPrefix => "User";

    /// <summary>
    /// 分页查询用户列表（接口无 category 参数，转发基类标准实现）。
    /// </summary>
    public Task<PagedResult<UserListDto>> GetPagedAsync(int page = 1, int pageSize = 20, string? keyword = null, CancellationToken ct = default)
        => base.GetPagedAsync(page, pageSize, keyword, null, ct);

    #region 搜索

    public async Task<List<UserListDto>> SearchAsync(string keyword, CancellationToken ct = default)
    {
        return await ExecuteAsync(
            async () =>
            {
                var response = await _identity.GetUsersAsync(1, 100, keyword);
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
                var response = await _identity.GetUsersAsync(1, 100, username);
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

            var response = await _identity.GetUsersAsync(1, 100, null);
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
                var response = await _identity.ChangeProfileAsync(userId, dto);
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

    public async Task<CommandResult> ChangePasswordAsync(Guid userId, ChangePasswordRequest request, CancellationToken ct = default)
    {
        // Returns CommandResult.Failed on exception instead of rethrowing — keep manual try/catch.
        try
        {
            Logger.LogInformation("[REPO] User.ChangePassword - UserId={UserId}", userId);

            var response = await _identity.ChangePasswordAsync(userId, request);
            if (response.Success)
            {
                Logger.LogInformation("[REPO] User.ChangePassword completed - UserId={UserId}", userId);
                return CommandResult.Succeeded();
            }

            var errorMsg = response.Message ?? "修改密码失败";
            Logger.LogWarning("[REPO] User.ChangePassword failed - {Message}", errorMsg);
            return CommandResult.Failed(errorMsg);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "[REPO] User.ChangePassword failed - UserId={UserId}", userId);
            return CommandResult.Failed(ClientErrorMessageMapper.GetSafeOperationFailureMessage("修改密码", ex));
        }
    }

    public async Task<CommandResult<ResetPasswordResponseDto>> ResetPasswordAsync(
        Guid userId,
        ResetPasswordRequest request,
        CancellationToken ct = default)
    {
        // Returns CommandResult.Failed on exception instead of rethrowing — keep manual try/catch.
        try
        {
            Logger.LogDebug("[REPO] User.ResetPassword - UserId={UserId}", userId);

            var apiResponse = await _identity.ResetPasswordAsync(userId, request);
            if (apiResponse.Success && apiResponse.Data != null)
            {
                Logger.LogInformation("[REPO] User.ResetPassword completed - UserId={UserId}", userId);
                return CommandResult<ResetPasswordResponseDto>.Succeeded(apiResponse.Data);
            }

            Logger.LogWarning("[REPO] User.ResetPassword failed - UserId={UserId}, Message={Message}",
                userId, apiResponse.Message);
            return CommandResult<ResetPasswordResponseDto>.Failed(
                apiResponse.Message ?? "重置密码失败");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "[REPO] User.ResetPassword failed - UserId={UserId}", userId);
            return CommandResult<ResetPasswordResponseDto>.Failed(
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
                var response = await _identity.ToggleStatusAsync(id);
                if (!response.Success || response.Data == null)
                    throw new InvalidOperationException(response.Message ?? "切换用户状态失败");

                Logger.LogInformation("[REPO] User.ToggleStatus completed - Id={Id}, Status={Status}",
                    id, response.Data.Status);
                return response.Data;
            },
            "ToggleStatus",
            LogLevel.Information);
    }

    public async Task<UserDetailDto?> RestoreAsync(Guid id, CancellationToken ct = default)
    {
        return await ExecuteAsync(
            async () =>
            {
                var response = await _identity.RestoreAsync(id);
                if (!response.Success || response.Data == null)
                    throw new InvalidOperationException(response.Message ?? "恢复用户失败");

                Logger.LogInformation("[REPO] User.Restore completed - Id={Id}", id);
                return response.Data;
            },
            "Restore",
            LogLevel.Information);
    }

    public async Task<BatchOperationResultDto?> BatchDeleteAsync(List<Guid> ids, CancellationToken ct = default)
    {
        return await ExecuteBatchDeleteAsync(
            () => _identity.BatchDeleteAsync(new BatchDeleteInputDto { Ids = ids }),
            "BatchDelete",
            "批量删除失败",
            ids.Count);
    }

    #endregion
}
