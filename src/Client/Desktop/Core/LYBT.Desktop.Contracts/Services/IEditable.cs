namespace LYBT.Desktop.Contracts.Services;

/// <summary>
/// 可编辑对象接口
/// 用于标记和管理编辑状态，支持未保存变更检测
/// OpenSpec: navigation-guard-and-editable-interface
/// </summary>
public interface IEditable
{
    /// <summary>
    /// 是否有未保存的变更
    /// </summary>
    bool HasUnsavedChanges { get; }

    /// <summary>
    /// 是否正在编辑
    /// </summary>
    bool IsEditing { get; }

    /// <summary>
    /// 标记为已变更
    /// </summary>
    void MarkAsChanged();

    /// <summary>
    /// 标记为已保存
    /// </summary>
    void MarkAsSaved();

    /// <summary>
    /// 开始编辑
    /// </summary>
    void BeginEdit();

    /// <summary>
    /// 取消编辑（恢复原始值）
    /// </summary>
    void CancelEdit();

    /// <summary>
    /// 确认编辑完成
    /// </summary>
    void EndEdit();
}
