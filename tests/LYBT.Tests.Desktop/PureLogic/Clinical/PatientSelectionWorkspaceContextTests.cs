using FluentAssertions;
using LYBT.Desktop.Clinical.ViewModels;
using LYBT.Desktop.MedicalCase.Models;

namespace LYBT.Tests.Desktop.PureLogic.Clinical;

/// <summary>
/// PatientSelectionWorkspaceContext 单元测试
/// 验证患者选择阶段的空上下文适配器行为（无活跃医案）
/// TDD RED 阶段 - 验证现有实现
/// </summary>
public class PatientSelectionWorkspaceContextTests
{
    private static PatientSelectionWorkspaceContext CreateSut() => new();

    [Fact]
    public void MedicalCaseId_Always_ReturnsGuidEmpty()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        var result = sut.MedicalCaseId;

        // Assert
        result.Should().Be(Guid.Empty);
    }

    [Fact]
    public void CurrentPatient_Always_ReturnsNull()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        var result = sut.CurrentPatient;

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void State_Always_IsReadOnly()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        var state = sut.State;

        // Assert
        state.Should().NotBeNull();
        state.EditState.Should().Be(EditState.ReadOnly);
        state.CanEdit.Should().BeFalse();
    }

    [Fact]
    public void SessionManager_Always_ReturnsNull()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        var result = sut.SessionManager;

        // Assert
        result.Should().BeNull();
    }
}
