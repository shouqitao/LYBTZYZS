using LYBT.Desktop.Infrastructure.Interfaces;
using LYBT.Desktop.Models.ViewModels.Base;
using LYBT.Desktop.Patients.ViewModels.Components;
using LYBT.Shared.Models.Contracts.Patients;
using Microsoft.Extensions.Logging;
using Prism.Commands;
using Prism.Events;
using Prism.Regions;

namespace LYBT.Desktop.Patients.ViewModels
{
    /// <summary>
    /// 患者管理视图模型
    /// 基于UnifiedListViewModelBase实现完整患者管理功能
    /// </summary>
    public class PatientManagementViewModel : UnifiedListViewModelBase<PatientDto>
    {
        #region 服务依赖

        private readonly PatientCommandHandler _commandHandler;

        #endregion

        #region 患者特定命令

        /// <summary>
        /// 编辑患者命令
        /// </summary>
        public DelegateCommand<PatientDto> EditCommand { get; private set; } = null!;

        /// <summary>
        /// 查看详情命令
        /// </summary>
        public DelegateCommand<PatientDto> ViewDetailsCommand { get; private set; } = null!;

        /// <summary>
        /// 首页命令
        /// </summary>
        public DelegateCommand FirstPageCommand { get; private set; } = null!;

        /// <summary>
        /// 末页命令
        /// </summary>
        public DelegateCommand LastPageCommand { get; private set; } = null!;

        #endregion

        #region 构造函数

        public PatientManagementViewModel(
            PatientCommandHandler commandHandler,
            IEventAggregator eventAggregator,
            ILoggerFactory loggerFactory,
            IRegionManager regionManager,
            ISessionManager? sessionManager = null,
            IUserNotificationService? userNotificationService = null)
            : base(eventAggregator, loggerFactory, regionManager, sessionManager, userNotificationService)
        {
            _commandHandler = commandHandler ?? throw new ArgumentNullException(nameof(commandHandler));

            // 初始化页面标题
            PageTitle = "患者管理";
            PageSize = 20;

            // 初始化患者特定命令
            InitializePatientCommands();

            Logger.LogDebug("患者管理ViewModel已初始化");
        }

        #endregion

        #region 命令初始化

        private void InitializePatientCommands()
        {
            EditCommand = new DelegateCommand<PatientDto>(ExecuteEditPatient, CanExecuteEditPatient);
            ViewDetailsCommand = new DelegateCommand<PatientDto>(ExecuteViewDetails, patient => patient != null);
            FirstPageCommand = new DelegateCommand(ExecuteFirstPage, () => CanGoPreviousPage && !IsLoading);
            LastPageCommand = new DelegateCommand(ExecuteLastPage, () => CanGoNextPage && !IsLoading);
        }

        #endregion

        #region 暴露基类命令

        /// <summary>
        /// 搜索命令 - 暴露基类实现
        /// </summary>
        public new DelegateCommand SearchCommand => base.SearchCommand;

        /// <summary>
        /// 刷新命令 - 暴露基类实现
        /// </summary>
        public new DelegateCommand RefreshCommand => base.RefreshCommand;

        /// <summary>
        /// 添加命令 - 暴露基类实现
        /// </summary>
        public new DelegateCommand AddCommand => base.AddCommand;

        /// <summary>
        /// 删除命令 - 暴露基类实现
        /// </summary>
        public new DelegateCommand<PatientDto> DeleteCommand => base.DeleteCommand;

        /// <summary>
        /// 上一页命令 - 暴露基类实现
        /// </summary>
        public new DelegateCommand PreviousPageCommand => base.PreviousPageCommand;

        /// <summary>
        /// 下一页命令 - 暴露基类实现
        /// </summary>
        public new DelegateCommand NextPageCommand => base.NextPageCommand;

        #endregion

        #region 数据加载

        /// <summary>
        /// 获取数据项
        /// </summary>
        protected override async Task<IEnumerable<PatientDto>> GetItemsAsync(int page, int pageSize, string? searchText)
        {
            Logger.LogDebug("加载患者列表: 第{Page}页, 每页{PageSize}条, 关键词: {SearchText}", page, pageSize, searchText);

            try
            {
                var cmdResult = await _commandHandler.GetPatientsPagedAsync(page, pageSize, searchText);

                if (cmdResult.IsSuccess && cmdResult.Data != null)
                {
                    TotalCount = cmdResult.Data.TotalCount;
                    return cmdResult.Data.Items;
                }
                else
                {
                    Logger.LogWarning("加载患者列表失败: {ErrorMessage}", cmdResult.ErrorMessage);
                    TotalCount = 0;
                    return new List<PatientDto>();
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "加载患者列表时发生异常");
                var contextMessage = $"加载患者列表 - 模块:{nameof(PatientManagementViewModel)}";
                await UserNotificationService!.HandleExceptionAsync(ex, contextMessage);

                TotalCount = 0;
                return new List<PatientDto>();
            }
        }

        #endregion

        #region 患者操作实现

        /// <summary>
        /// 添加新患者
        /// </summary>
        protected override Task OnExecuteAddAsync()
        {
            Logger.LogDebug("执行添加新患者");

            // TODO: 导航到患者详情页（新增模式）
            NavigateTo("ContentRegion", "PatientDetailView", new Prism.Regions.NavigationParameters
            {
                { "mode", "create" }
            });

            return Task.CompletedTask;
        }

        /// <summary>
        /// 删除患者
        /// </summary>
        protected override async Task OnExecuteDeleteAsync(PatientDto patient)
        {
            if (patient == null) return;

            Logger.LogDebug("删除患者: {PatientId} - {PatientName}", patient.Id, patient.Name);

            var result = await _commandHandler.DeletePatientAsync(patient.Id);
            if (!result.IsSuccess)
            {
                throw new InvalidOperationException(result.ErrorMessage ?? "删除患者失败");
            }

            Logger.LogInformation("成功删除患者: {PatientName}", patient.Name);
        }

        /// <summary>
        /// 批量删除患者
        /// </summary>
        protected override async Task OnExecuteBatchDeleteAsync(List<PatientDto> patients)
        {
            Logger.LogDebug("批量删除{Count}个患者", patients.Count);

            var patientIds = patients.Select(p => p.Id).ToList();
            var result = await _commandHandler.BatchDeletePatientsAsync(patientIds);

            if (!result.IsSuccess)
            {
                throw new InvalidOperationException(result.ErrorMessage ?? "批量删除患者失败");
            }

            Logger.LogInformation("成功批量删除{Count}个患者", patients.Count);
        }

        #endregion

        #region 患者特定命令实现

        /// <summary>
        /// 编辑患者
        /// </summary>
        private void ExecuteEditPatient(PatientDto patient)
        {
            if (patient == null) return;

            Logger.LogDebug("编辑患者: {PatientId} - {PatientName}", patient.Id, patient.Name);

            // 导航到患者详情页（编辑模式）
            NavigateTo("ContentRegion", "PatientDetailView", new Prism.Regions.NavigationParameters
            {
                { "mode", "edit" },
                { "PatientId", patient.Id }
            });
        }

        /// <summary>
        /// 是否可以编辑患者
        /// </summary>
        private bool CanExecuteEditPatient(PatientDto patient)
        {
            return patient != null && !IsLoading;
        }

        /// <summary>
        /// 查看详情
        /// </summary>
        private void ExecuteViewDetails(PatientDto patient)
        {
            if (patient == null) return;

            Logger.LogDebug("查看患者详情: {PatientId} - {PatientName}", patient.Id, patient.Name);

            NavigateTo("ContentRegion", "PatientDetailView", new Prism.Regions.NavigationParameters
            {
                { "PatientId", patient.Id },
                { "title", $"患者详情 - {patient.Name}" }
            });
        }

        /// <summary>
        /// 跳转首页
        /// </summary>
        private void ExecuteFirstPage()
        {
            CurrentPage = 1;
        }

        /// <summary>
        /// 跳转末页
        /// </summary>
        private void ExecuteLastPage()
        {
            CurrentPage = TotalPages;
        }

        #endregion

        #region 命令刷新

        protected override void RefreshCanExecuteChanged()
        {
            base.RefreshCanExecuteChanged();

            EditCommand?.RaiseCanExecuteChanged();
            ViewDetailsCommand?.RaiseCanExecuteChanged();
            FirstPageCommand?.RaiseCanExecuteChanged();
            LastPageCommand?.RaiseCanExecuteChanged();
        }

        #endregion
    }
}
