using FluentAssertions;
using LYBT.Entities.Prescriptions;
using Xunit;

namespace LYBT.Tests.Server.PureLogic.Entities.Prescriptions;

/// <summary>
/// Prescription entity tests - business defaults and collection behavior only.
/// Trivial property getter/setter tests removed (test restructuring 2026-03-05).
/// </summary>
public class PrescriptionModelTests
{
    [Fact]
    public void Constructor_ShouldInitializeBusinessDefaults()
    {
        var prescription = new Prescription();

        prescription.DosageCount.Should().Be(7, "默认帖数为7");
        prescription.Discount.Should().Be(1.0m, "默认折扣为1（不打折）");
        prescription.Items.Should().NotBeNull();
        prescription.Items.Should().BeEmpty();
    }

    [Fact]
    public void Items_ShouldSupportAddingWithForeignKey()
    {
        var prescription = new Prescription { Id = Guid.NewGuid() };
        var item = new PrescriptionItem
        {
            Id = Guid.NewGuid(),
            PrescriptionId = prescription.Id,
            HerbId = Guid.NewGuid(),
            HerbName = "当归",
            Dosage = 12,
            Unit = "g",
            UnitPrice = 2.5m
        };

        prescription.Items.Add(item);

        prescription.Items.Should().HaveCount(1);
        prescription.Items.First().PrescriptionId.Should().Be(prescription.Id);
    }
}
