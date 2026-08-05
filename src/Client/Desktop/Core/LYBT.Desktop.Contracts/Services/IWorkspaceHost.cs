namespace LYBT.Desktop.Contracts.Services;

/// <summary>
/// 复合 ViewModel 模式中子级向父级请求操作的服务契约。
/// 子级 ViewModel 通过这些方法向父级请求 UI 操作。
/// 取代 Handler 回调 Action/Func 属性（SetBusy、ShowErrorMessage 等）
/// </summary>
public interface IWorkspaceHost
{
    void SetBusy(bool isBusy, string? message = null);
    Task ShowErrorAsync(string message);
    Task ShowSuccessAsync(string message);
    Task<bool> ShowConfirmAsync(string message, string title = "确认");
    ICommonDialogService? CommonDialogService { get; }

    /// <summary>
    /// 子级 ViewModel 通知父级需要重新计算状态。
    /// 父级应重新计算 WorkspaceState（CanComplete、CanPrint 等）
    /// </summary>
    void NotifyStateChanged();

    /// <summary>
    /// P1-2 修复：请求切换到编辑模式。
    /// 触发 EditModeStateMachine 的 EnterEdit 事件，从 ReadOnly 状态转换到 Editing 状态。
    /// </summary>
    void RequestEnterEditMode();
}
