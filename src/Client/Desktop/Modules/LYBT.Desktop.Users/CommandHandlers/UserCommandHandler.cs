using LYBT.Desktop.Contracts.CommandHandlers;
using LYBT.Desktop.Users.Interfaces;
using LYBT.Shared.Models.Contracts.Users;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Users.CommandHandlers;

/// <summary>
/// 用户CommandHandler实现
/// OpenSpec: unify-desktop-architecture (Phase 2.6)
/// 封装IUserRepository，提供统一的CRUD操作和错误处理
/// </summary>
public class UserCommandHandler : IUserCommandHandler
{
    private readonly IUserRepository _repository;
    private readonly ILogger<UserCommandHandler> _logger;

    public UserCommandHandler(
        IUserRepository repository,
        ILogger<UserCommandHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<CommandResult<List<UserListDto>>> GetListAsync(QueryParams? query = null)
    {
        try
        {
            var result = await _repository.GetPagedAsync(
                query?.Page ?? 1,
                query?.PageSize ?? 20,
                query?.SearchText);
            return CommandResult<List<UserListDto>>.Succeeded(result.Items.ToList());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取用户列表失败");
            return CommandResult<List<UserListDto>>.Failed($"获取用户列表失败: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task<CommandResult<UserDetailDto>> GetDetailAsync(Guid id)
    {
        try
        {
            var result = await _repository.GetByIdAsync(id);
            if (result == null)
            {
                return CommandResult<UserDetailDto>.NotFound($"未找到ID为 {id} 的用户");
            }
            return CommandResult<UserDetailDto>.Succeeded(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取用户详情失败: {UserId}", id);
            return CommandResult<UserDetailDto>.Failed($"获取用户详情失败: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task<CommandResult<UserDetailDto>> SaveAsync(UserInputDto input)
    {
        try
        {
            UserDetailDto result;
            if (input.Id == Guid.Empty)
            {
                result = await _repository.CreateAsync(input);
                _logger.LogInformation("创建用户成功: {UserId}", result.Id);
            }
            else
            {
                result = await _repository.UpdateAsync(input);
                _logger.LogInformation("更新用户成功: {UserId}", result.Id);
            }
            return CommandResult<UserDetailDto>.Succeeded(result);
        }
        catch (Exception ex)
        {
            var operation = input.Id == Guid.Empty ? "创建" : "更新";
            _logger.LogError(ex, "{Operation}用户失败", operation);
            return CommandResult<UserDetailDto>.Failed($"{operation}用户失败: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task<CommandResult<bool>> DeleteAsync(Guid id)
    {
        try
        {
            var success = await _repository.DeleteAsync(id);
            if (success)
            {
                _logger.LogInformation("删除用户成功: {UserId}", id);
                return CommandResult<bool>.Succeeded(true);
            }
            return CommandResult<bool>.Failed("删除用户失败");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "删除用户失败: {UserId}", id);
            return CommandResult<bool>.Failed($"删除用户失败: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task<CommandResult<List<UserListDto>>> SearchByUsernameAsync(string username)
    {
        try
        {
            var result = await _repository.SearchAsync(username);
            return CommandResult<List<UserListDto>>.Succeeded(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "按用户名搜索用户失败: {Username}", username);
            return CommandResult<List<UserListDto>>.Failed($"搜索用户失败: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task<CommandResult<bool>> ResetPasswordAsync(Guid id, string newPassword)
    {
        try
        {
            var request = new ResetPasswordRequestDto { MustChangeOnNextLogin = true };
            var result = await _repository.ResetPasswordAsync(id, request);
            if (result.IsSuccess)
            {
                _logger.LogInformation("重置用户密码成功: {UserId}", id);
                return CommandResult<bool>.Succeeded(true);
            }
            return CommandResult<bool>.Failed(result.Message ?? "重置密码失败");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "重置用户密码失败: {UserId}", id);
            return CommandResult<bool>.Failed($"重置密码失败: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task<CommandResult<bool>> SetActiveStatusAsync(Guid id, bool isActive)
    {
        try
        {
            var result = await _repository.ToggleStatusAsync(id);
            if (result != null)
            {
                _logger.LogInformation("设置用户状态成功: {UserId}, IsActive: {IsActive}", id, isActive);
                return CommandResult<bool>.Succeeded(true);
            }
            return CommandResult<bool>.Failed("设置用户状态失败");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "设置用户状态失败: {UserId}", id);
            return CommandResult<bool>.Failed($"设置用户状态失败: {ex.Message}");
        }
    }
}
