using CommunityToolkit.Mvvm.ComponentModel;

namespace LYBT.Desktop.Infrastructure.Services
{
    /// <summary>
    /// 加载状态管理服务实现
    /// OpenSpec: refactor-viewmodel-composition
    /// </summary>
    public partial class LoadingStateManager : ObservableObject, ILoadingStateManager
    {
        private readonly object _lockObject = new();
        private int _loadingCount;

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private bool _isBusy;

        [ObservableProperty]
        private string _busyMessage = string.Empty;

        /// <inheritdoc/>
        public int LoadingCount
        {
            get
            {
                lock (_lockObject)
                {
                    return _loadingCount;
                }
            }
        }

        /// <inheritdoc/>
        public async Task ExecuteWithLoadingAsync(Func<Task> action, string? message = null, bool isBusy = false)
        {
            BeginLoading(message);
            if (isBusy) IsBusy = true;

            try
            {
                await action();
            }
            finally
            {
                EndLoading();
                if (isBusy) IsBusy = false;
            }
        }

        /// <inheritdoc/>
        public async Task<T> ExecuteWithLoadingAsync<T>(Func<Task<T>> action, string? message = null, bool isBusy = false)
        {
            BeginLoading(message);
            if (isBusy) IsBusy = true;

            try
            {
                return await action();
            }
            finally
            {
                EndLoading();
                if (isBusy) IsBusy = false;
            }
        }

        /// <inheritdoc/>
        public void BeginLoading(string? message = null)
        {
            lock (_lockObject)
            {
                _loadingCount++;
                IsLoading = true;
                if (!string.IsNullOrEmpty(message))
                {
                    BusyMessage = message;
                }
            }
        }

        /// <inheritdoc/>
        public void EndLoading()
        {
            lock (_lockObject)
            {
                if (_loadingCount > 0)
                {
                    _loadingCount--;
                }

                if (_loadingCount == 0)
                {
                    IsLoading = false;
                    BusyMessage = string.Empty;
                }
            }
        }

        /// <inheritdoc/>
        public void Reset()
        {
            lock (_lockObject)
            {
                _loadingCount = 0;
                IsLoading = false;
                IsBusy = false;
                BusyMessage = string.Empty;
            }
        }
    }
}
