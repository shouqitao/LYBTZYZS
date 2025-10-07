using System.Collections.ObjectModel;
using LYBT.Desktop.Infrastructure.Interfaces;
using LYBT.Desktop.Models.ViewModels.Base;
using LYBT.Shared.Interfaces.Services;
using LYBT.Shared.Models.Contracts.Prescriptions;
using Microsoft.Extensions.Logging;
using Prism.Commands;
using Prism.Events;
using Prism.Regions;

namespace LYBT.Desktop.Modules.Prescriptions.ViewModels
{
    /// <summary>
    /// 处方管理视图模型 - UltraThink精简架构
    /// 作为处方模块的主导航和管理容器
    /// </summary>
    public class PrescriptionManagementViewModel : UnifiedViewModelBase
    {
        #region 服务依赖

        private readonly IPrescriptionService _prescriptionService;

        #endregion

        #region 数据属性

        private ObservableCollection<PrescriptionDto> _prescriptions = new();
        private PrescriptionDto? _selectedPrescription;
        private string _searchText = string.Empty;
        private DateTime? _startDate;
        private DateTime? _endDate;
        private int _totalCount;
        private int _currentPage = 1;
        private int _pageSize = 20;

        /// <summary>
        /// 处方列表
        /// </summary>
        public ObservableCollection<PrescriptionDto> Prescriptions
        {
            get => _prescriptions;
            set => SetProperty(ref _prescriptions, value);
        }

        /// <summary>
        /// 选中的处方
        /// </summary>
        public PrescriptionDto? SelectedPrescription
        {
            get => _selectedPrescription;
            set
            {
                if (SetProperty(ref _selectedPrescription, value))
                {
                    UpdateCommandStates();
                }
            }
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
        /// 开始日期
        /// </summary>
        public DateTime? StartDate
        {
            get => _startDate;
            set => SetProperty(ref _startDate, value);
        }

        /// <summary>
        /// 结束日期
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
        /// 创建处方命令
        /// </summary>
        public DelegateCommand CreateCommand { get; }

        /// <summary>
        /// 编辑处方命令
        /// </summary>
        public DelegateCommand EditCommand { get; }

        /// <summary>
        /// 删除处方命令
        /// </summary>
        public DelegateCommand DeleteCommand { get; }

        /// <summary>
        /// 查看详情命令
        /// </summary>
        public DelegateCommand ViewDetailCommand { get; }

        /// <summary>
        /// 打印命令
        /// </summary>
        public DelegateCommand PrintCommand { get; }

        /// <summary>
        /// 刷新命令
        /// </summary>
        public DelegateCommand RefreshCommand { get; }

        /// <summary>
        /// 上一页命令
        /// </summary>
        public DelegateCommand PreviousPageCommand { get; }

        /// <summary>
        /// 下一页命令
        /// </summary>
        public DelegateCommand NextPageCommand { get; }


        /// <summary>
        /// 添加处方命令（别名）
        /// </summary>
        public DelegateCommand AddPrescriptionCommand { get; }

        /// <summary>
        /// 清除筛选命令
        /// </summary>
        public DelegateCommand ClearFiltersCommand { get; }

        /// <summary>
        /// 导出处方命令
        /// </summary>
        public DelegateCommand ExportPrescriptionsCommand { get; }

        /// <summary>
        /// 查看处方命令（DataGrid 行命令）
        /// </summary>
        public DelegateCommand<PrescriptionDto> ViewPrescriptionCommand { get; }

        /// <summary>
        /// 查看患者历史命令（DataGrid 行命令）
        /// </summary>
        public DelegateCommand<PrescriptionDto> ViewPatientHistoryCommand { get; }

        /// <summary>
        /// 编辑处方命令（DataGrid 行命令）
        /// </summary>
        public DelegateCommand<PrescriptionDto> EditPrescriptionCommand { get; }

        /// <summary>
        /// 复制处方命令（DataGrid 行命令）
        /// </summary>
        public DelegateCommand<PrescriptionDto> CopyPrescriptionCommand { get; }

        /// <summary>
        /// 删除处方命令（DataGrid 行命令）
        /// </summary>
        public DelegateCommand<PrescriptionDto> DeletePrescriptionCommand { get; }

        #endregion

        #region 构造函数

        public PrescriptionManagementViewModel(
            IPrescriptionService prescriptionService,
            IEventAggregator eventAggregator,
            ILoggerFactory loggerFactory,
            IRegionManager regionManager,
            ISessionManager? sessionManager = null,
            IUserNotificationService? userNotificationService = null)
            : base(eventAggregator, loggerFactory, regionManager, sessionManager, userNotificationService)
        {
            _prescriptionService = prescriptionService ?? throw new ArgumentNullException(nameof(prescriptionService));

            // 初始化命令
            LoadDataCommand = new DelegateCommand(async () => await LoadDataAsync());
            SearchCommand = new DelegateCommand(async () => await SearchAsync());
            CreateCommand = new DelegateCommand(Create);
            AddPrescriptionCommand = CreateCommand; // 别名
            EditCommand = new DelegateCommand(Edit, CanEdit);
            DeleteCommand = new DelegateCommand(async () => await DeleteAsync(), CanDelete);
            ViewDetailCommand = new DelegateCommand(ViewDetail, CanViewDetail);
            PrintCommand = new DelegateCommand(Print, CanPrint);
            RefreshCommand = new DelegateCommand(async () => await RefreshAsync());
            PreviousPageCommand = new DelegateCommand(async () => await PreviousPageAsync(), CanPreviousPage);
            NextPageCommand = new DelegateCommand(async () => await NextPageAsync(), CanNextPage);

            // DataGrid 行命令
            ViewPrescriptionCommand = new DelegateCommand<PrescriptionDto>(ViewPrescriptionItem, item => item != null);
            ViewPatientHistoryCommand = new DelegateCommand<PrescriptionDto>(ViewPatientHistory, item => item != null);
            EditPrescriptionCommand = new DelegateCommand<PrescriptionDto>(EditPrescriptionItem, item => item != null && !IsBusy);
            CopyPrescriptionCommand = new DelegateCommand<PrescriptionDto>(CopyPrescription, item => item != null && !IsBusy);
            DeletePrescriptionCommand = new DelegateCommand<PrescriptionDto>(async item => await DeletePrescriptionItemAsync(item), item => item != null && !IsBusy);

            // 其他命令
            ClearFiltersCommand = new DelegateCommand(ClearFilters, () => !string.IsNullOrEmpty(SearchText) || StartDate.HasValue || EndDate.HasValue);
            ExportPrescriptionsCommand = new DelegateCommand(async () => await ExportPrescriptionsAsync(), () => Prescriptions.Count > 0 && !IsBusy);

            // 属性变更时刷新命令状态
            PropertyChanged += (s, e) => UpdateCommandStates();
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
                SetIsBusy(true, "正在加载处方列表...");

                var result = await _prescriptionService.GetPagedAsync(CurrentPage, PageSize, SearchText);
                if (result.IsSuccess && result.Data != null)
                {
                    Prescriptions.Clear();
                    foreach (var item in result.Data.Items)
                    {
                        Prescriptions.Add(item);
                    }
                    TotalCount = result.Data.TotalCount;
                }
                else
                {
                    await ShowErrorMessageAsync($"加载处方列表失败: {result.ErrorMessage}");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "加载处方列表时发生异常");
                await ShowErrorMessageAsync("加载处方列表时发生系统错误，请稍后重试");
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
        /// 创建处方
        /// </summary>
        private void Create()
        {
            NavigateTo("MainRegion", "PrescriptionComposerView");
        }

        /// <summary>
        /// 编辑处方
        /// </summary>
        private void Edit()
        {
            if (SelectedPrescription != null)
            {
                var parameters = new NavigationParameters
                {
                    { "PrescriptionId", SelectedPrescription.Id }
                };
                NavigateTo("MainRegion", "PrescriptionComposerView", parameters);
            }
        }

        /// <summary>
        /// 删除处方
        /// </summary>
        private async Task DeleteAsync()
        {
            if (SelectedPrescription == null) return;

            var confirmed = await ShowConfirmMessageAsync($"确定要删除处方吗？");
            if (!confirmed) return;

            try
            {
                SetIsBusy(true, "正在删除处方...");

                var result = await _prescriptionService.DeleteAsync(SelectedPrescription.Id);
                if (result.IsSuccess)
                {
                    await ShowSuccessMessageAsync("处方删除成功");
                    await LoadDataAsync();
                }
                else
                {
                    await ShowErrorMessageAsync($"删除处方失败: {result.ErrorMessage}");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "删除处方时发生异常");
                await ShowErrorMessageAsync("删除处方时发生系统错误，请稍后重试");
            }
            finally
            {
                SetIsBusy(false);
            }
        }

        /// <summary>
        /// 查看详情
        /// </summary>
        private void ViewDetail()
        {
            if (SelectedPrescription != null)
            {
                var parameters = new NavigationParameters
                {
                    { "PrescriptionId", SelectedPrescription.Id },
                    { "IsReadOnly", true }
                };
                NavigateTo("MainRegion", "PrescriptionDetailView", parameters);
            }
        }

        /// <summary>
        /// 打印
        /// </summary>
        private void Print()
        {
            if (SelectedPrescription != null)
            {
                Logger.LogInformation("打印处方: {PrescriptionId}", SelectedPrescription.Id);
                // 实现打印逻辑
                ShowInfoMessage("打印功能暂未实现");
            }
        }

        /// <summary>
        /// 刷新
        /// </summary>
        private async Task RefreshAsync()
        {
            await LoadDataAsync();
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

        /// <summary>
        /// 查看处方项
        /// </summary>
        private void ViewPrescriptionItem(PrescriptionDto prescription)
        {
            if (prescription != null)
            {
                SelectedPrescription = prescription;
                ViewDetail();
            }
        }

        /// <summary>
        /// 查看患者历史
        /// </summary>
        private void ViewPatientHistory(PrescriptionDto prescription)
        {
            if (prescription != null)
            {
                Logger.LogInformation("查看患者历史功能开发中: PatientId={PatientId}", prescription.PatientId);
                ShowInfoMessage("查看患者历史功能开发中");
            }
        }

        /// <summary>
        /// 编辑处方项
        /// </summary>
        private void EditPrescriptionItem(PrescriptionDto prescription)
        {
            if (prescription != null)
            {
                SelectedPrescription = prescription;
                Edit();
            }
        }

        /// <summary>
        /// 复制处方
        /// </summary>
        private void CopyPrescription(PrescriptionDto prescription)
        {
            if (prescription != null)
            {
                Logger.LogInformation("复制处方功能开发中: {PrescriptionId}", prescription.Id);
                ShowInfoMessage("复制处方功能开发中");
            }
        }

        /// <summary>
        /// 删除处方项
        /// </summary>
        private async Task DeletePrescriptionItemAsync(PrescriptionDto prescription)
        {
            if (prescription != null)
            {
                SelectedPrescription = prescription;
                await DeleteAsync();
            }
        }

        /// <summary>
        /// 清除筛选
        /// </summary>
        private void ClearFilters()
        {
            SearchText = string.Empty;
            StartDate = null;
            EndDate = null;
            _ = SearchAsync();
        }

        /// <summary>
        /// 导出处方
        /// </summary>
        private async Task ExportPrescriptionsAsync()
        {
            try
            {
                SetIsBusy(true, "正在导出处方...");
                Logger.LogInformation("导出处方功能开发中");
                await ShowSuccessMessageAsync("导出处方功能开发中");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "导出处方时发生异常");
                await ShowErrorMessageAsync("导出处方时发生系统错误");
            }
            finally
            {
                SetIsBusy(false);
            }
        }

        private bool CanEdit() => SelectedPrescription != null && !IsBusy;
        private bool CanDelete() => SelectedPrescription != null && !IsBusy;
        private bool CanViewDetail() => SelectedPrescription != null;
        private bool CanPrint() => SelectedPrescription != null;
        private bool CanPreviousPage() => CurrentPage > 1 && !IsBusy;
        private bool CanNextPage()
        {
            var totalPages = (int)Math.Ceiling((double)TotalCount / PageSize);
            return CurrentPage < totalPages && !IsBusy;
        }

        private void UpdateCommandStates()
        {
            EditCommand.RaiseCanExecuteChanged();
            DeleteCommand.RaiseCanExecuteChanged();
            ViewDetailCommand.RaiseCanExecuteChanged();
            PrintCommand.RaiseCanExecuteChanged();
            PreviousPageCommand.RaiseCanExecuteChanged();
            NextPageCommand.RaiseCanExecuteChanged();
            ViewPrescriptionCommand?.RaiseCanExecuteChanged();
            ViewPatientHistoryCommand?.RaiseCanExecuteChanged();
            EditPrescriptionCommand?.RaiseCanExecuteChanged();
            CopyPrescriptionCommand?.RaiseCanExecuteChanged();
            DeletePrescriptionCommand?.RaiseCanExecuteChanged();
            ClearFiltersCommand?.RaiseCanExecuteChanged();
            ExportPrescriptionsCommand?.RaiseCanExecuteChanged();
        }

        #endregion
    }
}
