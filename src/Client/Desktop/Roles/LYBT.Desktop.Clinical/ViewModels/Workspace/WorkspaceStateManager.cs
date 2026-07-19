using LYBT.Desktop.MedicalCase.Interfaces;
using LYBT.Desktop.MedicalCase.Models;
using LYBT.Desktop.MedicalCase.ViewModels.Workspace;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Enums;

namespace LYBT.Desktop.Clinical.ViewModels.Workspace;

/// <summary>
/// 医案工作台状态管理器
/// 职责：WorkspaceState 更新、5步工作流自动推进、完整性检查、编辑模式 FSM。
/// 从 MedicalCaseWorkspaceViewModel 提取，减少父 VM 体积。
/// </summary>
internal sealed class WorkspaceStateManager
{
    private readonly IEditModeStateMachine _editStateMachine;
    private readonly Func<ConsultationEditorViewModel> _getConsultationEditor;
    private readonly Func<PrescriptionEditorViewModel> _getPrescriptionEditor;
    private readonly Func<bool> _getIsPrescriptionEnabled;

    public WorkspaceStateManager(
        IEditModeStateMachine editStateMachine,
        Func<ConsultationEditorViewModel> getConsultationEditor,
        Func<PrescriptionEditorViewModel> getPrescriptionEditor,
        Func<bool> getIsPrescriptionEnabled)
    {
        _editStateMachine = editStateMachine;
        _getConsultationEditor = getConsultationEditor;
        _getPrescriptionEditor = getPrescriptionEditor;
        _getIsPrescriptionEnabled = getIsPrescriptionEnabled;
    }

    /// <summary>
    /// 更新工作区状态（步骤推进 + 完整性 + CanComplete/CanPrint）
    /// </summary>
    public WorkspaceState UpdateState(WorkspaceState current)
    {
        var newStep = CalculateCurrentStep();
        var completeness = CalculateCompleteness(current);
        var canComplete = CalculateCanComplete();

        return current with
        {
            Completeness = completeness,
            CanComplete = canComplete,
            CanPrint = _getPrescriptionEditor().HasItems
        };
    }

    /// <summary>
    /// 5步工作流自动推进
    /// Step 1→2: PresentIllness has content
    /// Step 2→3: TcmDiagnosis validated
    /// Step 3→4: NeedsPrescription decided
    /// Step 4→5: Prescription has items OR NoPrescription selected
    /// </summary>
    public int CalculateCurrentStep()
    {
        var consultation = _getConsultationEditor().Consultation;
        var prescription = _getPrescriptionEditor().Prescription;
        var isPrescriptionEnabled = _getIsPrescriptionEnabled();

        int step = 1;

        if (!string.IsNullOrWhiteSpace(consultation.PresentIllness))
            step = 2;

        if (step == 2 && consultation.IsDiagnosisComplete)
            step = 3;

        if (step == 3)
            step = 4;

        if (step == 4)
        {
            if (!isPrescriptionEnabled || prescription.ItemCount > 0)
                step = 5;
        }

        return step;
    }

    /// <summary>
    /// 根据当前诊断和处方数据计算完成度
    /// </summary>
    public CompletenessCheck CalculateCompleteness(WorkspaceState current)
    {
        var consultation = _getConsultationEditor().Consultation;
        var prescription = _getPrescriptionEditor().Prescription;
        var isPrescriptionEnabled = _getIsPrescriptionEnabled();

        return new CompletenessCheck(
            DiagnosisComplete: consultation.IsDiagnosisComplete,
            PrescriptionDecisionComplete: true,
            PrescriptionContentComplete: !isPrescriptionEnabled || prescription.HasItems,
            DosageCountComplete: !isPrescriptionEnabled || prescription.DosageCount > 0,
            CanCompleteCase: CalculateCanComplete(),
            PrescriptionItemCount: prescription.ItemCount,
            DosageCount: prescription.DosageCount
        );
    }

    /// <summary>
    /// 计算是否可以完成医案
    /// </summary>
    public bool CalculateCanComplete()
    {
        var consultation = _getConsultationEditor().Consultation;
        var prescription = _getPrescriptionEditor().Prescription;
        var isPrescriptionEnabled = _getIsPrescriptionEnabled();

        if (!consultation.IsDiagnosisComplete) return false;
        if (!isPrescriptionEnabled) return true;
        return prescription.ItemCount > 0;
    }

    /// <summary>
    /// 确定编辑模式
    /// </summary>
    public (WorkspaceState state, bool canEdit, bool startEditing) DetermineEditMode(
        WorkspaceState currentState,
        WorkspaceMode workspaceMode,
        MedicalCaseDetailDto? medicalCase,
        UserRole? currentUserRole,
        Guid currentUserId,
        EditState initialEditState,
        bool isHistoricalEdit)
    {
        if (medicalCase == null)
        {
            var newState = new WorkspaceState(Mode: workspaceMode, EditState: EditState.Editing, EditType: EditType.Create, CanEdit: true);
            return (newState, canEdit: true, startEditing: true);
        }

        var isAdmin = currentUserRole == UserRole.Admin || currentUserRole == UserRole.SuperAdmin;
        var isOwner = medicalCase.UserId == currentUserId;
        var isCompleted = medicalCase.CaseStatus == MedicalCaseStatus.Completed;
        var preferEditing = initialEditState == EditState.Editing || isHistoricalEdit;

        var state = currentState.DetermineFromContext(workspaceMode, isCompleted, isOwner, isAdmin, preferEditing);
        if (isHistoricalEdit) state = state with { EditType = EditType.EditCompleted };

        var canEditResult = isAdmin || (isOwner && !isCompleted);
        return (state, canEditResult, preferEditing && canEditResult);
    }

    /// <summary>
    /// 初始化编辑状态 FSM
    /// </summary>
    public void InitializeEditStateMachine(bool canEdit, bool startEditing)
    {
        var initialFsmState = startEditing
            ? WorkspaceEditState.Editing
            : WorkspaceEditState.ReadOnly;

        _editStateMachine.Initialize(initialFsmState, guardPredicate: evt =>
        {
            if (evt == WorkspaceEditEvent.EnterEdit && !canEdit)
                return false;
            return true;
        });
    }

    /// <summary>
    /// 处理 FSM 状态变更 — 更新 WorkspaceState.EditState
    /// </summary>
    public WorkspaceState OnEditStateChanged(WorkspaceState currentState, EditStateChangedEventArgs e)
    {
        var editState = e.NewState is WorkspaceEditState.Editing or WorkspaceEditState.DirtyEditing
            ? EditState.Editing
            : EditState.ReadOnly;

        return currentState with { EditState = editState };
    }
}
