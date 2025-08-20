using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using LYBT.Desktop.Core.Coordinators;
using LYBT.Desktop.Core.Managers;
// UltraThink v2.0: 移除Info模型引用，直接使用DTO
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Desktop.Core.Interfaces.Services;
using LYBT.Desktop.Core.Services;
using LYBT.Desktop.Core.ViewModels.Base;
using LYBT.Desktop.Core.ViewModels.MedicalCase;
using LYBT.Desktop.Services;
using LYBT.Desktop.MedicalCase.Services;
using LYBT.Shared.Interfaces.Services;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Logging;
using Prism.Commands;
using Prism.Regions;
using Prism.Services.Dialogs;
// UltraThink四层架构重构：使用新的三层架构组件实现医疗案例管理

namespace LYBT.Desktop.MedicalCase.ViewModels
{
    /// <summary>
    /// 医疗案例管理视图模型（UltraThink架构重构版）
    /// 使用新的三层架构：PaginationCoordinator + SearchManager + NewBaseListViewModel
    /// 实现完全的关注点分离和单一职责原则
    /// </summary>
    public class MedicalCaseListViewModel : NewBaseListViewModel<MedicalCaseDto>
    {
        #region Fields

        private readonly MedicalCaseModuleService _medicalCaseService;
        private readonly ICustomDialogService _dialogService;
        private readonly IRegionManager _regionManager;
        private readonly IMapper _mapper;
        
        private ObservableCollection<MedicalCaseViewModel> _medicalCaseViewModels = new();
        private MedicalCaseViewModel? _selectedMedicalCaseViewModel;
        private ObservableCollection<string> _statusFilters = new();
        private string _selectedStatusFilter = "全部";
        private DateTime? _startDate;
        private DateTime? _endDate;
        private bool _onlyUrgent;

        #endregion

        #region Properties

        /// <summary>医疗案例视图模型集合 - 替代原始的MedicalCaseInfo集合</summary>
        public ObservableCollection<MedicalCaseViewModel> MedicalCaseViewModels
        {
            get => _medicalCaseViewModels;
            set => SetProperty(ref _medicalCaseViewModels, value);
        }

        /// <summary>选中的医疗案例视图模型</summary>
        public MedicalCaseViewModel? SelectedMedicalCaseViewModel
        {
            get => _selectedMedicalCaseViewModel;
            set
            {
                if (SetProperty(ref _selectedMedicalCaseViewModel, value))
                {
                    // 更新命令状态
                    EditCommand.RaiseCanExecuteChanged();
                    DeleteCommand.RaiseCanExecuteChanged();
                    ViewDetailsCommand.RaiseCanExecuteChanged();
                    StartConsultationCommand.RaiseCanExecuteChanged();
                    CompleteCommand.RaiseCanExecuteChanged();
                    CancelCommand.RaiseCanExecuteChanged();
                }
            }
        }

        /// <summary>状态筛选列表</summary>
        public ObservableCollection<string> StatusFilters
        {
            get => _statusFilters;
            set => SetProperty(ref _statusFilters, value);
        }

        /// <summary>选中的状态筛选</summary>
        public string SelectedStatusFilter
        {
            get => _selectedStatusFilter;
            set
            {
                if (SetProperty(ref _selectedStatusFilter, value))
                {
                    // 状态变更时重新加载数据
                    _ = RefreshDataAsync();
                }
            }
        }

        /// <summary>开始日期筛选</summary>
        public DateTime? StartDate
        {
            get => _startDate;
            set
            {
                if (SetProperty(ref _startDate, value))
                {
                    // 日期变更时重新加载数据
                    _ = RefreshDataAsync();
                }
            }
        }

        /// <summary>结束日期筛选</summary>
        public DateTime? EndDate
        {
            get => _endDate;
            set
            {
                if (SetProperty(ref _endDate, value))
                {
                    // 日期变更时重新加载数据
                    _ = RefreshDataAsync();
                }
            }
        }

        /// <summary>只显示紧急案例</summary>
        public bool OnlyUrgent
        {
            get => _onlyUrgent;
            set
            {
                if (SetProperty(ref _onlyUrgent, value))
                {
                    // 紧急筛选变更时重新加载数据
                    _ = RefreshDataAsync();
                }
            }
        }

        /// <summary>批量选中的医疗案例数量</summary>
        public int SelectedMedicalCasesCount => MedicalCaseViewModels.Count(mc => mc.IsSelected);

        /// <summary>是否有选中的医疗案例</summary>
        public bool HasSelectedMedicalCases => SelectedMedicalCasesCount > 0;

        /// <summary>紧急案例数量</summary>
        public int UrgentCasesCount => MedicalCaseViewModels.Count(mc => mc.IsUrgent);

        /// <summary>今日案例数量</summary>
        public int TodayCasesCount => MedicalCaseViewModels.Count(mc => mc.IsToday);

        #endregion

        #region Commands

        public DelegateCommand AddCommand { get; private set; }
        public DelegateCommand<MedicalCaseViewModel> EditCommand { get; private set; }
        public DelegateCommand<MedicalCaseViewModel> DeleteCommand { get; private set; }
        public DelegateCommand<MedicalCaseViewModel> ViewDetailsCommand { get; private set; }
        public DelegateCommand<MedicalCaseViewModel> StartConsultationCommand { get; private set; }
        public DelegateCommand<MedicalCaseViewModel> CompleteCommand { get; private set; }
        public DelegateCommand<MedicalCaseViewModel> CancelCommand { get; private set; }
        public DelegateCommand BatchStartConsultationCommand { get; private set; }
        public DelegateCommand BatchCompleteCommand { get; private set; }
        public DelegateCommand BatchCancelCommand { get; private set; }
        public DelegateCommand ClearSelectionCommand { get; private set; }
        public DelegateCommand SelectAllCommand { get; private set; }
        public DelegateCommand ClearFiltersCommand { get; private set; }
        public DelegateCommand ExportCommand { get; private set; }
        public DelegateCommand ShowTodayCasesCommand { get; private set; }
        public DelegateCommand ShowUrgentCasesCommand { get; private set; }

        #endregion

        #region Constructor

        public MedicalCaseListViewModel(
            MedicalCaseModuleService medicalCaseService,
            ICustomDialogService dialogService,
            IRegionManager regionManager,
            IMapper mapper,
            ISessionManager sessionManager,
            INotificationService notificationService,
            ILogger<MedicalCaseListViewModel> logger,
            IPaginationCoordinator? paginationCoordinator = null,
            ISearchManager? searchManager = null)
            : base(sessionManager, notificationService, logger, paginationCoordinator, searchManager)
        {
            _medicalCaseService = medicalCaseService ?? throw new ArgumentNullException(nameof(medicalCaseService));
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
            _regionManager = regionManager ?? throw new ArgumentNullException(nameof(regionManager));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            
            // 初始化协调器 - 不需要赋值，通过构造函数参数传递给基类
            // PaginationCoordinator和SearchManager在基类中已经定义

            InitializeCommands();
            InitializeStatusFilters();
            
            // 监听选择状态变化
            MedicalCaseViewModels.CollectionChanged += (s, e) => UpdateSelectionProperties();
            
            // 初始化数据
            _ = InitializeAsync();
        }

        #endregion

        #region Command Initialization

        private new void InitializeCommands()
        {
            AddCommand = new DelegateCommand(async () => await AddMedicalCaseAsync());
            EditCommand = new DelegateCommand<MedicalCaseViewModel>(async medicalCase => await EditMedicalCaseAsync(medicalCase), CanExecuteMedicalCaseCommand);
            DeleteCommand = new DelegateCommand<MedicalCaseViewModel>(async medicalCase => await DeleteMedicalCaseAsync(medicalCase), CanExecuteMedicalCaseCommand);
            ViewDetailsCommand = new DelegateCommand<MedicalCaseViewModel>(async medicalCase => await ViewDetailsAsync(medicalCase), CanExecuteMedicalCaseCommand);
            StartConsultationCommand = new DelegateCommand<MedicalCaseViewModel>(async medicalCase => await StartConsultationAsync(medicalCase), CanExecuteMedicalCaseCommand);
            CompleteCommand = new DelegateCommand<MedicalCaseViewModel>(async medicalCase => await CompleteMedicalCaseAsync(medicalCase), CanExecuteMedicalCaseCommand);
            CancelCommand = new DelegateCommand<MedicalCaseViewModel>(async medicalCase => await CancelMedicalCaseAsync(medicalCase), CanExecuteMedicalCaseCommand);
            
            BatchStartConsultationCommand = new DelegateCommand(async () => await BatchStartConsultationAsync(), () => HasSelectedMedicalCases);
            BatchCompleteCommand = new DelegateCommand(async () => await BatchCompleteAsync(), () => HasSelectedMedicalCases);
            BatchCancelCommand = new DelegateCommand(async () => await BatchCancelAsync(), () => HasSelectedMedicalCases);
            ClearSelectionCommand = new DelegateCommand(ClearSelection, () => HasSelectedMedicalCases);
            SelectAllCommand = new DelegateCommand(SelectAll);
            ClearFiltersCommand = new DelegateCommand(ClearFilters);
            
            ExportCommand = new DelegateCommand(async () => await ExportMedicalCasesAsync());
            ShowTodayCasesCommand = new DelegateCommand(ShowTodayCases);
            ShowUrgentCasesCommand = new DelegateCommand(ShowUrgentCases);
        }

        private bool CanExecuteMedicalCaseCommand(MedicalCaseViewModel medicalCase)
        {
            return medicalCase != null && !IsLoading;
        }

        #endregion

        #region Initialization

        private async Task InitializeAsync()
        {
            await RefreshDataAsync();
        }

        private void InitializeStatusFilters()
        {
            StatusFilters.Clear();
            StatusFilters.Add("全部");
            StatusFilters.Add("已挂号");
            StatusFilters.Add("看诊中");
            StatusFilters.Add("已完成");
            StatusFilters.Add("已取消");
            SelectedStatusFilter = "全部";
        }

        #endregion

        #region Data Loading Override

        protected override async Task<ServiceResult<PagedResult<MedicalCaseDto>>> LoadDataAsync(PagedQueryBaseDto request)
        {
            // 转换为医疗案例查询DTO，包含筛选条件
            var medicalCaseQuery = new MedicalCaseQueryDto
            {
                PageIndex = request.CurrentPage,
                PageSize = request.PageSize,
                Keyword = request.SearchKeyword,
                // Status = GetStatusFromFilter(SelectedStatusFilter), // 移除不匹配的属性
                StartDate = StartDate,
                EndDate = EndDate,
                // OnlyUrgent = OnlyUrgent // MedicalCaseQueryDto没有这个属性
            };

            return await _medicalCaseService.GetPagedAsync(medicalCaseQuery);
        }

        protected override void OnDataLoaded(PagedResult<MedicalCaseDto> data)
        {
            base.OnDataLoaded(data);
            
            // 将MedicalCaseDto转换为MedicalCaseViewModel
            UpdateMedicalCaseViewModels(data.Items);
        }

        protected override void OnDataLoadFailed(string errorMessage)
        {
            base.OnDataLoadFailed(errorMessage);
            
            // 清空医疗案例视图模型
            MedicalCaseViewModels.Clear();
            UpdateSelectionProperties();
            
            // 显示错误
            _ = _dialogService.ShowErrorAsync(errorMessage, "加载失败");
        }

        #endregion
        
        #region Medical Case ViewModels Management

        private void UpdateMedicalCaseViewModels(System.Collections.Generic.List<MedicalCaseDto> medicalCaseDtos)
        {
            // 保存当前选择状态
            var selectedIds = MedicalCaseViewModels.Where(mc => mc.IsSelected).Select(mc => mc.Id).ToHashSet();
            
            // 清空并重新创建
            MedicalCaseViewModels.Clear();
            
            foreach (var dto in medicalCaseDtos)
            {
                // UltraThink v2.0: 直接使用DTO创建MedicalCaseViewModel
                var medicalCaseViewModel = MedicalCaseViewModel.Create(dto);
                
                // 恢复选择状态
                if (selectedIds.Contains(medicalCaseViewModel.Id))
                {
                    medicalCaseViewModel.IsSelected = true;
                }
                
                // 监听选择状态变化
                medicalCaseViewModel.State.PropertyChanged += (s, e) =>
                {
                    if (e.PropertyName == nameof(MedicalCaseStateViewModel.IsSelected))
                    {
                        UpdateSelectionProperties();
                    }
                };
                
                MedicalCaseViewModels.Add(medicalCaseViewModel);
            }
            
            UpdateSelectionProperties();
            UpdateStatistics();
        }

        private void UpdateSelectionProperties()
        {
            RaisePropertyChanged(nameof(SelectedMedicalCasesCount));
            RaisePropertyChanged(nameof(HasSelectedMedicalCases));
            
            BatchStartConsultationCommand.RaiseCanExecuteChanged();
            BatchCompleteCommand.RaiseCanExecuteChanged();
            BatchCancelCommand.RaiseCanExecuteChanged();
            ClearSelectionCommand.RaiseCanExecuteChanged();
        }

        private void UpdateStatistics()
        {
            RaisePropertyChanged(nameof(UrgentCasesCount));
            RaisePropertyChanged(nameof(TodayCasesCount));
        }

        #endregion

        #region CRUD Operations

        private async Task AddMedicalCaseAsync()
        {
            try
            {
                // TODO: 实现医疗案例创建对话框
                await _dialogService.ShowInformationAsync("新增医疗案例功能开发中", "提示");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "添加医疗案例失败");
                await _dialogService.ShowErrorAsync($"添加医疗案例失败: {ex.Message}", "错误");
            }
        }

        private async Task EditMedicalCaseAsync(MedicalCaseViewModel medicalCaseViewModel)
        {
            if (medicalCaseViewModel == null) return;
            
            try
            {
                // TODO: 实现医疗案例编辑对话框
                await _dialogService.ShowInformationAsync($"编辑医疗案例 {medicalCaseViewModel.PatientName} 功能开发中", "提示");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "编辑医疗案例失败: {MedicalCaseId}", medicalCaseViewModel.Id);
                await _dialogService.ShowErrorAsync($"编辑医疗案例失败: {ex.Message}", "错误");
            }
        }

        private async Task DeleteMedicalCaseAsync(MedicalCaseViewModel medicalCaseViewModel)
        {
            if (medicalCaseViewModel == null) return;
            
            var confirm = await _dialogService.ShowConfirmationAsync(
                $"确定要删除医疗案例吗？\n患者：{medicalCaseViewModel.PatientName}\n医生：{medicalCaseViewModel.DoctorName}\n此操作不可恢复。",
                "确认删除");

            if (confirm)
            {
                try
                {
                    medicalCaseViewModel.StartDeleting();
                    
                    var result = await _medicalCaseService.DeleteAsync(medicalCaseViewModel.Id);

                    if (result.IsSuccess)
                    {
                        await RefreshDataAsync();
                        await _dialogService.ShowInformationAsync("医疗案例删除成功", "成功");
                    }
                    else
                    {
                        await _dialogService.ShowErrorAsync(
                            result.ErrorMessage ?? "医疗案例删除失败",
                            "错误");
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "删除医疗案例失败: {MedicalCaseId}", medicalCaseViewModel.Id);
                    await _dialogService.ShowErrorAsync($"医疗案例删除失败: {ex.Message}", "错误");
                }
                finally
                {
                    medicalCaseViewModel.EndDeleting();
                }
            }
        }

        #endregion

        #region Business Operations

        private async Task ViewDetailsAsync(MedicalCaseViewModel medicalCaseViewModel)
        {
            if (medicalCaseViewModel == null) return;

            try
            {
                // 导航到详情界面
                _regionManager.RequestNavigate("MainContentRegion", $"MedicalCaseDetailView?MedicalCaseId={medicalCaseViewModel.Id}&ViewMode=Detail");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "查看医疗案例详情失败: {MedicalCaseId}", medicalCaseViewModel.Id);
                await _dialogService.ShowErrorAsync($"查看医疗案例详情失败: {ex.Message}", "错误");
            }
        }

        private async Task StartConsultationAsync(MedicalCaseViewModel medicalCaseViewModel)
        {
            if (medicalCaseViewModel == null) return;

            if (!medicalCaseViewModel.CanStartConsultation)
            {
                await _dialogService.ShowWarningAsync("当前医疗案例状态不允许开始看诊", "无法操作");
                return;
            }

            try
            {
                medicalCaseViewModel.StartStartingConsultation();
                
                var result = await _medicalCaseService.StartConsultationAsync(medicalCaseViewModel.Id);

                if (result.IsSuccess)
                {
                    // 导航到看诊界面
                    _regionManager.RequestNavigate("MainContentRegion", 
                        $"ConsultationMainView?MedicalCaseId={medicalCaseViewModel.Id}&PatientId={medicalCaseViewModel.PatientId}&ConsultationMode=Start");
                    
                    await RefreshDataAsync();
                    await _dialogService.ShowInformationAsync("已成功开始看诊", "成功");
                }
                else
                {
                    await _dialogService.ShowErrorAsync(
                        result.ErrorMessage ?? "开始看诊失败",
                        "错误");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "开始看诊失败: {MedicalCaseId}", medicalCaseViewModel.Id);
                await _dialogService.ShowErrorAsync($"开始看诊失败: {ex.Message}", "错误");
            }
            finally
            {
                medicalCaseViewModel.EndStartingConsultation();
            }
        }

        private async Task CompleteMedicalCaseAsync(MedicalCaseViewModel medicalCaseViewModel)
        {
            if (medicalCaseViewModel == null) return;

            if (!medicalCaseViewModel.CanComplete)
            {
                await _dialogService.ShowWarningAsync("当前医疗案例状态不允许完成", "无法操作");
                return;
            }

            var confirm = await _dialogService.ShowConfirmationAsync(
                $"确定要完成医疗案例吗？\n患者：{medicalCaseViewModel.PatientName}",
                "确认完成");

            if (confirm)
            {
                try
                {
                    medicalCaseViewModel.StartCompleting();
                    
                    // TODO: 可以添加诊断输入对话框
                    var result = await _medicalCaseService.CompleteConsultationAsync(medicalCaseViewModel.Id);

                    if (result.IsSuccess)
                    {
                        await RefreshDataAsync();
                        await _dialogService.ShowInformationAsync("医疗案例已完成", "成功");
                    }
                    else
                    {
                        await _dialogService.ShowErrorAsync(
                            result.ErrorMessage ?? "完成医疗案例失败",
                            "错误");
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "完成医疗案例失败: {MedicalCaseId}", medicalCaseViewModel.Id);
                    await _dialogService.ShowErrorAsync($"完成医疗案例失败: {ex.Message}", "错误");
                }
                finally
                {
                    medicalCaseViewModel.EndCompleting();
                }
            }
        }

        private async Task CancelMedicalCaseAsync(MedicalCaseViewModel medicalCaseViewModel)
        {
            if (medicalCaseViewModel == null) return;

            if (!medicalCaseViewModel.CanCancel)
            {
                await _dialogService.ShowWarningAsync("当前医疗案例状态不允许取消", "无法操作");
                return;
            }

            // TODO: 实现取消原因输入对话框
            var reason = "用户取消"; // 临时使用固定原因

            var confirm = await _dialogService.ShowConfirmationAsync(
                $"确定要取消医疗案例吗？\n患者：{medicalCaseViewModel.PatientName}\n原因：{reason}",
                "确认取消");

            if (confirm)
            {
                try
                {
                    medicalCaseViewModel.StartCancelling();
                    
                    var result = await _medicalCaseService.CancelAsync(medicalCaseViewModel.Id, reason);

                    if (result.IsSuccess)
                    {
                        await RefreshDataAsync();
                        await _dialogService.ShowInformationAsync("医疗案例已取消", "成功");
                    }
                    else
                    {
                        await _dialogService.ShowErrorAsync(
                            result.ErrorMessage ?? "取消医疗案例失败",
                            "错误");
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "取消医疗案例失败: {MedicalCaseId}", medicalCaseViewModel.Id);
                    await _dialogService.ShowErrorAsync($"取消医疗案例失败: {ex.Message}", "错误");
                }
                finally
                {
                    medicalCaseViewModel.EndCancelling();
                }
            }
        }

        #endregion

        #region Batch Operations

        private async Task BatchStartConsultationAsync()
        {
            var selectedMedicalCases = MedicalCaseViewModels.Where(mc => mc.IsSelected && mc.CanStartConsultation).ToList();
            if (!selectedMedicalCases.Any())
            {
                await _dialogService.ShowWarningAsync("没有可以开始看诊的医疗案例", "警告");
                return;
            }

            var confirm = await _dialogService.ShowConfirmationAsync(
                $"确定要批量开始看诊选中的 {selectedMedicalCases.Count} 个医疗案例吗？",
                "批量开始看诊");

            if (confirm)
            {
                try
                {
                    // UltraThink v2.0: 移除了批量操作，使用单个操作循环
                    var successCount = 0;
                    var failureCount = 0;
                    
                    foreach (var medicalCase in selectedMedicalCases)
                    {
                        var result = await _medicalCaseService.StartConsultationAsync(medicalCase.Id);
                        if (result.IsSuccess)
                        {
                            successCount++;
                        }
                        else
                        {
                            failureCount++;
                        }
                    }
                    
                    await RefreshDataAsync();
                    if (successCount > 0)
                    {
                        await _dialogService.ShowInformationAsync($"已成功开始看诊 {successCount} 个医疗案例" + 
                            (failureCount > 0 ? $"，{failureCount} 个失败" : ""), "成功");
                    }
                    else
                    {
                        await _dialogService.ShowErrorAsync("批量开始看诊失败", "错误");
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "批量开始看诊失败");
                    await _dialogService.ShowErrorAsync($"批量开始看诊失败: {ex.Message}", "错误");
                }
            }
        }

        private async Task BatchCompleteAsync()
        {
            var selectedMedicalCases = MedicalCaseViewModels.Where(mc => mc.IsSelected && mc.CanComplete).ToList();
            if (!selectedMedicalCases.Any())
            {
                await _dialogService.ShowWarningAsync("没有可以完成的医疗案例", "警告");
                return;
            }

            var confirm = await _dialogService.ShowConfirmationAsync(
                $"确定要批量完成选中的 {selectedMedicalCases.Count} 个医疗案例吗？",
                "批量完成");

            if (confirm)
            {
                try
                {
                    // UltraThink v2.0: 移除了批量操作，使用单个操作循环
                    var successCount = 0;
                    var failureCount = 0;
                    
                    foreach (var medicalCase in selectedMedicalCases)
                    {
                        var result = await _medicalCaseService.CompleteConsultationAsync(medicalCase.Id);
                        if (result.IsSuccess)
                        {
                            successCount++;
                        }
                        else
                        {
                            failureCount++;
                        }
                    }
                    
                    await RefreshDataAsync();
                    if (successCount > 0)
                    {
                        await _dialogService.ShowInformationAsync($"已成功完成 {successCount} 个医疗案例" + 
                            (failureCount > 0 ? $"，{failureCount} 个失败" : ""), "成功");
                    }
                    else
                    {
                        await _dialogService.ShowErrorAsync("批量完成失败", "错误");
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "批量完成医疗案例失败");
                    await _dialogService.ShowErrorAsync($"批量完成失败: {ex.Message}", "错误");
                }
            }
        }

        private async Task BatchCancelAsync()
        {
            var selectedMedicalCases = MedicalCaseViewModels.Where(mc => mc.IsSelected && mc.CanCancel).ToList();
            if (!selectedMedicalCases.Any())
            {
                await _dialogService.ShowWarningAsync("没有可以取消的医疗案例", "警告");
                return;
            }

            // TODO: 实现取消原因输入对话框
            var reason = "批量取消"; // 临时使用固定原因

            var confirm = await _dialogService.ShowConfirmationAsync(
                $"确定要批量取消选中的 {selectedMedicalCases.Count} 个医疗案例吗？\n原因：{reason}",
                "批量取消");

            if (confirm)
            {
                try
                {
                    // UltraThink v2.0: 移除了批量操作，使用单个操作循环
                    var successCount = 0;
                    var failureCount = 0;
                    
                    foreach (var medicalCase in selectedMedicalCases)
                    {
                        var result = await _medicalCaseService.CancelAsync(medicalCase.Id, reason);
                        if (result.IsSuccess)
                        {
                            successCount++;
                        }
                        else
                        {
                            failureCount++;
                        }
                    }
                    
                    await RefreshDataAsync();
                    if (successCount > 0)
                    {
                        await _dialogService.ShowInformationAsync($"已成功取消 {successCount} 个医疗案例" + 
                            (failureCount > 0 ? $"，{failureCount} 个失败" : ""), "成功");
                    }
                    else
                    {
                        await _dialogService.ShowErrorAsync("批量取消失败", "错误");
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "批量取消医疗案例失败");
                    await _dialogService.ShowErrorAsync($"批量取消失败: {ex.Message}", "错误");
                }
            }
        }

        #endregion

        #region Filter Operations

        private void ClearFilters()
        {
            SelectedStatusFilter = "全部";
            StartDate = null;
            EndDate = null;
            OnlyUrgent = false;
            SearchManager?.ClearSearch();
        }

        private void ShowTodayCases()
        {
            StartDate = DateTime.Today;
            EndDate = DateTime.Today.AddDays(1).AddTicks(-1);
            SelectedStatusFilter = "全部";
        }

        private void ShowUrgentCases()
        {
            OnlyUrgent = true;
            SelectedStatusFilter = "全部";
        }

        private LYBT.Shared.Models.Enums.MedicalCaseStatus? GetStatusFromFilter(string statusFilter)
        {
            return statusFilter switch
            {
                "已挂号" => LYBT.Shared.Models.Enums.MedicalCaseStatus.Registered,
                "看诊中" => LYBT.Shared.Models.Enums.MedicalCaseStatus.InConsultation,
                "已完成" => LYBT.Shared.Models.Enums.MedicalCaseStatus.Completed,
                "已取消" => LYBT.Shared.Models.Enums.MedicalCaseStatus.Cancelled,
                _ => null
            };
        }

        #endregion

        #region Export Operations

        private async Task ExportMedicalCasesAsync()
        {
            try
            {
                // TODO: 实现医疗案例导出功能
                await _dialogService.ShowInformationAsync("医疗案例导出功能开发中", "提示");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "导出医疗案例失败");
                await _dialogService.ShowErrorAsync($"导出医疗案例失败: {ex.Message}", "错误");
            }
        }

        #endregion

        #region Selection Management

        private void ClearSelection()
        {
            foreach (var medicalCase in MedicalCaseViewModels)
            {
                medicalCase.IsSelected = false;
            }
        }

        private void SelectAll()
        {
            foreach (var medicalCase in MedicalCaseViewModels)
            {
                medicalCase.IsSelected = true;
            }
        }

        #endregion
    }
}