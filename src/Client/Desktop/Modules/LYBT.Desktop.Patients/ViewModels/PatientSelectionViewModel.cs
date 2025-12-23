using System.Collections.ObjectModel;
using System.Net.Http;
using LYBT.Desktop.Contracts.Api;
using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Infrastructure.Events;
using LYBT.Shared.ExceptionHandling.Mappers;
using LYBT.Desktop.MedicalCase.Models;
using LYBT.Desktop.MedicalCase.Services;
using LYBT.Desktop.Models.ViewModels.Base;
using LYBT.Desktop.Patients.Services;
using LYBT.Desktop.Patients.ViewModels.Components;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Logging;
using Prism.Commands;
using Prism.Events;
using Prism.Regions;
using Prism.Services.Dialogs;

namespace LYBT.Desktop.Patients.ViewModels;

/// <summary>
/// 患者选择ViewModel - Issue #1557
/// OpenSpec: refactor-oversized-viewmodels Task 1.1 - 精简至500行以下
/// </summary>
public class PatientSelectionViewModel : UnifiedViewModelBase
{
    #region 依赖

    private readonly PatientCommandHandler _commandHandler;
    private readonly MedicalCaseDataManager _medicalCaseDataManager;
    private readonly IDialogService _dialogService;
    private readonly IMedicalCaseApi _medicalCaseApi;
    private readonly PatientSearchManager _searchManager;
    private readonly UnfinishedCaseHandler _unfinishedCaseHandler;
    private readonly PendingQueueManager _pendingQueueManager;
    private readonly MedicalCaseStartCoordinator _medicalCaseStartCoordinator;
    private PatientSelectionCommandExecutor? _commandExecutor;
    private System.Threading.Timer? _searchDebounceTimer;
    private bool _disposed;
    private Prism.Events.SubscriptionToken? _patientUpdatedToken;
    private Prism.Events.SubscriptionToken? _patientCreatedToken;  // OpenSpec: refactor-patient-selection Task 1.3

    #endregion

    #region 属性

    private Guid _medicalCaseFlowId;
    public Guid MedicalCaseFlowId { get => _medicalCaseFlowId; set => SetProperty(ref _medicalCaseFlowId, value); }

    public ObservableCollection<PatientListDto> Patients => _searchManager.Patients;

    private PatientListDto? _selectedPatient;
    public PatientListDto? SelectedPatient
    {
        get => _selectedPatient;
        set
        {
            if (SetProperty(ref _selectedPatient, value))
            {
                SelectPatientCommand.RaiseCanExecuteChanged();
                if (value != null)
                {
                    ClearPendingSelection();
                    // 选中列表项时异步加载详情
                    _ = LoadPatientDetailAsync(value.Id);
                }
            }
        }
    }

    private string _searchKeyword = string.Empty;
    public string SearchKeyword
    {
        get => _searchKeyword;
        set
        {
            if (SetProperty(ref _searchKeyword, value))
            {
                SearchCommand.RaiseCanExecuteChanged();
                ScheduleSearch();
            }
        }
    }

    private int _currentPage = 1;
    public int CurrentPage { get => _currentPage; set => SetProperty(ref _currentPage, value); }

    private int _totalPages = 1;
    public int TotalPages { get => _totalPages; set => SetProperty(ref _totalPages, value); }

    private int _totalCount;
    public int TotalCount { get => _totalCount; set => SetProperty(ref _totalCount, value); }

    public ObservableCollection<PendingMedicalCaseDto> PendingQueue => _pendingQueueManager.PendingQueue;
    public bool HasNoPendingPatients => PendingQueue?.Count == 0;

    private PendingMedicalCaseDto? _selectedPendingPatient;
    public PendingMedicalCaseDto? SelectedPendingPatient
    {
        get => _selectedPendingPatient;
        set
        {
            if (SetProperty(ref _selectedPendingPatient, value) && value != null)
            {
                ClearPatientSelection();
                CacheUnfinishedCase(value);
                _ = _pendingQueueManager.LoadPatientForPendingCaseAsync(value.PatientId);
            }
        }
    }

    private PatientDetailDto? _currentPatient;
    public PatientDetailDto? CurrentPatient
    {
        get => _currentPatient;
        set { if (SetProperty(ref _currentPatient, value)) StartConsultationCommand.RaiseCanExecuteChanged(); }
    }

    private string _statusBarMessage = string.Empty;
    public string StatusBarMessage { get => _statusBarMessage; set => SetProperty(ref _statusBarMessage, value); }

    private bool _statusBarIsError;
    public bool StatusBarIsError { get => _statusBarIsError; set => SetProperty(ref _statusBarIsError, value); }

    private bool _isRefreshing;
    public bool IsRefreshing { get => _isRefreshing; set => SetProperty(ref _isRefreshing, value); }

    #endregion

    #region 命令
    public DelegateCommand SearchCommand { get; }
    public DelegateCommand NewPatientCommand { get; }
    public DelegateCommand SelectPatientCommand { get; }
    public DelegateCommand<PatientListDto> DoubleClickPatientCommand { get; }
    public DelegateCommand PreviousPageCommand { get; }
    public DelegateCommand NextPageCommand { get; }
    public DelegateCommand BackToHomeCommand { get; }
    public DelegateCommand StartConsultationCommand { get; }
    public DelegateCommand RefreshPendingQueueCommand { get; }

    #endregion

    #region 构造函数

    public PatientSelectionViewModel(
        PatientCommandHandler commandHandler,
        MedicalCaseDataManager medicalCaseDataManager,
        PatientSearchManager searchManager,
        UnfinishedCaseHandler unfinishedCaseHandler,
        PendingQueueManager pendingQueueManager,
        MedicalCaseStartCoordinator medicalCaseStartCoordinator,
        IDialogService dialogService,
        IMedicalCaseApi medicalCaseApi,
        IEventAggregator eventAggregator,
        ILoggerFactory loggerFactory,
        IRegionManager regionManager,
        ISessionManager? sessionManager = null,
        IUserNotificationService? userNotificationService = null,
        ICommonDialogService? commonDialogService = null)
        : base(eventAggregator, loggerFactory, regionManager, sessionManager, userNotificationService, commonDialogService)
    {
        _commandHandler = commandHandler ?? throw new ArgumentNullException(nameof(commandHandler));
        _medicalCaseDataManager = medicalCaseDataManager ?? throw new ArgumentNullException(nameof(medicalCaseDataManager));
        _searchManager = searchManager ?? throw new ArgumentNullException(nameof(searchManager));
        _unfinishedCaseHandler = unfinishedCaseHandler ?? throw new ArgumentNullException(nameof(unfinishedCaseHandler));
        _pendingQueueManager = pendingQueueManager ?? throw new ArgumentNullException(nameof(pendingQueueManager));
        _medicalCaseStartCoordinator = medicalCaseStartCoordinator ?? throw new ArgumentNullException(nameof(medicalCaseStartCoordinator));
        _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
        _medicalCaseApi = medicalCaseApi ?? throw new ArgumentNullException(nameof(medicalCaseApi));

        // 命令初始化
        SearchCommand = new DelegateCommand(async () => await ExecuteSearchAsync(), () => !IsBusy);
        NewPatientCommand = new DelegateCommand(ExecuteNewPatient);
        SelectPatientCommand = new DelegateCommand(ExecuteSelectPatient, () => SelectedPatient != null);
        DoubleClickPatientCommand = new DelegateCommand<PatientListDto>(ExecuteDoubleClickPatient);
        PreviousPageCommand = new DelegateCommand(async () => await ExecutePreviousPageAsync(), () => _searchManager.CanPreviousPage());
        NextPageCommand = new DelegateCommand(async () => await ExecuteNextPageAsync(), () => _searchManager.CanNextPage());
        BackToHomeCommand = new DelegateCommand(ExecuteBackToHome);
        StartConsultationCommand = new DelegateCommand(ExecuteStartConsultation, () => CurrentPatient != null && !IsBusy);
        RefreshPendingQueueCommand = new DelegateCommand(async () => await RefreshPendingQueueAsync(), () => !IsRefreshing).ObservesProperty(() => IsRefreshing);

        // 初始化命令执行器
        _commandExecutor = new PatientSelectionCommandExecutor(
            _searchManager, Logger,
            (p, t, c) => { CurrentPage = p; TotalPages = t; TotalCount = c; },
            (busy, msg) => SetIsBusy(busy, msg),
            ShowErrorMessageAsync,
            PreviousPageCommand, NextPageCommand);

        // 事件订阅
        SubscribeToEvents();
        Logger.LogInformation("PatientSelectionViewModel已初始化");
    }

    #endregion

    #region 命令实现

    private async Task ExecuteSearchAsync() => await _commandExecutor!.ExecuteSearchAsync(SearchKeyword);

    private async Task ExecutePreviousPageAsync() => await _commandExecutor!.ExecutePreviousPageAsync(SearchKeyword);

    private async Task ExecuteNextPageAsync() => await _commandExecutor!.ExecuteNextPageAsync(SearchKeyword);

    private void ExecuteNewPatient()
    {
        _dialogService.ShowDialog("QuickCreatePatientDialog", new DialogParameters(), result =>
        {
            if (result.Result == ButtonResult.OK)
            {
                var newPatient = result.Parameters.GetValue<PatientDetailDto>("NewPatient");
                if (newPatient != null)
                {
                    Logger.LogInformation("新建患者成功：{Name}", newPatient.Name);
                    // 将DetailDto转换为ListDto插入列表
                    var listDto = ToListDto(newPatient);
                    Patients.Insert(0, listDto);
                    SelectedPatient = listDto;
                    // 直接设置CurrentPatient（已有详情数据）
                    CurrentPatient = newPatient;
                    PublishPatientSelectedEvent(newPatient);
                }
            }
        });
    }

    /// <summary>
    /// 将PatientDetailDto转换为PatientListDto
    /// </summary>
    private static PatientListDto ToListDto(PatientDetailDto detail) => new()
    {
        Id = detail.Id,
        Name = detail.Name,
        Gender = detail.Gender,
        Age = detail.Age,
        PhoneNumber = detail.PhoneNumber,
        LastVisitTime = detail.LastVisitTime,
        VisitCount = detail.VisitCount,
        PinYinCode = detail.PinYinCode,
        Status = detail.Status,
        CreatedAt = detail.CreatedAt
    };

    private void ExecuteSelectPatient()
    {
        if (CurrentPatient != null)
        {
            Logger.LogInformation("选择患者：{Name}", CurrentPatient.Name);
            PublishPatientSelectedEvent(CurrentPatient);
        }
    }

    private void ExecuteDoubleClickPatient(PatientListDto? patient)
    {
        if (patient != null)
        {
            SelectedPatient = patient;
            // CurrentPatient通过SelectedPatient setter异步加载
            // 发布事件延迟到CurrentPatient加载完成后进行
            _ = WaitAndPublishPatientSelectedAsync(patient.Id);
        }
    }

    private async Task WaitAndPublishPatientSelectedAsync(Guid patientId)
    {
        // 等待CurrentPatient加载
        await LoadPatientDetailAsync(patientId);
        if (CurrentPatient != null)
        {
            PublishPatientSelectedEvent(CurrentPatient);
        }
    }

    private void ExecuteBackToHome()
    {
        var view = SessionManager?.CurrentUser?.Role == UserRole.Admin ? "AdminHomeView" : "ClinicalHomeView";
        RegionManager.RequestNavigate("ContentRegion", view);
    }

    private async void ExecuteStartConsultation()
    {
        if (CurrentPatient == null) return;

        try
        {
            SetIsBusy(true, "正在检查患者医案...");
            var unfinishedCase = await _medicalCaseStartCoordinator.CheckUnfinishedCaseAsync(CurrentPatient.Id);

            if (unfinishedCase != null)
            {
                if (_medicalCaseStartCoordinator.IsOtherDoctorCase(unfinishedCase))
                {
                    SetIsBusy(false);
                    var doctorName = _medicalCaseStartCoordinator.GetOtherDoctorName(unfinishedCase);
                    await ShowSuccessMessageAsync($"患者「{CurrentPatient.Name}」在{doctorName}处有挂起医案，暂时无法开始新的诊断。");
                    return;
                }
                await HandleUnfinishedCaseAsync(unfinishedCase.Id);
            }
            else
            {
                PublishPatientSelectedEvent(CurrentPatient);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "开始看诊失败");
            await ShowErrorMessageAsync(ClientErrorMessageMapper.GetSafeOperationFailureMessage("开始看诊", ex));
        }
        finally
        {
            SetIsBusy(false);
        }
    }

    private async Task HandleUnfinishedCaseAsync(Guid unfinishedCaseId)
    {
        SetIsBusy(false);
        var choice = await ShowUnfinishedCaseDialogAsync(CurrentPatient!.Name);

        if (choice == 0) return;
        if (choice == 2 || choice == 3) SetIsBusy(true, choice == 2 ? "正在关闭旧医案..." : "正在关闭医案...");

        var result = await _medicalCaseStartCoordinator.HandleUserChoiceAsync(choice, CurrentPatient, unfinishedCaseId, LoadPendingCasesAsync);
        await HandleStartResultAsync(result);
    }

    private async Task HandleStartResultAsync(MedicalCaseStartCoordinator.StartResultData result)
    {
        switch (result.Result)
        {
            case MedicalCaseStartCoordinator.StartResult.ContinueExisting:
                PublishPatientSelectedEvent(CurrentPatient!, result.ExistingMedicalCaseId);
                break;
            case MedicalCaseStartCoordinator.StartResult.CreateNew:
                PublishPatientSelectedEvent(CurrentPatient!);
                break;
            case MedicalCaseStartCoordinator.StartResult.Error:
                await ShowErrorMessageAsync(result.ErrorMessage ?? "操作失败");
                break;
        }
    }

    private async Task RefreshPendingQueueAsync()
    {
        try
        {
            IsRefreshing = true;
            await LoadPendingCasesAsync();
            await ShowSuccessMessageAsync("待诊队列已刷新");
        }
        catch (HttpRequestException ex)
        {
            await ShowErrorMessageAsync(ClientErrorMessageMapper.GetSafeOperationFailureMessage("刷新待诊队列", ex));
        }
        catch (Exception ex)
        {
            await ShowErrorMessageAsync(ClientErrorMessageMapper.GetSafeOperationFailureMessage("刷新待诊队列", ex));
        }
        finally
        {
            IsRefreshing = false;
        }
    }

    #endregion

    #region 导航

    public override void OnNavigatedTo(NavigationContext navigationContext)
    {
        base.OnNavigatedTo(navigationContext);

        var flowId = navigationContext.Parameters.GetValue<Guid>("MedicalCaseFlowId");
        MedicalCaseFlowId = flowId != Guid.Empty ? flowId : Guid.NewGuid();

        var keyword = navigationContext.Parameters.GetValue<string>("SearchKeyword");
        if (!string.IsNullOrEmpty(keyword))
        {
            SearchKeyword = keyword;
            _ = ExecuteSearchAsync();
        }
        else
        {
            _ = _commandExecutor!.LoadInitialAsync();
        }

        _ = LoadPendingCasesAsync();
    }

    public override bool IsNavigationTarget(NavigationContext navigationContext) => false;

    #endregion

    #region 辅助方法

    private void ScheduleSearch()
    {
        _searchDebounceTimer?.Dispose();
        _searchDebounceTimer = new System.Threading.Timer(
            _ => System.Windows.Application.Current.Dispatcher.Invoke(async () => await ExecuteSearchAsync()),
            null, 500, System.Threading.Timeout.Infinite);  // 防抖时间500ms（优化自300ms）
    }

    private void ClearPendingSelection()
    {
        if (_selectedPendingPatient != null)
        {
            _selectedPendingPatient = null;
            RaisePropertyChanged(nameof(SelectedPendingPatient));
        }
    }

    private void ClearPatientSelection()
    {
        if (_selectedPatient != null)
        {
            _selectedPatient = null;
            RaisePropertyChanged(nameof(SelectedPatient));
            SelectPatientCommand.RaiseCanExecuteChanged();
        }
    }

    private void CacheUnfinishedCase(PendingMedicalCaseDto pending)
    {
        if (pending.MedicalCaseId.HasValue && pending.MedicalCaseId.Value != Guid.Empty)
            _unfinishedCaseHandler.SetCache(pending.PatientId, pending.MedicalCaseId.Value);
    }

    private Task<int> ShowUnfinishedCaseDialogAsync(string patientName)
    {
        var tcs = new TaskCompletionSource<int>();
        System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
            var dialog = new Views.UnfinishedCaseDialog();
            dialog.SetPatientName(patientName);
            var mainWindow = System.Windows.Application.Current.MainWindow;
            if (mainWindow != null && mainWindow != dialog) dialog.Owner = mainWindow;
            dialog.ShowDialog();
            tcs.SetResult(dialog.Result);
        });
        return tcs.Task;
    }

    private async Task LoadPendingCasesAsync() => await _pendingQueueManager.LoadPendingCasesAsync();

    /// <summary>
    /// 加载患者详情（从列表选中时）
    /// </summary>
    private async Task LoadPatientDetailAsync(Guid patientId)
    {
        try
        {
            var result = await _commandHandler.GetByIdAsync(patientId);
            if (result.IsSuccess && result.Data != null)
            {
                CurrentPatient = result.Data;
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "加载患者详情失败: {PatientId}", patientId);
        }
    }

    private void PublishPatientSelectedEvent(PatientDetailDto patient, Guid? medicalCaseId = null)
    {
        // OpenSpec: unify-event-system - 使用PatientEvents聚合类
        var payload = new PatientSelectedPayload
        {
            PatientId = patient.Id, PatientName = patient.Name,
            Gender = patient.Gender.ToString(), Age = patient.Age ?? 0,
            PhoneNumber = patient.PhoneNumber ?? string.Empty,
            LastVisitDate = patient.LastVisitTime, VisitCount = patient.VisitCount,
            AllergyHistory = patient.AllergyHistory ?? string.Empty,
            MedicalCaseFlowId = MedicalCaseFlowId, SelectedAt = DateTime.Now
        };
        EventAggregator.GetEvent<PatientEvents.SelectedEvent>().Publish(payload);
        NavigateToMedicalCase(patient, medicalCaseId);
    }

    private void NavigateToMedicalCase(PatientDetailDto patient, Guid? medicalCaseId)
    {
        var parameters = MedicalCaseNavigationParameters.ForClinical(patient.Id, medicalCaseId);
        parameters.Add("CurrentPatient", patient);
        RegionManager.RequestNavigate("ContentRegion", "MedicalCaseWorkspaceView", parameters);
    }

    protected override async Task ShowErrorMessageAsync(string message)
    {
        StatusBarMessage = message;
        StatusBarIsError = true;
        await Task.Delay(3000);
        if (StatusBarMessage == message) { StatusBarMessage = string.Empty; StatusBarIsError = false; }
    }

    protected override async Task ShowSuccessMessageAsync(string message)
    {
        StatusBarMessage = message;
        StatusBarIsError = false;
        await Task.Delay(3000);
        if (StatusBarMessage == message) StatusBarMessage = string.Empty;
    }

    #endregion

    #region 事件处理
    private new void SubscribeToEvents()
    {
        _searchManager.SearchCompleted += (_, e) => Logger.LogDebug("搜索完成: {Count}条，来自缓存: {FromCache}", e.ResultCount, e.FromCache);
        _unfinishedCaseHandler.CaseCheckCompleted += (_, _) => { };
        _unfinishedCaseHandler.CaseClosed += (_, _) => { };
        _pendingQueueManager.PendingQueueLoaded += (_, _) => RaisePropertyChanged(nameof(HasNoPendingPatients));
        _pendingQueueManager.PatientLoaded += (_, e) => CurrentPatient = e.Patient;
        // OpenSpec: unify-event-system - 使用PatientEvents聚合类
        _patientUpdatedToken = EventAggregator.GetEvent<PatientEvents.UpdatedEvent>().Subscribe(OnPatientUpdated);
        _patientCreatedToken = EventAggregator.GetEvent<PatientEvents.CreatedEvent>().Subscribe(OnPatientCreated);
    }

    /// <summary>
    /// 患者创建事件处理
    /// OpenSpec: refactor-patient-selection Task 1.3 - 失效缓存
    /// </summary>
    private void OnPatientCreated(PatientCreatedPayload payload)
    {
        Logger.LogDebug("患者创建事件：{PatientName}，失效搜索缓存", payload.Patient.Name);
        _searchManager.InvalidateCache();
    }

    /// <summary>
    /// 患者更新事件处理
    /// </summary>
    private void OnPatientUpdated(PatientUpdatedPayload payload)
    {
        var patient = payload.Patient;
        // 更新本地状态
        if (CurrentPatient?.Id == patient.Id) CurrentPatient = patient;
        var idx = Patients.ToList().FindIndex(p => p.Id == patient.Id);
        if (idx >= 0) Patients[idx] = ToListDto(patient);

        // OpenSpec: refactor-patient-selection Task 1.3 - 失效缓存
        Logger.LogDebug("患者更新事件：{PatientName}，失效搜索缓存", patient.Name);
        _searchManager.InvalidateCache();
    }

    #endregion

    #region IDisposable
    public new void Dispose()
    {
        Dispose(true);
        base.Dispose();
        GC.SuppressFinalize(this);
    }

    protected override void Dispose(bool disposing)
    {
        if (_disposed) return;
        if (disposing)
        {
            _searchManager.SearchCompleted -= (_, _) => { };
            // OpenSpec: unify-event-system - 使用PatientEvents聚合类
            if (_patientUpdatedToken != null)
            {
                EventAggregator.GetEvent<PatientEvents.UpdatedEvent>().Unsubscribe(_patientUpdatedToken);
                _patientUpdatedToken = null;
            }
            if (_patientCreatedToken != null)
            {
                EventAggregator.GetEvent<PatientEvents.CreatedEvent>().Unsubscribe(_patientCreatedToken);
                _patientCreatedToken = null;
            }
            _searchDebounceTimer?.Dispose();
        }
        _disposed = true;
    }

    #endregion
}
