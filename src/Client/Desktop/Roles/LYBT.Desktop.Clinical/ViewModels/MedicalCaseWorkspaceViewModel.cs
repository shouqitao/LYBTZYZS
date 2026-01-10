using System.Collections.ObjectModel;
using System.Windows.Media;
using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Herbs.Models.Items;
using LYBT.Desktop.Infrastructure.Constants;
using LYBT.Desktop.Infrastructure.Events;
using LYBT.Desktop.MedicalCase.Events;
using LYBT.Desktop.MedicalCase.Interfaces;
using LYBT.Desktop.MedicalCase.Mappers;
using LYBT.Desktop.MedicalCase.Models;
using LYBT.Desktop.MedicalCase.Models.Items;
using LYBT.Desktop.MedicalCase.Services;
using LYBT.Desktop.MedicalCase.ViewModels.Components;
using LYBT.Desktop.Models.ViewModels.Base;
using LYBT.Shared.ExceptionHandling.Mappers;
using LYBT.Shared.Models.Contracts.Consultation;
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
/// </summary>
public class MedicalCaseWorkspaceViewModel : UnifiedViewModelBase
{
    #region 字段

    private readonly IRegionManager _regionManager;
    private readonly MedicalCaseService _dataManager;
    private readonly IMedicalCaseService _medicalCaseService;
    private readonly MedicalCaseDataLoader _dataLoader;
    // OpenSpec: consolidate-panel-viewmodels - ConsultationPanelViewModel和PrescriptionPanelViewModel已删除，使用Consultation/Prescription属性替代
    private readonly IActiveConsultationService _activeConsultationService;
    private readonly IDialogService? _dialogService;
    private readonly MedicalCaseWorkspaceCoordinator _coordinator;
    private readonly MedicalCaseNavigationHandler _navigationHandler;
    private readonly MedicalCaseEditModeStateMachine _editModeStateMachine;
    // OpenSpec: create-printing-module - IPrescriptionPrintService已移除，打印功能通过PrescriptionPrintHandler使用
    private readonly IPendingQueueManager _pendingQueueManager;
    private readonly PrescriptionPrintHandler _printHandler;
    private readonly WorkspacePendingQueueHandler _pendingQueueHandler;
    private readonly PrescriptionImportHandler _importHandler;

    // OpenSpec: slim-medicalcase-workspace-viewmodel - 状态显示组件
    private readonly WorkspaceStatusDisplay _statusDisplay = new();

    #endregion

    #region 属性

    private string _patientName = string.Empty;
    public string PatientName { get => _patientName; set => SetProperty(ref _patientName, value); }

    private string _patientInfo = string.Empty;
    public string PatientInfo { get => _patientInfo; set => SetProperty(ref _patientInfo, value); }

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
                RaisePropertyChanged(nameof(CurrentPatientGenderDisplay));
                RaisePropertyChanged(nameof(RegistrationTime));
                RaisePropertyChanged(nameof(CurrentPatientDisplayModel));
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

    /// <summary>
    /// 是否需要开处方（医案级别属性）
    /// OpenSpec: optimize-medicalcase-navigation - 移至医案ViewModel，用CheckBox控制
    /// </summary>
    private bool _needsPrescription = true;
    public bool NeedsPrescription
    {
        get => _needsPrescription;
        set
        {
            if (SetProperty(ref _needsPrescription, value))
            {
                RaisePropertyChanged(nameof(NoPrescription));
            }
        }
    }

    /// <summary>
    /// 不开处方（反向绑定，用于UI显示）
    /// </summary>
    public bool NoPrescription => !NeedsPrescription;

    #region 派生状态属性
    // OpenSpec: simplify-workspace-event-architecture (Phase 1.3)

    /// <summary>
    /// 诊断面板状态（基于Consultation.IsDiagnosisComplete推导）
    /// OpenSpec: consolidate-panel-viewmodels - 不再依赖ConsultationPanelViewModel
    /// </summary>
    public LYBT.Desktop.MedicalCase.Enums.PanelStatus ConsultationStatus =>
        Consultation.IsDiagnosisComplete ? LYBT.Desktop.MedicalCase.Enums.PanelStatus.Completed : LYBT.Desktop.MedicalCase.Enums.PanelStatus.InProgress;

    /// <summary>
    /// 处方面板状态（基于Prescription.IsValid推导）
    /// OpenSpec: consolidate-panel-viewmodels - 不再依赖PrescriptionPanelViewModel
    /// </summary>
    public LYBT.Desktop.MedicalCase.Enums.PanelStatus PrescriptionStatus =>
        Prescription.IsValid ? LYBT.Desktop.MedicalCase.Enums.PanelStatus.Completed :
        (Prescription.ItemCount > 0 ? LYBT.Desktop.MedicalCase.Enums.PanelStatus.InProgress : LYBT.Desktop.MedicalCase.Enums.PanelStatus.NotStarted);

    #endregion

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
    /// <summary>
    /// 是否有未保存的更改 - 重写基类以提供处方更改状态
    /// OpenSpec: unify-navigation-architecture - IConfirmNavigationRequest支持
    /// </summary>
    protected override bool HasUnsavedChanges => _hasUnsavedPrescriptionChanges;
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

    private bool _isRefreshingPendingQueue;
    /// <summary>
    /// 是否正在刷新待诊队列
    /// </summary>
    public bool IsRefreshingPendingQueue
    {
        get => _isRefreshingPendingQueue;
        set => SetProperty(ref _isRefreshingPendingQueue, value);
    }

    /// <summary>
    /// 待诊队列是否为空
    /// </summary>
    public bool HasNoPendingCases => PendingQueue == null || PendingQueue.Count == 0;

    #endregion

    #region 命令

    public DelegateCommand BackCommand { get; }
    public DelegateCommand BackToPatientSelectionCommand => BackCommand;
    /// <summary>
    /// Clinical模式: 暂存医案命令 - 保存为Draft状态，留在当前界面
    /// </summary>
    public DelegateCommand SaveDraftCommand { get; }
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

    #endregion

    #region 构造函数

    // OpenSpec: consolidate-panel-viewmodels - 移除ConsultationPanelViewModel和PrescriptionPanelViewModel参数
    // OpenSpec: simplify-medicalcase-module - MedicalCaseLifecycleHandler已合并到IMedicalCaseService
    public MedicalCaseWorkspaceViewModel(
        MedicalCaseService dataManager, IMedicalCaseService medicalCaseService,
        MedicalCaseDataLoader dataLoader, MedicalCaseWorkspaceCoordinator coordinator,
        MedicalCaseNavigationHandler navigationHandler, MedicalCaseEditModeStateMachine editModeStateMachine,
        IRegionManager regionManager, IEventAggregator eventAggregator, ILoggerFactory loggerFactory,
        IActiveConsultationService activeConsultationService,
        PrescriptionImportHandler importHandler,
        ISessionManager? sessionManager = null,
        ICommonDialogService? commonDialogService = null, IDialogService? dialogService = null,
        // OpenSpec: create-printing-module - IPrescriptionPrintService参数已移除，打印功能通过PrescriptionPrintHandler使用
        IPendingQueueManager? pendingQueueManager = null,
        PrescriptionPrintHandler? printHandler = null)
        : base(eventAggregator, loggerFactory, regionManager, sessionManager, null, commonDialogService)
    {
        _dataManager = dataManager ?? throw new ArgumentNullException(nameof(dataManager));
        _regionManager = regionManager ?? throw new ArgumentNullException(nameof(regionManager));
        _medicalCaseService = medicalCaseService ?? throw new ArgumentNullException(nameof(medicalCaseService));
        _dataLoader = dataLoader ?? throw new ArgumentNullException(nameof(dataLoader));
        _coordinator = coordinator ?? throw new ArgumentNullException(nameof(coordinator));
        _navigationHandler = navigationHandler ?? throw new ArgumentNullException(nameof(navigationHandler));
        _activeConsultationService = activeConsultationService ?? throw new ArgumentNullException(nameof(activeConsultationService));
        _importHandler = importHandler ?? throw new ArgumentNullException(nameof(importHandler));
        _dialogService = dialogService;
        _editModeStateMachine = editModeStateMachine ?? throw new ArgumentNullException(nameof(editModeStateMachine));
        // OpenSpec: create-printing-module - _prescriptionPrintService字段已移除
        _pendingQueueManager = pendingQueueManager ?? throw new ArgumentNullException(nameof(pendingQueueManager));
        _printHandler = printHandler ?? throw new ArgumentNullException(nameof(printHandler));

        // OpenSpec: refactor-desktop-comprehensive Phase 3 - 初始化待诊队列处理器
        // OpenSpec: simplify-medicalcase-module - 使用IMedicalCaseService替代MedicalCaseLifecycleHandler
        _pendingQueueHandler = new WorkspacePendingQueueHandler(
            _pendingQueueManager,
            _medicalCaseService,
            _regionManager,
            commonDialogService,
            loggerFactory,
            () => MedicalCaseId,
            () => IsReadOnly,
            GetPatientDetailAsync,
            (busy, message) => SetIsBusy(busy, message),
            ShowErrorMessageAsync,
            ShowSuccessMessageAsync);

        // 订阅事件
        _editModeStateMachine.EditStateChanged += OnEditStateChanged;
        // OpenSpec: simplify-medicalcase-module - ActionCompleted事件已移除（服务方法直接返回结果）
        _dataLoader.DataLoaded += OnDataLoaded;

        // 配置导航处理器回调
        _navigationHandler.SaveDraftCallback = SaveDraftOnlyAsync;
        _navigationHandler.CancelCaseCallback = CancelCaseOnlyAsync;
        _navigationHandler.CheckAndGetAuditReasonCallback = CheckAndGetAuditReasonAsync;
        _navigationHandler.SetEditReasonCallback = reason => _editModeStateMachine.EditReason = reason;
        _navigationHandler.SetIsEditingCallback = value => { if (value) _editModeStateMachine.EnterEditMode(); else _editModeStateMachine.EnterReadOnlyMode(); };

        // 初始化命令
        BackCommand = new DelegateCommand(async () => await ExecuteBackAsync());
        // Clinical模式: 暂存医案
        SaveDraftCommand = new DelegateCommand(ExecuteSaveDraft, () => WorkspaceMode == WorkspaceMode.Clinical && _editModeStateMachine.IsEditing);
        // Management模式: 保存修改
        SaveChangesCommand = new DelegateCommand(ExecuteSaveChanges, () => WorkspaceMode == WorkspaceMode.Management && _editModeStateMachine.IsEditing);
        PrintPrescriptionCommand = new DelegateCommand(ExecutePrintPrescription, () => CanPrintPrescription).ObservesProperty(() => CanPrintPrescription);
        CompleteMedicalCaseCommand = new DelegateCommand(ExecuteCompleteMedicalCase, () => CanComplete).ObservesProperty(() => CanComplete);
        SaveCommand = new DelegateCommand(ExecuteSave, () => _editModeStateMachine.IsEditing);
        EnterEditModeCommand = new DelegateCommand(ExecuteEnterEditMode, () => _editModeStateMachine.CanEnterEditMode);
        ViewPatientHistoryCommand = new DelegateCommand(ExecuteViewPatientHistory, () => CurrentPatient != null).ObservesProperty(() => CurrentPatient);

        // OpenSpec: refactor-medicalcase-workspace V2 TASK-V2-010 - 待诊队列命令初始化
        RefreshQueueCommand = new DelegateCommand(async () => await ExecuteRefreshQueueAsync());
        SelectPendingCaseCommand = new DelegateCommand<PendingMedicalCaseDto>(async p => await ExecuteSelectPendingCaseAsync(p));

        // OpenSpec: consolidate-panel-viewmodels - 处方编辑命令初始化
        OpenFormulaImportDialogCommand = new DelegateCommand(ExecuteOpenFormulaImportDialog);
        OpenHistoryCopyDialogCommand = new DelegateCommand(ExecuteOpenHistoryCopyDialog);
        ClearHerbItemsCommand = new DelegateCommand(ExecuteClearHerbItems);

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

    // OpenSpec: consolidate-panel-viewmodels - 使用Item.ToInputDto()替代PanelViewModel
    // 创建适配器实现IDataProvider接口
    private IDataProvider? GetConsultationProvider() => new ConsultationDataProviderAdapter(Consultation);
    private IDataProvider? GetPrescriptionProvider() => new PrescriptionDataProviderAdapter(Prescription);
    // OpenSpec: consolidate-panel-viewmodels - 使用Item属性验证
    private IValidatable? GetConsultationValidator() => new ConsultationValidatorAdapter(Consultation);
    private IValidatable? GetPrescriptionValidator() => new PrescriptionValidatorAdapter(Prescription, IsPrescriptionEnabled);

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

    /// <summary>
    /// Clinical模式: 暂存医案 - 保存为Draft状态，留在当前界面继续编辑
    /// </summary>
    private async void ExecuteSaveDraft()
    {
        try
        {
            SetIsBusy(true, "正在暂存...");
            var result = await _coordinator.SaveDraftAsync(MedicalCaseId, GetConsultationProvider(), GetPrescriptionProvider(), Remark);
            if (result.IsSuccess)
            {
                _editModeStateMachine.EnterReadOnlyMode();
                await ShowSuccessMessageAsync("医案已暂存，可随时点击'修改医案'继续编辑");
                // 刷新待诊队列显示最新状态
                await _pendingQueueHandler.LoadPendingQueueAsync();
            }
            else
            {
                await ShowErrorMessageAsync(result.ErrorMessage ?? "暂存失败");
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "暂存医案失败");
            await ShowErrorMessageAsync(ClientErrorMessageMapper.GetSafeOperationFailureMessage("暂存", ex));
        }
        finally { SetIsBusy(false); }
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

            SetIsBusy(true, "正在保存...");
            var result = await _coordinator.SaveDraftAsync(MedicalCaseId, GetConsultationProvider(), GetPrescriptionProvider(), Remark);
            if (result.IsSuccess)
            {
                _editModeStateMachine.EnterReadOnlyMode();
                await ShowSuccessMessageAsync("保存成功");
            }
            else
            {
                await ShowErrorMessageAsync(result.ErrorMessage ?? "保存失败");
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "保存医案失败");
            await ShowErrorMessageAsync(ClientErrorMessageMapper.GetSafeOperationFailureMessage("保存", ex));
        }
        finally { SetIsBusy(false); }
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
        _regionManager.RequestNavigate(RegionNames.ContentRegion, ViewNames.PatientManagement);
    }

    #region 待诊队列操作 - OpenSpec: refactor-medicalcase-workspace V2 TASK-V2-010

    /// <summary>
    /// 刷新待诊队列
    /// </summary>
    private async Task ExecuteRefreshQueueAsync()
    {
        try
        {
            IsRefreshingPendingQueue = true;
            await _pendingQueueHandler.LoadPendingQueueAsync();
            RaisePropertyChanged(nameof(PendingQueue));
            RaisePropertyChanged(nameof(HasNoPendingCases));
        }
        finally
        {
            IsRefreshingPendingQueue = false;
        }
    }

    /// <summary>
    /// 选择待诊队列中的患者，切换到该患者的医案
    /// OpenSpec: refactor-medicalcase-workspace V2 TASK-V2-011 - 切换时显示选项对话框
    /// </summary>
    private async Task ExecuteSelectPendingCaseAsync(PendingMedicalCaseDto? pendingCase)
    {
        if (pendingCase == null) return;

        // 委托给Handler处理患者切换逻辑，传入暂存回调
        await _pendingQueueHandler.SelectPendingCaseAsync(pendingCase, SaveDraftOnlyAsync);
    }


    /// <summary>
    /// 处理挂起状态患者 - 显示四选项弹窗
    /// OpenSpec: optimize-medicalcase-navigation
    /// 选项：继续看诊 / 关闭挂起+新建 / 仅关闭 / 取消
    /// </summary>
    private async Task HandleSuspendedCaseAsync(PendingMedicalCaseDto pendingCase)
    {
        if (CommonDialogService == null)
        {
            Logger.LogWarning("CommonDialogService为空，无法显示四选项弹窗");
            return;
        }

        Logger.LogInformation("显示四选项弹窗，患者：{PatientName}，挂起医案：{MedicalCaseId}",
            pendingCase.PatientName, pendingCase.MedicalCaseId);

        var choice = await CommonDialogService.ShowUnfinishedCaseDialogAsync(pendingCase.PatientName ?? "未知患者");

        switch (choice)
        {
            case UnfinishedCaseChoice.Continue:
                // 继续看诊 - 导航到挂起的医案
                Logger.LogInformation("用户选择继续看诊，导航到挂起医案");
                await NavigateToExistingMedicalCaseAsync(pendingCase);
                break;

            case UnfinishedCaseChoice.CloseAndCreate:
                // 关闭挂起+新建 - 取消原医案后创建新医案
                Logger.LogInformation("用户选择关闭挂起+新建医案");
                SetIsBusy(true, "正在关闭旧医案...");
                if (pendingCase.MedicalCaseId.HasValue)
                {
                    var cancelResult = await _medicalCaseService.CancelMedicalCaseAsync(pendingCase.MedicalCaseId.Value);
                    if (!cancelResult.success)
                    {
                        Logger.LogWarning("取消挂起医案失败：{Error}", cancelResult.errorMessage);
                        await ShowErrorMessageAsync("关闭旧医案失败：" + cancelResult.errorMessage);
                        SetIsBusy(false);
                        return;
                    }
                }
                await NavigateToNewMedicalCaseAsync(pendingCase);
                break;

            case UnfinishedCaseChoice.CloseOnly:
                // 仅关闭 - 取消原医案，留在当前界面
                Logger.LogInformation("用户选择仅关闭挂起医案");
                SetIsBusy(true, "正在关闭挂起医案...");
                if (pendingCase.MedicalCaseId.HasValue)
                {
                    var cancelResult = await _medicalCaseService.CancelMedicalCaseAsync(pendingCase.MedicalCaseId.Value);
                    if (!cancelResult.success)
                    {
                        Logger.LogWarning("取消挂起医案失败：{Error}", cancelResult.errorMessage);
                        await ShowErrorMessageAsync("关闭医案失败：" + cancelResult.errorMessage);
                    }
                    else
                    {
                        await ShowSuccessMessageAsync("挂起医案已关闭");
                        // 刷新待诊队列（通过Handler同步全局状态）
                        await _pendingQueueHandler.LoadPendingQueueAsync();
                    }
                }
                SetIsBusy(false);
                break;

            case UnfinishedCaseChoice.Cancel:
            default:
                // 取消 - 不做任何操作
                Logger.LogInformation("用户取消操作，留在当前界面");
                break;
        }
    }

    /// <summary>
    /// 导航到新医案 - 为患者创建新医案并导航
    /// OpenSpec: optimize-medicalcase-navigation
    /// </summary>
    private async Task NavigateToNewMedicalCaseAsync(PendingMedicalCaseDto pendingCase)
    {
        try
        {
            SetIsBusy(true, "正在创建新医案...");
            Logger.LogInformation("为患者创建新医案：{PatientName}", pendingCase.PatientName);

            // 创建新医案
            var createResult = await _medicalCaseService.CreateMedicalCaseAsync(pendingCase.PatientId);
            if (!createResult.success)
            {
                Logger.LogWarning("创建医案失败：{Error}", createResult.errorMessage);
                await ShowErrorMessageAsync("创建医案失败：" + createResult.errorMessage);
                return;
            }

            // 导航到医案工作台
            var parameters = new NavigationParameters
            {
                { "MedicalCaseId", createResult.medicalCaseId },
                { "CurrentPatient", await GetPatientDetailAsync(pendingCase.PatientId) },
                { MedicalCaseNavigationParameters.WorkspaceModeKey, WorkspaceMode.Clinical },
                { MedicalCaseNavigationParameters.InitialEditStateKey, EditState.Editing }
            };

            _regionManager.RequestNavigate(RegionNames.ContentRegion, ViewNames.MedicalCaseWorkspace, parameters);
            Logger.LogInformation("已导航到新医案：{MedicalCaseId}", createResult.medicalCaseId);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "创建新医案并导航失败");
            await ShowErrorMessageAsync("创建医案失败，请重试");
        }
        finally
        {
            SetIsBusy(false);
        }
    }

    /// <summary>
    /// 导航到已存在的医案
    /// OpenSpec: optimize-medicalcase-navigation
    /// </summary>
    private async Task NavigateToExistingMedicalCaseAsync(PendingMedicalCaseDto pendingCase)
    {
        try
        {
            SetIsBusy(true, "正在加载医案...");

            if (!pendingCase.MedicalCaseId.HasValue)
            {
                Logger.LogWarning("挂起医案ID为空，无法导航");
                await ShowErrorMessageAsync("医案数据异常，请刷新后重试");
                return;
            }

            // 导航到医案工作台
            var parameters = new NavigationParameters
            {
                { "MedicalCaseId", pendingCase.MedicalCaseId.Value },
                { "CurrentPatient", await GetPatientDetailAsync(pendingCase.PatientId) },
                { MedicalCaseNavigationParameters.WorkspaceModeKey, WorkspaceMode.Clinical },
                { MedicalCaseNavigationParameters.InitialEditStateKey, EditState.Editing }
            };

            _regionManager.RequestNavigate(RegionNames.ContentRegion, ViewNames.MedicalCaseWorkspace, parameters);
            Logger.LogInformation("已导航到挂起医案：{MedicalCaseId}", pendingCase.MedicalCaseId.Value);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "导航到已存在医案失败");
            await ShowErrorMessageAsync("加载医案失败，请重试");
        }
        finally
        {
            SetIsBusy(false);
        }
    }

    /// <summary>
    /// 获取患者详情
    /// </summary>
    private Task<PatientDetailDto?> GetPatientDetailAsync(Guid patientId)
    {
        try
        {
            // 如果当前患者就是目标患者，直接返回
            if (CurrentPatient?.Id == patientId)
            {
                return Task.FromResult<PatientDetailDto?>(CurrentPatient);
            }

            // 否则通过服务获取患者详情
            // 注：这里假设有一个PatientService可以获取患者详情
            // 如果没有，可以通过PendingQueueManager或其他方式获取
            Logger.LogDebug("需要获取患者详情：{PatientId}", patientId);
            return Task.FromResult<PatientDetailDto?>(null); // 暂时返回null，OnNavigatedTo会处理
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "获取患者详情失败：{PatientId}", patientId);
            return Task.FromResult<PatientDetailDto?>(null);
        }
    }

    #endregion

    #region 处方编辑命令 - OpenSpec: consolidate-panel-viewmodels

    /// <summary>
    /// 打开验方导入对话框
    /// </summary>
    private void ExecuteOpenFormulaImportDialog()
    {
        if (_dialogService == null)
        {
            Logger.LogWarning("DialogService为空，无法打开验方导入对话框");
            return;
        }

        _dialogService.ShowDialog("FormulaImportDialog", null, async r =>
        {
            if (r.Result == ButtonResult.OK)
                await HandleFormulaImportResultAsync(r.Parameters);
        });
    }

    /// <summary>
    /// 打开历史处方复制对话框
    /// </summary>
    private void ExecuteOpenHistoryCopyDialog()
    {
        if (_dialogService == null)
        {
            Logger.LogWarning("DialogService为空，无法打开历史复制对话框");
            return;
        }

        var parameters = new DialogParameters
        {
            { "PatientId", CurrentPatient?.Id ?? Guid.Empty },
            { "PatientName", CurrentPatient?.Name ?? string.Empty }
        };

        _dialogService.ShowDialog("HistoryCopyDialog", parameters, async r =>
        {
            if (r.Result == ButtonResult.OK)
                await HandleHistoryCopyResultAsync(r.Parameters);
        });
    }

    /// <summary>
    /// 清空药材列表
    /// </summary>
    private async void ExecuteClearHerbItems()
    {
        var validItemCount = Prescription.Items.Count(h => h.IsValid);
        if (validItemCount == 0)
        {
            await ShowSuccessMessageAsync("当前没有可清空的药材");
            return;
        }

        if (!await ShowConfirmationAsync($"确定要清空当前所有药材（共{validItemCount}项）吗？", "清空药材"))
            return;

        Prescription.Items.Clear();
        Logger.LogInformation("已清空处方药材，共{Count}项", validItemCount);
        await ShowSuccessMessageAsync($"已清空{validItemCount}项药材");
    }

    /// <summary>
    /// 处理验方导入结果
    /// </summary>
    private async Task HandleFormulaImportResultAsync(IDialogParameters parameters)
    {
        try
        {
            SetIsBusy(true, "正在导入验方...");

            if (!parameters.TryGetValue<FormulaDetailDto>("SelectedFormula", out var formula) || formula == null)
                return;

            if (!parameters.TryGetValue<List<FormulaHerbItemDto>>("SelectedHerbs", out var herbs) || herbs?.Any() != true)
            {
                await ShowErrorMessageAsync("验方无药材信息");
                return;
            }

            // 转换为HerbItemDto并添加到Prescription.Items
            var herbItems = _importHandler.ToHerbItemDtos(formula, herbs);
            if (!herbItems.Any())
            {
                await ShowErrorMessageAsync("验方无有效药材");
                return;
            }

            foreach (var item in herbItems)
            {
                Prescription.Items.Add(item);
            }

            // 记录引用的验方名称
            if (!string.IsNullOrEmpty(formula.Name))
            {
                if (string.IsNullOrEmpty(Prescription.ReferencedFormulas))
                    Prescription.ReferencedFormulas = formula.Name;
                else if (!Prescription.ReferencedFormulas.Contains(formula.Name))
                    Prescription.ReferencedFormulas = $"{Prescription.ReferencedFormulas}, {formula.Name}";
            }

            await ShowSuccessMessageAsync($"已导入验方「{formula.Name}」，共 {herbItems.Count} 味药材");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "处理验方导入结果异常");
            await ShowErrorMessageAsync(ClientErrorMessageMapper.GetSafeOperationFailureMessage("导入", ex));
        }
        finally
        {
            SetIsBusy(false);
        }
    }

    /// <summary>
    /// 处理历史处方复制结果
    /// </summary>
    private async Task HandleHistoryCopyResultAsync(IDialogParameters parameters)
    {
        try
        {
            SetIsBusy(true, "正在复制处方...");

            if (!parameters.TryGetValue<List<PrescriptionItemDto>>("SelectedItems", out var items) || items?.Any() != true)
            {
                await ShowErrorMessageAsync("历史处方无药材记录");
                return;
            }

            // 转换为HerbItemDto并添加到Prescription.Items
            var herbItems = _importHandler.ToHerbItemDtos(items);
            if (!herbItems.Any())
            {
                await ShowErrorMessageAsync("历史处方无有效药材");
                return;
            }

            foreach (var item in herbItems)
            {
                Prescription.Items.Add(item);
            }

            await ShowSuccessMessageAsync($"已复制 {herbItems.Count} 味药材");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "处理历史复制结果异常");
            await ShowErrorMessageAsync(ClientErrorMessageMapper.GetSafeOperationFailureMessage("复制", ex));
        }
        finally
        {
            SetIsBusy(false);
        }
    }

    #endregion

    /// <summary>
    /// 执行打印处方笺
    /// OpenSpec: print-prescription-slip
    /// </summary>
    private async void ExecutePrintPrescription()
    {
        try
        {
            SetIsBusy(true, "正在准备预览...");

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
            SetIsBusy(false);
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
            SetIsBusy(true, "正在完成医案...");
            // OpenSpec: refactor-medicalcase-aggregate-crud (Phase 4.1) - 使用聚合完成
            var result = await _coordinator.CompleteAsync(
                MedicalCaseId, GetConsultationProvider(), GetPrescriptionProvider(),
                GetConsultationValidator(), GetPrescriptionValidator(), Remark, IsPrescriptionEnabled);
            if (result.IsSuccess)
            {
                await ShowSuccessMessageAsync("医案已完成，请从待诊列表选择下一位患者");
                // 进入只读模式
                _editModeStateMachine.EnterReadOnlyMode();
                // 刷新待诊队列，当前患者应该从队列中消失
                await _pendingQueueHandler.LoadPendingQueueAsync();
                // 更新按钮状态
                RaisePropertyChanged(nameof(ShowCompleteButton));
                RaisePropertyChanged(nameof(ShowDraftButton));
                RaisePropertyChanged(nameof(CanComplete));
            }
            else
            {
                await ShowErrorMessageAsync(result.ErrorMessage ?? "完成失败");
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "完成医案失败");
            await ShowErrorMessageAsync(ClientErrorMessageMapper.GetSafeOperationFailureMessage("完成医案", ex));
        }
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

        // OpenSpec: refactor-medicalcase-workspace Phase 5 - 从待诊队列恢复挂起医案
        // Clinical模式下，如果加载的是Draft状态医案，自动恢复为Active
        await ResumeDraftIfNeededAsync();

        InitializeChildViewModels();
        await DetermineEditModeAsync(initialEditState, isHistoricalEdit);

        // 加载待诊队列 - OpenSpec: refactor-desktop-comprehensive Phase 3
        _ = _pendingQueueHandler.LoadPendingQueueAsync();
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
                var result = await _medicalCaseService.CreateMedicalCaseAsync(CurrentPatient.Id);
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
            // 根据是否有处方来决定是否启用处方面板，而不是根据是否有诊断
            if (hasPrescription) IsPrescriptionEnabled = true;
            Remark = result.detail?.Remark ?? string.Empty;
            // NeedsPrescription默认为true（由用户决定是否需要处方）
            NeedsPrescription = true;
        }
        catch (Exception ex) { Logger.LogError(ex, "加载医案数据失败"); await ShowErrorMessageAsync(ClientErrorMessageMapper.GetSafeOperationFailureMessage("加载医案数据", ex)); }
        finally { SetIsBusy(false); }
    }

    /// <summary>
    /// Clinical模式下恢复Draft状态医案为Active
    /// OpenSpec: refactor-medicalcase-workspace Phase 5 - 从待诊队列恢复挂起医案
    /// </summary>
    private async Task ResumeDraftIfNeededAsync()
    {
        var medicalCase = _dataLoader.CachedMedicalCase;
        if (medicalCase == null) return;

        // 仅在Clinical模式且医案状态为Draft时恢复
        if (WorkspaceMode == WorkspaceMode.Clinical &&
            medicalCase.CaseStatus == Shared.Models.Enums.MedicalCaseStatus.Draft)
        {
            Logger.LogInformation("[CMD] ResumeDraft → MedicalCaseId={MedicalCaseId}", MedicalCaseId);

            var result = await _medicalCaseService.ResumeDraftAsync(MedicalCaseId);
            if (result.success)
            {
                // 更新缓存的医案状态
                medicalCase.CaseStatus = Shared.Models.Enums.MedicalCaseStatus.Active;
                Logger.LogInformation("[CMD] ResumeDraft completed → Status=Active");
            }
            else
            {
                Logger.LogWarning("[CMD] ResumeDraft failed → {ErrorMessage}", result.errorMessage);
            }
        }
    }

    private void InitializeChildViewModels()
    {
        // OpenSpec: consolidate-panel-viewmodels - 初始化ConsultationItem
        if (_dataLoader.CachedConsultation != null)
        {
            var dto = _dataLoader.CachedConsultation;
            Consultation.Id = dto.Id;
            Consultation.MedicalCaseId = dto.MedicalCaseId;
            Consultation.PatientId = dto.PatientId;
            Consultation.UserId = dto.UserId;
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
            Consultation.MedicalCaseId = MedicalCaseId;
            Consultation.PatientId = CurrentPatient?.Id ?? Guid.Empty;
            Consultation.PatientName = CurrentPatient?.Name ?? string.Empty;
        }

        // 订阅Consultation属性变更以实时更新CanComplete
        // OpenSpec: consolidate-panel-viewmodels - Consultation已是ConsultationItem，直接订阅PropertyChanged
        Consultation.PropertyChanged += OnChildViewModelPropertyChanged;

        // OpenSpec: consolidate-panel-viewmodels - 初始化PrescriptionItem
        var cachedPrescription = _dataLoader.CachedPrescription;
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
                    Prescription.Items.Add(HerbItemDto.FromPrescriptionItemDto(herbDto));
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
        _activeConsultationService.Register(MedicalCaseId, _navigationHandler.HandleLeaveRequestAsync);

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
        RaisePropertyChanged(nameof(IsEditing)); RaisePropertyChanged(nameof(IsReadOnly));
        RaisePropertyChanged(nameof(ShowEditButton)); RaisePropertyChanged(nameof(ShowEditButtonTopRight));
        RaisePropertyChanged(nameof(ShowSaveButton)); RaisePropertyChanged(nameof(ShowDraftButton));
        RaisePropertyChanged(nameof(ShowCompleteButton)); RaisePropertyChanged(nameof(HeaderTitle));
        RaisePropertyChanged(nameof(BackButtonText));
        SaveCommand?.RaiseCanExecuteChanged(); EnterEditModeCommand?.RaiseCanExecuteChanged();
    }

    private void OnConsultationCompleted(CaseConsultationCompletedPayload payload)
    {
        UpdateConsultationStatus(true);
        // OpenSpec: optimize-medicalcase-navigation - 使用ViewModel自己的NeedsPrescription（CheckBox绑定）
        IsPrescriptionEnabled = NeedsPrescription;
        if (NeedsPrescription) UpdatePrescriptionStatus(false, "待开方");
        else UpdatePrescriptionStatus(false, "无需开方");
        // CanComplete由UpdateCanComplete()实时计算，IsPrescriptionEnabled变更时自动触发
    }

    private void OnPrescriptionCompleted(CasePrescriptionCompletedPayload payload)
    {
        UpdatePrescriptionStatus(true);
        CanPrintPrescription = true;
        UpdateCanComplete();
    }

    private void OnPrescriptionSaved(PrescriptionSavedPayload payload)
    {
        if (payload.MedicalCaseId == MedicalCaseId) HasUnsavedPrescriptionChanges = false;
    }

    // OpenSpec: simplify-medicalcase-module - OnLifecycleActionCompleted已移除，服务方法直接返回结果

    private async void OnDataLoaded(object? sender, DataLoadedEventArgs e)
    {
        if (!e.Success) await ShowErrorMessageAsync(e.ErrorMessage ?? "数据加载失败");
    }

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

    private void UpdateConsultationStatus(bool isCompleted)
    {
        // OpenSpec: slim-medicalcase-workspace-viewmodel - 委托给状态显示组件
        _statusDisplay.UpdateConsultationStatus(isCompleted);

        // 同步到ViewModel属性（保持向后兼容）
        ConsultationStatusText = _statusDisplay.ConsultationStatusText;
        ConsultationStatusColor = _statusDisplay.ConsultationStatusColor;
    }

    private void UpdatePrescriptionStatus(bool isCompleted, string? customText = null)
    {
        // OpenSpec: slim-medicalcase-workspace-viewmodel - 委托给状态显示组件
        _statusDisplay.UpdatePrescriptionStatus(isCompleted, customText);

        // 同步到ViewModel属性（保持向后兼容）
        ShowPrescriptionStatus = _statusDisplay.ShowPrescriptionStatus;
        PrescriptionStatusText = _statusDisplay.PrescriptionStatusText;
        PrescriptionStatusBackground = _statusDisplay.PrescriptionStatusBackground;
        PrescriptionStatusSummary = _statusDisplay.PrescriptionStatusSummary;
        PrescriptionStatusSummaryColor = _statusDisplay.PrescriptionStatusSummaryColor;
    }

    #endregion

    #region IDisposable

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _activeConsultationService.Unregister();
            // OpenSpec: simplify-medicalcase-module - ActionCompleted事件已移除
            _dataLoader.DataLoaded -= OnDataLoaded;
            _editModeStateMachine.EditStateChanged -= OnEditStateChanged;
            EventAggregator.GetEvent<CaseEvents.ConsultationCompletedEvent>().Unsubscribe(OnConsultationCompleted);
            EventAggregator.GetEvent<CaseEvents.PrescriptionCompletedEvent>().Unsubscribe(OnPrescriptionCompleted);
            // OpenSpec: simplify-workspace-event-architecture (Phase 4) - PrescriptionSavedEvent已改用回调模式
        }
        base.Dispose(disposing);
    }

    #endregion

    #region 适配器类

    /// <summary>
    /// ConsultationItem的IDataProvider适配器
    /// OpenSpec: consolidate-panel-viewmodels - 将ConsultationItem包装为IDataProvider
    /// OpenSpec: adopt-mapperly-unified-mapping - 使用Mapper实例方法替代Item实例方法
    /// </summary>
    private sealed class ConsultationDataProviderAdapter : IDataProvider
    {
        private static readonly ConsultationMapper s_mapper = new();
        private readonly ConsultationItem _consultation;

        public ConsultationDataProviderAdapter(ConsultationItem consultation)
        {
            _consultation = consultation ?? throw new ArgumentNullException(nameof(consultation));
        }

        public ConsultationInputDto? GetConsultationData() => s_mapper.ToInputDto(_consultation);
        public PrescriptionInputDto? GetPrescriptionData() => null;
    }

    /// <summary>
    /// ConsultationItem的IValidatable适配器
    /// OpenSpec: consolidate-panel-viewmodels - 将ConsultationItem包装为IValidatable
    /// </summary>
    private sealed class ConsultationValidatorAdapter : IValidatable
    {
        private readonly ConsultationItem _consultation;

        public ConsultationValidatorAdapter(ConsultationItem consultation)
        {
            _consultation = consultation ?? throw new ArgumentNullException(nameof(consultation));
        }

        public string ValidationMessage { get; set; } = string.Empty;

        public bool Validate()
        {
            if (!_consultation.IsDiagnosisComplete)
            {
                ValidationMessage = "请填写中医诊断";
                return false;
            }
            ValidationMessage = string.Empty;
            return true;
        }
    }

    /// <summary>
    /// PrescriptionItem的IDataProvider适配器
    /// OpenSpec: consolidate-panel-viewmodels - 将PrescriptionItem包装为IDataProvider
    /// OpenSpec: adopt-mapperly-unified-mapping - 使用Mapper实例方法替代Item实例方法
    /// </summary>
    private sealed class PrescriptionDataProviderAdapter : IDataProvider
    {
        private static readonly PrescriptionMapper s_mapper = new();
        private readonly PrescriptionItem _prescription;

        public PrescriptionDataProviderAdapter(PrescriptionItem prescription)
        {
            _prescription = prescription ?? throw new ArgumentNullException(nameof(prescription));
        }

        public ConsultationInputDto? GetConsultationData() => null;
        public PrescriptionInputDto? GetPrescriptionData() => s_mapper.ToInputDto(_prescription);
    }

    /// <summary>
    /// PrescriptionItem的IValidatable适配器
    /// OpenSpec: consolidate-panel-viewmodels - 将PrescriptionItem包装为IValidatable
    /// </summary>
    private sealed class PrescriptionValidatorAdapter : IValidatable
    {
        private readonly PrescriptionItem _prescription;
        private readonly bool _isPrescriptionEnabled;

        public PrescriptionValidatorAdapter(PrescriptionItem prescription, bool isPrescriptionEnabled)
        {
            _prescription = prescription ?? throw new ArgumentNullException(nameof(prescription));
            _isPrescriptionEnabled = isPrescriptionEnabled;
        }

        public string ValidationMessage { get; set; } = string.Empty;

        public bool Validate()
        {
            // 如果不需要处方，直接返回true
            if (!_isPrescriptionEnabled)
            {
                ValidationMessage = string.Empty;
                return true;
            }

            // 需要处方时，检查是否有有效药材
            if (!_prescription.IsValid)
            {
                ValidationMessage = "请添加至少一种药材";
                return false;
            }
            ValidationMessage = string.Empty;
            return true;
        }
    }

    #endregion
}
