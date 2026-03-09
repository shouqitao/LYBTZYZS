using LYBT.Desktop.MedicalCase.Models;

namespace LYBT.Desktop.MedicalCase.Interfaces;

/// <summary>
/// Edit mode state machine for workspace ViewModel (US-MC-011).
/// Follows AuthenticationStateMachine pattern: transition table + thread-safe lock + events outside lock.
/// </summary>
public interface IEditModeStateMachine
{
    /// <summary>Current edit state.</summary>
    WorkspaceEditState CurrentState { get; }

    /// <summary>True when CurrentState is DirtyEditing (unsaved changes exist).</summary>
    bool IsDirty { get; }

    /// <summary>
    /// Initialize the state machine from navigation context.
    /// Must be called before first use.
    /// </summary>
    /// <param name="initialState">Computed initial state from context.</param>
    /// <param name="guardPredicate">Optional guard — Fire returns false when guard returns false.</param>
    void Initialize(WorkspaceEditState initialState, Func<WorkspaceEditEvent, bool>? guardPredicate = null);

    /// <summary>Returns true if the event can be fired from the current state.</summary>
    bool CanFire(WorkspaceEditEvent evt);

    /// <summary>
    /// Fires an event, transitioning state if the event is permitted.
    /// Returns false for invalid transitions or guard failures (never throws).
    /// </summary>
    bool Fire(WorkspaceEditEvent evt, string? context = null);

    /// <summary>Returns the events that are currently permitted.</summary>
    IEnumerable<WorkspaceEditEvent> GetPermittedEvents();

    /// <summary>Raised after a successful state transition (outside the state lock).</summary>
    event EventHandler<EditStateChangedEventArgs>? StateChanged;
}

/// <summary>
/// Event args for IEditModeStateMachine.StateChanged.
/// </summary>
public sealed class EditStateChangedEventArgs : EventArgs
{
    public WorkspaceEditState PreviousState { get; }
    public WorkspaceEditState NewState { get; }
    public WorkspaceEditEvent TriggerEvent { get; }
    public string? Context { get; }

    public EditStateChangedEventArgs(
        WorkspaceEditState previousState,
        WorkspaceEditState newState,
        WorkspaceEditEvent triggerEvent,
        string? context = null)
    {
        PreviousState = previousState;
        NewState = newState;
        TriggerEvent = triggerEvent;
        Context = context;
    }
}
