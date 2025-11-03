using LYBT.Desktop.Infrastructure.Events;
using LYBT.Desktop.Infrastructure.Interfaces;
using LYBT.Desktop.MedicalCase.Components; // Issue #1783: 添加Component命名空间
using LYBT.Desktop.MedicalCase.Interfaces;
using LYBT.Desktop.MedicalCase.Models;
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

                    // 更新步骤名称文本
                    UpdateCurrentStepText();

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
        /// Issue #1567 - 修改判断条件（删除SelectPatient）
        /// </summary>
        public bool CanGoBack => CurrentStep > ConsultationStep.Consultation;

        /// <summary>
        /// 是否可以前进下一步
        /// Issue #1567 - 修改判断条件（删除CompleteMedicalCase改为Completion）
        /// </summary>
        public bool CanGoNext => CurrentStep < ConsultationStep.Completion;

        /// <summary>
        /// 患者信息条是否可见
        /// Issue #1567 - 从Step 1开始就显示（已选中患者）
        /// </summary>
        public bool PatientInfoBarVisible => true;

        /// <summary>
        /// 下一步按钮文字
        /// Issue #1567 - Step 3显示"完成病案"
        /// </summary>
        public string NextButtonText => CurrentStep == ConsultationStep.Completion ? "完成病案" : "下一步";

        /// <summary>
        /// 上一步按钮文字
        /// Issue #1567 - 所有步骤都显示"上一步"
        /// </summary>
        public string PreviousButtonText => "上一步";


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

        #region 私有方法

        /// <summary>
        /// 更新当前步骤名称文本
        /// Issue #1567 - 修改步骤文本（辨证/施治/完成）
        /// </summary>
        private void UpdateCurrentStepText()
        {
            CurrentStepText = CurrentStep switch
            {
                ConsultationStep.Consultation => "辨证",
                ConsultationStep.Prescription => "施治",
                ConsultationStep.Completion => "完成",
                _ => string.Empty
            };
        }

        #endregion

        public DelegateCommand BackToHomeCommand { get; }
        public DelegateCommand PreviousStepCommand { get; }
        public DelegateCommand NextStepCommand { get; }
        public DelegateCommand SaveDraftCommand { get; }
        public DelegateCommand CancelCommand { get; }

        #endregion

        #region 构造函数

        public MedicalCaseFlowViewModel(
            MedicalCaseDataManager dataManager, // Issue #1783: 注入DataManager
            IRegionManager regionManager,
            IEventAggregator eventAggregator,
            ILoggerFactory loggerFactory,
            ISessionManager? sessionManager = null)
            : base(eventAggregator, loggerFactory, regionManager, sessionManager)
        {
            // Issue #1783: 注入DataManager
            _dataManager = dataManager ?? throw new ArgumentNullException(nameof(dataManager));
            _regionManager = regionManager ?? throw new ArgumentNullException(nameof(regionManager));

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

            // 初始化步骤名称文本
            UpdateCurrentStepText();
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
        /// Issue #1567 - 修改判断条件（删除SelectPatient）
        /// </summary>
        private void ExecutePreviousStep()
        {
            if (CurrentStep <= ConsultationStep.Consultation)
            {
                Logger.LogWarning("已是第一步，无法返回");
                return;
            }

            try
            {
                var previousStep = (ConsultationStep)((int)CurrentStep - 1);
                Logger.LogInformation("从 {CurrentStep} 返回到 {PreviousStep}", CurrentStep, previousStep);
                NavigateToStep(previousStep);
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
        /// Issue #1794: 从ExecuteNextStepAsync提取
        /// </summary>
        private async Task CompleteConsultationAsync()
        {
            Logger.LogInformation("完成病案，MedicalCaseId: {MedicalCaseId}", MedicalCaseId);

            try
            {
                SetIsBusy(true, "正在完成病案...");

                // 1. 验证并保存当前步骤数据
                if (!await ValidateAndSaveCurrentStepAsync())
                {
                    return;
                }

                // 2. 更新MedicalCase状态为Completed - Epic #1612修正版
                // Issue #1567 Phase 3 - Task 3.3
                await UpdateMedicalCaseStatusAsync(MedicalCaseStatus.Completed);

                Logger.LogInformation("病案已完成");
                await ShowSuccessMessageAsync("病案已完成");

                // 3. 返回患者选择界面
                _regionManager.RequestNavigate("ContentRegion", "PatientSelectionView");
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
        /// Issue #1794: 从ExecuteNextStepAsync提取
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

                // 2. 跳转到下一步
                var nextStep = (ConsultationStep)((int)CurrentStep + 1);
                Logger.LogInformation("从 {CurrentStep} 前进到 {NextStep}", CurrentStep, nextStep);
                NavigateToStep(nextStep);
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
            // 如果正在处理中，禁用下一步按钮
            if (IsBusy)
            {
                return false;
            }

            // Issue #1567 - 所有步骤都允许前进（数据验证在ExecuteNextStepAsync中处理）
            return CurrentStep switch
            {
                ConsultationStep.Consultation => true, // Step 1: 辨证（可选，允许前进）
                ConsultationStep.Prescription => true, // Step 2: 施治（可选，允许前进）
                ConsultationStep.Completion => true,   // Step 3: 完成确认
                _ => false
            };
        }

        /// <summary>
        /// 暂存医案（保存数据 + 更新状态 + 返回患者选择）
        /// Issue #1567 Phase 3 - Task 3.1
        /// </summary>
        private async void ExecuteSaveDraft()
        {
            try
            {
                Logger.LogInformation("暂存医案，当前步骤：{CurrentStep}, MedicalCaseId: {MedicalCaseId}", CurrentStep, MedicalCaseId);

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

                // 2. 更新MedicalCase状态为Active
                await UpdateMedicalCaseStatusAsync(MedicalCaseStatus.Active);

                Logger.LogInformation("医案暂存成功");
                await ShowSuccessMessageAsync("医案已暂存");

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
        /// Issue #1567 Phase 3 - Task 3.2
        /// </summary>
        private async void ExecuteCancel()
        {
            try
            {
                Logger.LogInformation("取消医案，MedicalCaseId: {MedicalCaseId}", MedicalCaseId);

                // 1. 显示确认对话框
                var confirmed = await ShowConfirmationAsync(
                    "确定要取消本次医案吗？未保存的数据将丢失！",
                    "取消医案");

                if (!confirmed)
                {
                    Logger.LogInformation("用户取消了取消操作");
                    return;
                }

                // 2. 更新MedicalCase状态为Cancelled - Epic #1612修正版
                await UpdateMedicalCaseStatusAsync(MedicalCaseStatus.Cancelled);

                Logger.LogInformation("医案已取消");

                // 3. 返回患者选择界面
                _regionManager.RequestNavigate("ContentRegion", "PatientSelectionView");
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
        /// 创建MedicalCase（Task #1501 - Step 1 → Step 2 自动创建）
        /// Phase 2: 实现真实API调用
        /// Issue #1794: 优化方法长度（70→48行），提取验证、DTO构建、错误处理逻辑
        /// </summary>
        /// <param name="patientId">患者ID</param>
        /// <returns>创建成功返回MedicalCaseId，失败返回Guid.Empty</returns>
        private async Task<Guid> CreateMedicalCaseAsync(Guid patientId)
        {
            try
            {
                Logger.LogInformation("开始创建MedicalCase，PatientId: {PatientId}", patientId);

                // Phase 2: 验证SessionManager和CurrentUser
                if (!ValidateSessionAndUser())
                {
                    return Guid.Empty;
                }

                Logger.LogInformation("✅ SessionManager验证通过，当前用户：{UserName}（ID: {UserId}）",
                    SessionManager!.CurrentUser!.UserName, SessionManager.CurrentUser.Id);

                // Phase 2: 构建MedicalCaseCreateDto
                var createDto = CreateMedicalCaseDto(patientId);

                Logger.LogInformation("📝 准备调用API创建MedicalCase，PatientId: {PatientId}, DoctorId: {DoctorId}, Status: {Status}",
                    createDto.PatientId, createDto.DoctorId, createDto.Status);

                // Issue #1783: 使用DataManager创建MedicalCase
                var createdDto = await _dataManager.CreateAsync(createDto);

                if (createdDto == null)
                {
                    Logger.LogError("❌ DataManager返回null，创建失败");
                    return Guid.Empty;
                }

                Logger.LogInformation("✅ MedicalCase创建成功，ID: {MedicalCaseId}", createdDto.Id);
                return createdDto.Id;
            }
            catch (Exception ex)
            {
                HandleCreateMedicalCaseError(ex, patientId);
                return Guid.Empty;
            }
        }

        /// <summary>
        /// 验证SessionManager和CurrentUser
        /// Issue #1794: 从CreateMedicalCaseAsync提取
        /// </summary>
        private bool ValidateSessionAndUser()
        {
            if (SessionManager == null)
            {
                Logger.LogError("❌ SessionManager为null，无法创建MedicalCase");
                if (UserNotificationService != null)
                {
                    _ = UserNotificationService.ShowErrorAsync("会话管理器未初始化，无法创建医案");
                }
                return false;
            }

            if (SessionManager.CurrentUser == null)
            {
                Logger.LogError("❌ SessionManager.CurrentUser为null，无法创建MedicalCase");
                if (UserNotificationService != null)
                {
                    _ = UserNotificationService.ShowErrorAsync("用户信息丢失，无法创建医案");
                }
                return false;
            }

            return true;
        }

        /// <summary>
        /// 构建MedicalCase创建DTO
        /// Issue #1794: 从CreateMedicalCaseAsync提取
        /// </summary>
        private MedicalCaseCreateDto CreateMedicalCaseDto(Guid patientId)
        {
            return new MedicalCaseCreateDto
            {
                PatientId = patientId,
                DoctorId = SessionManager!.CurrentUser!.Id,
                Status = MedicalCaseStatus.Active,
                Remark = null // 初始创建无备注
            };
        }

        /// <summary>
        /// 处理MedicalCase创建失败
        /// Issue #1794: 从CreateMedicalCaseAsync提取
        /// </summary>
        private void HandleCreateMedicalCaseError(Exception ex, Guid patientId)
        {
            Logger.LogError(ex, "❌ 创建MedicalCase失败，PatientId: {PatientId}，异常类型: {ExceptionType}，消息: {Message}",
                patientId, ex.GetType().Name, ex.Message);

            // 记录详细堆栈
            Logger.LogError("堆栈跟踪：{StackTrace}", ex.StackTrace);

            if (UserNotificationService != null)
            {
                var detailedMessage = $"创建医案失败：{ex.Message}\n\n异常类型：{ex.GetType().Name}";
                _ = UserNotificationService.ShowErrorAsync(detailedMessage);
            }
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

        /// <summary>
        /// 更新MedicalCase状态
        /// Issue #1567 Phase 3 - 支持暂存/取消/完成状态更新
        /// </summary>
        private async Task UpdateMedicalCaseStatusAsync(MedicalCaseStatus newStatus)
        {
            try
            {
                Logger.LogInformation("更新MedicalCase状态，MedicalCaseId: {MedicalCaseId}, 新状态: {NewStatus}",
                    MedicalCaseId, newStatus);

                // 构建更新DTO
                var updateDto = new MedicalCaseUpdateDto
                {
                    Id = MedicalCaseId,
                    Status = newStatus.ToString()
                };

                // Issue #1783: 使用DataManager更新状态
                var updated = await _dataManager.UpdateSimpleAsync(updateDto);

                if (updated == null)
                {
                    throw new InvalidOperationException("DataManager返回null，状态更新失败");
                }

                Logger.LogInformation("MedicalCase状态更新成功，新状态: {NewStatus}", newStatus);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "更新MedicalCase状态失败，MedicalCaseId: {MedicalCaseId}, 目标状态: {NewStatus}",
                    MedicalCaseId, newStatus);
                throw; // 重新抛出异常，让调用方处理
            }
        }

        #endregion

        #region INavigationAware

        public override async void OnNavigatedTo(NavigationContext navigationContext)
        {
            base.OnNavigatedTo(navigationContext);

            // Issue #1567 - 接收从PatientSelectionViewModel传入的参数
            MedicalCaseId = navigationContext.Parameters.GetValue<Guid>("MedicalCaseId");
            CurrentPatient = navigationContext.Parameters.GetValue<PatientDto>("CurrentPatient");

            Logger.LogInformation("进入看病流程，MedicalCaseId: {MedicalCaseId}, 患者: {PatientName}",
                MedicalCaseId, CurrentPatient?.Name);

            // 更新患者信息条
            if (CurrentPatient != null)
            {
                SelectedPatientName = CurrentPatient.Name;
                SelectedPatientInfo = $"{CurrentPatient.Gender} | {CurrentPatient.Age}岁 | {CurrentPatient.PhoneNumber}";

                // Issue #1596 - 新建医案场景：有患者但无MedicalCaseId
                if (MedicalCaseId == Guid.Empty)
                {
                    try
                    {
                        SetIsBusy(true, "正在创建医案...");
                        Logger.LogInformation("新建医案场景，开始创建MedicalCase，PatientId: {PatientId}", CurrentPatient.Id);

                        // 调用CreateMedicalCaseAsync创建医案
                        MedicalCaseId = await CreateMedicalCaseAsync(CurrentPatient.Id);

                        if (MedicalCaseId == Guid.Empty)
                        {
                            Logger.LogError("创建MedicalCase失败，无法继续");
                            await ShowErrorMessageAsync("创建医案失败，请重试");
                            return;
                        }

                        Logger.LogInformation("✅ MedicalCase创建成功，MedicalCaseId: {MedicalCaseId}", MedicalCaseId);
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError(ex, "创建MedicalCase异常");
                        await ShowErrorMessageAsync($"创建医案失败：{ex.Message}");
                        return;
                    }
                    finally
                    {
                        SetIsBusy(false);
                    }
                }
            }
            else
            {
                // 默认情况：导航到当前步骤（初始化时为 SelectPatient）
                // 修复 Issue #1564 Bug：构造函数只设置 CurrentStep，但没有触发 Region 导航
                Logger.LogInformation("执行默认导航到当前步骤：{CurrentStep}", CurrentStep);
                NavigateToStep(CurrentStep);
            }

            // Epic #1583 Phase 3: 继续看诊时加载Consultation和Prescription数据
            if (MedicalCaseId != Guid.Empty)
            {
                try
                {
                    SetIsBusy(true, "正在加载医案数据...");

                    Logger.LogInformation("检测到继续看诊场景，加载医案详情：MedicalCaseId={MedicalCaseId}", MedicalCaseId);

                    // Issue #1783: 使用DataManager加载完整医案数据
                    var medicalCaseDetail = await _dataManager.GetByIdWithDetailsAsync(MedicalCaseId);

                    if (medicalCaseDetail == null)
                    {
                        Logger.LogWarning("未找到医案数据，MedicalCaseId={MedicalCaseId}", MedicalCaseId);
                        await ShowErrorMessageAsync("未找到医案数据");
                        return;
                    }

                    if (medicalCaseDetail.Consultation != null)
                    {
                        Logger.LogInformation("加载到诊疗记录，ConsultationId={ConsultationId}", medicalCaseDetail.Consultation.Id);
                    }
                    else
                    {
                        Logger.LogInformation("无诊疗记录数据");
                    }

                    if (medicalCaseDetail.Prescription != null)
                    {
                        Logger.LogInformation("加载到处方信息，PrescriptionId={PrescriptionId}", medicalCaseDetail.Prescription.Id);
                    }
                    else
                    {
                        Logger.LogInformation("无处方数据");
                    }

                    // 将加载的数据保存到导航参数，供子步骤ViewModel使用
                    navigationContext.Parameters.Add("LoadedConsultation", medicalCaseDetail.Consultation);
                    navigationContext.Parameters.Add("LoadedPrescription", medicalCaseDetail.Prescription);

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

            // 默认导航到Step 1（辨证）
            Logger.LogInformation("执行默认导航到Step 1：辨证");
            NavigateToStep(ConsultationStep.Consultation);
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

        #endregion
    }
}
