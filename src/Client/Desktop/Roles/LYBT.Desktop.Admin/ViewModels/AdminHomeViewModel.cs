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
        /// 加载当前用户信息
        /// Issue #1887-1892: 个人信息修改
        /// Issue #1909: 三角色体系统一认证
        /// </summary>
        private async void LoadCurrentUser()
        {
            try
            {
                var currentUser = await _authService.GetCurrentUserAsync();
                if (currentUser != null && currentUser.Id != Guid.Empty)
                {
                    // Issue #1909: 所有用户（包括SuperAdmin）都在Users表中
                    CurrentUserName = currentUser.RealName ?? currentUser.UserName ?? "管理员";
                    // 不再需要IsSysAdmin标志，SuperAdmin也是普通用户，只是Role不同
                    IsSysAdmin = false;
                }
                else
                {
                    // 获取用户信息失败（不应该发生）
                    Logger.LogWarning("无法获取当前用户信息，可能未登录");
                    CurrentUserName = "未知用户";
                    IsSysAdmin = false;
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "加载当前用户信息失败");
                CurrentUserName = "加载失败";
                IsSysAdmin = false;
            }
        }

        /// <summary>
        /// 执行修改个人信息命令
        /// Issue #1887-1892: 个人信息修改功能
        /// Issue #1909: SuperAdmin也可以修改个人信息
        /// </summary>
        private void ExecuteEditProfileCommand()
        {
            try
            {
                Logger.LogInformation("导航到个人资料页面");

                // Issue #1929: Sprint 3 - 使用Navigation模式代替Dialog
                NavigateTo("ContentRegion", "UserProfileView");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "导航到个人资料页面失败");
            }
        }

        /// <summary>
        /// 执行修改密码命令
        /// Issue #1887-1892: 密码修改功能
        /// Issue #1909: 统一密码修改流程（SuperAdmin也使用UserService）
        /// Issue #1929: Sprint 3 - 使用Navigation模式代替Dialog
        /// </summary>
        private void ExecuteChangePasswordCommand()
        {
            try
            {
                Logger.LogInformation("导航到修改密码页面");

                // Issue #1929: Sprint 3 - 使用Navigation模式代替Dialog
                NavigateTo("ContentRegion", "ChangePasswordView");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "导航到修改密码页面失败");
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
