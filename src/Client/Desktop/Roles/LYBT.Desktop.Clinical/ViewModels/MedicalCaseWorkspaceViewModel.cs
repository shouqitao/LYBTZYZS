using System.Collections.ObjectModel;
using LYBT.Desktop.CardReader.Integration;
using LYBT.Desktop.CardReader.Services;
using LYBT.Desktop.Clinical.ViewModels.Workspace;
using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Infrastructure.Constants;
using LYBT.Desktop.Infrastructure.Events;
using LYBT.Desktop.Infrastructure.Extensions;
using LYBT.Desktop.MedicalCase.Interfaces;
using LYBT.Desktop.MedicalCase.Models;
using LYBT.Desktop.MedicalCase.ViewModels.Components;
using LYBT.Desktop.MedicalCase.ViewModels.Workspace;
using LYBT.Desktop.Models.ViewModels.Base;
using LYBT.Shared.ExceptionHandling.Mappers;
using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Contracts.Patients;
using Microsoft.Extensions.Logging;
using Prism.Commands;
using Prism.Events;
using Prism.Regions;
using Prism.Services.Dialogs;

namespace LYBT.Desktop.Clinical.ViewModels;

/// <summary>
/// 医案工作台 - Composite ViewModel thin shell.
/// Delegates to child VMs: ConsultationEditor, PrescriptionEditor, Commands, PendingQueue, CardReader.
/// Implements IMedicalCaseWorkspaceContext (state reading) and IWorkspaceHost (child-to-parent operations).
/// </summary>
public class MedicalCaseWorkspaceViewModel : NavigableViewModelBase,
    IMedicalCaseWorkspaceContext, IWorkspaceHost
{
    #region Fields

    private readonly IMedicalCaseService _medicalCaseService;
    private readonly INavigationCoordinator _navigationCoordinator;
    private readonly IActiveConsultationService _activeConsultationService;
    private readonly IDialogService? _dialogService;

    /// <summary>US-MC-011: Edit mode FSM (lifecycle tied to parent VM, not DI).</summary>
    private readonly IEditModeStateMachine _editStateMachine;

    #endregion

    #region Child VMs

    public ConsultationEditorViewModel ConsultationEditor { get; }
    public PrescriptionEditorViewModel PrescriptionEditor { get; }
    public MedicalCaseCommandsViewModel Commands { get; }
    public PendingQueueViewModel PendingQueue { get; }
    public CardReaderViewModel CardReader { get; }

    #endregion

    #region IMedicalCaseWorkspaceContext

    private WorkspaceState _state = new();
    public WorkspaceState State
    {
        get => _state;
        private set
        {
            if (SetProperty(ref _state, value))
            {
                OnPropertyChanged(nameof(State));
                Commands?.RefreshCanExecute();
                SaveChangesCommand?.RaiseCanExecuteChanged();
            }
        }
    }

    private Guid _medicalCaseId = Guid.Empty;
    public Guid MedicalCaseId { get => _medicalCaseId; set => SetProperty(ref _medicalCaseId, value); }

    private PatientDetailDto? _currentPatient;
    public PatientDetailDto? CurrentPatient
    {
        get => _currentPatient;
        set
        {
            if (SetProperty(ref _currentPatient, value))
            {
                OnPropertyChanged(nameof(CurrentPatientGenderDisplay));
                OnPropertyChanged(nameof(RegistrationTime));
                OnPropertyChanged(nameof(CurrentPatientDisplayModel));
                OnPropertyChanged(nameof(PatientName));
                OnPropertyChanged(nameof(PatientInfo));
            }
        }
    }

    ISessionManager? IMedicalCaseWorkspaceContext.SessionManager => SessionManager;

    #endregion

    #region IWorkspaceHost (explicit)

    void IWorkspaceHost.SetBusy(bool isBusy, string? message) => SetBusy(isBusy, message);
    Task IWorkspaceHost.ShowErrorAsync(string message) => ShowErrorMessageAsync(message);
    Task IWorkspaceHost.ShowSuccessAsync(string message) => ShowSuccessMessageAsync(message);
    Task<bool> IWorkspaceHost.ShowConfirmAsync(string message, string title) => ShowConfirmMessageAsync(message, title);
    ICommonDialogService? IWorkspaceHost.CommonDialogService => CommonDialogService;
    void IWorkspaceHost.NotifyStateChanged() => UpdateState();

    #endregion

    #region Patient Display

    public string PatientName => CurrentPatient?.Name ?? string.Empty;

    public string PatientInfo
    {
        get
        {
            if (CurrentPatient == null) return string.Empty;
            return $"{PatientName} ({CurrentPatientGenderDisplay}, {CurrentPatient.Age ?? 0}岁)";
        }
    }

    public string CurrentPatientGenderDisplay => CurrentPatient?.Gender switch
    {
        Shared.Models.Enums.Gender.Male => "男",
        Shared.Models.Enums.Gender.Female => "女",
        _ => "未知"
    };

    public DateTime? RegistrationTime => CurrentPatient?.CreatedAt;

    public Infrastructure.Controls.PatientDisplayModel? CurrentPatientDisplayModel =>
        CurrentPatient == null ? null : new Infrastructure.Controls.PatientDisplayModel
        {
            Name = CurrentPatient.Name ?? string.Empty,
            Gender = CurrentPatientGenderDisplay,
            Age = CurrentPatient.Age,
            PhoneNumber = CurrentPatient.PhoneNumber,
            VisitCount = CurrentPatient.VisitCount,
            RegistrationTime = RegistrationTime
        };

    #endregion

    #region Editable Properties (flat for TwoWay binding)

    private string _remark = string.Empty;
    public string Remark
    {
        get => _remark;
        set
        {
            if (SetProperty(ref _remark, value) && _medicalCaseService.CachedMedicalCase != null)
                _medicalCaseService.CachedMedicalCase.Remark = value;
        }
    }

    private string _editReason = string.Empty;
    public string EditReason { get => _editReason; set => SetProperty(ref _editReason, value); }

    private bool _isPrescriptionEnabled;
    public bool IsPrescriptionEnabled
    {
        get => _isPrescriptionEnabled;
        set
        {
            if (SetProperty(ref _isPrescriptionEnabled, value))
            {
                PrescriptionEditor.Prescription.ValidationEnabled = value;
                UpdateState();
            }
        }
    }

    private bool _needsPrescription = true;
    public bool NeedsPrescription
    {
        get => _needsPrescription;
        set
        {
            if (SetProperty(ref _needsPrescription, value))
                OnPropertyChanged(nameof(NoPrescription));
        }
    }

    public bool NoPrescription => !NeedsPrescription;

    private ObservableCollection<HerbListDto> _allHerbs = new();
    public ObservableCollection<HerbListDto> AllHerbs
    {
        get => _allHerbs;
        private set => SetProperty(ref _allHerbs, value);
    }

    #endregion

    #region Commands

    public DelegateCommand BackCommand { get; }
    public DelegateCommand BackToPatientSelectionCommand => BackCommand;
    public DelegateCommand ViewPatientHistoryCommand { get; }
    /// <summary>
    /// Management模式: 审计 + 保存 + 进入只读 (parent-level concern, not in child Commands VM)
    /// </summary>
    public DelegateCommand SaveChangesCommand { get; }

    #endregion

    #region Constructor

    public MedicalCaseWorkspaceViewModel(
        IViewModelServices services,
        IMedicalCaseService medicalCaseService,
        INavigationCoordinator navigationCoordinator,
        IActiveConsultationService activeConsultationService,
        IPendingQueueManager pendingQueueManager,
        PrescriptionPrintHandler printHandler,
        ICardReaderService cardReaderService,
        IPatientCardReaderIntegration patientCardReaderIntegration,
        IDialogService? dialogService = null)
        : base(services)
    {
        _medicalCaseService = medicalCaseService ?? throw new ArgumentNullException(nameof(medicalCaseService));
        _navigationCoordinator = navigationCoordinator ?? throw new ArgumentNullException(nameof(navigationCoordinator));
        _activeConsultationService = activeConsultationService ?? throw new ArgumentNullException(nameof(activeConsultationService));
        _dialogService = dialogService;

        // US-MC-011: Create edit mode FSM (lifecycle tied to this VM)
        _editStateMachine = new EditModeStateMachine(services.LoggerFactory.CreateLogger<EditModeStateMachine>());
        _editStateMachine.StateChanged += OnEditStateChanged;

        // Create child VMs (not container-resolved; coupled to parent lifecycle)
        ConsultationEditor = new ConsultationEditorViewModel(this, this, services.LoggerFactory);
        PrescriptionEditor = new PrescriptionEditorViewModel(this, this, services.LoggerFactory);
        Commands = new MedicalCaseCommandsViewModel(this, this, services.LoggerFactory, medicalCaseService, printHandler, dialogService);
        PendingQueue = new PendingQueueViewModel(this, this, services.LoggerFactory, medicalCaseService, pendingQueueManager, navigationCoordinator);
        CardReader = new CardReaderViewModel(cardReaderService, patientCardReaderIntegration, medicalCaseService, navigationCoordinator, this, this, services.LoggerFactory);

        // Wire data providers for Commands
        Commands.GetConsultationData = () => ConsultationEditor.GetConsultationData();
        Commands.GetPrescriptionData = () => PrescriptionEditor.GetPrescriptionData();
        Commands.GetConsultationValidator = () => ConsultationEditor.Consultation;
        Commands.GetPrescriptionValidator = () => PrescriptionEditor.Prescription;
        Commands.GetPrescriptionProvider = () => PrescriptionEditor.Prescription;
        Commands.GetConsultationItem = () => ConsultationEditor.Consultation;
        Commands.GetPrescriptionItem = () => PrescriptionEditor.Prescription;
        Commands.GetAllHerbs = () => AllHerbs;
        Commands.GetRemark = () => Remark;
        Commands.GetEditReason = () => EditReason;
        Commands.GetIsPrescriptionEnabled = () => IsPrescriptionEnabled;

        // Wire PendingQueue suspend delegate
        PendingQueue.SuspendCurrentCase = SuspendOnlyAsync;

        // Parent-level commands
        BackCommand = new DelegateCommand(async () => await ExecuteBackAsync());
        ViewPatientHistoryCommand = new DelegateCommand(ExecuteViewPatientHistory, () => CurrentPatient != null)
            .ObservesProperty(() => CurrentPatient);
        SaveChangesCommand = new DelegateCommand(ExecuteSaveChanges, () => State.ShowSaveButton);

        // Event subscriptions
        EventAggregator.GetEvent<CaseEvents.ConsultationCompletedEvent>().Subscribe(OnConsultationCompleted, ThreadOption.UIThread);
        EventAggregator.GetEvent<CaseEvents.PrescriptionCompletedEvent>().Subscribe(OnPrescriptionCompleted, ThreadOption.UIThread);
    }

    #endregion

    #region Navigation Lifecycle

    public override void OnNavigatedTo(NavigationContext navigationContext)
    {
        base.OnNavigatedTo(navigationContext);
        OnNavigatedToAsync(navigationContext).SafeFireAndForget(ex => Logger.LogError(ex, "医案工作区导航初始化失败"));
    }

    private async Task OnNavigatedToAsync(NavigationContext navigationContext)
    {
        MedicalCaseId = navigationContext.Parameters.GetValue<Guid>("MedicalCaseId");
        CurrentPatient = navigationContext.Parameters.GetValue<PatientDetailDto>("CurrentPatient");
        var workspaceMode = navigationContext.Parameters.GetValue<WorkspaceMode>(MedicalCaseNavigationParameters.WorkspaceModeKey);
        var initialEditState = navigationContext.Parameters.GetValue<EditState>(MedicalCaseNavigationParameters.InitialEditStateKey);
        var editMode = navigationContext.Parameters.GetValue<string>("EditMode");
        var isHistoricalEdit = editMode == "HistoricalEdit";

        // Set mode early so ResumeSuspended can check it
        State = new WorkspaceState(Mode: workspaceMode);

        await InitializePatientInfoAsync();
        await LoadMedicalCaseDataAsync();
        await ResumeSuspendedIfNeededAsync();

        InitializeChildViewModels();
        DetermineEditMode(workspaceMode, initialEditState, isHistoricalEdit);

        _activeConsultationService.Register(MedicalCaseId, HandleLeaveRequestAsync);
        _ = PendingQueue.RefreshQueueAsync();
        _ = CardReader.InitializeAsync();
    }

    public override bool IsNavigationTarget(NavigationContext navigationContext) => false;

    public override void OnNavigatedFrom(NavigationContext navigationContext)
    {
        _activeConsultationService.Unregister();
        base.OnNavigatedFrom(navigationContext);
    }

    #endregion

    #region State Management

    private void UpdateState()
    {
        State = State with
        {
            CanComplete = CalculateCanComplete(),
            CanPrint = PrescriptionEditor.HasItems
        };
    }

    private bool CalculateCanComplete()
    {
        if (!ConsultationEditor.Consultation.IsDiagnosisComplete) return false;
        if (!IsPrescriptionEnabled) return true;
        return PrescriptionEditor.Prescription.ItemCount > 0;
    }

    private void DetermineEditMode(WorkspaceMode workspaceMode, EditState initialEditState, bool isHistoricalEdit)
    {
        var medicalCase = _medicalCaseService.CachedMedicalCase;
        if (medicalCase == null)
        {
            State = new WorkspaceState(Mode: workspaceMode, EditState: EditState.Editing, EditType: EditType.Create, CanEdit: true);
            InitializeEditStateMachine(canEdit: true, startEditing: true);
            return;
        }

        var currentUserRole = SessionManager?.CurrentUser?.Role;
        var isAdmin = currentUserRole == Shared.Models.Enums.UserRole.Admin
                   || currentUserRole == Shared.Models.Enums.UserRole.SuperAdmin;
        var currentUserId = SessionManager?.CurrentUser?.Id ?? Guid.Empty;
        var isOwner = medicalCase.UserId == currentUserId;
        var isCompleted = medicalCase.CaseStatus == Shared.Models.Enums.MedicalCaseStatus.Completed;
        var preferEditing = initialEditState == EditState.Editing || isHistoricalEdit;

        State = State.DetermineFromContext(workspaceMode, isCompleted, isOwner, isAdmin, preferEditing);
        if (isHistoricalEdit) State = State with { EditType = EditType.EditCompleted };

        var canEdit = isAdmin || (isOwner && !isCompleted);
        InitializeEditStateMachine(canEdit, preferEditing && canEdit);
    }

    /// <summary>
    /// US-MC-011: Initialize the FSM from computed context.
    /// Maps EditState -> WorkspaceEditState for the state machine initial state.
    /// </summary>
    private void InitializeEditStateMachine(bool canEdit, bool startEditing)
    {
        var initialFsmState = startEditing
            ? WorkspaceEditState.Editing
            : WorkspaceEditState.ReadOnly;

        _editStateMachine.Initialize(initialFsmState, guardPredicate: evt =>
        {
            // EnterEdit guard: only allowed when CanEdit
            if (evt == WorkspaceEditEvent.EnterEdit && !canEdit)
                return false;
            return true;
        });
    }

    /// <summary>
    /// US-MC-011: Handle FSM state changes -- update WorkspaceState.EditState to match.
    /// </summary>
    private void OnEditStateChanged(object? sender, EditStateChangedEventArgs e)
    {
        var editState = e.NewState is WorkspaceEditState.Editing or WorkspaceEditState.DirtyEditing
            ? EditState.Editing
            : EditState.ReadOnly;

        State = State with { EditState = editState };

        Logger.LogDebug("WorkspaceState.EditState <- {NewEditState} (FSM: {FsmState})", editState, e.NewState);
    }

    #endregion

    #region Data Loading

    private async Task InitializePatientInfoAsync()
    {
        if (CurrentPatient == null) return;
        if (MedicalCaseId == Guid.Empty)
        {
            try
            {
                SetBusy(true, "正在创建医案...");
                var result = await _medicalCaseService.CreateMedicalCaseAsync(CurrentPatient.Id);
                if (!result.success) { await ShowErrorMessageAsync("创建医案失败，请重试"); return; }
                MedicalCaseId = result.medicalCaseId;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "创建医案失败");
                await ShowErrorMessageAsync(ClientErrorMessageMapper.GetSafeOperationFailureMessage("创建医案", ex));
            }
            finally { SetBusy(false); }
        }
    }

    private async Task LoadMedicalCaseDataAsync()
    {
        if (MedicalCaseId == Guid.Empty) return;
        try
        {
            SetBusy(true, "正在加载医案数据...");
            var result = await _medicalCaseService.LoadDetailsAsync(MedicalCaseId);
            if (!result.success) return;
            if (result.detail?.Prescription != null) IsPrescriptionEnabled = true;
            Remark = result.detail?.Remark ?? string.Empty;
            NeedsPrescription = true;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "加载医案数据失败");
            await ShowErrorMessageAsync(ClientErrorMessageMapper.GetSafeOperationFailureMessage("加载医案数据", ex));
        }
        finally { SetBusy(false); }
    }

    private async Task ResumeSuspendedIfNeededAsync()
    {
        var medicalCase = _medicalCaseService.CachedMedicalCase;
        if (medicalCase == null) return;

        if (State.Mode == WorkspaceMode.Clinical
            && medicalCase.CaseStatus == Shared.Models.Enums.MedicalCaseStatus.Suspended)
        {
            Logger.LogInformation("[CMD] ResumeSuspended -> MedicalCaseId={MedicalCaseId}", MedicalCaseId);
            var result = await _medicalCaseService.ResumeSuspendedAsync(MedicalCaseId);
            if (result.success) medicalCase.CaseStatus = Shared.Models.Enums.MedicalCaseStatus.Active;
        }
    }

    private void InitializeChildViewModels()
    {
        // Consultation
        if (_medicalCaseService.CachedConsultation != null)
            ConsultationEditor.InitializeFromDto(_medicalCaseService.CachedConsultation);
        else
            ConsultationEditor.InitializeForNewCase(
                CurrentPatient?.Name ?? string.Empty,
                CurrentPatient?.Id ?? Guid.Empty,
                SessionManager?.CurrentUser?.Id ?? Guid.Empty);

        // Prescription
        if (_medicalCaseService.CachedPrescription != null)
            PrescriptionEditor.InitializeFromDto(_medicalCaseService.CachedPrescription);
        else
            PrescriptionEditor.InitializeForNewCase();

        // Subscribe for state updates
        ConsultationEditor.Consultation.PropertyChanged += OnChildPropertyChanged;
        PrescriptionEditor.Prescription.PropertyChanged += OnChildPropertyChanged;

        // Initial print state
        if (_medicalCaseService.CachedPrescription?.Items is { Count: > 0 })
            State = State with { CanPrint = true };

        UpdateState();
    }

    #endregion

    #region Back Navigation & Leave Handling

    private async Task ExecuteBackAsync()
    {
        try
        {
            if (State.Mode == WorkspaceMode.Management)
            {
                if (State.IsReadOnly)
                {
                    _navigationCoordinator.NavigateTo(ViewNames.MedicalCaseMasterDetail);
                    return;
                }
                var shouldNavigate = await HandleManagementLeaveRequestAsync();
                if (shouldNavigate) _navigationCoordinator.NavigateTo(ViewNames.MedicalCaseMasterDetail);
                return;
            }

            var result = await HandleLeaveRequestAsync();
            if (result.CanLeave) _navigationCoordinator.NavigateTo(ViewNames.PatientSelection);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Navigation.ExecuteBack failed - Mode={Mode} IsReadOnly={IsReadOnly}", State.Mode, State.IsReadOnly);
        }
    }

    /// <summary>
    /// Clinical模式三选项离开确认 (供IActiveConsultationService调用)
    /// </summary>
    public async Task<LeaveConsultationResult> HandleLeaveRequestAsync()
    {
        var message = "您将离开看诊界面，是否暂存当前医案？\n\n" +
            "【是】暂存医案 - 保存当前进度，下次可继续\n" +
            "【否】取消医案 - 作废本次就诊\n" +
            "【取消】继续看诊 - 返回当前界面";

        LeaveConsultationChoice choice;
        if (CommonDialogService != null)
        {
            var dialogResult = await CommonDialogService.ShowTripleChoiceAsync(message, "离开确认");
            choice = dialogResult switch
            {
                TripleChoiceResult.Yes => LeaveConsultationChoice.Suspend,
                TripleChoiceResult.No => LeaveConsultationChoice.CancelCase,
                _ => LeaveConsultationChoice.Stay
            };
        }
        else
        {
            Logger.LogWarning("Navigation.HandleLeaveRequest -> CommonDialogServiceUnavailable");
            choice = LeaveConsultationChoice.Stay;
        }

        switch (choice)
        {
            case LeaveConsultationChoice.Suspend:
                await SuspendOnlyAsync();
                return LeaveConsultationResult.AllowLeave(choice);
            case LeaveConsultationChoice.CancelCase:
                await CancelCaseOnlyAsync();
                return LeaveConsultationResult.AllowLeave(choice);
            default:
                return LeaveConsultationResult.CancelLeave();
        }
    }

    private async Task<bool> HandleManagementLeaveRequestAsync()
    {
        if (_dialogService == null) { Logger.LogWarning("Navigation.HandleManagementLeave -> DialogServiceUnavailable"); return false; }

        var tcs = new TaskCompletionSource<bool>();
        _dialogService.ShowDialog("UnsavedChangesDialog", new DialogParameters(), async dialogResult =>
        {
            try
            {
                switch (dialogResult.Result)
                {
                    case ButtonResult.Yes:
                        var auditReason = await CheckAndGetAuditReasonAsync();
                        if (auditReason == null) { tcs.SetResult(false); return; }
                        if (!string.IsNullOrEmpty(auditReason)) EditReason = auditReason;
                        _editStateMachine.Fire(WorkspaceEditEvent.Save, "management-leave-save");
                        await SuspendOnlyAsync();
                        _editStateMachine.Fire(WorkspaceEditEvent.SaveCompleted, "management-leave-save-completed");
                        tcs.SetResult(true);
                        break;
                    case ButtonResult.No:
                        tcs.SetResult(true);
                        break;
                    default:
                        tcs.SetResult(false);
                        break;
                }
            }
            catch (Exception ex) { Logger.LogError(ex, "Navigation.HandleManagementLeave failed"); tcs.SetResult(false); }
        });

        return await tcs.Task;
    }

    private async Task SuspendOnlyAsync()
    {
        try
        {
            SetBusy(true, "正在保存...");
            await _medicalCaseService.SaveAndSuspendAsync(
                MedicalCaseId, ConsultationEditor.GetConsultationData(),
                PrescriptionEditor.GetPrescriptionData(), Remark);
        }
        finally { SetBusy(false); }
    }

    private async Task CancelCaseOnlyAsync()
    {
        try
        {
            SetBusy(true, "正在处理...");
            await _medicalCaseService.SaveAndCancelAsync(
                MedicalCaseId, ConsultationEditor.GetConsultationData(),
                PrescriptionEditor.GetPrescriptionData(), Remark);
        }
        finally { SetBusy(false); }
    }

    /// <summary>Management模式: 审计确认 + 保存 + 进入只读</summary>
    private void ExecuteSaveChanges()
        => ExecuteSaveChangesAsync().SafeFireAndForget(ex => Logger.LogError(ex, "保存医案失败"));

    private async Task ExecuteSaveChangesAsync()
    {
        try
        {
            var auditReason = await CheckAndGetAuditReasonAsync();
            if (auditReason == null) return;
            if (!string.IsNullOrEmpty(auditReason)) EditReason = auditReason;

            SetBusy(true, "正在保存...");
            _editStateMachine.Fire(WorkspaceEditEvent.Save, "save-changes");
            var result = await _medicalCaseService.SaveAndSuspendAsync(
                MedicalCaseId, ConsultationEditor.GetConsultationData(),
                PrescriptionEditor.GetPrescriptionData(), Remark);

            if (result.Success)
            {
                _editStateMachine.Fire(WorkspaceEditEvent.SaveCompleted, "save-changes-completed");
                await ShowSuccessMessageAsync("保存成功");
            }
            else
            {
                _editStateMachine.Fire(WorkspaceEditEvent.SaveFailed, "save-changes-failed");
                await ShowErrorMessageAsync(result.Error ?? "保存失败");
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "保存医案失败");
            await ShowErrorMessageAsync(ClientErrorMessageMapper.GetSafeOperationFailureMessage("保存", ex));
        }
        finally { SetBusy(false); }
    }

    private Task<string?> CheckAndGetAuditReasonAsync()
        => Task.FromResult<string?>(string.Empty); // FUTURE: 医案审计日志查看功能 (US-MC-012)

    #endregion

    #region Event Handlers

    private void OnConsultationCompleted(CaseConsultationCompletedPayload payload)
        => IsPrescriptionEnabled = NeedsPrescription;

    private void OnPrescriptionCompleted(CasePrescriptionCompletedPayload payload)
        => UpdateState();

    private void OnChildPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case "TcmDiagnosis":
            case "ItemCount":
                UpdateState();
                // US-MC-011: data modification -> FSM MakeChange (Editing -> DirtyEditing)
                _editStateMachine.Fire(WorkspaceEditEvent.MakeChange);
                break;
        }
    }

    private void ExecuteViewPatientHistory()
    {
        if (CurrentPatient == null) return;
        Logger.LogInformation("查看患者历史, PatientId: {PatientId}", CurrentPatient.Id);
        _navigationCoordinator.NavigateTo(ViewNames.PatientManagement);
    }

    #endregion

    #region IDisposable

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _editStateMachine.StateChanged -= OnEditStateChanged;
            _activeConsultationService.Unregister();
            EventAggregator.GetEvent<CaseEvents.ConsultationCompletedEvent>().Unsubscribe(OnConsultationCompleted);
            EventAggregator.GetEvent<CaseEvents.PrescriptionCompletedEvent>().Unsubscribe(OnPrescriptionCompleted);
            ConsultationEditor.Consultation.PropertyChanged -= OnChildPropertyChanged;
            PrescriptionEditor.Prescription.PropertyChanged -= OnChildPropertyChanged;
            ConsultationEditor.Dispose();
            PrescriptionEditor.Dispose();
            CardReader.Dispose();
        }
        base.Dispose(disposing);
    }

    #endregion
}
