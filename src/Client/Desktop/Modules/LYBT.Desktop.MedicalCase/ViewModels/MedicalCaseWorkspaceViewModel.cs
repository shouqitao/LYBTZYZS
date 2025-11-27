using LYBT.Desktop.Infrastructure.Events;
using LYBT.Desktop.Infrastructure.Interfaces;
using LYBT.Desktop.MedicalCase.Components;
using LYBT.Desktop.MedicalCase.Interfaces;
using LYBT.Desktop.MedicalCase.Services;
using LYBT.Desktop.Models.ViewModels.Base;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Shared.Models.Extensions;
using Microsoft.Extensions.Logging;
using Prism.Commands;
using Prism.Events;
using Prism.Regions;
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

        public DelegateCommand BackToPatientSelectionCommand { get; }
        public DelegateCommand CancelConsultationCommand { get; }
        public DelegateCommand SaveDraftCommand { get; }
        public DelegateCommand PrintPrescriptionCommand { get; }
        public DelegateCommand CompleteConsultationCommand { get; }

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
            ISessionManager? sessionManager = null)
            : base(eventAggregator, loggerFactory, regionManager, sessionManager)
        {
            _dataManager = dataManager ?? throw new ArgumentNullException(nameof(dataManager));
            _regionManager = regionManager ?? throw new ArgumentNullException(nameof(regionManager));
            _lifecycleHandler = lifecycleHandler ?? throw new ArgumentNullException(nameof(lifecycleHandler));
            _dataLoader = dataLoader ?? throw new ArgumentNullException(nameof(dataLoader));
            _injectedConsultationPanelViewModel = consultationPanelViewModel ?? throw new ArgumentNullException(nameof(consultationPanelViewModel));
            _injectedPrescriptionPanelViewModel = prescriptionPanelViewModel ?? throw new ArgumentNullException(nameof(prescriptionPanelViewModel));

            // 订阅生命周期事件
            _lifecycleHandler.ActionCompleted += OnLifecycleActionCompleted;
            _dataLoader.DataLoaded += OnDataLoaded;

            // 初始化命令
            BackToPatientSelectionCommand = new DelegateCommand(ExecuteBackToPatientSelection);
            CancelConsultationCommand = new DelegateCommand(ExecuteCancelConsultation);
            SaveDraftCommand = new DelegateCommand(ExecuteSaveDraft);
            PrintPrescriptionCommand = new DelegateCommand(ExecutePrintPrescription, () => CanPrintPrescription)
                .ObservesProperty(() => CanPrintPrescription);
            CompleteConsultationCommand = new DelegateCommand(ExecuteCompleteConsultation, () => CanComplete)
                .ObservesProperty(() => CanComplete);

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

        /// <summary>
        /// 返回患者选择页面
        /// </summary>
        private void ExecuteBackToPatientSelection()
        {
            try
            {
                Logger.LogInformation("返回患者选择页面");
                _regionManager.RequestNavigate("ContentRegion", "PatientSelectionView");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "返回患者选择时发生异常");
            }
        }

        /// <summary>
        /// 取消看诊（OpenSpec: clarify-cancel-consultation-logic）
        /// 语义：作废本次就诊，软删除数据保留供审计
        /// </summary>
        private async void ExecuteCancelConsultation()
        {
            try
            {
                // OpenSpec LIFECYCLE-004: 明确的确认提示
                var confirmed = await ShowConfirmationAsync(
                    "确定要取消本次看诊吗？\n\n" +
                    "取消后，本次就诊记录将被标记为已取消，无法继续编辑。\n" +
                    "如果只是临时离开，请使用「暂停看诊」保存进度。",
                    "取消看诊");

                if (!confirmed)
                {
                    return;
                }

                // OpenSpec LIFECYCLE-003: 取消前自动保存（供审计）
                try
                {
                    // OpenSpec: clarify-cancel-consultation-logic
                    // 医案备注通过ConsultationInputDto.MedicalCaseRemark传递保存
                    if (ConsultationPanelViewModel != null)
                    {
                        ConsultationPanelViewModel.MedicalCaseRemark = Remark;
                    }
                    if (ConsultationPanelViewModel is ISaveable consultationSaveable)
                    {
                        await consultationSaveable.SaveAsync();
                    }
                    if (PrescriptionPanelViewModel is ISaveable prescriptionSaveable)
                    {
                        await prescriptionSaveable.SaveAsync();
                    }
                    Logger.LogDebug("取消前数据已保存（供审计）");
                }
                catch (Exception saveEx)
                {
                    // 保存失败不阻止取消操作
                    Logger.LogWarning(saveEx, "取消前保存失败，继续执行取消操作");
                }

                // 执行软删除
                var result = await _lifecycleHandler.CancelAsync(MedicalCaseId);

                if (result.success)
                {
                    Logger.LogInformation("医案已取消（软删除）");
                    _regionManager.RequestNavigate("ContentRegion", "PatientSelectionView");
                }
                else
                {
                    await ShowErrorMessageAsync(result.errorMessage ?? "取消失败");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "取消看诊失败");
                await ShowErrorMessageAsync($"取消失败：{ex.Message}");
            }
        }

        /// <summary>
        /// 暂停看诊（保存草稿）
        /// </summary>
        private async void ExecuteSaveDraft()
        {
            try
            {
                SetIsBusy(true, "正在保存...");

                // OpenSpec: clarify-cancel-consultation-logic
                // 医案备注通过ConsultationInputDto.MedicalCaseRemark传递保存
                if (ConsultationPanelViewModel != null)
                {
                    ConsultationPanelViewModel.MedicalCaseRemark = Remark;
                }

                // 保存诊断数据（包含医案备注）
                if (ConsultationPanelViewModel is ISaveable consultationSaveable)
                {
                    await consultationSaveable.SaveAsync();
                }

                // 保存处方数据
                if (PrescriptionPanelViewModel is ISaveable prescriptionSaveable)
                {
                    await prescriptionSaveable.SaveAsync();
                }

                // 更新医案状态
                var result = await _lifecycleHandler.SaveDraftAsync(MedicalCaseId);

                if (result.success)
                {
                    await ShowSuccessMessageAsync("医案已暂存");
                    Logger.LogInformation("医案暂存成功");
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
                    _regionManager.RequestNavigate("ContentRegion", "PatientSelectionView");
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

            Logger.LogInformation("进入4:6看诊界面，MedicalCaseId: {MedicalCaseId}, 患者: {PatientName}",
                MedicalCaseId, CurrentPatient?.Name);

            // 初始化患者信息
            await InitializePatientInfoAsync();

            // 加载医案数据
            await LoadMedicalCaseDataAsync();

            // 初始化子面板ViewModel
            InitializeChildViewModels();
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

            Logger.LogInformation("子面板ViewModel初始化完成，MedicalCaseId: {MedicalCaseId}", MedicalCaseId);
        }

        public override bool IsNavigationTarget(NavigationContext navigationContext)
        {
            return false; // 允许重复导航（新的医案流程）
        }

        public override void OnNavigatedFrom(NavigationContext navigationContext)
        {
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

        /// <summary>
        /// 保存医案备注（已弃用）
        /// OpenSpec: clarify-cancel-consultation-logic - 改为通过ConsultationInputDto.MedicalCaseRemark保存
        /// </summary>
        [Obsolete("医案备注现在通过UpdateConsultationAsync保存，不再需要单独调用此方法")]
        private async Task SaveRemarkAsync()
        {
            if (_dataLoader.CachedMedicalCase == null || MedicalCaseId == Guid.Empty)
            {
                return;
            }

            try
            {
                // 同步备注到缓存
                _dataLoader.CachedMedicalCase.Remark = Remark;

                // 使用扩展方法转换为InputDto并保存
                var inputDto = _dataLoader.CachedMedicalCase.ToInputDto();
                await _dataManager.UpdateSimpleAsync(inputDto);

                Logger.LogDebug("医案备注已保存: {MedicalCaseId}", MedicalCaseId);
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "保存医案备注失败（非致命）: {MedicalCaseId}", MedicalCaseId);
                // 不抛出异常，备注保存失败不应阻断主流程
            }
        }

        #endregion

        #region IDisposable

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _lifecycleHandler.ActionCompleted -= OnLifecycleActionCompleted;
                _dataLoader.DataLoaded -= OnDataLoaded;

                Logger.LogInformation("MedicalCaseWorkspaceViewModel已释放资源");
            }

            base.Dispose(disposing);
        }

        #endregion
    }
}
