namespace LYBT.Desktop.MedicalCase.Models;

/// <summary>
/// Edit mode state for MedicalCaseWorkspaceViewModel (US-MC-011).
/// Replaces simple EditState (Editing/ReadOnly) with a 6-state FSM.
/// </summary>
public enum WorkspaceEditState
{
    /// <summary>Viewing case. Default for non-owner / completed cases.</summary>
    ReadOnly = 0,

    /// <summary>Actively modifying with no unsaved changes.</summary>
    Editing = 1,

    /// <summary>Actively modifying with unsaved changes (triggered by MakeChange event).</summary>
    DirtyEditing = 2,

    /// <summary>Transient: save operation in progress.</summary>
    Saving = 3,

    /// <summary>Transient: leave confirmation dialog is open.</summary>
    LeavingConfirming = 4,

    /// <summary>Transient: save failed, user must acknowledge before continuing.</summary>
    TransitionBlocked = 5
}
