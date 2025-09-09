using Prism.Regions;

namespace LYBT.Desktop.Workbench.Admin.Services
{

    /// <summary>
    /// 系统管理工作台导航服务实现
    /// </summary>
    public class SystemWorkbenchNavigator : ISystemWorkbenchNavigator
    {
        private readonly IRegionManager _regionManager;
        private string _contentRegion = "SystemWorkbenchContent";
        private readonly Stack<string> _navigationHistory = new Stack<string>();
        private string? _currentView;

        public SystemWorkbenchNavigator(IRegionManager regionManager)
        {
            _regionManager = regionManager ?? throw new ArgumentNullException(nameof(regionManager));
        }

        #region ISystemWorkbenchNavigator Implementation

        /// <inheritdoc/>
        public async Task NavigateToUsersAsync()
        {
            await NavigateToAsync("UserManagementView");
        }

        /// <inheritdoc/>
        public async Task NavigateToPatientsAsync()
        {
            await NavigateToAsync("PatientManagementView");
        }

        /// <inheritdoc/>
        public async Task NavigateToHerbsAsync()
        {
            await NavigateToAsync("HerbManagementView");
        }

        /// <inheritdoc/>
        public async Task NavigateToFormulasAsync()
        {
            await NavigateToAsync("FormulaManagementView");
        }

        /// <inheritdoc/>
        public async Task NavigateToPrescriptionsAsync()
        {
            await NavigateToAsync("PrescriptionManagementView");
        }

        /// <inheritdoc/>
        public async Task NavigateToReportsAsync()
        {
            await NavigateToAsync("ReportsView");
        }

        /// <inheritdoc/>
        public async Task NavigateToSettingsAsync()
        {
            await NavigateToAsync("SettingsView");
        }

        /// <inheritdoc/>
        public async Task NavigateToDashboardAsync()
        {
            await NavigateToAsync("DashboardView");
        }

        #endregion ISystemWorkbenchNavigator Implementation

        #region IWorkbenchNavigator Implementation

        /// <inheritdoc/>
        public Task NavigateToAsync(string viewName, NavigationParameters? parameters = null)
        {
            return Task.Run(() =>
            {
                if (!string.IsNullOrEmpty(_currentView))
                {
                    _navigationHistory.Push(_currentView);
                }

                _currentView = viewName;
                _regionManager.RequestNavigate(_contentRegion, viewName, parameters);
            });
        }

        /// <inheritdoc/>
        public Task NavigateToDefaultAsync()
        {
            return NavigateToUsersAsync(); // 默认导航到用户管理
        }

        /// <inheritdoc/>
        public Task GoBackAsync()
        {
            return Task.Run(() =>
            {
                if (_navigationHistory.Count > 0)
                {
                    var previousView = _navigationHistory.Pop();
                    _currentView = previousView;
                    _regionManager.RequestNavigate(_contentRegion, previousView);
                }
            });
        }

        /// <inheritdoc/>
        public bool CanNavigateTo(string viewName)
        {
            // 检查视图是否在可用视图列表中
            var availableViews = new[]
            {
                "UserManagementView",
                "PatientManagementView",
                "HerbManagementView",
                "FormulaManagementView",
                "PrescriptionManagementView",
                "ReportsView",
                "SettingsView",
                "DashboardView"
            };

            return Array.Exists(availableViews, v => v.Equals(viewName, StringComparison.OrdinalIgnoreCase));
        }

        /// <inheritdoc/>
        public string GetCurrentView()
        {
            return _currentView ?? string.Empty;
        }

        /// <inheritdoc/>
        public void ClearHistory()
        {
            _navigationHistory.Clear();
            _currentView = null;
        }

        /// <inheritdoc/>
        public void SetRegion(string regionName)
        {
            _contentRegion = regionName;
        }

        /// <inheritdoc/>
        public string GetRegionName()
        {
            return _contentRegion;
        }

        #endregion IWorkbenchNavigator Implementation

        #region Legacy Methods (for backward compatibility)

        public void NavigateToUsers()
        {
            NavigateToUsersAsync().Wait();
        }

        public void NavigateToPatients()
        {
            NavigateToPatientsAsync().Wait();
        }

        public void NavigateToHerbs()
        {
            NavigateToHerbsAsync().Wait();
        }

        public void NavigateToFormulas()
        {
            NavigateToFormulasAsync().Wait();
        }

        public void NavigateToPrescriptions()
        {
            NavigateToPrescriptionsAsync().Wait();
        }

        public void NavigateToReports()
        {
            NavigateToReportsAsync().Wait();
        }

        public void NavigateToSettings()
        {
            NavigateToSettingsAsync().Wait();
        }

        public void NavigateToView(string viewName, NavigationParameters? parameters = null)
        {
            NavigateToAsync(viewName, parameters).Wait();
        }

        public void NavigateToView(string regionName, string viewName, NavigationParameters? parameters = null)
        {
            _regionManager.RequestNavigate(regionName, viewName, parameters);
        }

        #endregion Legacy Methods (for backward compatibility)
    }
}
