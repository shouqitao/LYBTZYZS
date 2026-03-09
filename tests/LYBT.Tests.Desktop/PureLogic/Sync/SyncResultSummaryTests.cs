using LYBT.Desktop.Sync.ViewModels;

namespace LYBT.Tests.Desktop.PureLogic.Sync;

/// <summary>
/// US-SYNC-007 D.2: SyncResultSummary record
/// Tests computed properties TotalProcessed and HasRejections.
/// </summary>
public class SyncResultSummaryTests
{
    [Fact]
    public void TotalProcessed_sums_uploaded_downloaded_deleted_skipped()
    {
        var summary = new SyncResultSummary("Herb", 3, 5, 2, 1, 0, []);

        summary.TotalProcessed.Should().Be(11);
    }

    [Fact]
    public void TotalProcessed_is_zero_when_all_counts_zero()
    {
        var summary = new SyncResultSummary("Patient", 0, 0, 0, 0, 0, []);

        summary.TotalProcessed.Should().Be(0);
    }

    [Fact]
    public void TotalProcessed_excludes_FailedCount()
    {
        var summary = new SyncResultSummary("Herb", 1, 2, 3, 4, 10, []);

        summary.TotalProcessed.Should().Be(10, "FailedCount should not be included in TotalProcessed");
    }

    [Fact]
    public void HasRejections_false_when_empty()
    {
        var summary = new SyncResultSummary("Herb", 5, 3, 0, 0, 0, []);

        summary.HasRejections.Should().BeFalse();
    }

    [Fact]
    public void HasRejections_true_when_rejections_present()
    {
        var summary = new SyncResultSummary("Herb", 5, 3, 0, 0, 0, ["reason1"]);

        summary.HasRejections.Should().BeTrue();
    }

    [Fact]
    public void Record_equality_by_value()
    {
        var rejections = new List<string> { "ref check" };
        var a = new SyncResultSummary("Herb", 1, 2, 3, 0, 0, rejections);
        var b = new SyncResultSummary("Herb", 1, 2, 3, 0, 0, rejections);

        a.Should().Be(b);
    }

    [Fact]
    public void Record_inequality_different_entity_type()
    {
        var a = new SyncResultSummary("Herb", 1, 2, 3, 0, 0, []);
        var b = new SyncResultSummary("Patient", 1, 2, 3, 0, 0, []);

        a.Should().NotBe(b);
    }

    [Fact]
    public void EntityType_is_preserved()
    {
        var summary = new SyncResultSummary("Formula", 0, 0, 0, 0, 0, []);

        summary.EntityType.Should().Be("Formula");
    }
}
