
namespace LYBT.Desktop.Infrastructure.Services
{
    /// <summary>
    /// Master-Detail视图服务组合实现
    /// OpenSpec: refactor-viewmodel-composition
    /// </summary>
    /// <typeparam name="TListItem">列表项类型</typeparam>
    /// <typeparam name="TDetail">详情模型类型</typeparam>
    public class MasterDetailServices<TListItem, TDetail> : IMasterDetailServices<TListItem, TDetail>, IDisposable
        where TListItem : class
        where TDetail : class
    {
        private bool _disposed;

        /// <inheritdoc/>
        public IListViewServices<TListItem> List { get; }

        /// <inheritdoc/>
        public IDetailEditorService<TDetail> DetailEditor { get; }

        /// <inheritdoc/>
        public IDialogManager Dialog { get; }

        /// <inheritdoc/>
        public IViewNavigationService Navigation { get; }

        // === 便捷属性委托 ===

        /// <inheritdoc/>
        public ILoadingStateManager Loading => List.Loading;

        /// <inheritdoc/>
        public IPaginationService Pagination => List.Pagination;

        /// <inheritdoc/>
        public ISearchService Search => List.Search;

        /// <inheritdoc/>
        public ISelectionService<TListItem> Selection => List.Selection;

        /// <inheritdoc/>
        public IErrorHandler ErrorHandler => List.ErrorHandler;

        /// <inheritdoc/>
        public IAsyncExecutor AsyncExecutor => List.AsyncExecutor;

        public MasterDetailServices(
            IListViewServices<TListItem> list,
            IDetailEditorService<TDetail> detailEditor,
            IDialogManager dialog,
            IViewNavigationService navigation)
        {
            List = list;
            DetailEditor = detailEditor;
            Dialog = dialog;
            Navigation = navigation;
        }

        /// <inheritdoc/>
        public void ResetAll()
        {
            List.ResetAll();
            DetailEditor.Clear();
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) return;

            if (disposing)
            {
                ResetAll();
                List.Dispose();
            }

            _disposed = true;
        }
    }
}
