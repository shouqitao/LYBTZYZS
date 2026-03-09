using LYBT.Desktop.MedicalCase.Interfaces;
using LYBT.Desktop.MedicalCase.Models;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.MedicalCase.ViewModels.Components;

/// <summary>
/// Transition-table driven edit mode state machine for MedicalCaseWorkspaceViewModel (US-MC-011).
/// Follows AuthenticationStateMachine pattern: Dictionary transition table, thread-safe lock,
/// events raised outside lock to prevent deadlock.
/// </summary>
public class EditModeStateMachine : IEditModeStateMachine
{
    private readonly ILogger<EditModeStateMachine> _logger;
    private readonly object _stateLock = new();
    private WorkspaceEditState _currentState = WorkspaceEditState.ReadOnly;
    private WorkspaceEditState _returnState = WorkspaceEditState.ReadOnly;
    private Func<WorkspaceEditEvent, bool>? _guardPredicate;
    private bool _isProcessingTransition;

    /// <summary>
    /// Transition table: (CurrentState, Event) -> NextState.
    /// _returnState handles Saving and LeavingConfirming rollback paths.
    /// </summary>
    private static readonly Dictionary<(WorkspaceEditState, WorkspaceEditEvent), WorkspaceEditState> Transitions = new()
    {
        // ReadOnly transitions
        { (WorkspaceEditState.ReadOnly,          WorkspaceEditEvent.EnterEdit),       WorkspaceEditState.Editing },

        // Editing (clean) transitions
        { (WorkspaceEditState.Editing,           WorkspaceEditEvent.ExitEdit),        WorkspaceEditState.ReadOnly },
        { (WorkspaceEditState.Editing,           WorkspaceEditEvent.MakeChange),      WorkspaceEditState.DirtyEditing },
        { (WorkspaceEditState.Editing,           WorkspaceEditEvent.RequestLeave),    WorkspaceEditState.ReadOnly },
        { (WorkspaceEditState.Editing,           WorkspaceEditEvent.Save),            WorkspaceEditState.Saving },

        // DirtyEditing (unsaved) transitions
        { (WorkspaceEditState.DirtyEditing,      WorkspaceEditEvent.ExitEdit),        WorkspaceEditState.LeavingConfirming },
        { (WorkspaceEditState.DirtyEditing,      WorkspaceEditEvent.Save),            WorkspaceEditState.Saving },
        { (WorkspaceEditState.DirtyEditing,      WorkspaceEditEvent.RequestLeave),    WorkspaceEditState.LeavingConfirming },

        // Saving (transient) transitions
        { (WorkspaceEditState.Saving,            WorkspaceEditEvent.SaveCompleted),   WorkspaceEditState.ReadOnly },
        { (WorkspaceEditState.Saving,            WorkspaceEditEvent.SaveFailed),      WorkspaceEditState.TransitionBlocked },

        // TransitionBlocked transitions
        { (WorkspaceEditState.TransitionBlocked, WorkspaceEditEvent.MakeChange),      WorkspaceEditState.DirtyEditing },
        { (WorkspaceEditState.TransitionBlocked, WorkspaceEditEvent.RequestLeave),    WorkspaceEditState.LeavingConfirming },

        // LeavingConfirming (transient) transitions
        { (WorkspaceEditState.LeavingConfirming, WorkspaceEditEvent.LeaveConfirmed),  WorkspaceEditState.ReadOnly },
        { (WorkspaceEditState.LeavingConfirming, WorkspaceEditEvent.LeaveCancelled),  WorkspaceEditState.DirtyEditing },
        { (WorkspaceEditState.LeavingConfirming, WorkspaceEditEvent.Save),            WorkspaceEditState.Saving },
    };

    /// <inheritdoc/>
    public WorkspaceEditState CurrentState
    {
        get { lock (_stateLock) { return _currentState; } }
    }

    /// <inheritdoc/>
    public bool IsDirty => CurrentState == WorkspaceEditState.DirtyEditing;

    /// <inheritdoc/>
    public event EventHandler<EditStateChangedEventArgs>? StateChanged;

    public EditModeStateMachine(ILogger<EditModeStateMachine> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>Internal constructor for tests — allows setting initial state directly.</summary>
    internal EditModeStateMachine(ILogger<EditModeStateMachine> logger, WorkspaceEditState initialState)
        : this(logger)
    {
        _currentState = initialState;
    }

    /// <inheritdoc/>
    public void Initialize(WorkspaceEditState initialState, Func<WorkspaceEditEvent, bool>? guardPredicate = null)
    {
        lock (_stateLock)
        {
            _currentState = initialState;
            _returnState = initialState;
            _guardPredicate = guardPredicate;
        }

        _logger.LogInformation("编辑状态机初始化 [{InitialState}]", initialState);
    }

    /// <inheritdoc/>
    public bool CanFire(WorkspaceEditEvent evt)
    {
        lock (_stateLock)
        {
            return Transitions.ContainsKey((_currentState, evt))
                && (_guardPredicate == null || _guardPredicate(evt));
        }
    }

    /// <inheritdoc/>
    public bool Fire(WorkspaceEditEvent evt, string? context = null)
    {
        WorkspaceEditState previousState;
        WorkspaceEditState newState;

        lock (_stateLock)
        {
            // Reentrancy guard: prevent duplicate Save/Leave during processing
            if (_isProcessingTransition)
            {
                _logger.LogWarning("编辑状态机重入被阻止 [当前状态: {CurrentState}] [事件: {Event}]",
                    _currentState, evt);
                return false;
            }

            var key = (_currentState, evt);
            if (!Transitions.TryGetValue(key, out newState))
            {
                _logger.LogWarning("无效的编辑状态转换 [当前状态: {CurrentState}] [事件: {Event}]",
                    _currentState, evt);
                return false;
            }

            // Apply guard predicate (e.g., CanEdit blocks EnterEdit)
            if (_guardPredicate != null && !_guardPredicate(evt))
            {
                _logger.LogWarning("编辑状态转换被守卫阻止 [当前状态: {CurrentState}] [事件: {Event}]",
                    _currentState, evt);
                return false;
            }

            previousState = _currentState;
            _isProcessingTransition = true;

            // Save _returnState before entering transient states
            if (evt is WorkspaceEditEvent.Save or WorkspaceEditEvent.RequestLeave or WorkspaceEditEvent.ExitEdit)
                _returnState = _currentState;

            _currentState = newState;
        }

        _logger.LogInformation("编辑状态转换 [{PreviousState}] --({Event})--> [{NewState}] {Context}",
            previousState, evt, newState, context ?? string.Empty);

        // Raise event outside lock to prevent deadlock
        try
        {
            var args = new EditStateChangedEventArgs(previousState, newState, evt, context);
            StateChanged?.Invoke(this, args);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "编辑状态变更事件处理异常 [{PreviousState}] -> [{NewState}]",
                previousState, newState);
        }
        finally
        {
            lock (_stateLock)
            {
                _isProcessingTransition = false;
            }
        }

        return true;
    }

    /// <inheritdoc/>
    public IEnumerable<WorkspaceEditEvent> GetPermittedEvents()
    {
        lock (_stateLock)
        {
            return Transitions.Keys
                .Where(k => k.Item1 == _currentState)
                .Select(k => k.Item2)
                .ToList();
        }
    }
}
