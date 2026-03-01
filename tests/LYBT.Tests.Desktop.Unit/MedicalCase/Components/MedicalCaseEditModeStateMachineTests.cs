using FluentAssertions;
using LYBT.Desktop.MedicalCase.Models;
using LYBT.Desktop.MedicalCase.ViewModels.Components;
using Xunit;

namespace LYBT.Desktop.MedicalCase.Tests.Components
{
    /// <summary>
    /// MedicalCaseEditModeStateMachine单元测试
    /// OpenSpec: refactor-viewmodel-layer Phase 1
    /// </summary>
    public class MedicalCaseEditModeStateMachineTests
    {
        private readonly MedicalCaseEditModeStateMachine _sut;

        public MedicalCaseEditModeStateMachineTests()
        {
            _sut = new MedicalCaseEditModeStateMachine();
        }

        #region 初始状态测试

        [Fact]
        public void Constructor_ShouldSetDefaultValues()
        {
            // Assert - 默认值
            _sut.WorkspaceMode.Should().Be(WorkspaceMode.Clinical);
            _sut.EditState.Should().Be(EditState.Editing);
            _sut.EditType.Should().Be(EditType.Create);
            _sut.CanEdit.Should().BeFalse();
            _sut.HasUnsavedChanges.Should().BeFalse();
            _sut.EditReason.Should().BeEmpty();
        }

        [Fact]
        public void Constructor_ShouldSetIsEditingTrue_ByDefault()
        {
            // Assert
            _sut.IsEditing.Should().BeTrue();
            _sut.IsReadOnly.Should().BeFalse();
        }

        #endregion

        #region 计算属性测试

        [Fact]
        public void IsHistoricalEditMode_ShouldReturnTrue_WhenEditTypeIsEditCompleted()
        {
            // Arrange
            _sut.EditType = EditType.EditCompleted;

            // Assert
            _sut.IsHistoricalEditMode.Should().BeTrue();
        }

        [Fact]
        public void IsHistoricalEditMode_ShouldReturnFalse_WhenEditTypeIsNotEditCompleted()
        {
            // Arrange & Act
            _sut.EditType = EditType.Create;

            // Assert
            _sut.IsHistoricalEditMode.Should().BeFalse();
        }

        [Fact]
        public void ShowEditButton_ShouldReturnTrue_WhenReadOnlyAndCanEditInClinicalMode()
        {
            // Arrange
            _sut.Initialize(WorkspaceMode.Clinical, EditType.ViewOnly, canEdit: true, EditState.ReadOnly);

            // Assert
            _sut.ShowEditButton.Should().BeTrue();
        }

        [Fact]
        public void ShowEditButton_ShouldReturnFalse_WhenEditing()
        {
            // Arrange
            _sut.Initialize(WorkspaceMode.Clinical, EditType.EditSuspended, canEdit: true, EditState.Editing);

            // Assert
            _sut.ShowEditButton.Should().BeFalse();
        }

        [Fact]
        public void ShowEditButtonTopRight_ShouldReturnTrue_WhenReadOnlyAndCanEditInManagementMode()
        {
            // Arrange
            _sut.Initialize(WorkspaceMode.Management, EditType.ViewOnly, canEdit: true, EditState.ReadOnly);

            // Assert
            _sut.ShowEditButtonTopRight.Should().BeTrue();
        }

        [Fact]
        public void ShowSaveButton_ShouldReturnTrue_WhenEditingInManagementMode()
        {
            // Arrange
            _sut.Initialize(WorkspaceMode.Management, EditType.EditSuspended, canEdit: true, EditState.Editing);

            // Assert
            _sut.ShowSaveButton.Should().BeTrue();
        }

        [Fact]
        public void ShowSuspendButton_ShouldReturnTrue_WhenEditingInClinicalMode()
        {
            // Arrange
            _sut.Initialize(WorkspaceMode.Clinical, EditType.EditSuspended, canEdit: true, EditState.Editing);

            // Assert
            _sut.ShowSuspendButton.Should().BeTrue();
        }

        [Fact]
        public void ShowCompleteButton_ShouldReturnTrue_WhenEditingInClinicalMode()
        {
            // Arrange
            _sut.Initialize(WorkspaceMode.Clinical, EditType.EditSuspended, canEdit: true, EditState.Editing);

            // Assert
            _sut.ShowCompleteButton.Should().BeTrue();
        }

        #endregion

        #region 标题和显示文本测试

        [Fact]
        public void HeaderTitle_ShouldReturn看诊中_WhenClinicalAndEditing()
        {
            // Arrange
            _sut.Initialize(WorkspaceMode.Clinical, EditType.Create, canEdit: true, EditState.Editing);

            // Assert
            _sut.HeaderTitle.Should().Be("看诊中");
        }

        [Fact]
        public void HeaderTitle_ShouldReturn查看医案_WhenClinicalAndReadOnly()
        {
            // Arrange
            _sut.Initialize(WorkspaceMode.Clinical, EditType.ViewOnly, canEdit: false, EditState.ReadOnly);

            // Assert
            _sut.HeaderTitle.Should().Be("查看医案");
        }

        [Fact]
        public void HeaderTitle_ShouldReturn编辑医案_WhenManagementAndEditing()
        {
            // Arrange
            _sut.Initialize(WorkspaceMode.Management, EditType.EditSuspended, canEdit: true, EditState.Editing);

            // Assert
            _sut.HeaderTitle.Should().Be("编辑医案");
        }

        [Fact]
        public void BackButtonText_ShouldReturn返回患者选择_WhenClinicalMode()
        {
            // Arrange
            _sut.WorkspaceMode = WorkspaceMode.Clinical;

            // Assert
            _sut.BackButtonText.Should().Be("返回患者选择");
        }

        [Fact]
        public void BackButtonText_ShouldReturn返回医案列表_WhenManagementMode()
        {
            // Arrange
            _sut.WorkspaceMode = WorkspaceMode.Management;

            // Assert
            _sut.BackButtonText.Should().Be("返回医案列表");
        }

        [Fact]
        public void EditStateText_ShouldReturn编辑中_WhenEditingAndNotHistorical()
        {
            // Arrange
            _sut.Initialize(WorkspaceMode.Clinical, EditType.Create, canEdit: true, EditState.Editing);

            // Assert
            _sut.EditStateText.Should().Be("编辑中");
        }

        [Fact]
        public void EditStateText_ShouldReturn历史编辑中_WhenEditingAndHistorical()
        {
            // Arrange
            _sut.Initialize(WorkspaceMode.Clinical, EditType.EditCompleted, canEdit: true, EditState.Editing);

            // Assert
            _sut.EditStateText.Should().Be("历史编辑中");
        }

        [Fact]
        public void EditStateText_ShouldReturn只读_WhenReadOnly()
        {
            // Arrange
            _sut.Initialize(WorkspaceMode.Clinical, EditType.ViewOnly, canEdit: false, EditState.ReadOnly);

            // Assert
            _sut.EditStateText.Should().Be("只读");
        }

        #endregion

        #region 状态转换测试

        [Fact]
        public void EnterEditMode_ShouldReturnTrue_WhenCanEdit()
        {
            // Arrange
            _sut.Initialize(WorkspaceMode.Clinical, EditType.EditSuspended, canEdit: true, EditState.ReadOnly);

            // Act
            var result = _sut.EnterEditMode();

            // Assert
            result.Should().BeTrue();
            _sut.IsEditing.Should().BeTrue();
            _sut.IsReadOnly.Should().BeFalse();
        }

        [Fact]
        public void EnterEditMode_ShouldReturnFalse_WhenCannotEdit()
        {
            // Arrange
            _sut.Initialize(WorkspaceMode.Clinical, EditType.ViewOnly, canEdit: false, EditState.ReadOnly);

            // Act
            var result = _sut.EnterEditMode();

            // Assert
            result.Should().BeFalse();
            _sut.IsReadOnly.Should().BeTrue();
        }

        [Fact]
        public void EnterReadOnlyMode_ShouldSetEditStateToReadOnly()
        {
            // Arrange
            _sut.Initialize(WorkspaceMode.Clinical, EditType.Create, canEdit: true, EditState.Editing);

            // Act
            _sut.EnterReadOnlyMode();

            // Assert
            _sut.IsReadOnly.Should().BeTrue();
            _sut.IsEditing.Should().BeFalse();
        }

        [Fact]
        public void EditStateChanged_ShouldRaiseEvent_WhenStateChanges()
        {
            // Arrange
            EditStateChangedEventArgs? receivedArgs = null;
            _sut.EditStateChanged += (sender, args) => receivedArgs = args;
            _sut.Initialize(WorkspaceMode.Clinical, EditType.EditSuspended, canEdit: true, EditState.Editing);

            // Act
            _sut.EnterReadOnlyMode();

            // Assert
            receivedArgs.Should().NotBeNull();
            receivedArgs!.OldState.Should().Be(EditState.Editing);
            receivedArgs.NewState.Should().Be(EditState.ReadOnly);
        }

        #endregion

        #region Initialize测试

        [Fact]
        public void Initialize_ShouldSetAllProperties()
        {
            // Act
            _sut.Initialize(
                WorkspaceMode.Management,
                EditType.EditSuspended,
                canEdit: true,
                EditState.ReadOnly);

            // Assert
            _sut.WorkspaceMode.Should().Be(WorkspaceMode.Management);
            _sut.EditType.Should().Be(EditType.EditSuspended);
            _sut.CanEdit.Should().BeTrue();
            _sut.EditState.Should().Be(EditState.ReadOnly);
        }

        [Fact]
        public void Initialize_ShouldSetEditingForCreate_RegardlessOfInitialState()
        {
            // Act
            _sut.Initialize(
                WorkspaceMode.Clinical,
                EditType.Create,
                canEdit: true,
                EditState.ReadOnly); // 即使传入ReadOnly

            // Assert - Create类型应该始终是Editing
            _sut.IsEditing.Should().BeTrue();
        }

        [Fact]
        public void Initialize_ShouldSetReadOnly_WhenViewOnly()
        {
            // Act
            _sut.Initialize(
                WorkspaceMode.Clinical,
                EditType.ViewOnly,
                canEdit: false,
                EditState.Editing); // 即使传入Editing

            // Assert - ViewOnly类型应该始终是ReadOnly
            _sut.IsReadOnly.Should().BeTrue();
        }

        #endregion

        #region DetermineFromContext测试

        [Fact]
        public void DetermineFromContext_ShouldSetCanEditTrue_WhenAdmin()
        {
            // Act
            _sut.DetermineFromContext(
                WorkspaceMode.Management,
                isCompleted: true,
                isOwner: false,
                isAdmin: true,
                preferEditing: true);

            // Assert
            _sut.CanEdit.Should().BeTrue();
        }

        [Fact]
        public void DetermineFromContext_ShouldSetCanEditTrue_WhenOwnerAndNotCompleted()
        {
            // Act
            _sut.DetermineFromContext(
                WorkspaceMode.Clinical,
                isCompleted: false,
                isOwner: true,
                isAdmin: false,
                preferEditing: true);

            // Assert
            _sut.CanEdit.Should().BeTrue();
        }

        [Fact]
        public void DetermineFromContext_ShouldSetCanEditFalse_WhenOwnerButCompleted()
        {
            // Act
            _sut.DetermineFromContext(
                WorkspaceMode.Clinical,
                isCompleted: true,
                isOwner: true,
                isAdmin: false,
                preferEditing: true);

            // Assert
            _sut.CanEdit.Should().BeFalse();
        }

        [Fact]
        public void DetermineFromContext_ShouldSetEditTypeEditCompleted_WhenCompleted()
        {
            // Act
            _sut.DetermineFromContext(
                WorkspaceMode.Management,
                isCompleted: true,
                isOwner: true,
                isAdmin: true,
                preferEditing: true);

            // Assert
            _sut.EditType.Should().Be(EditType.EditCompleted);
        }

        [Fact]
        public void DetermineFromContext_ShouldSetEditTypeEditSuspended_WhenNotCompleted()
        {
            // Act
            _sut.DetermineFromContext(
                WorkspaceMode.Clinical,
                isCompleted: false,
                isOwner: true,
                isAdmin: false,
                preferEditing: true);

            // Assert
            _sut.EditType.Should().Be(EditType.EditSuspended);
        }

        [Fact]
        public void DetermineFromContext_ShouldSetEditing_WhenPreferEditingAndCanEdit()
        {
            // Act
            _sut.DetermineFromContext(
                WorkspaceMode.Clinical,
                isCompleted: false,
                isOwner: true,
                isAdmin: false,
                preferEditing: true);

            // Assert
            _sut.IsEditing.Should().BeTrue();
        }

        [Fact]
        public void DetermineFromContext_ShouldSetReadOnly_WhenNotPreferEditing()
        {
            // Act
            _sut.DetermineFromContext(
                WorkspaceMode.Clinical,
                isCompleted: false,
                isOwner: true,
                isAdmin: false,
                preferEditing: false);

            // Assert
            _sut.IsReadOnly.Should().BeTrue();
        }

        #endregion

        #region CanEnterEditMode测试

        [Fact]
        public void CanEnterEditMode_ShouldReturnTrue_WhenReadOnlyAndCanEdit()
        {
            // Arrange
            _sut.Initialize(WorkspaceMode.Clinical, EditType.EditSuspended, canEdit: true, EditState.ReadOnly);

            // Assert
            _sut.CanEnterEditMode.Should().BeTrue();
        }

        [Fact]
        public void CanEnterEditMode_ShouldReturnFalse_WhenAlreadyEditing()
        {
            // Arrange
            _sut.Initialize(WorkspaceMode.Clinical, EditType.EditSuspended, canEdit: true, EditState.Editing);

            // Assert
            _sut.CanEnterEditMode.Should().BeFalse();
        }

        [Fact]
        public void CanEnterEditMode_ShouldReturnFalse_WhenCannotEdit()
        {
            // Arrange
            _sut.Initialize(WorkspaceMode.Clinical, EditType.ViewOnly, canEdit: false, EditState.ReadOnly);

            // Assert
            _sut.CanEnterEditMode.Should().BeFalse();
        }

        #endregion

        #region HasUnsavedChanges测试

        [Fact]
        public void HasUnsavedChanges_ShouldBeSettable()
        {
            // Arrange & Act
            _sut.HasUnsavedChanges = true;

            // Assert
            _sut.HasUnsavedChanges.Should().BeTrue();
        }

        #endregion

        #region EditReason测试

        [Fact]
        public void EditReason_ShouldBeSettable()
        {
            // Arrange & Act
            _sut.EditReason = "修正诊断信息";

            // Assert
            _sut.EditReason.Should().Be("修正诊断信息");
        }

        #endregion
    }
}
