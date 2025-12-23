namespace LYBT.Desktop.Contracts.Services;

/// <summary>
/// API连接恢复操作类型
/// enhance-shell-connection-dialog: 定义用户在连接失败对话框中可选择的操作
/// </summary>
public enum RecoveryAction
{
    /// <summary>
    /// 重试连接
    /// </summary>
    Retry,

    /// <summary>
    /// 进入离线模式 (v2.0预留)
    /// </summary>
    OfflineMode,

    /// <summary>
    /// 退出应用
    /// </summary>
    Exit
}
