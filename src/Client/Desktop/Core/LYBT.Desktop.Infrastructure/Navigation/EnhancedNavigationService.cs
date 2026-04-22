using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows.Input;
using Microsoft.Extensions.Logging;
using Prism.Commands;
using Prism.Events;
using Prism.Regions;
using LYBT.Desktop.Infrastructure.Events;

namespace LYBT.Desktop.Infrastructure.Navigation
{
    /// <summary>
    /// 增强型导航服务实现 - Phase 2.1: Navigation Improvements
    /// 提供集中化导航管理，包括历史记录、面包屑、状态恢复等
    /// </summary>
    public partial class EnhancedNavigationService : IEnhancedNavigationService
    {
        private readonly IRegionManager _regionManager;
        private readonly ILogger<EnhancedNavigationService> _logger;
        private readonly IEventAggregator _eventAggregator;

        // Navigation stacks
        private readonly Stack<NavigationEntry> _history = new();
        private readonly Stack<NavigationEntry> _forwardStack = new();
        private NavigationEntry? _currentEntry;

        // Observable collections for binding
        private readonly ObservableCollection<NavigationEntry> _historyCollection = new();
        private readonly ObservableCollection<NavigationEntry> _forwardCollection = new();
        private readonly ObservableCollection<BreadcrumbItem> _breadcrumbs = new();

        // Read-only observable collections
        public ReadOnlyObservableCollection<NavigationEntry> History { get; }
        public ReadOnlyObservableCollection<NavigationEntry> ForwardStack { get; }
        public ReadOnlyObservableCollection<BreadcrumbItem> Breadcrumbs { get; }

        // Suggestions cache
        private readonly List<NavigationSuggestion> _cachedSuggestions = new();

        /// <summary>
        /// 构造函数
        /// </summary>
        public EnhancedNavigationService(
            IRegionManager regionManager,
            ILogger<EnhancedNavigationService> logger,
            IEventAggregator eventAggregator)
        {
            _regionManager = regionManager ?? throw new ArgumentNullException(nameof(regionManager));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _eventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));

            // Initialize read-only collections
            History = new ReadOnlyObservableCollection<NavigationEntry>(_historyCollection);
            ForwardStack = new ReadOnlyObservableCollection<NavigationEntry>(_forwardCollection);
            Breadcrumbs = new ReadOnlyObservableCollection<BreadcrumbItem>(_breadcrumbs);

            // Subscribe to journal events if available
            SubscribeToNavigationEvents();
        }

        #region IEnhancedNavigationService Implementation

        /// <summary>
        /// 导航到指定 URI
        /// </summary>
        public async Task<bool> NavigateAsync(string uri, NavigationParameters parameters = null!)
        {
            try
            {
                _logger.LogInformation("Navigating to: {Uri}", uri);

                // Parse URI to extract region and view
                var (regionName, viewPath) = ParseUri(uri);

                // Create navigation entry
                var entry = CreateNavigationEntry(uri, viewPath, parameters);

                // Save current state before navigating
                if (_currentEntry != null)
                {
                    _history.Push(_currentEntry);
                    UpdateHistoryCollection();
                }

                // Clear forward stack when navigating to new location
                _forwardStack.Clear();
                UpdateForwardCollection();

                // Perform navigation
                var success = await NavigateToRegionAsync(regionName, viewPath, parameters);

                if (success)
                {
                    _currentEntry = entry;
                    UpdateBreadcrumbs();
                    OnNavigated(new NavigatedEventArgs { Entry = entry, IsBack = false, IsForward = false });

                    // Publish navigation event
                    PublishNavigationEvent(uri, parameters);

                    _logger.LogInformation("Navigation successful: {Uri}", uri);
                }
                else
                {
                    // Rollback history on failure
                    if (_history.Count > 0)
                    {
                        _history.Pop();
                        UpdateHistoryCollection();
                    }

                    OnNavigationFailed(new NavigationFailedEventArgs
                    {
                        Uri = uri,
                        ErrorMessage = "Navigation failed"
                    });

                    _logger.LogWarning("Navigation failed: {Uri}", uri);
                }

                return success;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Navigation error: {Uri}", uri);
                OnNavigationFailed(new NavigationFailedEventArgs
                {
                    Uri = uri,
                    Exception = ex,
                    ErrorMessage = ex.Message
                });
                return false;
            }
        }

        /// <summary>
        /// 导航到指定区域和视图
        /// </summary>
        public Task<bool> NavigateToRegionAsync(string regionName, string viewName, NavigationParameters parameters = null!)
        {
            try
            {
                _logger.LogInformation("Navigating to region: {Region}, view: {View}", regionName, viewName);

                var region = _regionManager.Regions.ContainsRegionWithName(regionName)
                    ? _regionManager.Regions[regionName]
                    : null;

                if (region == null)
                {
                    _logger.LogWarning("Region not found: {Region}", regionName);
                    return Task.FromResult(false);
                }

                // Request navigation
                var request = new NavigationRequest(
                    regionName,
                    CreateUri(regionName, viewName, parameters!),
                    parameters ?? new NavigationParameters()
                );

                // Execute navigation - Prism IRegion.RequestNavigate(Uri source, Action<NavigationResult> callback)
                var navigationUri = new Uri(CreateUri(regionName, viewName, parameters!), UriKind.RelativeOrAbsolute);
                region.RequestNavigate(navigationUri, result =>
                {
                    if (result.Result.HasValue && result.Result.Value)
                    {
                        _logger.LogInformation("Region navigation successful: {Region}/{View}", regionName, viewName);
                    }
                    else
                    {
                        _logger.LogWarning("Region navigation failed: {Region}/{View}", regionName, viewName);
                    }
                }, parameters ?? new NavigationParameters());

                return Task.FromResult(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Region navigation error: {Region}/{View}", regionName, viewName);
                return Task.FromResult(false);
            }
        }

        /// <summary>
        /// 返回到上一个导航位置
        /// </summary>
        public async Task<bool> GoBackAsync()
        {
            if (!CanGoBack)
            {
                _logger.LogWarning("Cannot go back - history is empty");
                return false;
            }

            try
            {
                _logger.LogInformation("Going back in navigation history");

                // Push current entry to forward stack
                if (_currentEntry != null)
                {
                    _forwardStack.Push(_currentEntry);
                    UpdateForwardCollection();
                }

                // Pop from history
                var previousEntry = _history.Pop();
                UpdateHistoryCollection();

                // Navigate to previous entry
                var success = await NavigateToEntry(previousEntry, isBack: true);

                if (success)
                {
                    _currentEntry = previousEntry;
                    UpdateBreadcrumbs();
                    OnNavigated(new NavigatedEventArgs { Entry = previousEntry, IsBack = true, IsForward = false });
                }

                return success;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Go back error");
                return false;
            }
        }

        /// <summary>
        /// 前进到下一个导航位置
        /// </summary>
        public async Task<bool> GoForwardAsync()
        {
            if (!CanGoForward)
            {
                _logger.LogWarning("Cannot go forward - forward stack is empty");
                return false;
            }

            try
            {
                _logger.LogInformation("Going forward in navigation history");

                // Push current entry to history
                if (_currentEntry != null)
                {
                    _history.Push(_currentEntry);
                    UpdateHistoryCollection();
                }

                // Pop from forward stack
                var nextEntry = _forwardStack.Pop();
                UpdateForwardCollection();

                // Navigate to next entry
                var success = await NavigateToEntry(nextEntry, isForward: true);

                if (success)
                {
                    _currentEntry = nextEntry;
                    UpdateBreadcrumbs();
                    OnNavigated(new NavigatedEventArgs { Entry = nextEntry, IsBack = false, IsForward = true });
                }

                return success;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Go forward error");
                return false;
            }
        }

        /// <summary>
        /// 导航到主页
        /// </summary>
        public Task<bool> NavigateHomeAsync()
        {
            _logger.LogInformation("Navigating to home");
            // Clear history when going home
            ClearHistory();
            return NavigateAsync("/Home");
        }

        /// <summary>
        /// 清除导航历史
        /// </summary>
        public void ClearHistory()
        {
            _logger.LogInformation("Clearing navigation history");
            _history.Clear();
            _forwardStack.Clear();
            _currentEntry = null;
            UpdateHistoryCollection();
            UpdateForwardCollection();
            UpdateBreadcrumbs();
        }

        /// <summary>
        /// 当前导航条目
        /// </summary>
        public NavigationEntry CurrentEntry => _currentEntry ?? new NavigationEntry(
                "/",
                "Home",
                new NavigationParameters(),
                DateTime.UtcNow
            );

        /// <summary>
        /// 是否可以返回
        /// </summary>
        public bool CanGoBack => _history.Count > 0;

        /// <summary>
        /// 是否可以前进
        /// </summary>
        public bool CanGoForward => _forwardStack.Count > 0;

        /// <summary>
        /// 获取导航建议
        /// </summary>
        public IEnumerable<NavigationSuggestion> GetSuggestions(int count = 5)
        {
            _logger.LogDebug("Getting {Count} navigation suggestions", count);

            var suggestions = new List<NavigationSuggestion>();

            // Context-based suggestions from current entry
            if (_currentEntry != null)
            {
                var contextual = GetContextualSuggestions(_currentEntry);
                suggestions.AddRange(contextual);
            }

            // Frequent destinations (from history)
            var frequent = GetFrequentSuggestions(count);
            suggestions.AddRange(frequent);

            // Recent destinations
            var recent = GetRecentSuggestions(count);
            suggestions.AddRange(recent);

            // Sort by confidence and take top N
            return suggestions
                .OrderByDescending(s => s.Confidence)
                .Take(count)
                .ToList();
        }

        /// <summary>
        /// 导航完成事件
        /// </summary>
        public event EventHandler<NavigatedEventArgs>? Navigated;

        /// <summary>
        /// 导航取消事件
        /// </summary>
        public event EventHandler<NavigationCancelledEventArgs>? NavigationCancelled;

        /// <summary>
        /// 导航失败事件
        /// </summary>
        public event EventHandler<NavigationFailedEventArgs>? NavigationFailed;

        #endregion

        #region Private Helper Methods

        /// <summary>
        /// 解析 URI 提取区域和视图路径
        /// </summary>
        private (string regionName, string viewPath) ParseUri(string uri)
        {
            // Format: /Region/View/Id or /View or /Region/View
            var parts = uri.Split('/', StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length == 0)
            {
                return ("ContentRegion", "Home");
            }

            // Check if first part is a known region
            var knownRegions = new[] { "ContentRegion", "MainRegion", "DetailRegion" };
            string regionName, viewPath;

            if (knownRegions.Contains(parts[0], StringComparer.OrdinalIgnoreCase))
            {
                regionName = parts[0];
                viewPath = parts.Length > 1 ? parts[1] : "Home";
            }
            else
            {
                regionName = "ContentRegion";
                viewPath = parts[0];
            }

            return (regionName, viewPath);
        }

        /// <summary>
        /// 创建 URI
        /// </summary>
        private string CreateUri(string regionName, string viewPath, NavigationParameters parameters)
        {
            var uri = $"/{regionName}/{viewPath}";

            // Add ID from parameters if present
            if (parameters != null && parameters.ContainsKey("id"))
            {
                var id = parameters["id"]?.ToString();
                if (!string.IsNullOrEmpty(id))
                {
                    uri += $"/{id}";
                }
            }

            return uri;
        }

        /// <summary>
        /// 创建导航条目
        /// </summary>
        private NavigationEntry CreateNavigationEntry(string uri, string viewPath, NavigationParameters parameters)
        {
            var title = GenerateTitle(uri, viewPath);
            return new NavigationEntry(
                uri,
                title,
                parameters ?? new NavigationParameters(),
                DateTime.UtcNow,
                RegionName: ParseUri(uri).regionName
            );
        }

        /// <summary>
        /// 生成导航标题
        /// </summary>
        private string GenerateTitle(string uri, string viewPath)
        {
            // Map view paths to friendly titles
            return viewPath switch
            {
                "Home" => "主页",
                "MedicalCase" or "MedicalCaseList" => "医案管理",
                "MedicalCaseDetails" => "医案详情",
                "MedicalCaseEdit" => "编辑医案",
                "PatientList" => "患者管理",
                "PatientDetails" => "患者详情",
                "PrescriptionList" => "处方管理",
                "PrescriptionDetails" => "处方详情",
                _ => viewPath // Fallback to view path
            };
        }

        /// <summary>
        /// 导航到指定条目
        /// </summary>
        private async Task<bool> NavigateToEntry(NavigationEntry entry, bool isBack = false, bool isForward = false)
        {
            try
            {
                var (regionName, viewPath) = ParseUri(entry.Uri);
                var success = await NavigateToRegionAsync(regionName, viewPath, entry.Parameters);

                if (success && entry.State != null)
                {
                    // Restore state if available
                    RestoreState(entry.State);
                }

                return success;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Navigate to entry error: {Uri}", entry.Uri);
                return false;
            }
        }

        /// <summary>
        /// 恢复视图状态
        /// </summary>
        private void RestoreState(object state)
        {
            // TODO: Implement state restoration
            // This would restore scroll position, form data, etc.
            _logger.LogDebug("Restoring navigation state");

            // Publish state restoration event
            // _eventAggregator.GetEvent<RestoreStateEvent>().Publish(state);
        }

        /// <summary>
        /// 更新面包屑导航
        /// </summary>
        private void UpdateBreadcrumbs()
        {
            _breadcrumbs.Clear();

            // Generate breadcrumbs from current URI
            if (_currentEntry != null)
            {
                var parts = _currentEntry.Uri.Split('/', StringSplitOptions.RemoveEmptyEntries);
                var accumulatedPath = string.Empty;

                for (int i = 0; i < parts.Length; i++)
                {
                    accumulatedPath += "/" + parts[i];
                    var title = GenerateTitle(accumulatedPath, parts[i]);
                    var isActive = (i == parts.Length - 1);

                    var breadcrumb = new BreadcrumbItem(
                        title,
                        accumulatedPath,
                        isActive,
                        i,
                        NavigateCommand: new DelegateCommand(
                            () => { var _ = NavigateAsync(accumulatedPath); },
                            () => !isActive
                        )
                    );

                    _breadcrumbs.Add(breadcrumb);
                }
            }
        }

        /// <summary>
        /// 更新历史集合
        /// </summary>
        private void UpdateHistoryCollection()
        {
            _historyCollection.Clear();
            foreach (var entry in _history.Reverse())
            {
                _historyCollection.Add(entry);
            }
        }

        /// <summary>
        /// 更新前进集合
        /// </summary>
        private void UpdateForwardCollection()
        {
            _forwardCollection.Clear();
            foreach (var entry in _forwardStack.Reverse())
            {
                _forwardCollection.Add(entry);
            }
        }

        /// <summary>
        /// 获取上下文建议
        /// </summary>
        private List<NavigationSuggestion> GetContextualSuggestions(NavigationEntry currentEntry)
        {
            var suggestions = new List<NavigationSuggestion>();

            // Example: After completing a case, suggest viewing patient history
            if (currentEntry.Uri.Contains("MedicalCase") &&
                currentEntry.Uri.Contains("Complete"))
            {
                suggestions.Add(new NavigationSuggestion(
                    "查看患者历史",
                    "/Patient/History",
                    0.9,
                    "看完诊后通常查看历史",
                    SuggestionType.Contextual
                ));
            }

            // Example: After viewing patient, suggest recent cases
            if (currentEntry.Uri.Contains("Patient") &&
                currentEntry.Uri.Contains("Details"))
            {
                suggestions.Add(new NavigationSuggestion(
                    "历史医案",
                    $"/Patient/{ExtractId(currentEntry)}/Cases",
                    0.8,
                    "查看该患者的历史医案",
                    SuggestionType.Contextual
                ));
            }

            return suggestions;
        }

        /// <summary>
        /// 获取频繁建议
        /// </summary>
        private List<NavigationSuggestion> GetFrequentSuggestions(int count)
        {
            // Count frequency of URIs in history
            var frequency = new Dictionary<string, int>();
            foreach (var entry in _history)
            {
                var baseUri = GetBaseUri(entry.Uri);
                if (frequency.ContainsKey(baseUri))
                    frequency[baseUri]++;
                else
                    frequency[baseUri] = 1;
            }

            // Convert to suggestions
            return frequency
                .OrderByDescending(kvp => kvp.Value)
                .Take(count)
                .Select(kvp => new NavigationSuggestion(
                    GenerateTitle(kvp.Key, kvp.Key),
                    kvp.Key,
                    0.7,
                    $"您经常访问此页面（{kvp.Value} 次）",
                    SuggestionType.Frequent,
                    Frequency: kvp.Value
                ))
                .ToList();
        }

        /// <summary>
        /// 获取最近建议
        /// </summary>
        private List<NavigationSuggestion> GetRecentSuggestions(int count)
        {
            return _history
                .Take(count)
                .Select(entry => new NavigationSuggestion(
                    entry.Title,
                    entry.Uri,
                    0.6,
                    $"最近访问于 {entry.Timestamp:HH:mm}",
                    SuggestionType.Recent
                ))
                .ToList();
        }

        /// <summary>
        /// 提取 ID 从 URI
        /// </summary>
        private string? ExtractId(NavigationEntry entry)
        {
            var parts = entry.Uri.Split('/');
            return parts.Length > 0 ? parts[^1] : null;
        }

        /// <summary>
        /// 获取基础 URI（移除 ID）
        /// </summary>
        private string GetBaseUri(string uri)
        {
            var parts = uri.Split('/');
            if (parts.Length <= 2)
                return uri;

            // Remove last segment (ID)
            return string.Join("/", parts.Take(parts.Length - 1));
        }

        /// <summary>
        /// 订阅导航事件
        /// </summary>
        private void SubscribeToNavigationEvents()
        {
            // TODO: Subscribe to region navigation events
            // This would allow tracking all navigation in the application
        }

        /// <summary>
        /// 发布导航事件
        /// </summary>
        private void PublishNavigationEvent(string uri, NavigationParameters? parameters)
        {
            // TODO: Publish navigation event for other components to react
            // _eventAggregator.GetEvent<NavigatedEvent>().Publish(uri);
        }

        #region Event Invokers

        protected virtual void OnNavigated(NavigatedEventArgs e)
        {
            Navigated?.Invoke(this, e);
        }

        protected virtual void OnNavigationCancelled(NavigationCancelledEventArgs e)
        {
            NavigationCancelled?.Invoke(this, e);
        }

        protected virtual void OnNavigationFailed(NavigationFailedEventArgs e)
        {
            NavigationFailed?.Invoke(this, e);
        }

        #endregion

        #endregion
        partial void OnAnalyticsInitialized();
    }

    #region Navigation Request (for Prism compatibility)

    /// <summary>
    /// 导航请求
    /// </summary>
    public record NavigationRequest(
        string RegionName,
        string Uri,
        NavigationParameters Parameters
    );

    #endregion
}
