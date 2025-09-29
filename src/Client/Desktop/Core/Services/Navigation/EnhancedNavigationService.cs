using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Prism.Regions;
using LYBT.Desktop.Core.Interfaces.Navigation;

namespace LYBT.Desktop.Core.Services.Navigation
{
    /// <summary>
    /// 增强导航服务实现
    /// 提供NavigationJournal支持，实现前进/后退功能
    /// </summary>
    public class EnhancedNavigationService : IEnhancedNavigationService
    {
        private readonly IRegionManager _regionManager;
        private readonly ILogger<EnhancedNavigationService> _logger;
        private readonly Dictionary<string, Stack<NavigationHistoryItem>> _navigationHistory;
        private readonly Dictionary<string, Stack<NavigationHistoryItem>> _forwardHistory;

        public event EventHandler<LYBT.Desktop.Core.Interfaces.Navigation.NavigatingEventArgs>? Navigating;
        public event EventHandler<LYBT.Desktop.Core.Interfaces.Navigation.NavigatedEventArgs>? Navigated;
        public event EventHandler<LYBT.Desktop.Core.Interfaces.Navigation.NavigationFailedEventArgs>? NavigationFailed;

        public EnhancedNavigationService(
            IRegionManager regionManager,
            ILogger<EnhancedNavigationService> logger)
        {
            _regionManager = regionManager ?? throw new ArgumentNullException(nameof(regionManager));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _navigationHistory = new Dictionary<string, Stack<NavigationHistoryItem>>();
            _forwardHistory = new Dictionary<string, Stack<NavigationHistoryItem>>();
        }

        #region 导航操作

        public async Task<LYBT.Desktop.Core.Interfaces.Navigation.NavigationResult> NavigateAsync(string regionName, string viewName, NavigationParameters? parameters = null)
        {
            try
            {
                // 触发导航开始事件
                var navigatingArgs = new LYBT.Desktop.Core.Interfaces.Navigation.NavigatingEventArgs
                {
                    RegionName = regionName,
                    ViewName = viewName,
                    Parameters = parameters
                };
                Navigating?.Invoke(this, navigatingArgs);

                if (navigatingArgs.Cancel)
                {
                    _logger.LogInformation("导航被取消: {RegionName} -> {ViewName}", regionName, viewName);
                    return new LYBT.Desktop.Core.Interfaces.Navigation.NavigationResult { Success = false };
                }

                // 保存当前视图到历史
                SaveCurrentViewToHistory(regionName);

                // 清空前进历史（因为有了新的导航）
                ClearForwardHistory(regionName);

                // 执行导航
                var navigationResult = await Task.Run(() =>
                {
                    var result = new LYBT.Desktop.Core.Interfaces.Navigation.NavigationResult();
                    _regionManager.RequestNavigate(regionName, viewName,
                        navigationCallback =>
                        {
                            result.Success = navigationCallback.Result ?? false;
                            result.Error = navigationCallback.Error;
                        },
                        parameters);
                    return result;
                });

                // 触发导航完成事件
                var navigatedArgs = new LYBT.Desktop.Core.Interfaces.Navigation.NavigatedEventArgs
                {
                    RegionName = regionName,
                    ViewName = viewName,
                    Parameters = parameters,
                    Result = navigationResult
                };
                Navigated?.Invoke(this, navigatedArgs);

                if (navigationResult.Success)
                {
                    _logger.LogInformation("导航成功: {RegionName} -> {ViewName}", regionName, viewName);
                }
                else
                {
                    _logger.LogWarning("导航失败: {RegionName} -> {ViewName}", regionName, viewName);
                    NavigationFailed?.Invoke(this, new LYBT.Desktop.Core.Interfaces.Navigation.NavigationFailedEventArgs
                    {
                        RegionName = regionName,
                        ViewName = viewName,
                        Error = navigationResult.Error,
                        ErrorMessage = navigationResult.Error?.Message ?? "未知错误"
                    });
                }

                return navigationResult;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "导航异常: {RegionName} -> {ViewName}", regionName, viewName);
                NavigationFailed?.Invoke(this, new LYBT.Desktop.Core.Interfaces.Navigation.NavigationFailedEventArgs
                {
                    RegionName = regionName,
                    ViewName = viewName,
                    Error = ex,
                    ErrorMessage = ex.Message
                });
                throw;
            }
        }

        public void Navigate(string regionName, string viewName, NavigationParameters? parameters = null)
        {
            NavigateAsync(regionName, viewName, parameters).GetAwaiter().GetResult();
        }

        public bool GoBack(string regionName)
        {
            try
            {
                if (!CanGoBack(regionName))
                    return false;

                // 保存当前视图到前进历史
                var currentView = GetCurrentView(regionName);
                if (currentView != null)
                {
                    if (!_forwardHistory.ContainsKey(regionName))
                        _forwardHistory[regionName] = new Stack<NavigationHistoryItem>();

                    _forwardHistory[regionName].Push(new NavigationHistoryItem
                    {
                        ViewName = currentView,
                        Timestamp = DateTime.Now
                    });
                }

                // 从历史中获取上一个视图
                var historyItem = _navigationHistory[regionName].Pop();

                // 导航到历史视图
                Navigate(regionName, historyItem.ViewName, historyItem.Parameters);

                _logger.LogInformation("后退导航成功: {RegionName} -> {ViewName}", regionName, historyItem.ViewName);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "后退导航失败: {RegionName}", regionName);
                return false;
            }
        }

        public async Task<bool> GoBackAsync(string regionName)
        {
            return await Task.Run(() => GoBack(regionName));
        }

        public bool GoForward(string regionName)
        {
            try
            {
                if (!CanGoForward(regionName))
                    return false;

                // 保存当前视图到历史
                SaveCurrentViewToHistory(regionName);

                // 从前进历史中获取视图
                var historyItem = _forwardHistory[regionName].Pop();

                // 导航到前进视图
                Navigate(regionName, historyItem.ViewName, historyItem.Parameters);

                _logger.LogInformation("前进导航成功: {RegionName} -> {ViewName}", regionName, historyItem.ViewName);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "前进导航失败: {RegionName}", regionName);
                return false;
            }
        }

        public async Task<bool> GoForwardAsync(string regionName)
        {
            return await Task.Run(() => GoForward(regionName));
        }

        #endregion

        #region 导航状态

        public bool CanGoBack(string regionName)
        {
            return _navigationHistory.ContainsKey(regionName) &&
                   _navigationHistory[regionName].Count > 0;
        }

        public bool CanGoForward(string regionName)
        {
            return _forwardHistory.ContainsKey(regionName) &&
                   _forwardHistory[regionName].Count > 0;
        }

        public string? GetCurrentView(string regionName)
        {
            try
            {
                var region = _regionManager.Regions[regionName];
                var activeView = region.ActiveViews.FirstOrDefault();
                return activeView?.GetType().Name;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取当前视图失败: {RegionName}", regionName);
                return null;
            }
        }

        public IRegionNavigationJournal? GetNavigationJournal(string regionName)
        {
            try
            {
                var region = _regionManager.Regions[regionName];
                return region.NavigationService.Journal;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取导航日志失败: {RegionName}", regionName);
                return null;
            }
        }

        #endregion

        #region 导航管理

        public void ClearHistory(string regionName)
        {
            if (_navigationHistory.ContainsKey(regionName))
            {
                _navigationHistory[regionName].Clear();
            }
            if (_forwardHistory.ContainsKey(regionName))
            {
                _forwardHistory[regionName].Clear();
            }
            _logger.LogInformation("清除导航历史: {RegionName}", regionName);
        }

        public void ClearAllHistory()
        {
            _navigationHistory.Clear();
            _forwardHistory.Clear();
            _logger.LogInformation("清除所有导航历史");
        }

        public bool RemoveView(string regionName, string viewName)
        {
            try
            {
                var region = _regionManager.Regions[regionName];
                var view = region.Views.FirstOrDefault(v => v.GetType().Name == viewName);
                if (view != null)
                {
                    region.Remove(view);
                    _logger.LogInformation("移除视图: {RegionName}/{ViewName}", regionName, viewName);
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "移除视图失败: {RegionName}/{ViewName}", regionName, viewName);
                return false;
            }
        }

        public bool IsViewLoaded(string regionName, string viewName)
        {
            try
            {
                var region = _regionManager.Regions[regionName];
                return region.Views.Any(v => v.GetType().Name == viewName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "检查视图是否加载失败: {RegionName}/{ViewName}", regionName, viewName);
                return false;
            }
        }

        #endregion

        #region 私有方法

        private void SaveCurrentViewToHistory(string regionName)
        {
            var currentView = GetCurrentView(regionName);
            if (currentView != null)
            {
                if (!_navigationHistory.ContainsKey(regionName))
                    _navigationHistory[regionName] = new Stack<NavigationHistoryItem>();

                _navigationHistory[regionName].Push(new NavigationHistoryItem
                {
                    ViewName = currentView,
                    Timestamp = DateTime.Now
                });
            }
        }

        private void ClearForwardHistory(string regionName)
        {
            if (_forwardHistory.ContainsKey(regionName))
            {
                _forwardHistory[regionName].Clear();
            }
        }

        #endregion

        #region 内部类

        private class NavigationHistoryItem
        {
            public string ViewName { get; set; } = string.Empty;
            public NavigationParameters? Parameters { get; set; }
            public DateTime Timestamp { get; set; }
        }


        #endregion
    }
}