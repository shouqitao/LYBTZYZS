using LYBT.Shared.Models.Contracts.Users;
using Microsoft.Extensions.Logging;
using Prism.Services.Dialogs;

namespace LYBT.Desktop.Users.ViewModels.Handlers;

/// <summary>
/// 用户审计日志处理实现
/// OpenSpec: refactor-frontend-srp-patterns - Handler提取模式
/// </summary>
public class UserAuditHandler : IUserAuditHandler
{
    private readonly IDialogService _dialogService;
    private readonly ILogger<UserAuditHandler> _logger;

    public UserAuditHandler(
        IDialogService dialogService,
        ILogger<UserAuditHandler> logger)
    {
        _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public void ShowAuditLog(UserListDto user)
    {
        _logger.LogInformation("查看用户审计日志：{UserId}", user.Id);
        _dialogService.ShowDialog("EntityAuditLogDialog",
            new DialogParameters
            {
                { "EntityType", "user" },
                { "EntityId", user.Id },
                { "EntityDescription", $"用户：{user.RealName ?? user.UserName}" }
            },
            _ => { });
    }

    /// <inheritdoc/>
    public bool CanShowAuditLog(UserListDto? user)
    {
        return user != null;
    }
}
