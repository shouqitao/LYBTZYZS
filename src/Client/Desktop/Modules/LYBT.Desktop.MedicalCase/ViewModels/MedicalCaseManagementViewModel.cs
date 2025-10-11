using LYBT.Desktop.Infrastructure.Events;
using LYBT.Desktop.Infrastructure.Interfaces;
using LYBT.Desktop.Models.ViewModels.Base;
using LYBT.Desktop.MedicalCase.Interfaces;
using Microsoft.Extensions.Logging;
using Prism.Commands;
using Prism.Events;
using Prism.Regions;

namespace LYBT.Desktop.Modules.MedicalCase.ViewModels
{
    /// <summary>
    /// 病历管理主视图模型 - UltraThink精简架构
    /// 作为病历模块的主导航和管理容器
    /// </summary>
    public class MedicalCaseManagementViewModel : UnifiedViewModelBase
    {
        #region 服务依赖

        private readonly IMedicalCaseRepository _medicalCaseRepository;

        #endregion

        #region 导航属性

        private string _activeView = "MedicalCaseListView";

        /// <summary>
        /// 当前激活的视图
        /// </summary>
        public string ActiveView
        {
            get => _activeView;
            set => SetProperty(ref _activeView, value);
        }

        #endregion

        #region 命令

        /// <summary>
        /// 显示病历列表命令
        /// </summary>
        public DelegateCommand ShowListCommand { get; }

        /// <summary>
        /// 创建新病历命令
        /// </summary>
        public DelegateCommand CreateNewCommand { get; }

        /// <summary>
        /// 刷新数据命令
        /// </summary>
        public DelegateCommand RefreshCommand { get; }

        /// <summary>
        /// 返回主页命令
        /// </summary>
        public DelegateCommand BackToHomeCommand { get; }


        /// <summary>
        /// 搜索命令
        /// </summary>
        public DelegateCommand SearchCommand { get; }

        /// <summary>
        /// 添加命令
        /// </summary>
        public DelegateCommand AddCommand { get; }

        /// <summary>
        /// 查看详情命令（DataGrid 行命令）
        /// </summary>
        public DelegateCommand<object> ViewDetailsCommand { get; }

        /// <summary>
        /// 编辑命令（DataGrid 行命令）
        /// </summary>
        public DelegateCommand<object> EditCommand { get; }

        /// <summary>
        /// 查看诊疗命令（DataGrid 行命令）
        /// </summary>
        public DelegateCommand<object> ViewConsultationCommand { get; }

        /// <summary>
        /// 创建处方命令（DataGrid 行命令）
        /// </summary>
        public DelegateCommand<object> CreatePrescriptionCommand { get; }

        /// <summary>
        /// 打印命令（DataGrid 行命令）
        /// </summary>
        public DelegateCommand<object> PrintCommand { get; }

        /// <summary>
        /// 删除命令（DataGrid 行命令）
        /// </summary>
        public DelegateCommand<object> DeleteCommand { get; }

        /// <summary>
        /// 首页命令
        /// </summary>
        public DelegateCommand FirstPageCommand { get; }

        /// <summary>
        /// 上一页命令
        /// </summary>
        public DelegateCommand PreviousPageCommand { get; }

        /// <summary>
        /// 下一页命令
        /// </summary>
        public DelegateCommand NextPageCommand { get; }

        /// <summary>
        /// 末页命令
        /// </summary>
        public DelegateCommand LastPageCommand { get; }

        #endregion

        #region 构造函数

        public MedicalCaseManagementViewModel(
            IMedicalCaseRepository medicalCaseService,
            IEventAggregator eventAggregator,
            ILoggerFactory loggerFactory,
            IRegionManager regionManager,
            ISessionManager? sessionManager = null,
            IUserNotificationService? userNotificationService = null)
            : base(eventAggregator, loggerFactory, regionManager, sessionManager, userNotificationService)
        {
            _medicalCaseRepository = medicalCaseService ?? throw new ArgumentNullException(nameof(medicalCaseService));

            // 初始化命令
            ShowListCommand = new DelegateCommand(ShowList);
            CreateNewCommand = new DelegateCommand(CreateNew);
            RefreshCommand = new DelegateCommand(async () => await RefreshAsync());
            BackToHomeCommand = new DelegateCommand(BackToHome);

            // 列表管理命令
            SearchCommand = new DelegateCommand(async () => await ExecuteSearchAsync());
            AddCommand = new DelegateCommand(CreateNew); // 重用 CreateNew

            // DataGrid 行命令
            ViewDetailsCommand = new DelegateCommand<object>(ExecuteViewDetails);
            EditCommand = new DelegateCommand<object>(ExecuteEdit);
            ViewConsultationCommand = new DelegateCommand<object>(ExecuteViewConsultation);
            CreatePrescriptionCommand = new DelegateCommand<object>(ExecuteCreatePrescription);
            PrintCommand = new DelegateCommand<object>(ExecutePrint);
            DeleteCommand = new DelegateCommand<object>(async item => await ExecuteDeleteAsync(item));

            // 分页命令
            FirstPageCommand = new DelegateCommand(ExecuteFirstPage);
            PreviousPageCommand = new DelegateCommand(ExecutePreviousPage);
            NextPageCommand = new DelegateCommand(ExecuteNextPage);
            LastPageCommand = new DelegateCommand(ExecuteLastPage);
        }

        #endregion

        #region 生命周期

        /// <summary>
        /// 页面加载时调用
        /// </summary>
        protected override async Task OnNavigatedToAsync(NavigationContext navigationContext)
        {
            await base.OnNavigatedToAsync(navigationContext);

            // 默认显示病历列表
            ShowList();
        }

        #endregion

        #region 命令实现

        /// <summary>
        /// 显示病历列表
        /// </summary>
        private void ShowList()
        {
            try
            {
                NavigateTo("MedicalCaseContentRegion", "MedicalCaseListView");
                ActiveView = "MedicalCaseListView";
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "导航到病历列表时发生异常");
                ShowErrorMessage("加载病历列表失败，请稍后重试");
            }
        }

        /// <summary>
        /// 创建新病历
        /// </summary>
        private void CreateNew()
        {
            try
            {
                NavigateTo("MedicalCaseContentRegion", "CreateMedicalCaseView");
                ActiveView = "CreateMedicalCaseView";
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "导航到创建病历页面时发生异常");
                ShowErrorMessage("打开创建病历页面失败，请稍后重试");
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
                EventAggregator.GetEvent<DataRefreshEvent>().Publish("MedicalCase");

                await ShowSuccessMessageAsync("数据刷新成功");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "刷新病历数据时发生异常");
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

        #region 导航方法

        /// <summary>
        /// 导航到病历详情
        /// </summary>
        /// <param name="medicalCaseId">病历ID</param>
        /// <param name="isReadOnly">是否只读模式</param>
        /// <summary>
        /// 执行搜索
        /// </summary>
        private async Task ExecuteSearchAsync()
        {
            // TODO: 实现搜索逻辑或转发到子视图
            await ShowSuccessMessageAsync("搜索功能由子视图处理");
        }

        /// <summary>
        /// 查看详情
        /// </summary>
        private void ExecuteViewDetails(object item)
        {
            Logger.LogInformation("查看病历详情功能开发中");
            ShowInfoMessage("查看详情功能开发中");
        }

        /// <summary>
        /// 编辑病历
        /// </summary>
        private void ExecuteEdit(object item)
        {
            Logger.LogInformation("编辑病历功能开发中");
            ShowInfoMessage("编辑功能开发中");
        }

        /// <summary>
        /// 查看诊疗记录
        /// </summary>
        private void ExecuteViewConsultation(object item)
        {
            Logger.LogInformation("查看诊疗记录功能开发中");
            ShowInfoMessage("查看诊疗记录功能开发中");
        }

        /// <summary>
        /// 创建处方
        /// </summary>
        private void ExecuteCreatePrescription(object item)
        {
            Logger.LogInformation("创建处方功能开发中");
            ShowInfoMessage("创建处方功能开发中");
        }

        /// <summary>
        /// 打印病历
        /// </summary>
        private void ExecutePrint(object item)
        {
            Logger.LogInformation("打印病历功能开发中");
            ShowInfoMessage("打印功能开发中");
        }

        /// <summary>
        /// 删除病历
        /// </summary>
        private async Task ExecuteDeleteAsync(object item)
        {
            Logger.LogInformation("删除病历功能开发中");
            await ShowSuccessMessageAsync("删除功能开发中");
        }

        /// <summary>
        /// 首页
        /// </summary>
        private void ExecuteFirstPage()
        {
            Logger.LogDebug("首页命令由子视图处理");
        }

        /// <summary>
        /// 上一页
        /// </summary>
        private void ExecutePreviousPage()
        {
            Logger.LogDebug("上一页命令由子视图处理");
        }

        /// <summary>
        /// 下一页
        /// </summary>
        private void ExecuteNextPage()
        {
            Logger.LogDebug("下一页命令由子视图处理");
        }

        /// <summary>
        /// 末页
        /// </summary>
        private void ExecuteLastPage()
        {
            Logger.LogDebug("末页命令由子视图处理");
        }

        public void NavigateToDetail(Guid medicalCaseId, bool isReadOnly = false)
        {
            try
            {
                var parameters = new NavigationParameters
                {
                    { "MedicalCaseId", medicalCaseId },
                    { "IsReadOnly", isReadOnly }
                };

                NavigateTo("MedicalCaseContentRegion", "MedicalCaseDetailView", parameters);
                ActiveView = "MedicalCaseDetailView";
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "导航到病历详情时发生异常，病历ID: {MedicalCaseId}", medicalCaseId);
                ShowErrorMessage("打开病历详情失败，请稍后重试");
            }
        }

        /// <summary>
        /// 导航回列表视图
        /// </summary>
        public void NavigateBackToList()
        {
            ShowList();
        }

        #endregion
    }
}
