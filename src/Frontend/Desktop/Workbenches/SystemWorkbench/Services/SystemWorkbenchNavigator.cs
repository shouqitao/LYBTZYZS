using System;
using Prism.Regions;
using LYBT.WPF.Client.Workbenches.SystemWorkbench.Navigation;

namespace LYBT.WPF.Client.Workbenches.SystemWorkbench.Services
{
    /// <summary>
    /// 系统管理工作台导航服务实现
    /// </summary>
    public class SystemWorkbenchNavigator : ISystemWorkbenchNavigator
    {
        private readonly IRegionManager _regionManager;
        private const string ContentRegion = "SystemWorkbenchContent";

        public SystemWorkbenchNavigator(IRegionManager regionManager)
        {
            _regionManager = regionManager ?? throw new ArgumentNullException(nameof(regionManager));
        }

        public void NavigateToUsers()
        {
            NavigateToView("UserManagementView");
        }

        public void NavigateToPatients()
        {
            NavigateToView("PatientManagementView");
        }

        public void NavigateToHerbs()
        {
            NavigateToView("HerbManagementView");
        }

        public void NavigateToFormulas()
        {
            NavigateToView("FormulaManagementView");
        }

        public void NavigateToPrescriptions()
        {
            NavigateToView("PrescriptionManagementView");
        }

        public void NavigateToReports()
        {
            NavigateToView("ReportsView");
        }

        public void NavigateToSettings()
        {
            NavigateToView("SettingsView");
        }

        public void NavigateToView(string viewName, NavigationParameters parameters = null)
        {
            _regionManager.RequestNavigate(ContentRegion, viewName, parameters);
        }

        public void NavigateToView(string regionName, string viewName, NavigationParameters parameters = null)
        {
            _regionManager.RequestNavigate(regionName, viewName, parameters);
        }
    }
}