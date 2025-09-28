using System.Collections.ObjectModel;
using System.Windows.Input;
using LYBT.Desktop.Core.Interfaces.Services;
using LYBT.Desktop.Core.ViewModels.Base;
using LYBT.Shared.Interfaces.Services;
using LYBT.Shared.Models.Contracts.Consultation;
using Microsoft.Extensions.Logging;
using Prism.Commands;
using Prism.Events;
using Prism.Regions;

namespace LYBT.Desktop.Consultation.ViewModels
{

    /// <summary>
    /// 诊疗记录管理视图模型 - 简化版
    /// 只负责显示和基本管理诊疗记录，不包含复杂的流程控制
    /// </summary>
    public class ConsultationManagementViewModel : UnifiedViewModelBase
    {

        #region 服务依赖

        private readonly IConsultationService _consultationService;

        #endregion 服务依赖

        #region 属性

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

        public bool IsLoading
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

        #region 命令

        public ICommand LoadDataCommand { get; }
        public ICommand SearchCommand { get; }
        public ICommand RefreshCommand { get; }
        public ICommand ViewDetailsCommand { get; }

        #endregion 命令

        #region 构造函数

        public ConsultationManagementViewModel(
        IConsultationService consultationService,
        IEventAggregator eventAggregator,
        ILoggerFactory loggerFactory,
        IRegionManager regionManager,
        ISessionManager sessionManager)
        : base(eventAggregator, loggerFactory, regionManager, sessionManager)
        {
            _consultationService = consultationService ?? throw new ArgumentNullException(nameof(consultationService));

            LoadDataCommand = new DelegateCommand(async () => await LoadDataAsync());
            SearchCommand = new DelegateCommand(async () => await SearchAsync());
            RefreshCommand = new DelegateCommand(async () => await RefreshAsync());
            ViewDetailsCommand = new DelegateCommand(ViewDetails, () => SelectedConsultation != null)
            .ObservesProperty(() => SelectedConsultation);

            // 修复: 使用Task.Run安全初始化，防止未处理异常
            _ = Task.Run(async () => await InitializeAsync());
        }

        #endregion 构造函数

        #region 初始化

        private async Task InitializeAsync()
        {
            try
            {
                await LoadDataAsync();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "初始化诊疗管理失败");

                // 提供用户友好的错误提示
                ShowErrorMessage("诊疗管理模块初始化失败，请尝试刷新页面");
            }
        }

        #endregion 初始化

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

                var result = await _consultationService.GetPagedAsync(query.PageIndex, query.PageSize, query.Keyword);
                if (result.IsSuccess && result.Data != null)
                {
                    Consultations.Clear();
                    foreach (var consultation in result.Data.Items)
                    {
                        Consultations.Add(consultation);
                    }
                }
                else
                {
                    ShowErrorMessage($"加载数据失败: {result.Message}");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "加载诊疗记录失败");
                // 加载数据失败
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

        private void ViewDetails()
        {
            if (SelectedConsultation != null)
            {
                // 简单的详情显示，不涉及复杂导航
                Logger.LogInformation("查看诊疗记录详情: {ConsultationId}", SelectedConsultation.Id);
            }
        }

        #endregion 数据操作
    }
}
