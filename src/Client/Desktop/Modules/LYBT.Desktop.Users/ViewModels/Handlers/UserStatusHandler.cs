using LYBT.Desktop.Infrastructure.Services;
using LYBT.Desktop.Users.Interfaces;
using LYBT.Desktop.Users.Models;
using LYBT.Desktop.Users.ViewModels.Components;
using LYBT.Shared.Models.Contracts.Users;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Users.ViewModels.Handlers;

/// <summary>
/// 用户状态处理实现
/// OpenSpec: refactor-frontend-srp-patterns - Handler提取模式
/// </summary>
public class UserStatusHandler : IUserStatusHandler
{
    private readonly UserService _userService;
    private readonly IUserRepository _userRepository;
    private readonly IMasterDetailServices<UserListDto, UserDetailModel> _masterDetailServices;
    private readonly ILogger<UserStatusHandler> _logger;

    public UserStatusHandler(
        UserService userService,
        IUserRepository userRepository,
        IMasterDetailServices<UserListDto, UserDetailModel> masterDetailServices,
        ILogger<UserStatusHandler> logger)
    {
        _userService = userService ?? throw new ArgumentNullException(nameof(userService));
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _masterDetailServices = masterDetailServices ?? throw new ArgumentNullException(nameof(masterDetailServices));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public async Task<bool> ToggleUserStatusAsync(UserListDto user)
    {
        try
        {
            var action = user.Status == Shared.Models.Enums.CommonStatus.Enabled ? "禁用" : "启用";

            var result = await _userService.ToggleStatusAsync(user.Id);
            if (result.success)
            {
                _logger.LogInformation("成功{Action}用户: {UserName}", action, user.UserName);
                return true;
            }
            else
            {
                await _masterDetailServices.Dialog.ShowErrorAsync(result.errorMessage ?? "切换用户状态失败", "操作失败");
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "切换用户状态失败");
            await _masterDetailServices.Dialog.ShowErrorAsync("切换用户状态失败", "操作失败");
            return false;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> RestoreAsync(UserListDto user)
    {
        try
        {
            var confirmed = await _masterDetailServices.Dialog.ShowConfirmAsync(
                $"确认恢复用户 [{user.RealName ?? user.UserName}] 吗？", "恢复确认");
            if (!confirmed) return false;

            var result = await _userRepository.RestoreAsync(user.Id);
            if (result != null)
            {
                _logger.LogInformation("用户已恢复: {UserName}", user.UserName);
                await _masterDetailServices.Dialog.ShowSuccessAsync($"用户 '{user.RealName ?? user.UserName}' 已恢复", "操作成功");
                return true;
            }
            else
            {
                await _masterDetailServices.Dialog.ShowErrorAsync("恢复用户失败", "操作失败");
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "恢复用户失败");
            await _masterDetailServices.Dialog.ShowErrorAsync("恢复用户失败", "操作失败");
            return false;
        }
    }

    /// <inheritdoc/>
    public bool CanToggleUserStatus(UserListDto? user, bool isBusy)
    {
        return user != null && !isBusy;
    }

    /// <inheritdoc/>
    public bool CanRestore(UserListDto? user, bool isBusy, bool isAdmin)
    {
        return user != null && !isBusy && isAdmin;
    }
}
