using FluentAssertions;
using LYBT.Desktop.Controls.Controls.HerbList;
using LYBT.Desktop.Controls.Models;
using LYBT.Desktop.Infrastructure.Services.FeatureToggle;
using LYBT.Shared.Models.Contracts.Prescriptions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace LYBT.Tests.Desktop.Unit;

/// <summary>
/// F-01: US-CFG-004 功能开关 DuplicateHerbMergeStrategy 消费链路
/// （feature-toggles.json → FeatureToggleService → 药材列表合并行为）。
/// </summary>
public class FeatureToggleDuplicateStrategyTests
{
    private static FeatureToggleService CreateService(string? configuredValue)
    {
        var settings = new Dictionary<string, string?>();
        if (configuredValue != null)
        {
            settings["FeatureToggles:DuplicateHerbMergeStrategy"] = configuredValue;
        }

        var configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        return new FeatureToggleService(configuration, Substitute.For<ILogger<FeatureToggleService>>());
    }

    [Theory]
    [InlineData("Sum", DuplicateDosageStrategy.Sum)]
    [InlineData("sum", DuplicateDosageStrategy.Sum)]
    [InlineData("Min", DuplicateDosageStrategy.Min)]
    [InlineData("Average", DuplicateDosageStrategy.Average)]
    [InlineData("First", DuplicateDosageStrategy.First)]
    [InlineData("Max", DuplicateDosageStrategy.Max)]
    public void GetDuplicateMergeStrategy_ParsesConfiguredValue(string configured, DuplicateDosageStrategy expected)
    {
        using var service = CreateService(configured);

        service.GetDuplicateMergeStrategy().Should().Be(expected);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("NotAStrategy")]
    [InlineData("99")]
    public void GetDuplicateMergeStrategy_FallsBackToMax_WhenMissingOrInvalid(string? configured)
    {
        using var service = CreateService(configured);

        service.GetDuplicateMergeStrategy().Should().Be(DuplicateDosageStrategy.Max);
    }

    [Fact]
    public void ConfiguredStrategy_DrivesHerbListMergeBehavior()
    {
        using var service = CreateService("Sum");
        var herbId = Guid.NewGuid();

        var list = new HerbListControlViewModel { DuplicateStrategy = service.GetDuplicateMergeStrategy() };
        list.AddHerbs(new[] { new PrescriptionItemDto { HerbId = herbId, HerbName = "甘草", Dosage = 3 } });
        list.AddHerbs(new[] { new PrescriptionItemDto { HerbId = herbId, HerbName = "甘草", Dosage = 4 } });

        list.Items.Single(i => !i.IsEmpty).Dosage.Should().Be(7, "Sum 策略应累加重复药材剂量");
    }

    [Fact]
    public void DefaultStrategy_MergesDuplicatesByMax()
    {
        var herbId = Guid.NewGuid();

        var list = new HerbListControlViewModel();
        list.AddHerbs(new[] { new PrescriptionItemDto { HerbId = herbId, HerbName = "甘草", Dosage = 3 } });
        list.AddHerbs(new[] { new PrescriptionItemDto { HerbId = herbId, HerbName = "甘草", Dosage = 4 } });

        list.Items.Single(i => !i.IsEmpty).Dosage.Should().Be(4);
    }
}
