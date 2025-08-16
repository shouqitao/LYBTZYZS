using LYBT.Shared.Models.Contracts.Common;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Prism.Navigation.Regions;
using LYBT.Desktop.Workbench.Admin.Services;

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
        private string _currentView;

        public SystemWorkbenchNavigator(IRegionManager regionManager)
        {
            _regionManager = regionManager ?? throw new ArgumentNullException(nameof(regionManager));
        }

        #region ISystemWorkbenchNavigator Implementation

        public async Task NavigateToUsersAsync()
        {
            await NavigateToAsync("UserManagementView");
        }

        public async Task NavigateToPatientsAsync()
        {
            await NavigateToAsync("PatientManagementView");
        }

        public async Task NavigateToHerbsAsync()
        {
            await NavigateToAsync("HerbManagementView");
        }

        public async Task NavigateToFormulasAsync()
        {
            await NavigateToAsync("FormulaManagementView");
        }

        public async Task NavigateToPrescriptionsAsync()
        {
            await NavigateToAsync("PrescriptionManagementView");
        }

        public async Task NavigateToReportsAsync()
        {
            await NavigateToAsync("ReportsView");
        }

        public async Task NavigateToSettingsAsync()
        {
            await NavigateToAsync("SettingsView");
        }

        public async Task NavigateToDashboardAsync()
        {
            await NavigateToAsync("DashboardView");
        }

        #endregion

        #region IWorkbenchNavigator Implementation

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

        public Task NavigateToDefaultAsync()
        {
            return NavigateToUsersAsync(); // 默认导航到用户管理
        }

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

        public string GetCurrentView()
        {
            return _currentView;
        }

        public void ClearHistory()
        {
            _navigationHistory.Clear();
            _currentView = null;
        }

        public void SetRegion(string regionName)
        {
            _contentRegion = regionName;
        }

        public string GetRegionName()
        {
            return _contentRegion;
        }

        #endregion

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

        #endregion
    }
}