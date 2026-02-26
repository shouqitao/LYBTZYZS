using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Foundation.Security;
using LYBT.Desktop.Models.ViewModels.Base;
using LYBT.Desktop.Infrastructure.Constants;
using Microsoft.Extensions.Logging;
using Prism.Events;
using Prism.Regions;
using Prism.Services.Dialogs;

// OpenSpec: unify-navigation-architecture Phase 6 - 整合INavigationCoordinator

namespace LYBT.Desktop.Clinical.ViewModels
{
    /// <summary>
    /// 医生工作台主页视图模型
    /// 核心功能："开始接诊"按钮（导航到PatientSelectionView） + 今日统计 + 个人资料
    /// Issue #1553: 角色模块化重构 - Clinical模块
    /// Issue #1567: 导航到患者选择视图（新流程：主页 → 患者选择 → 3步看病流程）
    /// Issue #1887-1891: 添加个人资料编辑功能
    /// OpenSpec: standardize-viewmodel-framework - 迁移到NavigableViewModelBase
    /// </summary>
    public partial class ClinicalHomeViewModel : NavigableViewModelBase
    {
        #region 依赖服务

        private readonly IAuthenticationService _authService;
        private readonly IDialogService _dialogService;
        private readonly INavigationCoordinator _navigationCoordinator;

        #endregion 依赖服务

        #region 可观察属性

        /// <summary>
        /// 当前用户名 (Issue #1887-1891)
        /// </summary>
        [ObservableProperty]
        private string _currentUserName = "医生";

        /// <summary>
        /// 今日接诊数量
        /// </summary>
        [ObservableProperty]
        private int _todayConsultationCount;

        /// <summary>
        /// 待完成医案数量
        /// </summary>
        [ObservableProperty]
        private int _pendingCaseCount;

        #endregion 可观察属性

        #region 构造函数

        /// <summary>
        /// 构造函数
        /// OpenSpec: enhance-viewmodel-architecture - 使用IViewModelServices聚合服务
        /// </summary>
        public ClinicalHomeViewModel(
            IViewModelServices services,
            IAuthenticationService authService,
            IDialogService dialogService,
            INavigationCoordinator navigationCoordinator)
            : base(services)
        {
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
            _navigationCoordinator = navigationCoordinator ?? throw new ArgumentNullException(nameof(navigationCoordinator));

            // 加载当前用户信息
            LoadCurrentUser();

            // 加载今日统计数据
            LoadTodayStatistics();
        }

        #endregion 构造函数

        #region 命令

        /// <summary>
        /// 开始看诊
        /// Issue #1567 - 导航到患者选择视图（PatientSelectionView）
        /// 新流程：主页 → 患者选择 → 3步看病流程
        /// </summary>
        [RelayCommand]
        private void StartMedicalCase()
        {
            try
            {
                Logger.LogInformation("开始看诊，导航到患者选择视图");
                _navigationCoordinator.NavigateTo(ViewNames.PatientSelection);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "开始看诊时发生异常");
            }
        }

        /// <summary>
        /// 导航到患者管理 - Issue #1827
        /// OpenSpec: rename-reference-to-management - 使用Clinical角色台管理视图
        /// </summary>
        [RelayCommand]
        private void NavigateToPatientManagement()
        {
            try
            {
                Logger.LogInformation("导航到患者管理视图");
                _navigationCoordinator.NavigateTo(ViewNames.PatientManagement);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "导航到患者管理时发生异常");
            }
        }

        /// <summary>
        /// 导航到医案查询 - Issue #1827
        /// OpenSpec: rename-reference-to-management - 使用Clinical角色台管理视图
        /// </summary>
        [RelayCommand]
        private void NavigateToMedicalCaseQuery()
        {
            try
            {
                Logger.LogInformation("导航到医案管理视图");
                _navigationCoordinator.NavigateTo(ViewNames.MedicalCaseManagement);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "导航到医案管理时发生异常");
            }
        }

        /// <summary>
        /// 导航到药材库 - Issue #1827
        /// OpenSpec: rename-reference-to-management - 使用Clinical角色台管理视图
        /// </summary>
        [RelayCommand]
        private void NavigateToHerbLibrary()
        {
            try
            {
                Logger.LogInformation("导航到药材管理视图");
                _navigationCoordinator.NavigateTo(ViewNames.HerbManagement);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "导航到药材管理时发生异常");
            }
        }

        /// <summary>
        /// 导航到验方库 - Issue #1827
        /// OpenSpec: rename-reference-to-management - 使用Clinical角色台管理视图
        /// </summary>
        [RelayCommand]
        private void NavigateToFormulaLibrary()
        {
            try
            {
                Logger.LogInformation("导航到经验方管理视图");
                _navigationCoordinator.NavigateTo(ViewNames.FormulaManagement);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "导航到经验方管理时发生异常");
            }
        }

        /// <summary>
        /// 导航到数据同步
        /// </summary>
        [RelayCommand]
        private void NavigateToSync()
        {
            try
            {
                Logger.LogInformation("导航到数据同步视图");
                _navigationCoordinator.NavigateTo(ViewNames.Sync);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "导航到数据同步时发生异常");
            }
        }

        /// <summary>
        /// 编辑个人资料 (Issue #1887-1891)
        /// OpenSpec: unify-navigation-architecture (ADR-6) - 合并到AccountSettingsView
        /// </summary>
        [RelayCommand]
        private void EditProfile()
        {
            try
            {
                Logger.LogInformation("导航到账户设置页面(个人资料)");
                _navigationCoordinator.NavigateTo(ViewNames.AccountSettings);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "导航到账户设置页面时发生异常");
            }
        }

        /// <summary>
        /// 修改密码 (Issue #1887-1892)
        /// OpenSpec: unify-navigation-architecture (ADR-6) - 合并到AccountSettingsView
        /// </summary>
        [RelayCommand]
        private void ChangePassword()
        {
            try
            {
                Logger.LogInformation("导航到账户设置页面(修改密码)");
                var parameters = new Dictionary<string, object> { { "Tab", "Password" } };
                _navigationCoordinator.NavigateTo(ViewNames.AccountSettings, parameters);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "导航到账户设置页面时发生异常");
            }
        }

        #endregion 命令

        #region 辅助方法

        /// <summary>
        /// 加载当前用户信息 (Issue #1887-1891)
        /// </summary>
        private async void LoadCurrentUser()
        {
            try
            {
                var currentUser = await _authService.GetCurrentUserAsync();
                if (currentUser != null)
                {
                    CurrentUserName = currentUser.RealName ?? currentUser.UserName ?? "医生";
                }
                else
                {
                    CurrentUserName = "医生";
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "加载当前用户信息失败");
                CurrentUserName = "医生";
            }
        }

        /// <summary>
        /// 加载今日统计数据
        /// </summary>
        private void LoadTodayStatistics()
        {
            try
            {
                // TODO: 从服务获取今日统计数据
                // 临时使用模拟数据
                TodayConsultationCount = 0;
                PendingCaseCount = 0;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "加载今日统计数据时发生异常");
            }
        }

        #endregion 辅助方法

        #region INavigationAware

        public override void OnNavigatedTo(NavigationContext navigationContext)
        {
            base.OnNavigatedTo(navigationContext);
            // 每次导航到主页时刷新统计数据
            LoadTodayStatistics();
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
