using System.Net.Http;
using LYBT.Desktop.Infrastructure.Services;
using LYBT.Desktop.Infrastructure.ViewModels.Handlers;
using LYBT.Desktop.Contracts.Repositories;
using LYBT.Desktop.Users.Models;
using LYBT.Desktop.Users.ViewModels.Components;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Users.ViewModels.Handlers;

/// <summary>
/// 用户状态处理实现
/// RestoreAsync 复用基类统一实现，ToggleUserStatusAsync 独立实现 (走 UserService 元组模式)
/// </summary>
public class UserStatusHandler : BaseStatusHandler<UserListDto>, IUserStatusHandler
{
    private readonly UserService _userService;
    private readonly IUserRepository _userRepository;

    public UserStatusHandler(
        UserService userService,
        IUserRepository userRepository,
        IMasterDetailServices<UserListDto, UserDetailModel> masterDetailServices,
        ILogger<UserStatusHandler> logger)
        : base(masterDetailServices.Dialog, logger)
    {
        _userService = userService ?? throw new ArgumentNullException(nameof(userService));
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
    }

    protected override string EntityTypeName => "用户";
    protected override Guid GetEntityId(UserListDto e) => e.Id;
    protected override string GetEntityDisplayName(UserListDto e) => e.RealName ?? e.UserName;

    protected override async Task<object?> ExecuteRestoreAsync(Guid id)
        => await _userRepository.RestoreAsync(id);

    /// <inheritdoc/>
    public async Task<bool> ToggleUserStatusAsync(UserListDto user)
    {
        var action = user.Status == CommonStatus.Enabled ? "禁用" : "启用";
        try
        {
            var result = await _userService.ToggleStatusAsync(user.Id);
            if (result.success)
            {
                Logger.LogInformation("成功{Action}用户: {UserName}", action, user.UserName);
                return true;
            }

            await Dialog.ShowErrorAsync(result.errorMessage ?? "切换用户状态失败", "操作失败");
            return false;
        }
        catch (HttpRequestException ex)
        {
            Logger.LogError(ex, "切换用户状态失败");
            await Dialog.ShowErrorAsync("切换用户状态失败", "操作失败");
            return false;
        }
    }

    /// <inheritdoc/>
    public bool CanToggleUserStatus(UserListDto? user, bool isBusy) => user != null && !isBusy;

    /// <inheritdoc/>
    public bool CanRestore(UserListDto? user, bool isBusy, bool isAdmin) => user != null && !isBusy && isAdmin;
}
