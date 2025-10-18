using System.Collections.ObjectModel;
using System.Windows.Input;
using LYBT.Desktop.Infrastructure.Interfaces;
using LYBT.Desktop.Models.ViewModels.Base;
using LYBT.Desktop.Consultation.Interfaces;
using LYBT.Shared.Models.Contracts.Consultation;
using Microsoft.Extensions.Logging;
using Prism.Commands;
using Prism.Events;
using Prism.Regions;

namespace LYBT.Desktop.Consultation.ViewModels
{

    /// <summary>
    /// 诊疗记录管理视图模型 - 简化版 (Issue #1477 #1479)
    /// 只支持查看和基本操作，诊疗记录的创建由病案流程控制
    /// </summary>
    public class ConsultationManagementViewModel : UnifiedViewModelBase
    {

        #region 私有字段

        private readonly IConsultationRepository _consultationRepository;
        private readonly IFeatureToggleService _featureToggleService;

        #endregion 私有字段

        #region ����

        private ObservableCollection<ConsultationDto> _consultations = new();

        public ObservableCollection<ConsultationDto> Consultations
        {
            get => _consultations;
            set => SetProperty(ref _consultations, value);
        }

        private ConsultationDto? _selectedConsultation;

        public ConsultationDto? SelectedConsultation
        {
            get => _selectedConsultation;
            set => SetProperty(ref _selectedConsultation, value);
        }

        private bool _isLoading = false;

        public new bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        private string _searchKeyword = string.Empty;

        public string SearchKeyword
        {
            get => _searchKeyword;
            set => SetProperty(ref _searchKeyword, value);
        }

        #endregion 属性

        #region 功能开关属性 (Issue #1477 #1479)

        /// <summary>
        /// 是否允许查看详情
        /// </summary>
        public bool CanViewDetail => _featureToggleService.IsEnabled("Consultation.ViewDetail");

        /// <summary>
        /// 是否允许搜索
        /// </summary>
        public bool CanSearch => _featureToggleService.IsEnabled("Consultation.Search");

        #endregion 功能开关属性

        #region ����

        public ICommand LoadDataCommand { get; }
        public ICommand SearchCommand { get; }
        public ICommand RefreshCommand { get; }
        public DelegateCommand<ConsultationDto> ViewDetailsCommand { get; }

        public DelegateCommand<ConsultationDto> ViewPrescriptionCommand { get; }
        public DelegateCommand<ConsultationDto> PrintCommand { get; }
        public DelegateCommand<ConsultationDto> CopyRecordCommand { get; }
        public ICommand StatisticsCommand { get; }
        public ICommand FirstPageCommand { get; }
        public ICommand LastPageCommand { get; }
        public ICommand PreviousPageCommand { get; }
        public ICommand NextPageCommand { get; }

        #endregion ����

        #region 构造函数

        public ConsultationManagementViewModel(
        IConsultationRepository consultationRepository,
        IFeatureToggleService featureToggleService,
        IEventAggregator eventAggregator,
        ILoggerFactory loggerFactory,
        IRegionManager regionManager,
        ISessionManager? sessionManager = null,
        IUserNotificationService? userNotificationService = null)
        : base(eventAggregator, loggerFactory, regionManager, sessionManager, userNotificationService)
        {
            _consultationRepository = consultationRepository ?? throw new ArgumentNullException(nameof(consultationRepository));
            _featureToggleService = featureToggleService ?? throw new ArgumentNullException(nameof(featureToggleService));

            LoadDataCommand = new DelegateCommand(async () => await LoadDataAsync());
            SearchCommand = new DelegateCommand(async () => await SearchAsync(), () => CanSearch);
            RefreshCommand = new DelegateCommand(async () => await RefreshAsync());
            ViewDetailsCommand = new DelegateCommand<ConsultationDto>(ViewDetails, item => item != null && CanViewDetail);

            ViewPrescriptionCommand = new DelegateCommand<ConsultationDto>(ViewPrescription, item => item != null);
            PrintCommand = new DelegateCommand<ConsultationDto>(Print, item => item != null);
            CopyRecordCommand = new DelegateCommand<ConsultationDto>(CopyRecord, item => item != null);
            StatisticsCommand = new DelegateCommand(ShowStatistics);

            FirstPageCommand = new DelegateCommand(ExecuteFirstPage);
            LastPageCommand = new DelegateCommand(ExecuteLastPage);
            PreviousPageCommand = new DelegateCommand(ExecutePreviousPage);
            NextPageCommand = new DelegateCommand(ExecuteNextPage);
        }

        #endregion 构造函数

        #region 导航生命周期 (Issue #1240)

        /// &lt;summary&gt;
        /// 异步初始化数据 - Issue #1240
        /// &lt;/summary&gt;
        protected override async Task InitializeAsync(NavigationParameters parameters)
        {
            await base.InitializeAsync(parameters);

            try
            {
                await LoadDataAsync();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "初始化诊疗管理失败");
                await ShowErrorMessageAsync("诊疗管理模块初始化失败，请尝试刷新页面");
            }
        }

        #endregion 导航生命周期

        #region 数据操作

        private async Task LoadDataAsync()
        {
            try
            {
                IsLoading = true;

                var query = new LYBT.Shared.Models.Contracts.Common.PagedQueryBaseDto
                {
                    PageIndex = 1,
                    PageSize = 100,
                    Keyword = SearchKeyword
                };

                var result = await _consultationRepository.GetPagedAsync(query.PageIndex, query.PageSize, query.Keyword);
                if (result != null && result.Items != null)
                {
                    Consultations.Clear();
                    foreach (var consultation in result.Items)
                    {
                        Consultations.Add(consultation);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "�������Ƽ�¼ʧ��");
                // ��������ʧ��
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task SearchAsync()
        {
            await LoadDataAsync();
        }

        private async Task RefreshAsync()
        {
            SearchKeyword = string.Empty;
            await LoadDataAsync();
        }

        private void ViewDetails(ConsultationDto consultation)
        {
            if (consultation == null) return;

            Logger.LogInformation("查看诊疗记录详情: {ConsultationId}", consultation.Id);
            ShowInfoMessage("查看详情功能开发中");
        }


        private void ViewPrescription(ConsultationDto consultation)
        {
            if (consultation == null) return;

            Logger.LogInformation("查看处方: {ConsultationId}", consultation.Id);
            ShowInfoMessage("查看处方功能开发中");
        }

        private void Print(ConsultationDto consultation)
        {
            if (consultation == null) return;

            Logger.LogInformation("打印诊疗记录: {ConsultationId}", consultation.Id);
            ShowInfoMessage("打印功能开发中");
        }

        private void CopyRecord(ConsultationDto consultation)
        {
            if (consultation == null) return;

            Logger.LogInformation("复制诊疗记录: {ConsultationId}", consultation.Id);
            ShowInfoMessage("复制记录功能开发中");
        }

        private void ShowStatistics()
        {
            Logger.LogInformation("统计功能开发中");
            ShowInfoMessage("统计功能开发中");
        }

        private void ExecuteFirstPage()
        {
            Logger.LogDebug("首页命令 - 功能开发中");
        }

        private void ExecuteLastPage()
        {
            Logger.LogDebug("末页命令 - 功能开发中");
        }

        private void ExecutePreviousPage()
        {
            Logger.LogDebug("上一页命令 - 功能开发中");
        }

        private void ExecuteNextPage()
        {
            Logger.LogDebug("下一页命令 - 功能开发中");
        }

        #endregion ���ݲ���
    }
}
