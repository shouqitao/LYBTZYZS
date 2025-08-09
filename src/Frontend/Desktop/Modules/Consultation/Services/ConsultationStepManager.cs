using System;
using System.Threading.Tasks;
using LYBT.WPF.Client.Core.Models.Consultation;
using Prism.Mvvm;

namespace LYBT.WPF.Client.Modules.Consultation.Services
{
    /// <summary>
    /// 诊疗步骤管理器 - 专门负责工作流步骤状态管理
    /// UltraThink重构：从ConsultationWorkflowViewModel中提取步骤管理职责
    /// </summary>
    public class ConsultationStepManager : BindableBase
    {
        #region 步骤状态属性

        private WorkflowStep _currentStep = WorkflowStep.PatientSelection;
        public WorkflowStep CurrentStep
        {
            get => _currentStep;
            set
            {
                if (SetProperty(ref _currentStep, value))
                {
                    UpdateStepStatus();
                    RaisePropertyChanged(nameof(CanGoToPreviousStep));
                    RaisePropertyChanged(nameof(CanGoToNextStep));
                }
            }
        }

        private bool _isPatientStepActive;
        public bool IsPatientStepActive
        {
            get => _isPatientStepActive;
            set => SetProperty(ref _isPatientStepActive, value);
        }

        private bool _isFourDiagnosisStepActive;
        public bool IsFourDiagnosisStepActive
        {
            get => _isFourDiagnosisStepActive;
            set => SetProperty(ref _isFourDiagnosisStepActive, value);
        }

        private bool _isDifferentiationStepActive;
        public bool IsDifferentiationStepActive
        {
            get => _isDifferentiationStepActive;
            set => SetProperty(ref _isDifferentiationStepActive, value);
        }

        private bool _isPrescriptionStepActive;
        public bool IsPrescriptionStepActive
        {
            get => _isPrescriptionStepActive;
            set => SetProperty(ref _isPrescriptionStepActive, value);
        }

        private bool _isPatientStepCompleted;
        public bool IsPatientStepCompleted
        {
            get => _isPatientStepCompleted;
            set => SetProperty(ref _isPatientStepCompleted, value);
        }

        private bool _isFourDiagnosisStepCompleted;
        public bool IsFourDiagnosisStepCompleted
        {
            get => _isFourDiagnosisStepCompleted;
            set => SetProperty(ref _isFourDiagnosisStepCompleted, value);
        }

        private bool _isDifferentiationStepCompleted;
        public bool IsDifferentiationStepCompleted
        {
            get => _isDifferentiationStepCompleted;
            set => SetProperty(ref _isDifferentiationStepCompleted, value);
        }

        private bool _isPrescriptionStepCompleted;
        public bool IsPrescriptionStepCompleted
        {
            get => _isPrescriptionStepCompleted;
            set => SetProperty(ref _isPrescriptionStepCompleted, value);
        }

        #endregion

        #region 导航计算属性

        public bool CanGoToPreviousStep => CurrentStep > WorkflowStep.PatientSelection;

        public bool CanGoToNextStep => CurrentStep < WorkflowStep.Prescription && GetStepCompletion(CurrentStep);

        public bool IsWorkflowCompleted => CurrentStep == WorkflowStep.Prescription && IsPrescriptionStepCompleted;

        #endregion

        #region 公共方法

        /// <summary>
        /// 移动到下一步
        /// </summary>
        public async Task<bool> MoveToNextStepAsync()
        {
            if (!CanGoToNextStep) return false;

            var nextStep = (WorkflowStep)((int)CurrentStep + 1);
            CurrentStep = nextStep;
            
            return await Task.FromResult(true);
        }

        /// <summary>
        /// 移动到上一步
        /// </summary>
        public async Task<bool> MoveToPreviousStepAsync()
        {
            if (!CanGoToPreviousStep) return false;

            var previousStep = (WorkflowStep)((int)CurrentStep - 1);
            CurrentStep = previousStep;
            
            return await Task.FromResult(true);
        }

        /// <summary>
        /// 跳转到指定步骤
        /// </summary>
        public async Task<bool> JumpToStepAsync(WorkflowStep targetStep)
        {
            if (targetStep == CurrentStep) return true;

            // 验证是否可以跳转到目标步骤
            if (targetStep > CurrentStep && !CanJumpToStep(targetStep))
            {
                return false;
            }

            CurrentStep = targetStep;
            return await Task.FromResult(true);
        }

        /// <summary>
        /// 标记当前步骤为完成
        /// </summary>
        public void MarkCurrentStepAsCompleted()
        {
            switch (CurrentStep)
            {
                case WorkflowStep.PatientSelection:
                    IsPatientStepCompleted = true;
                    break;
                case WorkflowStep.FourDiagnosis:
                    IsFourDiagnosisStepCompleted = true;
                    break;
                case WorkflowStep.Differentiation:
                    IsDifferentiationStepCompleted = true;
                    break;
                case WorkflowStep.Prescription:
                    IsPrescriptionStepCompleted = true;
                    break;
            }
        }

        /// <summary>
        /// 重置工作流状态
        /// </summary>
        public void ResetWorkflow()
        {
            CurrentStep = WorkflowStep.PatientSelection;
            IsPatientStepCompleted = false;
            IsFourDiagnosisStepCompleted = false;
            IsDifferentiationStepCompleted = false;
            IsPrescriptionStepCompleted = false;
        }

        #endregion

        #region 私有方法

        private void UpdateStepStatus()
        {
            // 重置所有步骤状态
            IsPatientStepActive = false;
            IsFourDiagnosisStepActive = false;
            IsDifferentiationStepActive = false;
            IsPrescriptionStepActive = false;

            // 激活当前步骤
            switch (CurrentStep)
            {
                case WorkflowStep.PatientSelection:
                    IsPatientStepActive = true;
                    break;
                case WorkflowStep.FourDiagnosis:
                    IsFourDiagnosisStepActive = true;
                    break;
                case WorkflowStep.Differentiation:
                    IsDifferentiationStepActive = true;
                    break;
                case WorkflowStep.Prescription:
                    IsPrescriptionStepActive = true;
                    break;
            }
        }

        private bool GetStepCompletion(WorkflowStep step)
        {
            return step switch
            {
                WorkflowStep.PatientSelection => IsPatientStepCompleted,
                WorkflowStep.FourDiagnosis => IsFourDiagnosisStepCompleted,
                WorkflowStep.Differentiation => IsDifferentiationStepCompleted,
                WorkflowStep.Prescription => IsPrescriptionStepCompleted,
                _ => false
            };
        }

        private bool CanJumpToStep(WorkflowStep targetStep)
        {
            // 检查前置步骤是否都已完成
            for (var step = WorkflowStep.PatientSelection; step < targetStep; step++)
            {
                if (!GetStepCompletion(step))
                {
                    return false;
                }
            }
            return true;
        }

        #endregion
    }
}