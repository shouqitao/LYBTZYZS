using System.Collections;

namespace LYBT.Desktop.Contracts.Services.MasterDetail;

/// <summary>
/// 选择服务接口
/// OpenSpec: unify-desktop-architecture (Phase 1.2)
/// 管理列表的单选和多选状态
/// </summary>
public interface ISelectionService
{
    /// <summary>
    /// 当前选中项
    /// </summary>
    object? SelectedItem { get; set; }

    /// <summary>
    /// 多选项集合
    /// </summary>
    IList SelectedItems { get; }

    /// <summary>
    /// 是否有选中项
    /// </summary>
    bool HasSelection { get; }

    /// <summary>
    /// 选中项数量
    /// </summary>
    int SelectionCount { get; }

    /// <summary>
    /// 清除选择
    /// </summary>
    void ClearSelection();

    /// <summary>
    /// 全选
    /// </summary>
    void SelectAll();

    /// <summary>
    /// 选择变化事件
    /// </summary>
    event EventHandler? SelectionChanged;
}

/// <summary>
/// 泛型选择服务接口
/// 提供类型安全的选择操作
/// </summary>
/// <typeparam name="T">列表项类型</typeparam>
public interface ISelectionService<T> : ISelectionService where T : class
{
    /// <summary>
    /// 当前选中项（强类型）
    /// </summary>
    new T? SelectedItem { get; set; }

    /// <summary>
    /// 多选项集合（强类型）
    /// </summary>
    new IList<T> SelectedItems { get; }
}
