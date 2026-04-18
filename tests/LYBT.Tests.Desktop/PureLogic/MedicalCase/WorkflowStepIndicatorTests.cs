using FluentAssertions;
using LYBT.Desktop.MedicalCase.Controls;
using LYBT.Tests.Desktop.Infrastructure;
using Xunit;

namespace LYBT.Tests.Desktop.PureLogic.MedicalCase;

/// <summary>
/// Phase 1.2: WorkflowStepIndicator tests
/// Tests for workflow step progression and visualization
/// </summary>
public class WorkflowStepIndicatorTests : UserJourneyTestBase
{
    public WorkflowStepIndicatorTests(UserJourneyFixture fixture) : base(fixture)
    {
        // Ensure WPF environment is initialized
        WpfTestHelper.InitializeWpf();
    }

    private WorkflowStepIndicator CreateSut() => new();

    [Fact]
    public void Constructor_InitializesWithDefaults()
    {
        var sut = CreateSut();

        sut.CurrentStep.Should().Be(1);
        sut.StepWidth.Should().Be(120.0);
        sut.Steps.Should().HaveCount(5);
    }

    [Fact]
    public void Constructor_InitializesStepsWithCorrectLabels()
    {
        var sut = CreateSut();

        sut.Steps[0].Label.Should().Be("四诊采集");
        sut.Steps[1].Label.Should().Be("中医辨证");
        sut.Steps[2].Label.Should().Be("处方决策");
        sut.Steps[3].Label.Should().Be("处方编辑");
        sut.Steps[4].Label.Should().Be("完成看诊");
    }

    [Fact]
    public void Constructor_InitializesStep1Active()
    {
        var sut = CreateSut();

        sut.Steps[0].Number.Should().Be(1);
        sut.Steps[0].IsActive.Should().BeTrue();
        sut.Steps[0].IsCompleted.Should().BeFalse();
        sut.Steps[0].IsLast.Should().BeFalse();
    }

    [Fact]
    public void Constructor_InitializesOtherStepsInactive()
    {
        var sut = CreateSut();

        for (int i = 1; i < sut.Steps.Count; i++)
        {
            sut.Steps[i].IsActive.Should().BeFalse();
            sut.Steps[i].IsCompleted.Should().BeFalse();
        }
    }

    [Fact]
    public void Constructor_MarksLastStep()
    {
        var sut = CreateSut();

        sut.Steps[4].IsLast.Should().BeTrue();
    }

    [Fact]
    public void CurrentStep_WhenSetTo2_UpdatesStepStates()
    {
        var sut = CreateSut();
        sut.CurrentStep = 2;

        sut.Steps[0].IsActive.Should().BeFalse();
        sut.Steps[0].IsCompleted.Should().BeTrue();

        sut.Steps[1].IsActive.Should().BeTrue();
        sut.Steps[1].IsCompleted.Should().BeFalse();
    }

    [Fact]
    public void CurrentStep_WhenSetTo3_UpdatesStepStates()
    {
        var sut = CreateSut();
        sut.CurrentStep = 3;

        sut.Steps[0].IsCompleted.Should().BeTrue();
        sut.Steps[1].IsCompleted.Should().BeTrue();
        sut.Steps[2].IsActive.Should().BeTrue();
        sut.Steps[2].IsCompleted.Should().BeFalse();
    }

    [Fact]
    public void CurrentStep_WhenSetTo5_MarksAllPreviousCompleted()
    {
        var sut = CreateSut();
        sut.CurrentStep = 5;

        for (int i = 0; i < 4; i++)
        {
            sut.Steps[i].IsCompleted.Should().BeTrue();
            sut.Steps[i].IsActive.Should().BeFalse();
        }

        sut.Steps[4].IsActive.Should().BeTrue();
        sut.Steps[4].IsCompleted.Should().BeFalse();
    }

    [Fact]
    public void CurrentStep_CanBeSetBackwards()
    {
        var sut = CreateSut();
        sut.CurrentStep = 4;

        sut.CurrentStep = 2;

        sut.Steps[0].IsCompleted.Should().BeTrue();
        sut.Steps[1].IsActive.Should().BeTrue();
        sut.Steps[1].IsCompleted.Should().BeFalse();
        sut.Steps[2].IsActive.Should().BeFalse();
        sut.Steps[2].IsCompleted.Should().BeFalse();
    }

    [Fact]
    public void StepWidth_CanBeCustomized()
    {
        var sut = CreateSut();
        sut.StepWidth = 150.0;

        sut.StepWidth.Should().Be(150.0);
    }

    [Fact]
    public void Steps_CollectionIsObservable()
    {
        var sut = CreateSut();

        sut.Steps.Should().NotBeNull();
        sut.Steps.Should().HaveCount(5);
    }

    [Fact]
    public void CurrentStep_WhenChanged_RebuildsStepsCollection()
    {
        var sut = CreateSut();
        var initialCount = sut.Steps.Count;

        sut.CurrentStep = 3;

        sut.Steps.Should().HaveCount(initialCount);
    }

    [Fact]
    public void WorkflowStep_Model_HasAllProperties()
    {
        var step = new WorkflowStep
        {
            Number = 1,
            Label = "测试步骤",
            IsActive = true,
            IsCompleted = false,
            IsLast = false
        };

        step.Number.Should().Be(1);
        step.Label.Should().Be("测试步骤");
        step.IsActive.Should().BeTrue();
        step.IsCompleted.Should().BeFalse();
        step.IsLast.Should().BeFalse();
    }
}
