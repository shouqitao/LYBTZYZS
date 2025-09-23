using System.Windows.Input;
using LYBT.Desktop.Core.ViewModels.Base;
using LYBT.Desktop.Core.Interfaces.Services;
using LYBT.Shared.Interfaces.Services;
using Prism.Commands;
using Prism.Events;
using Prism.Regions;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Workbench.Medical.ViewModels.Workflow
{
    /// <summary>
    /// 诊疗流程视图模型
    /// 管理患者选择 → 诊断 → 处方的完整流程
    /// </summary>
    public class MedicalWorkflowViewModel : ServiceViewModel, INavigationAware
    {
        private readonly IRegionManager _regionManager;
        private readonly IPatientService _patientService;
        private readonly IConsultationService _consultationService;
        private readonly IPrescriptionService _prescriptionService;
        private readonly ILogger<MedicalWorkflowViewModel> _logger;
        private readonly IErrorHandlingService _errorHandlingService;

        #region 流程状态

        public enum WorkflowStep
        {
            PatientSelection,  // 患者选择
            Diagnosis,         // 诊断录入
            Prescription       // 处方开立（可选）
        }

        private WorkflowStep _currentStep = WorkflowStep.PatientSelection;
        public WorkflowStep CurrentStep
        {
            get => _currentStep;
            set
            {
                if (SetProperty(ref _currentStep, value))
                {
                    UpdateStepIndicators();
                    LoadStepContent();
                }
            }
        }

        private Guid _selectedPatientId = Guid.Empty;
        public Guid SelectedPatientId
        {
            get => _selectedPatientId;
            set => SetProperty(ref _selectedPatientId, value);
        }

        private string _patientName = string.Empty;
        public string PatientName
        {
            get => _patientName;
            set => SetProperty(ref _patientName, value);
        }

        private Guid _currentConsultationId = Guid.Empty;
        public Guid CurrentConsultationId
        {
            get => _currentConsultationId;
            set => SetProperty(ref _currentConsultationId, value);
        }

        #endregion

        #region 步骤指示器

        private bool _isPatientStepCompleted;
        public bool IsPatientStepCompleted
        {
            get => _isPatientStepCompleted;
            set => SetProperty(ref _isPatientStepCompleted, value);
        }

        private bool _isDiagnosisStepCompleted;
        public bool IsDiagnosisStepCompleted
        {
            get => _isDiagnosisStepCompleted;
            set => SetProperty(ref _isDiagnosisStepCompleted, value);
        }

        private bool _isPrescriptionStepCompleted;
        public bool IsPrescriptionStepCompleted
        {
            get => _isPrescriptionStepCompleted;
            set => SetProperty(ref _isPrescriptionStepCompleted, value);
        }

        #endregion

        #region 命令

        public ICommand NextStepCommand { get; }
        public ICommand PreviousStepCommand { get; }
        public ICommand SkipPrescriptionCommand { get; }
        public ICommand CompleteWorkflowCommand { get; }
        public ICommand CancelWorkflowCommand { get; }

        #endregion

        public MedicalWorkflowViewModel(
            IRegionManager regionManager,
            IEventAggregator eventAggregator,
            IErrorHandlingService errorHandlingService,
            IPatientService patientService,
            IConsultationService consultationService,
            IPrescriptionService prescriptionService,
            ILogger<MedicalWorkflowViewModel> logger)
            : base(eventAggregator, errorHandlingService)
        {
            _regionManager = regionManager;
            _errorHandlingService = errorHandlingService;
            _patientService = patientService;
            _consultationService = consultationService;
            _prescriptionService = prescriptionService;
            _logger = logger;

            // 初始化命令
            NextStepCommand = new DelegateCommand(ExecuteNextStep, CanExecuteNextStep);
            PreviousStepCommand = new DelegateCommand(ExecutePreviousStep, CanExecutePreviousStep);
            SkipPrescriptionCommand = new DelegateCommand(ExecuteSkipPrescription, CanExecuteSkipPrescription);
            CompleteWorkflowCommand = new DelegateCommand(ExecuteCompleteWorkflow, CanExecuteCompleteWorkflow);
            CancelWorkflowCommand = new DelegateCommand(ExecuteCancelWorkflow);
        }

        #region 流程控制

        private void ExecuteNextStep()
        {
            switch (CurrentStep)
            {
                case WorkflowStep.PatientSelection:
                    if (SelectedPatientId != Guid.Empty)
                    {
                        IsPatientStepCompleted = true;
                        CurrentStep = WorkflowStep.Diagnosis;
                        _logger.LogInformation("进入诊断步骤，患者ID: {PatientId}", SelectedPatientId);
                    }
                    break;

                case WorkflowStep.Diagnosis:
                    if (CurrentConsultationId != Guid.Empty)
                    {
                        IsDiagnosisStepCompleted = true;
                        CurrentStep = WorkflowStep.Prescription;
                        _logger.LogInformation("进入处方步骤，诊疗ID: {ConsultationId}", CurrentConsultationId);
                    }
                    break;

                case WorkflowStep.Prescription:
                    CompleteWorkflow();
                    break;
            }
        }

        private bool CanExecuteNextStep()
        {
            return CurrentStep switch
            {
                WorkflowStep.PatientSelection => SelectedPatientId != Guid.Empty,
                WorkflowStep.Diagnosis => CurrentConsultationId != Guid.Empty,
                WorkflowStep.Prescription => true,
                _ => false
            };
        }

        private void ExecutePreviousStep()
        {
            switch (CurrentStep)
            {
                case WorkflowStep.Diagnosis:
                    CurrentStep = WorkflowStep.PatientSelection;
                    break;

                case WorkflowStep.Prescription:
                    CurrentStep = WorkflowStep.Diagnosis;
                    break;
            }
        }

        private bool CanExecutePreviousStep()
        {
            return CurrentStep != WorkflowStep.PatientSelection;
        }

        private void ExecuteSkipPrescription()
        {
            _logger.LogInformation("跳过处方步骤，完成诊疗流程");
            IsPrescriptionStepCompleted = false;
            CompleteWorkflow();
        }

        private bool CanExecuteSkipPrescription()
        {
            return CurrentStep == WorkflowStep.Prescription;
        }

        private void ExecuteCompleteWorkflow()
        {
            _logger.LogInformation("完成诊疗流程");
            CompleteWorkflow();
        }

        private bool CanExecuteCompleteWorkflow()
        {
            return IsDiagnosisStepCompleted;
        }

        private void ExecuteCancelWorkflow()
        {
            _logger.LogInformation("取消诊疗流程");
            ResetWorkflow();

            // 返回主页或管理界面
            _regionManager.RequestNavigate("MedicalWorkbenchContentRegion", "MedicalManagementView");
        }

        private void CompleteWorkflow()
        {
            _logger.LogInformation("诊疗流程完成 - 患者: {PatientName}, 诊疗ID: {ConsultationId}",
                PatientName, CurrentConsultationId);

            // 发布诊疗完成事件
            EventAggregator.GetEvent<ConsultationCompletedEvent>()
                .Publish(new ConsultationCompletedEventArgs
                {
                    ConsultationId = CurrentConsultationId,
                    PatientId = SelectedPatientId,
                    HasPrescription = IsPrescriptionStepCompleted
                });

            ResetWorkflow();
        }

        private void ResetWorkflow()
        {
            CurrentStep = WorkflowStep.PatientSelection;
            SelectedPatientId = Guid.Empty;
            PatientName = string.Empty;
            CurrentConsultationId = Guid.Empty;
            IsPatientStepCompleted = false;
            IsDiagnosisStepCompleted = false;
            IsPrescriptionStepCompleted = false;
        }

        private void UpdateStepIndicators()
        {
            // 更新步骤指示器UI
            RaisePropertyChanged(nameof(IsPatientStepActive));
            RaisePropertyChanged(nameof(IsDiagnosisStepActive));
            RaisePropertyChanged(nameof(IsPrescriptionStepActive));
        }

        private void LoadStepContent()
        {
            string viewName = CurrentStep switch
            {
                WorkflowStep.PatientSelection => "PatientSelectionView",
                WorkflowStep.Diagnosis => "DiagnosisEntryView",
                WorkflowStep.Prescription => "PrescriptionEntryView",
                _ => "PatientSelectionView"
            };

            var parameters = new NavigationParameters
            {
                { "WorkflowMode", true },
                { "PatientId", SelectedPatientId },
                { "ConsultationId", CurrentConsultationId }
            };

            _regionManager.RequestNavigate("WorkflowContentRegion", viewName, parameters);
        }

        #endregion

        #region 辅助属性

        public bool IsPatientStepActive => CurrentStep == WorkflowStep.PatientSelection;
        public bool IsDiagnosisStepActive => CurrentStep == WorkflowStep.Diagnosis;
        public bool IsPrescriptionStepActive => CurrentStep == WorkflowStep.Prescription;

        public string StepTitle => CurrentStep switch
        {
            WorkflowStep.PatientSelection => "选择患者",
            WorkflowStep.Diagnosis => "录入诊断",
            WorkflowStep.Prescription => "开立处方",
            _ => "诊疗流程"
        };

        public string StepDescription => CurrentStep switch
        {
            WorkflowStep.PatientSelection => "请选择或新增患者开始诊疗",
            WorkflowStep.Diagnosis => "录入诊断信息和医嘱",
            WorkflowStep.Prescription => "根据诊断开立处方（可选）",
            _ => ""
        };

        #endregion

        #region INavigationAware

        public void OnNavigatedTo(NavigationContext navigationContext)
        {
            // 如果带有患者ID参数，直接进入诊断步骤
            if (navigationContext.Parameters.TryGetValue<Guid>("PatientId", out var patientId)
                && patientId != Guid.Empty)
            {
                SelectedPatientId = patientId;
                LoadPatientInfo(patientId);
                CurrentStep = WorkflowStep.Diagnosis;
            }
            else
            {
                ResetWorkflow();
            }
        }

        public bool IsNavigationTarget(NavigationContext navigationContext) => true;

        public void OnNavigatedFrom(NavigationContext navigationContext)
        {
            // 保存流程状态（如需要）
        }

        #endregion

        private async void LoadPatientInfo(Guid patientId)
        {
            try
            {
                var result = await _patientService.GetByIdAsync(patientId);
                if (result.IsSuccess && result.Data != null)
                {
                    PatientName = result.Data.Name;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "加载患者信息失败");
            }
        }
    }

    #region 事件定义

    public class ConsultationCompletedEvent : PubSubEvent<ConsultationCompletedEventArgs> { }

    public class ConsultationCompletedEventArgs
    {
        public Guid ConsultationId { get; set; }
        public Guid PatientId { get; set; }
        public bool HasPrescription { get; set; }
    }

    #endregion
}