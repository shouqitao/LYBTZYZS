using FluentAssertions;
using LYBT.Shared.Configuration.Options.Server;
using LYBT.Tests.Server.Infrastructure;
using Xunit;

namespace LYBT.Tests.Server.Features.Infrastructure;

/// <summary>
/// Could Have User Stories for Logging module.
/// PRD: US-LOG-005 (Log cleanup configuration), US-LOG-006 (Audit retention configuration)
/// Collection: SystemOps (isolated DB, parallel with other domains)
/// </summary>
[Collection("SystemOps")]
public sealed class US_Logging_CouldHaveTests : IntegrationTestBase<SystemOpsFixture>
{
    public US_Logging_CouldHaveTests(SystemOpsFixture fixture) : base(fixture) { }

    #region US-LOG-005: Log cleanup configuration defaults

    [Fact]
    public void US_LOG_005_LogCleanupOptions_DefaultRetentionDays_Is90()
    {
        // Arrange & Act
        var options = new LoggingOptions();

        // Assert
        options.Cleanup.RetentionDays.Should().Be(90,
            "US-LOG-005: default log retention should be 90 days");
    }

    [Fact]
    public void US_LOG_005_LogCleanupOptions_DefaultEnabled_IsTrue()
    {
        // Arrange & Act
        var options = new LoggingOptions();

        // Assert
        options.Cleanup.Enabled.Should().BeTrue(
            "US-LOG-005: log cleanup should be enabled by default");
    }

    [Fact]
    public void US_LOG_005_LogCleanupOptions_DefaultCleanupIntervalHours_Is24()
    {
        // Arrange & Act
        var options = new LoggingOptions();

        // Assert
        options.Cleanup.CleanupIntervalHours.Should().Be(24,
            "US-LOG-005: default cleanup interval should be 24 hours");
    }

    [Fact]
    public void US_LOG_005_LogCleanupOptions_DefaultBatchSize_Is1000()
    {
        // Arrange & Act
        var options = new LoggingOptions();

        // Assert
        options.Cleanup.BatchSize.Should().Be(1000,
            "US-LOG-005: default batch size should be 1000 records");
    }

    #endregion

    #region US-LOG-006: Audit log retention configuration defaults

    [Fact]
    public void US_LOG_006_SecurityOptions_DefaultAuditRetentionDays_Is365()
    {
        // Arrange & Act
        var secOpts = new SecurityOptions();

        // Assert
        secOpts.AuditRetentionDays.Should().Be(365,
            "US-LOG-006: audit log retention should default to 365 days (1 year)");
    }

    #endregion
}
