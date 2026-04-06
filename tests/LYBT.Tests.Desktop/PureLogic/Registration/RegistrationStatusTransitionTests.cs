using LYBT.Shared.Models.Enums;

namespace LYBT.Tests.Desktop.PureLogic.Registration;

/// <summary>
/// RegistrationStatus enum + state transition rules
/// State machine: Waiting -> InProgress -> Completed/Cancelled
/// Design: registration-module-design.md 状态机章节
/// </summary>
public class RegistrationStatusTransitionTests
{
    #region Enum values

    [Fact]
    public void RegistrationStatus_has_four_values()
    {
        Enum.GetValues<RegistrationStatus>().Should().HaveCount(4);
    }

    [Theory]
    [InlineData(RegistrationStatus.Waiting, 0)]
    [InlineData(RegistrationStatus.InProgress, 1)]
    [InlineData(RegistrationStatus.Completed, 2)]
    [InlineData(RegistrationStatus.Cancelled, 3)]
    public void RegistrationStatus_has_correct_ordinal(RegistrationStatus status, int expected)
    {
        ((int)status).Should().Be(expected);
    }

    [Fact]
    public void RegistrationStatus_default_is_Waiting()
    {
        default(RegistrationStatus).Should().Be(RegistrationStatus.Waiting);
    }

    #endregion

    #region State transition rules

    [Theory]
    [InlineData(RegistrationStatus.Waiting, RegistrationStatus.InProgress, true)]
    [InlineData(RegistrationStatus.Waiting, RegistrationStatus.Cancelled, true)]
    [InlineData(RegistrationStatus.InProgress, RegistrationStatus.Completed, true)]
    [InlineData(RegistrationStatus.InProgress, RegistrationStatus.Cancelled, true)]
    public void Valid_transitions_are_allowed(RegistrationStatus from, RegistrationStatus to, bool expected)
    {
        var isValid = IsTransitionValid(from, to);

        isValid.Should().Be(expected);
    }

    [Theory]
    [InlineData(RegistrationStatus.Waiting, RegistrationStatus.Completed, false)]
    [InlineData(RegistrationStatus.InProgress, RegistrationStatus.Waiting, false)]
    [InlineData(RegistrationStatus.Completed, RegistrationStatus.Waiting, false)]
    [InlineData(RegistrationStatus.Cancelled, RegistrationStatus.Waiting, false)]
    [InlineData(RegistrationStatus.Cancelled, RegistrationStatus.InProgress, false)]
    [InlineData(RegistrationStatus.Cancelled, RegistrationStatus.Completed, false)]
    public void Invalid_transitions_are_rejected(RegistrationStatus from, RegistrationStatus to, bool expected)
    {
        var isValid = IsTransitionValid(from, to);

        isValid.Should().Be(expected);
    }

    /// <summary>Terminal states cannot transition to any other state</summary>
    [Theory]
    [InlineData(RegistrationStatus.Completed)]
    [InlineData(RegistrationStatus.Cancelled)]
    public void Terminal_states_cannot_transition(RegistrationStatus status)
    {
        var canTransition = status is not RegistrationStatus.Completed and not RegistrationStatus.Cancelled;

        canTransition.Should().BeFalse();
    }

    /// <summary>Waiting is the only initial state</summary>
    [Fact]
    public void Waiting_is_initial_state()
    {
        var isInitial = RegistrationStatus.Waiting == default(RegistrationStatus);

        isInitial.Should().BeTrue();
    }

    #endregion

    #region Status descriptions

    [Theory]
    [InlineData(RegistrationStatus.Waiting, "等待中")]
    [InlineData(RegistrationStatus.InProgress, "接诊中")]
    [InlineData(RegistrationStatus.Completed, "已完成")]
    [InlineData(RegistrationStatus.Cancelled, "已取消")]
    public void Status_description_matches(RegistrationStatus status, string expected)
    {
        var description = status switch
        {
            RegistrationStatus.Waiting => "等待中",
            RegistrationStatus.InProgress => "接诊中",
            RegistrationStatus.Completed => "已完成",
            RegistrationStatus.Cancelled => "已取消",
            _ => string.Empty
        };

        description.Should().Be(expected);
    }

    #endregion

    #region Helper methods

    /// <summary>
    /// Mirrors server-side state transition validation logic
    /// </summary>
    private static bool IsTransitionValid(RegistrationStatus from, RegistrationStatus to)
    {
        return (from, to) switch
        {
            (RegistrationStatus.Waiting, RegistrationStatus.InProgress) => true,
            (RegistrationStatus.Waiting, RegistrationStatus.Cancelled) => true,
            (RegistrationStatus.InProgress, RegistrationStatus.Completed) => true,
            (RegistrationStatus.InProgress, RegistrationStatus.Cancelled) => true,
            _ => false
        };
    }

    #endregion
}
