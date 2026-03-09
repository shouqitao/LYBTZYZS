namespace LYBT.Desktop.MedicalCase.Models;

/// <summary>
/// Events that drive the edit mode state machine (US-MC-011).
/// </summary>
public enum WorkspaceEditEvent
{
    /// <summary>User clicks the "Edit" button.</summary>
    EnterEdit = 0,

    /// <summary>User cancels editing and reverts (from clean Editing state).</summary>
    ExitEdit = 1,

    /// <summary>Any data modification — transitions Editing -> DirtyEditing.</summary>
    MakeChange = 2,

    /// <summary>User clicks Save/Suspend.</summary>
    Save = 3,

    /// <summary>Save operation completed successfully.</summary>
    SaveCompleted = 4,

    /// <summary>Save operation failed.</summary>
    SaveFailed = 5,

    /// <summary>User clicks Back or triggers navigation away.</summary>
    RequestLeave = 6,

    /// <summary>User confirmed leave (discard or save-then-leave).</summary>
    LeaveConfirmed = 7,

    /// <summary>User cancelled the leave dialog (stay).</summary>
    LeaveCancelled = 8,

    /// <summary>Navigation context initialization — sets state from context.</summary>
    Initialize = 9
}
