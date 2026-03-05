using System.Collections.ObjectModel;
using LYBT.Desktop.CardReader.Integration;
using LYBT.Desktop.CardReader.Models;
using LYBT.Desktop.CardReader.Services;
using LYBT.Desktop.Clinical.Handlers; // OpenSpec: refactor-workspace-srp - Handler提取
using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Infrastructure.Constants;
using LYBT.Desktop.Infrastructure.Events;
using LYBT.Desktop.MedicalCase.Extensions; // OpenSpec: simplify-workspace-architecture - PrescriptionImportExtensions
using LYBT.Desktop.MedicalCase.Interfaces;
using LYBT.Desktop.MedicalCase.Mappers;
using LYBT.Desktop.MedicalCase.Models;
using LYBT.Desktop.MedicalCase.Models.Items;
using LYBT.Desktop.MedicalCase.ViewModels.Components;
using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Desktop.Models.ViewModels.Base;
using LYBT.Shared.ExceptionHandling.Mappers;
using LYBT.Shared.Models.Contracts.Formula;
using LYBT.Shared.Models.Contracts.Herbs;
// OpenSpec: refactor-medicalcase-workspace - 使用接口解耦，不直接引用Patients模块
// OpenSpec: create-printing-module - IPrescriptionPrintService已迁移到独立Printing模块，通过PrescriptionPrintHandler使用
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Shared.Models.Contracts.Prescriptions;
using Microsoft.Extensions.Logging;
using Prism.Commands;
using Prism.Events;
using Prism.Regions;
using Prism.Services.Dialogs;

namespace LYBT.Desktop.Clinical.ViewModels;

/// <summary>
/// 医案工作区ViewModel - 4:6统一看诊界面
/// OpenSpec: refactor-clinical-workflow - 从MedicalCase模块迁移到Clinical模块
/// OpenSpec: refactor-oversized-viewmodels - 重构后 &lt; 500行
/// OpenSpec: refactor-viewmodel-base-classes - 从UnifiedViewModelBase迁移到NavigableViewModelBase
/// </summary>
public class MedicalCaseWorkspaceViewModel : NavigableViewModelBase
{
    #region 字段

    private readonly IRegionManager _regionManager;
    private readonly INavigationCoordinator _navigationCoordinator;
    private readonly IMedicalCaseService _medicalCaseService;
    private readonly IActiveConsultationService _activeConsultationService;
    private readonly IDialogService? _dialogService;
    private readonly MedicalCaseEditModeStateMachine _editModeStateMachine;
    private readonly IPendingQueueManager _pendingQueueManager;
    private readonly PrescriptionPrintHandler _printHandler;
    private readonly PendingQueueHandler _pendingQueueHandler;
    private readonly PrescriptionImportHandler _prescriptionImportHandler;
    private readonly CardReaderWorkspaceHandler _cardReaderHandler;
    private readonly IPatientCardReaderIntegration _patientCardReaderIntegration;

    #endregion

    #region 属性

    /// <summary>
    /// 工作区状态聚合对象
    /// OpenSpec: slim-workspace-viewmodel - State对象模式
    /// </summary>
    public WorkspaceState State { get; } = new();

    // OpenSpec: slim-workspace-viewmodel - 以下属性委托给State，保持XAML兼容
    public string PatientName { get => State.PatientName; set => State.PatientName = value; }
    public string PatientInfo { get => State.PatientInfo; set => State.PatientInfo = value; }

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
            }
        }
    }

    /// <summary>
    /// 患者性别显示文本 - OpenSpec: refactor-medicalcase-workspace
    /// </summary>
    public string CurrentPatientGenderDisplay => CurrentPatient?.Gender switch
    {
        Shared.Models.Enums.Gender.Male => "男",
        Shared.Models.Enums.Gender.Female => "女",
        _ => "未知"
    };

    /// <summary>
    /// 挂号时间 - OpenSpec: refactor-medicalcase-workspace
    /// 使用患者创建时间作为挂号时间（简化实现）
    /// </summary>
    public DateTime? RegistrationTime => CurrentPatient?.CreatedAt;

    /// <summary>
    /// 患者信息展示模型 - OpenSpec: refactor-medicalcase-workspace
    /// 用于PatientInfoCardControl数据绑定
    /// </summary>
    public Infrastructure.Controls.PatientDisplayModel? CurrentPatientDisplayModel => CurrentPatient == null ? null : new Infrastructure.Controls.PatientDisplayModel
    {
        Name = CurrentPatient.Name ?? string.Empty,
        Gender = CurrentPatientGenderDisplay,
        Age = CurrentPatient.Age,
        PhoneNumber = CurrentPatient.PhoneNumber,
        VisitCount = CurrentPatient.VisitCount,
        RegistrationTime = RegistrationTime
    };

    /// <summary>
    /// 查看患者历史命令 - OpenSpec: refactor-medicalcase-workspace
    /// </summary>
    public DelegateCommand ViewPatientHistoryCommand { get; }

    /// <summary>
    /// 诊断数据Item - OpenSpec: consolidate-panel-viewmodels
    /// 遵循Entity→DTO→Item模式，直接持有ConsultationItem替代ConsultationPanelViewModel
    /// </summary>
    public ConsultationItem Consultation { get; } = new();

    /// <summary>
    /// 处方数据Item - OpenSpec: consolidate-panel-viewmodels
    /// 遵循Entity→DTO→Item模式，直接持有PrescriptionItem替代PrescriptionPanelViewModel
    /// </summary>
    public PrescriptionItem Prescription { get; } = new();

    /// <summary>
    /// 药材库数据 - 供HerbListControl绑定
    /// OpenSpec: consolidate-panel-viewmodels - 从PrescriptionPanelViewModel迁移
    /// </summary>
    private ObservableCollection<HerbListDto> _allHerbs = new();
    public ObservableCollection<HerbListDto> AllHerbs
    {
        get => _allHerbs;
        private set => SetProperty(ref _allHerbs, value);
    }

    // OpenSpec: consolidate-panel-viewmodels - ConsultationPanelViewModel和PrescriptionPanelViewModel已删除，使用Item模式

    // OpenSpec: slim-workspace-viewmodel - IsPrescriptionEnabled委托给State
    // OpenSpec: simplify-workspace-architecture - 同步Prescription.ValidationEnabled
    public bool IsPrescriptionEnabled
    {
        get => State.IsPrescriptionEnabled;
        set
        {
            if (State.IsPrescriptionEnabled != value)
            {
                State.IsPrescriptionEnabled = value;
                Prescription.ValidationEnabled = value; // 同步验证启用状态
                OnPropertyChanged(nameof(IsPrescriptionEnabled));
                UpdateCanComplete();
            }
        }
    }

    /// <summary>
    /// 是否需要开处方（医案级别属性）
    /// OpenSpec: optimize-medicalcase-navigation - 移至医案ViewModel，用CheckBox控制
    /// OpenSpec: slim-workspace-viewmodel - 委托给State
    /// </summary>
    public bool NeedsPrescription
    {
        get => State.NeedsPrescription;
        set
        {
            if (State.NeedsPrescription != value)
            {
                State.NeedsPrescription = value;
                OnPropertyChanged(nameof(NeedsPrescription));
                OnPropertyChanged(nameof(NoPrescription));
            }
        }
    }

    /// <summary>
    /// 不开处方（反向绑定，用于UI显示）
    /// </summary>
    public bool NoPrescription => State.NoPrescription;

    // OpenSpec: slim-workspace-viewmodel - 以下属性委托给State
    public bool CanPrintPrescription { get => State.CanPrintPrescription; set { State.CanPrintPrescription = value; OnPropertyChanged(nameof(CanPrintPrescription)); } }
    public bool CanComplete { get => State.CanComplete; set { State.CanComplete = value; OnPropertyChanged(nameof(CanComplete)); } }
    public bool IsFromManagement { get => State.IsFromManagement; set { State.IsFromManagement = value; OnPropertyChanged(nameof(IsFromManagement)); } }

    /// <summary>
    /// 是否有未保存的更改 - 重写基类以提供处方更改状态
    /// OpenSpec: unify-navigation-architecture - IConfirmNavigationRequest支持
    /// OpenSpec: slim-workspace-viewmodel - 委托给State
    /// </summary>
    protected override bool HasUnsavedChanges => State.HasUnsavedChanges;
    public bool HasUnsavedPrescriptionChanges
    {
        get => State.HasUnsavedChanges;
        private set { State.HasUnsavedChanges = value; OnPropertyChanged(nameof(HasUnsavedPrescriptionChanges)); OnPropertyChanged(nameof(HasUnsavedChanges)); }
    }

    private string _remark = string.Empty;
    public string Remark
    {
        get => _remark;
        set { if (SetProperty(ref _remark, value) && _medicalCaseService.CachedMedicalCase != null) _medicalCaseService.CachedMedicalCase.Remark = value; }
    }

    // 委托给状态机的属性
    public bool IsEditing => _editModeStateMachine.IsEditing;
    public bool IsReadOnly => _editModeStateMachine.IsReadOnly;
    public bool ShowEditButton => _editModeStateMachine.ShowEditButton;
    public bool ShowEditButtonTopRight => _editModeStateMachine.ShowEditButtonTopRight;
    public bool ShowSaveButton => _editModeStateMachine.ShowSaveButton;
    public bool ShowSuspendButton => _editModeStateMachine.ShowSuspendButton;
    public bool ShowCompleteButton => _editModeStateMachine.ShowCompleteButton;
    public bool IsHistoricalEditMode => _editModeStateMachine.IsHistoricalEditMode;
    public bool CanEdit => _editModeStateMachine.CanEdit;
    public string EditReason { get => _editModeStateMachine.EditReason; set => _editModeStateMachine.EditReason = value; }
    public WorkspaceMode WorkspaceMode { get => _editModeStateMachine.WorkspaceMode; set => _editModeStateMachine.WorkspaceMode = value; }
    public string HeaderTitle => _editModeStateMachine.HeaderTitle;
    public string BackButtonText => _editModeStateMachine.BackButtonText;

    // OpenSpec: refactor-medicalcase-workspace V2 TASK-V2-010 - 待诊队列UI属性恢复
    // 待诊队列现在显示在Workspace左侧，需要以下属性支持

    /// <summary>
    /// 待诊队列 - 通过IPendingQueueManager获取
    /// </summary>
    public System.Collections.ObjectModel.ObservableCollection<PendingMedicalCaseDto> PendingQueue =>
        _pendingQueueManager.PendingQueue;

    private PendingMedicalCaseDto? _selectedPendingCase;
    /// <summary>
    /// 当前选中的待诊患者
    /// </summary>
    public PendingMedicalCaseDto? SelectedPendingCase
    {
        get => _selectedPendingCase;
        set => SetProperty(ref _selectedPendingCase, value);
    }

    /// <summary>
    /// 是否正在刷新待诊队列
    /// OpenSpec: slim-workspace-viewmodel - 委托给State
    /// </summary>
    public bool IsRefreshingPendingQueue
    {
        get => State.IsRefreshingPendingQueue;
        set { State.IsRefreshingPendingQueue = value; OnPropertyChanged(nameof(IsRefreshingPendingQueue)); }
    }

    /// <summary>
    /// 待诊队列是否为空
    /// </summary>
    public bool HasNoPendingCases => PendingQueue == null || PendingQueue.Count == 0;

    #region 读卡器属性 - OpenSpec: integrate-cardreader-module

    /// <summary>
    /// 读卡器是否已连接
    /// </summary>
    public bool IsCardReaderConnected => _cardReaderHandler?.IsConnected ?? false;

    /// <summary>
    /// 是否启用自动读卡
    /// </summary>
    public bool IsAutoReadEnabled
    {
        get => _cardReaderHandler?.IsAutoReadEnabled ?? false;
        set
        {
            if (_cardReaderHandler != null && _cardReaderHandler.IsAutoReadEnabled != value)
            {
                _cardReaderHandler.ToggleAutoRead();
                OnPropertyChanged(nameof(IsAutoReadEnabled));
            }
        }
    }

    /// <summary>
    /// 是否正在读卡
    /// </summary>
    public bool IsReading => _cardReaderHandler?.IsReading ?? false;

    /// <summary>
    /// 读卡器状态信息
    /// </summary>
    public string CardReaderStatusMessage => _cardReaderHandler?.StatusMessage ?? "读卡器未连接";

    #endregion

    #endregion

    #region 命令

    public DelegateCommand BackCommand { get; }
    public DelegateCommand BackToPatientSelectionCommand => BackCommand;
    /// <summary>
    /// Clinical模式: 挂起医案命令 - 保存为Suspended状态，留在当前界面
    /// </summary>
    public DelegateCommand SuspendCommand { get; }
    /// <summary>
    /// Management模式: 保存修改命令 - 保存修改，留在当前界面
    /// </summary>
    public DelegateCommand SaveChangesCommand { get; }
    public DelegateCommand PrintPrescriptionCommand { get; }
    /// <summary>
    /// 完成医案命令 - 完成当前医案
    /// </summary>
    public DelegateCommand CompleteMedicalCaseCommand { get; }
    public DelegateCommand SaveCommand { get; }
    public DelegateCommand EnterEditModeCommand { get; }

    // OpenSpec: refactor-medicalcase-workspace V2 TASK-V2-010 - 待诊队列命令恢复
    /// <summary>
    /// 刷新待诊队列命令
    /// </summary>
    public DelegateCommand RefreshQueueCommand { get; }

    /// <summary>
    /// 选择待诊患者命令
    /// </summary>
    public DelegateCommand<PendingMedicalCaseDto> SelectPendingCaseCommand { get; }

    // OpenSpec: consolidate-panel-viewmodels - 处方编辑命令（从PrescriptionPanelViewModel迁移）
    /// <summary>
    /// 打开验方导入对话框命令
    /// </summary>
    public DelegateCommand OpenFormulaImportDialogCommand { get; }

    /// <summary>
    /// 打开历史处方复制对话框命令
    /// </summary>
    public DelegateCommand OpenHistoryCopyDialogCommand { get; }

    /// <summary>
    /// 清空药材列表命令
    /// </summary>
    public DelegateCommand ClearHerbItemsCommand { get; }

    // OpenSpec: integrate-cardreader-module - 读卡器命令
    /// <summary>
    /// 手动读卡命令
    /// </summary>
    public DelegateCommand ReadCardCommand { get; }

    /// <summary>
    /// 切换自动读卡命令
    /// </summary>
    public DelegateCommand ToggleAutoReadCommand { get; }

    #endregion

    #region 构造函数

    public MedicalCaseWorkspaceViewModel(
        IViewModelServices services,
        IMedicalCaseService medicalCaseService,
        MedicalCaseEditModeStateMachine editModeStateMachine,
        INavigationCoordinator navigationCoordinator,
        IActiveConsultationService activeConsultationService,
        IPendingQueueManager pendingQueueManager,
        PrescriptionPrintHandler printHandler,
        ICardReaderService cardReaderService,
        IPatientCardReaderIntegration patientCardReaderIntegration,
        IDialogService? dialogService = null)
        : base(services)
    {
        _regionManager = services.RegionManager;
        _navigationCoordinator = navigationCoordinator ?? throw new ArgumentNullException(nameof(navigationCoordinator));
        _medicalCaseService = medicalCaseService ?? throw new ArgumentNullException(nameof(medicalCaseService));
        _activeConsultationService = activeConsultationService ?? throw new ArgumentNullException(nameof(activeConsultationService));
        _dialogService = dialogService;
        _editModeStateMachine = editModeStateMachine ?? throw new ArgumentNullException(nameof(editModeStateMachine));
        _pendingQueueManager = pendingQueueManager;
        _printHandler = printHandler;

        // OpenSpec: refactor-workspace-srp - 初始化Handler
        _pendingQueueHandler = new PendingQueueHandler(
            medicalCaseService,
            pendingQueueManager,
            navigationCoordinator,
            services.LoggerFactory);
        _pendingQueueHandler.GetCommonDialogService = () => CommonDialogService;
        _pendingQueueHandler.SetBusy = (busy, msg) => SetBusy(busy, msg);
        _pendingQueueHandler.ShowErrorMessage = ShowErrorMessageAsync;
        _pendingQueueHandler.GetCurrentMedicalCaseId = () => MedicalCaseId;
        _pendingQueueHandler.GetCurrentPatient = () => CurrentPatient;
        _pendingQueueHandler.GetIsReadOnly = () => IsReadOnly;
        _pendingQueueHandler.SuspendOnly = SuspendOnlyAsync;
        _pendingQueueHandler.OnPropertyChanged = OnPropertyChanged;

        _prescriptionImportHandler = new PrescriptionImportHandler(
            dialogService,
            services.LoggerFactory);
        _prescriptionImportHandler.GetCommonDialogService = () => CommonDialogService;
        _prescriptionImportHandler.SetBusy = (busy, msg) => SetBusy(busy, msg);
        _prescriptionImportHandler.ShowErrorMessage = ShowErrorMessageAsync;
        _prescriptionImportHandler.ShowSuccessMessage = ShowSuccessMessageAsync;
        _prescriptionImportHandler.ShowConfirmMessage = ShowConfirmMessageAsync;
        _prescriptionImportHandler.GetCurrentPatient = () => CurrentPatient;
        _prescriptionImportHandler.GetPrescription = () => Prescription;
        _prescriptionImportHandler.GetAllHerbs = () => AllHerbs;

        // OpenSpec: integrate-cardreader-module - 保存读卡器集成服务引用
        _patientCardReaderIntegration = patientCardReaderIntegration ?? throw new ArgumentNullException(nameof(patientCardReaderIntegration));

        // OpenSpec: integrate-cardreader-module - 初始化读卡器Handler
        _cardReaderHandler = new CardReaderWorkspaceHandler(
            cardReaderService,
            patientCardReaderIntegration,
            medicalCaseService,
            navigationCoordinator,
            services.LoggerFactory);
        _cardReaderHandler.GetCommonDialogService = () => CommonDialogService;
        _cardReaderHandler.SetBusy = (busy, msg) => SetBusy(busy, msg);
        _cardReaderHandler.ShowErrorMessage = ShowErrorMessageAsync;
        _cardReaderHandler.ShowSuccessMessage = ShowSuccessMessageAsync;
        _cardReaderHandler.OnPropertyChanged = propertyName =>
        {
            // 转发Handler属性变更到ViewModel
            switch (propertyName)
            {
                case nameof(CardReaderWorkspaceHandler.IsConnected):
                    OnPropertyChanged(nameof(IsCardReaderConnected));
                    break;
                case nameof(CardReaderWorkspaceHandler.IsAutoReadEnabled):
                    OnPropertyChanged(nameof(IsAutoReadEnabled));
                    break;
                case nameof(CardReaderWorkspaceHandler.IsReading):
                    OnPropertyChanged(nameof(IsReading));
                    break;
                case nameof(CardReaderWorkspaceHandler.StatusMessage):
                    OnPropertyChanged(nameof(CardReaderStatusMessage));
                    break;
            }
        };
        _cardReaderHandler.OnPatientReadyForMedicalCase = HandlePatientReadyForMedicalCaseAsync;

        // 订阅事件
        _editModeStateMachine.EditStateChanged += OnEditStateChanged;

        // 初始化命令
        BackCommand = new DelegateCommand(async () => await ExecuteBackAsync());
        // Clinical模式: 挂起医案
        SuspendCommand = new DelegateCommand(ExecuteSuspend, () => WorkspaceMode == WorkspaceMode.Clinical && _editModeStateMachine.IsEditing);
        // Management模式: 保存修改
        SaveChangesCommand = new DelegateCommand(ExecuteSaveChanges, () => WorkspaceMode == WorkspaceMode.Management && _editModeStateMachine.IsEditing);
        PrintPrescriptionCommand = new DelegateCommand(ExecutePrintPrescription, () => CanPrintPrescription).ObservesProperty(() => CanPrintPrescription);
        CompleteMedicalCaseCommand = new DelegateCommand(ExecuteCompleteMedicalCase, () => CanComplete).ObservesProperty(() => CanComplete);
        SaveCommand = new DelegateCommand(ExecuteSave, () => _editModeStateMachine.IsEditing);
        EnterEditModeCommand = new DelegateCommand(ExecuteEnterEditMode, () => _editModeStateMachine.CanEnterEditMode);
        ViewPatientHistoryCommand = new DelegateCommand(ExecuteViewPatientHistory, () => CurrentPatient != null).ObservesProperty(() => CurrentPatient);

        // OpenSpec: refactor-workspace-srp - 待诊队列命令委托给Handler
        RefreshQueueCommand = new DelegateCommand(async () => await _pendingQueueHandler.RefreshQueueAsync(v => IsRefreshingPendingQueue = v));
        SelectPendingCaseCommand = new DelegateCommand<PendingMedicalCaseDto>(async p => await _pendingQueueHandler.SelectPendingCaseAsync(p));

        // OpenSpec: refactor-workspace-srp - 处方导入命令委托给Handler
        OpenFormulaImportDialogCommand = new DelegateCommand(() => _prescriptionImportHandler.OpenFormulaImportDialog());
        OpenHistoryCopyDialogCommand = new DelegateCommand(() => _prescriptionImportHandler.OpenHistoryCopyDialog());
        ClearHerbItemsCommand = new DelegateCommand(async () => await _prescriptionImportHandler.ClearHerbItemsAsync());

        // OpenSpec: integrate-cardreader-module - 读卡器命令初始化
        ReadCardCommand = new DelegateCommand(async () => await _cardReaderHandler.ManualReadCardAsync(), () => _cardReaderHandler?.IsConnected == true);
        ToggleAutoReadCommand = new DelegateCommand(() => _cardReaderHandler?.ToggleAutoRead(), () => _cardReaderHandler?.IsConnected == true);

        // 订阅Prism事件 - OpenSpec: unify-event-system
        EventAggregator.GetEvent<CaseEvents.ConsultationCompletedEvent>().Subscribe(OnConsultationCompleted, ThreadOption.UIThread);
        EventAggregator.GetEvent<CaseEvents.PrescriptionCompletedEvent>().Subscribe(OnPrescriptionCompleted, ThreadOption.UIThread);
        // OpenSpec: simplify-workspace-event-architecture (Phase 4) - PrescriptionSavedEvent改用回调模式
    }

    #endregion

    #region 命令实现

    private async void ExecuteSave()
    {
        try
        {
            SetBusy(true, "正在保存...");
            var result = await _medicalCaseService.AggregateSaveAsync(
                MedicalCaseId, GetConsultationData(), GetPrescriptionData(), Remark, EditReason);
            if (result.Success) { if (IsHistoricalEditMode && !string.IsNullOrWhiteSpace(EditReason)) Logger.LogInformation("历史修改保存，原因: {EditReason}", EditReason); await ShowSuccessMessageAsync("保存成功"); }
            else await ShowErrorMessageAsync(result.Error ?? "保存失败");
        }
        catch (Exception ex) { Logger.LogError(ex, "保存医案数据失败"); await ShowErrorMessageAsync(ClientErrorMessageMapper.GetSafeOperationFailureMessage("保存", ex)); }
        finally { SetBusy(false); }
    }

    private void ExecuteEnterEditMode()
    {
        if (_editModeStateMachine.EnterEditMode()) Logger.LogInformation("进入编辑模式，MedicalCaseId: {MedicalCaseId}", MedicalCaseId);
        else Logger.LogWarning("无编辑权限，无法进入编辑模式");
    }

    /// <summary>
    /// 执行返回导航
    /// Clinical: 显示三选项对话框后返回PatientSelectionView
    /// Management只读: 直接返回MedicalCaseMasterDetailView
    /// Management编辑: 显示UnsavedChangesDialog后返回
    /// OpenSpec: simplify-workspace-architecture - 从NavigationHandler内联
    /// </summary>
    private async Task ExecuteBackAsync()
    {
        try
        {
            // Management模式处理
            if (WorkspaceMode == WorkspaceMode.Management)
            {
                // Management只读模式: 直接返回
                if (IsReadOnly)
                {
                    _navigationCoordinator.NavigateTo(ViewNames.MedicalCaseMasterDetail);
                    return;
                }

                // Management编辑模式: 显示UnsavedChangesDialog
                var shouldNavigate = await HandleManagementLeaveRequestAsync();
                if (shouldNavigate)
                {
                    _navigationCoordinator.NavigateTo(ViewNames.MedicalCaseMasterDetail);
                }
                return;
            }

            // Clinical模式: 使用现有的三选项对话框
            var result = await HandleLeaveRequestAsync();
            if (result.CanLeave)
            {
                _navigationCoordinator.NavigateTo(ViewNames.PatientSelection);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Navigation.ExecuteBack failed - Mode={Mode} IsReadOnly={IsReadOnly}", WorkspaceMode, IsReadOnly);
        }
    }

    /// <summary>
    /// Management编辑模式返回确认
    /// OpenSpec: simplify-workspace-architecture - 从NavigationHandler内联
    /// 三选项: 保存修改(Yes) / 放弃修改(No) / 取消(Cancel)
    /// </summary>
    /// <returns>true: 允许导航; false: 留在当前界面</returns>
    private async Task<bool> HandleManagementLeaveRequestAsync()
    {
        if (_dialogService == null)
        {
            Logger.LogWarning("Navigation.HandleManagementLeave → DialogServiceUnavailable");
            return false;
        }

        var tcs = new TaskCompletionSource<bool>();
        _dialogService.ShowDialog("UnsavedChangesDialog", new DialogParameters(), async dialogResult =>
        {
            try
            {
                switch (dialogResult.Result)
                {
                    case ButtonResult.Yes: // 保存修改
                        // 检查审计需求
                        var auditReason = await CheckAndGetAuditReasonAsync();
                        if (auditReason == null)
                        {
                            tcs.SetResult(false); // 用户取消审计
                            return;
                        }

                        if (!string.IsNullOrEmpty(auditReason))
                        {
                            _editModeStateMachine.EditReason = auditReason;
                        }

                        await SuspendOnlyAsync();
                        _editModeStateMachine.EnterReadOnlyMode();
                        tcs.SetResult(true);
                        break;
                    case ButtonResult.No: // 放弃修改
                        tcs.SetResult(true);
                        break;
                    default: // 取消
                        tcs.SetResult(false);
                        break;
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Navigation.HandleManagementLeave failed");
                tcs.SetResult(false);
            }
        });

        return await tcs.Task;
    }

    /// <summary>
    /// 显示离开确认对话框（三选项）并处理用户选择
    /// OpenSpec: simplify-workspace-architecture - 从NavigationHandler内联
    /// 此方法可由IActiveConsultationService调用（退出登录时）
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
            Logger.LogWarning("Navigation.HandleLeaveRequest → CommonDialogServiceUnavailable");
            choice = LeaveConsultationChoice.Stay;
        }

        // 根据用户选择执行对应操作
        switch (choice)
        {
            case LeaveConsultationChoice.Suspend:
                Logger.LogDebug("Navigation.HandleLeaveRequest → Suspend");
                await SuspendOnlyAsync();
                return LeaveConsultationResult.AllowLeave(choice);

            case LeaveConsultationChoice.CancelCase:
                Logger.LogDebug("Navigation.HandleLeaveRequest → CancelCase");
                await CancelCaseOnlyAsync();
                return LeaveConsultationResult.AllowLeave(choice);

            case LeaveConsultationChoice.Stay:
            default:
                Logger.LogDebug("Navigation.HandleLeaveRequest → Stay");
                return LeaveConsultationResult.CancelLeave();
        }
    }

    // OpenSpec: refactor-diagnosis-fields - 移除SyncRemarkToPanel方法，MedicalCaseRemark已从ConsultationPanelViewModel移除

    // 数据收集：从 Item 提取 DTO 供 Service 调用
    private ConsultationInputDto? GetConsultationData() => ((IDataProvider)Consultation).GetConsultationData();
    private PrescriptionInputDto? GetPrescriptionData() => ((IDataProvider)Prescription).GetPrescriptionData();
    private IValidatable GetConsultationValidator() => Consultation;
    private IValidatable GetPrescriptionValidator() => Prescription;
    // 保留 IDataProvider 引用供 PrintHandler 使用
    private IDataProvider GetPrescriptionProvider() => Prescription;

    private async Task SuspendOnlyAsync()
    {
        try { SetBusy(true, "正在保存..."); await _medicalCaseService.SaveAndSuspendAsync(MedicalCaseId, GetConsultationData(), GetPrescriptionData(), Remark); }
        finally { SetBusy(false); }
    }

    private async Task CancelCaseOnlyAsync()
    {
        try { SetBusy(true, "正在处理..."); await _medicalCaseService.SaveAndCancelAsync(MedicalCaseId, GetConsultationData(), GetPrescriptionData(), Remark); }
        finally { SetBusy(false); }
    }

    /// <summary>
    /// Clinical模式: 挂起医案 - 保存为Suspended状态，留在当前界面继续编辑
    /// </summary>
    private async void ExecuteSuspend()
    {
        try
        {
            SetBusy(true, "正在挂起...");
            var result = await _medicalCaseService.SaveAndSuspendAsync(MedicalCaseId, GetConsultationData(), GetPrescriptionData(), Remark);
            if (result.Success)
            {
                _editModeStateMachine.EnterReadOnlyMode();
                await ShowSuccessMessageAsync("医案已暂存，可随时点击'修改医案'继续编辑");
                // OpenSpec: simplify-workspace-architecture - 委托Handler刷新待诊队列
                await _pendingQueueHandler.RefreshQueueAsync(v => IsRefreshingPendingQueue = v);
            }
            else
            {
                await ShowErrorMessageAsync(result.Error ?? "暂存失败");
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "暂存医案失败");
            await ShowErrorMessageAsync(ClientErrorMessageMapper.GetSafeOperationFailureMessage("暂存", ex));
        }
        finally { SetBusy(false); }
    }

    /// <summary>
    /// Management模式: 保存修改 - 保存修改，留在当前界面
    /// </summary>
    private async void ExecuteSaveChanges()
    {
        try
        {
            // Management模式需要审计原因
            var auditReason = await CheckAndGetAuditReasonAsync();
            if (auditReason == null) return;
            if (!string.IsNullOrEmpty(auditReason)) EditReason = auditReason;

            SetBusy(true, "正在保存...");
            var result = await _medicalCaseService.SaveAndSuspendAsync(MedicalCaseId, GetConsultationData(), GetPrescriptionData(), Remark);
            if (result.Success)
            {
                _editModeStateMachine.EnterReadOnlyMode();
                await ShowSuccessMessageAsync("保存成功");
            }
            else
            {
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

    /// <summary>
    /// 检查并获取审计原因 - 占位实现
    /// OpenSpec: migrate-views-to-role-modules - 审计功能后续单独规划
    /// </summary>
    private Task<string?> CheckAndGetAuditReasonAsync()
    {
        // TODO: 审计功能将来单独创建project实现
        return Task.FromResult<string?>(string.Empty);
    }

    /// <summary>
    /// 执行查看患者历史记录
    /// OpenSpec: refactor-medicalcase-workspace
    /// </summary>
    /// <summary>查看患者历史 - OpenSpec: migrate-views-to-role-modules</summary>
    private void ExecuteViewPatientHistory()
    {
        if (CurrentPatient == null) return;

        Logger.LogInformation("查看患者历史，PatientId: {PatientId}", CurrentPatient.Id);

        // 导航到患者管理视图，用户可在MasterDetail界面查看患者详情
        _navigationCoordinator.NavigateTo(ViewNames.PatientManagement);
    }

    // OpenSpec: refactor-workspace-srp - 待诊队列操作已迁移到PendingQueueHandler

    // OpenSpec: refactor-workspace-srp - 处方编辑命令已迁移到PrescriptionImportHandler

    /// <summary>
    /// 执行打印处方笺
    /// OpenSpec: print-prescription-slip
    /// </summary>
    private async void ExecutePrintPrescription()
    {
        try
        {
            SetBusy(true, "正在准备预览...");

            // OpenSpec: slim-medicalcase-viewmodel - 委托给打印处理器
            // OpenSpec: consolidate-panel-viewmodels - 使用Mapper.ToInputDto()替代实例方法
            // OpenSpec: adopt-mapperly-unified-mapping - 使用Mapper实例方法
            var consultationMapper = new ConsultationMapper();
            var result = await _printHandler.PrintPreviewAsync(
                MedicalCaseId,
                GetPrescriptionProvider(),
                CurrentPatient,
                consultationMapper.ToInputDto(Consultation));

            if (!result.IsSuccess)
            {
                await ShowErrorMessageAsync(result.ErrorMessage ?? "打印失败");
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "打印处方笺失败");
            await ShowErrorMessageAsync(ClientErrorMessageMapper.GetSafeOperationFailureMessage("打印", ex));
        }
        finally
        {
            SetBusy(false);
        }
    }

    /// <summary>
    /// 构建处方DTO用于打印
    /// OpenSpec: print-prescription-slip
    /// </summary>
    // OpenSpec: slim-medicalcase-viewmodel - BuildPrescriptionDetailDto已移至PrescriptionPrintHandler

    /// <summary>
    /// 完成医案 - 完成当前医案后留在当前界面，刷新待诊队列
    /// OpenSpec: refactor-medicalcase-workspace - 完成后不跳转，界面有待诊清单可选择下一位患者
    /// </summary>
    private async void ExecuteCompleteMedicalCase()
    {
        try
        {
            SetBusy(true, "正在完成医案...");
            var result = await _medicalCaseService.SaveAndCompleteAsync(
                MedicalCaseId, GetConsultationData(), GetPrescriptionData(),
                GetConsultationValidator(), GetPrescriptionValidator(), Remark, IsPrescriptionEnabled);
            if (result.Success)
            {
                await ShowSuccessMessageAsync("医案已完成，请从待诊列表选择下一位患者");
                // 进入只读模式
                _editModeStateMachine.EnterReadOnlyMode();
                // OpenSpec: simplify-workspace-architecture - 委托Handler刷新待诊队列
                await _pendingQueueHandler.RefreshQueueAsync(v => IsRefreshingPendingQueue = v);
                // 更新按钮状态
                OnPropertyChanged(nameof(ShowCompleteButton));
                OnPropertyChanged(nameof(ShowSuspendButton));
                OnPropertyChanged(nameof(CanComplete));
            }
            else
            {
                await ShowErrorMessageAsync(result.Error ?? "完成失败");
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "完成医案失败");
            await ShowErrorMessageAsync(ClientErrorMessageMapper.GetSafeOperationFailureMessage("完成医案", ex));
        }
        finally { SetBusy(false); }
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

        // OpenSpec: refactor-medicalcase-workspace Phase 5 - 从待诊队列恢复挂起医案
        // Clinical模式下，如果加载的是Suspended状态医案，自动恢复为Active
        await ResumeSuspendedIfNeededAsync();

        InitializeChildViewModels();
        await DetermineEditModeAsync(initialEditState, isHistoricalEdit);

        // OpenSpec: simplify-workspace-architecture - 委托Handler加载待诊队列
        _ = _pendingQueueHandler.RefreshQueueAsync(v => IsRefreshingPendingQueue = v);

        // OpenSpec: integrate-cardreader-module - 初始化读卡器
        _ = _cardReaderHandler.InitializeAsync();
    }

    private async Task DetermineEditModeAsync(EditState initialEditState = EditState.Editing, bool isHistoricalEdit = false)
    {
        var medicalCase = _medicalCaseService.CachedMedicalCase;
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
        // OpenSpec: slim-workspace-viewmodel - 使用State.UpdateFromPatient替代手动设置
        State.UpdateFromPatient(CurrentPatient);
        OnPropertyChanged(nameof(PatientName));
        OnPropertyChanged(nameof(PatientInfo));
        if (MedicalCaseId == Guid.Empty)
        {
            try
            {
                SetBusy(true, "正在创建医案...");
                var result = await _medicalCaseService.CreateMedicalCaseAsync(CurrentPatient.Id);
                if (!result.success) { await ShowErrorMessageAsync("创建医案失败，请重试"); return; }
                MedicalCaseId = result.medicalCaseId;
            }
            catch (Exception ex) { Logger.LogError(ex, "创建医案失败"); await ShowErrorMessageAsync(ClientErrorMessageMapper.GetSafeOperationFailureMessage("创建医案", ex)); }
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
            var hasPrescription = result.detail?.Prescription != null;
            // 根据是否有处方来决定是否启用处方面板
            if (hasPrescription) IsPrescriptionEnabled = true;
            Remark = result.detail?.Remark ?? string.Empty;
            // NeedsPrescription默认为true（由用户决定是否需要处方）
            NeedsPrescription = true;
        }
        catch (Exception ex) { Logger.LogError(ex, "加载医案数据失败"); await ShowErrorMessageAsync(ClientErrorMessageMapper.GetSafeOperationFailureMessage("加载医案数据", ex)); }
        finally { SetBusy(false); }
    }

    /// <summary>
    /// Clinical模式下恢复Draft状态医案为Active
    /// OpenSpec: refactor-medicalcase-workspace Phase 5 - 从待诊队列恢复挂起医案
    /// </summary>
    private async Task ResumeSuspendedIfNeededAsync()
    {
        var medicalCase = _medicalCaseService.CachedMedicalCase;
        if (medicalCase == null) return;

        // 仅在Clinical模式且医案状态为Suspended时恢复
        if (WorkspaceMode == WorkspaceMode.Clinical &&
            medicalCase.CaseStatus == Shared.Models.Enums.MedicalCaseStatus.Suspended)
        {
            Logger.LogInformation("[CMD] ResumeSuspended → MedicalCaseId={MedicalCaseId}", MedicalCaseId);

            var result = await _medicalCaseService.ResumeSuspendedAsync(MedicalCaseId);
            if (result.success)
            {
                // 更新缓存的医案状态
                medicalCase.CaseStatus = Shared.Models.Enums.MedicalCaseStatus.Active;
                Logger.LogInformation("[CMD] ResumeSuspended completed → Status=Active");
            }
            else
            {
                Logger.LogWarning("[CMD] ResumeSuspended failed → {ErrorMessage}", result.errorMessage);
            }
        }
    }


    /// <summary>
    /// 处理读卡成功后的患者就绪事件
    /// OpenSpec: integrate-cardreader-module - 读卡器集成回调处理
    /// </summary>
    /// <param name="patient">从读卡获取的患者信息</param>
    /// <param name="cardResult">读卡原始结果</param>
    private async Task HandlePatientReadyForMedicalCaseAsync(PatientFromCardResult patient, CardReadResult cardResult)
    {
        try
        {
            SetBusy(true, "正在准备就诊...");
            Logger.LogInformation("[CardReader] 患者就绪：{PatientId}, {Name}, 新建={IsNew}",
                patient.PatientId, patient.Name, patient.IsNewlyCreated);

            // 获取患者详情 - OpenSpec: integrate-cardreader-module
            var patientDetail = await _patientCardReaderIntegration.GetPatientDetailByIdAsync(patient.PatientId);
            if (patientDetail == null)
            {
                Logger.LogWarning("[CardReader] 获取患者详情失败：{PatientId}", patient.PatientId);
                await ShowErrorMessageAsync("获取患者信息失败，请重试");
                return;
            }

            // 检查患者是否有未完成的医案 - OpenSpec: integrate-cardreader-module
            var currentDoctorId = SessionManager?.CurrentUser?.Id ?? Guid.Empty;
            var existingCase = await _medicalCaseService.GetUnfinishedCaseByPatientIdAsync(patient.PatientId, currentDoctorId);
            if (existingCase != null)
            {
                // 有未完成的医案，直接加载
                Logger.LogInformation("[CardReader] 找到未完成医案：{MedicalCaseId}", existingCase.Id);
                await ShowSuccessMessageAsync($"找到未完成医案，正在加载...");

                var parameters = new Dictionary<string, object>
                {
                    { "MedicalCaseId", existingCase.Id },
                    { "CurrentPatient", patientDetail },
                    { MedicalCaseNavigationParameters.WorkspaceModeKey, WorkspaceMode.Clinical },
                    { MedicalCaseNavigationParameters.InitialEditStateKey, EditState.Editing }
                };
                _navigationCoordinator.NavigateTo(ViewNames.MedicalCaseWorkspace, parameters);
            }
            else
            {
                // 没有未完成的医案，创建新医案
                Logger.LogInformation("[CardReader] 创建新医案：{PatientId}", patient.PatientId);
                var createResult = await _medicalCaseService.CreateMedicalCaseAsync(patient.PatientId);

                if (!createResult.success)
                {
                    Logger.LogWarning("[CardReader] 创建医案失败：{Error}", createResult.errorMessage);
                    await ShowErrorMessageAsync("创建医案失败：" + createResult.errorMessage);
                    return;
                }

                Logger.LogInformation("[CardReader] 医案创建成功：{MedicalCaseId}", createResult.medicalCaseId);
                await ShowSuccessMessageAsync("医案创建成功，正在打开...");

                var parameters = new Dictionary<string, object>
                {
                    { "MedicalCaseId", createResult.medicalCaseId },
                    { "CurrentPatient", patientDetail },
                    { MedicalCaseNavigationParameters.WorkspaceModeKey, WorkspaceMode.Clinical },
                    { MedicalCaseNavigationParameters.InitialEditStateKey, EditState.Editing }
                };
                _navigationCoordinator.NavigateTo(ViewNames.MedicalCaseWorkspace, parameters);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "[CardReader] 处理患者就绪事件失败");
            await ShowErrorMessageAsync("处理患者信息失败，请重试");
        }
        finally
        {
            SetBusy(false, null);
        }
    }

    private void InitializeChildViewModels()
    {
        if (_medicalCaseService.CachedConsultation != null)
        {
            var dto = _medicalCaseService.CachedConsultation;
            // Id/MedicalCaseId/PatientId/UserId 已从 ConsultationInputDto 移除 (服务端通过聚合根获取)
            Consultation.PatientName = dto.PatientName ?? string.Empty;
            Consultation.DoctorName = dto.DoctorName ?? string.Empty;
            Consultation.PresentIllness = dto.PresentIllness;
            Consultation.TongueDiagnosis = dto.TongueDiagnosis;
            Consultation.PulseDiagnosis = dto.PulseDiagnosis;
            Consultation.TcmDiagnosis = dto.TcmDiagnosis;
            Consultation.CreatedAt = dto.CreatedAt;
            Consultation.UpdatedAt = dto.UpdatedAt;
        }
        else
        {
            // 新建医案时初始化基础信息
            Consultation.PatientName = CurrentPatient?.Name ?? string.Empty;
        }

        // 订阅Consultation属性变更以实时更新CanComplete
        // OpenSpec: consolidate-panel-viewmodels - Consultation已是ConsultationItem，直接订阅PropertyChanged
        Consultation.PropertyChanged += OnChildViewModelPropertyChanged;

        var cachedPrescription = _medicalCaseService.CachedPrescription;
        if (cachedPrescription != null)
        {
            // 使用FromDto静态方法加载数据（保持原有Prescription实例，手动复制属性）
            Prescription.Id = cachedPrescription.Id;
            Prescription.PrescriptionNumber = cachedPrescription.PrescriptionNumber;
            Prescription.MedicalCaseId = cachedPrescription.MedicalCaseId;
            Prescription.DosageCount = cachedPrescription.DosageCount;
            Prescription.Usage = cachedPrescription.Usage ?? "水煎服，一日一剂，分早晚两次温服";
            Prescription.Advice = cachedPrescription.Advice;
            Prescription.ReferencedFormulas = cachedPrescription.ReferencedFormulas;
            Prescription.Remark = cachedPrescription.Remark;
            Prescription.Discount = cachedPrescription.Discount;
            Prescription.SingleDosePrice = cachedPrescription.SingleDosePrice;
            Prescription.TotalWeight = cachedPrescription.TotalWeight;
            Prescription.Status = cachedPrescription.Status;
            Prescription.CreatedAt = cachedPrescription.CreatedAt;
            Prescription.UpdatedAt = cachedPrescription.UpdatedAt;
            Prescription.DuplicateWarning = cachedPrescription.DuplicateWarning;
            Prescription.MissingDrugWarning = cachedPrescription.MissingDrugWarning;

            // 加载药材列表
            Prescription.Items.Clear();
            if (cachedPrescription.Items != null)
            {
                foreach (var herbDto in cachedPrescription.Items)
                {
                    // OpenSpec: unify-control-data-binding - 类型已统一，直接添加
                    Prescription.Items.Add(herbDto);
                }
            }
        }
        else
        {
            // 新建处方时初始化基础信息
            Prescription.MedicalCaseId = MedicalCaseId;
        }

        // 订阅Prescription属性变更以实时更新CanComplete
        Prescription.PropertyChanged += OnChildViewModelPropertyChanged;

        // OpenSpec: consolidate-panel-viewmodels - PrescriptionPanelViewModel已删除，使用PrescriptionItem
        // OpenSpec: simplify-workspace-architecture - 使用内联的HandleLeaveRequestAsync方法
        _activeConsultationService.Register(MedicalCaseId, HandleLeaveRequestAsync);

        // OpenSpec: print-prescription-slip - 如果已有处方数据，启用打印按钮
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
        OnPropertyChanged(nameof(IsEditing)); OnPropertyChanged(nameof(IsReadOnly));
        OnPropertyChanged(nameof(ShowEditButton)); OnPropertyChanged(nameof(ShowEditButtonTopRight));
        OnPropertyChanged(nameof(ShowSaveButton)); OnPropertyChanged(nameof(ShowSuspendButton));
        OnPropertyChanged(nameof(ShowCompleteButton)); OnPropertyChanged(nameof(HeaderTitle));
        OnPropertyChanged(nameof(BackButtonText));
        SaveCommand?.RaiseCanExecuteChanged(); EnterEditModeCommand?.RaiseCanExecuteChanged();
    }

    private void OnConsultationCompleted(CaseConsultationCompletedPayload payload)
    {
        // OpenSpec: optimize-medicalcase-navigation - 使用ViewModel自己的NeedsPrescription（CheckBox绑定）
        IsPrescriptionEnabled = NeedsPrescription;
        // CanComplete由UpdateCanComplete()实时计算，IsPrescriptionEnabled变更时自动触发
    }

    private void OnPrescriptionCompleted(CasePrescriptionCompletedPayload payload)
    {
        CanPrintPrescription = true;
        UpdateCanComplete();
    }

    // OpenSpec: simplify-medicalcase-module - OnLifecycleActionCompleted已移除，服务方法直接返回结果

    #endregion

    #region 状态更新

    /// <summary>
    /// 更新完成医案按钮可用性
    /// OpenSpec: optimize-medicalcase-navigation - 基于诊断和处方状态的条件逻辑
    /// 逻辑流程：
    /// 1. 诊断必填字段为空 -> 不可用
    /// 2. 需要处方=否 -> 可以完成
    /// 3. 需要处方=是 -> 药材数量>0才可完成
    /// </summary>
    private void UpdateCanComplete()
    {
        // OpenSpec: consolidate-panel-viewmodels - 使用Consultation.IsDiagnosisComplete替代ConsultationPanelViewModel.Validate()
        // 1. 诊断必填字段为空 -> 不可用
        if (!Consultation.IsDiagnosisComplete)
        {
            CanComplete = false;
            return;
        }

        // 2. 需要处方 = 否 -> 可以完成
        if (!IsPrescriptionEnabled)
        {
            CanComplete = true;
            return;
        }

        // 3. 需要处方 = 是 -> 判断药材数量 > 0
        // OpenSpec: consolidate-panel-viewmodels - 使用Prescription.ItemCount替代PrescriptionPanelViewModel.ItemCount
        CanComplete = Prescription.ItemCount > 0;
    }

    /// <summary>
    /// 子ViewModel属性变更处理 - 用于实时更新CanComplete
    /// OpenSpec: optimize-medicalcase-navigation - 监听NeedsPrescription同步IsPrescriptionEnabled
    /// </summary>
    private void OnChildViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        // 监听影响CanComplete的属性变化
        // OpenSpec: refactor-diagnosis-fields - 移除ChiefComplaint，仅监听TcmDiagnosis
        // OpenSpec: optimize-medicalcase-navigation - 添加NeedsPrescription监听
        switch (e.PropertyName)
        {
            case "TcmDiagnosis":
            case "ItemCount":
                UpdateCanComplete();
                break;
            case "NeedsPrescription":
                // OpenSpec: optimize-medicalcase-navigation - 同步CheckBox选择到IsPrescriptionEnabled
                IsPrescriptionEnabled = NeedsPrescription;
                break;
        }
    }

    #endregion

    #region IDisposable

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _activeConsultationService.Unregister();
            _editModeStateMachine.EditStateChanged -= OnEditStateChanged;
            EventAggregator.GetEvent<CaseEvents.ConsultationCompletedEvent>().Unsubscribe(OnConsultationCompleted);
            EventAggregator.GetEvent<CaseEvents.PrescriptionCompletedEvent>().Unsubscribe(OnPrescriptionCompleted);
            // OpenSpec: simplify-workspace-event-architecture (Phase 4) - PrescriptionSavedEvent已改用回调模式

            // OpenSpec: integrate-cardreader-module - 清理读卡器Handler
            _cardReaderHandler?.Dispose();
        }
        base.Dispose(disposing);
    }

    #endregion
}
