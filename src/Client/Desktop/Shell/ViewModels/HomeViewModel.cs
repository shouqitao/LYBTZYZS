using LYBT.Desktop.Models.ViewModels.Base;
using Microsoft.Extensions.Logging;
using Prism.Commands;
using Prism.Events;
using Prism.Regions;
using Prism.Services.Dialogs;
using LYBT.Shared.Models.Contracts.Patients;

namespace LYBT.Desktop.Shell.ViewModels
{
    /// <summary>
    /// 主页视图模型 - Issue #1486 Dashboard实现
    /// 核心功能："开始接诊"按钮（打开PatientSelectionDialog）+ 今日统计
    /// </summary>
    public class HomeViewModel : UnifiedViewModelBase
    {
        #region 依赖服务

        private readonly IRegionManager _regionManager;
        private readonly IDialogService _dialogService;

        #endregion 依赖服务

        #region 属性

        private string _searchKeyword = string.Empty;
        public string SearchKeyword
        {
            get => _searchKeyword;
            set => SetProperty(ref _searchKeyword, value);
        }

        private int _todayConsultationCount = 0;
        public int TodayConsultationCount
        {
            get => _todayConsultationCount;
            set => SetProperty(ref _todayConsultationCount, value);
        }

        private int _pendingCaseCount = 0;
        public int PendingCaseCount
        {
            get => _pendingCaseCount;
            set => SetProperty(ref _pendingCaseCount, value);
        }

        #endregion 属性

        #region 命令

        public DelegateCommand StartConsultationCommand { get; }
        public DelegateCommand QuickSearchCommand { get; }

        // 折叠区域的次要功能命令
        public DelegateCommand NavigateToPatientManagementCommand { get; }
        public DelegateCommand NavigateToPrescriptionQueryCommand { get; }
        public DelegateCommand NavigateToHerbsCommand { get; }
        public DelegateCommand NavigateToSystemSettingsCommand { get; }

        #endregion 命令

        #region 构造函数

        public HomeViewModel(
            IRegionManager regionManager,
            IEventAggregator eventAggregator,
            ILoggerFactory loggerFactory,
            IDialogService dialogService)
            : base(eventAggregator, loggerFactory, regionManager)
        {
            _regionManager = regionManager ?? throw new ArgumentNullException(nameof(regionManager));
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));

            // Issue #1486: 初始化核心命令
            StartConsultationCommand = new DelegateCommand(ExecuteStartConsultation);
            QuickSearchCommand = new DelegateCommand(ExecuteQuickSearch, CanExecuteQuickSearch)
                .ObservesProperty(() => SearchKeyword);

            // 初始化次要功能命令
            NavigateToPatientManagementCommand = new DelegateCommand(() => NavigateTo("PatientManagementView"));
            NavigateToPrescriptionQueryCommand = new DelegateCommand(() => NavigateTo("PrescriptionManagementView"));
            NavigateToHerbsCommand = new DelegateCommand(() => NavigateTo("HerbManagementView"));
            NavigateToSystemSettingsCommand = new DelegateCommand(ExecuteNavigateToSystemSettings);

            // 加载今日统计数据
            LoadTodayStatistics();
        }

        #endregion 构造函数

        #region 命令实现

        /// <summary>
        /// 开始看诊 - Issue #1486核心功能
        /// 打开PatientSelectionDialog，选择患者后导航到医案流程视图
        /// </summary>
        private void ExecuteStartConsultation()
        {
            try
            {
                Logger.LogInformation("开始看诊，打开患者选择对话框");

                // Issue #1486: 使用IDialogService打开PatientSelectionDialog
                var parameters = new DialogParameters();
                _dialogService.ShowDialog("PatientSelectionDialog", parameters, result =>
                {
                    if (result.Result == ButtonResult.OK)
                    {
                        // 获取选中的患者
                        var selectedPatient = result.Parameters.GetValue<PatientDto>("SelectedPatient");
                        if (selectedPatient != null)
                        {
                            Logger.LogInformation("患者已选择：{PatientName}，导航到医案流程", selectedPatient.Name);

                            // 导航到医案流程视图，传递患者信息
                            var navParams = new NavigationParameters
                            {
                                { "Patient", selectedPatient }
                            };
                            _regionManager.RequestNavigate("ContentRegion", "MedicalCaseFlowView", navParams);
                        }
                        else
                        {
                            Logger.LogWarning("PatientSelectionDialog返回OK但未提供患者信息");
                        }
                    }
                    else
                    {
                        Logger.LogInformation("患者选择已取消");
                    }
                });
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "开始看诊时发生异常");
            }
        }

        /// <summary>
        /// 快速搜索患者 - 支持姓名/拼音码/手机号搜索
        /// </summary>
        private void ExecuteQuickSearch()
        {
            try
            {
                Logger.LogInformation("快速搜索患者: {SearchKeyword}", SearchKeyword);
                // TODO: 实现搜索患者逻辑
                // 找到患者后，携带患者信息导航到医案流程
                _regionManager.RequestNavigate("ContentRegion", "MedicalCaseFlowView",
                    new NavigationParameters
                    {
                        { "StartStep", 1 },
                        { "SearchKeyword", SearchKeyword }
                    });
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "快速搜索时发生异常");
            }
        }

        /// <summary>
        /// 验证快速搜索命令是否可执行
        /// </summary>
        private bool CanExecuteQuickSearch()
        {
            return !string.IsNullOrWhiteSpace(SearchKeyword);
        }

        /// <summary>
        /// 导航到系统设置
        /// </summary>
        private void ExecuteNavigateToSystemSettings()
        {
            try
            {
                Logger.LogInformation("导航到系统设置");
                Logger.LogWarning("系统设置功能开发中");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "导航到系统设置时发生异常");
            }
        }

        #endregion 命令实现

        #region 辅助方法

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

        private void NavigateTo(string viewName)
        {
            try
            {
                _regionManager.RequestNavigate("ContentRegion", viewName);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "导航到 {ViewName} 失败", viewName);
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
