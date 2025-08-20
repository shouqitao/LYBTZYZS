using LYBT.Shared.Models.Contracts.Common;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Media;
using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;
using Microsoft.Extensions.Logging;
using LYBT.Desktop.Core.Events;
using LYBT.Desktop.Core.Interfaces.Services;
using LYBT.Shared.Interfaces.Services;

using LYBT.Desktop.Core.Models.Consultation;
namespace LYBT.Desktop.Consultation.ViewModels
{
    /// <summary>
    /// 工作流导航器视图模型
    /// 提供增强的步骤导航、状态管理和验证功能
    /// </summary>
    public class WorkflowNavigatorViewModel : BindableBase
    {
        private readonly IEventAggregator _eventAggregator;
        private readonly ICustomDialogService _dialogService;
        private readonly ILogger<WorkflowNavigatorViewModel> _logger;

        #region 属性

        private ObservableCollection<WorkflowStepViewModel> _steps;
        public ObservableCollection<WorkflowStepViewModel> Steps
        {
            get => _steps;
            set => SetProperty(ref _steps, value);
        }

        private WorkflowStepViewModel? _currentStep;
        public WorkflowStepViewModel? CurrentStep
        {
            get => _currentStep;
            set
            {
                if (SetProperty(ref _currentStep, value))
                {
                    UpdateStepStates();
                    UpdateProgress();
                }
            }
        }

        private double _progress;
        public double Progress
        {
            get => _progress;
            set => SetProperty(ref _progress, value);
        }

        private bool _isNavigating;
        public bool IsNavigating
        {
            get => _isNavigating;
            set => SetProperty(ref _isNavigating, value);
        }

        #endregion

        #region 命令

        public DelegateCommand<WorkflowStepViewModel> NavigateToStepCommand { get; }
        public DelegateCommand NavigateNextCommand { get; }
        public DelegateCommand NavigatePreviousCommand { get; }
        public DelegateCommand ValidateCurrentStepCommand { get; }
        public DelegateCommand SaveProgressCommand { get; }

        #endregion

        public WorkflowNavigatorViewModel(
            IEventAggregator eventAggregator,
            ICustomDialogService dialogService,
            ILogger<WorkflowNavigatorViewModel> logger)
        {
            _steps = new ObservableCollection<WorkflowStepViewModel>();
            _eventAggregator = eventAggregator;
            _dialogService = dialogService;
            _logger = logger;

            // 初始化步骤
            InitializeSteps();

            // 初始化命令
            NavigateToStepCommand = new DelegateCommand<WorkflowStepViewModel>(
                async (step) => await NavigateToStepAsync(step),
                CanNavigateToStep);
            
            NavigateNextCommand = new DelegateCommand(
                async () => await NavigateNextAsync(),
                CanNavigateNext);
            
            NavigatePreviousCommand = new DelegateCommand(
                async () => await NavigatePreviousAsync(),
                CanNavigatePrevious);
            
            ValidateCurrentStepCommand = new DelegateCommand(
                async () => await ValidateCurrentStepAsync());
            
            SaveProgressCommand = new DelegateCommand(
                async () => await SaveProgressAsync());

            // 订阅事件
            SubscribeEvents();
        }

        private void InitializeSteps()
        {
            Steps = new ObservableCollection<WorkflowStepViewModel>
            {
                new WorkflowStepViewModel
                {
                    StepIndex = 0,
                    StepType = WorkflowStep.PatientSelection,
                    Title = "患者选择",
                    Description = "选择或创建患者",
                    IconKind = "Account",
                    IconColor = "#2196F3",
                    ShortcutKey = "Alt+1",
                    IsCompleted = false,
                    IsCurrent = true,
                    IsLocked = false,
                    ShowConnector = true
                },
                new WorkflowStepViewModel
                {
                    StepIndex = 1,
                    StepType = WorkflowStep.FourDiagnosis,
                    Title = "四诊采集",
                    Description = "望闻问切信息录入",
                    IconKind = "Stethoscope",
                    IconColor = "#4CAF50",
                    ShortcutKey = "Alt+2",
                    IsCompleted = false,
                    IsCurrent = false,
                    IsLocked = true,
                    ShowConnector = true
                },
                new WorkflowStepViewModel
                {
                    StepIndex = 2,
                    StepType = WorkflowStep.Differentiation,
                    Title = "辨证分析",
                    Description = "证型分析与治则",
                    IconKind = "Brain",
                    IconColor = "#FF9800",
                    ShortcutKey = "Alt+3",
                    IsCompleted = false,
                    IsCurrent = false,
                    IsLocked = true,
                    ShowConnector = true
                },
                new WorkflowStepViewModel
                {
                    StepIndex = 3,
                    StepType = WorkflowStep.Prescription,
                    Title = "处方开具",
                    Description = "开具中药处方",
                    IconKind = "Pill",
                    IconColor = "#9C27B0",
                    ShortcutKey = "Alt+4",
                    IsCompleted = false,
                    IsCurrent = false,
                    IsLocked = true,
                    ShowConnector = false
                }
            };

            CurrentStep = Steps.First();
        }

        private void SubscribeEvents()
        {
            // 订阅步骤完成事件
            _eventAggregator.GetEvent<WorkflowStepCompletedEvent>()
                .Subscribe(OnStepCompleted);

            // 订阅步骤验证事件
            _eventAggregator.GetEvent<StepValidationRequestEvent>()
                .Subscribe(OnStepValidationRequested);
        }

        private void OnStepCompleted(WorkflowStepData stepData)
        {
            var step = Steps.FirstOrDefault(s => s.StepType == stepData.Step);
            if (step != null)
            {
                step.IsCompleted = true;
                step.CompletionTime = DateTime.Now;
                
                // 解锁下一步
                var nextStep = GetNextStep(step);
                if (nextStep != null)
                {
                    nextStep.IsLocked = false;
                }

                UpdateProgress();
                _logger.LogInformation($"步骤 {step.Title} 已完成");
            }
        }

        private void OnStepValidationRequested(WorkflowStep step)
        {
            _ = ValidateStepAsync(step);
        }

        private async Task NavigateToStepAsync(WorkflowStepViewModel targetStep)
        {
            if (targetStep == null || targetStep.IsLocked)
                return;

            try
            {
                IsNavigating = true;

                // 验证当前步骤
                if (CurrentStep != null && !CurrentStep.IsCompleted)
                {
                    var canLeave = await ValidateCurrentStepAsync();
                    if (!canLeave)
                    {
                        await _dialogService.ShowWarningAsync(
                            "请先完成当前步骤的必填信息",
                            "无法切换步骤");
                        return;
                    }
                }

                // 保存当前步骤数据
                if (CurrentStep != null)
                {
                    _eventAggregator.GetEvent<SaveStepDataEvent>()
                        .Publish(CurrentStep.StepType);
                }

                // 切换步骤
                if (CurrentStep != null)
                    CurrentStep.IsCurrent = false;
                
                targetStep.IsCurrent = true;
                CurrentStep = targetStep;

                // 发布导航事件
                _eventAggregator.GetEvent<NavigateToStepEvent>()
                    .Publish(targetStep.StepType);

                _logger.LogInformation($"导航到步骤: {targetStep.Title}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "导航到步骤失败");
                await _dialogService.ShowErrorAsync(
                    $"切换步骤失败: {ex.Message}",
                    "错误");
            }
            finally
            {
                IsNavigating = false;
            }
        }

        private bool CanNavigateToStep(WorkflowStepViewModel step)
        {
            return step != null && !step.IsLocked && !IsNavigating;
        }

        private async Task NavigateNextAsync()
        {
            var nextStep = GetNextStep(CurrentStep);
            if (nextStep != null)
            {
                await NavigateToStepAsync(nextStep);
            }
            else
            {
                // 已是最后一步，完成工作流
                await CompleteWorkflowAsync();
            }
        }

        private bool CanNavigateNext()
        {
            return CurrentStep != null && !IsNavigating;
        }

        private async Task NavigatePreviousAsync()
        {
            var previousStep = GetPreviousStep(CurrentStep);
            if (previousStep != null)
            {
                await NavigateToStepAsync(previousStep);
            }
        }

        private bool CanNavigatePrevious()
        {
            return CurrentStep != null && 
                   CurrentStep.StepIndex > 0 && 
                   !IsNavigating;
        }

        private WorkflowStepViewModel? GetNextStep(WorkflowStepViewModel? currentStep)
        {
            if (currentStep == null) return null;
            
            var nextIndex = currentStep.StepIndex + 1;
            return Steps.FirstOrDefault(s => s.StepIndex == nextIndex);
        }

        private WorkflowStepViewModel? GetPreviousStep(WorkflowStepViewModel? currentStep)
        {
            if (currentStep == null) return null;
            
            var previousIndex = currentStep.StepIndex - 1;
            return Steps.FirstOrDefault(s => s.StepIndex == previousIndex);
        }

        private async Task<bool> ValidateCurrentStepAsync()
        {
            if (CurrentStep == null)
                return false;

            // 发布验证请求
            var validationRequest = new StepValidationRequest
            {
                Step = CurrentStep.StepType,
                RequestId = Guid.NewGuid()
            };

            var validationResult = await Task.Run(() =>
            {
                // 这里应该等待验证响应
                // 暂时返回true
                return true;
            });

            return validationResult;
        }

        private async Task ValidateStepAsync(WorkflowStep step)
        {
            var stepVm = Steps.FirstOrDefault(s => s.StepType == step);
            if (stepVm != null)
            {
                // 执行验证逻辑
                await Task.Delay(100); // 模拟验证
                
                _logger.LogInformation($"步骤 {stepVm.Title} 验证完成");
            }
        }

        private void UpdateStepStates()
        {
            foreach (var step in Steps)
            {
                step.IsCurrent = step == CurrentStep;
            }
        }

        private void UpdateProgress()
        {
            var completedCount = Steps.Count(s => s.IsCompleted);
            var totalCount = Steps.Count;
            
            Progress = totalCount > 0 ? (completedCount * 100.0 / totalCount) : 0;
        }

        private async Task SaveProgressAsync()
        {
            try
            {
                // 保存所有步骤的进度
                foreach (var step in Steps.Where(s => !s.IsCompleted))
                {
                    _eventAggregator.GetEvent<SaveStepDataEvent>()
                        .Publish(step.StepType);
                }

                await _dialogService.ShowInformationAsync(
                    "诊疗进度已保存",
                    "保存成功");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "保存进度失败");
                await _dialogService.ShowErrorAsync(
                    $"保存失败: {ex.Message}",
                    "错误");
            }
        }

        private async Task CompleteWorkflowAsync()
        {
            try
            {
                // 验证所有步骤是否完成
                var incompleteSteps = Steps.Where(s => !s.IsCompleted).ToList();
                if (incompleteSteps.Any())
                {
                    var stepNames = string.Join("、", incompleteSteps.Select(s => s.Title));
                    await _dialogService.ShowWarningAsync(
                        $"以下步骤尚未完成：{stepNames}",
                        "无法完成诊疗");
                    return;
                }

                // 发布工作流完成事件
                _eventAggregator.GetEvent<WorkflowCompletedEvent>()
                    .Publish(new Core.Events.WorkflowCompletionData
                    {
                        CompletionTime = DateTime.Now,
                        TotalDuration = CalculateTotalDuration()
                    });

                await _dialogService.ShowInformationAsync(
                    "诊疗流程已完成，处方已生成",
                    "完成");

                _logger.LogInformation("诊疗工作流完成");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "完成工作流失败");
                await _dialogService.ShowErrorAsync(
                    $"完成诊疗失败: {ex.Message}",
                    "错误");
            }
        }

        private TimeSpan CalculateTotalDuration()
        {
            var firstStep = Steps.FirstOrDefault(s => s.CompletionTime.HasValue);
            var lastStep = Steps.LastOrDefault(s => s.CompletionTime.HasValue);

            if (firstStep != null && lastStep != null && 
                firstStep.CompletionTime.HasValue && lastStep.CompletionTime.HasValue)
            {
                return lastStep.CompletionTime.Value - firstStep.CompletionTime.Value;
            }

            return TimeSpan.Zero;
        }
    }

    /// <summary>
    /// 工作流步骤视图模型
    /// </summary>
    public class WorkflowStepViewModel : BindableBase
    {
        private bool _isCompleted;
        public bool IsCompleted
        {
            get => _isCompleted;
            set => SetProperty(ref _isCompleted, value);
        }

        private bool _isCurrent;
        public bool IsCurrent
        {
            get => _isCurrent;
            set => SetProperty(ref _isCurrent, value);
        }

        private bool _isLocked;
        public bool IsLocked
        {
            get => _isLocked;
            set => SetProperty(ref _isLocked, value);
        }

        public int StepIndex { get; set; }
        public WorkflowStep StepType { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string IconKind { get; set; } = string.Empty;
        public string IconColor { get; set; } = "#000000";
        public string ShortcutKey { get; set; } = string.Empty;
        public bool ShowConnector { get; set; }
        public DateTime? CompletionTime { get; set; }
    }

    /// <summary>
    /// 步骤验证请求
    /// </summary>
    public class StepValidationRequest
    {
        public WorkflowStep Step { get; set; }
        public Guid RequestId { get; set; }
    }

    /// <summary>
    /// 工作流完成数据
    /// </summary>
    public class WorkflowCompletionData
    {
        public DateTime CompletionTime { get; set; }
        public TimeSpan TotalDuration { get; set; }
    }
}