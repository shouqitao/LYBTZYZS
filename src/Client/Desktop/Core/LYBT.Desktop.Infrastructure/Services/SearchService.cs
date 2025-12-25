using CommunityToolkit.Mvvm.ComponentModel;

namespace LYBT.Desktop.Infrastructure.Services
{
    /// <summary>
    /// 搜索服务实现
    /// OpenSpec: refactor-viewmodel-composition
    /// </summary>
    public partial class SearchService : ObservableObject, ISearchService, IDisposable
    {
        private CancellationTokenSource? _debounceCts;
        private CancellationTokenSource? _searchCts;
        private bool _disposed;

        [ObservableProperty]
        private string _searchText = string.Empty;

        [ObservableProperty]
        private bool _isSearching;

        [ObservableProperty]
        private int _debounceDelay = 300;

        /// <inheritdoc/>
        public event EventHandler<SearchRequestedEventArgs>? SearchRequested;

        /// <inheritdoc/>
        public async Task ExecuteSearchAsync(Func<string, Task> searchAction)
        {
            // 取消之前的防抖
            _debounceCts?.Cancel();
            _debounceCts?.Dispose();
            _debounceCts = new CancellationTokenSource();

            try
            {
                await Task.Delay(DebounceDelay, _debounceCts.Token);
                await ExecuteSearchImmediateAsync(searchAction);
            }
            catch (TaskCanceledException)
            {
                // 防抖被取消，忽略
            }
        }

        /// <inheritdoc/>
        public async Task ExecuteSearchImmediateAsync(Func<string, Task> searchAction)
        {
            // 取消之前的搜索
            _searchCts?.Cancel();
            _searchCts?.Dispose();
            _searchCts = new CancellationTokenSource();

            IsSearching = true;
            try
            {
                SearchRequested?.Invoke(this, new SearchRequestedEventArgs(SearchText));
                await searchAction(SearchText);
            }
            finally
            {
                IsSearching = false;
            }
        }

        /// <inheritdoc/>
        public void ClearSearch()
        {
            SearchText = string.Empty;
            CancelSearch();
        }

        /// <inheritdoc/>
        public void CancelSearch()
        {
            _debounceCts?.Cancel();
            _searchCts?.Cancel();
            IsSearching = false;
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        /// <param name="disposing">是否正在释放托管资源</param>
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) return;

            if (disposing)
            {
                _debounceCts?.Cancel();
                _debounceCts?.Dispose();
                _debounceCts = null;

                _searchCts?.Cancel();
                _searchCts?.Dispose();
                _searchCts = null;
            }

            _disposed = true;
        }
    }
}
