using System;
using System.Collections.Generic;
using FluentAssertions;
using LYBT.Desktop.MedicalCase.Interfaces;
using LYBT.Desktop.MedicalCase.Models.Items;
using LYBT.Desktop.MedicalCase.ViewModels.Workspace;
using LYBT.Desktop.Contracts.Services;
using LYBT.Shared.Models.Contracts.Prescriptions;
using LYBT.Shared.Models.Enums;
using LYBT.Tests.Desktop.Infrastructure;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace LYBT.Tests.Desktop.PureLogic.MedicalCase;

public class PrescriptionEditorViewModelTests : UserJourneyTestBase
{
    private readonly IMedicalCaseWorkspaceContext _context;
    private readonly IWorkspaceHost _host;
    private readonly ILoggerFactory _loggerFactory;

    public PrescriptionEditorViewModelTests(UserJourneyFixture fixture) : base(fixture)
    {
        _context = Substitute.For<IMedicalCaseWorkspaceContext>();
        _context.MedicalCaseId.Returns(Guid.NewGuid());

        _host = Substitute.For<IWorkspaceHost>();
        _loggerFactory = Substitute.For<ILoggerFactory>();
        _loggerFactory.CreateLogger(Arg.Any<string>()).Returns(Substitute.For<ILogger>());
    }

    private PrescriptionEditorViewModel CreateSut() => new(_context, _host, _loggerFactory);

    [Fact]
    public void Constructor_InitializesEmptyPrescriptionAndHooksCollection()
    {
        var sut = CreateSut();

        sut.Prescription.Should().NotBeNull();
        sut.Prescription.Items.Should().BeEmpty();
        sut.HasItems.Should().BeFalse();
    }

    [Fact]
    public void InitializeForNewCase_SetsMedicalCaseId_AndClearsItems()
    {
        var sut = CreateSut();
        sut.Prescription.Items.Add(new PrescriptionItemDto { HerbName = "人参", Dosage = 10 });

        sut.InitializeForNewCase();

        sut.Prescription.MedicalCaseId.Should().Be(_context.MedicalCaseId);
        sut.Prescription.Items.Should().BeEmpty();
        sut.HasItems.Should().BeFalse();
    }

    [Fact]
    public void InitializeFromDto_CopiesFieldsAndItems()
    {
        var sut = CreateSut();
        var dto = new PrescriptionDetailDto
        {
            Id = Guid.NewGuid(),
            MedicalCaseId = Guid.NewGuid(),
            PrescriptionNumber = "RX-20260407-0001",
            DosageCount = 14,
            Usage = "饭后温服",
            Advice = "忌生冷",
            ReferencedFormulas = "桂枝汤",
            Remark = "测试备注",
            Discount = 0.8m,
            SingleDosePrice = 12.5m,
            TotalWeight = 210m,
            Status = CommonStatus.Enabled,
            Items = new List<PrescriptionItemDto>
            {
                new() { HerbName = "人参", Dosage = 10 },
                new() { HerbName = "当归", Dosage = 15 }
            }
        };

        sut.InitializeFromDto(dto);

        sut.Prescription.Id.Should().Be(dto.Id);
        sut.Prescription.MedicalCaseId.Should().Be(dto.MedicalCaseId);
        sut.Prescription.PrescriptionNumber.Should().Be(dto.PrescriptionNumber);
        sut.Prescription.DosageCount.Should().Be(14);
        sut.Prescription.Usage.Should().Be("饭后温服");
        sut.Prescription.Advice.Should().Be("忌生冷");
        sut.Prescription.ReferencedFormulas.Should().Be("桂枝汤");
        sut.Prescription.Remark.Should().Be("测试备注");
        sut.Prescription.Discount.Should().Be(0.8m);
        sut.Prescription.SingleDosePrice.Should().Be(12.5m);
        sut.Prescription.TotalWeight.Should().Be(210m);
        sut.Prescription.Items.Should().HaveCount(2);
        sut.HasItems.Should().BeTrue();
    }

    [Fact]
    public void AddItem_RaisesPropertyChanged_AndNotifiesHost()
    {
        var sut = CreateSut();
        var notifications = new List<string?>();
        sut.PropertyChanged += (_, e) => notifications.Add(e.PropertyName);

        sut.Prescription.Items.Add(new PrescriptionItemDto { HerbName = "人参", Dosage = 10 });

        sut.HasItems.Should().BeTrue();
        notifications.Should().Contain(nameof(PrescriptionEditorViewModel.HasItems));
        _host.Received(1).NotifyStateChanged();
    }

    [Fact]
    public void RemoveItem_RaisesPropertyChanged_AndNotifiesHost()
    {
        var sut = CreateSut();
        var item = new PrescriptionItemDto { HerbName = "人参", Dosage = 10 };
        sut.Prescription.Items.Add(item);
        _host.ClearReceivedCalls();

        sut.Prescription.Items.Remove(item);

        sut.HasItems.Should().BeFalse();
        _host.Received(1).NotifyStateChanged();
    }

    [Fact]
    public void Reset_ClearsItems_AndRaisesHasItemsChange()
    {
        var sut = CreateSut();
        sut.Prescription.Items.Add(new PrescriptionItemDto { HerbName = "人参", Dosage = 10 });
        sut.Prescription.DosageCount = 14;
        sut.Prescription.Advice = "test";

        var raised = false;
        sut.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(PrescriptionEditorViewModel.HasItems))
                raised = true;
        };

        sut.Reset();

        sut.Prescription.Items.Should().BeEmpty();
        sut.Prescription.DosageCount.Should().Be(7);
        sut.Prescription.Advice.Should().BeNull();
        sut.HasItems.Should().BeFalse();
        raised.Should().BeTrue();
    }

    [Fact]
    public void Validate_ReturnsFalse_WhenNoItems()
    {
        var sut = CreateSut();

        sut.Validate().Should().BeFalse();
        sut.ValidationMessage.Should().Be("请添加至少一味药材");
    }

    [Fact]
    public void Validate_ReturnsTrue_WhenHasItems()
    {
        var sut = CreateSut();
        sut.Prescription.Items.Add(new PrescriptionItemDto { HerbName = "人参", Dosage = 10 });

        sut.Validate().Should().BeTrue();
        sut.ValidationMessage.Should().BeEmpty();
    }

    [Fact]
    public void GetPrescriptionData_ReturnsNull_WhenNoItems()
    {
        var sut = CreateSut();

        sut.GetPrescriptionData().Should().BeNull();
    }

    [Fact]
    public void GetPrescriptionData_ReturnsDto_WhenValidAndHasItems()
    {
        var sut = CreateSut();
        sut.Prescription.Items.Add(new PrescriptionItemDto { HerbName = "人参", Dosage = 10 });

        var result = sut.GetPrescriptionData();

        result.Should().NotBeNull();
        result!.Items.Should().HaveCount(1);
    }

    [Fact]
    public void Dispose_UnsubscribesFromCollectionChanged()
    {
        var sut = CreateSut();
        sut.Dispose();
        _host.ClearReceivedCalls();

        sut.Prescription.Items.Add(new PrescriptionItemDto { HerbName = "人参", Dosage = 10 });

        _host.DidNotReceive().NotifyStateChanged();
    }

    [Fact]
    public void PrescriptionCollection_ChangingItems_RaisesHasItemsPropertyChanged()
    {
        var sut = CreateSut();
        var changed = new List<string?>();
        sut.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

        sut.Prescription.Items.Add(new PrescriptionItemDto { HerbName = "人参", Dosage = 10 });
        sut.Prescription.Items.Clear();

        changed.Should().Contain(nameof(PrescriptionEditorViewModel.HasItems));
    }
}
