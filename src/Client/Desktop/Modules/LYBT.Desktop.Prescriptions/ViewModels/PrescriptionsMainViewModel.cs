using LYBT.Desktop.Infrastructure.Events;
using LYBT.Desktop.Infrastructure.Interfaces;
using LYBT.Desktop.Models.ViewModels.Base;
using LYBT.Desktop.Prescriptions.Interfaces;
using Microsoft.Extensions.Logging;
using Prism.Commands;
using Prism.Events;
using Prism.Regions;

namespace LYBT.Desktop.Modules.Prescriptions.ViewModels
{
    /// <summary>
    /// 处方主视图模型 - UltraThink精简架构
    /// 作为处方模块的入口和主导航容器
    /// Issue #1445 (ARCH-3): 统一导航目标到PrescriptionView（已删除空骨架）
    /// </summary>
    public class PrescriptionsMainViewModel : UnifiedViewModelBase
    {
        #region 服务依赖

        private readonly IPrescriptionRepository _prescriptionRepository;

        #endregion

        #region 导航属性

        private string _activeView = "PrescriptionManagementView";

        /// <summary>
        /// 当前激活的视图
        /// </summary>
        public string ActiveView
        {
            get => _activeView;
            set => SetProperty(ref _activeView, value);
        }

        #endregion

        #region 统计属性

        private int _totalPrescriptionsCount;
        private int _todayPrescriptionsCount;
        private decimal _todayTotalAmount;

        /// <summary>
        /// 总处方数
        /// </summary>
        public int TotalPrescriptionsCount
        {
            get => _totalPrescriptionsCount;
            set => SetProperty(ref _totalPrescriptionsCount, value);
        }

        /// <summary>
        /// 今日处方数
        /// </summary>
        public int TodayPrescriptionsCount
        {
            get => _todayPrescriptionsCount;
            set => SetProperty(ref _todayPrescriptionsCount, value);
        }

        /// <summary>
        /// 今日总金额
        /// </summary>
        public decimal TodayTotalAmount
        {
            get => _todayTotalAmount;
            set => SetProperty(ref _todayTotalAmount, value);
        }

        #endregion

        #region 命令

        /// <summary>
        /// 显示处方管理命令
        /// </summary>
        public DelegateCommand ShowManagementCommand { get; }

        /// <summary>
        /// 创建新处方命令
        /// </summary>
        public DelegateCommand CreateNewCommand { get; }

        /// <summary>
        /// 显示统计报表命令
        /// </summary>
        public DelegateCommand ShowReportsCommand { get; }

        /// <summary>
        /// 刷新数据命令
        /// </summary>
        public DelegateCommand RefreshCommand { get; }

        /// <summary>
        /// 返回主页命令
        /// </summary>
        public DelegateCommand BackToHomeCommand { get; }

        /// <summary>
        /// 创建新处方命令（别名）
        /// </summary>
        public DelegateCommand CreateNewPrescriptionCommand { get; }

        /// <summary>
        /// 返回源视图命令（别名）
        /// </summary>
        public DelegateCommand ReturnToSourceCommand { get; }

        /// <summary>
        /// 切换到管理视图命令（别名）
        /// </summary>
        public DelegateCommand SwitchToManagementCommand { get; }

        #endregion

        #region 构造函数

        public PrescriptionsMainViewModel(
            IPrescriptionRepository prescriptionRepository,
            IEventAggregator eventAggregator,
            ILoggerFactory loggerFactory,
            IRegionManager regionManager,
            ISessionManager? sessionManager = null,
            IUserNotificationService? userNotificationService = null)
            : base(eventAggregator, loggerFactory, regionManager, sessionManager, userNotificationService)
        {
            _prescriptionRepository = prescriptionRepository ?? throw new ArgumentNullException(nameof(prescriptionRepository));

            // 初始化命令
            ShowManagementCommand = new DelegateCommand(ShowManagement);
            CreateNewCommand = new DelegateCommand(CreateNew);
            ShowReportsCommand = new DelegateCommand(ShowReports);
            RefreshCommand = new DelegateCommand(async () => await RefreshAsync());
            BackToHomeCommand = new DelegateCommand(BackToHome);

            // 初始化别名命令
            CreateNewPrescriptionCommand = CreateNewCommand; // 别名
            ReturnToSourceCommand = BackToHomeCommand; // 别名
            SwitchToManagementCommand = ShowManagementCommand; // 别名
        }

        #endregion

        #region 生命周期

        /// <summary>
        /// 页面加载时调用
        /// </summary>
        protected override async Task InitializeAsync(NavigationParameters parameters)
        {
            await base.InitializeAsync(parameters);

            // 默认显示处方管理
            ShowManagement();

            // 加载统计数据
            await LoadStatisticsAsync();
        }

        #endregion

        #region 命令实现

        /// <summary>
        /// 显示处方管理
        /// </summary>
        private void ShowManagement()
        {
            try
            {
                NavigateTo("PrescriptionContentRegion", "PrescriptionManagementView");
                ActiveView = "PrescriptionManagementView";
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "导航到处方管理时发生异常");
                ShowErrorMessage("加载处方管理失败，请稍后重试");
            }
        }

        /// <summary>
        /// 创建新处方
        /// </summary>
        private void CreateNew()
        {
            try
            {
                NavigateTo("PrescriptionContentRegion", "PrescriptionView");
                ActiveView = "PrescriptionView";
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "导航到创建处方页面时发生异常");
                ShowErrorMessage("打开创建处方页面失败，请稍后重试");
            }
        }

        /// <summary>
        /// 显示统计报表
        /// </summary>
        private void ShowReports()
        {
            try
            {
                NavigateTo("PrescriptionContentRegion", "PrescriptionReportsView");
                ActiveView = "PrescriptionReportsView";
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "导航到处方报表时发生异常");
                ShowErrorMessage("打开处方报表失败，请稍后重试");
            }
        }

        /// <summary>
        /// 刷新数据
        /// </summary>
        private async Task RefreshAsync()
        {
            try
            {
                SetIsBusy(true, "正在刷新数据...");

                // 发送刷新事件通知子视图
                EventAggregator.GetEvent<DataRefreshEvent>().Publish("Prescription");

                // 刷新统计数据
                await LoadStatisticsAsync();

                await ShowSuccessMessageAsync("数据刷新成功");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "刷新处方数据时发生异常");
                await ShowErrorMessageAsync("刷新数据失败，请稍后重试");
            }
            finally
            {
                SetIsBusy(false);
            }
        }

        /// <summary>
        /// 返回主页
        /// </summary>
        private void BackToHome()
        {
            try
            {
                NavigateTo("MainRegion", "HomeView");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "导航到主页时发生异常");
                ShowErrorMessage("返回主页失败，请稍后重试");
            }
        }

        #endregion

        #region 数据加载

        /// <summary>
        /// 加载统计数据
        /// </summary>
        private async Task LoadStatisticsAsync()
        {
            try
            {
                Logger.LogInformation("开始加载处方统计数据");

                // 加载总处方数
                var totalResult = await _prescriptionRepository.GetPagedAsync(1, 1, null);
                TotalPrescriptionsCount = totalResult.TotalCount;

                // 加载今日处方数据
                var today = DateTime.Today;
                var todayResult = await _prescriptionRepository.GetPagedAsync(1, int.MaxValue, null);
                var todayPrescriptions = todayResult.Items
                    .Where(p => p.CreatedAt.Date == today)
                    .ToList();
                TodayPrescriptionsCount = todayPrescriptions.Count;
                TodayTotalAmount = todayPrescriptions.Sum(p => p.TotalAmount);

                Logger.LogInformation("处方统计数据加载完成");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "加载处方统计数据时发生异常");

                // 设置默认值
                TotalPrescriptionsCount = 0;
                TodayPrescriptionsCount = 0;
                TodayTotalAmount = 0;
            }
        }

        #endregion

        #region 导航方法

        /// <summary>
        /// 导航到处方详情
        /// </summary>
        /// <param name="prescriptionId">处方ID</param>
        /// <param name="isReadOnly">是否只读模式</param>
        public void NavigateToDetail(Guid prescriptionId, bool isReadOnly = false)
        {
            try
            {
                var parameters = new NavigationParameters
                {
                    { "PrescriptionId", prescriptionId },
                    { "IsReadOnly", isReadOnly }
                };

                NavigateTo("PrescriptionContentRegion", "PrescriptionDetailView", parameters);
                ActiveView = "PrescriptionDetailView";
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "导航到处方详情时发生异常，处方ID: {PrescriptionId}", prescriptionId);
                ShowErrorMessage("打开处方详情失败，请稍后重试");
            }
        }

        /// <summary>
        /// 导航到处方编辑
        /// </summary>
        /// <param name="prescriptionId">处方ID</param>
        public void NavigateToEdit(Guid prescriptionId)
        {
            try
            {
                var parameters = new NavigationParameters
                {
                    { "PrescriptionId", prescriptionId }
                };

                NavigateTo("PrescriptionContentRegion", "PrescriptionView", parameters);
                ActiveView = "PrescriptionView";
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "导航到处方编辑时发生异常，处方ID: {PrescriptionId}", prescriptionId);
                ShowErrorMessage("打开处方编辑失败，请稍后重试");
            }
        }

        /// <summary>
        /// 导航回管理视图
        /// </summary>
        public void NavigateBackToManagement()
        {
            ShowManagement();
        }

        #endregion
    }
}
