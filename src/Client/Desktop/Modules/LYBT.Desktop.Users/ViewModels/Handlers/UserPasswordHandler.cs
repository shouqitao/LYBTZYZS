using LYBT.Desktop.Infrastructure.Services;
using LYBT.Desktop.Users.Interfaces;
using LYBT.Desktop.Users.Models;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Users.ViewModels.Handlers;

/// <summary>
/// 用户密码处理实现
/// OpenSpec: refactor-frontend-srp-patterns - Handler提取模式
/// </summary>
public class UserPasswordHandler : IUserPasswordHandler
{
    private readonly IUserService _userService;
    private readonly IMasterDetailServices<UserListDto, UserDetailModel> _masterDetailServices;
    private readonly ILogger<UserPasswordHandler> _logger;

    public UserPasswordHandler(
        IUserService userService,
        IMasterDetailServices<UserListDto, UserDetailModel> masterDetailServices,
        ILogger<UserPasswordHandler> logger)
    {
        _userService = userService ?? throw new ArgumentNullException(nameof(userService));
        _masterDetailServices = masterDetailServices ?? throw new ArgumentNullException(nameof(masterDetailServices));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public async Task ResetPasswordAsync(UserListDto user)
    {
        try
        {
            var confirmed = await _masterDetailServices.Dialog.ShowConfirmAsync(
                $"确认重置用户 [{user.RealName ?? user.UserName}] 的密码吗？\n\n密码将被重置为系统配置的默认密码",
                "重置密码确认");
            if (!confirmed) return;

            var result = await _userService.ResetPasswordAsync(user.Id, null!);
            if (result.Success && result.Data != null)
            {
                await _masterDetailServices.Dialog.ShowSuccessAsync(
                    $"用户 [{user.RealName ?? user.UserName}] 的密码已重置\n\n新密码：{result.Data.TemporaryPassword}",
                    "重置成功");
            }
            else
            {
                await _masterDetailServices.Dialog.ShowErrorAsync(result.Error ?? "重置密码失败", "操作失败");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "重置密码失败");
            await _masterDetailServices.Dialog.ShowErrorAsync("重置密码失败", "操作失败");
        }
    }

    /// <inheritdoc/>
    public bool CanResetPassword(UserListDto? user, bool isBusy)
    {
        return user != null && !isBusy && user.Status == CommonStatus.Enabled;
    }
}
