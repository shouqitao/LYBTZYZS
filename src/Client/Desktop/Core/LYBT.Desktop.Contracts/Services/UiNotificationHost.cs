namespace LYBT.Desktop.Contracts.Services;

/// <summary>
/// 通知/对话框服务宿主 — 仅供无法构造注入的 WPF Control code-behind 使用。
/// Shell 侧由 <c>ViewModelServices</c> 构造时装配；Service/ViewModel 层禁止使用（应走构造注入）。
/// N6：Control 层消灭 System.Windows.MessageBox 双轨。
/// </summary>
public static class UiNotificationHost
{
    private static IUserNotificationService? _userNotification;
    private static IToastService? _toast;
    private static ICommonDialogService? _dialog;

    /// <summary>装配服务（幂等；后写覆盖）</summary>
    public static void Set(
        IUserNotificationService userNotification,
        IToastService toast,
        ICommonDialogService dialog)
    {
        _userNotification = userNotification ?? throw new ArgumentNullException(nameof(userNotification));
        _toast = toast ?? throw new ArgumentNullException(nameof(toast));
        _dialog = dialog ?? throw new ArgumentNullException(nameof(dialog));
    }

    /// <summary>用户通知服务（未装配时为 null）</summary>
    public static IUserNotificationService? UserNotification => _userNotification;

    /// <summary>Toast 轻提示（未装配时为 null）</summary>
    public static IToastService? Toast => _toast;

    /// <summary>通用确认/消息对话框（未装配时为 null）</summary>
    public static ICommonDialogService? Dialog => _dialog;
}
