using System.Collections.ObjectModel;
using System.ComponentModel;

namespace LYBT.Desktop.Infrastructure.Services
{
    /// <summary>
    /// 选择服务接口
    /// OpenSpec: refactor-viewmodel-composition
    ///
    /// 提供单选、多选状态管理
    /// </summary>
    /// <typeparam name="T">列表项类型</typeparam>
    public interface ISelectionService<T> : INotifyPropertyChanged where T : class
    {
        /// <summary>当前选中项</summary>
        T? SelectedItem { get; set; }

        /// <summary>选中项集合（多选模式）</summary>
        ObservableCollection<T> SelectedItems { get; }

        /// <summary>是否有选中项</summary>
        bool HasSelection { get; }

        /// <summary>选中项数量</summary>
        int SelectionCount { get; }

        /// <summary>是否为多选模式</summary>
        bool IsMultiSelectMode { get; set; }

        /// <summary>
        /// 选择变更事件
        /// </summary>
        event EventHandler<SelectionChangedEventArgs<T>>? SelectionChanged;

        /// <summary>
        /// 选择单个项
        /// </summary>
        /// <param name="item">要选择的项</param>
        void Select(T? item);

        /// <summary>
        /// 选择多个项
        /// </summary>
        /// <param name="items">要选择的项集合</param>
        void SelectMultiple(IEnumerable<T> items);

        /// <summary>
        /// 切换项的选中状态
        /// </summary>
        /// <param name="item">目标项</param>
        void ToggleSelection(T item);

        /// <summary>清空选择</summary>
        void ClearSelection();
    }

    /// <summary>
    /// 选择变更事件参数
    /// </summary>
    /// <typeparam name="T">列表项类型</typeparam>
    public class SelectionChangedEventArgs<T> : EventArgs where T : class
    {
        /// <summary>新选中项</summary>
        public T? NewSelection { get; }

        /// <summary>旧选中项</summary>
        public T? OldSelection { get; }

        /// <summary>当前所有选中项</summary>
        public IReadOnlyList<T> AllSelectedItems { get; }

        public SelectionChangedEventArgs(T? newSelection, T? oldSelection, IReadOnlyList<T> allSelectedItems)
        {
            NewSelection = newSelection;
            OldSelection = oldSelection;
            AllSelectedItems = allSelectedItems;
        }
    }
}
