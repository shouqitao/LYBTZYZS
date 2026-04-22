namespace LYBT.Desktop.MedicalCase.Models;

/// <summary>
/// Phase 1.4: 完整性检查状态记录
/// 用于实时显示医案完成度检查结果
/// </summary>
public record CompletenessCheck(
    bool DiagnosisComplete = false,
    bool PrescriptionDecisionComplete = false,
    bool PrescriptionContentComplete = false,
    bool DosageCountComplete = false,
    bool CanCompleteCase = false,
    int PrescriptionItemCount = 0,
    int DosageCount = 0)
{
    /// <summary>
    /// 处方决策是否完成（已选择需要或不需要处方）
    /// </summary>
    public bool IsDecisionMade => PrescriptionDecisionComplete;

    /// <summary>
    /// 所有必填项是否完成
    /// </summary>
    public bool AllRequiredComplete => DiagnosisComplete && IsDecisionMade;

    /// <summary>
    /// 处方内容是否完整（如果需要处方）
    /// </summary>
    public bool PrescriptionComplete => !PrescriptionDecisionComplete || (PrescriptionContentComplete && DosageCountComplete);
}

/// <summary>
/// Immutable workspace state record. Replaces 30+ inline properties + RaiseEditStateProperties().
/// Use 'with' expressions for state transitions; single OnPropertyChanged(nameof(State)) in parent VM.
/// </summary>
public record WorkspaceState(
    EditState EditState = EditState.Editing,
    EditType EditType = EditType.Create,
    WorkspaceMode Mode = WorkspaceMode.Clinical,
    bool CanEdit = false,
    bool IsPrescriptionEnabled = false,
    bool NeedsPrescription = true,
    bool CanComplete = false,
    bool CanPrint = false,
    string Remark = "",
    string EditReason = "",
    CompletenessCheck? Completeness = null)
{
    // Edit state computed properties
    public bool IsEditing => EditState == EditState.Editing;
    public bool IsReadOnly => EditState == EditState.ReadOnly;
    public bool IsHistoricalEditMode => EditType == EditType.EditCompleted;

    /// <summary>
    /// Show persistent amber edit-mode banner (US-MC-011).
    /// True when user is actively editing (Editing or DirtyEditing state machine state).
    /// </summary>
    public bool ShowEditBanner => IsEditing;

    // Button visibility computed properties
    public bool ShowEditButton => IsReadOnly && CanEdit && Mode == WorkspaceMode.Clinical;
    public bool ShowEditButtonTopRight => IsReadOnly && CanEdit && Mode == WorkspaceMode.Management;
    public bool ShowSaveButton => IsEditing && Mode == WorkspaceMode.Management;
    public bool ShowSuspendButton => IsEditing && Mode == WorkspaceMode.Clinical;
    public bool ShowCompleteButton => IsEditing && Mode == WorkspaceMode.Clinical;

    // Display text computed properties
    public string HeaderTitle => Mode switch
    {
        WorkspaceMode.Clinical => IsEditing ? "看诊中" : "查看医案",
        WorkspaceMode.Management => IsEditing ? "编辑医案" : "查看医案",
        _ => "看诊中"
    };

    public string BackButtonText => Mode switch
    {
        WorkspaceMode.Clinical => "返回患者选择",
        WorkspaceMode.Management => "返回医案列表",
        _ => "返回"
    };

    // State transition methods (return new instances - immutable)
    public WorkspaceState EnterEditMode()
        => CanEdit ? this with { EditState = EditState.Editing } : this;

    public WorkspaceState EnterReadOnlyMode()
        => this with { EditState = EditState.ReadOnly };

    public WorkspaceState DetermineFromContext(
        WorkspaceMode workspaceMode, bool isCompleted, bool isOwner,
        bool isAdmin, bool preferEditing)
    {
        var canEdit = isAdmin || (isOwner && !isCompleted);
        var editType = isCompleted ? EditType.EditCompleted : EditType.EditSuspended;
        var editState = preferEditing && canEdit ? EditState.Editing : EditState.ReadOnly;
        return this with
        {
            Mode = workspaceMode,
            CanEdit = canEdit,
            EditType = editType,
            EditState = editState
        };
    }
}
