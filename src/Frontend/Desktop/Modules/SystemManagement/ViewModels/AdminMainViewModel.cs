using System;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Navigation.Regions;

namespace LYBT.WPF.Client.Modules.SystemManagement.ViewModels
{
    /// <summary>
    /// 系统管理主界面视图模型
    /// </summary>
    public class AdminMainViewModel : BindableBase
    {
        private readonly IRegionManager _regionManager;

        public DelegateCommand NavigateToUserManagementCommand { get; }
        public DelegateCommand NavigateToPatientManagementCommand { get; }
        public DelegateCommand NavigateToMedicalCaseManagementCommand { get; }
        public DelegateCommand NavigateToConsultationManagementCommand { get; }
        public DelegateCommand NavigateToRoleManagementCommand { get; }
        public DelegateCommand NavigateToSystemSettingsCommand { get; }
        public DelegateCommand NavigateToBackupCommand { get; }
        public DelegateCommand NavigateToSystemLogsCommand { get; }
        public DelegateCommand NavigateToHerbManagementCommand { get; }
        public DelegateCommand NavigateToPrescriptionTemplatesCommand { get; }
        public DelegateCommand NavigateToFormulaManagementCommand { get; }
        public DelegateCommand NavigateToPrescriptionManagementCommand { get; }

        public AdminMainViewModel(IRegionManager regionManager)
        {
            _regionManager = regionManager;

            // 初始化导航命令
            NavigateToUserManagementCommand = new DelegateCommand(() => NavigateTo("UserManagementView"));
            NavigateToPatientManagementCommand = new DelegateCommand(() => NavigateTo("PatientManagementView"));
            NavigateToMedicalCaseManagementCommand = new DelegateCommand(() => NavigateTo("MedicalCaseManagementView"));
            NavigateToConsultationManagementCommand = new DelegateCommand(() => NavigateTo("ConsultationManagementView"));
            NavigateToRoleManagementCommand = new DelegateCommand(() => NavigateTo("RoleManagementView"));
            NavigateToSystemSettingsCommand = new DelegateCommand(() => NavigateTo("SystemSettingsView"));
            NavigateToBackupCommand = new DelegateCommand(() => NavigateTo("BackupView"));
            NavigateToSystemLogsCommand = new DelegateCommand(() => NavigateTo("SystemLogsView"));
            NavigateToHerbManagementCommand = new DelegateCommand(() => NavigateTo("HerbManagementView"));
            NavigateToPrescriptionTemplatesCommand = new DelegateCommand(() => NavigateTo("PrescriptionTemplatesView"));
            NavigateToFormulaManagementCommand = new DelegateCommand(() => NavigateTo("FormulaManagementView"));
            NavigateToPrescriptionManagementCommand = new DelegateCommand(() => NavigateTo("PrescriptionManagementView"));

            // 默认导航到用户管理
            NavigateTo("UserManagementView");
        }

        private void NavigateTo(string viewName)
        {
            try
            {
                _regionManager.RequestNavigate("SystemManagementContentRegion", viewName);
            }
            catch (Exception ex)
            {
                // 如果视图不存在，显示占位界面
            }
        }
    }
}