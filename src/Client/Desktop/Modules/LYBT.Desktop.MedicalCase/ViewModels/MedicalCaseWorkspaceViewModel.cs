using LYBT.Desktop.Infrastructure.Events;
using LYBT.Desktop.Infrastructure.Interfaces;
using LYBT.Desktop.MedicalCase.Components;
using LYBT.Desktop.MedicalCase.Interfaces;
using LYBT.Desktop.MedicalCase.Models;
using LYBT.Desktop.MedicalCase.Services;
using LYBT.Desktop.Models.ViewModels.Base;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Shared.Models.Extensions;
using Microsoft.Extensions.Logging;
using Prism.Commands;
using Prism.Events;
using Prism.Regions;
using Prism.Services.Dialogs;
using System.Windows;
using System.Windows.Media;

namespace LYBT.Desktop.MedicalCase.ViewModels
{
    /// <summary>
    /// Epic #2210 Phase 4: 4:6统一看诊界面ViewModel
    /// 设计文档: docs/explanation/architecture/patient-medicalcase-integration/patient-selection-workspace-integration.md
    /// 布局: 左侧40%诊断(Consultation) + 右侧60%处方(Prescription)
    /// 替代: MedicalCaseFlowViewModel (已标记为Deprecated)
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
        private readonly IActiveConsultationService _activeConsultationService; // OpenSpec: clarify-cancel-consultation-logic
        private readonly IDialogService? _dialogService; // OpenSpec: medicalcase-management-ui-refactor (EDITMODE-008)
        private readonly IAuditRequirementChecker? _auditRequirementChecker; // OpenSpec: medicalcase-management-ui-refactor (EDITMODE-010)

        #endregion

        #region 患者和医案属性

        private string _patientName = string.Empty;
        /// <summary>
        /// 患者姓名
        /// </summary>
        public string PatientName
        {
            get => _patientName;
            set => SetProperty(ref _patientName, value);
        }

        private string _patientInfo = string.Empty;
        /// <summary>
        /// 患者信息（性别/年龄/电话）
        /// </summary>
        public string PatientInfo
        {
            get => _patientInfo;
            set => SetProperty(ref _patientInfo, value);
        }

        private Guid _medicalCaseId = Guid.Empty;
        /// <summary>
        /// 当前医案ID
        /// </summary>
        public Guid MedicalCaseId
        {
            get => _medicalCaseId;
            set => SetProperty(ref _medicalCaseId, value);
        }

        private PatientDto? _currentPatient;
        /// <summary>
        /// 当前选择的患者信息
        /// </summary>
        public PatientDto? CurrentPatient
        {
            get => _currentPatient;
            set => SetProperty(ref _currentPatient, value);
        }

        #endregion

        #region 子面板ViewModel

        private ConsultationPanelViewModel? _consultationPanelViewModel;
        /// <summary>
        /// 诊断面板ViewModel
        /// </summary>
        public ConsultationPanelViewModel? ConsultationPanelViewModel
        {
            get => _consultationPanelViewModel;
            set => SetProperty(ref _consultationPanelViewModel, value);
        }

        private PrescriptionPanelViewModel? _prescriptionPanelViewModel;
        /// <summary>
        /// 处方面板ViewModel
        /// </summary>
        public PrescriptionPanelViewModel? PrescriptionPanelViewModel
        {
            get => _prescriptionPanelViewModel;
            set => SetProperty(ref _prescriptionPanelViewModel, value);
        }

        #endregion

        #region 状态属性

        private bool _isPrescriptionEnabled;
        /// <summary>
        /// 处方面板是否可用（诊断完成后开启）
        /// </summary>
        public bool IsPrescriptionEnabled
        {
            get => _isPrescriptionEnabled;
            set => SetProperty(ref _isPrescriptionEnabled, value);
        }

        private bool _showPrescriptionStatus;
        /// <summary>
        /// 是否显示处方状态标签
        /// </summary>
        public bool ShowPrescriptionStatus
        {
            get => _showPrescriptionStatus;
            set => SetProperty(ref _showPrescriptionStatus, value);
        }

        private string _prescriptionStatusText = "待诊断";
        /// <summary>
        /// 处方状态文本
        /// </summary>
        public string PrescriptionStatusText
        {
            get => _prescriptionStatusText;
            set => SetProperty(ref _prescriptionStatusText, value);
        }

        private Brush _prescriptionStatusBackground = new SolidColorBrush(Color.FromRgb(158, 158, 158));
        /// <summary>
        /// 处方状态背景色
        /// </summary>
        public Brush PrescriptionStatusBackground
        {
            get => _prescriptionStatusBackground;
            set => SetProperty(ref _prescriptionStatusBackground, value);
        }

        private string _consultationStatusText = "未完成";
        /// <summary>
        /// 诊断状态文本
        /// </summary>
        public string ConsultationStatusText
        {
            get => _consultationStatusText;
            set => SetProperty(ref _consultationStatusText, value);
        }

        private Brush _consultationStatusColor = new SolidColorBrush(Color.FromRgb(255, 152, 0));
        /// <summary>
        /// 诊断状态颜色
        /// </summary>
        public Brush ConsultationStatusColor
        {
            get => _consultationStatusColor;
            set => SetProperty(ref _consultationStatusColor, value);
        }

        private string _prescriptionStatusSummary = "待开方";
        /// <summary>
        /// 处方状态摘要
        /// </summary>
        public string PrescriptionStatusSummary
        {
            get => _prescriptionStatusSummary;
            set => SetProperty(ref _prescriptionStatusSummary, value);
        }

        private Brush _prescriptionStatusSummaryColor = new SolidColorBrush(Color.FromRgb(158, 158, 158));
        /// <summary>
        /// 处方状态摘要颜色
        /// </summary>
        public Brush PrescriptionStatusSummaryColor
        {
            get => _prescriptionStatusSummaryColor;
            set => SetProperty(ref _prescriptionStatusSummaryColor, value);
        }

        private bool _canPrintPrescription;
        /// <summary>
        /// 是否可以打印处方
        /// </summary>
        public bool CanPrintPrescription
        {
            get => _canPrintPrescription;
            set => SetProperty(ref _canPrintPrescription, value);
        }

        private bool _canComplete;
        /// <summary>
        /// 是否可以完成看诊
        /// </summary>
        public bool CanComplete
        {
            get => _canComplete;
            set => SetProperty(ref _canComplete, value);
        }

        #region refactor-medicalcase-management: 编辑模式属性

        private bool _isEditing = true;
        /// <summary>
        /// 是否处于编辑模式
        /// - 新建医案或继续Draft/Active医案时为true
        /// - 查看已完成医案时为false（只读模式）
        /// </summary>
        public bool IsEditing
        {
            get => _isEditing;
            set
            {
                if (SetProperty(ref _isEditing, value))
                {
                    RaisePropertyChanged(nameof(IsReadOnly));
                    RaisePropertyChanged(nameof(ShowEditButton));
                    // OpenSpec: medicalcase-management-ui-refactor - 新增按钮可见性属性
                    RaisePropertyChanged(nameof(ShowEditButtonTopRight));
                    RaisePropertyChanged(nameof(ShowSaveButton));
                    RaisePropertyChanged(nameof(ShowDraftButton));
                    RaisePropertyChanged(nameof(ShowCompleteButton));
                    SaveCommand?.RaiseCanExecuteChanged();
                    EnterEditModeCommand?.RaiseCanExecuteChanged();
                }
            }
        }

        /// <summary>
        /// 是否处于只读模式（与IsEditing互斥）
        /// </summary>
        public bool IsReadOnly => !IsEditing;

        /// <summary>
        /// 是否显示编辑按钮（底部，仅Clinical只读模式且有权限）
        /// OpenSpec: medicalcase-management-ui-refactor - Clinical模式在底部显示"修改医案"
        /// </summary>
        public bool ShowEditButton => IsReadOnly && CanEdit && WorkspaceMode == WorkspaceMode.Clinical;

        /// <summary>
        /// 是否在右上角显示编辑按钮（Management只读模式且有权限）
        /// OpenSpec: medicalcase-management-ui-refactor TASK-003
        /// </summary>
        public bool ShowEditButtonTopRight => IsReadOnly && CanEdit && WorkspaceMode == WorkspaceMode.Management;

        /// <summary>
        /// 是否显示保存医案按钮（Management编辑模式）
        /// OpenSpec: medicalcase-management-ui-refactor TASK-003
        /// </summary>
        public bool ShowSaveButton => IsEditing && WorkspaceMode == WorkspaceMode.Management;

        /// <summary>
        /// 是否显示暂存医案按钮（Clinical编辑模式）
        /// OpenSpec: medicalcase-management-ui-refactor TASK-003
        /// </summary>
        public bool ShowDraftButton => IsEditing && WorkspaceMode == WorkspaceMode.Clinical;

        /// <summary>
        /// 是否显示完成看诊按钮（Clinical编辑模式）
        /// OpenSpec: medicalcase-management-ui-refactor TASK-003
        /// </summary>
        public bool ShowCompleteButton => IsEditing && WorkspaceMode == WorkspaceMode.Clinical;

        private bool _isHistoricalEditMode;
        /// <summary>
        /// 是否为历史修改模式（从管理界面进入）
        /// - 管理员编辑已完成医案时为true
        /// - 需要填写修改原因
        /// </summary>
        public bool IsHistoricalEditMode
        {
            get => _isHistoricalEditMode;
            set => SetProperty(ref _isHistoricalEditMode, value);
        }

        private bool _canEdit;
        /// <summary>
        /// 当前用户是否有编辑权限
        /// - 管理员：所有医案
        /// - 医生：仅自己未完成的医案
        /// </summary>
        public bool CanEdit
        {
            get => _canEdit;
            set
            {
                if (SetProperty(ref _canEdit, value))
                {
                    RaisePropertyChanged(nameof(ShowEditButton));
                    EnterEditModeCommand?.RaiseCanExecuteChanged();
                }
            }
        }

        private string _editReason = string.Empty;
        /// <summary>
        /// 修改原因（历史修改模式下必填）
        /// </summary>
        public string EditReason
        {
            get => _editReason;
            set => SetProperty(ref _editReason, value);
        }

        private bool _isFromManagement;
        /// <summary>
        /// 是否来自管理界面
        /// </summary>
        public bool IsFromManagement
        {
            get => _isFromManagement;
            set => SetProperty(ref _isFromManagement, value);
        }

        #endregion

        #region OpenSpec: refine-medicalcase-edit-modes - 工作区模式属性

        private WorkspaceMode _workspaceMode = WorkspaceMode.Clinical;
        /// <summary>
        /// 工作区模式
        /// OpenSpec: refine-medicalcase-edit-modes - EDITMODE-001
        /// Clinical: 临床看诊模式（从PatientSelection进入）
        /// Management: 管理编辑模式（从MedicalCaseManagement进入）
        /// </summary>
        public WorkspaceMode WorkspaceMode
        {
            get => _workspaceMode;
            set
            {
                if (SetProperty(ref _workspaceMode, value))
                {
                    RaisePropertyChanged(nameof(HeaderTitle));
                    RaisePropertyChanged(nameof(BackButtonText));
                    // OpenSpec: medicalcase-management-ui-refactor - 模式切换影响按钮可见性
                    RaisePropertyChanged(nameof(ShowEditButton));
                    RaisePropertyChanged(nameof(ShowEditButtonTopRight));
                    RaisePropertyChanged(nameof(ShowSaveButton));
                    RaisePropertyChanged(nameof(ShowDraftButton));
                    RaisePropertyChanged(nameof(ShowCompleteButton));
                }
            }
        }

        /// <summary>
        /// 标题文本（动态计算）
        /// OpenSpec: refine-medicalcase-edit-modes - EDITMODE-001
        /// Clinical: "看诊中 | 患者：XXX"
        /// Management: "编辑医案 | 患者：XXX"
        /// </summary>
        public string HeaderTitle => WorkspaceMode switch
        {
            WorkspaceMode.Clinical => "看诊中",
            WorkspaceMode.Management => "编辑医案",
            _ => "看诊中"
        };

        /// <summary>
        /// 返回按钮文本（动态计算）
        /// OpenSpec: refine-medicalcase-edit-modes - EDITMODE-005
        /// Clinical: "返回患者选择"
        /// Management: "返回医案列表"
        /// </summary>
        public string BackButtonText => WorkspaceMode switch
        {
            WorkspaceMode.Clinical => "返回患者选择",
            WorkspaceMode.Management => "返回医案列表",
            _ => "返回患者选择"
        };

        #endregion

        private string _remark = string.Empty;
        /// <summary>
        /// 医案备注（OpenSpec: refactor-medicalcase-ui - 替代底部状态指示器）
        /// </summary>
        public string Remark
        {
            get => _remark;
            set
            {
                if (SetProperty(ref _remark, value))
                {
                    // 同步到缓存的医案数据
                    if (_dataLoader.CachedMedicalCase != null)
                    {
                        _dataLoader.CachedMedicalCase.Remark = value;
                    }
                }
            }
        }

        #endregion

        #region 命令

        /// <summary>
        /// 返回命令 - 根据WorkspaceMode导航到不同目标
        /// OpenSpec: refine-medicalcase-edit-modes - EDITMODE-005
        /// Clinical: 返回PatientSelectionView
        /// Management: 返回MedicalCaseManagementView
        /// </summary>
        public DelegateCommand BackCommand { get; }

        // 兼容性别名
        public DelegateCommand BackToPatientSelectionCommand => BackCommand;

        /// <summary>
        /// 暂存医案命令
        /// OpenSpec: refine-medicalcase-edit-modes - EDITMODE-003
        /// 保存当前数据 + 设置状态为Draft + 切换到只读模式（留在当前界面）
        /// </summary>
        public DelegateCommand SaveAndStayCommand { get; }

        // 兼容性别名
        public DelegateCommand SaveDraftCommand => SaveAndStayCommand;

        public DelegateCommand PrintPrescriptionCommand { get; }
        public DelegateCommand CompleteConsultationCommand { get; }

        // refactor-medicalcase-management: 新增保存和编辑模式切换命令
        /// <summary>
        /// 保存命令 - 保存当前进度但不改变医案状态
        /// 编辑模式下可见
        /// </summary>
        public DelegateCommand SaveCommand { get; }

        /// <summary>
        /// 修改医案命令
        /// OpenSpec: refine-medicalcase-edit-modes - EDITMODE-004
        /// 切换到编辑状态（需权限检查）
        /// </summary>
        public DelegateCommand EnterEditModeCommand { get; }

        #endregion

        #region 构造函数

        public MedicalCaseWorkspaceViewModel(
            MedicalCaseDataManager dataManager,
            MedicalCaseLifecycleHandler lifecycleHandler,
            MedicalCaseDataLoader dataLoader,
            IRegionManager regionManager,
            IEventAggregator eventAggregator,
            ILoggerFactory loggerFactory,
            ConsultationPanelViewModel consultationPanelViewModel,
            PrescriptionPanelViewModel prescriptionPanelViewModel,
            IActiveConsultationService activeConsultationService, // OpenSpec: clarify-cancel-consultation-logic
            ISessionManager? sessionManager = null,
            ICommonDialogService? commonDialogService = null, // Issue #2247: 统一对话框服务
            IDialogService? dialogService = null, // OpenSpec: medicalcase-management-ui-refactor (EDITMODE-008)
            IAuditRequirementChecker? auditRequirementChecker = null) // OpenSpec: medicalcase-management-ui-refactor (EDITMODE-010)
            : base(eventAggregator, loggerFactory, regionManager, sessionManager, null, commonDialogService)
        {
            _dataManager = dataManager ?? throw new ArgumentNullException(nameof(dataManager));
            _regionManager = regionManager ?? throw new ArgumentNullException(nameof(regionManager));
            _lifecycleHandler = lifecycleHandler ?? throw new ArgumentNullException(nameof(lifecycleHandler));
            _dataLoader = dataLoader ?? throw new ArgumentNullException(nameof(dataLoader));
            _injectedConsultationPanelViewModel = consultationPanelViewModel ?? throw new ArgumentNullException(nameof(consultationPanelViewModel));
            _injectedPrescriptionPanelViewModel = prescriptionPanelViewModel ?? throw new ArgumentNullException(nameof(prescriptionPanelViewModel));
            _activeConsultationService = activeConsultationService ?? throw new ArgumentNullException(nameof(activeConsultationService));
            _dialogService = dialogService; // 可选依赖
            _auditRequirementChecker = auditRequirementChecker; // 可选依赖

            // 订阅生命周期事件
            _lifecycleHandler.ActionCompleted += OnLifecycleActionCompleted;
            _dataLoader.DataLoaded += OnDataLoaded;

            // 初始化命令
            // OpenSpec: refine-medicalcase-edit-modes - 返回按钮根据WorkspaceMode导航
            BackCommand = new DelegateCommand(async () => await ExecuteBackAsync());
            // OpenSpec: refine-medicalcase-edit-modes - 暂存医案（保存+切换到只读模式）
            SaveAndStayCommand = new DelegateCommand(ExecuteSaveAndStay);
            PrintPrescriptionCommand = new DelegateCommand(ExecutePrintPrescription, () => CanPrintPrescription)
                .ObservesProperty(() => CanPrintPrescription);
            CompleteConsultationCommand = new DelegateCommand(ExecuteCompleteConsultation, () => CanComplete)
                .ObservesProperty(() => CanComplete);

            // refactor-medicalcase-management: 保存和编辑模式切换命令
            SaveCommand = new DelegateCommand(ExecuteSave, () => IsEditing)
                .ObservesProperty(() => IsEditing);
            EnterEditModeCommand = new DelegateCommand(ExecuteEnterEditMode, () => IsReadOnly && CanEdit)
                .ObservesProperty(() => IsReadOnly)
                .ObservesProperty(() => CanEdit);

            // 订阅诊断完成事件（启用处方面板）
            EventAggregator.GetEvent<ConsultationCompletedEvent>()
                .Subscribe(OnConsultationCompleted, ThreadOption.UIThread);

            // 订阅处方完成事件
            EventAggregator.GetEvent<PrescriptionCompletedEvent>()
                .Subscribe(OnPrescriptionCompleted, ThreadOption.UIThread);

            Logger.LogInformation("MedicalCaseWorkspaceViewModel已初始化（4:6统一界面）");
        }

        #endregion

        #region 命令实现

        #region refactor-medicalcase-management: 保存和编辑模式命令

        /// <summary>
        /// 执行保存 - 保存当前进度但不改变医案状态
        /// </summary>
        private async void ExecuteSave()
        {
            try
            {
                SetIsBusy(true, "正在保存...");

                // 同步备注到面板
                if (ConsultationPanelViewModel != null)
                {
                    ConsultationPanelViewModel.MedicalCaseRemark = Remark;
                }

                // 保存诊断数据
                if (ConsultationPanelViewModel is ISaveable consultationSaveable)
                {
                    await consultationSaveable.SaveSilentlyAsync();
                }

                // 保存处方数据
                if (PrescriptionPanelViewModel is ISaveable prescriptionSaveable)
                {
                    await prescriptionSaveable.SaveSilentlyAsync();
                }

                // 历史修改模式下记录修改原因
                if (IsHistoricalEditMode && !string.IsNullOrWhiteSpace(EditReason))
                {
                    // TODO: Phase 4实现 - 通过审计服务记录修改原因
                    Logger.LogInformation("历史修改保存，原因: {EditReason}", EditReason);
                }

                await ShowSuccessMessageAsync("保存成功");
                Logger.LogInformation("医案数据保存成功，MedicalCaseId: {MedicalCaseId}", MedicalCaseId);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "保存医案数据失败");
                await ShowErrorMessageAsync($"保存失败：{ex.Message}");
            }
            finally
            {
                SetIsBusy(false);
            }
        }

        /// <summary>
        /// 进入编辑模式
        /// </summary>
        private void ExecuteEnterEditMode()
        {
            if (!CanEdit)
            {
                Logger.LogWarning("无编辑权限，无法进入编辑模式");
                return;
            }

            IsEditing = true;
            Logger.LogInformation("进入编辑模式，MedicalCaseId: {MedicalCaseId}", MedicalCaseId);
        }

        #endregion

        /// <summary>
        /// 返回（根据WorkspaceMode导航）
        /// OpenSpec: refine-medicalcase-edit-modes - EDITMODE-005
        /// OpenSpec: medicalcase-management-ui-refactor - EDITMODE-008
        /// Clinical: 返回PatientSelectionView（显示离开确认对话框）
        /// Management只读: 直接返回MedicalCaseManagementView
        /// Management编辑: 显示UnsavedChangesDialog后返回
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
                        _regionManager.RequestNavigate("ContentRegion", "MedicalCaseManagementView");
                        return;
                    }

                    // Management编辑模式: 显示UnsavedChangesDialog
                    var shouldNavigate = await HandleManagementLeaveRequestAsync();
                    if (shouldNavigate)
                    {
                        _regionManager.RequestNavigate("ContentRegion", "MedicalCaseManagementView");
                    }
                    return;
                }

                // Clinical模式: 使用现有的三选项对话框
                var result = await HandleLeaveRequestAsync();
                if (result.CanLeave)
                {
                    _regionManager.RequestNavigate("ContentRegion", "PatientSelectionView");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "返回时发生异常");
            }
        }

        /// <summary>
        /// Management编辑模式返回确认
        /// OpenSpec: medicalcase-management-ui-refactor (EDITMODE-008)
        /// 三选项: 保存修改(Yes) / 放弃修改(No) / 取消(Cancel)
        /// </summary>
        /// <returns>true: 允许导航; false: 留在当前界面</returns>
        private async Task<bool> HandleManagementLeaveRequestAsync()
        {
            if (_dialogService == null)
            {
                // Fallback: 如果没有IDialogService，使用MessageBox
                var result = MessageBox.Show(
                    "您有未保存的修改，请选择操作：\n\n" +
                    "【是】保存修改后返回\n" +
                    "【否】放弃修改直接返回\n" +
                    "【取消】留在当前界面",
                    "未保存的修改",
                    MessageBoxButton.YesNoCancel,
                    MessageBoxImage.Warning);

                switch (result)
                {
                    case MessageBoxResult.Yes:
                        // 检查审计需求
                        var auditReason = await CheckAndGetAuditReasonAsync();
                        if (auditReason == null) return false; // 用户取消审计

                        if (!string.IsNullOrEmpty(auditReason))
                        {
                            EditReason = auditReason;
                        }

                        await SaveDraftOnlyAsync();
                        IsEditing = false;
                        return true;
                    case MessageBoxResult.No:
                        return true;
                    default:
                        return false;
                }
            }

            // 使用Prism Dialog
            var tcs = new TaskCompletionSource<bool>();
            _dialogService.ShowDialog(nameof(Dialogs.UnsavedChangesDialog), new DialogParameters(), async dialogResult =>
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
                                EditReason = auditReason;
                            }

                            await SaveDraftOnlyAsync();
                            IsEditing = false;
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
                    Logger.LogError(ex, "处理返回确认对话框时发生异常");
                    tcs.SetResult(false);
                }
            });

            return await tcs.Task;
        }

        /// <summary>
        /// 显示离开确认对话框（三选项）并处理用户选择
        /// OpenSpec: clarify-cancel-consultation-logic
        /// 此方法由IActiveConsultationService调用（退出登录时）
        /// </summary>
        /// <remarks>Issue #2247: 使用ICommonDialogService替代直接MessageBox.Show调用</remarks>
        private async Task<LeaveConsultationResult> HandleLeaveRequestAsync()
        {
            var message = "您将离开看诊界面，是否暂存当前医案？\n\n" +
                "【是】暂存医案 - 保存当前进度，下次可继续\n" +
                "【否】取消医案 - 作废本次就诊\n" +
                "【取消】继续看诊 - 返回当前界面";

            LeaveConsultationChoice choice;

            if (CommonDialogService != null)
            {
                // Issue #2247: 使用抽象的对话框服务
                var dialogResult = await CommonDialogService.ShowTripleChoiceAsync(message, "离开确认");
                choice = dialogResult switch
                {
                    TripleChoiceResult.Yes => LeaveConsultationChoice.SaveDraft,
                    TripleChoiceResult.No => LeaveConsultationChoice.CancelCase,
                    _ => LeaveConsultationChoice.Stay
                };
            }
            else
            {
                // Fallback: 直接使用MessageBox
                var result = MessageBox.Show(message, "离开确认", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
                choice = result switch
                {
                    MessageBoxResult.Yes => LeaveConsultationChoice.SaveDraft,
                    MessageBoxResult.No => LeaveConsultationChoice.CancelCase,
                    _ => LeaveConsultationChoice.Stay
                };
            }

            // 根据用户选择执行对应操作
            switch (choice)
            {
                case LeaveConsultationChoice.SaveDraft:
                    await SaveDraftOnlyAsync();
                    return LeaveConsultationResult.AllowLeave(choice);

                case LeaveConsultationChoice.CancelCase:
                    await CancelCaseOnlyAsync();
                    return LeaveConsultationResult.AllowLeave(choice);

                case LeaveConsultationChoice.Stay:
                default:
                    Logger.LogDebug("用户选择继续停留");
                    return LeaveConsultationResult.CancelLeave();
            }
        }

        /// <summary>
        /// 仅保存草稿（不导航）
        /// </summary>
        private async Task SaveDraftOnlyAsync()
        {
            try
            {
                SetIsBusy(true, "正在保存...");

                if (ConsultationPanelViewModel != null)
                {
                    ConsultationPanelViewModel.MedicalCaseRemark = Remark;
                }
                if (ConsultationPanelViewModel is ISaveable consultationSaveable)
                {
                    await consultationSaveable.SaveSilentlyAsync();
                }
                if (PrescriptionPanelViewModel is ISaveable prescriptionSaveable)
                {
                    await prescriptionSaveable.SaveSilentlyAsync();
                }

                await _lifecycleHandler.SaveDraftAsync(MedicalCaseId);
                Logger.LogInformation("医案已暂存");
            }
            finally
            {
                SetIsBusy(false);
            }
        }

        /// <summary>
        /// 仅取消医案（不导航）
        /// </summary>
        private async Task CancelCaseOnlyAsync()
        {
            try
            {
                SetIsBusy(true, "正在处理...");

                // 取消前自动保存（供审计）
                try
                {
                    if (ConsultationPanelViewModel != null)
                    {
                        ConsultationPanelViewModel.MedicalCaseRemark = Remark;
                    }
                    if (ConsultationPanelViewModel is ISaveable consultationSaveable)
                    {
                        await consultationSaveable.SaveSilentlyAsync();
                    }
                    if (PrescriptionPanelViewModel is ISaveable prescriptionSaveable)
                    {
                        await prescriptionSaveable.SaveSilentlyAsync();
                    }
                }
                catch (Exception saveEx)
                {
                    Logger.LogWarning(saveEx, "取消前保存失败，继续执行取消操作");
                }

                await _lifecycleHandler.CancelAsync(MedicalCaseId);
                Logger.LogInformation("医案已取消（软删除）");
            }
            finally
            {
                SetIsBusy(false);
            }
        }

        /// <summary>
        /// 暂存医案并返回患者列表
        /// </summary>
        private async Task SaveDraftAndNavigateBackAsync()
        {
            try
            {
                SetIsBusy(true, "正在保存...");

                // 设置医案备注
                if (ConsultationPanelViewModel != null)
                {
                    ConsultationPanelViewModel.MedicalCaseRemark = Remark;
                }

                // 保存诊断和处方数据
                if (ConsultationPanelViewModel is ISaveable consultationSaveable)
                {
                    await consultationSaveable.SaveSilentlyAsync();
                }
                if (PrescriptionPanelViewModel is ISaveable prescriptionSaveable)
                {
                    await prescriptionSaveable.SaveSilentlyAsync();
                }

                // 更新状态为Draft
                var result = await _lifecycleHandler.SaveDraftAsync(MedicalCaseId);

                if (result.success)
                {
                    Logger.LogInformation("医案已暂存，返回患者列表");
                    _regionManager.RequestNavigate("ContentRegion", "PatientSelectionView");
                }
                else
                {
                    await ShowErrorMessageAsync(result.errorMessage ?? "暂存失败");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "暂存医案失败");
                await ShowErrorMessageAsync($"暂存失败：{ex.Message}");
            }
            finally
            {
                SetIsBusy(false);
            }
        }

        /// <summary>
        /// 取消医案并返回患者列表（软删除）
        /// OpenSpec: clarify-cancel-consultation-logic
        /// </summary>
        private async Task CancelCaseAndNavigateBackAsync()
        {
            try
            {
                SetIsBusy(true, "正在处理...");

                // 取消前自动保存（供审计）
                try
                {
                    if (ConsultationPanelViewModel != null)
                    {
                        ConsultationPanelViewModel.MedicalCaseRemark = Remark;
                    }
                    if (ConsultationPanelViewModel is ISaveable consultationSaveable)
                    {
                        await consultationSaveable.SaveSilentlyAsync();
                    }
                    if (PrescriptionPanelViewModel is ISaveable prescriptionSaveable)
                    {
                        await prescriptionSaveable.SaveSilentlyAsync();
                    }
                    Logger.LogDebug("取消前数据已保存（供审计）");
                }
                catch (Exception saveEx)
                {
                    Logger.LogWarning(saveEx, "取消前保存失败，继续执行取消操作");
                }

                // 执行软删除
                var result = await _lifecycleHandler.CancelAsync(MedicalCaseId);

                if (result.success)
                {
                    Logger.LogInformation("医案已取消（软删除），返回患者列表");
                    _regionManager.RequestNavigate("ContentRegion", "PatientSelectionView");
                }
                else
                {
                    await ShowErrorMessageAsync(result.errorMessage ?? "取消失败");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "取消医案失败");
                await ShowErrorMessageAsync($"取消失败：{ex.Message}");
            }
            finally
            {
                SetIsBusy(false);
            }
        }

        /// <summary>
        /// 暂存医案（保存当前数据 + 切换到只读模式，留在当前界面）
        /// OpenSpec: refine-medicalcase-edit-modes - EDITMODE-003
        /// OpenSpec: medicalcase-management-ui-refactor - EDITMODE-010 审计检查
        /// </summary>
        private async void ExecuteSaveAndStay()
        {
            try
            {
                // Management模式下检查审计需求
                if (WorkspaceMode == WorkspaceMode.Management)
                {
                    var auditReason = await CheckAndGetAuditReasonAsync();
                    if (auditReason == null)
                    {
                        // 用户取消了审计对话框
                        return;
                    }

                    // 如果有审计原因，记录到EditReason
                    if (!string.IsNullOrEmpty(auditReason))
                    {
                        EditReason = auditReason;
                    }
                }

                SetIsBusy(true, WorkspaceMode == WorkspaceMode.Management ? "正在保存..." : "正在暂存...");

                // 同步医案备注到面板
                if (ConsultationPanelViewModel != null)
                {
                    ConsultationPanelViewModel.MedicalCaseRemark = Remark;
                }

                // 保存诊断数据
                if (ConsultationPanelViewModel is ISaveable consultationSaveable)
                {
                    await consultationSaveable.SaveAsync();
                }

                // 保存处方数据
                if (PrescriptionPanelViewModel is ISaveable prescriptionSaveable)
                {
                    await prescriptionSaveable.SaveAsync();
                }

                // 更新医案状态为Draft
                var result = await _lifecycleHandler.SaveDraftAsync(MedicalCaseId);

                if (result.success)
                {
                    // OpenSpec: refine-medicalcase-edit-modes - 切换到只读模式（留在当前界面）
                    IsEditing = false;

                    var message = WorkspaceMode == WorkspaceMode.Management
                        ? "保存成功"
                        : "医案已暂存，可随时点击'修改医案'继续编辑";
                    await ShowSuccessMessageAsync(message);
                    Logger.LogInformation("医案保存成功，切换到只读模式");
                }
                else
                {
                    await ShowErrorMessageAsync(result.errorMessage ?? "保存失败");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "保存医案失败");
                await ShowErrorMessageAsync($"保存失败：{ex.Message}");
            }
            finally
            {
                SetIsBusy(false);
            }
        }

        /// <summary>
        /// 检查审计需求并获取审计原因
        /// OpenSpec: medicalcase-management-ui-refactor (EDITMODE-010, EDITMODE-011)
        /// </summary>
        /// <returns>
        /// null: 用户取消，不应继续保存
        /// 空字符串: 无需审计，可以直接保存
        /// 非空字符串: 需要审计，返回用户填写的原因
        /// </returns>
        private async Task<string?> CheckAndGetAuditReasonAsync()
        {
            // 如果没有审计检查器，跳过审计
            if (_auditRequirementChecker == null)
            {
                return string.Empty;
            }

            // 获取当前医案数据
            var medicalCase = _dataLoader.CachedMedicalCase;
            if (medicalCase == null)
            {
                Logger.LogWarning("CheckAndGetAuditReasonAsync: 无法获取当前医案数据");
                return string.Empty;
            }

            // 获取当前用户ID
            var currentUserId = SessionManager?.CurrentUser?.Id ?? Guid.Empty;

            // 检查是否需要审计
            var needsAudit = _auditRequirementChecker.IsAuditRequired(medicalCase, currentUserId);
            if (!needsAudit)
            {
                return string.Empty;
            }

            // 需要审计 - 显示审计理由对话框
            return await ShowAuditReasonDialogAsync();
        }

        /// <summary>
        /// 显示审计理由对话框
        /// </summary>
        /// <returns>用户输入的原因，或null表示取消</returns>
        private Task<string?> ShowAuditReasonDialogAsync()
        {
            if (_dialogService == null)
            {
                // Fallback: 简单的输入框
                // 在实际实现中可以使用简单MessageBox
                Logger.LogWarning("ShowAuditReasonDialogAsync: 无IDialogService，跳过审计对话框");
                return Task.FromResult<string?>(string.Empty);
            }

            var tcs = new TaskCompletionSource<string?>();
            _dialogService.ShowDialog(nameof(Dialogs.AuditReasonDialog), new DialogParameters(), dialogResult =>
            {
                if (dialogResult.Result == ButtonResult.OK &&
                    dialogResult.Parameters.TryGetValue("Reason", out string? reason))
                {
                    tcs.SetResult(reason);
                }
                else
                {
                    tcs.SetResult(null); // 用户取消
                }
            });

            return tcs.Task;
        }

        /// <summary>
        /// 打印处方笺
        /// </summary>
        private async void ExecutePrintPrescription()
        {
            try
            {
                SetIsBusy(true, "正在准备打印...");

                // TODO: 实现打印逻辑
                Logger.LogInformation("打印处方笺，MedicalCaseId: {MedicalCaseId}", MedicalCaseId);

                await ShowSuccessMessageAsync("处方笺打印成功");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "打印处方笺失败");
                await ShowErrorMessageAsync($"打印失败：{ex.Message}");
            }
            finally
            {
                SetIsBusy(false);
            }
        }

        /// <summary>
        /// 完成看诊
        /// </summary>
        private async void ExecuteCompleteConsultation()
        {
            try
            {
                SetIsBusy(true, "正在完成看诊...");

                // OpenSpec: clarify-cancel-consultation-logic
                // 医案备注通过ConsultationInputDto.MedicalCaseRemark传递保存
                if (ConsultationPanelViewModel != null)
                {
                    ConsultationPanelViewModel.MedicalCaseRemark = Remark;
                }

                // 保存诊断数据（包含医案备注）
                if (ConsultationPanelViewModel is ISaveable consultationSaveable)
                {
                    var consultationResult = await consultationSaveable.SaveAsync();
                    if (!consultationResult)
                    {
                        await ShowErrorMessageAsync("保存诊断数据失败");
                        return;
                    }
                }

                // 保存处方数据（如果启用）
                if (IsPrescriptionEnabled && PrescriptionPanelViewModel is ISaveable prescriptionSaveable)
                {
                    var prescriptionResult = await prescriptionSaveable.SaveAsync();
                    if (!prescriptionResult)
                    {
                        await ShowErrorMessageAsync("保存处方数据失败");
                        return;
                    }
                }

                // 完成医案
                var result = await _lifecycleHandler.CompleteAsync(MedicalCaseId);

                if (result.success)
                {
                    await ShowSuccessMessageAsync("看诊已完成");
                    Logger.LogInformation("医案已完成");

                    // OpenSpec: refine-medicalcase-edit-modes - EDITMODE-007 完成后根据模式返回
                    var targetView = WorkspaceMode switch
                    {
                        WorkspaceMode.Clinical => "PatientSelectionView",
                        WorkspaceMode.Management => "MedicalCaseManagementView",
                        _ => "PatientSelectionView"
                    };
                    _regionManager.RequestNavigate("ContentRegion", targetView);
                }
                else
                {
                    await ShowErrorMessageAsync(result.errorMessage ?? "完成失败");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "完成看诊失败");
                await ShowErrorMessageAsync($"完成失败：{ex.Message}");
            }
            finally
            {
                SetIsBusy(false);
            }
        }

        #endregion

        #region INavigationAware

        public override async void OnNavigatedTo(NavigationContext navigationContext)
        {
            base.OnNavigatedTo(navigationContext);

            // 接收导航参数
            MedicalCaseId = navigationContext.Parameters.GetValue<Guid>("MedicalCaseId");
            CurrentPatient = navigationContext.Parameters.GetValue<PatientDto>("CurrentPatient");

            // OpenSpec: refine-medicalcase-edit-modes - 处理新的导航参数
            WorkspaceMode = navigationContext.Parameters.GetValue<WorkspaceMode>(MedicalCaseNavigationParameters.WorkspaceModeKey);
            var initialEditState = navigationContext.Parameters.GetValue<EditState>(MedicalCaseNavigationParameters.InitialEditStateKey);

            // 兼容性处理：旧参数
            var editMode = navigationContext.Parameters.GetValue<string>("EditMode");
            IsFromManagement = navigationContext.Parameters.GetValue<bool>("IsFromManagement") ||
                               WorkspaceMode == WorkspaceMode.Management;
            IsHistoricalEditMode = editMode == "HistoricalEdit";

            Logger.LogInformation("进入看诊界面，MedicalCaseId: {MedicalCaseId}, 患者: {PatientName}, 工作区模式: {WorkspaceMode}, 初始编辑状态: {EditState}",
                MedicalCaseId, CurrentPatient?.Name, WorkspaceMode, initialEditState);

            // 初始化患者信息
            await InitializePatientInfoAsync();

            // 加载医案数据
            await LoadMedicalCaseDataAsync();

            // 初始化子面板ViewModel
            InitializeChildViewModels();

            // OpenSpec: refine-medicalcase-edit-modes - 根据导航参数或医案状态决定编辑模式
            await DetermineEditModeAsync(initialEditState);
        }

        /// <summary>
        /// 根据医案状态和用户权限决定编辑模式
        /// OpenSpec: refine-medicalcase-edit-modes - EDITMODE-002
        /// </summary>
        /// <param name="initialEditState">导航参数中的初始编辑状态（可选）</param>
        private async Task DetermineEditModeAsync(EditState initialEditState = EditState.Editing)
        {
            try
            {
                var medicalCase = _dataLoader.CachedMedicalCase;
                if (medicalCase == null)
                {
                    // 新建医案，默认编辑模式
                    IsEditing = true;
                    CanEdit = true;
                    return;
                }

                // 获取当前用户权限
                var currentUserRole = SessionManager?.CurrentUser?.Role;
                var isAdmin = currentUserRole == Shared.Models.Enums.UserRole.Admin ||
                              currentUserRole == Shared.Models.Enums.UserRole.SuperAdmin;

                var caseStatus = medicalCase.CaseStatus;
                var isCompleted = caseStatus == Shared.Models.Enums.MedicalCaseStatus.Completed;

                // 确定是否有编辑权限
                if (isAdmin)
                {
                    // 管理员可以编辑所有医案
                    CanEdit = true;
                }
                else
                {
                    // 医生只能编辑自己未完成的医案
                    var currentUserId = SessionManager?.CurrentUser?.Id ?? Guid.Empty;
                    var isOwner = medicalCase.DoctorId == currentUserId;
                    var isNotCompleted = !isCompleted;
                    CanEdit = isOwner && isNotCompleted;
                }

                // OpenSpec: refine-medicalcase-edit-modes - 根据导航参数决定初始编辑状态
                // 优先使用导航参数，但需检查权限
                if (initialEditState == EditState.Editing && CanEdit)
                {
                    IsEditing = true;
                }
                else if (initialEditState == EditState.ReadOnly)
                {
                    IsEditing = false;
                }
                else
                {
                    // 回退到原有逻辑
                    IsEditing = IsHistoricalEditMode || (CanEdit && !isCompleted);
                }

                Logger.LogInformation("编辑模式确定：IsEditing={IsEditing}, CanEdit={CanEdit}, CaseStatus={CaseStatus}, InitialEditState={InitialEditState}",
                    IsEditing, CanEdit, caseStatus, initialEditState);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "确定编辑模式失败");
                // 默认只读模式
                IsEditing = false;
                CanEdit = false;
            }

            await Task.CompletedTask;
        }

        /// <summary>
        /// 初始化患者信息
        /// </summary>
        private async Task InitializePatientInfoAsync()
        {
            if (CurrentPatient != null)
            {
                var (patientName, patientInfo) = _dataLoader.FormatPatientInfo(CurrentPatient);
                PatientName = patientName;
                PatientInfo = patientInfo;

                // 新建医案场景
                if (MedicalCaseId == Guid.Empty)
                {
                    try
                    {
                        SetIsBusy(true, "正在创建医案...");

                        var result = await _lifecycleHandler.CreateMedicalCaseAsync(CurrentPatient.Id);

                        if (!result.success)
                        {
                            Logger.LogError("创建MedicalCase失败：{ErrorMessage}", result.errorMessage);
                            await ShowErrorMessageAsync("创建医案失败，请重试");
                            return;
                        }

                        MedicalCaseId = result.medicalCaseId;
                        Logger.LogInformation("MedicalCase创建成功，MedicalCaseId: {MedicalCaseId}", MedicalCaseId);
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError(ex, "创建MedicalCase异常");
                        await ShowErrorMessageAsync($"创建医案失败：{ex.Message}");
                    }
                    finally
                    {
                        SetIsBusy(false);
                    }
                }
            }
        }

        /// <summary>
        /// 加载医案数据
        /// </summary>
        private async Task LoadMedicalCaseDataAsync()
        {
            if (MedicalCaseId == Guid.Empty)
            {
                return;
            }

            try
            {
                SetIsBusy(true, "正在加载医案数据...");

                var result = await _dataLoader.LoadMedicalCaseDetailsAsync(MedicalCaseId);

                if (!result.success)
                {
                    Logger.LogWarning("加载医案数据失败：{ErrorMessage}", result.errorMessage);
                    return;
                }

                // 更新状态（从detail对象检查子实体是否存在）
                var hasConsultation = result.detail?.Consultation != null;
                var hasPrescription = result.detail?.Prescription != null;

                UpdateConsultationStatus(hasConsultation);
                UpdatePrescriptionStatus(hasPrescription);

                // 如果诊断已完成，启用处方面板
                if (hasConsultation)
                {
                    IsPrescriptionEnabled = true;
                }

                // OpenSpec: refactor-medicalcase-ui - 加载医案备注
                Remark = result.detail?.Remark ?? string.Empty;

                Logger.LogInformation("医案数据加载完成");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "加载医案数据失败：MedicalCaseId={MedicalCaseId}", MedicalCaseId);
                await ShowErrorMessageAsync($"加载医案数据失败：{ex.Message}");
            }
            finally
            {
                SetIsBusy(false);
            }
        }

        /// <summary>
        /// 初始化子面板ViewModel
        /// </summary>
        private void InitializeChildViewModels()
        {
            // 设置子面板ViewModel（通过构造函数注入）
            ConsultationPanelViewModel = _injectedConsultationPanelViewModel;
            PrescriptionPanelViewModel = _injectedPrescriptionPanelViewModel;

            // 初始化子面板（传入MedicalCaseId）
            ConsultationPanelViewModel?.Initialize(MedicalCaseId, null);

            // PrescriptionPanelViewModel 需要异步初始化，在数据加载后调用
            // 此处先设置基础信息，药材数据将在后续加载
            _ = PrescriptionPanelViewModel?.InitializeAsync(
                MedicalCaseId,
                CurrentPatient?.Id ?? Guid.Empty,
                CurrentPatient?.Name ?? string.Empty,
                null);

            // OpenSpec: clarify-cancel-consultation-logic - 注册活跃医案服务
            _activeConsultationService.Register(MedicalCaseId, HandleLeaveRequestAsync);

            Logger.LogInformation("子面板ViewModel初始化完成，MedicalCaseId: {MedicalCaseId}", MedicalCaseId);
        }

        public override bool IsNavigationTarget(NavigationContext navigationContext)
        {
            return false; // 允许重复导航（新的医案流程）
        }

        public override void OnNavigatedFrom(NavigationContext navigationContext)
        {
            // OpenSpec: clarify-cancel-consultation-logic - 注销活跃医案服务
            _activeConsultationService.Unregister();

            base.OnNavigatedFrom(navigationContext);
            Logger.LogInformation("离开MedicalCaseWorkspaceView");
        }

        #endregion

        #region 事件处理

        /// <summary>
        /// 诊断完成事件处理
        /// </summary>
        private void OnConsultationCompleted(ConsultationCompletedPayload payload)
        {
            Logger.LogInformation("接收到ConsultationCompletedEvent，MedicalCaseId: {MedicalCaseId}", payload.MedicalCaseId);

            // 更新诊断状态
            UpdateConsultationStatus(true);

            // 根据是否需要开方启用处方面板
            IsPrescriptionEnabled = payload.NeedsPrescription;

            if (payload.NeedsPrescription)
            {
                UpdatePrescriptionStatus(false, "待开方");
            }
            else
            {
                UpdatePrescriptionStatus(false, "无需开方");
                CanComplete = true;
            }
        }

        /// <summary>
        /// 处方完成事件处理
        /// </summary>
        private void OnPrescriptionCompleted(PrescriptionCompletedPayload payload)
        {
            Logger.LogInformation("接收到PrescriptionCompletedEvent，PrescriptionId: {PrescriptionId}", payload.PrescriptionId);

            // 更新处方状态
            UpdatePrescriptionStatus(true);

            // 启用打印和完成按钮
            CanPrintPrescription = true;
            CanComplete = true;
        }

        /// <summary>
        /// 生命周期操作完成事件处理
        /// </summary>
        private async void OnLifecycleActionCompleted(object? sender, LifecycleActionCompletedEventArgs e)
        {
            Logger.LogInformation("生命周期操作完成：{Action}, 成功: {Success}", e.Action, e.Success);

            if (!e.Success)
            {
                await ShowErrorMessageAsync(e.ErrorMessage ?? "操作失败");
            }
        }

        /// <summary>
        /// 数据加载完成事件处理
        /// </summary>
        private async void OnDataLoaded(object? sender, DataLoadedEventArgs e)
        {
            Logger.LogInformation("数据加载完成：MedicalCaseId: {MedicalCaseId}, 成功: {Success}", e.MedicalCaseId, e.Success);

            if (!e.Success)
            {
                await ShowErrorMessageAsync(e.ErrorMessage ?? "数据加载失败");
            }
        }

        #endregion

        #region 状态更新

        /// <summary>
        /// 更新诊断状态
        /// </summary>
        private void UpdateConsultationStatus(bool isCompleted)
        {
            if (isCompleted)
            {
                ConsultationStatusText = "已完成";
                ConsultationStatusColor = new SolidColorBrush(Color.FromRgb(76, 175, 80)); // 绿色
            }
            else
            {
                ConsultationStatusText = "未完成";
                ConsultationStatusColor = new SolidColorBrush(Color.FromRgb(255, 152, 0)); // 橙色
            }
        }

        /// <summary>
        /// 更新处方状态
        /// </summary>
        private void UpdatePrescriptionStatus(bool isCompleted, string? customText = null)
        {
            ShowPrescriptionStatus = true;

            if (isCompleted)
            {
                PrescriptionStatusText = "已完成";
                PrescriptionStatusBackground = new SolidColorBrush(Color.FromRgb(76, 175, 80)); // 绿色
                PrescriptionStatusSummary = "已开方";
                PrescriptionStatusSummaryColor = new SolidColorBrush(Color.FromRgb(76, 175, 80));
            }
            else
            {
                PrescriptionStatusText = customText ?? "待开方";
                PrescriptionStatusBackground = new SolidColorBrush(Color.FromRgb(158, 158, 158)); // 灰色
                PrescriptionStatusSummary = customText ?? "待开方";
                PrescriptionStatusSummaryColor = new SolidColorBrush(Color.FromRgb(158, 158, 158));
            }
        }

        #endregion

        #region IDisposable

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                // OpenSpec: clarify-cancel-consultation-logic - 确保注销活跃医案服务
                _activeConsultationService.Unregister();

                _lifecycleHandler.ActionCompleted -= OnLifecycleActionCompleted;
                _dataLoader.DataLoaded -= OnDataLoaded;

                Logger.LogInformation("MedicalCaseWorkspaceViewModel已释放资源");
            }

            base.Dispose(disposing);
        }

        #endregion
    }
}
