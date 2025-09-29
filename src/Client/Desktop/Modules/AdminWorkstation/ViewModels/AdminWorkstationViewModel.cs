using LYBT.Desktop.Core.ViewModels.Base;
using LYBT.Desktop.Core.Events;
using LYBT.Desktop.Core.Interfaces.Services;
using Microsoft.Extensions.Logging;
using Prism.Commands;
using Prism.Events;
using Prism.Regions;
using System.Windows.Input;

namespace LYBT.Desktop.AdminWorkstation.ViewModels
{
    /// <summary>
    /// 管理工作台视图模型
    /// </summary>
    public class AdminWorkstationViewModel : ModernViewModelBase
    {
        private readonly IRegionManager _regionManager;
        private string _currentUserName = string.Empty;

        // 导航选中状态
        private bool _isUserManagementSelected = true;
        private bool _isHerbManagementSelected;
        private bool _isPatientManagementSelected;
        private bool _isFormulaManagementSelected;
        private bool _isMedicalCaseManagementSelected;
        private bool _isSystemSettingsSelected;

        public AdminWorkstationViewModel(
            IRegionManager regionManager,
            IEventAggregator eventAggregator,
            ILoggerFactory loggerFactory,
            IErrorHandlingService errorHandlingService)
            : base(eventAggregator, loggerFactory, errorHandlingService)
        {
            _regionManager = regionManager;

            // 初始化命令
            NavigateCommand = new DelegateCommand<string>(ExecuteNavigate);
            LogoutCommand = new DelegateCommand(ExecuteLogout);

            // 订阅登录成功事件
            EventAggregator.GetEvent<UserLoggedInEvent>().Subscribe(OnUserLoggedIn);

            // 默认导航到用户管理
            ExecuteNavigate("UserManagement");
        }

        #region Properties

        public string CurrentUserName
        {
            get => _currentUserName;
            set => SetProperty(ref _currentUserName, value);
        }

        public bool IsUserManagementSelected
        {
            get => _isUserManagementSelected;
            set => SetProperty(ref _isUserManagementSelected, value);
        }

        public bool IsHerbManagementSelected
        {
            get => _isHerbManagementSelected;
            set => SetProperty(ref _isHerbManagementSelected, value);
        }

        public bool IsPatientManagementSelected
        {
            get => _isPatientManagementSelected;
            set => SetProperty(ref _isPatientManagementSelected, value);
        }

        public bool IsFormulaManagementSelected
        {
            get => _isFormulaManagementSelected;
            set => SetProperty(ref _isFormulaManagementSelected, value);
        }

        public bool IsMedicalCaseManagementSelected
        {
            get => _isMedicalCaseManagementSelected;
            set => SetProperty(ref _isMedicalCaseManagementSelected, value);
        }

        public bool IsSystemSettingsSelected
        {
            get => _isSystemSettingsSelected;
            set => SetProperty(ref _isSystemSettingsSelected, value);
        }

        #endregion

        #region Commands

        public ICommand NavigateCommand { get; }
        public ICommand LogoutCommand { get; }

        #endregion

        #region Methods

        private void ExecuteNavigate(string targetView)
        {
            try
            {
                Logger.LogInformation($"导航到管理模块：{targetView}");

                // 更新选中状态
                UpdateSelectionState(targetView);

                // 导航到对应的视图
                string viewName = targetView switch
                {
                    "UserManagement" => "UserManagementView",
                    "HerbManagement" => "HerbManagementView",
                    "PatientManagement" => "PatientManagementView",
                    "FormulaManagement" => "FormulaManagementView",
                    "MedicalCaseManagement" => "MedicalCaseManagementView",
                    "SystemSettings" => "SystemSettingsView",
                    _ => "UserManagementView"
                };

                _regionManager.RequestNavigate("AdminContentRegion", viewName);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, $"导航到{targetView}失败");
                ShowErrorMessage($"导航失败：{ex.Message}");
            }
        }

        private void UpdateSelectionState(string selectedModule)
        {
            // 重置所有选中状态
            IsUserManagementSelected = false;
            IsHerbManagementSelected = false;
            IsPatientManagementSelected = false;
            IsFormulaManagementSelected = false;
            IsMedicalCaseManagementSelected = false;
            IsSystemSettingsSelected = false;

            // 设置选中状态
            switch (selectedModule)
            {
                case "UserManagement":
                    IsUserManagementSelected = true;
                    break;
                case "HerbManagement":
                    IsHerbManagementSelected = true;
                    break;
                case "PatientManagement":
                    IsPatientManagementSelected = true;
                    break;
                case "FormulaManagement":
                    IsFormulaManagementSelected = true;
                    break;
                case "MedicalCaseManagement":
                    IsMedicalCaseManagementSelected = true;
                    break;
                case "SystemSettings":
                    IsSystemSettingsSelected = true;
                    break;
            }
        }

        private void ExecuteLogout()
        {
            try
            {
                Logger.LogInformation("用户请求退出登录");

                // 发布登出事件
                EventAggregator.GetEvent<UserLoggedOutEvent>().Publish();

                // 导航回登录界面
                _regionManager.RequestNavigate("ContentRegion", "LoginView");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "退出登录失败");
                ShowErrorMessage($"退出登录失败：{ex.Message}");
            }
        }

        private void OnUserLoggedIn(UserLoggedInEventArgs args)
        {
            CurrentUserName = args.Username;
            Logger.LogInformation($"管理员 {args.Username} 已登录");
        }

        #endregion
    }
}