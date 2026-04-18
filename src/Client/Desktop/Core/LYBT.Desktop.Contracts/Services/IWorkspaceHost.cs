namespace LYBT.Desktop.Contracts.Services;

/// <summary>
/// Generic child-to-parent operations contract for Composite ViewModel pattern.
/// Child VMs call these methods to request UI operations from the parent.
/// Replaces Handler callback Action/Func properties (SetBusy, ShowErrorMessage, etc.)
/// </summary>
public interface IWorkspaceHost
{
    void SetBusy(bool isBusy, string? message = null);
    Task ShowErrorAsync(string message);
    Task ShowSuccessAsync(string message);
    Task<bool> ShowConfirmAsync(string message, string title = "确认");
    ICommonDialogService? CommonDialogService { get; }

    /// <summary>
    /// Child VM notifies parent that state needs recalculation.
    /// Parent should recompute WorkspaceState (CanComplete, CanPrint, etc.)
    /// </summary>
    void NotifyStateChanged();

    /// <summary>
    /// P1-2 FIX: Request transition to edit mode.
    /// Triggers the EditModeStateMachine's EnterEdit event to transition from ReadOnly to Editing.
    /// </summary>
    void RequestEnterEditMode();
}
