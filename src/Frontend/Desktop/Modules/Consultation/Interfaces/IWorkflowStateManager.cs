using System.ComponentModel;
using LYBT.WPF.Client.Modules.Consultation.Services;

namespace LYBT.WPF.Client.Modules.Consultation.Interfaces
{
    /// <summary>
    /// 工作流状态管理接口
    /// </summary>
    public interface IWorkflowStateManager : INotifyPropertyChanged
    {
        /// <summary>当前工作流步骤</summary>
        WorkflowStep CurrentStep { get; set; }

        /// <summary>患者选择步骤是否激活</summary>
        bool IsPatientStepActive { get; }

        /// <summary>四诊步骤是否激活</summary>
        bool IsFourDiagnosisStepActive { get; }

        /// <summary>辨证步骤是否激活</summary>
        bool IsDifferentiationStepActive { get; }

        /// <summary>处方步骤是否激活</summary>
        bool IsPrescriptionStepActive { get; }

        /// <summary>更新步骤状态</summary>
        void UpdateStepStatus();

        /// <summary>验证是否可以进行下一步</summary>
        bool CanProceedToNextStep();

        /// <summary>移动到下一步</summary>
        bool MoveToNextStep();

        /// <summary>移动到上一步</summary>
        bool MoveToPreviousStep();

        /// <summary>重置工作流</summary>
        void ResetWorkflow();
    }
}