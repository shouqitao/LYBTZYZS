namespace LYBT.Desktop.MedicalCase.Models;

/// <summary>
/// 编辑类型枚举
/// OpenSpec: controlify-workspace
/// </summary>
public enum EditType
{
    /// <summary>
    /// 新建医案
    /// </summary>
    Create,

    /// <summary>
    /// 编辑挂起医案
    /// </summary>
    EditSuspended,

    /// <summary>
    /// 编辑已完成（历史编辑，需审计）
    /// </summary>
    EditCompleted,

    /// <summary>
    /// 只读查看
    /// </summary>
    ViewOnly
}
