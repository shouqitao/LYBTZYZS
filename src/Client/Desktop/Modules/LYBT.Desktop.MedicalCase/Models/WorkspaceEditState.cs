namespace LYBT.Desktop.MedicalCase.Models;

/// <summary>
/// MedicalCaseWorkspaceViewModel 的编辑模式状态（US-MC-011）。
/// 用 6 状态有限状态机取代简单 EditState（Editing/ReadOnly）。
/// </summary>
public enum WorkspaceEditState
{
    /// <summary>查看医案。非所有者/已完成医案的默认状态。</summary>
    ReadOnly = 0,

    /// <summary>正在修改且无未保存更改。</summary>
    Editing = 1,

    /// <summary>正在修改且存在未保存更改（由 MakeChange 事件触发）。</summary>
    DirtyEditing = 2,

    /// <summary>瞬态：保存操作进行中。</summary>
    Saving = 3,

    /// <summary>瞬态：离开确认对话框已打开。</summary>
    LeavingConfirming = 4,

    /// <summary>瞬态：保存失败，用户必须先确认才能继续。</summary>
    TransitionBlocked = 5
}
