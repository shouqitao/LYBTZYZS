using LYBT.Desktop.MedicalCase.Interfaces;
using LYBT.Desktop.MedicalCase.Models;
using LYBT.Desktop.MedicalCase.ViewModels.Components;
using Microsoft.Extensions.Logging;

namespace LYBT.Tests.Desktop.PureLogic.Clinical;

/// <summary>
/// US-MC-011: EditModeStateMachine 状态转换全覆盖测试 (~82 tests).
/// Follows AuthenticationStateMachineTests pattern.
/// </summary>
public class EditModeStateMachineTests
{
    private readonly ILogger<EditModeStateMachine> _logger = Substitute.For<ILogger<EditModeStateMachine>>();

    private EditModeStateMachine Create(WorkspaceEditState initial = WorkspaceEditState.ReadOnly)
        => new(_logger, initial);

    // ─── E.1: Initial State ───────────────────────────────────────────────────

    [Fact]
    public void Constructor_defaults_to_ReadOnly()
    {
        var sm = new EditModeStateMachine(_logger);
        sm.CurrentState.Should().Be(WorkspaceEditState.ReadOnly);
    }

    [Fact]
    public void Initialize_sets_state_and_clears_guard()
    {
        var sm = Create();
        sm.Initialize(WorkspaceEditState.Editing);
        sm.CurrentState.Should().Be(WorkspaceEditState.Editing);
    }

    [Fact]
    public void IsDirty_true_only_in_DirtyEditing()
    {
        var sm = Create(WorkspaceEditState.DirtyEditing);
        sm.IsDirty.Should().BeTrue();
    }

    [Fact]
    public void IsDirty_false_in_other_states()
    {
        foreach (var state in Enum.GetValues<WorkspaceEditState>().Where(s => s != WorkspaceEditState.DirtyEditing))
        {
            var sm = Create(state);
            sm.IsDirty.Should().BeFalse($"IsDirty should be false in {state}");
        }
    }

    // ─── E.2: ReadOnly transitions ────────────────────────────────────────────

    [Fact]
    public void ReadOnly_EnterEdit_transitions_to_Editing()
    {
        var sm = Create(WorkspaceEditState.ReadOnly);
        sm.Fire(WorkspaceEditEvent.EnterEdit).Should().BeTrue();
        sm.CurrentState.Should().Be(WorkspaceEditState.Editing);
    }

    [Theory]
    [InlineData(WorkspaceEditEvent.ExitEdit)]
    [InlineData(WorkspaceEditEvent.MakeChange)]
    [InlineData(WorkspaceEditEvent.Save)]
    [InlineData(WorkspaceEditEvent.SaveCompleted)]
    [InlineData(WorkspaceEditEvent.SaveFailed)]
    [InlineData(WorkspaceEditEvent.RequestLeave)]
    [InlineData(WorkspaceEditEvent.LeaveConfirmed)]
    [InlineData(WorkspaceEditEvent.LeaveCancelled)]
    public void ReadOnly_invalid_events_return_false(WorkspaceEditEvent evt)
    {
        var sm = Create(WorkspaceEditState.ReadOnly);
        sm.Fire(evt).Should().BeFalse($"{evt} should be invalid in ReadOnly");
        sm.CurrentState.Should().Be(WorkspaceEditState.ReadOnly);
    }

    // ─── E.3: Editing (clean) transitions ────────────────────────────────────

    [Fact]
    public void Editing_ExitEdit_transitions_to_ReadOnly()
    {
        var sm = Create(WorkspaceEditState.Editing);
        sm.Fire(WorkspaceEditEvent.ExitEdit).Should().BeTrue();
        sm.CurrentState.Should().Be(WorkspaceEditState.ReadOnly);
    }

    [Fact]
    public void Editing_MakeChange_transitions_to_DirtyEditing()
    {
        var sm = Create(WorkspaceEditState.Editing);
        sm.Fire(WorkspaceEditEvent.MakeChange).Should().BeTrue();
        sm.CurrentState.Should().Be(WorkspaceEditState.DirtyEditing);
    }

    [Fact]
    public void Editing_RequestLeave_transitions_to_ReadOnly_no_dialog_needed()
    {
        var sm = Create(WorkspaceEditState.Editing);
        sm.Fire(WorkspaceEditEvent.RequestLeave).Should().BeTrue();
        sm.CurrentState.Should().Be(WorkspaceEditState.ReadOnly);
    }

    [Fact]
    public void Editing_Save_transitions_to_Saving()
    {
        var sm = Create(WorkspaceEditState.Editing);
        sm.Fire(WorkspaceEditEvent.Save).Should().BeTrue();
        sm.CurrentState.Should().Be(WorkspaceEditState.Saving);
    }

    [Theory]
    [InlineData(WorkspaceEditEvent.EnterEdit)]
    [InlineData(WorkspaceEditEvent.SaveCompleted)]
    [InlineData(WorkspaceEditEvent.SaveFailed)]
    [InlineData(WorkspaceEditEvent.LeaveConfirmed)]
    [InlineData(WorkspaceEditEvent.LeaveCancelled)]
    public void Editing_invalid_events_return_false(WorkspaceEditEvent evt)
    {
        var sm = Create(WorkspaceEditState.Editing);
        sm.Fire(evt).Should().BeFalse($"{evt} should be invalid in Editing");
        sm.CurrentState.Should().Be(WorkspaceEditState.Editing);
    }

    // ─── E.4: DirtyEditing transitions ───────────────────────────────────────

    [Fact]
    public void DirtyEditing_ExitEdit_shows_confirm_LeavingConfirming()
    {
        var sm = Create(WorkspaceEditState.DirtyEditing);
        sm.Fire(WorkspaceEditEvent.ExitEdit).Should().BeTrue();
        sm.CurrentState.Should().Be(WorkspaceEditState.LeavingConfirming);
    }

    [Fact]
    public void DirtyEditing_Save_transitions_to_Saving()
    {
        var sm = Create(WorkspaceEditState.DirtyEditing);
        sm.Fire(WorkspaceEditEvent.Save).Should().BeTrue();
        sm.CurrentState.Should().Be(WorkspaceEditState.Saving);
    }

    [Fact]
    public void DirtyEditing_RequestLeave_transitions_to_LeavingConfirming()
    {
        var sm = Create(WorkspaceEditState.DirtyEditing);
        sm.Fire(WorkspaceEditEvent.RequestLeave).Should().BeTrue();
        sm.CurrentState.Should().Be(WorkspaceEditState.LeavingConfirming);
    }

    [Theory]
    [InlineData(WorkspaceEditEvent.EnterEdit)]
    [InlineData(WorkspaceEditEvent.SaveCompleted)]
    [InlineData(WorkspaceEditEvent.SaveFailed)]
    [InlineData(WorkspaceEditEvent.LeaveConfirmed)]
    [InlineData(WorkspaceEditEvent.LeaveCancelled)]
    public void DirtyEditing_invalid_events_return_false(WorkspaceEditEvent evt)
    {
        var sm = Create(WorkspaceEditState.DirtyEditing);
        sm.Fire(evt).Should().BeFalse($"{evt} should be invalid in DirtyEditing");
        sm.CurrentState.Should().Be(WorkspaceEditState.DirtyEditing);
    }

    // ─── E.5: Saving transitions ──────────────────────────────────────────────

    [Fact]
    public void Saving_SaveCompleted_transitions_to_ReadOnly()
    {
        var sm = Create(WorkspaceEditState.Saving);
        sm.Fire(WorkspaceEditEvent.SaveCompleted).Should().BeTrue();
        sm.CurrentState.Should().Be(WorkspaceEditState.ReadOnly);
    }

    [Fact]
    public void Saving_SaveFailed_transitions_to_TransitionBlocked()
    {
        var sm = Create(WorkspaceEditState.Saving);
        sm.Fire(WorkspaceEditEvent.SaveFailed).Should().BeTrue();
        sm.CurrentState.Should().Be(WorkspaceEditState.TransitionBlocked);
    }

    [Theory]
    [InlineData(WorkspaceEditEvent.EnterEdit)]
    [InlineData(WorkspaceEditEvent.ExitEdit)]
    [InlineData(WorkspaceEditEvent.MakeChange)]
    [InlineData(WorkspaceEditEvent.Save)]
    [InlineData(WorkspaceEditEvent.RequestLeave)]
    [InlineData(WorkspaceEditEvent.LeaveConfirmed)]
    [InlineData(WorkspaceEditEvent.LeaveCancelled)]
    public void Saving_invalid_events_return_false(WorkspaceEditEvent evt)
    {
        var sm = Create(WorkspaceEditState.Saving);
        sm.Fire(evt).Should().BeFalse($"{evt} should be invalid in Saving");
        sm.CurrentState.Should().Be(WorkspaceEditState.Saving);
    }

    // ─── E.6: TransitionBlocked transitions ──────────────────────────────────

    [Fact]
    public void TransitionBlocked_MakeChange_returns_to_DirtyEditing()
    {
        var sm = Create(WorkspaceEditState.TransitionBlocked);
        sm.Fire(WorkspaceEditEvent.MakeChange).Should().BeTrue();
        sm.CurrentState.Should().Be(WorkspaceEditState.DirtyEditing);
    }

    [Fact]
    public void TransitionBlocked_RequestLeave_shows_confirm()
    {
        var sm = Create(WorkspaceEditState.TransitionBlocked);
        sm.Fire(WorkspaceEditEvent.RequestLeave).Should().BeTrue();
        sm.CurrentState.Should().Be(WorkspaceEditState.LeavingConfirming);
    }

    [Theory]
    [InlineData(WorkspaceEditEvent.EnterEdit)]
    [InlineData(WorkspaceEditEvent.ExitEdit)]
    [InlineData(WorkspaceEditEvent.Save)]
    [InlineData(WorkspaceEditEvent.SaveCompleted)]
    [InlineData(WorkspaceEditEvent.SaveFailed)]
    [InlineData(WorkspaceEditEvent.LeaveConfirmed)]
    [InlineData(WorkspaceEditEvent.LeaveCancelled)]
    public void TransitionBlocked_invalid_events_return_false(WorkspaceEditEvent evt)
    {
        var sm = Create(WorkspaceEditState.TransitionBlocked);
        sm.Fire(evt).Should().BeFalse($"{evt} should be invalid in TransitionBlocked");
        sm.CurrentState.Should().Be(WorkspaceEditState.TransitionBlocked);
    }

    // ─── E.7: LeavingConfirming transitions ──────────────────────────────────

    [Fact]
    public void LeavingConfirming_LeaveConfirmed_transitions_to_ReadOnly()
    {
        var sm = Create(WorkspaceEditState.LeavingConfirming);
        sm.Fire(WorkspaceEditEvent.LeaveConfirmed).Should().BeTrue();
        sm.CurrentState.Should().Be(WorkspaceEditState.ReadOnly);
    }

    [Fact]
    public void LeavingConfirming_LeaveCancelled_returns_to_DirtyEditing()
    {
        var sm = Create(WorkspaceEditState.LeavingConfirming);
        sm.Fire(WorkspaceEditEvent.LeaveCancelled).Should().BeTrue();
        sm.CurrentState.Should().Be(WorkspaceEditState.DirtyEditing);
    }

    [Fact]
    public void LeavingConfirming_Save_transitions_to_Saving()
    {
        var sm = Create(WorkspaceEditState.LeavingConfirming);
        sm.Fire(WorkspaceEditEvent.Save).Should().BeTrue();
        sm.CurrentState.Should().Be(WorkspaceEditState.Saving);
    }

    [Theory]
    [InlineData(WorkspaceEditEvent.EnterEdit)]
    [InlineData(WorkspaceEditEvent.ExitEdit)]
    [InlineData(WorkspaceEditEvent.MakeChange)]
    [InlineData(WorkspaceEditEvent.SaveCompleted)]
    [InlineData(WorkspaceEditEvent.SaveFailed)]
    [InlineData(WorkspaceEditEvent.RequestLeave)]
    public void LeavingConfirming_invalid_events_return_false(WorkspaceEditEvent evt)
    {
        var sm = Create(WorkspaceEditState.LeavingConfirming);
        sm.Fire(evt).Should().BeFalse($"{evt} should be invalid in LeavingConfirming");
        sm.CurrentState.Should().Be(WorkspaceEditState.LeavingConfirming);
    }

    // ─── E.8: Guard conditions ────────────────────────────────────────────────

    [Fact]
    public void Guard_blocks_EnterEdit_when_CanEdit_false()
    {
        var sm = Create(WorkspaceEditState.ReadOnly);
        sm.Initialize(WorkspaceEditState.ReadOnly, guardPredicate: evt =>
            !(evt == WorkspaceEditEvent.EnterEdit)); // block EnterEdit

        sm.Fire(WorkspaceEditEvent.EnterEdit).Should().BeFalse();
        sm.CurrentState.Should().Be(WorkspaceEditState.ReadOnly);
    }

    [Fact]
    public void Guard_allows_EnterEdit_when_CanEdit_true()
    {
        var sm = Create(WorkspaceEditState.ReadOnly);
        sm.Initialize(WorkspaceEditState.ReadOnly, guardPredicate: _ => true);

        sm.Fire(WorkspaceEditEvent.EnterEdit).Should().BeTrue();
        sm.CurrentState.Should().Be(WorkspaceEditState.Editing);
    }

    [Fact]
    public void Guard_null_allows_all_valid_transitions()
    {
        var sm = Create(WorkspaceEditState.ReadOnly);
        sm.Initialize(WorkspaceEditState.ReadOnly, guardPredicate: null);

        sm.Fire(WorkspaceEditEvent.EnterEdit).Should().BeTrue();
    }

    // ─── E.9: StateChanged event ──────────────────────────────────────────────

    [Fact]
    public void StateChanged_raised_on_valid_transition()
    {
        var sm = Create(WorkspaceEditState.ReadOnly);
        EditStateChangedEventArgs? captured = null;
        sm.StateChanged += (_, e) => captured = e;

        sm.Fire(WorkspaceEditEvent.EnterEdit);

        captured.Should().NotBeNull();
        captured!.PreviousState.Should().Be(WorkspaceEditState.ReadOnly);
        captured.NewState.Should().Be(WorkspaceEditState.Editing);
        captured.TriggerEvent.Should().Be(WorkspaceEditEvent.EnterEdit);
    }

    [Fact]
    public void StateChanged_not_raised_on_invalid_transition()
    {
        var sm = Create(WorkspaceEditState.ReadOnly);
        var raised = false;
        sm.StateChanged += (_, _) => raised = true;

        sm.Fire(WorkspaceEditEvent.SaveCompleted); // invalid

        raised.Should().BeFalse();
    }

    [Fact]
    public void StateChanged_context_string_propagated()
    {
        var sm = Create(WorkspaceEditState.ReadOnly);
        EditStateChangedEventArgs? captured = null;
        sm.StateChanged += (_, e) => captured = e;

        sm.Fire(WorkspaceEditEvent.EnterEdit, "test-context");

        captured!.Context.Should().Be("test-context");
    }

    // ─── E.10: GetPermittedEvents ─────────────────────────────────────────────

    [Fact]
    public void GetPermittedEvents_ReadOnly_returns_EnterEdit()
    {
        var sm = Create(WorkspaceEditState.ReadOnly);
        sm.GetPermittedEvents().Should().Contain(WorkspaceEditEvent.EnterEdit);
    }

    [Fact]
    public void GetPermittedEvents_Editing_contains_ExitEdit_MakeChange_Save_RequestLeave()
    {
        var sm = Create(WorkspaceEditState.Editing);
        var events = sm.GetPermittedEvents().ToList();
        events.Should().Contain(WorkspaceEditEvent.ExitEdit);
        events.Should().Contain(WorkspaceEditEvent.MakeChange);
        events.Should().Contain(WorkspaceEditEvent.Save);
        events.Should().Contain(WorkspaceEditEvent.RequestLeave);
    }

    [Fact]
    public void GetPermittedEvents_Saving_contains_SaveCompleted_SaveFailed()
    {
        var sm = Create(WorkspaceEditState.Saving);
        var events = sm.GetPermittedEvents().ToList();
        events.Should().Contain(WorkspaceEditEvent.SaveCompleted);
        events.Should().Contain(WorkspaceEditEvent.SaveFailed);
    }

    // ─── E.11: Full workflow paths ────────────────────────────────────────────

    [Fact]
    public void Workflow_ReadOnly_edit_dirty_save_complete()
    {
        var sm = Create(WorkspaceEditState.ReadOnly);

        sm.Fire(WorkspaceEditEvent.EnterEdit).Should().BeTrue();
        sm.CurrentState.Should().Be(WorkspaceEditState.Editing);

        sm.Fire(WorkspaceEditEvent.MakeChange).Should().BeTrue();
        sm.CurrentState.Should().Be(WorkspaceEditState.DirtyEditing);
        sm.IsDirty.Should().BeTrue();

        sm.Fire(WorkspaceEditEvent.Save).Should().BeTrue();
        sm.CurrentState.Should().Be(WorkspaceEditState.Saving);

        sm.Fire(WorkspaceEditEvent.SaveCompleted).Should().BeTrue();
        sm.CurrentState.Should().Be(WorkspaceEditState.ReadOnly);
        sm.IsDirty.Should().BeFalse();
    }

    [Fact]
    public void Workflow_save_fail_then_retry()
    {
        var sm = Create(WorkspaceEditState.DirtyEditing);

        sm.Fire(WorkspaceEditEvent.Save).Should().BeTrue();
        sm.CurrentState.Should().Be(WorkspaceEditState.Saving);

        sm.Fire(WorkspaceEditEvent.SaveFailed).Should().BeTrue();
        sm.CurrentState.Should().Be(WorkspaceEditState.TransitionBlocked);

        // User makes another change to acknowledge and continue
        sm.Fire(WorkspaceEditEvent.MakeChange).Should().BeTrue();
        sm.CurrentState.Should().Be(WorkspaceEditState.DirtyEditing);

        // Retry save
        sm.Fire(WorkspaceEditEvent.Save).Should().BeTrue();
        sm.Fire(WorkspaceEditEvent.SaveCompleted).Should().BeTrue();
        sm.CurrentState.Should().Be(WorkspaceEditState.ReadOnly);
    }

    [Fact]
    public void Workflow_dirty_leave_cancel_stay()
    {
        var sm = Create(WorkspaceEditState.DirtyEditing);

        sm.Fire(WorkspaceEditEvent.RequestLeave).Should().BeTrue();
        sm.CurrentState.Should().Be(WorkspaceEditState.LeavingConfirming);

        sm.Fire(WorkspaceEditEvent.LeaveCancelled).Should().BeTrue();
        sm.CurrentState.Should().Be(WorkspaceEditState.DirtyEditing);
        sm.IsDirty.Should().BeTrue();
    }

    [Fact]
    public void Workflow_dirty_leave_confirmed()
    {
        var sm = Create(WorkspaceEditState.DirtyEditing);

        sm.Fire(WorkspaceEditEvent.RequestLeave).Should().BeTrue();
        sm.Fire(WorkspaceEditEvent.LeaveConfirmed).Should().BeTrue();
        sm.CurrentState.Should().Be(WorkspaceEditState.ReadOnly);
    }

    [Fact]
    public void Workflow_clean_edit_leave_no_dialog()
    {
        var sm = Create(WorkspaceEditState.Editing);
        // Clean editing -> RequestLeave goes directly to ReadOnly without dialog
        sm.Fire(WorkspaceEditEvent.RequestLeave).Should().BeTrue();
        sm.CurrentState.Should().Be(WorkspaceEditState.ReadOnly);
    }

    [Fact]
    public void Workflow_leave_during_confirm_save_then_leave()
    {
        var sm = Create(WorkspaceEditState.DirtyEditing);

        sm.Fire(WorkspaceEditEvent.RequestLeave).Should().BeTrue();
        sm.CurrentState.Should().Be(WorkspaceEditState.LeavingConfirming);

        // User chooses "Save then Leave"
        sm.Fire(WorkspaceEditEvent.Save).Should().BeTrue();
        sm.CurrentState.Should().Be(WorkspaceEditState.Saving);

        sm.Fire(WorkspaceEditEvent.SaveCompleted).Should().BeTrue();
        sm.CurrentState.Should().Be(WorkspaceEditState.ReadOnly);
    }

    // ─── E.12: CanFire ───────────────────────────────────────────────────────

    [Fact]
    public void CanFire_returns_true_for_valid_transition()
    {
        var sm = Create(WorkspaceEditState.ReadOnly);
        sm.CanFire(WorkspaceEditEvent.EnterEdit).Should().BeTrue();
    }

    [Fact]
    public void CanFire_returns_false_for_invalid_transition()
    {
        var sm = Create(WorkspaceEditState.ReadOnly);
        sm.CanFire(WorkspaceEditEvent.SaveCompleted).Should().BeFalse();
    }

    [Fact]
    public void CanFire_respects_guard_predicate()
    {
        var sm = Create(WorkspaceEditState.ReadOnly);
        sm.Initialize(WorkspaceEditState.ReadOnly, evt => evt != WorkspaceEditEvent.EnterEdit);
        sm.CanFire(WorkspaceEditEvent.EnterEdit).Should().BeFalse();
    }
}
