using LYBT.Desktop.Infrastructure.Events;
using LYBT.Desktop.Infrastructure.Interfaces;
using LYBT.Desktop.MedicalCase.Components; // Issue #1783: 添加Component命名空间
using LYBT.Desktop.MedicalCase.Interfaces;
using LYBT.Desktop.MedicalCase.Models;
using LYBT.Desktop.MedicalCase.Services; // Issue #1806: 引入组件EventArgs类型
using LYBT.Desktop.Models.ViewModels.Base;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Logging;
using Prism.Commands;
using Prism.Events;
using Prism.Ioc;
using Prism.Regions;

namespace LYBT.Desktop.MedicalCase.ViewModels
{
    /// <summary>
    /// 医案流程主视图ViewModel
    /// Issue #1567 - 管理3步看病流程：辨证 → 施治 → 完成
    /// 患者选择已独立化为PatientSelectionView
    /// </summary>
    public class MedicalCaseFlowViewModel : UnifiedViewModelBase
    {
        #region 字段

        private readonly IRegionManager _regionManager;
        // Issue #1783: 使用DataManager替代直接Repository访问
        private readonly MedicalCaseDataManager _dataManager;

        // Issue #1806: 注入组件化服务（Epic #1805 Phase 2）
        private readonly MedicalCaseFlowManager _flowManager;
        private readonly MedicalCaseLifecycleHandler _lifecycleHandler;
        private readonly MedicalCaseDataLoader _dataLoader;

        #endregion

        #region 属性

        private ConsultationStep _currentStep = ConsultationStep.Consultation;
        /// <summary>
        /// 当前流程步骤
        /// Issue #1567 - 重构为ConsultationStep（删除患者选择）
        /// </summary>
        public ConsultationStep CurrentStep
        {
            get => _currentStep;
            set
            {
                if (SetProperty(ref _currentStep, value))
                {
                    RaisePropertyChanged(nameof(CanGoBack));
                    RaisePropertyChanged(nameof(CanGoNext));
                    RaisePropertyChanged(nameof(PatientInfoBarVisible));
                    RaisePropertyChanged(nameof(NextButtonText));
                    RaisePropertyChanged(nameof(PreviousButtonText));

                    // Issue #1806: UpdateCurrentStepText已删除,由FlowManager的StepChanged事件处理

                    PreviousStepCommand.RaiseCanExecuteChanged();
                    NextStepCommand.RaiseCanExecuteChanged();
                }
            }
        }

        private ViewModelBase? _currentStepViewModel;
        /// <summary>
        /// 当前步骤的ViewModel（用于ContentControl绑定）
        /// </summary>
        public ViewModelBase? CurrentStepViewModel
        {
            get => _currentStepViewModel;
            set => SetProperty(ref _currentStepViewModel, value);
        }

        private string _selectedPatientName = string.Empty;
        /// <summary>
        /// 已选患者姓名（Step 2-4显示在患者信息条）
        /// </summary>
        public string SelectedPatientName
        {
            get => _selectedPatientName;
            set => SetProperty(ref _selectedPatientName, value);
        }

        private string _selectedPatientInfo = string.Empty;
        /// <summary>
        /// 已选患者信息（性别/年龄/电话）
        /// </summary>
        public string SelectedPatientInfo
        {
            get => _selectedPatientInfo;
            set => SetProperty(ref _selectedPatientInfo, value);
        }

        private Guid _medicalCaseId = Guid.Empty;
        /// <summary>
        /// 当前医案ID（患者选择后自动创建）
        /// </summary>
        public Guid MedicalCaseId
        {
            get => _medicalCaseId;
            set => SetProperty(ref _medicalCaseId, value);
        }

        private PatientDto? _currentPatient;
        /// <summary>
        /// 当前选择的患者信息（用于传递给子步骤ViewModel）
        /// </summary>
        public PatientDto? CurrentPatient
        {
            get => _currentPatient;
            set => SetProperty(ref _currentPatient, value);
        }

        /// <summary>
        /// 是否可以返回上一步
        /// Issue #1806: 委托给FlowManager
        /// </summary>
        public bool CanGoBack => _flowManager?.CanGoBack ?? false;

        /// <summary>
        /// 是否可以前进下一步
        /// Issue #1806: 委托给FlowManager
        /// </summary>
        public bool CanGoNext => _flowManager?.CanGoNext ?? false;

        /// <summary>
        /// 患者信息条是否可见
        /// Issue #1567 - 从Step 1开始就显示（已选中患者）
        /// </summary>
        public bool PatientInfoBarVisible => true;

        /// <summary>
        /// 下一步按钮文字
        /// Issue #1806: 委托给FlowManager
        /// </summary>
        public string NextButtonText => _flowManager?.NextButtonText ?? "下一步";

        /// <summary>
        /// 上一步按钮文字
        /// Issue #1806: 委托给FlowManager
        /// </summary>
        public string PreviousButtonText => _flowManager?.PreviousButtonText ?? "上一步";


        /// <summary>
        /// 当前步骤名称文本
        /// </summary>
        private string _currentStepText = "患者选择";
        public string CurrentStepText
        {
            get => _currentStepText;
            set => SetProperty(ref _currentStepText, value);
        }

        #endregion

        #region 命令

        // Issue #1806: UpdateCurrentStepText已删除,由FlowManager的StepChanged事件处理步骤文本更新

        public DelegateCommand BackToHomeCommand { get; }
        public DelegateCommand PreviousStepCommand { get; }
        public DelegateCommand NextStepCommand { get; }
        public DelegateCommand SaveDraftCommand { get; }
        public DelegateCommand CancelCommand { get; }

        #endregion

        #region 构造函数

        public MedicalCaseFlowViewModel(
            MedicalCaseDataManager dataManager, // Issue #1783: 注入DataManager
            MedicalCaseFlowManager flowManager, // Issue #1806: 注入FlowManager
            MedicalCaseLifecycleHandler lifecycleHandler, // Issue #1806: 注入LifecycleHandler
            MedicalCaseDataLoader dataLoader, // Issue #1806: 注入DataLoader
            IRegionManager regionManager,
            IEventAggregator eventAggregator,
            ILoggerFactory loggerFactory,
            ISessionManager? sessionManager = null)
            : base(eventAggregator, loggerFactory, regionManager, sessionManager)
        {
            // Issue #1783: 注入DataManager
            _dataManager = dataManager ?? throw new ArgumentNullException(nameof(dataManager));
            _regionManager = regionManager ?? throw new ArgumentNullException(nameof(regionManager));

            // Issue #1806: 注入组件化服务并订阅事件
            _flowManager = flowManager ?? throw new ArgumentNullException(nameof(flowManager));
            _lifecycleHandler = lifecycleHandler ?? throw new ArgumentNullException(nameof(lifecycleHandler));
            _dataLoader = dataLoader ?? throw new ArgumentNullException(nameof(dataLoader));

            // 订阅组件事件
            _flowManager.StepChanged += OnStepChanged;
            _lifecycleHandler.ActionCompleted += OnLifecycleActionCompleted;
            _dataLoader.DataLoaded += OnDataLoaded;

            // 初始化命令
            BackToHomeCommand = new DelegateCommand(ExecuteBackToHome);
            PreviousStepCommand = new DelegateCommand(ExecutePreviousStep, CanExecutePreviousStep);
            NextStepCommand = new DelegateCommand(async () => await ExecuteNextStepAsync(), CanExecuteNextStep)
                .ObservesProperty(() => CurrentPatient)  // 监听CurrentPatient变化
                .ObservesProperty(() => IsBusy);         // 监听IsBusy变化
            SaveDraftCommand = new DelegateCommand(ExecuteSaveDraft);
            CancelCommand = new DelegateCommand(ExecuteCancel);

            // Issue #1562 Phase 1: 已删除ConsultationCompletedEvent订阅（工作流机制）

            // Issue #1557 Phase 4: 订阅处方完成事件
            EventAggregator.GetEvent<PrescriptionCompletedEvent>()
                .Subscribe(OnPrescriptionCompleted, ThreadOption.UIThread);

            // Issue #1567: 删除PatientSelectedEvent订阅（患者选择已独立化）

            Logger.LogInformation("MedicalCaseFlowViewModel已初始化，当前步骤：{CurrentStep}", CurrentStep);

            // Issue #1806: 步骤名称文本由FlowManager自动管理,无需手动初始化
        }

        #endregion

        #region 命令实现

        /// <summary>
        /// 返回患者选择页面
        /// Issue #1595: 修复导航错误（之前导航到主页，现在正确导航到患者列表）
        /// </summary>
        private void ExecuteBackToHome()
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
        /// 上一步
        /// Issue #1806: 委托给FlowManager处理步骤切换
        /// </summary>
        private void ExecutePreviousStep()
        {
            try
            {
                // 委托给FlowManager处理步骤回退
                if (_flowManager.MoveToPreviousStep())
                {
                    // FlowManager已更新CurrentStep并触发StepChanged事件
                    // OnStepChanged会自动同步ViewModel状态和导航到新步骤
                    NavigateToStep(_flowManager.CurrentStep);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "执行上一步时发生异常");
            }
        }

        private bool CanExecutePreviousStep()
        {
            return CanGoBack;
        }

        /// <summary>
        /// 下一步
        /// Issue #1567 - 删除MedicalCase创建逻辑（移至PatientSelectionViewModel）
        /// Issue #1794: 优化方法长度（98→46行），提取验证保存、完成、下一步逻辑
        /// </summary>
        private async Task ExecuteNextStepAsync()
        {
            if (CurrentStep >= ConsultationStep.Completion)
            {
                await CompleteConsultationAsync();
                return;
            }

            await ProcessNormalNextStepAsync();
        }

        /// <summary>
        /// 验证并保存当前步骤
        /// Issue #1794: 从ExecuteNextStepAsync提取重复验证保存逻辑
        /// </summary>
        private async Task<bool> ValidateAndSaveCurrentStepAsync()
        {
            // 1. 验证当前步骤
            if (CurrentStepViewModel is IValidatable validatable)
            {
                Logger.LogInformation("验证Step {CurrentStep}数据", CurrentStep);
                if (!validatable.Validate())
                {
                    Logger.LogWarning("Step {CurrentStep}验证失败：{Message}", CurrentStep, validatable.ValidationMessage);
                    await ShowErrorMessageAsync(validatable.ValidationMessage);
                    return false;
                }
            }

            // 2. 保存当前步骤
            if (CurrentStepViewModel is ISaveable saveable)
            {
                Logger.LogInformation("保存Step {CurrentStep}数据", CurrentStep);
                var saveResult = await saveable.SaveAsync();
                if (!saveResult)
                {
                    Logger.LogWarning("Step {CurrentStep}保存失败", CurrentStep);
                    await ShowErrorMessageAsync("保存失败，请检查数据后重试");
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// 完成病案流程
        /// Issue #1806: 委托给LifecycleHandler处理完成
        /// </summary>
        private async Task CompleteConsultationAsync()
        {
            try
            {
                SetIsBusy(true, "正在完成病案...");

                // 1. 验证并保存当前步骤数据
                if (!await ValidateAndSaveCurrentStepAsync())
                {
                    return;
                }

                // 2. 委托给LifecycleHandler更新MedicalCase状态为Completed
                var result = await _lifecycleHandler.CompleteAsync(MedicalCaseId);

                if (result.success)
                {
                    // 成功消息已在OnLifecycleActionCompleted事件处理中显示
                    Logger.LogInformation("病案已完成");

                    // 3. 返回患者选择界面
                    _regionManager.RequestNavigate("ContentRegion", "PatientSelectionView");
                }
                else
                {
                    await ShowErrorMessageAsync(result.errorMessage ?? "完成失败");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "完成病案失败");
                await ShowErrorMessageAsync($"完成失败：{ex.Message}");
            }
            finally
            {
                SetIsBusy(false);
            }
        }

        /// <summary>
        /// 处理普通下一步流程
        /// Issue #1806: 委托给FlowManager处理步骤前进
        /// </summary>
        private async Task ProcessNormalNextStepAsync()
        {
            try
            {
                SetIsBusy(true, "正在处理...");

                // 1. 验证并保存当前步骤
                if (!await ValidateAndSaveCurrentStepAsync())
                {
                    return;
                }

                // 2. 委托给FlowManager跳转到下一步
                if (_flowManager.MoveToNextStep())
                {
                    // FlowManager已更新CurrentStep并触发StepChanged事件
                    // OnStepChanged会自动同步ViewModel状态和导航到新步骤
                    NavigateToStep(_flowManager.CurrentStep);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "执行下一步时发生异常");
                await ShowErrorMessageAsync($"操作失败：{ex.Message}");
            }
            finally
            {
                SetIsBusy(false);
            }
        }

        private bool CanExecuteNextStep()
        {
            // Issue #1806: 委托给FlowManager验证是否可以执行下一步
            return _flowManager?.ValidateCanExecuteNext(IsBusy) ?? false;
        }

        /// <summary>
        /// 暂存医案（保存数据 + 更新状态）
        /// Issue #1806: 委托给LifecycleHandler处理暂存
        /// </summary>
        private async void ExecuteSaveDraft()
        {
            try
            {
                SetIsBusy(true, "正在保存...");

                // 1. 调用当前Step的ISaveable接口保存数据
                if (CurrentStepViewModel is ISaveable saveable)
                {
                    var success = await saveable.SaveAsync();
                    if (!success)
                    {
                        Logger.LogWarning("当前步骤数据保存失败");
                        await ShowErrorMessageAsync("保存失败，请检查数据");
                        return;
                    }
                }

                // 2. 委托给LifecycleHandler更新MedicalCase状态为Active
                var result = await _lifecycleHandler.SaveDraftAsync(MedicalCaseId);

                if (result.success)
                {
                    // 成功消息已在OnLifecycleActionCompleted事件处理中显示
                    Logger.LogInformation("医案暂存成功");
                }
                else
                {
                    await ShowErrorMessageAsync(result.errorMessage ?? "暂存失败");
                }

                // Epic #1583 Phase 4: 移除自动导航，暂存后停留在当前界面（修复Issue #1569）
                // 用户可以通过"返回主页"按钮手动返回
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
        /// 取消医案（确认对话框 + 更新状态 + 返回患者选择）
        /// Issue #1806: 委托给LifecycleHandler处理取消
        /// </summary>
        private async void ExecuteCancel()
        {
            try
            {
                // 1. 显示确认对话框
                var confirmed = await ShowConfirmationAsync(
                    "确定要取消本次医案吗？未保存的数据将丢失！",
                    "取消医案");

                if (!confirmed)
                {
                    Logger.LogInformation("用户取消了取消操作");
                    return;
                }

                // 2. 委托给LifecycleHandler更新MedicalCase状态为Cancelled
                var result = await _lifecycleHandler.CancelAsync(MedicalCaseId);

                if (result.success)
                {
                    // 成功消息已在OnLifecycleActionCompleted事件处理中显示
                    Logger.LogInformation("医案已取消");

                    // 3. 返回患者选择界面
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
        }

        #endregion

        #region 辅助方法

        /// <summary>
        /// 创建MedicalCase
        /// Issue #1806: 委托给LifecycleHandler处理创建
        /// </summary>
        /// <param name="patientId">患者ID</param>
        /// <returns>创建成功返回MedicalCaseId，失败返回Guid.Empty</returns>
        private async Task<Guid> CreateMedicalCaseAsync(Guid patientId)
        {
            // 委托给LifecycleHandler创建MedicalCase
            var result = await _lifecycleHandler.CreateMedicalCaseAsync(patientId);

            if (!result.success)
            {
                // 错误消息已在OnLifecycleActionCompleted事件处理中显示
                Logger.LogError("创建MedicalCase失败：{ErrorMessage}", result.errorMessage);
            }

            return result.medicalCaseId;
        }

        /// <summary>
        /// 执行Region导航
        /// </summary>
        private void NavigateToRegion(string stepName, string viewName, NavigationParameters parameters)
        {
            Logger.LogInformation("导航到{StepName}步骤（使用Region导航）", stepName);
            _regionManager.RequestNavigate("WorkflowContentRegion", viewName, parameters);
            Logger.LogInformation("Region导航到{ViewName}，MedicalCaseId: {MedicalCaseId}", viewName, MedicalCaseId);
        }

        /// <summary>
        /// 导航到指定步骤
        /// Issue #1567 - 删除SelectPatient分支，更新步骤枚举
        /// </summary>
        private void NavigateToStep(ConsultationStep step)
        {
            CurrentStep = step;

            switch (step)
            {
                case ConsultationStep.Consultation:
                    NavigateToRegion("辨证", "ConsultationFormView", new NavigationParameters
                    {
                        { "MedicalCaseId", MedicalCaseId },
                        { "CurrentPatient", CurrentPatient }
                    });
                    break;

                case ConsultationStep.Prescription:
                    NavigateToRegion("施治", "PrescriptionEditorView", new NavigationParameters
                    {
                        { "MedicalCaseId", MedicalCaseId },
                        { "CurrentPatient", CurrentPatient }
                    });
                    break;

                case ConsultationStep.Completion:
                    NavigateToRegion("完成", "CompletionView", new NavigationParameters
                    {
                        { "MedicalCaseId", MedicalCaseId }
                    });
                    break;

                default:
                    Logger.LogWarning("未知步骤：{Step}", step);
                    break;
            }
        }

        // Issue #1806: UpdateMedicalCaseStatusAsync已删除,由LifecycleHandler处理所有状态更新

        #endregion

        #region INavigationAware

        /// <summary>
        /// 导航进入时处理
        /// Issue #1794: 优化方法长度（115→28行），提取患者初始化和数据加载逻辑
        /// </summary>
        public override async void OnNavigatedTo(NavigationContext navigationContext)
        {
            base.OnNavigatedTo(navigationContext);

            // Issue #1567 - 接收从PatientSelectionViewModel传入的参数
            MedicalCaseId = navigationContext.Parameters.GetValue<Guid>("MedicalCaseId");
            CurrentPatient = navigationContext.Parameters.GetValue<PatientDto>("CurrentPatient");

            Logger.LogInformation("进入看病流程，MedicalCaseId: {MedicalCaseId}, 患者: {PatientName}",
                MedicalCaseId, CurrentPatient?.Name);

            // 初始化患者信息和医案
            var initializationFailed = await InitializePatientInfoAsync();
            if (initializationFailed)
            {
                return;
            }

            // 加载继续看诊的医案数据
            await LoadMedicalCaseDetailsAsync(navigationContext);

            // 默认导航到Step 1（辨证）
            Logger.LogInformation("执行默认导航到Step 1：辨证");
            NavigateToStep(ConsultationStep.Consultation);
        }

        /// <summary>
        /// 初始化患者信息和医案
        /// Issue #1806: 委托给DataLoader格式化患者信息
        /// </summary>
        private async Task<bool> InitializePatientInfoAsync()
        {
            if (CurrentPatient != null)
            {
                // Issue #1806: 委托给DataLoader格式化患者信息
                var (patientName, patientInfo) = _dataLoader.FormatPatientInfo(CurrentPatient);
                SelectedPatientName = patientName;
                SelectedPatientInfo = patientInfo;

                // Issue #1596 - 新建医案场景：有患者但无MedicalCaseId
                if (MedicalCaseId == Guid.Empty)
                {
                    try
                    {
                        SetIsBusy(true, "正在创建医案...");

                        // Issue #1806: 委托给LifecycleHandler创建医案
                        MedicalCaseId = await CreateMedicalCaseAsync(CurrentPatient.Id);

                        if (MedicalCaseId == Guid.Empty)
                        {
                            Logger.LogError("创建MedicalCase失败，无法继续");
                            await ShowErrorMessageAsync("创建医案失败，请重试");
                            return true; // 失败，需要中止
                        }

                        Logger.LogInformation(" MedicalCase创建成功，MedicalCaseId: {MedicalCaseId}", MedicalCaseId);
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError(ex, "创建MedicalCase异常");
                        await ShowErrorMessageAsync($"创建医案失败：{ex.Message}");
                        return true; // 失败，需要中止
                    }
                    finally
                    {
                        SetIsBusy(false);
                    }
                }
            }
            else
            {
                // 默认情况：导航到当前步骤
                Logger.LogInformation("执行默认导航到当前步骤：{CurrentStep}", CurrentStep);
                NavigateToStep(CurrentStep);
            }

            return false; // 成功
        }

        /// <summary>
        /// 加载继续看诊的医案数据
        /// Issue #1806: 委托给DataLoader处理数据加载
        /// </summary>
        private async Task LoadMedicalCaseDetailsAsync(NavigationContext navigationContext)
        {
            // Epic #1583 Phase 3: 继续看诊时加载Consultation和Prescription数据
            if (MedicalCaseId == Guid.Empty)
            {
                return;
            }

            try
            {
                SetIsBusy(true, "正在加载医案数据...");

                // Issue #1806: 委托给DataLoader加载医案数据
                var result = await _dataLoader.LoadMedicalCaseDetailsAsync(MedicalCaseId);

                if (!result.success)
                {
                    // 错误消息已在OnDataLoaded事件处理中显示
                    Logger.LogWarning("加载医案数据失败：{ErrorMessage}", result.errorMessage);
                    return;
                }

                // 将加载的数据保存到导航参数，供子步骤ViewModel使用
                navigationContext.Parameters.Add("LoadedConsultation", _dataLoader.CachedConsultation);
                navigationContext.Parameters.Add("LoadedPrescription", _dataLoader.CachedPrescription);

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

        public override bool IsNavigationTarget(NavigationContext navigationContext)
        {
            // 允许重复导航（新的医案流程）
            return false;
        }

        public override void OnNavigatedFrom(NavigationContext navigationContext)
        {
            base.OnNavigatedFrom(navigationContext);
            Logger.LogInformation("离开MedicalCaseFlowView，当前步骤：{CurrentStep}", CurrentStep);
        }

        #endregion

        #region 事件处理方法

        // Issue #1567: 删除OnPatientSelected方法（患者选择已独立化，直接通过OnNavigatedTo接收参数）

        // Issue #1562 Phase 1: 已删除 OnConsultationCompleted（工作流事件处理）

        /// <summary>
        /// 处方完成事件处理方法
        /// Issue #1557 Phase 4 - 订阅PrescriptionCompletedEvent，接收PrescriptionEditorViewModel发布的事件
        /// Issue #1567 - 修改为跳转到Step 3（完成病案）
        /// </summary>
        private async void OnPrescriptionCompleted(PrescriptionCompletedPayload payload)
        {
            try
            {
                Logger.LogInformation("接收到PrescriptionCompletedEvent，PrescriptionId: {PrescriptionId}, 药材总数: {TotalItems}, 总金额: {TotalAmount:F2}",
                    payload.PrescriptionId, payload.TotalItems, payload.TotalAmount);

                // 自动触发下一步：跳转到Step 3（完成病案）
                await ExecuteNextStepAsync();

                Logger.LogInformation("处方完成事件处理完成，准备跳转到Step 3");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "处理PrescriptionCompletedEvent失败");
                await ShowErrorMessageAsync($"处理处方完成失败：{ex.Message}");
            }
        }

        /// <summary>
        /// 流程步骤变更事件处理
        /// Issue #1806: 响应MedicalCaseFlowManager的StepChanged事件
        /// </summary>
        private void OnStepChanged(object? sender, StepChangedEventArgs e)
        {
            Logger.LogInformation("流程步骤已变更：{PreviousStep} → {CurrentStep}", e.PreviousStep, e.CurrentStep);

            // 同步CurrentStep属性（触发UI更新）
            CurrentStep = e.CurrentStep;

            // 更新步骤文本
            CurrentStepText = e.StepText;

            // 刷新命令状态
            PreviousStepCommand.RaiseCanExecuteChanged();
            NextStepCommand.RaiseCanExecuteChanged();
        }

        /// <summary>
        /// 生命周期操作完成事件处理
        /// Issue #1806: 响应MedicalCaseLifecycleHandler的ActionCompleted事件
        /// </summary>
        private async void OnLifecycleActionCompleted(object? sender, LifecycleActionCompletedEventArgs e)
        {
            Logger.LogInformation("生命周期操作完成：{Action}, 成功: {Success}", e.Action, e.Success);

            if (e.Success)
            {
                var message = e.Action switch
                {
                    LifecycleAction.Create => $"医案创建成功，ID: {e.MedicalCaseId}",
                    LifecycleAction.SaveDraft => "医案已暂存",
                    LifecycleAction.Cancel => "医案已取消",
                    LifecycleAction.Complete => "医案已完成",
                    _ => "操作已完成"
                };

                await ShowSuccessMessageAsync(message);
            }
            else
            {
                await ShowErrorMessageAsync(e.ErrorMessage ?? "操作失败");
            }
        }

        /// <summary>
        /// 数据加载完成事件处理
        /// Issue #1806: 响应MedicalCaseDataLoader的DataLoaded事件
        /// </summary>
        private async void OnDataLoaded(object? sender, DataLoadedEventArgs e)
        {
            Logger.LogInformation("数据加载完成：MedicalCaseId: {MedicalCaseId}, 成功: {Success}, 包含诊疗: {HasConsultation}, 包含处方: {HasPrescription}",
                e.MedicalCaseId, e.Success, e.HasConsultation, e.HasPrescription);

            if (!e.Success)
            {
                await ShowErrorMessageAsync(e.ErrorMessage ?? "数据加载失败");
            }
        }

        #endregion

        #region IDisposable

        /// <summary>
        /// 释放资源，取消订阅组件事件
        /// Issue #1806: 实现Dispose模式
        /// </summary>
        /// <summary>
        /// Issue #1806: 释放组件事件订阅
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                // 取消订阅组件事件
                if (_flowManager != null)
                {
                    _flowManager.StepChanged -= OnStepChanged;
                }

                if (_lifecycleHandler != null)
                {
                    _lifecycleHandler.ActionCompleted -= OnLifecycleActionCompleted;
                }

                if (_dataLoader != null)
                {
                    _dataLoader.DataLoaded -= OnDataLoaded;
                }

                Logger.LogInformation("MedicalCaseFlowViewModel已释放资源");
            }

            base.Dispose(disposing);
        }

        #endregion
    }
}
