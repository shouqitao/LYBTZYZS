using System.Windows.Media;
using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Infrastructure.Events;
using LYBT.Shared.ExceptionHandling.Mappers;
using LYBT.Desktop.MedicalCase.Events;
using LYBT.Desktop.MedicalCase.Interfaces;
using LYBT.Desktop.MedicalCase.Models;
using LYBT.Desktop.MedicalCase.Services;
using LYBT.Desktop.MedicalCase.ViewModels.Components;
using LYBT.Desktop.Models.ViewModels.Base;
using LYBT.Desktop.Prescriptions.Interfaces;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Shared.Models.Contracts.Prescriptions;
using Microsoft.Extensions.Logging;
using Prism.Commands;
using Prism.Events;
using Prism.Regions;
using Prism.Services.Dialogs;

namespace LYBT.Desktop.MedicalCase.ViewModels;

/// <summary>
/// 医案工作区ViewModel - 4:6统一看诊界面
/// OpenSpec: refactor-oversized-viewmodels - 重构后 < 500行
/// </summary>
public class MedicalCaseWorkspaceViewModel : UnifiedViewModelBase
{
    #region 字段

    private readonly IRegionManager _regionManager;
    private readonly MedicalCaseDataManager _dataManager;
    private readonly MedicalCaseLifecycleHandler _lifecycleHandler;
    private readonly MedicalCaseDataLoader _dataLoader;
    private readonly ConsultationPanelViewModel _injectedConsultationPanelViewModel;
    private readonly PrescriptionPanelViewModel _injectedPrescriptionPanelViewModel;
    private readonly IActiveConsultationService _activeConsultationService;
    private readonly IDialogService? _dialogService;
    private readonly IAuditRequirementChecker? _auditRequirementChecker;
    private readonly MedicalCaseWorkspaceCoordinator _coordinator;
    private readonly MedicalCaseNavigationHandler _navigationHandler;
    private readonly MedicalCaseEditModeStateMachine _editModeStateMachine;
    private readonly IPrescriptionPrintService? _prescriptionPrintService;

    #endregion

    #region 属性

    private string _patientName = string.Empty;
    public string PatientName { get => _patientName; set => SetProperty(ref _patientName, value); }

    private string _patientInfo = string.Empty;
    public string PatientInfo { get => _patientInfo; set => SetProperty(ref _patientInfo, value); }

    private Guid _medicalCaseId = Guid.Empty;
    public Guid MedicalCaseId { get => _medicalCaseId; set => SetProperty(ref _medicalCaseId, value); }

    private PatientDetailDto? _currentPatient;
    public PatientDetailDto? CurrentPatient { get => _currentPatient; set => SetProperty(ref _currentPatient, value); }

    private ConsultationPanelViewModel? _consultationPanelViewModel;
    public ConsultationPanelViewModel? ConsultationPanelViewModel { get => _consultationPanelViewModel; set => SetProperty(ref _consultationPanelViewModel, value); }

    private PrescriptionPanelViewModel? _prescriptionPanelViewModel;
    public PrescriptionPanelViewModel? PrescriptionPanelViewModel { get => _prescriptionPanelViewModel; set => SetProperty(ref _prescriptionPanelViewModel, value); }

    private bool _isPrescriptionEnabled;
    public bool IsPrescriptionEnabled
    {
        get => _isPrescriptionEnabled;
        set
        {
            if (SetProperty(ref _isPrescriptionEnabled, value))
            {
                UpdateCanComplete();
            }
        }
    }

    private bool _showPrescriptionStatus;
    public bool ShowPrescriptionStatus { get => _showPrescriptionStatus; set => SetProperty(ref _showPrescriptionStatus, value); }

    private string _prescriptionStatusText = "待诊断";
    public string PrescriptionStatusText { get => _prescriptionStatusText; set => SetProperty(ref _prescriptionStatusText, value); }

    private Brush _prescriptionStatusBackground = new SolidColorBrush(Color.FromRgb(158, 158, 158));
    public Brush PrescriptionStatusBackground { get => _prescriptionStatusBackground; set => SetProperty(ref _prescriptionStatusBackground, value); }

    private string _consultationStatusText = "未完成";
    public string ConsultationStatusText { get => _consultationStatusText; set => SetProperty(ref _consultationStatusText, value); }

    private Brush _consultationStatusColor = new SolidColorBrush(Color.FromRgb(255, 152, 0));
    public Brush ConsultationStatusColor { get => _consultationStatusColor; set => SetProperty(ref _consultationStatusColor, value); }

    private string _prescriptionStatusSummary = "待开方";
    public string PrescriptionStatusSummary { get => _prescriptionStatusSummary; set => SetProperty(ref _prescriptionStatusSummary, value); }

    private Brush _prescriptionStatusSummaryColor = new SolidColorBrush(Color.FromRgb(158, 158, 158));
    public Brush PrescriptionStatusSummaryColor { get => _prescriptionStatusSummaryColor; set => SetProperty(ref _prescriptionStatusSummaryColor, value); }

    private bool _canPrintPrescription;
    public bool CanPrintPrescription { get => _canPrintPrescription; set => SetProperty(ref _canPrintPrescription, value); }

    private bool _canComplete;
    public bool CanComplete { get => _canComplete; set => SetProperty(ref _canComplete, value); }

    private bool _isFromManagement;
    public bool IsFromManagement { get => _isFromManagement; set => SetProperty(ref _isFromManagement, value); }

    private bool _hasUnsavedPrescriptionChanges;
    public bool HasUnsavedChanges => _hasUnsavedPrescriptionChanges;
    public bool HasUnsavedPrescriptionChanges
    {
        get => _hasUnsavedPrescriptionChanges;
        private set { if (SetProperty(ref _hasUnsavedPrescriptionChanges, value)) RaisePropertyChanged(nameof(HasUnsavedChanges)); }
    }

    private string _remark = string.Empty;
    public string Remark
    {
        get => _remark;
        set { if (SetProperty(ref _remark, value) && _dataLoader.CachedMedicalCase != null) _dataLoader.CachedMedicalCase.Remark = value; }
    }

    // 委托给状态机的属性
    public MedicalCaseEditModeStateMachine EditModeState => _editModeStateMachine;
    public bool IsEditing => _editModeStateMachine.IsEditing;
    public bool IsReadOnly => _editModeStateMachine.IsReadOnly;
    public bool ShowEditButton => _editModeStateMachine.ShowEditButton;
    public bool ShowEditButtonTopRight => _editModeStateMachine.ShowEditButtonTopRight;
    public bool ShowSaveButton => _editModeStateMachine.ShowSaveButton;
    public bool ShowDraftButton => _editModeStateMachine.ShowDraftButton;
    public bool ShowCompleteButton => _editModeStateMachine.ShowCompleteButton;
    public bool IsHistoricalEditMode => _editModeStateMachine.IsHistoricalEditMode;
    public bool CanEdit => _editModeStateMachine.CanEdit;
    public string EditReason { get => _editModeStateMachine.EditReason; set => _editModeStateMachine.EditReason = value; }
    public WorkspaceMode WorkspaceMode { get => _editModeStateMachine.WorkspaceMode; set => _editModeStateMachine.WorkspaceMode = value; }
    public string HeaderTitle => _editModeStateMachine.HeaderTitle;
    public string BackButtonText => _editModeStateMachine.BackButtonText;

    #endregion

    #region 命令

    public DelegateCommand BackCommand { get; }
    public DelegateCommand BackToPatientSelectionCommand => BackCommand;
    public DelegateCommand SaveAndStayCommand { get; }
    public DelegateCommand SaveDraftCommand => SaveAndStayCommand;
    public DelegateCommand PrintPrescriptionCommand { get; }
    public DelegateCommand CompleteConsultationCommand { get; }
    public DelegateCommand SaveCommand { get; }
    public DelegateCommand EnterEditModeCommand { get; }

    #endregion

    #region 构造函数

    public MedicalCaseWorkspaceViewModel(
        MedicalCaseDataManager dataManager, MedicalCaseLifecycleHandler lifecycleHandler,
        MedicalCaseDataLoader dataLoader, MedicalCaseWorkspaceCoordinator coordinator,
        MedicalCaseNavigationHandler navigationHandler, MedicalCaseEditModeStateMachine editModeStateMachine,
        IRegionManager regionManager, IEventAggregator eventAggregator, ILoggerFactory loggerFactory,
        ConsultationPanelViewModel consultationPanelViewModel, PrescriptionPanelViewModel prescriptionPanelViewModel,
        IActiveConsultationService activeConsultationService, ISessionManager? sessionManager = null,
        ICommonDialogService? commonDialogService = null, IDialogService? dialogService = null,
        IAuditRequirementChecker? auditRequirementChecker = null,
        IPrescriptionPrintService? prescriptionPrintService = null)
        : base(eventAggregator, loggerFactory, regionManager, sessionManager, null, commonDialogService)
    {
        _dataManager = dataManager ?? throw new ArgumentNullException(nameof(dataManager));
        _regionManager = regionManager ?? throw new ArgumentNullException(nameof(regionManager));
        _lifecycleHandler = lifecycleHandler ?? throw new ArgumentNullException(nameof(lifecycleHandler));
        _dataLoader = dataLoader ?? throw new ArgumentNullException(nameof(dataLoader));
        _coordinator = coordinator ?? throw new ArgumentNullException(nameof(coordinator));
        _navigationHandler = navigationHandler ?? throw new ArgumentNullException(nameof(navigationHandler));
        _injectedConsultationPanelViewModel = consultationPanelViewModel ?? throw new ArgumentNullException(nameof(consultationPanelViewModel));
        _injectedPrescriptionPanelViewModel = prescriptionPanelViewModel ?? throw new ArgumentNullException(nameof(prescriptionPanelViewModel));
        _activeConsultationService = activeConsultationService ?? throw new ArgumentNullException(nameof(activeConsultationService));
        _dialogService = dialogService;
        _auditRequirementChecker = auditRequirementChecker;
        _editModeStateMachine = editModeStateMachine ?? throw new ArgumentNullException(nameof(editModeStateMachine));
        _prescriptionPrintService = prescriptionPrintService;

        // 订阅事件
        _editModeStateMachine.EditStateChanged += OnEditStateChanged;
        _lifecycleHandler.ActionCompleted += OnLifecycleActionCompleted;
        _dataLoader.DataLoaded += OnDataLoaded;

        // 配置导航处理器回调
        _navigationHandler.SaveDraftCallback = SaveDraftOnlyAsync;
        _navigationHandler.CancelCaseCallback = CancelCaseOnlyAsync;
        _navigationHandler.CheckAndGetAuditReasonCallback = CheckAndGetAuditReasonAsync;
        _navigationHandler.SetEditReasonCallback = reason => _editModeStateMachine.EditReason = reason;
        _navigationHandler.SetIsEditingCallback = value => { if (value) _editModeStateMachine.EnterEditMode(); else _editModeStateMachine.EnterReadOnlyMode(); };

        // 初始化命令
        BackCommand = new DelegateCommand(async () => await ExecuteBackAsync());
        SaveAndStayCommand = new DelegateCommand(ExecuteSaveAndStay);
        PrintPrescriptionCommand = new DelegateCommand(ExecutePrintPrescription, () => CanPrintPrescription).ObservesProperty(() => CanPrintPrescription);
        CompleteConsultationCommand = new DelegateCommand(ExecuteCompleteConsultation, () => CanComplete).ObservesProperty(() => CanComplete);
        SaveCommand = new DelegateCommand(ExecuteSave, () => _editModeStateMachine.IsEditing);
        EnterEditModeCommand = new DelegateCommand(ExecuteEnterEditMode, () => _editModeStateMachine.CanEnterEditMode);

        // 订阅Prism事件
        EventAggregator.GetEvent<ConsultationCompletedEvent>().Subscribe(OnConsultationCompleted, ThreadOption.UIThread);
        EventAggregator.GetEvent<PrescriptionCompletedEvent>().Subscribe(OnPrescriptionCompleted, ThreadOption.UIThread);
        EventAggregator.GetEvent<PrescriptionSavedEvent>().Subscribe(OnPrescriptionSaved, ThreadOption.UIThread);
    }

    #endregion

    #region 命令实现

    private async void ExecuteSave()
    {
        try
        {
            SetIsBusy(true, "正在保存...");
            // OpenSpec: refactor-medicalcase-aggregate-crud (Phase 4.1) - 使用聚合保存
            // OpenSpec: refactor-diagnosis-fields - 移除SyncRemarkToPanel调用
            var result = await _coordinator.SaveAsync(MedicalCaseId, GetConsultationProvider(), GetPrescriptionProvider(), Remark, EditReason);
            if (result.IsSuccess) { if (IsHistoricalEditMode && !string.IsNullOrWhiteSpace(EditReason)) Logger.LogInformation("历史修改保存，原因: {EditReason}", EditReason); await ShowSuccessMessageAsync("保存成功"); }
            else await ShowErrorMessageAsync(result.ErrorMessage ?? "保存失败");
        }
        catch (Exception ex) { Logger.LogError(ex, "保存医案数据失败"); await ShowErrorMessageAsync(ClientErrorMessageMapper.GetSafeOperationFailureMessage("保存", ex)); }
        finally { SetIsBusy(false); }
    }

    private void ExecuteEnterEditMode()
    {
        if (_editModeStateMachine.EnterEditMode()) Logger.LogInformation("进入编辑模式，MedicalCaseId: {MedicalCaseId}", MedicalCaseId);
        else Logger.LogWarning("无编辑权限，无法进入编辑模式");
    }

    private async Task ExecuteBackAsync() => await _navigationHandler.ExecuteBackAsync(WorkspaceMode, IsReadOnly);

    // OpenSpec: refactor-diagnosis-fields - 移除SyncRemarkToPanel方法，MedicalCaseRemark已从ConsultationPanelViewModel移除

    // OpenSpec: refactor-medicalcase-aggregate-crud (Phase 4.1) - 迁移到IDataProvider
    private IDataProvider? GetConsultationProvider() => ConsultationPanelViewModel;
    private IDataProvider? GetPrescriptionProvider() => PrescriptionPanelViewModel;
    private IValidatable? GetConsultationValidator() => ConsultationPanelViewModel;
    private IValidatable? GetPrescriptionValidator() => PrescriptionPanelViewModel as IValidatable;

    private async Task SaveDraftOnlyAsync()
    {
        // OpenSpec: refactor-medicalcase-aggregate-crud (Phase 4.1) - 使用聚合暂存
        try { SetIsBusy(true, "正在保存..."); await _coordinator.SaveDraftAsync(MedicalCaseId, GetConsultationProvider(), GetPrescriptionProvider(), Remark); }
        finally { SetIsBusy(false); }
    }

    private async Task CancelCaseOnlyAsync()
    {
        // OpenSpec: refactor-medicalcase-aggregate-crud (Phase 4.1) - 使用聚合取消
        try { SetIsBusy(true, "正在处理..."); await _coordinator.CancelAsync(MedicalCaseId, GetConsultationProvider(), GetPrescriptionProvider(), Remark); }
        finally { SetIsBusy(false); }
    }

    private async void ExecuteSaveAndStay()
    {
        try
        {
            if (WorkspaceMode == WorkspaceMode.Management)
            {
                var auditReason = await CheckAndGetAuditReasonAsync();
                if (auditReason == null) return;
                if (!string.IsNullOrEmpty(auditReason)) EditReason = auditReason;
            }
            SetIsBusy(true, WorkspaceMode == WorkspaceMode.Management ? "正在保存..." : "正在暂存...");
            // OpenSpec: refactor-medicalcase-aggregate-crud (Phase 4.1) - 使用聚合暂存
            var result = await _coordinator.SaveDraftAsync(MedicalCaseId, GetConsultationProvider(), GetPrescriptionProvider(), Remark);
            if (result.IsSuccess) { _editModeStateMachine.EnterReadOnlyMode(); await ShowSuccessMessageAsync(WorkspaceMode == WorkspaceMode.Management ? "保存成功" : "医案已暂存，可随时点击'修改医案'继续编辑"); }
            else await ShowErrorMessageAsync(result.ErrorMessage ?? "保存失败");
        }
        catch (Exception ex) { Logger.LogError(ex, "保存医案失败"); await ShowErrorMessageAsync(ClientErrorMessageMapper.GetSafeOperationFailureMessage("保存", ex)); }
        finally { SetIsBusy(false); }
    }

    private async Task<string?> CheckAndGetAuditReasonAsync()
    {
        if (_auditRequirementChecker == null) return string.Empty;
        var medicalCase = _dataLoader.CachedMedicalCase;
        if (medicalCase == null) return string.Empty;
        var currentUserId = SessionManager?.CurrentUser?.Id ?? Guid.Empty;
        if (!_auditRequirementChecker.IsAuditRequired(medicalCase, currentUserId)) return string.Empty;
        return await ShowAuditReasonDialogAsync();
    }

    private Task<string?> ShowAuditReasonDialogAsync()
    {
        if (_dialogService == null) return Task.FromResult<string?>(string.Empty);
        var tcs = new TaskCompletionSource<string?>();
        _dialogService.ShowDialog(nameof(Dialogs.AuditReasonDialog), new DialogParameters(), r =>
        { tcs.SetResult(r.Result == ButtonResult.OK && r.Parameters.TryGetValue("Reason", out string? reason) ? reason : null); });
        return tcs.Task;
    }

    /// <summary>
    /// 执行打印处方笺
    /// OpenSpec: print-prescription-slip
    /// </summary>
    private async void ExecutePrintPrescription()
    {
        try
        {
            SetIsBusy(true, "正在准备预览...");
            Logger.LogInformation("预览处方笺，MedicalCaseId: {MedicalCaseId}", MedicalCaseId);

            if (_prescriptionPrintService == null)
            {
                await ShowErrorMessageAsync("打印服务未配置");
                return;
            }

            // 获取处方数据（从缓存或当前ViewModel构建）
            var prescription = BuildPrescriptionDetailDto();
            if (prescription == null)
            {
                await ShowErrorMessageAsync("没有可打印的处方数据");
                return;
            }

            // 获取患者和诊断信息
            var patient = CurrentPatient;
            var consultation = ConsultationPanelViewModel?.GetConsultationData();

            // 调用打印预览服务
            await _prescriptionPrintService.PreviewPrescriptionAsync(prescription, patient, consultation);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "打印处方笺失败");
            await ShowErrorMessageAsync(ClientErrorMessageMapper.GetSafeOperationFailureMessage("打印", ex));
        }
        finally
        {
            SetIsBusy(false);
        }
    }

    /// <summary>
    /// 构建处方DTO用于打印
    /// OpenSpec: print-prescription-slip
    /// </summary>
    private PrescriptionDetailDto? BuildPrescriptionDetailDto()
    {
        // 优先使用缓存的处方数据
        var cachedPrescription = _dataLoader.GetCachedPrescription();
        if (cachedPrescription != null)
        {
            return cachedPrescription;
        }

        // 如果没有缓存，从ViewModel构建
        var prescriptionData = PrescriptionPanelViewModel?.GetPrescriptionData();
        if (prescriptionData == null || !prescriptionData.NeedsPrescription || prescriptionData.Items == null || prescriptionData.Items.Count == 0)
        {
            return null;
        }

        // 转换药材项类型
        var items = prescriptionData.Items.Select(item => new PrescriptionItemDto
        {
            Id = item.Id ?? Guid.NewGuid(),
            HerbId = item.HerbId,
            HerbName = item.HerbName ?? string.Empty,
            Dosage = item.Dosage,
            Unit = item.Unit,
            UnitPrice = item.UnitPrice,
            DecocteMethod = item.DecocteMethod
        }).ToList();

        return new PrescriptionDetailDto
        {
            Id = prescriptionData.Id ?? Guid.NewGuid(),
            MedicalCaseId = MedicalCaseId,
            DosageCount = prescriptionData.DosageCount,
            Usage = prescriptionData.Usage,
            Advice = prescriptionData.Advice,
            ReferencedFormulas = prescriptionData.ReferencedFormulas,
            Remark = prescriptionData.Remark,
            Items = items
        };
    }

    private async void ExecuteCompleteConsultation()
    {
        try
        {
            SetIsBusy(true, "正在完成看诊...");
            // OpenSpec: refactor-medicalcase-aggregate-crud (Phase 4.1) - 使用聚合完成
            var result = await _coordinator.CompleteAsync(
                MedicalCaseId, GetConsultationProvider(), GetPrescriptionProvider(),
                GetConsultationValidator(), GetPrescriptionValidator(), Remark, IsPrescriptionEnabled);
            if (result.IsSuccess)
            {
                await ShowSuccessMessageAsync("看诊已完成");
                // OpenSpec: refactor-medicalcase-management - 使用新的Master-Detail视图
                _regionManager.RequestNavigate("ContentRegion", WorkspaceMode == WorkspaceMode.Management ? "MedicalCaseMasterDetailView" : "PatientSelectionView");
            }
            else await ShowErrorMessageAsync(result.ErrorMessage ?? "完成失败");
        }
        catch (Exception ex) { Logger.LogError(ex, "完成看诊失败"); await ShowErrorMessageAsync(ClientErrorMessageMapper.GetSafeOperationFailureMessage("完成看诊", ex)); }
        finally { SetIsBusy(false); }
    }

    #endregion

    #region INavigationAware

    public override async void OnNavigatedTo(NavigationContext navigationContext)
    {
        base.OnNavigatedTo(navigationContext);
        MedicalCaseId = navigationContext.Parameters.GetValue<Guid>("MedicalCaseId");
        CurrentPatient = navigationContext.Parameters.GetValue<PatientDetailDto>("CurrentPatient");
        WorkspaceMode = navigationContext.Parameters.GetValue<WorkspaceMode>(MedicalCaseNavigationParameters.WorkspaceModeKey);
        var initialEditState = navigationContext.Parameters.GetValue<EditState>(MedicalCaseNavigationParameters.InitialEditStateKey);
        var editMode = navigationContext.Parameters.GetValue<string>("EditMode");
        IsFromManagement = navigationContext.Parameters.GetValue<bool>("IsFromManagement") || WorkspaceMode == WorkspaceMode.Management;
        var isHistoricalEdit = editMode == "HistoricalEdit";

        await InitializePatientInfoAsync();
        await LoadMedicalCaseDataAsync();
        InitializeChildViewModels();
        await DetermineEditModeAsync(initialEditState, isHistoricalEdit);
    }

    private async Task DetermineEditModeAsync(EditState initialEditState = EditState.Editing, bool isHistoricalEdit = false)
    {
        var medicalCase = _dataLoader.CachedMedicalCase;
        if (medicalCase == null) { _editModeStateMachine.Initialize(WorkspaceMode, EditType.Create, canEdit: true, EditState.Editing); return; }
        var currentUserRole = SessionManager?.CurrentUser?.Role;
        var isAdmin = currentUserRole == Shared.Models.Enums.UserRole.Admin || currentUserRole == Shared.Models.Enums.UserRole.SuperAdmin;
        var currentUserId = SessionManager?.CurrentUser?.Id ?? Guid.Empty;
        var isOwner = medicalCase.UserId == currentUserId;
        var isCompleted = medicalCase.CaseStatus == Shared.Models.Enums.MedicalCaseStatus.Completed;
        var preferEditing = initialEditState == EditState.Editing || isHistoricalEdit;
        _editModeStateMachine.DetermineFromContext(WorkspaceMode, isCompleted, isOwner, isAdmin, preferEditing);
        if (isHistoricalEdit) _editModeStateMachine.EditType = EditType.EditCompleted;
        await Task.CompletedTask;
    }

    private async Task InitializePatientInfoAsync()
    {
        if (CurrentPatient == null) return;
        var (patientName, patientInfo) = _dataLoader.FormatPatientInfo(CurrentPatient);
        PatientName = patientName; PatientInfo = patientInfo;
        if (MedicalCaseId == Guid.Empty)
        {
            try
            {
                SetIsBusy(true, "正在创建医案...");
                var result = await _lifecycleHandler.CreateMedicalCaseAsync(CurrentPatient.Id);
                if (!result.success) { await ShowErrorMessageAsync("创建医案失败，请重试"); return; }
                MedicalCaseId = result.medicalCaseId;
            }
            catch (Exception ex) { Logger.LogError(ex, "创建医案失败"); await ShowErrorMessageAsync(ClientErrorMessageMapper.GetSafeOperationFailureMessage("创建医案", ex)); }
            finally { SetIsBusy(false); }
        }
    }

    private async Task LoadMedicalCaseDataAsync()
    {
        if (MedicalCaseId == Guid.Empty) return;
        try
        {
            SetIsBusy(true, "正在加载医案数据...");
            var result = await _dataLoader.LoadMedicalCaseDetailsAsync(MedicalCaseId);
            if (!result.success) return;
            var hasConsultation = result.detail?.Consultation != null;
            var hasPrescription = result.detail?.Prescription != null;
            UpdateConsultationStatus(hasConsultation);
            UpdatePrescriptionStatus(hasPrescription);
            if (hasConsultation) IsPrescriptionEnabled = true;
            Remark = result.detail?.Remark ?? string.Empty;
        }
        catch (Exception ex) { Logger.LogError(ex, "加载医案数据失败"); await ShowErrorMessageAsync(ClientErrorMessageMapper.GetSafeOperationFailureMessage("加载医案数据", ex)); }
        finally { SetIsBusy(false); }
    }

    private void InitializeChildViewModels()
    {
        ConsultationPanelViewModel = _injectedConsultationPanelViewModel;
        PrescriptionPanelViewModel = _injectedPrescriptionPanelViewModel;
        ConsultationPanelViewModel?.Initialize(MedicalCaseId, _dataLoader.CachedConsultation);
        _ = PrescriptionPanelViewModel?.InitializeAsync(MedicalCaseId, CurrentPatient?.Id ?? Guid.Empty, CurrentPatient?.Name ?? string.Empty, _dataLoader.CachedPrescription);
        _activeConsultationService.Register(MedicalCaseId, _navigationHandler.HandleLeaveRequestAsync);

        // 订阅子ViewModel属性变更以实时更新CanComplete
        if (ConsultationPanelViewModel != null)
            ConsultationPanelViewModel.PropertyChanged += OnChildViewModelPropertyChanged;
        if (PrescriptionPanelViewModel != null)
            PrescriptionPanelViewModel.PropertyChanged += OnChildViewModelPropertyChanged;

        // OpenSpec: print-prescription-slip - 如果已有处方数据，启用打印按钮
        var cachedPrescription = _dataLoader.CachedPrescription;
        if (cachedPrescription != null && cachedPrescription.Items != null && cachedPrescription.Items.Count > 0)
        {
            CanPrintPrescription = true;
        }

        // 初始计算CanComplete状态
        UpdateCanComplete();
    }

    public override bool IsNavigationTarget(NavigationContext navigationContext) => false;

    public override void OnNavigatedFrom(NavigationContext navigationContext)
    {
        _activeConsultationService.Unregister();
        base.OnNavigatedFrom(navigationContext);
    }

    #endregion

    #region 事件处理

    private void OnEditStateChanged(object? sender, EditStateChangedEventArgs e)
    {
        RaisePropertyChanged(nameof(IsEditing)); RaisePropertyChanged(nameof(IsReadOnly));
        RaisePropertyChanged(nameof(ShowEditButton)); RaisePropertyChanged(nameof(ShowEditButtonTopRight));
        RaisePropertyChanged(nameof(ShowSaveButton)); RaisePropertyChanged(nameof(ShowDraftButton));
        RaisePropertyChanged(nameof(ShowCompleteButton)); RaisePropertyChanged(nameof(HeaderTitle));
        RaisePropertyChanged(nameof(BackButtonText));
        SaveCommand?.RaiseCanExecuteChanged(); EnterEditModeCommand?.RaiseCanExecuteChanged();
    }

    private void OnConsultationCompleted(ConsultationCompletedPayload payload)
    {
        UpdateConsultationStatus(true);
        IsPrescriptionEnabled = payload.NeedsPrescription;
        if (payload.NeedsPrescription) UpdatePrescriptionStatus(false, "待开方");
        else UpdatePrescriptionStatus(false, "无需开方");
        // CanComplete由UpdateCanComplete()实时计算，IsPrescriptionEnabled变更时自动触发
    }

    private void OnPrescriptionCompleted(PrescriptionCompletedPayload payload)
    {
        UpdatePrescriptionStatus(true);
        CanPrintPrescription = true;
        UpdateCanComplete();
    }

    private void OnPrescriptionSaved(PrescriptionSavedPayload payload)
    {
        if (payload.MedicalCaseId == MedicalCaseId) HasUnsavedPrescriptionChanges = false;
    }

    private async void OnLifecycleActionCompleted(object? sender, LifecycleActionCompletedEventArgs e)
    {
        if (!e.Success) await ShowErrorMessageAsync(e.ErrorMessage ?? "操作失败");
    }

    private async void OnDataLoaded(object? sender, DataLoadedEventArgs e)
    {
        if (!e.Success) await ShowErrorMessageAsync(e.ErrorMessage ?? "数据加载失败");
    }

    #endregion

    #region 状态更新

    /// <summary>
    /// 更新完成看诊按钮可用性
    /// 简化逻辑：诊断必填有值 && (不需要开方 || 药材数量>0)
    /// </summary>
    private void UpdateCanComplete()
    {
        var consultationValid = ConsultationPanelViewModel?.Validate() ?? false;
        var prescriptionSatisfied = !IsPrescriptionEnabled || (PrescriptionPanelViewModel?.ItemCount > 0);
        CanComplete = consultationValid && prescriptionSatisfied;
    }

    /// <summary>
    /// 子ViewModel属性变更处理 - 用于实时更新CanComplete
    /// </summary>
    private void OnChildViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        // 监听影响CanComplete的属性变化
        // OpenSpec: refactor-diagnosis-fields - 移除ChiefComplaint，仅监听TCMDiagnosis
        if (e.PropertyName is "TCMDiagnosis" or "ItemCount")
        {
            UpdateCanComplete();
        }
    }

    private void UpdateConsultationStatus(bool isCompleted)
    {
        ConsultationStatusText = isCompleted ? "已完成" : "未完成";
        ConsultationStatusColor = new SolidColorBrush(isCompleted ? Color.FromRgb(76, 175, 80) : Color.FromRgb(255, 152, 0));
    }

    private void UpdatePrescriptionStatus(bool isCompleted, string? customText = null)
    {
        ShowPrescriptionStatus = true;
        var color = isCompleted ? Color.FromRgb(76, 175, 80) : Color.FromRgb(158, 158, 158);
        PrescriptionStatusText = isCompleted ? "已完成" : (customText ?? "待开方");
        PrescriptionStatusBackground = new SolidColorBrush(color);
        PrescriptionStatusSummary = isCompleted ? "已开方" : (customText ?? "待开方");
        PrescriptionStatusSummaryColor = new SolidColorBrush(color);
    }

    #endregion

    #region IDisposable

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _activeConsultationService.Unregister();
            _lifecycleHandler.ActionCompleted -= OnLifecycleActionCompleted;
            _dataLoader.DataLoaded -= OnDataLoaded;
            _editModeStateMachine.EditStateChanged -= OnEditStateChanged;
            EventAggregator.GetEvent<ConsultationCompletedEvent>().Unsubscribe(OnConsultationCompleted);
            EventAggregator.GetEvent<PrescriptionCompletedEvent>().Unsubscribe(OnPrescriptionCompleted);
            EventAggregator.GetEvent<PrescriptionSavedEvent>().Unsubscribe(OnPrescriptionSaved);
        }
        base.Dispose(disposing);
    }

    #endregion
}
