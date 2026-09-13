using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Shell.Services.Login;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Shell.Services;

/// <summary>
/// Shell 登出协调服务实现（单例）。
/// 流程：活跃医案 → RequestLeaveAsync（保存/放弃/取消）；无活跃医案 → 二次确认；随后 PerformLogoutAsync。
/// </summary>
public class ShellLogoutService : IShellLogoutService
{
    private readonly IActiveConsultationService _activeConsultation;
    private readonly ILoginStateManager _loginState;
    private readonly ShellDialogHelper _dialogs;
    private readonly ILogger<ShellLogoutService> _logger;

    public ShellLogoutService(
        IActiveConsultationService activeConsultation,
        ILoginStateManager loginState,
        ShellDialogHelper dialogs,
        ILogger<ShellLogoutService> logger)
    {
        _activeConsultation = activeConsultation ?? throw new ArgumentNullException(nameof(activeConsultation));
        _loginState = loginState ?? throw new ArgumentNullException(nameof(loginState));
        _dialogs = dialogs ?? throw new ArgumentNullException(nameof(dialogs));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public async Task<LogoutOutcome> RequestLogoutAsync()
    {
        try
        {
            if (_activeConsultation.HasActiveConsultation)
            {
                var leaveResult = await _activeConsultation.RequestLeaveAsync();
                if (!leaveResult.CanLeave)
                {
                    _logger.LogDebug("用户选择继续停留，取消退出登录");
                    return LogoutOutcome.Cancelled;
                }

                _logger.LogInformation("活跃医案已处理（选择: {Choice}），继续退出登录", leaveResult.Choice);
            }
            else if (!await _dialogs.ShowConfirmationAsync("确定要退出登录吗？"))
            {
                return LogoutOutcome.Cancelled;
            }

            await _loginState.PerformLogoutAsync();
            return LogoutOutcome.LoggedOut;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "退出登录时发生异常");
            return LogoutOutcome.Failed;
        }
    }
}
