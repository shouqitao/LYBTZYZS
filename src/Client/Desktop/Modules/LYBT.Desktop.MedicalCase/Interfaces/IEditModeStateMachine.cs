using LYBT.Desktop.MedicalCase.Models;

namespace LYBT.Desktop.MedicalCase.Interfaces;

/// <summary>
/// 工作区 ViewModel 的编辑模式状态机（US-MC-011）。
/// 遵循 AuthenticationStateMachine 模式：转换表 + 线程安全锁 + 锁外触发事件。
/// </summary>
public interface IEditModeStateMachine
{
    /// <summary>当前编辑状态。</summary>
    WorkspaceEditState CurrentState { get; }

    /// <summary>CurrentState 为 DirtyEditing（存在未保存更改）时为 true。</summary>
    bool IsDirty { get; }

    /// <summary>
    /// 从导航上下文初始化状态机。
    /// 首次使用前必须调用。
    /// </summary>
    /// <param name="initialState">Computed initial state from context.</param>
    /// <param name="guardPredicate">Optional guard — Fire returns false when guard returns false.</param>
    void Initialize(WorkspaceEditState initialState, Func<WorkspaceEditEvent, bool>? guardPredicate = null);

    /// <summary>
    /// 触发事件，若允许则转换状态。
    /// 非法转换或守卫失败时返回 false（从不抛出异常）。
    /// </summary>
    bool Fire(WorkspaceEditEvent evt, string? context = null);

    /// <summary>成功状态转换后触发（在状态锁之外）。</summary>
    event EventHandler<EditStateChangedEventArgs>? StateChanged;
}

/// <summary>
/// IEditModeStateMachine.StateChanged 的事件参数。
/// </summary>
public sealed class EditStateChangedEventArgs : EventArgs
{
    public WorkspaceEditState PreviousState { get; }
    public WorkspaceEditState NewState { get; }
    public WorkspaceEditEvent TriggerEvent { get; }
    public string? Context { get; }

    public EditStateChangedEventArgs(
        WorkspaceEditState previousState,
        WorkspaceEditState newState,
        WorkspaceEditEvent triggerEvent,
        string? context = null)
    {
        PreviousState = previousState;
        NewState = newState;
        TriggerEvent = triggerEvent;
        Context = context;
    }
}
