using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using LYBT.WPF.Client.Core.Interfaces.Services;
using LYBT.WPF.Client.Core.ViewModels;
using LYBT.WPF.Client.Services.Interfaces;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Enums;
using Prism.Commands;
using Prism.Events;
using Refit;

namespace LYBT.WPF.Client.Modules.MedicalCase.ViewModels
{
    /// <summary>
    /// 医疗案例列表视图模型 - 完整版
    /// </summary>
    public class MedicalCaseListViewModel : BaseViewModel
    {
        private readonly IMedicalCaseApiService _medicalCaseApiService;
        private readonly ICommonDialogService _dialogService;

        #region Properties

        private ObservableCollection<MedicalCaseDisplayItem> _medicalCases = new();
        public ObservableCollection<MedicalCaseDisplayItem> MedicalCases
        {
            get => _medicalCases;
            set => SetProperty(ref _medicalCases, value);
        }

        private MedicalCaseDisplayItem? _selectedMedicalCase;
        public MedicalCaseDisplayItem? SelectedMedicalCase
        {
            get => _selectedMedicalCase;
            set => SetProperty(ref _selectedMedicalCase, value);
        }

        private string _searchKeyword = string.Empty;
        public string SearchKeyword
        {
            get => _searchKeyword;
            set => SetProperty(ref _searchKeyword, value);
        }

        private MedicalCaseStatus? _filterStatus;
        public MedicalCaseStatus? FilterStatus
        {
            get => _filterStatus;
            set => SetProperty(ref _filterStatus, value);
        }

        private int _currentPage = 1;
        public int CurrentPage
        {
            get => _currentPage;
            set => SetProperty(ref _currentPage, value);
        }

        private int _pageSize = 20;
        public int PageSize
        {
            get => _pageSize;
            set => SetProperty(ref _pageSize, value);
        }

        private int _totalCount;
        public int TotalCount
        {
            get => _totalCount;
            set => SetProperty(ref _totalCount, value);
        }

        private int _totalPages;
        public int TotalPages
        {
            get => _totalPages;
            set => SetProperty(ref _totalPages, value);
        }

        #endregion

        #region Commands

        public DelegateCommand LoadDataCommand { get; }
        public DelegateCommand SearchCommand { get; }
        public DelegateCommand AddCommand { get; }
        public new DelegateCommand RefreshCommand { get; }
        public DelegateCommand<MedicalCaseDisplayItem> ViewDetailCommand { get; }
        public DelegateCommand<MedicalCaseDisplayItem> StartConsultationCommand { get; }
        public DelegateCommand<MedicalCaseDisplayItem> EditCommand { get; }
        public DelegateCommand<MedicalCaseDisplayItem> DeleteCommand { get; }
        public DelegateCommand PreviousPageCommand { get; }
        public DelegateCommand NextPageCommand { get; }

        #endregion

        public MedicalCaseListViewModel(
            IMedicalCaseApiService medicalCaseApiService,
            ICommonDialogService dialogService,
            IEventAggregator eventAggregator)
            : base(eventAggregator)
        {
            _medicalCaseApiService = medicalCaseApiService;
            _dialogService = dialogService;

            MedicalCases = new ObservableCollection<MedicalCaseDisplayItem>();

            // Initialize Commands
            LoadDataCommand = new DelegateCommand(async () => await LoadDataAsync());
            SearchCommand = new DelegateCommand(async () => await SearchAsync());
            AddCommand = new DelegateCommand(async () => await AddMedicalCaseAsync());
            RefreshCommand = new DelegateCommand(async () => await RefreshAsync());
            ViewDetailCommand = new DelegateCommand<MedicalCaseDisplayItem>(async (item) => await ViewDetailAsync(item));
            StartConsultationCommand = new DelegateCommand<MedicalCaseDisplayItem>(async (item) => await StartConsultationAsync(item));
            EditCommand = new DelegateCommand<MedicalCaseDisplayItem>(async (item) => await EditAsync(item));
            DeleteCommand = new DelegateCommand<MedicalCaseDisplayItem>(async (item) => await DeleteAsync(item));
            PreviousPageCommand = new DelegateCommand(async () => await PreviousPageAsync(), () => CurrentPage > 1);
            NextPageCommand = new DelegateCommand(async () => await NextPageAsync(), () => CurrentPage < TotalPages);

            // Load initial data
            LoadDataCommand.Execute();
        }

        #region Private Methods

        private async Task LoadDataAsync()
        {
            try
            {
                IsLoading = true;

                var response = await _medicalCaseApiService.GetPagedAsync(CurrentPage, PageSize);

                if (response.IsSuccessStatusCode && response.Content != null)
                {
                    var result = response.Content;
                    TotalCount = result.TotalCount;
                    TotalPages = (int)Math.Ceiling((double)TotalCount / PageSize);

                    MedicalCases.Clear();
                    foreach (var dto in result.Items)
                    {
                        MedicalCases.Add(new MedicalCaseDisplayItem(dto));
                    }

                    // Update command states
                    PreviousPageCommand.RaiseCanExecuteChanged();
                    NextPageCommand.RaiseCanExecuteChanged();
                }
                else
                {
                    await HandleApiError(response);
                }
            }
            catch (Exception ex)
            {
                await _dialogService.ShowErrorAsync($"加载数据失败: {ex.Message}", "错误");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task SearchAsync()
        {
            CurrentPage = 1;
            await LoadDataAsync();
        }

        private async Task RefreshAsync()
        {
            SearchKeyword = string.Empty;
            FilterStatus = null;
            CurrentPage = 1;
            await LoadDataAsync();
        }

        private async Task AddMedicalCaseAsync()
        {
            try
            {
                // TODO: 实现新增医疗案例对话框
                await _dialogService.ShowInformationAsync("新增医疗案例功能开发中", "提示");
            }
            catch (Exception ex)
            {
                await _dialogService.ShowErrorAsync($"操作失败: {ex.Message}", "错误");
            }
        }

        private async Task ViewDetailAsync(MedicalCaseDisplayItem item)
        {
            if (item == null) return;

            try
            {
                // TODO: 打开医疗案例详情页面
                await _dialogService.ShowInformationAsync($"查看案例详情: {item.CaseNumber}", "详情");
            }
            catch (Exception ex)
            {
                await _dialogService.ShowErrorAsync($"查看详情失败: {ex.Message}", "错误");
            }
        }

        private async Task StartConsultationAsync(MedicalCaseDisplayItem item)
        {
            if (item == null) return;

            try
            {
                // 发布开始看诊事件，其他模块监听
                EventAggregator.GetEvent<ConsultationStartedEvent>()
                    .Publish(new ConsultationStartedEventArgs 
                    { 
                        MedicalCaseId = item.Id,
                        PatientId = item.PatientId,
                        PatientName = item.PatientName
                    });

                await _dialogService.ShowInformationAsync($"开始为 {item.PatientName} 看诊", "看诊");
            }
            catch (Exception ex)
            {
                await _dialogService.ShowErrorAsync($"启动看诊失败: {ex.Message}", "错误");
            }
        }

        private async Task EditAsync(MedicalCaseDisplayItem item)
        {
            if (item == null) return;

            try
            {
                // TODO: 实现编辑医疗案例对话框
                await _dialogService.ShowInformationAsync($"编辑案例: {item.CaseNumber}", "编辑");
            }
            catch (Exception ex)
            {
                await _dialogService.ShowErrorAsync($"编辑失败: {ex.Message}", "错误");
            }
        }

        private async Task DeleteAsync(MedicalCaseDisplayItem item)
        {
            if (item == null) return;

            try
            {
                var result = await _dialogService.ShowConfirmationAsync(
                    $"确定要删除案例 {item.CaseNumber} 吗？", 
                    "确认删除");

                if (result)
                {
                    var response = await _medicalCaseApiService.DeleteAsync(item.Id);
                    if (response.IsSuccessStatusCode)
                    {
                        await _dialogService.ShowInformationAsync("删除成功", "成功");
                        await LoadDataAsync();
                    }
                    else
                    {
                        await HandleApiError(response);
                    }
                }
            }
            catch (Exception ex)
            {
                await _dialogService.ShowErrorAsync($"删除失败: {ex.Message}", "错误");
            }
        }

        private async Task PreviousPageAsync()
        {
            if (CurrentPage > 1)
            {
                CurrentPage--;
                await LoadDataAsync();
            }
        }

        private async Task NextPageAsync()
        {
            if (CurrentPage < TotalPages)
            {
                CurrentPage++;
                await LoadDataAsync();
            }
        }

        private async Task HandleApiError<T>(ApiResponse<T> response)
        {
            var errorMessage = $"API调用失败 (状态码: {response.StatusCode})";
            
            if (response.Error != null)
            {
                errorMessage += $"\n错误信息: {response.Error.Content}";
            }

            await _dialogService.ShowErrorAsync(errorMessage, "API错误");
        }

        #endregion
    }

    /// <summary>
    /// 医疗案例显示项目
    /// </summary>
    public class MedicalCaseDisplayItem
    {
        public Guid Id { get; set; }
        public string CaseNumber { get; set; }
        public Guid PatientId { get; set; }
        public string PatientName { get; set; }
        public string PatientGender { get; set; }
        public int PatientAge { get; set; }
        public string ChiefComplaint { get; set; }
        public string DoctorName { get; set; }
        public MedicalCaseStatus Status { get; set; }
        public string StatusText { get; set; }
        public DateTime CreateTime { get; set; }
        public string CreateTimeText { get; set; }

        public MedicalCaseDisplayItem() { }

        public MedicalCaseDisplayItem(MedicalCaseDto dto)
        {
            Id = dto.Id;
            CaseNumber = $"MC{dto.Id.ToString().Substring(0, 8).ToUpper()}"; // 生成案例编号
            PatientId = dto.PatientId;
            PatientName = dto.PatientName ?? string.Empty;
            PatientGender = "未知"; // DTO中无此字段，使用默认值
            PatientAge = 0; // DTO中无此字段，使用默认值
            ChiefComplaint = dto.DiagnosisSummary ?? string.Empty; // 使用诊断摘要作为主诉
            DoctorName = dto.DoctorName ?? string.Empty;
            Status = ParseStatus(dto.Status); // 解析字符串状态
            StatusText = dto.Status ?? "未知";
            CreateTime = dto.CreateTime;
            CreateTimeText = dto.CreateTime.ToString("yyyy-MM-dd HH:mm");
        }

        private static MedicalCaseStatus ParseStatus(string status)
        {
            return status?.ToLower() switch
            {
                "registered" or "已挂号" => MedicalCaseStatus.Registered,
                "inconsultation" or "看诊中" => MedicalCaseStatus.InConsultation,
                "completed" or "已完成" => MedicalCaseStatus.Completed,
                "cancelled" or "已取消" => MedicalCaseStatus.Cancelled,
                _ => MedicalCaseStatus.Registered
            };
        }

        private static string GetStatusText(MedicalCaseStatus status)
        {
            return status switch
            {
                MedicalCaseStatus.Registered => "已挂号",
                MedicalCaseStatus.InConsultation => "看诊中",
                MedicalCaseStatus.Completed => "已完成",
                MedicalCaseStatus.Cancelled => "已取消",
                _ => "未知"
            };
        }
    }

    /// <summary>
    /// 开始看诊事件参数
    /// </summary>
    public class ConsultationStartedEventArgs
    {
        public Guid MedicalCaseId { get; set; }
        public Guid PatientId { get; set; }
        public string PatientName { get; set; }
    }

    /// <summary>
    /// 开始看诊事件
    /// </summary>
    public class ConsultationStartedEvent : PubSubEvent<ConsultationStartedEventArgs> { }
}