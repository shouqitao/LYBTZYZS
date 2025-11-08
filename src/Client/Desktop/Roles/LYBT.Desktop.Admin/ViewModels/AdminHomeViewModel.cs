using LYBT.Desktop.Foundation.Security;
using LYBT.Desktop.Models.ViewModels.Base;
using Microsoft.Extensions.Logging;
using Prism.Commands;
using Prism.Events;
using Prism.Regions;
using Prism.Services.Dialogs;

namespace LYBT.Desktop.Admin.ViewModels
{
    /// <summary>
    /// 管理员工作台主页视图模型
    /// 核心功能：6个功能卡片导航 + 修改密码
    /// Issue #1553: 角色模块化重构 - Admin模块
    /// Issue #1892: 添加系统管理员修改密码功能
    /// </summary>
    public class AdminHomeViewModel : UnifiedViewModelBase
    {
        #region 依赖服务

        private readonly IRegionManager _regionManager;
        private readonly IAuthenticationService _authService;
        private readonly IDialogService _dialogService;

        #endregion 依赖服务

        #region 属性

        private string _currentUserName = "系统管理员";

        /// <summary>
        /// 当前用户名
        /// </summary>
        public string CurrentUserName
        {
            get => _currentUserName;
            set => SetProperty(ref _currentUserName, value);
        }

        private bool _isSysAdmin = true; // 默认为true，避免按钮闪现
        /// <summary>
        /// 是否为系统管理员 (Issue #1887-1892)
        /// </summary>
        public bool IsSysAdmin
        {
            get => _isSysAdmin;
            set
            {
                if (SetProperty(ref _isSysAdmin, value))
                {
                    RaisePropertyChanged(nameof(IsNotSysAdmin));
                }
            }
        }

        /// <summary>
        /// 是否不是系统管理员（用于UI可见性绑定）
        /// </summary>
        public bool IsNotSysAdmin => !IsSysAdmin;

        #endregion 属性

        #region 命令

        /// <summary>
        /// 导航到用户管理
        /// </summary>
        public DelegateCommand NavigateToUserManagementCommand { get; }

        /// <summary>
        /// 导航到药材管理
        /// </summary>
        public DelegateCommand NavigateToHerbManagementCommand { get; }

        /// <summary>
        /// 导航到患者管理
        /// </summary>
        public DelegateCommand NavigateToPatientManagementCommand { get; }

        /// <summary>
        /// 导航到验方管理
        /// </summary>
        public DelegateCommand NavigateToFormulaManagementCommand { get; }

        /// <summary>
        /// 导航到病历管理
        /// </summary>
        public DelegateCommand NavigateToMedicalCaseManagementCommand { get; }

        /// <summary>
        /// 导航到系统设置
        /// </summary>
        public DelegateCommand NavigateToSystemSettingsCommand { get; }

        /// <summary>
        /// 修改个人信息命令 (Issue #1887-1892)
        /// </summary>
        public DelegateCommand EditProfileCommand { get; }

        /// <summary>
        /// 修改密码命令 (Issue #1887-1892)
        /// </summary>
        public DelegateCommand ChangePasswordCommand { get; }

        #endregion 命令

        #region 构造函数

        public AdminHomeViewModel(
            IRegionManager regionManager,
            IEventAggregator eventAggregator,
            ILoggerFactory loggerFactory,
            IAuthenticationService authService,
            IDialogService dialogService)
            : base(eventAggregator, loggerFactory, regionManager)
        {
            _regionManager = regionManager ?? throw new ArgumentNullException(nameof(regionManager));
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));

            // 初始化导航命令
            NavigateToUserManagementCommand = new DelegateCommand(() => NavigateTo("UserManagementView"));
            NavigateToHerbManagementCommand = new DelegateCommand(() => NavigateTo("HerbManagementView"));
            NavigateToPatientManagementCommand = new DelegateCommand(() => NavigateTo("PatientManagementView"));
            NavigateToFormulaManagementCommand = new DelegateCommand(() => NavigateTo("FormulaManagementView"));
            NavigateToMedicalCaseManagementCommand = new DelegateCommand(() => NavigateTo("MedicalCaseManagementView"));
            NavigateToSystemSettingsCommand = new DelegateCommand(() => NavigateTo("SystemSettingsView"));

            // Issue #1887-1892: 初始化修改信息和修改密码命令
            EditProfileCommand = new DelegateCommand(ExecuteEditProfileCommand);
            ChangePasswordCommand = new DelegateCommand(ExecuteChangePasswordCommand);

            // 加载当前用户信息
            LoadCurrentUser();
        }

        #endregion 构造函数

        #region 辅助方法

        /// <summary>
        /// 导航到指定视图
        /// </summary>
        /// <param name="viewName">视图名称</param>
        private void NavigateTo(string viewName)
        {
            try
            {
                Logger.LogInformation("导航到 {ViewName}", viewName);
                _regionManager.RequestNavigate("ContentRegion", viewName, navigationResult =>
                {
                    if (navigationResult.Result == true)
                    {
                        Logger.LogInformation("导航成功：{ViewName}", viewName);
                    }
                    else
                    {
                        Logger.LogError("导航失败：{ViewName}，错误：{Error}", viewName, navigationResult.Error?.Message ?? "未知错误");
                        if (navigationResult.Error != null)
                        {
                            Logger.LogError(navigationResult.Error, "导航异常详情");
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "导航到 {ViewName} 时发生异常", viewName);
            }
        }

        /// <summary>
        /// 加载当前用户信息 (Issue #1887-1892)
        /// </summary>
        private async void LoadCurrentUser()
        {
            try
            {
                var currentUser = await _authService.GetCurrentUserAsync();
                if (currentUser != null && currentUser.Id != Guid.Empty)
                {
                    // 普通管理员用户
                    CurrentUserName = currentUser.RealName ?? currentUser.UserName ?? "管理员";
                    IsSysAdmin = false;
                }
                else
                {
                    // sysadmin 系统管理员（虚拟用户）
                    CurrentUserName = "系统管理员";
                    IsSysAdmin = true;
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "加载当前用户信息失败");
                CurrentUserName = "系统管理员";
                IsSysAdmin = true;
            }
        }

        /// <summary>
        /// 执行修改个人信息命令 (Issue #1887-1892)
        /// </summary>
        private void ExecuteEditProfileCommand()
        {
            try
            {
                // Issue #1887-1892: sysadmin 不允许修改个人信息
                if (IsSysAdmin)
                {
                    Logger.LogWarning("sysadmin 不允许修改个人信息");
                    return;
                }

                Logger.LogInformation("打开个人信息对话框");

                // 打开 UserProfileDialog（不传递参数，普通模式）
                _dialogService.ShowDialog("UserProfileDialog", null, result =>
                {
                    if (result.Result == ButtonResult.OK)
                    {
                        Logger.LogInformation("个人信息修改成功");
                        // 刷新当前用户显示名称
                        LoadCurrentUser();
                    }
                });
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "打开个人信息对话框失败");
            }
        }

        /// <summary>
        /// 执行修改密码命令 (Issue #1887-1892)
        /// </summary>
        private void ExecuteChangePasswordCommand()
        {
            try
            {
                Logger.LogInformation("打开系统管理员修改密码对话框");

                // 打开 ChangePasswordDialog，传递参数标识为 sysadmin 模式
                var parameters = new DialogParameters
                {
                    { "IsSysAdmin", true }
                };

                _dialogService.ShowDialog("ChangePasswordDialog", parameters, result =>
                {
                    if (result.Result == ButtonResult.OK)
                    {
                        Logger.LogInformation("系统管理员密码修改成功");
                        // 可选：显示成功提示
                    }
                    else if (result.Result == ButtonResult.Cancel)
                    {
                        Logger.LogInformation("取消修改密码");
                    }
                });
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "打开修改密码对话框时发生异常");
            }
        }

        #endregion 辅助方法

        #region INavigationAware

        public override void OnNavigatedTo(NavigationContext navigationContext)
        {
            // 导航到主页时的逻辑（如果需要）
        }

        public override bool IsNavigationTarget(NavigationContext navigationContext)
        {
            return true;
        }

        public override void OnNavigatedFrom(NavigationContext navigationContext)
        {
            // 简化实现 - 无需清理
        }

        #endregion INavigationAware
    }
}
