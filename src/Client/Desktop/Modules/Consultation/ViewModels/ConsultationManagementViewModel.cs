using System.Collections.ObjectModel;
using System.Windows.Input;
using LYBT.Desktop.Core.Interfaces.Services;
using LYBT.Desktop.Core.ViewModels.Base;
using LYBT.Shared.Interfaces.Services;
using LYBT.Shared.Models.Contracts.Consultation;
using Microsoft.Extensions.Logging;
using Prism.Commands;

namespace LYBT.Desktop.Consultation.ViewModels
{

    /// <summary>
    /// 看诊记录管理视图模型 - 简化版
    /// 只负责显示和基本管理看诊记录，不包含复杂的流程控制
    /// </summary>
    public class ConsultationManagementViewModel : SessionAwareViewModel
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

        private bool _isLoading;

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
            ISessionManager sessionManager,
            INotificationService notificationService,
            ILogger<ConsultationManagementViewModel> logger)
            : base(sessionManager, notificationService, logger)
        {
            _consultationService = consultationService ?? throw new ArgumentNullException(nameof(consultationService));

            LoadDataCommand = new DelegateCommand(async () => await LoadDataAsync());
            SearchCommand = new DelegateCommand(async () => await SearchAsync());
            RefreshCommand = new DelegateCommand(async () => await RefreshAsync());
            ViewDetailsCommand = new DelegateCommand(ViewDetails, () => SelectedConsultation != null)
                .ObservesProperty(() => SelectedConsultation);

            // ✅ 修复: 使用Task.Run安全初始化，防止未处理异常
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
                LogError(ex, "初始化看诊管理失败");

                // 提供用户友好的错误提示
                ShowError("看诊管理模块初始化失败，请尝试刷新页面");
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

                var result = await _consultationService.GetPagedAsync(query);
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
                    ShowError($"加载数据失败: {result.Message}");
                }
            }
            catch (Exception ex)
            {
                LogError(ex, "加载看诊记录失败");
                ShowError("加载数据失败，请重试");
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
                ShowInfo($"看诊记录详情:\n患者ID: {SelectedConsultation.PatientId}\n看诊时间: {SelectedConsultation.ConsultationTime:yyyy-MM-dd HH:mm}\n诊断: {SelectedConsultation.Diagnosis ?? "暂无"}");
            }
        }

        #endregion 数据操作
    }
}
