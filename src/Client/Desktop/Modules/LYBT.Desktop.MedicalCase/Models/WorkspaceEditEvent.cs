namespace LYBT.Desktop.MedicalCase.Models;

/// <summary>
/// 驱动编辑模式状态机的事件（US-MC-011）。
/// </summary>
public enum WorkspaceEditEvent
{
    /// <summary>用户点击"编辑"按钮。</summary>
    EnterEdit = 0,

    /// <summary>用户取消编辑并回退（从干净的 Editing 状态）。</summary>
    ExitEdit = 1,

    /// <summary>任意数据修改——将 Editing 转换为 DirtyEditing。</summary>
    MakeChange = 2,

    /// <summary>用户点击保存/挂起。</summary>
    Save = 3,

    /// <summary>保存操作成功完成。</summary>
    SaveCompleted = 4,

    /// <summary>保存操作失败。</summary>
    SaveFailed = 5,

    /// <summary>用户点击返回或触发导航离开。</summary>
    RequestLeave = 6,

    /// <summary>用户确认离开（放弃或先保存再离开）。</summary>
    LeaveConfirmed = 7,

    /// <summary>用户取消离开对话框（留在当前页面）。</summary>
    LeaveCancelled = 8,

    /// <summary>导航上下文初始化——从上下文设置状态。</summary>
    Initialize = 9
}
