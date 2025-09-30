using LYBT.Desktop.Infrastructure.Interfaces;
using LYBT.Desktop.Models.ViewModels.Base;
using LYBT.Shared.Interfaces.Services;
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

        private readonly IMedicalCaseService _medicalCaseService;

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

        #endregion

        #region 构造函数

        public MedicalCaseManagementViewModel(
            IEventAggregator eventAggregator,
            ILoggerFactory loggerFactory,
            IRegionManager regionManager,
            IMedicalCaseService medicalCaseService,
            ISessionManager? sessionManager = null,
            IErrorHandlingService? errorHandlingService = null)
            : base(eventAggregator, loggerFactory, regionManager, sessionManager, errorHandlingService)
        {
            _medicalCaseService = medicalCaseService ?? throw new ArgumentNullException(nameof(medicalCaseService));

            // 初始化命令
            ShowListCommand = new DelegateCommand(ShowList);
            CreateNewCommand = new DelegateCommand(CreateNew);
            RefreshCommand = new DelegateCommand(async () => await RefreshAsync());
            BackToHomeCommand = new DelegateCommand(BackToHome);
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

    /// <summary>
    /// 数据刷新事件
    /// </summary>
    public class DataRefreshEvent : PubSubEvent<string> { }
}
