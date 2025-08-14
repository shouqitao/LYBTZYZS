using Prism.Commands;
using Prism.Mvvm;
using Prism.Navigation.Regions;
using System.Windows;

using LYBT.Desktop.Core.Interfaces.Services;
using Prism.Dialogs;
using LYBT.Desktop.Core.Extensions;
namespace LYBT.Desktop.Admin.ViewModels
{
    /// <summary>
    /// 系统管理主界面视图模型
    /// </summary>
    public class SystemManagementViewModel : BindableBase
    {
        private readonly IDialogService _commonDialogService;

        private readonly IRegionManager _regionManager;

        public DelegateCommand NavigateToUserManagementCommand { get; }
        public DelegateCommand NavigateToRoleManagementCommand { get; }
        public DelegateCommand NavigateToSystemSettingsCommand { get; }
        public DelegateCommand NavigateToBackupCommand { get; }
        public DelegateCommand NavigateToSystemLogsCommand { get; }
        public DelegateCommand NavigateToHerbManagementCommand { get; }
        public DelegateCommand NavigateToPrescriptionTemplatesCommand { get; }
        public DelegateCommand NavigateToPrescriptionManagementCommand { get; }
        public DelegateCommand NavigateToPatientManagementCommand { get; }
        public DelegateCommand NavigateToRecordManagementCommand { get; }

        public SystemManagementViewModel(IRegionManager regionManager,
            IDialogService commonDialogService)
        {
            _commonDialogService = commonDialogService;
            _regionManager = regionManager;

            // 初始化命令
            NavigateToUserManagementCommand = new DelegateCommand(ExecuteNavigateToUserManagement);
            NavigateToRoleManagementCommand = new DelegateCommand(ExecuteNavigateToRoleManagement);
            NavigateToSystemSettingsCommand = new DelegateCommand(ExecuteNavigateToSystemSettings);
            NavigateToBackupCommand = new DelegateCommand(ExecuteNavigateToBackup);
            NavigateToSystemLogsCommand = new DelegateCommand(ExecuteNavigateToSystemLogs);
            NavigateToHerbManagementCommand = new DelegateCommand(ExecuteNavigateToHerbManagement);
            NavigateToPrescriptionTemplatesCommand = new DelegateCommand(ExecuteNavigateToPrescriptionTemplates);
            NavigateToPrescriptionManagementCommand = new DelegateCommand(ExecuteNavigateToPrescriptionManagement);
            NavigateToPatientManagementCommand = new DelegateCommand(ExecuteNavigateToPatientManagement);
            NavigateToRecordManagementCommand = new DelegateCommand(ExecuteNavigateToRecordManagement);

            // 默认导航到用户管理
            ExecuteNavigateToUserManagement();
        }

        private void ExecuteNavigateToUserManagement()
        {
            _regionManager.RequestNavigate("SystemManagementContentRegion", "UserManagementView");
        }

        private void ExecuteNavigateToRoleManagement()
        {
            _commonDialogService.ShowInformationAsync("角色权限管理功能正在开发中...", "提示").GetAwaiter().GetResult();
        }

        private void ExecuteNavigateToSystemSettings()
        {
            _commonDialogService.ShowInformationAsync("系统设置功能正在开发中...", "提示").GetAwaiter().GetResult();
        }

        private void ExecuteNavigateToBackup()
        {
            _commonDialogService.ShowInformationAsync("数据备份功能正在开发中...", "提示").GetAwaiter().GetResult();
        }

        private void ExecuteNavigateToSystemLogs()
        {
            _commonDialogService.ShowInformationAsync("系统日志功能正在开发中...", "提示").GetAwaiter().GetResult();
        }

        private void ExecuteNavigateToHerbManagement()
        {
            _regionManager.RequestNavigate("SystemManagementContentRegion", "HerbManagementView");
        }

        private void ExecuteNavigateToPrescriptionTemplates()
        {
            _regionManager.RequestNavigate("SystemManagementContentRegion", "PrescriptionTemplateManagementView");
        }

        private void ExecuteNavigateToPrescriptionManagement()
        {
            _regionManager.RequestNavigate("SystemManagementContentRegion", "PrescriptionManagementView");
        }

        private void ExecuteNavigateToPatientManagement()
        {
            _regionManager.RequestNavigate("SystemManagementContentRegion", "PatientManagementView");
        }

        private void ExecuteNavigateToRecordManagement()
        {
            _regionManager.RequestNavigate("SystemManagementContentRegion", "RecordManagementView");
        }
    }
}