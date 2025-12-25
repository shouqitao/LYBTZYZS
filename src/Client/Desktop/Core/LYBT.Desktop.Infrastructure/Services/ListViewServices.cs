
namespace LYBT.Desktop.Infrastructure.Services
{
    /// <summary>
    /// 列表视图服务组合实现
    /// OpenSpec: refactor-viewmodel-composition
    /// </summary>
    /// <typeparam name="T">列表项类型</typeparam>
    public class ListViewServices<T> : IListViewServices<T>, IDisposable where T : class
    {
        private bool _disposed;

        /// <inheritdoc/>
        public ILoadingStateManager Loading { get; }

        /// <inheritdoc/>
        public IPaginationService Pagination { get; }

        /// <inheritdoc/>
        public ISearchService Search { get; }

        /// <inheritdoc/>
        public ISelectionService<T> Selection { get; }

        /// <inheritdoc/>
        public IErrorHandler ErrorHandler { get; }

        /// <inheritdoc/>
        public IAsyncExecutor AsyncExecutor { get; }

        public ListViewServices(
            ILoadingStateManager loading,
            IPaginationService pagination,
            ISearchService search,
            ISelectionService<T> selection,
            IErrorHandler errorHandler,
            IAsyncExecutor asyncExecutor)
        {
            Loading = loading;
            Pagination = pagination;
            Search = search;
            Selection = selection;
            ErrorHandler = errorHandler;
            AsyncExecutor = asyncExecutor;
        }

        /// <inheritdoc/>
        public void ResetAll()
        {
            Loading.Reset();
            Pagination.Reset();
            Search.ClearSearch();
            Selection.ClearSelection();
            ErrorHandler.ClearAllErrors();
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
            }

            _disposed = true;
        }
    }
}
