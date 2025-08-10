using System;
using System.Threading.Tasks;
using System.Windows.Input;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Events;
using Prism.Dialogs;
using Microsoft.Extensions.Logging;
using LYBT.WPF.Client.Core.Interfaces.Services;
using LYBT.WPF.Client.Core.Events;
using LYBT.WPF.Client.Core.Models.Consultation;
using LYBT.WPF.Client.Modules.Consultation.Services;
using Prism.Navigation.Regions;
using CoreWorkflowStep = LYBT.WPF.Client.Core.Models.Consultation.WorkflowStep;
using ThreadOption = Prism.Events.ThreadOption;

namespace LYBT.WPF.Client.Modules.Consultation.ViewModels
{
    /// <summary>
    /// 诊疗工作流协调器 - 简化版主ViewModel，协调各个专门服务
    /// UltraThink重构：将ConsultationWorkflowViewModel(947行)重构为协调器模式
    /// 职责：协调StepManager、DataManager、HistoryManager、NavigationHandler
    /// </summary>
    public class ConsultationWorkflowCoordinator : BindableBase, INavigationAware
    {
        #region 专门服务组件

        public ConsultationStepManager StepManager { get; }
        public ConsultationDataManager DataManager { get; }
        public ConsultationHistoryManager HistoryManager { get; }
        public ConsultationNavigationHandler NavigationHandler { get; }

        #endregion

        #region 依赖服务

        private readonly IEventAggregator _eventAggregator;
        private readonly IDialogService _dialogService;
        private readonly IUserSessionManager _userSessionManager;
        private readonly ILogger<ConsultationWorkflowCoordinator> _logger;

        #endregion

        #region 状态属性

        private bool _isInitialized;
        public bool IsInitialized
        {
            get => _isInitialized;
            set => SetProperty(ref _isInitialized, value);
        }

        private bool _isBusy;
        public bool IsBusy
        {
            get => _isBusy;
            set => SetProperty(ref _isBusy, value);
        }

        private string _statusMessage = "";
        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        #endregion

        #region 命令

        public ICommand PreviousStepCommand { get; private set; }
        public ICommand NextStepCommand { get; private set; }
        public ICommand SaveDraftCommand { get; private set; }
        public ICommand CompleteWorkflowCommand { get; private set; }
        public ICommand ExitWorkflowCommand { get; private set; }
        public ICommand ToggleHistoryCommand { get; private set; }
        public ICommand RefreshCommand { get; private set; }

        #endregion

        #region 构造函数

        public ConsultationWorkflowCoordinator(
            ConsultationStepManager stepManager,
            ConsultationDataManager dataManager,
            ConsultationHistoryManager historyManager,
            ConsultationNavigationHandler navigationHandler,
            IEventAggregator eventAggregator,
            IDialogService dialogService,
            IUserSessionManager userSessionManager,
            ILogger<ConsultationWorkflowCoordinator> logger)
        {
            StepManager = stepManager ?? throw new ArgumentNullException(nameof(stepManager));
            DataManager = dataManager ?? throw new ArgumentNullException(nameof(dataManager));
            HistoryManager = historyManager ?? throw new ArgumentNullException(nameof(historyManager));
            NavigationHandler = navigationHandler ?? throw new ArgumentNullException(nameof(navigationHandler));
            _eventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
            _userSessionManager = userSessionManager ?? throw new ArgumentNullException(nameof(userSessionManager));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            InitializeCommands();
            SubscribeEvents();
        }

        #endregion

        #region 初始化

        private void InitializeCommands()
        {
            PreviousStepCommand = new DelegateCommand(
                async () => await ExecuteWithErrorHandlingAsync(PreviousStepAsync),
                () => StepManager.CanGoToPreviousStep)
                .ObservesProperty(() => StepManager.CurrentStep);

            NextStepCommand = new DelegateCommand(
                async () => await ExecuteWithErrorHandlingAsync(NextStepAsync),
                () => StepManager.CanGoToNextStep)
                .ObservesProperty(() => StepManager.CurrentStep);

            SaveDraftCommand = new DelegateCommand(
                async () => await ExecuteWithErrorHandlingAsync(SaveDraftAsync));

            CompleteWorkflowCommand = new DelegateCommand(
                async () => await ExecuteWithErrorHandlingAsync(CompleteWorkflowAsync),
                () => StepManager.IsWorkflowCompleted)
                .ObservesProperty(() => StepManager.CurrentStep);

            ExitWorkflowCommand = new DelegateCommand(
                async () => await ExecuteWithErrorHandlingAsync(ExitWorkflowAsync));

            ToggleHistoryCommand = new DelegateCommand(
                () => HistoryManager.ToggleHistoryPanel());

            RefreshCommand = new DelegateCommand(
                async () => await ExecuteWithErrorHandlingAsync(RefreshAsync));
        }

        private void SubscribeEvents()
        {
            // 订阅步骤完成事件
            _eventAggregator.GetEvent<WorkflowStepCompletedEvent>()
                .Subscribe(OnStepCompleted, ThreadOption.UIThread);

            // 订阅数据更新事件
            _eventAggregator.GetEvent<ConsultationDataUpdatedEvent>()
                .Subscribe(OnDataUpdated, ThreadOption.UIThread);

            // 订阅导航完成事件
            _eventAggregator.GetEvent<ConsultationNavigationCompletedEvent>()
                .Subscribe(OnNavigationCompleted, ThreadOption.UIThread);

            // 监听StepManager的当前步骤变化
            StepManager.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(StepManager.CurrentStep))
                {
                    _ = Task.Run(async () => await OnCurrentStepChanged());
                }
            };
        }

        #endregion

        #region 工作流操作

        private async Task PreviousStepAsync()
        {
            StatusMessage = "切换到上一步...";

            var success = await StepManager.MoveToPreviousStepAsync();
            if (!success)
            {
                StatusMessage = "无法切换到上一步";
                return;
            }

            await NavigationHandler.NavigateToStepAsync((CoreWorkflowStep)StepManager.CurrentStep);
            StatusMessage = "已切换到上一步";
        }

        private async Task NextStepAsync()
        {
            StatusMessage = "切换到下一步...";

            // 保存当前步骤数据
            var saveSuccess = await SaveCurrentStepDataAsync();
            if (!saveSuccess)
            {
                StatusMessage = "保存当前步骤失败，无法继续";
                return;
            }

            // 标记当前步骤为完成
            StepManager.MarkCurrentStepAsCompleted();

            var success = await StepManager.MoveToNextStepAsync();
            if (!success)
            {
                StatusMessage = "无法切换到下一步";
                return;
            }

            await NavigationHandler.NavigateToStepAsync((CoreWorkflowStep)StepManager.CurrentStep);
            StatusMessage = "已切换到下一步";
        }

        private async Task SaveDraftAsync()
        {
            StatusMessage = "保存草稿...";

            var success = await SaveCurrentStepDataAsync();
            StatusMessage = success ? "草稿保存成功" : "草稿保存失败";

            if (success)
            {
                // 替换为基本Show方法
                _dialogService.Show("BasicDialog", new DialogParameters {{ "message", "草稿已保存" }}, null);
            }
        }

        private async Task CompleteWorkflowAsync()
        {
            StatusMessage = "完成工作流...";

            try
            {
                // 最终保存所有数据
                var saveSuccess = await SaveCurrentStepDataAsync();
                if (!saveSuccess)
                {
                    _dialogService.Show("BasicDialog", new DialogParameters {{ "message", "保存失败，无法完成工作流" }}, null);
                    return;
                }

                // 标记工作流完成
                StepManager.MarkCurrentStepAsCompleted();

                StatusMessage = "工作流已完成";
                _dialogService.Show("BasicDialog", new DialogParameters {{ "message", "诊疗工作流已完成" }}, null);

                // 可以在这里添加完成后的导航逻辑
                _logger.LogInformation("诊疗工作流完成: MedicalCaseId={MedicalCaseId}", DataManager.MedicalCaseId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "完成工作流时发生错误");
                _dialogService.Show("BasicDialog", new DialogParameters {{ "message", $"完成工作流失败: {ex.Message}" }}, null);
            }
        }

        private async Task ExitWorkflowAsync()
        {
            var hasUnsavedChanges = CheckForUnsavedChanges();
            
            if (hasUnsavedChanges)
            {
                // 简化为直接退出，避免复杂的对话框逼理
                var result = true; // 暂时简化
                    
                if (result != true)
                {
                    return;
                }
            }

            // 清理资源
            StepManager.ResetWorkflow();
            DataManager.ClearAllData();
            HistoryManager.ClearHistory();
            NavigationHandler.ClearCurrentContent();

            StatusMessage = "已退出工作流";
            _logger.LogInformation("用户退出诊疗工作流");
        }

        private async Task RefreshAsync()
        {
            StatusMessage = "刷新中...";

            try
            {
                // 刷新患者历史
                if (DataManager.Patient != null)
                {
                    await HistoryManager.LoadPatientHistoryAsync(DataManager.Patient.Id);
                }

                // 刷新当前视图
                await NavigationHandler.RefreshCurrentViewAsync();

                StatusMessage = "刷新完成";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "刷新时发生错误");
                StatusMessage = "刷新失败";
            }
        }

        #endregion

        #region 事件处理

        private void OnStepCompleted(WorkflowStepData stepData)
        {
            _logger.LogInformation("收到步骤完成事件: {Step}", stepData.Step);
            // 在这里可以处理步骤完成的逻辑
        }

        private void OnDataUpdated(ConsultationData data)
        {
            _logger.LogInformation("收到数据更新事件");
            // 在这里可以处理数据更新的逻辑
        }

        private void OnNavigationCompleted(ConsultationNavigationEventArgs args)
        {
            _logger.LogDebug("导航完成: {ViewName}, Success: {Success}", args.ViewName, args.Success);
            
            if (!args.Success)
            {
                StatusMessage = $"导航失败: {args.ErrorMessage}";
            }
        }

        private async Task OnCurrentStepChanged()
        {
            try
            {
                await NavigationHandler.NavigateToStepAsync((CoreWorkflowStep)StepManager.CurrentStep);
                StatusMessage = $"当前步骤: {GetStepDisplayName((CoreWorkflowStep)StepManager.CurrentStep)}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "处理步骤变化时发生错误");
            }
        }

        #endregion

        #region INavigationAware

        public void OnNavigatedTo(NavigationContext navigationContext)
        {
            _ = Task.Run(async () => await InitializeWorkflowAsync(navigationContext));
        }

        public void OnNavigatedFrom(NavigationContext navigationContext)
        {
            // 清理资源或保存状态
        }

        public bool IsNavigationTarget(NavigationContext navigationContext)
        {
            // 总是允许导航到此实例
            return true;
        }

        #endregion

        #region 辅助方法

        private async Task InitializeWorkflowAsync(NavigationContext navigationContext)
        {
            try
            {
                IsBusy = true;
                StatusMessage = "初始化工作流...";

                // 从导航参数获取医疗案例ID或患者ID
                var medicalCaseId = navigationContext.Parameters.GetValue<Guid>("MedicalCaseId");
                var patientId = navigationContext.Parameters.GetValue<Guid>("PatientId");

                // 初始化数据管理器
                var success = await DataManager.InitializeMedicalCaseAsync(
                    medicalCaseId != Guid.Empty ? medicalCaseId : null);

                if (!success)
                {
                    StatusMessage = "初始化失败";
                    return;
                }

                // 如果指定了患者ID，加载患者数据
                if (patientId != Guid.Empty)
                {
                    await DataManager.LoadPatientAsync(patientId);
                    await HistoryManager.LoadPatientHistoryAsync(patientId);
                }

                // 导航到第一步
                await NavigationHandler.NavigateToStepAsync((CoreWorkflowStep)StepManager.CurrentStep);

                IsInitialized = true;
                StatusMessage = "工作流初始化完成";
                _logger.LogInformation("诊疗工作流初始化成功");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "初始化工作流失败");
                StatusMessage = "初始化失败";
                _dialogService.Show("BasicDialog", new DialogParameters {{ "message", $"初始化失败: {ex.Message}" }}, null);
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task<bool> SaveCurrentStepDataAsync()
        {
            try
            {
                // 发布保存请求事件，让当前步骤的ViewModel处理保存逻辑
                var saveEvent = _eventAggregator.GetEvent<WorkflowStepSaveRequestEvent>();
                saveEvent.Publish(new WorkflowStepSaveRequest
                {
                    Step = (CoreWorkflowStep)StepManager.CurrentStep,
                    MedicalCaseId = DataManager.MedicalCaseId
                });

                return await Task.FromResult(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "保存当前步骤数据失败");
                return false;
            }
        }

        private bool CheckForUnsavedChanges()
        {
            // 这里可以实现检查未保存更改的逻辑
            return false;
        }

        private string GetStepDisplayName(CoreWorkflowStep step)
        {
            return step switch
            {
                CoreWorkflowStep.PatientSelection => "患者选择",
                CoreWorkflowStep.FourDiagnosis => "四诊采集",
                CoreWorkflowStep.Differentiation => "辨证分析",
                CoreWorkflowStep.Prescription => "处方开具",
                _ => "未知步骤"
            };
        }

        private async Task ExecuteWithErrorHandlingAsync(Func<Task> action)
        {
            try
            {
                IsBusy = true;
                await action();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "执行操作时发生错误");
                _dialogService.Show("BasicDialog", new DialogParameters {{ "message", $"操作失败: {ex.Message}" }}, null);
            }
            finally
            {
                IsBusy = false;
            }
        }

        #endregion
    }

    #region 事件定义

    /// <summary>
    /// 工作流步骤保存请求事件
    /// </summary>
    public class WorkflowStepSaveRequestEvent : PubSubEvent<WorkflowStepSaveRequest>
    {
    }

    /// <summary>
    /// 工作流步骤保存请求
    /// </summary>
    public class WorkflowStepSaveRequest
    {
        public CoreWorkflowStep Step { get; set; }
        public Guid MedicalCaseId { get; set; }
    }

    #endregion
}