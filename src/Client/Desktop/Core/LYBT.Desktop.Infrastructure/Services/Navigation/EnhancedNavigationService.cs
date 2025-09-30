using Microsoft.Extensions.Logging;
using Prism.Regions;

namespace LYBT.Desktop.Services.Navigation
{
    /// <summary>
    /// 增强导航服务接口
    /// </summary>
    public interface IEnhancedNavigationService
    {
        Task<bool> NavigateAsync(string regionName, string viewName, NavigationParameters? parameters = null);
        Task<bool> NavigateBackAsync(string regionName);
        bool CanNavigateBack(string regionName);
        void ClearHistory(string regionName);
        string GetCurrentView(string regionName);
    }

    /// <summary>
    /// 增强导航服务实现 - UltraThink架构
    /// </summary>
    public class EnhancedNavigationService : IEnhancedNavigationService
    {
        private readonly IRegionManager _regionManager;
        private readonly ILogger<EnhancedNavigationService> _logger;
        private readonly Dictionary<string, string> _currentViews = new();

        public EnhancedNavigationService(
            IRegionManager regionManager,
            ILogger<EnhancedNavigationService> logger)
        {
            _regionManager = regionManager ?? throw new ArgumentNullException(nameof(regionManager));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<bool> NavigateAsync(string regionName, string viewName, NavigationParameters? parameters = null)
        {
            try
            {
                await Task.Run(() =>
                {
                    var region = _regionManager.Regions[regionName];
                    region.RequestNavigate(viewName, parameters ?? new NavigationParameters());
                    _currentViews[regionName] = viewName;
                });

                _logger.LogInformation("导航成功：{Region} -> {View}", regionName, viewName);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "导航失败：{Region} -> {View}", regionName, viewName);
                return false;
            }
        }

        public async Task<bool> NavigateBackAsync(string regionName)
        {
            try
            {
                await Task.Run(() =>
                {
                    var journal = _regionManager.Regions[regionName].NavigationService.Journal;
                    if (journal.CanGoBack)
                    {
                        journal.GoBack();
                    }
                });

                _logger.LogInformation("导航返回：{Region}", regionName);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "导航返回失败：{Region}", regionName);
                return false;
            }
        }

        public bool CanNavigateBack(string regionName)
        {
            try
            {
                return _regionManager.Regions[regionName].NavigationService.Journal.CanGoBack;
            }
            catch
            {
                return false;
            }
        }

        public void ClearHistory(string regionName)
        {
            try
            {
                _regionManager.Regions[regionName].NavigationService.Journal.Clear();
                _logger.LogInformation("清除导航历史：{Region}", regionName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "清除导航历史失败：{Region}", regionName);
            }
        }

        public string GetCurrentView(string regionName)
        {
            return _currentViews.TryGetValue(regionName, out var view) ? view : string.Empty;
        }
    }
}
