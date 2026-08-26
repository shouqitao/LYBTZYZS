using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Foundation.Security;
using LYBT.Desktop.Infrastructure.Constants;
using LYBT.Desktop.Infrastructure.Extensions;
using LYBT.Desktop.Infrastructure.ViewModels.Base;
using Microsoft.Extensions.Logging;
using Prism.Events;
using Prism.Regions;
using Prism.Services.Dialogs;

namespace LYBT.Desktop.Clinical.ViewModels
{
    /// <summary>
    /// 医生工作台主页视图模型
    /// 核心功能："开始接诊"按钮（导航到PatientSelectionView） + 今日统计 + 个人资料
    /// Issue #1553: 角色模块化重构 - Clinical模块
    /// Issue #1567: 导航到患者选择视图（新流程：主页 → 患者选择 → 3步看病流程）
    /// Issue #1887-1891: 添加个人资料编辑功能
    /// </summary>
    public partial class ClinicalHomeViewModel : NavigableViewModelBase
    {
        #region 依赖服务
        private readonly IAuthenticationService _authService;
        private readonly INavigationCoordinator _navigationCoordinator;
        private readonly IReportService _reportService;
        private readonly IRegistrationService _registrationService;

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
        /// </summary>
        public ClinicalHomeViewModel(
            IViewModelServices services,
            IAuthenticationService authService,
            INavigationCoordinator navigationCoordinator,
            IReportService reportService,
            IRegistrationService registrationService)
            : base(services)
        {
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));
            _navigationCoordinator = navigationCoordinator ?? throw new ArgumentNullException(nameof(navigationCoordinator));
            _reportService = reportService ?? throw new ArgumentNullException(nameof(reportService));
            _registrationService = registrationService ?? throw new ArgumentNullException(nameof(registrationService));

            // 加载当前用户信息
            LoadCurrentUserAsync().SafeFireAndForget(ex => Logger.LogError(ex, "加载当前用户信息失败"));

            // 加载今日统计数据
            LoadTodayStatisticsAsync().SafeFireAndForget(ex => Logger.LogError(ex, "加载今日统计数据失败"));
        }

        #endregion 构造函数

        #region 命令

        /// <summary>
        /// 开始看诊
        /// 导航到 ClinicalWorkspaceView（一体化临床工作台）
        /// 新流程：主页 → 临床工作台（左侧患者选择 + 右侧看诊工作区）
        /// </summary>
        [RelayCommand]
        private void StartMedicalCase()
        {
            Logger.LogInformation("开始看诊，导航到临床工作台");
            _ = _navigationCoordinator.NavigateTo(ViewNames.ClinicalWorkspace);
        }

        /// <summary>
        /// 导航到患者管理 - Issue #1827
        /// </summary>
        [RelayCommand]
        private void NavigateToPatientManagement()
        {
            Logger.LogInformation("导航到患者管理视图");
            _ = _navigationCoordinator.NavigateTo(ViewNames.PatientManagement);
        }

        /// <summary>
        /// 导航到医案查询 - Issue #1827
        /// </summary>
        [RelayCommand]
        private void NavigateToMedicalCaseQuery()
        {
            Logger.LogInformation("导航到医案管理视图");
            _ = _navigationCoordinator.NavigateTo(ViewNames.MedicalCaseManagement);
        }

        /// <summary>
        /// 导航到药材库 - Issue #1827
        /// </summary>
        [RelayCommand]
        private void NavigateToHerbLibrary()
        {
            Logger.LogInformation("导航到药材管理视图");
            _ = _navigationCoordinator.NavigateTo(ViewNames.HerbManagement);
        }

        /// <summary>
        /// 导航到验方库 - Issue #1827
        /// </summary>
        [RelayCommand]
        private void NavigateToFormulaLibrary()
        {
            Logger.LogInformation("导航到经验方管理视图");
            _ = _navigationCoordinator.NavigateTo(ViewNames.FormulaManagement);
        }

        /// <summary>
        /// 导航到挂号队列
        /// PRD: registration.md - US-REG-003 查看挂号队列
        /// </summary>
        [RelayCommand]
        private void NavigateToRegistrationQueue()
        {
            Logger.LogInformation("导航到挂号队列视图");
            _ = _navigationCoordinator.NavigateTo(ViewNames.RegistrationList);
        }

        /// <summary>
        /// 导航到统计报表
        /// </summary>
        [RelayCommand]
        private void NavigateToReports()
        {
            Logger.LogInformation("导航到统计报表视图");
            _ = _navigationCoordinator.NavigateTo(ViewNames.ReportsHome);
        }

        /// <summary>
        /// 导航到医案审计日志
        /// </summary>
        [RelayCommand]
        private void NavigateToAuditLog()
        {
            Logger.LogInformation("导航到医案审计日志视图");
            _ = _navigationCoordinator.NavigateTo(ViewNames.AuditLog);
        }

        /// <summary>
        /// 编辑个人资料 (Issue #1887-1891)
        /// </summary>
        [RelayCommand]
        private void EditProfile()
        {
            Logger.LogInformation("导航到账户设置页面(个人资料)");
            _ = _navigationCoordinator.NavigateTo(ViewNames.AccountSettings);
        }

        /// <summary>
        /// 修改密码 (Issue #1887-1892)
        /// </summary>
        [RelayCommand]
        private void ChangePassword()
        {
            Logger.LogInformation("导航到账户设置页面(修改密码)");
            var parameters = new Dictionary<string, object> { { "Tab", "Password" } };
            _ = _navigationCoordinator.NavigateTo(ViewNames.AccountSettings, parameters);
        }

        #endregion 命令

        #region 辅助方法

        /// <summary>
        /// 加载当前用户信息 (Issue #1887-1891)
        /// </summary>
        private async Task LoadCurrentUserAsync()
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
        private async Task LoadTodayStatisticsAsync()
        {
            try
            {
                // 今日接诊量：日问诊报表（服务端按登录医生角色过滤）
                var consultationResult = await _reportService.GetDailyConsultationsAsync(DateTime.Today, DateTime.Today);
                if (consultationResult.Success && consultationResult.Data != null)
                {
                    TodayConsultationCount = consultationResult.Data.TotalCount;
                }

                // 待看诊数：本人等待队列（Waiting 状态挂号）
                var doctorId = SessionManager.CurrentUserId;
                var queueResult = await _registrationService.GetQueueAsync(doctorId);
                if (queueResult.Success && queueResult.Data != null)
                {
                    PendingCaseCount = queueResult.Data.Count;
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "加载今日统计数据失败");
            }
        }

        #endregion 辅助方法

        #region INavigationAware

        public override void OnNavigatedTo(NavigationContext navigationContext)
        {
            base.OnNavigatedTo(navigationContext);
            // 每次导航到主页时刷新统计数据
            LoadTodayStatisticsAsync().SafeFireAndForget(ex => Logger.LogError(ex, "刷新今日统计数据失败"));
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
