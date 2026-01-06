using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LYBT.Desktop.Foundation.Security;
using LYBT.Desktop.Models.ViewModels.Base;
using Microsoft.Extensions.Logging;
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
    /// OpenSpec: standardize-viewmodel-framework - 迁移到NavigableViewModelBase
    /// </summary>
    public partial class AdminHomeViewModel : NavigableViewModelBase
    {
        #region 依赖服务

        private readonly IAuthenticationService _authService;
        private readonly IDialogService _dialogService;

        #endregion 依赖服务

        #region 可观察属性

        /// <summary>
        /// 当前用户名
        /// </summary>
        [ObservableProperty]
        private string _currentUserName = "系统管理员";

        /// <summary>
        /// 是否为系统管理员 (Issue #1887-1892)
        /// </summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsNotSysAdmin))]
        private bool _isSysAdmin = true; // 默认为true，避免按钮闪现

        #endregion 可观察属性

        #region 计算属性

        /// <summary>
        /// 是否不是系统管理员（用于UI可见性绑定）
        /// </summary>
        public bool IsNotSysAdmin => !IsSysAdmin;

        #endregion 计算属性

        #region 构造函数

        public AdminHomeViewModel(
            IRegionManager regionManager,
            IEventAggregator eventAggregator,
            ILoggerFactory loggerFactory,
            IAuthenticationService authService,
            IDialogService dialogService)
            : base(loggerFactory, eventAggregator, regionManager)
        {
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));

            // 加载当前用户信息
            LoadCurrentUser();
        }

        #endregion 构造函数

        #region 命令

        /// <summary>
        /// 导航到用户管理
        /// OpenSpec: refactor-admin-workspace - 导航到角色台管理视图
        /// </summary>
        [RelayCommand]
        private void NavigateToUserManagement() => NavigateTo("UserManagementView");

        /// <summary>
        /// 导航到药材管理
        /// </summary>
        [RelayCommand]
        private void NavigateToHerbManagement() => NavigateTo("HerbManagementView");

        /// <summary>
        /// 导航到患者管理
        /// </summary>
        [RelayCommand]
        private void NavigateToPatientManagement() => NavigateTo("PatientManagementView");

        /// <summary>
        /// 导航到验方管理
        /// </summary>
        [RelayCommand]
        private void NavigateToFormulaManagement() => NavigateTo("FormulaManagementView");

        /// <summary>
        /// 导航到病历管理
        /// </summary>
        [RelayCommand]
        private void NavigateToMedicalCaseManagement() => NavigateTo("MedicalCaseManagementView");

        /// <summary>
        /// 导航到系统设置
        /// </summary>
        [RelayCommand]
        private void NavigateToSystemSettings() => NavigateTo("SystemSettingsView");

        /// <summary>
        /// 修改个人信息命令 (Issue #1887-1892)
        /// Issue #1929: Sprint 3 - 使用Navigation模式代替Dialog
        /// </summary>
        [RelayCommand]
        private void EditProfile()
        {
            try
            {
                Logger.LogInformation("导航到个人资料页面");
                NavigateTo("UserProfileView");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "导航到个人资料页面失败");
            }
        }

        /// <summary>
        /// 修改密码命令 (Issue #1887-1892)
        /// Issue #1909: 统一密码修改流程（SuperAdmin也使用UserService）
        /// Issue #1929: Sprint 3 - 使用Navigation模式代替Dialog
        /// </summary>
        [RelayCommand]
        private void ChangePassword()
        {
            try
            {
                Logger.LogInformation("导航到修改密码页面");
                NavigateTo("ChangePasswordView");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "导航到修改密码页面失败");
            }
        }

        #endregion 命令

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
                RegionManager.RequestNavigate("ContentRegion", viewName, navigationResult =>
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

        #endregion 辅助方法

        #region INavigationAware

        public override void OnNavigatedTo(NavigationContext navigationContext)
        {
            base.OnNavigatedTo(navigationContext);
            // 导航到主页时的逻辑（如果需要）
        }

        public override bool IsNavigationTarget(NavigationContext navigationContext)
        {
            return true;
        }

        public override void OnNavigatedFrom(NavigationContext navigationContext)
        {
            base.OnNavigatedFrom(navigationContext);
            // 简化实现 - 无需清理
        }

        #endregion INavigationAware
    }
}
