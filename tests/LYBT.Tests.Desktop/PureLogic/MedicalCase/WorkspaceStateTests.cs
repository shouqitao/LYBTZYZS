using LYBT.Desktop.MedicalCase.Models;
using Xunit;

namespace LYBT.Tests.Desktop.PureLogic.MedicalCase;

public class WorkspaceStateTests
{
    [Fact]
    public void Default_state_is_editing_create_mode()
    {
        var state = new WorkspaceState();
        Assert.Equal(EditState.Editing, state.EditState);
        Assert.Equal(EditType.Create, state.EditType);
        Assert.Equal(WorkspaceMode.Clinical, state.Mode);
        Assert.True(state.IsEditing);
        Assert.False(state.IsReadOnly);
    }

    [Fact]
    public void EnterReadOnlyMode_returns_new_instance_with_readonly()
    {
        var state = new WorkspaceState();
        var readOnly = state.EnterReadOnlyMode();

        Assert.True(readOnly.IsReadOnly);
        Assert.False(readOnly.IsEditing);
        Assert.True(state.IsEditing); // Original unchanged (immutable)
    }

    [Fact]
    public void EnterEditMode_when_CanEdit_returns_editing_state()
    {
        var state = new WorkspaceState(CanEdit: true, EditState: EditState.ReadOnly);
        var editing = state.EnterEditMode();
        Assert.True(editing.IsEditing);
    }

    [Fact]
    public void EnterEditMode_when_CannotEdit_returns_same_state()
    {
        var state = new WorkspaceState(CanEdit: false, EditState: EditState.ReadOnly);
        var result = state.EnterEditMode();
        Assert.True(result.IsReadOnly);
    }

    [Fact]
    public void DetermineFromContext_completed_case_owner_clinical()
    {
        var state = new WorkspaceState();
        var result = state.DetermineFromContext(
            workspaceMode: WorkspaceMode.Clinical, isCompleted: true,
            isOwner: true, isAdmin: false, preferEditing: true);

        Assert.False(result.CanEdit); // Completed + owner (not admin) => cannot edit
        Assert.True(result.IsReadOnly);
        Assert.Equal(EditType.EditCompleted, result.EditType);
    }

    [Fact]
    public void DetermineFromContext_suspended_case_owner_clinical()
    {
        var state = new WorkspaceState();
        var result = state.DetermineFromContext(
            workspaceMode: WorkspaceMode.Clinical, isCompleted: false,
            isOwner: true, isAdmin: false, preferEditing: true);

        Assert.True(result.CanEdit);
        Assert.True(result.IsEditing);
        Assert.Equal(EditType.EditSuspended, result.EditType);
    }

    [Fact]
    public void DetermineFromContext_admin_can_always_edit()
    {
        var state = new WorkspaceState();
        var result = state.DetermineFromContext(
            workspaceMode: WorkspaceMode.Management, isCompleted: true,
            isOwner: false, isAdmin: true, preferEditing: false);

        Assert.True(result.CanEdit);
        Assert.True(result.IsReadOnly); // preferEditing=false
        Assert.Equal(WorkspaceMode.Management, result.Mode);
    }

    [Theory]
    [InlineData(WorkspaceMode.Clinical, true, "看诊中")]
    [InlineData(WorkspaceMode.Clinical, false, "查看医案")]
    [InlineData(WorkspaceMode.Management, true, "编辑医案")]
    [InlineData(WorkspaceMode.Management, false, "查看医案")]
    public void HeaderTitle_matches_mode_and_editing(WorkspaceMode mode, bool isEditing, string expected)
    {
        var editState = isEditing ? EditState.Editing : EditState.ReadOnly;
        var state = new WorkspaceState(Mode: mode, EditState: editState);
        Assert.Equal(expected, state.HeaderTitle);
    }

    [Fact]
    public void ShowSuspendButton_only_when_editing_clinical()
    {
        var editing = new WorkspaceState(EditState: EditState.Editing, Mode: WorkspaceMode.Clinical);
        var readOnly = new WorkspaceState(EditState: EditState.ReadOnly, Mode: WorkspaceMode.Clinical);
        var mgmt = new WorkspaceState(EditState: EditState.Editing, Mode: WorkspaceMode.Management);

        Assert.True(editing.ShowSuspendButton);
        Assert.False(readOnly.ShowSuspendButton);
        Assert.False(mgmt.ShowSuspendButton);
    }

    [Fact]
    public void With_expression_creates_new_instance()
    {
        var state = new WorkspaceState(Remark: "old");
        var updated = state with { Remark = "new" };

        Assert.Equal("old", state.Remark);
        Assert.Equal("new", updated.Remark);
    }

    [Fact]
    public void DetermineFromContext_non_owner_non_admin_gets_readonly()
    {
        var state = new WorkspaceState();
        var result = state.DetermineFromContext(
            workspaceMode: WorkspaceMode.Management, isCompleted: false,
            isOwner: false, isAdmin: false, preferEditing: true);

        Assert.False(result.CanEdit);
        Assert.True(result.IsReadOnly); // preferEditing=true but CanEdit=false
    }

    [Fact]
    public void DetermineFromContext_admin_completed_preferEditing_gets_editing()
    {
        var state = new WorkspaceState();
        var result = state.DetermineFromContext(
            workspaceMode: WorkspaceMode.Management, isCompleted: true,
            isOwner: false, isAdmin: true, preferEditing: true);

        Assert.True(result.CanEdit);
        Assert.True(result.IsEditing); // admin can edit even completed, preferEditing=true
        Assert.True(result.IsHistoricalEditMode);
    }

    [Theory]
    [InlineData(WorkspaceMode.Clinical, "返回患者选择")]
    [InlineData(WorkspaceMode.Management, "返回医案列表")]
    public void BackButtonText_matches_mode(WorkspaceMode mode, string expected)
    {
        var state = new WorkspaceState(Mode: mode);
        Assert.Equal(expected, state.BackButtonText);
    }
}
