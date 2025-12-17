using LYBT.Desktop.Foundation.Security;
using LYBT.Desktop.Models.ViewModels.Base;
using Microsoft.Extensions.Logging;
using Prism.Commands;
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
    public class ClinicalHomeViewModel : UnifiedViewModelBase
    {
        #region 依赖服务

        private readonly IRegionManager _regionManager;
        private readonly IAuthenticationService _authService;
        private readonly IDialogService _dialogService;

        #endregion 依赖服务

        #region 属性

        private string _currentUserName = "医生";
        /// <summary>
        /// 当前用户名 (Issue #1887-1891)
        /// </summary>
        public string CurrentUserName
        {
            get => _currentUserName;
            set => SetProperty(ref _currentUserName, value);
        }

        private int _todayConsultationCount = 0;
        /// <summary>
        /// 今日接诊数量
        /// </summary>
        public int TodayConsultationCount
        {
            get => _todayConsultationCount;
            set => SetProperty(ref _todayConsultationCount, value);
        }

        private int _pendingCaseCount = 0;
        /// <summary>
        /// 待完成医案数量
        /// </summary>
        public int PendingCaseCount
        {
            get => _pendingCaseCount;
            set => SetProperty(ref _pendingCaseCount, value);
        }

        #endregion 属性

        #region 命令

        /// <summary>
        /// 开始看诊命令
        /// </summary>
        public DelegateCommand StartConsultationCommand { get; }

        /// <summary>
        /// 导航到患者管理命令 - Issue #1827
        /// </summary>
        public DelegateCommand NavigateToPatientManagementCommand { get; }

        /// <summary>
        /// 导航到病历查询命令 - Issue #1827
        /// </summary>
        public DelegateCommand NavigateToMedicalCaseQueryCommand { get; }

        /// <summary>
        /// 导航到药材库命令 - Issue #1827
        /// </summary>
        public DelegateCommand NavigateToHerbLibraryCommand { get; }

        /// <summary>
        /// 导航到验方库命令 - Issue #1827
        /// </summary>
        public DelegateCommand NavigateToFormulaLibraryCommand { get; }

        /// <summary>
        /// 编辑个人资料命令 (Issue #1887-1892)
        /// </summary>
        public DelegateCommand EditProfileCommand { get; }

        /// <summary>
        /// 修改密码命令 (Issue #1887-1892)
        /// </summary>
        public DelegateCommand ChangePasswordCommand { get; }

        #endregion 命令

        #region 构造函数

        public ClinicalHomeViewModel(
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

            // 初始化核心命令
            StartConsultationCommand = new DelegateCommand(ExecuteStartConsultation);

            // Issue #1827: 初始化4个辅助导航命令
            NavigateToPatientManagementCommand = new DelegateCommand(ExecuteNavigateToPatientManagement);
            NavigateToMedicalCaseQueryCommand = new DelegateCommand(ExecuteNavigateToMedicalCaseQuery);
            NavigateToHerbLibraryCommand = new DelegateCommand(ExecuteNavigateToHerbLibrary);
            NavigateToFormulaLibraryCommand = new DelegateCommand(ExecuteNavigateToFormulaLibrary);

            // Issue #1887-1892: 初始化编辑个人资料和修改密码命令
            EditProfileCommand = new DelegateCommand(ExecuteEditProfileCommand);
            ChangePasswordCommand = new DelegateCommand(ExecuteChangePasswordCommand);

            // 加载当前用户信息
            LoadCurrentUser();

            // 加载今日统计数据
            LoadTodayStatistics();
        }

        #endregion 构造函数

        #region 命令实现

        /// <summary>
        /// 开始看诊
        /// Issue #1567 - 导航到患者选择视图（PatientSelectionView）
        /// 新流程：主页 → 患者选择 → 3步看病流程
        /// </summary>
        private void ExecuteStartConsultation()
        {
            try
            {
                Logger.LogInformation("开始看诊，导航到患者选择视图");

                // Issue #1567 - 导航到患者选择视图（独立化）
                // 流程：主页 → PatientSelectionView → MedicalCaseFlowView（3步）
                _regionManager.RequestNavigate("ContentRegion", "PatientSelectionView", navigationResult =>
                {
                    if (navigationResult.Result == true)
                    {
                        Logger.LogInformation("导航成功：PatientSelectionView");
                    }
                    else
                    {
                        Logger.LogError("导航失败：PatientSelectionView，错误：{Error}", navigationResult.Error?.Message ?? "未知错误");
                        if (navigationResult.Error != null)
                        {
                            Logger.LogError(navigationResult.Error, "导航异常详情");
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "开始看诊时发生异常");
            }
        }

        /// <summary>
        /// 导航到患者管理 - Issue #1827
        /// </summary>
        private void ExecuteNavigateToPatientManagement()
        {
            try
            {
                Logger.LogInformation("导航到患者管理视图");
                _regionManager.RequestNavigate("ContentRegion", "PatientMasterDetailView");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "导航到患者管理时发生异常");
            }
        }

        /// <summary>
        /// 导航到病历查询 - Issue #1827
        /// 修复: MedicalCaseQueryView不存在，使用MedicalCaseManagementView
        /// </summary>
        private void ExecuteNavigateToMedicalCaseQuery()
        {
            try
            {
                Logger.LogInformation("导航到医案管理视图");
                _regionManager.RequestNavigate("ContentRegion", "MedicalCaseManagementView");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "导航到医案管理时发生异常");
            }
        }

        /// <summary>
        /// 导航到药材库 - Issue #1827
        /// </summary>
        private void ExecuteNavigateToHerbLibrary()
        {
            try
            {
                Logger.LogInformation("导航到药材库视图");
                _regionManager.RequestNavigate("ContentRegion", "HerbMasterDetailView");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "导航到药材库时发生异常");
            }
        }

        /// <summary>
        /// 导航到验方库 - Issue #1827
        /// </summary>
        private void ExecuteNavigateToFormulaLibrary()
        {
            try
            {
                Logger.LogInformation("导航到验方库视图");
                _regionManager.RequestNavigate("ContentRegion", "FormulaMasterDetailView");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "导航到验方库时发生异常");
            }
        }

        /// <summary>
        /// 编辑个人资料 (Issue #1887-1891)
        /// </summary>
        private void ExecuteEditProfileCommand()
        {
            try
            {
                Logger.LogInformation("导航到个人资料页面");

                // Issue #1929: Sprint 3 - 使用Navigation模式代替Dialog
                _regionManager.RequestNavigate("ContentRegion", "UserProfileView");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "导航到个人资料页面时发生异常");
            }
        }


        /// <summary>
        /// 修改密码 (Issue #1887-1892)
        /// Issue #1929: Sprint 3 - 使用Navigation模式代替Dialog
        /// </summary>
        private void ExecuteChangePasswordCommand()
        {
            try
            {
                Logger.LogInformation("导航到修改密码页面");

                // Issue #1929: Sprint 3 - 使用Navigation模式代替Dialog
                _regionManager.RequestNavigate("ContentRegion", "ChangePasswordView");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "导航到修改密码页面时发生异常");
            }
        }

        #endregion 命令实现

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
            // 每次导航到主页时刷新统计数据
            LoadTodayStatistics();
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
