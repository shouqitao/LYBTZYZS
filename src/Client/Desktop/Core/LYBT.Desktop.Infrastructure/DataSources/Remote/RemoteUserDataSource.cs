using LYBT.Desktop.Contracts.Api;
using LYBT.Desktop.Contracts.DataSources;
using LYBT.Desktop.Infrastructure.DataSources.Mappers;
using LYBT.Entities.Users;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Contracts.Auth;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Infrastructure.DataSources.Remote;

/// <summary>
/// 远程用户数据源 - 通过 API 访问服务端
/// OpenSpec: implement-local-mode
/// </summary>
public class RemoteUserDataSource : IUserDataSource
{
    private readonly IUserApi _api;
    private readonly ILogger<RemoteUserDataSource> _logger;
    private readonly UserDataSourceMapper _mapper = new();

    public RemoteUserDataSource(IUserApi api, ILogger<RemoteUserDataSource> logger)
    {
        _api = api;
        _logger = logger;
    }

    public async Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default)
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
            return _mapper.ToEntity(response.Data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[RemoteDataSource] User.GetById failed - Id={Id}", id);
            throw;
        }
    }

    public async Task<User> CreateAsync(User entity, CancellationToken ct = default)
    {
        _logger.LogInformation("[RemoteDataSource] User.Create - Username={Username}", entity.UserName);

        try
        {
            var inputDto = _mapper.ToInputDto(entity);
            var response = await _api.CreateUserAsync(inputDto);

            if (!response.Success || response.Data == null)
            {
                throw new InvalidOperationException(response.Message ?? "创建用户失败");
            }

            return _mapper.ToEntity(response.Data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[RemoteDataSource] User.Create failed");
            throw;
        }
    }

    public async Task<User> UpdateAsync(User entity, CancellationToken ct = default)
    {
        _logger.LogInformation("[RemoteDataSource] User.Update - Id={Id}", entity.Id);

        try
        {
            var inputDto = _mapper.ToInputDto(entity);
            inputDto.Id = entity.Id;
            var response = await _api.UpdateUserAsync(entity.Id, inputDto);

            if (!response.Success || response.Data == null)
            {
                throw new InvalidOperationException(response.Message ?? "更新用户失败");
            }

            return _mapper.ToEntity(response.Data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[RemoteDataSource] User.Update failed - Id={Id}", entity.Id);
            throw;
        }
    }

    public async Task<(List<User> Items, int Total)> GetPagedAsync(
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
                return (new List<User>(), 0);
            }

            var items = response.Data.Items.Select(_mapper.ToEntity).ToList();
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

    public async Task<User?> GetByUsernameAsync(string username, CancellationToken ct = default)
    {
        _logger.LogDebug("[RemoteDataSource] User.GetByUsername - Username={Username}", username);

        try
        {
            // 通过搜索获取用户
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

    public async Task<bool> UpdateLastLoginTimeAsync(Guid id, CancellationToken ct = default)
    {
        _logger.LogDebug("[RemoteDataSource] User.UpdateLastLoginTime - Id={Id}", id);

        // Remote模式下，登录时间由服务端自动更新
        // 此方法仅用于Local模式
        return true;
    }

    public async Task<bool> ResetFailedLoginCountAsync(Guid id, CancellationToken ct = default)
    {
        _logger.LogDebug("[RemoteDataSource] User.ResetFailedLoginCount - Id={Id}", id);

        // Remote模式下，失败次数由服务端管理
        // 此方法仅用于Local模式
        return true;
    }

    public async Task<int> IncrementFailedLoginCountAsync(Guid id, CancellationToken ct = default)
    {
        _logger.LogDebug("[RemoteDataSource] User.IncrementFailedLoginCount - Id={Id}", id);

        // Remote模式下，失败次数由服务端管理
        // 此方法仅用于Local模式
        return 0;
    }

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
}
