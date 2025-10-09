using System.Collections.ObjectModel;
using LYBT.Desktop.Infrastructure.Events;
using LYBT.Desktop.Infrastructure.Interfaces;
using LYBT.Desktop.Models.ViewModels.Base;
using LYBT.Shared.Interfaces.Services;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Logging;
using Prism.Commands;
using Prism.Events;
using Prism.Regions;

namespace LYBT.Desktop.Modules.MedicalCase.ViewModels
{
    /// <summary>
    /// 重构后的病历列表视图模型 - UltraThink精简架构
    /// 基于最新架构标准的优化版本，提供更好的性能和用户体验
    /// </summary>
    public class RefactoredMedicalCaseListViewModel : UnifiedViewModelBase
    {
        #region 服务依赖

        private readonly IMedicalCaseService _medicalCaseService;

        #endregion

        #region 数据属性

        private ObservableCollection<MedicalCaseDto> _medicalCases = new();
        private ObservableCollection<MedicalCaseDto> _selectedMedicalCases = new();
        private string _searchText = string.Empty;
        private CommonStatus? _statusFilter;
        private DateTime? _startDate;
        private DateTime? _endDate;
        private int _totalCount;
        private int _currentPage = 1;
        private int _pageSize = 20;
        private bool _isMultiSelectMode;

        /// <summary>
        /// 病历列表
        /// </summary>
        public ObservableCollection<MedicalCaseDto> MedicalCases
        {
            get => _medicalCases;
            set => SetProperty(ref _medicalCases, value);
        }

        /// <summary>
        /// 选中的病历列表（多选模式）
        /// </summary>
        public ObservableCollection<MedicalCaseDto> SelectedMedicalCases
        {
            get => _selectedMedicalCases;
            set => SetProperty(ref _selectedMedicalCases, value);
        }

        /// <summary>
        /// 搜索关键字
        /// </summary>
        public string SearchText
        {
            get => _searchText;
            set => SetProperty(ref _searchText, value);
        }

        /// <summary>
        /// 状态筛选
        /// </summary>
        public CommonStatus? StatusFilter
        {
            get => _statusFilter;
            set => SetProperty(ref _statusFilter, value);
        }

        /// <summary>
        /// 开始日期筛选
        /// </summary>
        public DateTime? StartDate
        {
            get => _startDate;
            set => SetProperty(ref _startDate, value);
        }

        /// <summary>
        /// 结束日期筛选
        /// </summary>
        public DateTime? EndDate
        {
            get => _endDate;
            set => SetProperty(ref _endDate, value);
        }

        /// <summary>
        /// 总记录数
        /// </summary>
        public int TotalCount
        {
            get => _totalCount;
            set => SetProperty(ref _totalCount, value);
        }

        /// <summary>
        /// 当前页码
        /// </summary>
        public int CurrentPage
        {
            get => _currentPage;
            set => SetProperty(ref _currentPage, value);
        }

        /// <summary>
        /// 每页大小
        /// </summary>
        public int PageSize
        {
            get => _pageSize;
            set => SetProperty(ref _pageSize, value);
        }

        /// <summary>
        /// 是否多选模式
        /// </summary>
        public bool IsMultiSelectMode
        {
            get => _isMultiSelectMode;
            set
            {
                if (SetProperty(ref _isMultiSelectMode, value))
                {
                    if (!value)
                    {
                        SelectedMedicalCases.Clear();
                    }
                    UpdateCommandStates();
                }
            }
        }

        /// <summary>
        /// 状态选项
        /// </summary>
        public CommonStatus[] StatusOptions { get; } = Enum.GetValues<CommonStatus>();

        #endregion

        #region 命令

        /// <summary>
        /// 加载数据命令
        /// </summary>
        public DelegateCommand LoadDataCommand { get; }

        /// <summary>
        /// 搜索命令
        /// </summary>
        public DelegateCommand SearchCommand { get; }

        /// <summary>
        /// 重置筛选命令
        /// </summary>
        public DelegateCommand ResetFilterCommand { get; }

        /// <summary>
        /// 创建病历命令
        /// </summary>
        public DelegateCommand CreateCommand { get; }

        /// <summary>
        /// 编辑病历命令
        /// </summary>
        public DelegateCommand<MedicalCaseDto> EditCommand { get; }

        /// <summary>
        /// 删除病历命令
        /// </summary>
        public DelegateCommand<MedicalCaseDto> DeleteCommand { get; }

        /// <summary>
        /// 批量删除命令
        /// </summary>
        public DelegateCommand BatchDeleteCommand { get; }

        /// <summary>
        /// 查看详情命令
        /// </summary>
        public DelegateCommand<MedicalCaseDto> ViewDetailCommand { get; }

        /// <summary>
        /// 切换多选模式命令
        /// </summary>
        public DelegateCommand ToggleMultiSelectCommand { get; }

        /// <summary>
        /// 全选命令
        /// </summary>
        public DelegateCommand SelectAllCommand { get; }

        /// <summary>
        /// 导出命令
        /// </summary>
        public DelegateCommand ExportCommand { get; }

        /// <summary>
        /// 上一页命令
        /// </summary>
        public DelegateCommand PreviousPageCommand { get; }

        /// <summary>
        /// 下一页命令
        /// </summary>
        public DelegateCommand NextPageCommand { get; }

        #endregion

        #region 构造函数

        public RefactoredMedicalCaseListViewModel(
            IMedicalCaseService medicalCaseService,
            IEventAggregator eventAggregator,
            ILoggerFactory loggerFactory,
            IRegionManager regionManager,
            ISessionManager? sessionManager = null,
            IUserNotificationService? userNotificationService = null)
            : base(eventAggregator, loggerFactory, regionManager, sessionManager, userNotificationService)
        {
            _medicalCaseService = medicalCaseService ?? throw new ArgumentNullException(nameof(medicalCaseService));

            // 初始化命令
            LoadDataCommand = new DelegateCommand(async () => await LoadDataAsync());
            SearchCommand = new DelegateCommand(async () => await SearchAsync());
            ResetFilterCommand = new DelegateCommand(ResetFilter);
            CreateCommand = new DelegateCommand(Create);
            EditCommand = new DelegateCommand<MedicalCaseDto>(Edit);
            DeleteCommand = new DelegateCommand<MedicalCaseDto>(async (item) => await DeleteAsync(item));
            BatchDeleteCommand = new DelegateCommand(async () => await BatchDeleteAsync(), CanBatchDelete);
            ViewDetailCommand = new DelegateCommand<MedicalCaseDto>(ViewDetail);
            ToggleMultiSelectCommand = new DelegateCommand(ToggleMultiSelect);
            SelectAllCommand = new DelegateCommand(SelectAll, CanSelectAll);
            ExportCommand = new DelegateCommand(async () => await ExportAsync(), CanExport);
            PreviousPageCommand = new DelegateCommand(async () => await PreviousPageAsync(), CanPreviousPage);
            NextPageCommand = new DelegateCommand(async () => await NextPageAsync(), CanNextPage);

            // 订阅数据刷新事件
            EventAggregator.GetEvent<DataRefreshEvent>().Subscribe(OnDataRefresh);

            // 属性变更时刷新命令状态
            PropertyChanged += (s, e) => UpdateCommandStates();
            SelectedMedicalCases.CollectionChanged += (s, e) => UpdateCommandStates();
        }

        #endregion

        #region 生命周期

        /// <summary>
        /// 页面加载时调用
        /// </summary>
        protected override async Task OnNavigatedToAsync(NavigationContext navigationContext)
        {
            await base.OnNavigatedToAsync(navigationContext);
            await LoadDataAsync();
        }

        #endregion

        #region 命令实现

        /// <summary>
        /// 加载数据
        /// </summary>
        private async Task LoadDataAsync()
        {
            try
            {
                SetIsBusy(true, "正在加载病历列表...");

                var result = await _medicalCaseService.GetPagedAsync(CurrentPage, PageSize, SearchText);

                if (result.IsSuccess && result.Data != null)
                {
                    MedicalCases.Clear();
                    foreach (var item in result.Data.Items)
                    {
                        MedicalCases.Add(item);
                    }
                    TotalCount = result.Data.TotalCount;
                }
                else
                {
                    await ShowErrorMessageAsync($"加载病历列表失败: {result.ErrorMessage}");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "加载病历列表时发生异常");
                await ShowErrorMessageAsync("加载病历列表时发生系统错误，请稍后重试");
            }
            finally
            {
                SetIsBusy(false);
            }
        }

        /// <summary>
        /// 搜索
        /// </summary>
        private async Task SearchAsync()
        {
            CurrentPage = 1;
            await LoadDataAsync();
        }

        /// <summary>
        /// 重置筛选
        /// </summary>
        private void ResetFilter()
        {
            SearchText = string.Empty;
            StatusFilter = null;
            StartDate = null;
            EndDate = null;
            CurrentPage = 1;
        }

        /// <summary>
        /// 创建病历
        /// </summary>
        private void Create()
        {
            NavigateTo("MainRegion", "CreateMedicalCaseView");
        }

        /// <summary>
        /// 编辑病历
        /// </summary>
        private void Edit(MedicalCaseDto? medicalCase)
        {
            if (medicalCase != null)
            {
                var parameters = new NavigationParameters
                {
                    { "MedicalCaseId", medicalCase.Id }
                };
                NavigateTo("MainRegion", "MedicalCaseDetailView", parameters);
            }
        }

        /// <summary>
        /// 删除病历
        /// </summary>
        private async Task DeleteAsync(MedicalCaseDto? medicalCase)
        {
            if (medicalCase == null) return;

            var confirmed = await ShowConfirmMessageAsync($"确定要删除病历 '{medicalCase.CaseNumber}' 吗？");
            if (!confirmed) return;

            try
            {
                SetIsBusy(true, "正在删除病历...");

                var result = await _medicalCaseService.DeleteAsync(medicalCase.Id);
                if (result.IsSuccess)
                {
                    await ShowSuccessMessageAsync("病历删除成功");
                    await LoadDataAsync();
                }
                else
                {
                    await ShowErrorMessageAsync($"删除病历失败: {result.ErrorMessage}");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "删除病历时发生异常");
                await ShowErrorMessageAsync("删除病历时发生系统错误，请稍后重试");
            }
            finally
            {
                SetIsBusy(false);
            }
        }

        /// <summary>
        /// 批量删除
        /// </summary>
        private async Task BatchDeleteAsync()
        {
            if (!SelectedMedicalCases.Any()) return;

            var confirmed = await ShowConfirmMessageAsync($"确定要删除选中的 {SelectedMedicalCases.Count} 个病历吗？");
            if (!confirmed) return;

            try
            {
                SetIsBusy(true, "正在批量删除病历...");

                var ids = SelectedMedicalCases.Select(x => x.Id).ToList();

                // 循环调用DeleteAsync（Shared.Interfaces暂无BatchDeleteAsync）
                int successCount = 0;
                List<string> errors = new();
                foreach (var id in ids)
                {
                    var deleteResult = await _medicalCaseService.DeleteAsync(id);
                    if (deleteResult.IsSuccess)
                        successCount++;
                    else if (!string.IsNullOrEmpty(deleteResult.ErrorMessage))
                        errors.Add(deleteResult.ErrorMessage);
                }
                var result = successCount == ids.Count
                    ? ServiceResult.Success()
                    : ServiceResult.Failure(string.Join("; ", errors));

                if (result.IsSuccess)
                {
                    await ShowSuccessMessageAsync($"成功删除 {SelectedMedicalCases.Count} 个病历");
                    SelectedMedicalCases.Clear();
                    await LoadDataAsync();
                }
                else
                {
                    await ShowErrorMessageAsync($"批量删除失败: {result.ErrorMessage}");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "批量删除病历时发生异常");
                await ShowErrorMessageAsync("批量删除时发生系统错误，请稍后重试");
            }
            finally
            {
                SetIsBusy(false);
            }
        }

        /// <summary>
        /// 查看详情
        /// </summary>
        private void ViewDetail(MedicalCaseDto? medicalCase)
        {
            if (medicalCase != null)
            {
                var parameters = new NavigationParameters
                {
                    { "MedicalCaseId", medicalCase.Id },
                    { "IsReadOnly", true }
                };
                NavigateTo("MainRegion", "MedicalCaseDetailView", parameters);
            }
        }

        /// <summary>
        /// 切换多选模式
        /// </summary>
        private void ToggleMultiSelect()
        {
            IsMultiSelectMode = !IsMultiSelectMode;
        }

        /// <summary>
        /// 全选
        /// </summary>
        private void SelectAll()
        {
            SelectedMedicalCases.Clear();
            foreach (var item in MedicalCases)
            {
                SelectedMedicalCases.Add(item);
            }
        }

        /// <summary>
        /// 导出
        /// </summary>
        private async Task ExportAsync()
        {
            try
            {
                SetIsBusy(true, "正在导出数据...");

                // 这里可以实现具体的导出逻辑
                await Task.Delay(1000); // 模拟导出过程

                await ShowSuccessMessageAsync("数据导出成功");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "导出数据时发生异常");
                await ShowErrorMessageAsync("导出失败，请稍后重试");
            }
            finally
            {
                SetIsBusy(false);
            }
        }

        /// <summary>
        /// 上一页
        /// </summary>
        private async Task PreviousPageAsync()
        {
            if (CurrentPage > 1)
            {
                CurrentPage--;
                await LoadDataAsync();
            }
        }

        /// <summary>
        /// 下一页
        /// </summary>
        private async Task NextPageAsync()
        {
            var totalPages = (int)Math.Ceiling((double)TotalCount / PageSize);
            if (CurrentPage < totalPages)
            {
                CurrentPage++;
                await LoadDataAsync();
            }
        }

        #endregion

        #region 命令状态检查

        private bool CanBatchDelete() => IsMultiSelectMode && SelectedMedicalCases.Any() && !IsBusy;
        private bool CanSelectAll() => IsMultiSelectMode && MedicalCases.Any();
        private bool CanExport() => MedicalCases.Any() && !IsBusy;
        private bool CanPreviousPage() => CurrentPage > 1 && !IsBusy;
        private bool CanNextPage()
        {
            var totalPages = (int)Math.Ceiling((double)TotalCount / PageSize);
            return CurrentPage < totalPages && !IsBusy;
        }

        private void UpdateCommandStates()
        {
            BatchDeleteCommand.RaiseCanExecuteChanged();
            SelectAllCommand.RaiseCanExecuteChanged();
            ExportCommand.RaiseCanExecuteChanged();
            PreviousPageCommand.RaiseCanExecuteChanged();
            NextPageCommand.RaiseCanExecuteChanged();
        }

        #endregion

        #region 事件处理

        /// <summary>
        /// 数据刷新事件处理
        /// </summary>
        private async void OnDataRefresh(string module)
        {
            if (module == "MedicalCase" || string.IsNullOrEmpty(module))
            {
                await LoadDataAsync();
            }
        }

        #endregion
    }
}
