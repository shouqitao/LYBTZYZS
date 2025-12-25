using Prism.Regions;

namespace LYBT.Desktop.Infrastructure.Services
{
    /// <summary>
    /// 视图导航服务实现
    /// OpenSpec: refactor-viewmodel-composition
    ///
    /// 集成Prism IRegionManager
    /// </summary>
    public class ViewNavigationService : IViewNavigationService
    {
        private readonly IRegionManager _regionManager;
        private readonly List<string> _navigationHistory = new();
        private const string DefaultRegion = "MainRegion";

        public ViewNavigationService(IRegionManager regionManager)
        {
            _regionManager = regionManager;
        }

        /// <inheritdoc/>
        public string? CurrentView => _navigationHistory.LastOrDefault();

        /// <inheritdoc/>
        public IReadOnlyList<string> NavigationHistory => _navigationHistory.AsReadOnly();

        /// <inheritdoc/>
        public bool CanNavigateBack => _navigationHistory.Count > 1;

        /// <inheritdoc/>
        public event EventHandler<NavigationChangedEventArgs>? NavigationChanged;

        /// <inheritdoc/>
        public Task NavigateToAsync(string viewName, string? regionName = null, IDictionary<string, object>? parameters = null)
        {
            var tcs = new TaskCompletionSource<bool>();
            var region = regionName ?? DefaultRegion;
            var fromView = CurrentView;

            // 构建带参数的URI
            var uri = viewName;
            if (parameters != null && parameters.Count > 0)
            {
                var queryString = string.Join("&", parameters.Select(kvp =>
                    $"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value?.ToString() ?? "")}"));
                uri = $"{viewName}?{queryString}";
            }

            _regionManager.RequestNavigate(region, uri, result =>
            {
                if (result.Result == true)
                {
                    _navigationHistory.Add(viewName);
                    NavigationChanged?.Invoke(this, new NavigationChangedEventArgs(fromView, viewName, parameters));
                }
                tcs.SetResult(result.Result == true);
            });

            return tcs.Task;
        }

        /// <inheritdoc/>
        public Task NavigateBackAsync()
        {
            if (!CanNavigateBack) return Task.CompletedTask;

            // 移除当前视图
            _navigationHistory.RemoveAt(_navigationHistory.Count - 1);

            // 导航到上一个视图
            var previousView = _navigationHistory.LastOrDefault();
            if (previousView != null)
            {
                // 从历史中移除，因为NavigateToAsync会再次添加
                _navigationHistory.RemoveAt(_navigationHistory.Count - 1);
                return NavigateToAsync(previousView);
            }

            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task NavigateToDetailAsync<TKey>(string viewName, TKey id, string? regionName = null)
        {
            var parameters = new Dictionary<string, object>
            {
                { "id", id! }
            };
            return NavigateToAsync(viewName, regionName, parameters);
        }

        /// <inheritdoc/>
        public void ClearHistory()
        {
            _navigationHistory.Clear();
        }
    }
}
