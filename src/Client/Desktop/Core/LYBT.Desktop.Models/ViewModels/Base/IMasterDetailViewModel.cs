using System.Windows.Input;

namespace LYBT.Desktop.Models.ViewModels.Base
{
    /// <summary>
    /// Master-Detail ViewModel接口
    /// OpenSpec: refactor-master-detail-layout
    ///
    /// 定义Master-Detail模式所需的属性和命令
    /// </summary>
    /// <typeparam name="TListItem">列表项类型</typeparam>
    /// <typeparam name="TDetail">详情类型</typeparam>
    public interface IMasterDetailViewModel<TListItem, TDetail>
        where TListItem : class
        where TDetail : class
    {
        #region 列表相关属性

        /// <summary>列表项集合</summary>
        IEnumerable<TListItem> Items { get; }

        /// <summary>当前选中项</summary>
        TListItem? SelectedItem { get; set; }

        /// <summary>搜索文本</summary>
        string SearchText { get; set; }

        /// <summary>当前页码</summary>
        int CurrentPage { get; set; }

        /// <summary>总页数</summary>
        int TotalPages { get; }

        /// <summary>是否有选中项</summary>
        bool HasSelection { get; }

        #endregion

        #region 详情相关属性

        /// <summary>当前详情数据</summary>
        TDetail? CurrentDetail { get; }

        /// <summary>是否处于编辑模式</summary>
        bool IsEditMode { get; set; }

        /// <summary>是否正在加载详情</summary>
        bool IsLoadingDetail { get; }

        /// <summary>是否有未保存的更改</summary>
        bool HasUnsavedChanges { get; }

        #endregion

        #region 列表命令

        /// <summary>搜索命令</summary>
        ICommand SearchCommand { get; }

        /// <summary>刷新命令</summary>
        ICommand RefreshCommand { get; }

        /// <summary>新增命令</summary>
        ICommand AddCommand { get; }

        /// <summary>上一页命令</summary>
        ICommand PreviousPageCommand { get; }

        /// <summary>下一页命令</summary>
        ICommand NextPageCommand { get; }

        #endregion

        #region 详情命令

        /// <summary>进入编辑模式</summary>
        ICommand EditCommand { get; }

        /// <summary>保存命令</summary>
        ICommand SaveCommand { get; }

        /// <summary>取消编辑</summary>
        ICommand CancelCommand { get; }

        /// <summary>删除当前项</summary>
        ICommand DeleteCurrentCommand { get; }

        #endregion
    }
}
