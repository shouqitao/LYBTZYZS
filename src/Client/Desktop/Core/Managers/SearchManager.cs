using Prism.Mvvm;

namespace LYBT.Desktop.Core.Managers
{

    /// <summary>
    /// 搜索管理器实现 - 管理搜索状态和防抖逻辑
    /// UltraThink架构: 将搜索职责从ViewModel中分离，提供智能搜索功能
    /// </summary>
    public class SearchManager : BindableBase, ISearchManager
    {

        #region Fields

        private string _searchKeyword = string.Empty;
        private bool _isSearching = false;
        private int _searchDelay = 500; // 默认500ms防抖
        private CancellationTokenSource? _searchCancellationTokenSource;

        #endregion Fields

        #region Properties

        /// <summary>
        /// 搜索关键字
        /// </summary>
        public string SearchKeyword
        {
            get => _searchKeyword;
            set
            {
                if (SetProperty(ref _searchKeyword, value))
                {
                    RaisePropertyChanged(nameof(HasSearchCriteria));

                    // 自动触发防抖搜索
                    _ = TriggerDelayedSearchAsync();
                }
            }
        }

        /// <summary>
        /// 是否有搜索条件
        /// </summary>
        public bool HasSearchCriteria => !string.IsNullOrWhiteSpace(SearchKeyword);

        /// <summary>
        /// 是否正在搜索
        /// </summary>
        public bool IsSearching
        {
            get => _isSearching;
            private set => SetProperty(ref _isSearching, value);
        }

        /// <summary>
        /// 搜索延迟时间(毫秒) - 防抖功能
        /// </summary>
        public int SearchDelay
        {
            get => _searchDelay;
            set => SetProperty(ref _searchDelay, Math.Max(0, value));
        }

        #endregion Properties

        #region Events

        /// <summary>
        /// 搜索执行事件
        /// </summary>
        public event EventHandler<SearchExecutedEventArgs>? SearchExecuted;

        /// <summary>
        /// 搜索清除事件
        /// </summary>
        public event EventHandler? SearchCleared;

        #endregion Events

        #region Methods

        /// <summary>
        /// 执行搜索
        /// </summary>
        public async Task ExecuteSearchAsync()
        {
            try
            {
                IsSearching = true;

                var args = new SearchExecutedEventArgs(SearchKeyword);
                SearchExecuted?.Invoke(this, args);

                await Task.CompletedTask;
            }
            finally
            {
                IsSearching = false;
            }
        }

        /// <summary>
        /// 清除搜索条件
        /// </summary>
        public void ClearSearch()
        {
            // 取消正在进行的搜索
            _searchCancellationTokenSource?.Cancel();

            SearchKeyword = string.Empty;
            SearchCleared?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// 立即搜索(不使用防抖)
        /// </summary>
        public async Task SearchImmediatelyAsync()
        {
            // 取消之前的延迟搜索
            _searchCancellationTokenSource?.Cancel();

            await ExecuteSearchAsync();
        }

        /// <summary>
        /// 设置搜索关键字并触发搜索
        /// </summary>
        public async Task SetSearchKeywordAsync(string keyword)
        {
            SearchKeyword = keyword;
            await SearchImmediatelyAsync();
        }

        #endregion Methods

        #region Private Methods

        /// <summary>
        /// 触发延迟搜索 - 实现防抖功能
        /// </summary>
        private async Task TriggerDelayedSearchAsync()
        {
            // 取消之前的搜索
            _searchCancellationTokenSource?.Cancel();
            _searchCancellationTokenSource = new CancellationTokenSource();

            var cancellationToken = _searchCancellationTokenSource.Token;

            try
            {
                // 如果搜索延迟为0，立即执行搜索
                if (SearchDelay <= 0)
                {
                    await ExecuteSearchAsync();
                    return;
                }

                // 等待指定的延迟时间
                await Task.Delay(SearchDelay, cancellationToken);

                // 如果没有被取消，执行搜索
                if (!cancellationToken.IsCancellationRequested)
                {
                    await ExecuteSearchAsync();
                }
            }
            catch (OperationCanceledException)
            {
                // 搜索被取消，忽略异常
            }
        }

        #endregion Private Methods

        #region IDisposable Support

        private bool _disposed = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _searchCancellationTokenSource?.Cancel();
                    _searchCancellationTokenSource?.Dispose();
                }

                _disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion IDisposable Support
    }
}
