using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.MedicalCase.Interfaces;
using LYBT.Desktop.MedicalCase.Models.Items;
using LYBT.Desktop.MedicalCase.ViewModels.Workspace;
using LYBT.Shared.Models.Contracts.Prescriptions;
using Microsoft.Extensions.Logging;

namespace LYBT.Tests.Desktop.PureLogic.MedicalCase.Workspace;

public class PrescriptionEditorPureTests
{
    private readonly IMedicalCaseWorkspaceContext _context;
    private readonly IWorkspaceHost _host;
    private readonly ILoggerFactory _loggerFactory;

    public PrescriptionEditorPureTests()
    {
        _context = Substitute.For<IMedicalCaseWorkspaceContext>();
        _context.MedicalCaseId.Returns(Guid.NewGuid());
        _host = Substitute.For<IWorkspaceHost>();
        _loggerFactory = Substitute.For<ILoggerFactory>();
        _loggerFactory.CreateLogger(Arg.Any<string>()).Returns(Substitute.For<ILogger>());
    }

    private PrescriptionEditorViewModel CreateSut() => new(_context, _host, _loggerFactory);

    [Fact]
    public void InitializeForNewCase_clears_prescription_and_sets_medicalCaseId()
    {
        var sut = CreateSut();

        sut.InitializeForNewCase();

        sut.Prescription.MedicalCaseId.Should().Be(_context.MedicalCaseId);
        sut.Prescription.Items.Should().BeEmpty();
        sut.HasItems.Should().BeFalse();
    }

    [Fact]
    public void HasItems_reflects_collection_state()
    {
        var sut = CreateSut();

        sut.HasItems.Should().BeFalse();

        sut.Prescription.Items.Add(new PrescriptionItemDto { HerbName = "Herb A", Dosage = 10 });

        sut.HasItems.Should().BeTrue();
    }

    [Fact]
    public void Adding_item_notifies_host_state_changed()
    {
        var sut = CreateSut();

        sut.Prescription.Items.Add(new PrescriptionItemDto { HerbName = "Herb A", Dosage = 10 });

        _host.Received(1).NotifyStateChanged();
    }

    [Fact]
    public void Removing_item_notifies_host_state_changed()
    {
        var sut = CreateSut();
        var item = new PrescriptionItemDto { HerbName = "Herb A", Dosage = 10 };
        sut.Prescription.Items.Add(item);
        _host.ClearReceivedCalls();

        sut.Prescription.Items.Remove(item);

        _host.Received(1).NotifyStateChanged();
    }

    [Fact]
    public void Validate_fails_when_no_items()
    {
        var sut = CreateSut();

        sut.Validate().Should().BeFalse();
        sut.ValidationMessage.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Validate_succeeds_when_has_items()
    {
        var sut = CreateSut();
        sut.Prescription.Items.Add(new PrescriptionItemDto { HerbName = "Herb A", Dosage = 10 });

        sut.Validate().Should().BeTrue();
        sut.ValidationMessage.Should().BeEmpty();
    }

    [Fact]
    public void Reset_clears_items_and_resets_defaults()
    {
        var sut = CreateSut();
        sut.Prescription.Items.Add(new PrescriptionItemDto { HerbName = "Herb A", Dosage = 10 });
        sut.Prescription.DosageCount = 14;
        sut.Prescription.Advice = "some advice";

        sut.Reset();

        sut.Prescription.Items.Should().BeEmpty();
        sut.Prescription.DosageCount.Should().Be(7);
        sut.Prescription.Advice.Should().BeNull();
        sut.HasItems.Should().BeFalse();
    }

    [Fact]
    public void GetPrescriptionData_returns_null_when_no_items()
    {
        var sut = CreateSut();

        sut.GetPrescriptionData().Should().BeNull();
    }

    [Fact]
    public void Dispose_unsubscribes_from_collection_changes()
    {
        var sut = CreateSut();
        sut.Dispose();
        _host.ClearReceivedCalls();

        // After dispose, adding items should NOT notify host
        sut.Prescription.Items.Add(new PrescriptionItemDto { HerbName = "Herb A", Dosage = 10 });

        _host.DidNotReceive().NotifyStateChanged();
    }

    [Fact]
    public void InitializeFromDto_copies_prescription_fields()
    {
        var sut = CreateSut();
        var dto = new PrescriptionDetailDto
        {
            Id = Guid.NewGuid(),
            MedicalCaseId = Guid.NewGuid(),
            DosageCount = 14,
            Usage = "custom usage",
            Advice = "take after meals",
            Discount = 0.8m,
            Items = new List<PrescriptionItemDto>
            {
                new() { HerbName = "Herb A", Dosage = 10 },
                new() { HerbName = "Herb B", Dosage = 15 }
            }
        };

        sut.InitializeFromDto(dto);

        sut.Prescription.Id.Should().Be(dto.Id);
        sut.Prescription.DosageCount.Should().Be(14);
        sut.Prescription.Usage.Should().Be("custom usage");
        sut.Prescription.Advice.Should().Be("take after meals");
        sut.Prescription.Discount.Should().Be(0.8m);
        sut.Prescription.Items.Should().HaveCount(2);
    }
}
