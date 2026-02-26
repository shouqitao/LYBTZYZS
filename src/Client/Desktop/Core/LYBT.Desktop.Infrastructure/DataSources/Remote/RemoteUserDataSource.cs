using LYBT.Desktop.Contracts.Api;
using LYBT.Desktop.Contracts.DataSources;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Contracts.Auth;
using Microsoft.Extensions.Logging;
using Riok.Mapperly.Abstractions;

namespace LYBT.Desktop.Infrastructure.DataSources.Remote;

/// <summary>
/// 远程用户数据源 - 通过 API 访问服务端
/// </summary>
public class RemoteUserDataSource : IUserDataSource
{
    private readonly IUserApi _api;
    private readonly ILogger<RemoteUserDataSource> _logger;
    private readonly UserListToDetailMapper _listMapper = new();

    public RemoteUserDataSource(IUserApi api, ILogger<RemoteUserDataSource> logger)
    {
        _api = api;
        _logger = logger;
    }

    public async Task<UserDetailDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        _logger.LogDebug("[RemoteDataSource] User.GetById - Id={Id}", id);

        try
        {
            var response = await _api.GetUserByIdAsync(id);
            if (response.Data == null)
            {
                _logger.LogWarning("[RemoteDataSource] User.GetById - NotFound: {Id}", id);
                return null;
            }
            return response.Data;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[RemoteDataSource] User.GetById failed - Id={Id}", id);
            throw;
        }
    }

    public async Task<UserDetailDto> CreateAsync(UserInputDto input, CancellationToken ct = default)
    {
        _logger.LogInformation("[RemoteDataSource] User.Create - Username={Username}", input.UserName);

        try
        {
            var response = await _api.CreateUserAsync(input);

            if (!response.Success || response.Data == null)
            {
                throw new InvalidOperationException(response.Message ?? "创建用户失败");
            }

            return response.Data;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[RemoteDataSource] User.Create failed");
            throw;
        }
    }

    public async Task<UserDetailDto> UpdateAsync(UserInputDto input, CancellationToken ct = default)
    {
        _logger.LogInformation("[RemoteDataSource] User.Update - Id={Id}", input.Id);

        try
        {
            var response = await _api.UpdateUserAsync(input.Id!.Value, input);

            if (!response.Success || response.Data == null)
            {
                throw new InvalidOperationException(response.Message ?? "更新用户失败");
            }

            return response.Data;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[RemoteDataSource] User.Update failed - Id={Id}", input.Id);
            throw;
        }
    }

    public async Task<(List<UserDetailDto> Items, int Total)> GetPagedAsync(
        int page,
        int pageSize,
        string? keyword = null,
        CancellationToken ct = default)
    {
        _logger.LogDebug("[RemoteDataSource] User.GetPaged - Page={Page}", page);

        try
        {
            var response = await _api.GetUsersAsync(page, pageSize, keyword);
            if (response.Data == null)
            {
                return (new List<UserDetailDto>(), 0);
            }

            var items = response.Data.Items.Select(_listMapper.ToDetailDto).ToList();
            return (items, response.Data.TotalCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[RemoteDataSource] User.GetPaged failed");
            throw;
        }
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        _logger.LogInformation("[RemoteDataSource] User.Delete - Id={Id}", id);

        try
        {
            var response = await _api.DeleteUserAsync(id);
            return response.Success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[RemoteDataSource] User.Delete failed - Id={Id}", id);
            return false;
        }
    }

    public async Task<UserDetailDto?> GetByUsernameAsync(string username, CancellationToken ct = default)
    {
        _logger.LogDebug("[RemoteDataSource] User.GetByUsername - Username={Username}", username);

        try
        {
            var response = await _api.GetUsersAsync(1, 100, username);
            if (response.Data == null)
            {
                return null;
            }

            // 精确匹配用户名
            var user = response.Data.Items
                .FirstOrDefault(u => u.UserName.Equals(username, StringComparison.OrdinalIgnoreCase));

            if (user == null)
            {
                return null;
            }

            // 获取完整详情
            return await GetByIdAsync(user.Id, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[RemoteDataSource] User.GetByUsername failed");
            return null;
        }
    }

    public async Task<bool> ChangePasswordAsync(Guid id, string oldPasswordHash, string newPasswordHash, CancellationToken ct = default)
    {
        _logger.LogInformation("[RemoteDataSource] User.ChangePassword - Id={Id}", id);

        try
        {
            var request = new ChangePasswordRequest
            {
                OldPassword = oldPasswordHash,
                NewPassword = newPasswordHash
            };
            var response = await _api.ChangePasswordAsync(id, request);
            return response.Success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[RemoteDataSource] User.ChangePassword failed - Id={Id}", id);
            return false;
        }
    }

    public async Task<bool> ToggleStatusAsync(Guid id, CancellationToken ct = default)
    {
        _logger.LogInformation("[RemoteDataSource] User.ToggleStatus - Id={Id}", id);

        try
        {
            var response = await _api.ToggleStatusAsync(id);
            return response.Success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[RemoteDataSource] User.ToggleStatus failed - Id={Id}", id);
            return false;
        }
    }

    public Task<bool> UpdateLastLoginTimeAsync(Guid id, CancellationToken ct = default)
    {
        _logger.LogDebug("[RemoteDataSource] User.UpdateLastLoginTime - Id={Id}", id);

        // Remote模式下，登录时间由服务端自动更新
        return Task.FromResult(true);
    }

    public Task<bool> ResetFailedLoginCountAsync(Guid id, CancellationToken ct = default)
    {
        _logger.LogDebug("[RemoteDataSource] User.ResetFailedLoginCount - Id={Id}", id);

        // Remote模式下，失败次数由服务端管理
        return Task.FromResult(true);
    }

    public Task<int> IncrementFailedLoginCountAsync(Guid id, CancellationToken ct = default)
    {
        _logger.LogDebug("[RemoteDataSource] User.IncrementFailedLoginCount - Id={Id}", id);

        // Remote模式下，失败次数由服务端管理
        return Task.FromResult(0);
    }

    // ==================== Sprint 4 X2 扩展方法 ====================
    // OpenSpec: SYNC-D02 - 过渡态方法

    public async Task<BatchOperationResultDto> BatchDeleteAsync(List<Guid> ids, CancellationToken ct = default)
    {
        _logger.LogInformation("[RemoteDataSource] User.BatchDelete - Count={Count}", ids.Count);

        try
        {
            var response = await _api.BatchDeleteAsync(new BatchDeleteInputDto { Ids = ids });
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
            _logger.LogError(ex, "[RemoteDataSource] User.BatchDelete failed");
            return new BatchOperationResultDto
            {
                TotalCount = ids.Count,
                FailureCount = ids.Count,
                IsSuccess = false,
                Message = ex.Message
            };
        }
    }

    /// <summary>T4-X2-03: 恢复已删除的用户</summary>
    public async Task<UserDetailDto?> RestoreAsync(Guid id, CancellationToken ct = default)
    {
        _logger.LogInformation("[RemoteDataSource] User.Restore - Id={Id}", id);

        try
        {
            var response = await _api.RestoreAsync(id);
            return response.Success ? response.Data : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[RemoteDataSource] User.Restore failed - Id={Id}", id);
            return null;
        }
    }

    /// <summary>T4-X2-05: 管理员重置用户密码</summary>
    public async Task<ResetPasswordResponseDto> ResetPasswordAsync(Guid id, CancellationToken ct = default)
    {
        _logger.LogInformation("[RemoteDataSource] User.ResetPassword - Id={Id}", id);

        try
        {
            var response = await _api.ResetPasswordAsync(id, new ResetPasswordRequestDto());
            return response.Data ?? new ResetPasswordResponseDto { Success = false };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[RemoteDataSource] User.ResetPassword failed - Id={Id}", id);
            return new ResetPasswordResponseDto { Success = false };
        }
    }

    /// <summary>T4-X2-07: 批量切换用户状态</summary>
    public async Task<BatchOperationResultDto> BatchToggleStatusAsync(List<Guid> ids, bool enable, CancellationToken ct = default)
    {
        _logger.LogInformation("[RemoteDataSource] User.BatchToggleStatus - Count={Count}, Enable={Enable}", ids.Count, enable);

        try
        {
            var input = new BatchDeleteInputDto { Ids = ids };
            var response = enable
                ? await _api.BatchEnableAsync(input)
                : await _api.BatchDisableAsync(input);

            if (!response.Success || response.Data == null)
            {
                return new BatchOperationResultDto
                {
                    TotalCount = ids.Count,
                    FailureCount = ids.Count,
                    IsSuccess = false,
                    Message = response.Message ?? $"批量{(enable ? "启用" : "禁用")}失败"
                };
            }

            return response.Data;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[RemoteDataSource] User.BatchToggleStatus failed");
            return new BatchOperationResultDto
            {
                TotalCount = ids.Count,
                FailureCount = ids.Count,
                IsSuccess = false,
                Message = ex.Message
            };
        }
    }

    /// <summary>T4-X2-08: 获取当前用户 (从服务端 session 获取)</summary>
    public async Task<UserDetailDto?> GetCurrentUserAsync(CancellationToken ct = default)
    {
        _logger.LogDebug("[RemoteDataSource] User.GetCurrentUser");

        // 远程模式: 复用 GetUsersAsync 搜索或由调用方从 SessionManager 获取
        // 此处返回 null，实际场景由上层 AuthenticationService 管理当前用户
        return await Task.FromResult<UserDetailDto?>(null);
    }
}

/// <summary>
/// UserListDto -> UserDetailDto 映射器 (仅限 DTO 间转换, 无 Entity 依赖)
/// </summary>
[Mapper]
internal partial class UserListToDetailMapper
{
    [MapperIgnoreTarget(nameof(UserDetailDto.Email))]
    [MapperIgnoreTarget(nameof(UserDetailDto.PinYinCode))]
    [MapperIgnoreTarget(nameof(UserDetailDto.FailedLoginCount))]
    [MapperIgnoreTarget(nameof(UserDetailDto.UpdatedAt))]
    [MapperIgnoreTarget(nameof(UserDetailDto.Remark))]
    public partial UserDetailDto ToDetailDto(UserListDto listDto);
}
