using LYBT.Shared.Models.Enums;
using System.Collections.ObjectModel;
using LYBT.Desktop.Clinical.ViewModels.Workspace;
using LYBT.Desktop.Contracts.Enums;
using LYBT.Desktop.Contracts.Models;
using LYBT.Desktop.Contracts.Models.Navigation;
using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Controls.Models;
using LYBT.Desktop.Infrastructure.Constants;
using LYBT.Desktop.Infrastructure.Extensions;
using LYBT.Desktop.Infrastructure.Services.FeatureToggle;
using LYBT.Desktop.MedicalCase.Interfaces;
using LYBT.Desktop.MedicalCase.Models;
using LYBT.Desktop.MedicalCase.Models.Items;
using LYBT.Desktop.MedicalCase.ViewModels.Items;
using LYBT.Desktop.MedicalCase.ViewModels.Components;
using LYBT.Desktop.MedicalCase.ViewModels.Workspace;
using LYBT.Desktop.Infrastructure.ViewModels.Base;
using LYBT.Desktop.Foundation.ExceptionHandling;
using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Shared.Models.Contracts.Prescriptions;
using Microsoft.Extensions.Logging;
using CommunityToolkit.Mvvm.Input;
using Prism.Events;
using Prism.Regions;
using Prism.Services.Dialogs;

namespace LYBT.Desktop.Clinical.ViewModels;

/// <summary>
/// 医案工作台 - Composite ViewModel 薄壳。
/// 委托给子级 ViewModel：ConsultationEditor、PrescriptionEditor、Commands、PendingQueue、CardReader。
/// 实现 IMedicalCaseWorkspaceContext（状态读取）和 IWorkspaceHost（子级到父级操作）。
/// </summary>
public class MedicalCaseWorkspaceViewModel : NavigableViewModelBase,
    IMedicalCaseWorkspaceContext, IWorkspaceHost, IMedicalCaseDataProvider
{
    #region Fields

    private readonly IMedicalCaseService _medicalCaseService;
    private readonly INavigationCoordinator _navigationCoordinator;
    private readonly IActiveConsultationService _activeConsultationService;
    private readonly IPatientService _patientService;
    private readonly IDialogService? _dialogService;
    private readonly IToastService _toastService;

    /// <summary>US-MC-011：编辑模式有限状态机（生命周期绑定父级 ViewModel，而非 DI）。</summary>
    private readonly IEditModeStateMachine _editStateMachine;
    private readonly WorkspaceStateManager _stateManager;
    private readonly WorkspaceNavigationHandler _navHandler;

    /// <summary>返回目标视图（生产方经 ReturnView 参数写入；Clinical 模式 Back 消费）</summary>
    private string? _returnView;

    #endregion

    #region Child VMs

    public ConsultationEditorViewModel ConsultationEditor { get; }
    public PrescriptionEditorViewModel PrescriptionEditor { get; }
    public MedicalCaseCommandsViewModel Commands { get; }

    #endregion

    #region 导航行为

    /// <summary>
    /// 医案工作区按医案整体重建，实例不可复用（见 <see cref="IsNavigationTarget"/>）。
    /// 与之保持一致的 KeepAlive=false：不可复用的视图不应被区域保留，否则每次导航新建实例、
    /// 旧实例滞留区域（区域视图与订阅泄漏）。
    /// </summary>
    public override bool KeepAlive => false;

    #endregion 导航行为

    #region IMedicalCaseWorkspaceContext

    private WorkspaceState _state = new();
    public WorkspaceState State
    {
        get => _state;
        private set
        {
            if (SetProperty(ref _state, value))
            {
                // SetProperty 已通知 State 本身，此处只补派生属性与命令状态
                OnPropertyChanged(nameof(Completeness));
                Commands?.RefreshCanExecute();
                SaveChangesCommand?.NotifyCanExecuteChanged();
            }
        }
    }

    /// <summary>
    /// Phase 1.4: 完整性检查状态
    /// 从State.Completeness暴露出来，便于XAML绑定
    /// </summary>
    public CompletenessCheck Completeness => State.Completeness ?? new();

    private Guid _medicalCaseId = Guid.Empty;
    public Guid MedicalCaseId
    {
        get => _medicalCaseId;
        set
        {
            if (SetProperty(ref _medicalCaseId, value))
                ViewAuditLogsCommand?.NotifyCanExecuteChanged();
        }
    }

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
                ViewPatientHistoryCommand?.NotifyCanExecuteChanged();
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

    /// <summary>
    /// P1-2 修复：通过触发状态机的 EnterEdit 事件请求切换到编辑模式。
    /// 这会将 WorkspaceState.EditState 从 ReadOnly 正确转换到 Editing。
    /// </summary>
    void IWorkspaceHost.RequestEnterEditMode()
    {
        var result = _editStateMachine.Fire(WorkspaceEditEvent.EnterEdit, context: "User clicked EnterEditMode");
        if (!result)
        {
            Logger.LogWarning("EnterEditMode transition failed - state machine guard prevented transition");
        }
    }

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
        Gender.Male => "男",
        Gender.Female => "女",
        _ => "未知"
    };

    public DateTime? RegistrationTime => CurrentPatient?.CreatedAt;

    public Controls.Controls.PatientDisplayModel? CurrentPatientDisplayModel =>
        CurrentPatient == null ? null : new Controls.Controls.PatientDisplayModel
        {
            Name = CurrentPatient.Name ?? string.Empty,
            Gender = CurrentPatientGenderDisplay,
            Age = CurrentPatient.Age,
            PhoneNumber = CurrentPatient.PhoneNumber,
            RegistrationTime = RegistrationTime
        };

    #endregion

    #region Editable Properties (flat for TwoWay binding)

    private int _currentStep = 1;
    /// <summary>
    /// 当前工作流步骤 (1-5): 四诊采集→中医辨证→处方决策→处方编辑→完成看诊
    /// Phase 1.2: 自动推进逻辑
    /// </summary>
    public int CurrentStep
    {
        get => _currentStep;
        private set => SetProperty(ref _currentStep, value);
    }

    private string _editReason = string.Empty;
    public string EditReason { get => _editReason; set => SetProperty(ref _editReason, value); }

    private string _remark = string.Empty;
    public string Remark { get => _remark; set => SetProperty(ref _remark, value); }

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
            {
                OnPropertyChanged(nameof(NoPrescription));
                UpdateState(); // Phase 1.2: 更新步骤状态
            }
        }
    }

    public bool NoPrescription => !NeedsPrescription;

    private ObservableCollection<HerbListDto> _allHerbs = new();
    public ObservableCollection<HerbListDto> AllHerbs
    {
        get => _allHerbs;
        private set => SetProperty(ref _allHerbs, value);
    }

    /// <summary>
    /// 重复药材剂量合并策略（F-01: US-CFG-004 功能开关 DuplicateHerbMergeStrategy → 药材列表控件）
    /// </summary>
    public DuplicateDosageStrategy DuplicateStrategy { get; }

    #endregion

    #region Commands

    public IAsyncRelayCommand BackCommand { get; }
    public IAsyncRelayCommand BackToPatientSelectionCommand => BackCommand;
    public IRelayCommand ViewPatientHistoryCommand { get; }
    public IRelayCommand ViewAuditLogsCommand { get; }
    /// <summary>
    /// Management模式: 审计 + 保存 + 进入只读 (parent-level concern, not in child Commands VM)
    /// </summary>
    public IRelayCommand SaveChangesCommand { get; }

    #endregion

    #region Constructor

    public MedicalCaseWorkspaceViewModel(
        IViewModelServices services,
        IMedicalCaseService medicalCaseService,
        INavigationCoordinator navigationCoordinator,
        IActiveConsultationService activeConsultationService,
        IPatientService patientService,
        IToastService toastService,
        PrescriptionPrintHandler printHandler,
        IDialogService? dialogService = null,
        IFeatureToggleService? featureToggles = null)
        : base(services)
    {
        _medicalCaseService = medicalCaseService ?? throw new ArgumentNullException(nameof(medicalCaseService));
        _navigationCoordinator = navigationCoordinator ?? throw new ArgumentNullException(nameof(navigationCoordinator));
        _activeConsultationService = activeConsultationService ?? throw new ArgumentNullException(nameof(activeConsultationService));
        _patientService = patientService ?? throw new ArgumentNullException(nameof(patientService));
        _toastService = toastService ?? throw new ArgumentNullException(nameof(toastService));
        _dialogService = dialogService;

        // F-01: 配置的策略在每次导航（工作区 VM 不可复用）时读取；缺省/非法值回退 Max（服务内部已兜底）
        DuplicateStrategy = featureToggles?.GetDuplicateMergeStrategy() ?? DuplicateDosageStrategy.Max;

        // US-MC-011: Create edit mode FSM (lifecycle tied to this VM)
        _editStateMachine = new EditModeStateMachine(services.LoggerFactory.CreateLogger<EditModeStateMachine>());
        _editStateMachine.StateChanged += OnEditStateChangedFsm;

        // Create child VMs (not container-resolved; coupled to parent lifecycle)
        ConsultationEditor = new ConsultationEditorViewModel(this, this, services.LoggerFactory);
        PrescriptionEditor = new PrescriptionEditorViewModel(this, this, services.LoggerFactory);

        // Create extracted components
        _stateManager = new WorkspaceStateManager(
            _editStateMachine,
            () => ConsultationEditor,
            () => PrescriptionEditor,
            () => IsPrescriptionEnabled);

        _navHandler = new WorkspaceNavigationHandler(
            medicalCaseService,
            navigationCoordinator,
            activeConsultationService,
            _editStateMachine,
            dialogService,
            toastService,
            () => CommonDialogService,
            () => IsBusy,
            (busy, msg) => SetBusy(busy, msg),
            async () => await ShowSuccessMessageAsync("保存成功"),
            async (msg) => await ShowErrorMessageAsync(msg));

        Commands = new MedicalCaseCommandsViewModel(this, this, services.LoggerFactory, medicalCaseService, this, printHandler, _toastService, dialogService);

        // Wire PendingQueue suspend delegate

        // Parent-level commands
        BackCommand = new AsyncRelayCommand(ExecuteBackAsync);
        ViewPatientHistoryCommand = new RelayCommand(ExecuteViewPatientHistory, () => CurrentPatient != null);
        ViewAuditLogsCommand = new RelayCommand(ExecuteViewAuditLogs, () => MedicalCaseId != Guid.Empty);
        SaveChangesCommand = new RelayCommand(ExecuteSaveChanges, () => State.ShowSaveButton);
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
        var parameters = navigationContext.Parameters;
        MedicalCaseId = parameters.GetValue<Guid>(MedicalCaseNav.MedicalCaseId);
        CurrentPatient = parameters.GetValue<PatientDetailDto>(MedicalCaseNav.CurrentPatient);
        var workspaceMode = parameters.GetValue<WorkspaceMode>(MedicalCaseNav.WorkspaceMode);
        var initialEditState = parameters.GetValue<EditState>(MedicalCaseNav.InitialEditState);
        var editMode = parameters.GetValue<string>(MedicalCaseNav.EditMode);
        var isHistoricalEdit = editMode == "HistoricalEdit";
        // N4：记录返回目标（ClinicalWorkspace / RegistrationList / PatientSelection）
        _returnView = parameters.GetValue<string>(MedicalCaseNav.ReturnView);

        // PatientId-only 路径：CurrentPatient 缺失时按 PatientId 回填
        if (CurrentPatient == null)
        {
            var patientId = parameters.GetValue<Guid>(MedicalCaseNav.PatientId);
            if (patientId != Guid.Empty)
            {
                try
                {
                    var patientResult = await _patientService.GetByIdAsync(patientId);
                    if (patientResult.Success && patientResult.Data != null)
                        CurrentPatient = patientResult.Data;
                    else
                        Logger.LogWarning("按 PatientId={PatientId} 回填患者失败: {Error}", patientId, patientResult.Error);
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "按 PatientId={PatientId} 回填患者异常", patientId);
                }
            }
        }

        // 无上下文导航：明确失败而非空白页
        if (CurrentPatient == null && MedicalCaseId == Guid.Empty)
        {
            await ShowErrorMessageAsync("请从患者列表或挂号队列选择患者后再开始看诊");
            _ = _navigationCoordinator.NavigateToHome();
            return;
        }

        // Set mode early so ResumeSuspended can check it
        State = new WorkspaceState(Mode: workspaceMode);

        await InitializePatientInfoAsync();
        await LoadMedicalCaseDataAsync();
        await ResumeSuspendedIfNeededAsync();

        InitializeChildViewModels();
        DetermineEditMode(workspaceMode, initialEditState, isHistoricalEdit);

        _activeConsultationService.Register(MedicalCaseId, HandleLeaveRequestAsync);
    }

    /// <summary>
    /// 每次导航到工作区都新建实例并按导航参数完整重建（见 <see cref="OnNavigatedTo"/>）。
    /// 不复用实例的原因：本 VM 的按医案状态（Remark / EditReason / IsPrescriptionEnabled）不在导航中复位，
    /// 复用会把上一医案的编辑内容带入新医案；与 KeepAlive=false 配对（不复用即不保留）。
    /// </summary>
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
        var newState = _stateManager.UpdateState(State);
        CurrentStep = _stateManager.CalculateCurrentStep();
        if (State.Completeness != newState.Completeness || State.CanComplete != newState.CanComplete || State.CanPrint != newState.CanPrint)
        {
            State = newState;
        }
    }

    private void DetermineEditMode(WorkspaceMode workspaceMode, EditState initialEditState, bool isHistoricalEdit)
    {
        var (newState, canEdit, startEditing) = _stateManager.DetermineEditMode(
            State, workspaceMode, _medicalCaseService.Current,
            SessionManager?.CurrentUser?.Role, SessionManager?.CurrentUser?.Id ?? Guid.Empty,
            initialEditState, isHistoricalEdit);

        State = newState;
        _stateManager.InitializeEditStateMachine(canEdit, startEditing);
    }

    private void OnEditStateChangedFsm(object? sender, EditStateChangedEventArgs e)
    {
        State = _stateManager.OnEditStateChanged(State, e);
        Logger.LogDebug("WorkspaceState.EditState <- {NewEditState} (FSM: {FsmState})", State.EditState, e.NewState);
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
                if (!result.Success) { await ShowErrorMessageAsync("创建医案失败，请重试"); return; }
                MedicalCaseId = result.Data;
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
            if (!result.Success) return;
            if (result.Data?.PrescriptionItems?.Count > 0) IsPrescriptionEnabled = true;
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
        try
        {
            var medicalCase = _medicalCaseService.Current;
            if (medicalCase == null) return;

            if (State.Mode == WorkspaceMode.Clinical
                && medicalCase.CaseStatus == MedicalCaseStatus.Suspended)
            {
                Logger.LogInformation("[CMD] ResumeSuspended -> MedicalCaseId={MedicalCaseId}", MedicalCaseId);
                var result = await _medicalCaseService.ResumeSuspendedAsync(MedicalCaseId);
                if (result.Success) medicalCase.CaseStatus = MedicalCaseStatus.Active;
            }
        }
        catch (Exception ex)
        {
            // P2-A：恢复失败不阻断后续 InitializeChildViewModels / IActiveConsultationService.Register
            Logger.LogError(ex, "[WS] ResumeSuspended 失败: MedicalCaseId={MedicalCaseId}", MedicalCaseId);
        }
    }

    private void InitializeChildViewModels()
    {
        // Consultation
        if (_medicalCaseService.CurrentConsultation != null)
            ConsultationEditor.InitializeFromDto(_medicalCaseService.CurrentConsultation);
        else
            ConsultationEditor.InitializeForNewCase();

        // Prescription
        if (_medicalCaseService.CurrentPrescription != null)
            PrescriptionEditor.InitializeFromDto(_medicalCaseService.CurrentPrescription);
        else
            PrescriptionEditor.InitializeForNewCase();

        // Subscribe for state updates（先退订再订阅：本方法每次导航都会执行，多次执行不得叠加订阅）
        ConsultationEditor.Consultation.PropertyChanged -= OnChildPropertyChanged;
        ConsultationEditor.Consultation.PropertyChanged += OnChildPropertyChanged;
        PrescriptionEditor.Prescription.PropertyChanged -= OnChildPropertyChanged;
        PrescriptionEditor.Prescription.PropertyChanged += OnChildPropertyChanged;

        // Initial print state
        if (_medicalCaseService.CurrentPrescription?.Items is { Count: > 0 })
            State = State with { CanPrint = true };

        UpdateState();
    }

    #endregion

    #region Back Navigation & Leave Handling

    private async Task ExecuteBackAsync()
    {
        await _navHandler.ExecuteBackAsync(State, MedicalCaseId,
            () => ConsultationEditor.GetConsultationData(),
            () => PrescriptionEditor.GetPrescriptionData(),
            () => HandleLeaveRequestAsync(),
            _returnView);
    }

    /// <summary>
    /// Clinical模式三选项离开确认 (供IActiveConsultationService调用)
    /// </summary>
    public async Task<LeaveConsultationResult> HandleLeaveRequestAsync()
    {
        return await _navHandler.HandleLeaveRequestAsync(
            MedicalCaseId,
            () => ConsultationEditor.GetConsultationData(),
            () => PrescriptionEditor.GetPrescriptionData());
    }

    /// <summary>Management模式: 审计确认 + 保存 + 进入只读</summary>
    private void ExecuteSaveChanges()
        => _navHandler.ExecuteSaveChangesAsync(
            MedicalCaseId,
            () => ConsultationEditor.GetConsultationData(),
            () => PrescriptionEditor.GetPrescriptionData()).SafeFireAndForget(ex => Logger.LogError(ex, "保存医案失败"));

    #endregion

    #region Event Handlers

    private void OnChildPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case "PresentIllness":
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
        _ = _navigationCoordinator.NavigateTo(ViewNames.PatientManagement);
    }

    private void ExecuteViewAuditLogs()
    {
        if (MedicalCaseId == Guid.Empty) return;
        Logger.LogInformation("查看审计日志, MedicalCaseId: {MedicalCaseId}", MedicalCaseId);
        var parameters = new Dictionary<string, object> { { "MedicalCaseId", MedicalCaseId } };
        _ = _navigationCoordinator.NavigateTo(ViewNames.AuditLog, parameters);
    }

    #endregion

    #region IMedicalCaseDataProvider 实现

    ConsultationInputDto? IMedicalCaseDataProvider.GetConsultationData()
        => ConsultationEditor.GetConsultationData();

    PrescriptionInputDto? IMedicalCaseDataProvider.GetPrescriptionData()
        => PrescriptionEditor.GetPrescriptionData();

    IValidatable? IMedicalCaseDataProvider.GetConsultationValidator()
        => ConsultationEditor.Consultation;

    IValidatable? IMedicalCaseDataProvider.GetPrescriptionValidator()
        => PrescriptionEditor.Prescription;

    IDataProvider? IMedicalCaseDataProvider.GetPrescriptionProvider()
        => PrescriptionEditor.Prescription;

    ConsultationItem? IMedicalCaseDataProvider.GetConsultationItem()
        => ConsultationEditor.Consultation;

    PrescriptionItemViewModel? IMedicalCaseDataProvider.GetPrescriptionItem()
        => PrescriptionEditor.Prescription;

    IEnumerable<HerbListDto>? IMedicalCaseDataProvider.GetAllHerbs()
        => AllHerbs;

    string IMedicalCaseDataProvider.GetRemark() => Remark;

    string IMedicalCaseDataProvider.GetEditReason() => EditReason;

    bool IMedicalCaseDataProvider.GetIsPrescriptionEnabled() => IsPrescriptionEnabled;

    #endregion

    #region IDisposable

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _editStateMachine.StateChanged -= OnEditStateChangedFsm;
            _activeConsultationService.Unregister();
            ConsultationEditor.Consultation.PropertyChanged -= OnChildPropertyChanged;
            PrescriptionEditor.Prescription.PropertyChanged -= OnChildPropertyChanged;
            ConsultationEditor.Dispose();
            PrescriptionEditor.Dispose();
            Commands.Dispose();
        }
        base.Dispose(disposing);
    }

    #endregion
}
