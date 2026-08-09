using LYBT.Desktop.Infrastructure.Services;
using LYBT.Desktop.Infrastructure.ViewModels.Handlers;
using LYBT.Desktop.Contracts.Repositories;
using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Users.Models;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Users.ViewModels.Handlers;

/// <summary>
/// 用户状态处理实现
/// RestoreAsync/ToggleStatusAsync 均复用基类统一模式
/// </summary>
public class UserStatusHandler : BaseStatusHandler<UserListDto>, IUserStatusHandler
{
    private readonly IUserService _userService;
    private readonly IUserRepository _userRepository;

    public UserStatusHandler(
        IUserService userService,
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
    protected override CommonStatus GetEntityStatus(UserListDto e) => e.Status;

    protected override async Task<object?> ExecuteRestoreAsync(Guid id)
        => await _userRepository.RestoreAsync(id);

    protected override async Task<CommonStatus?> ExecuteToggleStatusAsync(Guid id)
    {
        var result = await _userService.ToggleStatusAsync(id);
        return result.Success ? result.Data?.Status : null;
    }

    /// <inheritdoc/>
    public async Task<bool> ToggleUserStatusAsync(UserListDto user)
        => await ToggleStatusAsync(user);

    /// <inheritdoc/>
    public bool CanToggleUserStatus(UserListDto? user, bool isBusy) => user != null && !isBusy;
}
