using LYBT.Desktop.MedicalCase.Interfaces;
using LYBT.Desktop.MedicalCase.Models;
using LYBT.Desktop.Models.ViewModels.Base;
using LYBT.Shared.Models.Contracts.Patients;
using Microsoft.Extensions.Logging;
using Prism.Commands;
using Prism.Events;
using Prism.Ioc;
using Prism.Regions;

namespace LYBT.Desktop.MedicalCase.ViewModels
{
    /// <summary>
    /// 医案流程主视图ViewModel（Epic #1494 - Task #1496）
    /// 管理4步流程：患者选择 → 填写诊断 → 填写处方 → 完成医案
    /// </summary>
    public class MedicalCaseFlowViewModel : UnifiedViewModelBase
    {
        #region 字段

        private readonly IRegionManager _regionManager;
        private readonly IContainerProvider _containerProvider;
        private readonly ILocalStorageService _localStorageService;
        private readonly System.Windows.Threading.DispatcherTimer _autoSaveTimer;

        #endregion

        #region 属性

        private FlowStep _currentStep = FlowStep.SelectPatient;
        /// <summary>
        /// 当前流程步骤
        /// </summary>
        public FlowStep CurrentStep
        {
            get => _currentStep;
            set
            {
                if (SetProperty(ref _currentStep, value))
                {
                    RaisePropertyChanged(nameof(CanGoBack));
                    RaisePropertyChanged(nameof(CanGoNext));
                    RaisePropertyChanged(nameof(PatientInfoBarVisible));
                    RaisePropertyChanged(nameof(IsStep1));
                    RaisePropertyChanged(nameof(IsStep2));
                    RaisePropertyChanged(nameof(IsStep3));
                    RaisePropertyChanged(nameof(IsStep4));
                    RaisePropertyChanged(nameof(NextButtonText));
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
        /// </summary>
        public bool CanGoBack => CurrentStep > FlowStep.SelectPatient;

        /// <summary>
        /// 是否可以前进下一步
        /// </summary>
        public bool CanGoNext => CurrentStep < FlowStep.CompleteMedicalCase;

        /// <summary>
        /// 患者信息条是否可见（Step 2-4显示）
        /// </summary>
        public bool PatientInfoBarVisible => CurrentStep >= FlowStep.FillConsultation;

        /// <summary>
        /// 下一步按钮文字（Step 4显示"完成看诊"）
        /// </summary>
        public string NextButtonText => CurrentStep == FlowStep.CompleteMedicalCase ? "完成看诊" : "下一步";

        // 进度条高亮标记
        public bool IsStep1 => CurrentStep == FlowStep.SelectPatient;
        public bool IsStep2 => CurrentStep == FlowStep.FillConsultation;
        public bool IsStep3 => CurrentStep == FlowStep.FillPrescription;
        public bool IsStep4 => CurrentStep == FlowStep.CompleteMedicalCase;

        #endregion

        #region 命令

        public DelegateCommand BackToHomeCommand { get; }
        public DelegateCommand PreviousStepCommand { get; }
        public DelegateCommand NextStepCommand { get; }
        public DelegateCommand SaveDraftCommand { get; }
        public DelegateCommand CancelCommand { get; }

        #endregion

        #region 构造函数

        public MedicalCaseFlowViewModel(
            IRegionManager regionManager,
            IContainerProvider containerProvider,
            ILocalStorageService localStorageService,
            IEventAggregator eventAggregator,
            ILoggerFactory loggerFactory)
            : base(eventAggregator, loggerFactory, regionManager)
        {
            _regionManager = regionManager ?? throw new ArgumentNullException(nameof(regionManager));
            _containerProvider = containerProvider ?? throw new ArgumentNullException(nameof(containerProvider));
            _localStorageService = localStorageService ?? throw new ArgumentNullException(nameof(localStorageService));

            // 初始化命令
            BackToHomeCommand = new DelegateCommand(ExecuteBackToHome);
            PreviousStepCommand = new DelegateCommand(ExecutePreviousStep, CanExecutePreviousStep);
            NextStepCommand = new DelegateCommand(async () => await ExecuteNextStepAsync(), CanExecuteNextStep);
            SaveDraftCommand = new DelegateCommand(async () => await SaveDraftAsync());
            CancelCommand = new DelegateCommand(ExecuteCancel);

            // 初始化自动保存定时器（Issue #1502 - 每5分钟自动保存）
            _autoSaveTimer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromMinutes(5)
            };
            _autoSaveTimer.Tick += async (s, e) => await AutoSaveTickAsync();
            _autoSaveTimer.Start();

            Logger.LogInformation("MedicalCaseFlowViewModel已初始化，当前步骤：{CurrentStep}，自动保存已启用（5分钟间隔）", CurrentStep);

            // 尝试恢复草稿（Fire-and-Forget）
            _ = RestoreDraftAsync();
        }

        #endregion

        #region 命令实现

        /// <summary>
        /// 返回主页
        /// </summary>
        private void ExecuteBackToHome()
        {
            try
            {
                Logger.LogInformation("返回主页");
                _regionManager.RequestNavigate("ContentRegion", "HomeView");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "返回主页时发生异常");
            }
        }

        /// <summary>
        /// 上一步
        /// </summary>
        private void ExecutePreviousStep()
        {
            if (CurrentStep <= FlowStep.SelectPatient)
            {
                Logger.LogWarning("已是第一步，无法返回");
                return;
            }

            try
            {
                var previousStep = (FlowStep)((int)CurrentStep - 1);
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
        /// 下一步（Task #1501 - 状态机逻辑）
        /// </summary>
        private async Task ExecuteNextStepAsync()
        {
            if (CurrentStep >= FlowStep.CompleteMedicalCase)
            {
                // Step 4: 完成看诊，返回主页
                Logger.LogInformation("完成看诊，MedicalCaseId: {MedicalCaseId}", MedicalCaseId);

                // Issue #1502 - 完成医案后清除草稿
                try
                {
                    await _localStorageService.ClearDraftAsync();
                    Logger.LogInformation("医案已完成，草稿已清除");
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "清除草稿失败");
                }

                ExecuteBackToHome();
                return;
            }

            try
            {
                SetIsBusy(true, "正在处理...");

                // 1. 验证当前步骤
                if (CurrentStepViewModel is IValidatable validatable)
                {
                    Logger.LogInformation("验证Step {CurrentStep}数据", CurrentStep);
                    if (!validatable.Validate())
                    {
                        Logger.LogWarning("Step {CurrentStep}验证失败：{Message}", CurrentStep, validatable.ValidationMessage);
                        await ShowErrorMessageAsync(validatable.ValidationMessage);
                        return;
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
                        return;
                    }
                }

                // 3. 关键步骤自动创建实体
                if (CurrentStep == FlowStep.SelectPatient)
                {
                    // Step 1 → Step 2: 自动创建MedicalCase
                    Logger.LogInformation("Step 1完成，准备创建MedicalCase，患者：{PatientName}", CurrentPatient?.Name);

                    if (CurrentPatient == null)
                    {
                        Logger.LogError("CurrentPatient为空，无法创建MedicalCase");
                        await ShowErrorMessageAsync("患者信息丢失，请重新选择患者");
                        return;
                    }

                    // 创建MedicalCase
                    var medicalCaseId = await CreateMedicalCaseAsync(CurrentPatient.Id);
                    if (medicalCaseId == Guid.Empty)
                    {
                        Logger.LogError("创建MedicalCase失败，PatientId: {PatientId}", CurrentPatient.Id);
                        await ShowErrorMessageAsync("创建医案失败，请重试");
                        return;
                    }

                    MedicalCaseId = medicalCaseId;
                    Logger.LogInformation("MedicalCase创建成功，ID: {MedicalCaseId}, 患者: {PatientName}",
                        MedicalCaseId, CurrentPatient.Name);
                }

                // 4. 跳转到下一步
                var nextStep = (FlowStep)((int)CurrentStep + 1);
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
            // TODO: 添加每个Step的验证逻辑
            // Step 1: 必须选择患者
            // Step 2: Consultation必填字段验证
            // Step 3: Prescription至少1味药材
            return true; // 暂时允许所有步骤前进
        }

        /// <summary>
        /// 保存草稿（Issue #1502 - 手动保存）
        /// </summary>
        private async Task SaveDraftAsync()
        {
            try
            {
                Logger.LogInformation("手动保存草稿，当前步骤：{CurrentStep}, MedicalCaseId: {MedicalCaseId}", CurrentStep, MedicalCaseId);

                var draftState = CollectDraftState();
                if (draftState != null)
                {
                    await _localStorageService.SaveDraftAsync(draftState);
                    await ShowSuccessMessageAsync("草稿已保存");
                    Logger.LogInformation("草稿手动保存成功");
                }
                else
                {
                    Logger.LogWarning("无法收集草稿数据，保存跳过");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "保存草稿时发生异常");
                await ShowErrorMessageAsync("保存草稿失败");
            }
        }

        /// <summary>
        /// 自动保存草稿（Issue #1502 - Timer触发）
        /// </summary>
        private async Task AutoSaveTickAsync()
        {
            try
            {
                // 只在Step 2-3时自动保存（Step 1无需保存，Step 4已完成）
                if (CurrentStep == FlowStep.FillConsultation || CurrentStep == FlowStep.FillPrescription)
                {
                    Logger.LogInformation("自动保存草稿，当前步骤：{CurrentStep}", CurrentStep);

                    var draftState = CollectDraftState();
                    if (draftState != null)
                    {
                        await _localStorageService.SaveDraftAsync(draftState);
                        Logger.LogInformation("草稿自动保存成功");
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "自动保存草稿时发生异常");
            }
        }

        /// <summary>
        /// 收集当前流程状态为草稿数据
        /// </summary>
        private FlowDraftState? CollectDraftState()
        {
            // 只有Step 2-3需要保存草稿
            if (CurrentStep < FlowStep.FillConsultation)
            {
                Logger.LogInformation("Step 1无需保存草稿");
                return null;
            }

            var state = new FlowDraftState
            {
                CurrentStep = CurrentStep,
                PatientId = CurrentPatient?.Id,
                MedicalCaseId = MedicalCaseId == Guid.Empty ? null : MedicalCaseId,
                SavedAt = DateTime.Now
            };

            // TODO: 从CurrentStepViewModel收集表单数据
            // 由于ConsultationFormViewModel和PrescriptionEditorViewModel可能在不同模块，
            // 这里使用反射或接口方式获取数据
            // 暂时留空，后续完善

            return state;
        }

        /// <summary>
        /// 恢复草稿（Issue #1502 - 启动时恢复）
        /// </summary>
        private async Task RestoreDraftAsync()
        {
            try
            {
                var draft = await _localStorageService.LoadDraftAsync();
                if (draft == null)
                {
                    Logger.LogInformation("无草稿需要恢复");
                    return;
                }

                Logger.LogInformation("发现草稿，保存时间：{SavedAt}, CurrentStep: {CurrentStep}", draft.SavedAt, draft.CurrentStep);

                // TODO: Issue #1502 - 弹出RestoreDraftDialog询问用户是否恢复
                // 当前暂时自动恢复草稿（MVP简化）
                // await ShowRestoreDraftDialog(draft);

                // 暂时跳过草稿恢复，等待RestoreDraftDialog实现
                Logger.LogInformation("草稿恢复功能待完善（需要RestoreDraftDialog）");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "恢复草稿时发生异常");
            }
        }

        /// <summary>
        /// 取消流程
        /// </summary>
        private void ExecuteCancel()
        {
            try
            {
                Logger.LogInformation("取消医案流程，MedicalCaseId: {MedicalCaseId}", MedicalCaseId);
                // TODO: 确认对话框（是否放弃当前编辑？）
                ExecuteBackToHome();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "取消流程时发生异常");
            }
        }

        #endregion

        #region 辅助方法

        /// <summary>
        /// 创建MedicalCase（Task #1501 - Step 1 → Step 2 自动创建）
        /// </summary>
        /// <param name="patientId">患者ID</param>
        /// <returns>创建成功返回MedicalCaseId，失败返回Guid.Empty</returns>
        private async Task<Guid> CreateMedicalCaseAsync(Guid patientId)
        {
            try
            {
                Logger.LogInformation("开始创建MedicalCase，PatientId: {PatientId}", patientId);

                // TODO: Task #1497实现后，调用真实API创建MedicalCase
                // var request = new CreateMedicalCaseRequest
                // {
                //     PatientId = patientId,
                //     DoctorId = _sessionManager.CurrentUser.Id,
                //     VisitDate = DateTime.Now
                // };
                // var response = await _medicalCaseRepository.CreateAsync(request);
                // return response.Id;

                // 临时模拟：返回新GUID
                await Task.Delay(500); // 模拟网络延迟
                var medicalCaseId = Guid.NewGuid();
                Logger.LogInformation("MedicalCase创建成功（模拟），ID: {MedicalCaseId}", medicalCaseId);
                return medicalCaseId;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "创建MedicalCase失败，PatientId: {PatientId}", patientId);
                return Guid.Empty;
            }
        }

        /// <summary>
        /// 导航到指定步骤
        /// </summary>
        private void NavigateToStep(FlowStep step)
        {
            CurrentStep = step;

            // TODO: Task #1497-#1500 - 创建各个Step的View后，实现真实导航
            // 当前使用占位ViewModel
            switch (step)
            {
                case FlowStep.SelectPatient:
                    Logger.LogInformation("导航到患者选择步骤");
                    // Task #1497: 创建PatientSelectionViewModel实例
                    var patientSelectionViewModel = _containerProvider.Resolve<PatientSelectionViewModel>();

                    // 监听患者选择事件
                    patientSelectionViewModel.PatientSelected += async (sender, selectedPatient) =>
                    {
                        Logger.LogInformation("患者选择事件触发，患者：{PatientName}（ID: {PatientId}）",
                            selectedPatient.Name, selectedPatient.Id);

                        // 更新CurrentPatient
                        CurrentPatient = selectedPatient;

                        // 更新患者信息条
                        SelectedPatientName = selectedPatient.Name;
                        SelectedPatientInfo = $"{selectedPatient.Gender} | {selectedPatient.Age}岁 | {selectedPatient.PhoneNumber}";

                        // 触发NextStep，自动创建MedicalCase并跳转到Step 2
                        await ExecuteNextStepAsync();
                    };

                    CurrentStepViewModel = patientSelectionViewModel;
                    Logger.LogInformation("PatientSelectionViewModel已创建");
                    break;

                case FlowStep.FillConsultation:
                    Logger.LogInformation("导航到诊断录入步骤");
                    // Task #1498: 创建ConsultationFormViewModel实例（来自LYBT.Desktop.Consultation模块）
                    // 使用动态解析避免循环引用
                    var consultationFormViewModelType = Type.GetType("LYBT.Desktop.Consultation.ViewModels.ConsultationFormViewModel, LYBT.Desktop.Consultation");
                    if (consultationFormViewModelType != null)
                    {
                        var consultationFormViewModel = _containerProvider.Resolve(consultationFormViewModelType) as ViewModelBase;
                        if (consultationFormViewModel != null)
                        {
                            // 使用反射设置属性
                            var currentPatientProperty = consultationFormViewModelType.GetProperty("CurrentPatient");
                            var medicalCaseIdProperty = consultationFormViewModelType.GetProperty("MedicalCaseId");

                            currentPatientProperty?.SetValue(consultationFormViewModel, CurrentPatient);
                            medicalCaseIdProperty?.SetValue(consultationFormViewModel, MedicalCaseId);

                            CurrentStepViewModel = consultationFormViewModel;
                            Logger.LogInformation("ConsultationFormViewModel已创建，MedicalCaseId: {MedicalCaseId}", MedicalCaseId);
                        }
                        else
                        {
                            Logger.LogError("无法将ConsultationFormViewModel转换为ViewModelBase");
                            CurrentStepViewModel = null;
                        }
                    }
                    else
                    {
                        Logger.LogError("无法加载ConsultationFormViewModel类型");
                        CurrentStepViewModel = null;
                    }
                    break;

                case FlowStep.FillPrescription:
                    Logger.LogInformation("导航到处方编辑步骤");
                    // Task #1499: 创建PrescriptionEditorViewModel实例
                    var prescriptionEditorViewModel = _containerProvider.Resolve<PrescriptionEditorViewModel>();
                    prescriptionEditorViewModel.CurrentPatient = CurrentPatient;
                    prescriptionEditorViewModel.MedicalCaseId = MedicalCaseId;
                    CurrentStepViewModel = prescriptionEditorViewModel;
                    Logger.LogInformation("PrescriptionEditorViewModel已创建，MedicalCaseId: {MedicalCaseId}", MedicalCaseId);
                    break;

                case FlowStep.CompleteMedicalCase:
                    Logger.LogInformation("导航到完成医案步骤");

                    // Task #1500 - 创建CompletionViewModel实例
                    var completionVM = _containerProvider.Resolve<CompletionViewModel>();

                    // 初始化（异步调用，Fire-and-Forget模式）
                    // TODO: 改进为async/await模式以更好地处理异常
                    _ = completionVM.InitializeAsync(MedicalCaseId);

                    CurrentStepViewModel = completionVM;
                    break;

                default:
                    Logger.LogWarning("未知步骤：{Step}", step);
                    break;
            }
        }

        #endregion

        #region INavigationAware

        public override void OnNavigatedTo(NavigationContext navigationContext)
        {
            base.OnNavigatedTo(navigationContext);

            // 从HomeView传来的参数
            var startStep = navigationContext.Parameters.GetValue<int>("StartStep");
            if (startStep > 0 && startStep <= 4)
            {
                Logger.LogInformation("接收到StartStep参数：{StartStep}", startStep);
                NavigateToStep((FlowStep)startStep);
            }

            var searchKeyword = navigationContext.Parameters.GetValue<string>("SearchKeyword");
            if (!string.IsNullOrEmpty(searchKeyword))
            {
                Logger.LogInformation("接收到SearchKeyword参数：{SearchKeyword}", searchKeyword);
                // TODO: 在Step 1中预填搜索关键字
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
    }
}
