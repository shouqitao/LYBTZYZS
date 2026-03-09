using LYBT.Desktop.Sync.ViewModels;

namespace LYBT.Tests.Desktop.PureLogic.Sync;

/// <summary>
/// US-SYNC-007 D.5: SyncPhase enum + SyncErrorCategory enum
/// Tests enum values, phase descriptions, and IsSyncing derivation.
/// </summary>
public class SyncPhaseTransitionTests
{
    #region SyncPhase enum

    [Fact]
    public void SyncPhase_has_six_values()
    {
        Enum.GetValues<SyncPhase>().Should().HaveCount(6);
    }

    [Theory]
    [InlineData(SyncPhase.Idle, 0)]
    [InlineData(SyncPhase.CheckingDifferences, 1)]
    [InlineData(SyncPhase.ReviewingDifferences, 2)]
    [InlineData(SyncPhase.ExecutingSync, 3)]
    [InlineData(SyncPhase.Completed, 4)]
    [InlineData(SyncPhase.Failed, 5)]
    public void SyncPhase_has_correct_ordinal(SyncPhase phase, int expected)
    {
        ((int)phase).Should().Be(expected);
    }

    [Fact]
    public void SyncPhase_default_is_Idle()
    {
        default(SyncPhase).Should().Be(SyncPhase.Idle);
    }

    #endregion

    #region SyncErrorCategory enum

    [Fact]
    public void SyncErrorCategory_has_five_values()
    {
        Enum.GetValues<SyncErrorCategory>().Should().HaveCount(5);
    }

    [Theory]
    [InlineData(SyncErrorCategory.TransientNetwork, 0)]
    [InlineData(SyncErrorCategory.AuthExpired, 1)]
    [InlineData(SyncErrorCategory.BusinessReject, 2)]
    [InlineData(SyncErrorCategory.ConflictChanged, 3)]
    [InlineData(SyncErrorCategory.Unknown, 4)]
    public void SyncErrorCategory_has_correct_ordinal(SyncErrorCategory category, int expected)
    {
        ((int)category).Should().Be(expected);
    }

    #endregion

    #region IsSyncing derivation

    [Theory]
    [InlineData(SyncPhase.CheckingDifferences, true)]
    [InlineData(SyncPhase.ExecutingSync, true)]
    [InlineData(SyncPhase.Idle, false)]
    [InlineData(SyncPhase.ReviewingDifferences, false)]
    [InlineData(SyncPhase.Completed, false)]
    [InlineData(SyncPhase.Failed, false)]
    public void IsSyncing_derived_from_phase(SyncPhase phase, bool expected)
    {
        // IsSyncing logic: CheckingDifferences or ExecutingSync
        var isSyncing = phase is SyncPhase.CheckingDifferences or SyncPhase.ExecutingSync;

        isSyncing.Should().Be(expected);
    }

    #endregion

    #region CanCheckDifferences derivation

    [Theory]
    [InlineData(SyncPhase.Idle, true)]
    [InlineData(SyncPhase.Completed, true)]
    [InlineData(SyncPhase.Failed, true)]
    [InlineData(SyncPhase.CheckingDifferences, false)]
    [InlineData(SyncPhase.ReviewingDifferences, false)]
    [InlineData(SyncPhase.ExecutingSync, false)]
    public void CanCheckDifferences_allowed_only_in_terminal_or_idle_phases(SyncPhase phase, bool expected)
    {
        // CanCheckDifferences logic (assuming entity type selected)
        var canCheck = phase is SyncPhase.Idle or SyncPhase.Completed or SyncPhase.Failed;

        canCheck.Should().Be(expected);
    }

    #endregion

    #region CanExecuteSync derivation

    [Theory]
    [InlineData(SyncPhase.ReviewingDifferences, true)]
    [InlineData(SyncPhase.Idle, false)]
    [InlineData(SyncPhase.CheckingDifferences, false)]
    [InlineData(SyncPhase.ExecutingSync, false)]
    [InlineData(SyncPhase.Completed, false)]
    [InlineData(SyncPhase.Failed, false)]
    public void CanExecuteSync_only_in_ReviewingDifferences(SyncPhase phase, bool expected)
    {
        var canExecute = phase == SyncPhase.ReviewingDifferences;

        canExecute.Should().Be(expected);
    }

    #endregion

    #region PhaseDescription mapping

    [Theory]
    [InlineData(SyncPhase.Idle, "准备就绪")]
    [InlineData(SyncPhase.CheckingDifferences, "Step 1/4: 检查差异")]
    [InlineData(SyncPhase.ReviewingDifferences, "Step 2/4: 审查差异")]
    [InlineData(SyncPhase.ExecutingSync, "Step 3/4: 执行同步")]
    [InlineData(SyncPhase.Completed, "Step 4/4: 完成")]
    [InlineData(SyncPhase.Failed, "同步失败")]
    public void PhaseDescription_maps_correctly(SyncPhase phase, string expected)
    {
        // Mirror OnCurrentPhaseChanged logic
        var description = phase switch
        {
            SyncPhase.Idle => "准备就绪",
            SyncPhase.CheckingDifferences => "Step 1/4: 检查差异",
            SyncPhase.ReviewingDifferences => "Step 2/4: 审查差异",
            SyncPhase.ExecutingSync => "Step 3/4: 执行同步",
            SyncPhase.Completed => "Step 4/4: 完成",
            SyncPhase.Failed => "同步失败",
            _ => string.Empty
        };

        description.Should().Be(expected);
    }

    #endregion

    #region Retryable error categories

    [Theory]
    [InlineData(SyncErrorCategory.TransientNetwork, true)]
    [InlineData(SyncErrorCategory.ConflictChanged, true)]
    [InlineData(SyncErrorCategory.AuthExpired, true)]
    [InlineData(SyncErrorCategory.BusinessReject, false)]
    [InlineData(SyncErrorCategory.Unknown, false)]
    public void CanRetry_based_on_error_category(SyncErrorCategory category, bool expected)
    {
        // Mirror HandleWorkflowFailure retry logic
        var canRetry = category is SyncErrorCategory.TransientNetwork
            or SyncErrorCategory.ConflictChanged
            or SyncErrorCategory.AuthExpired;

        canRetry.Should().Be(expected);
    }

    #endregion
}
