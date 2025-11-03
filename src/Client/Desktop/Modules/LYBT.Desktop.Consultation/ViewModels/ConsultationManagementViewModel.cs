using System.Collections.ObjectModel;
using System.Windows.Input;
using LYBT.Desktop.Contracts.Api;
using LYBT.Desktop.Infrastructure.Interfaces;
using LYBT.Desktop.Models.ViewModels.Base;
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

        // Issue #1607: 替换为API接口（Read操作）
        private readonly IConsultationApi _consultationApi;
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

        // Issue #1562 Phase 1: 已删除扩展功能命令（ViewPrescription/Print/Copy/Pagination）

        #endregion ����

        #region 构造函数

        public ConsultationManagementViewModel(
        IConsultationApi consultationApi, // Issue #1607: 使用API接口
        IFeatureToggleService featureToggleService,
        IEventAggregator eventAggregator,
        ILoggerFactory loggerFactory,
        IRegionManager regionManager,
        ISessionManager? sessionManager = null,
        IUserNotificationService? userNotificationService = null)
        : base(eventAggregator, loggerFactory, regionManager, sessionManager, userNotificationService)
        {
            // Issue #1607: 注入API接口
            _consultationApi = consultationApi ?? throw new ArgumentNullException(nameof(consultationApi));
            _featureToggleService = featureToggleService ?? throw new ArgumentNullException(nameof(featureToggleService));

            LoadDataCommand = new DelegateCommand(async () => await LoadDataAsync());
            SearchCommand = new DelegateCommand(async () => await SearchAsync(), () => CanSearch);
            RefreshCommand = new DelegateCommand(async () => await RefreshAsync());
            ViewDetailsCommand = new DelegateCommand<ConsultationDto>(ViewDetails, item => item != null && CanViewDetail);

            // Issue #1562 Phase 1: 已删除扩展功能命令初始化
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

                // Issue #1607: 使用API接口调用
                var apiResponse = await _consultationApi.GetConsultationsAsync(query.PageIndex, query.PageSize, query.Keyword);
                var result = apiResponse.Data; // 提取PagedResult<ConsultationDto>
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

        // Issue #1562 Phase 1: 已删除扩展功能实现（ViewPrescription/Print/CopyRecord/Pagination）

        #endregion ���ݲ���
    }
}
