using Prism.Commands;
using Prism.Mvvm;
using Prism.Navigation.Regions;
using System.Windows;

namespace LYBT.WPF.Client.Modules.SystemManagement.ViewModels
{
    /// <summary>
    /// 系统管理主界面视图模型
    /// </summary>
    public class SystemManagementViewModel : BindableBase
    {
        private readonly IRegionManager _regionManager;

        public DelegateCommand NavigateToUserManagementCommand { get; }
        public DelegateCommand NavigateToRoleManagementCommand { get; }
        public DelegateCommand NavigateToSystemSettingsCommand { get; }
        public DelegateCommand NavigateToBackupCommand { get; }
        public DelegateCommand NavigateToSystemLogsCommand { get; }
        public DelegateCommand NavigateToHerbManagementCommand { get; }
        public DelegateCommand NavigateToPrescriptionTemplatesCommand { get; }

        public SystemManagementViewModel(IRegionManager regionManager)
        {
            _regionManager = regionManager;

            // 初始化命令
            NavigateToUserManagementCommand = new DelegateCommand(ExecuteNavigateToUserManagement);
            NavigateToRoleManagementCommand = new DelegateCommand(ExecuteNavigateToRoleManagement);
            NavigateToSystemSettingsCommand = new DelegateCommand(ExecuteNavigateToSystemSettings);
            NavigateToBackupCommand = new DelegateCommand(ExecuteNavigateToBackup);
            NavigateToSystemLogsCommand = new DelegateCommand(ExecuteNavigateToSystemLogs);
            NavigateToHerbManagementCommand = new DelegateCommand(ExecuteNavigateToHerbManagement);
            NavigateToPrescriptionTemplatesCommand = new DelegateCommand(ExecuteNavigateToPrescriptionTemplates);

            // 默认导航到用户管理
            ExecuteNavigateToUserManagement();
        }

        private void ExecuteNavigateToUserManagement()
        {
            _regionManager.RequestNavigate("SystemManagementContentRegion", "UserManagementView");
        }

        private void ExecuteNavigateToRoleManagement()
        {
            MessageBox.Show("角色权限管理功能正在开发中...", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ExecuteNavigateToSystemSettings()
        {
            MessageBox.Show("系统设置功能正在开发中...", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ExecuteNavigateToBackup()
        {
            MessageBox.Show("数据备份功能正在开发中...", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ExecuteNavigateToSystemLogs()
        {
            MessageBox.Show("系统日志功能正在开发中...", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ExecuteNavigateToHerbManagement()
        {
            _regionManager.RequestNavigate("SystemManagementContentRegion", "HerbManagementView");
        }

        private void ExecuteNavigateToPrescriptionTemplates()
        {
            _regionManager.RequestNavigate("SystemManagementContentRegion", "PrescriptionTemplateManagementView");
        }
    }
}