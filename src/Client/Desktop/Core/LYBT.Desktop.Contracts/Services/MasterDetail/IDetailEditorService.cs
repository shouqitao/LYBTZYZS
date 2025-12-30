namespace LYBT.Desktop.Contracts.Services.MasterDetail;

/// <summary>
/// 详情编辑服务接口
/// OpenSpec: unify-desktop-architecture (Phase 1.2)
/// 管理详情面板的编辑状态和变更追踪
/// </summary>
public interface IDetailEditorService
{
    /// <summary>
    /// 是否处于编辑模式
    /// </summary>
    bool IsEditing { get; }

    /// <summary>
    /// 是否有未保存的更改
    /// </summary>
    bool HasChanges { get; }

    /// <summary>
    /// 是否为新建模式
    /// </summary>
    bool IsNew { get; }

    /// <summary>
    /// 开始编辑
    /// </summary>
    void BeginEdit();

    /// <summary>
    /// 开始新建
    /// </summary>
    void BeginNew();

    /// <summary>
    /// 结束编辑（保存后调用）
    /// </summary>
    void EndEdit();

    /// <summary>
    /// 取消编辑
    /// </summary>
    void CancelEdit();

    /// <summary>
    /// 标记为已修改
    /// </summary>
    void MarkAsChanged();

    /// <summary>
    /// 重置变更状态
    /// </summary>
    void ResetChanges();

    /// <summary>
    /// 编辑状态变化事件
    /// </summary>
    event EventHandler? EditStateChanged;

    /// <summary>
    /// 变更状态变化事件
    /// </summary>
    event EventHandler? ChangesStateChanged;
}
