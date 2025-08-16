using LYBT.Shared.Models.Contracts.Common;
using System;
using System.ComponentModel;
using LYBT.Desktop.Core.Models.Consultation;
using LYBT.Desktop.Consultation.Interfaces;
using LYBT.Shared.Models.Enums;

namespace LYBT.Desktop.Consultation.Services
{
    /// <summary>
    /// 诊疗工作流状态管理器
    /// 负责管理工作流的各个步骤状态和验证
    /// </summary>
    public class WorkflowStateManager : IWorkflowStateManager
    {
        #region 属性

        private WorkflowStep _currentStep = WorkflowStep.PatientSelection;
        public WorkflowStep CurrentStep
        {
            get => _currentStep;
            set
            {
                if (_currentStep != value)
                {
                    _currentStep = value;
                    OnPropertyChanged(nameof(CurrentStep));
                    UpdateStepStatus();
                }
            }
        }

        private bool _isPatientStepActive = true;
        public bool IsPatientStepActive
        {
            get => _isPatientStepActive;
            set
            {
                _isPatientStepActive = value;
                OnPropertyChanged(nameof(IsPatientStepActive));
            }
        }

        private bool _isFourDiagnosisStepActive;
        public bool IsFourDiagnosisStepActive
        {
            get => _isFourDiagnosisStepActive;
            set
            {
                _isFourDiagnosisStepActive = value;
                OnPropertyChanged(nameof(IsFourDiagnosisStepActive));
            }
        }

        private bool _isDifferentiationStepActive;
        public bool IsDifferentiationStepActive
        {
            get => _isDifferentiationStepActive;
            set
            {
                _isDifferentiationStepActive = value;
                OnPropertyChanged(nameof(IsDifferentiationStepActive));
            }
        }

        private bool _isPrescriptionStepActive;
        public bool IsPrescriptionStepActive
        {
            get => _isPrescriptionStepActive;
            set
            {
                _isPrescriptionStepActive = value;
                OnPropertyChanged(nameof(IsPrescriptionStepActive));
            }
        }

        #endregion

        #region 方法

        /// <summary>
        /// 更新步骤状态
        /// </summary>
        public void UpdateStepStatus()
        {
            IsPatientStepActive = CurrentStep == WorkflowStep.PatientSelection;
            IsFourDiagnosisStepActive = CurrentStep == WorkflowStep.FourDiagnosis;
            IsDifferentiationStepActive = CurrentStep == WorkflowStep.Differentiation;
            IsPrescriptionStepActive = CurrentStep == WorkflowStep.Prescription;
        }

        /// <summary>
        /// 验证当前步骤是否可以进行下一步
        /// </summary>
        public bool CanProceedToNextStep()
        {
            return CurrentStep switch
            {
                WorkflowStep.PatientSelection => true, // 患者选择后可进入四诊
                WorkflowStep.FourDiagnosis => true, // 四诊完成后可进入辨证
                WorkflowStep.Differentiation => true, // 辨证完成后可进入处方
                WorkflowStep.Prescription => false, // 处方是最后一步
                _ => false
            };
        }

        /// <summary>
        /// 移动到下一步
        /// </summary>
        public bool MoveToNextStep()
        {
            if (!CanProceedToNextStep()) return false;

            CurrentStep = CurrentStep switch
            {
                WorkflowStep.PatientSelection => WorkflowStep.FourDiagnosis,
                WorkflowStep.FourDiagnosis => WorkflowStep.Differentiation,
                WorkflowStep.Differentiation => WorkflowStep.Prescription,
                _ => CurrentStep
            };

            return true;
        }

        /// <summary>
        /// 移动到上一步
        /// </summary>
        public bool MoveToPreviousStep()
        {
            if (CurrentStep == WorkflowStep.PatientSelection) return false;

            CurrentStep = CurrentStep switch
            {
                WorkflowStep.FourDiagnosis => WorkflowStep.PatientSelection,
                WorkflowStep.Differentiation => WorkflowStep.FourDiagnosis,
                WorkflowStep.Prescription => WorkflowStep.Differentiation,
                _ => CurrentStep
            };

            return true;
        }

        /// <summary>
        /// 重置工作流到初始状态
        /// </summary>
        public void ResetWorkflow()
        {
            CurrentStep = WorkflowStep.PatientSelection;
        }

        #endregion

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }

    /// <summary>
    /// 工作流步骤枚举
    /// </summary>
    public enum WorkflowStep
    {
        PatientSelection = 0,
        FourDiagnosis = 1,
        Differentiation = 2,
        Prescription = 3
    }
}