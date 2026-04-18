using System.ComponentModel;
using FluentAssertions;
using LYBT.Desktop.MedicalCase.Models.Items;
using LYBT.Shared.Models.Contracts.Prescriptions;
using LYBT.Tests.Desktop.Infrastructure;
using Xunit;

namespace LYBT.Tests.Desktop.PureLogic.MedicalCase;

/// <summary>
/// Phase 1.3: PrescriptionItem validation property tests
/// Tests for prescription validation and indicators
/// </summary>
public class PrescriptionItemTests : UserJourneyTestBase
{
    public PrescriptionItemTests(UserJourneyFixture fixture) : base(fixture)
    {
    }

    private PrescriptionItem CreateSut() => new();

    [Fact]
    public void Constructor_InitializesWithDefaults()
    {
        var sut = CreateSut();

        sut.Id.Should().Be(Guid.Empty);
        sut.MedicalCaseId.Should().Be(Guid.Empty);
        sut.PrescriptionNumber.Should().BeNull();
        sut.DosageCount.Should().Be(7);
        sut.Usage.Should().Be(PrescriptionItem.DefaultUsage);
        sut.Advice.Should().BeNull();
        sut.Remark.Should().BeNull();
        sut.Discount.Should().Be(1.0m);
        sut.Items.Should().BeEmpty();
        sut.Status.Should().Be(LYBT.Shared.Models.Enums.CommonStatus.Enabled);
    }

    [Fact]
    public void ItemCount_ReturnsZero_WhenItemsIsEmpty()
    {
        var sut = CreateSut();

        sut.ItemCount.Should().Be(0);
    }

    [Fact]
    public void ItemCount_ReturnsCorrectCount_WhenItemsHasElements()
    {
        var sut = CreateSut();
        sut.Items.Add(new PrescriptionItemDto { HerbId = Guid.NewGuid() });
        sut.Items.Add(new PrescriptionItemDto { HerbId = Guid.NewGuid() });
        sut.Items.Add(new PrescriptionItemDto { HerbId = Guid.NewGuid() });

        sut.ItemCount.Should().Be(3);
    }

    [Fact]
    public void HasItems_ReturnsFalse_WhenItemsIsEmpty()
    {
        var sut = CreateSut();

        sut.HasItems.Should().BeFalse();
    }

    [Fact]
    public void HasItems_ReturnsFalse_WhenItemsHasZeroElements()
    {
        var sut = CreateSut();
        sut.Items.Clear();

        sut.HasItems.Should().BeFalse();
    }

    [Fact]
    public void HasItems_ReturnsTrue_WhenItemsHasElements()
    {
        var sut = CreateSut();
        sut.Items.Add(new PrescriptionItemDto { HerbId = Guid.NewGuid() });

        sut.HasItems.Should().BeTrue();
    }

    [Fact]
    public void Items_SetProperty_RaisesHasItemsPropertyChanged()
    {
        var sut = CreateSut();
        var propertiesChanged = new List<string?>();
        sut.PropertyChanged += (_, e) => propertiesChanged.Add(e.PropertyName);

        sut.Items.Add(new PrescriptionItemDto { HerbId = Guid.NewGuid() });

        propertiesChanged.Should().Contain(nameof(PrescriptionItem.HasItems));
        propertiesChanged.Should().Contain(nameof(PrescriptionItem.ItemCount));
        propertiesChanged.Should().Contain(nameof(PrescriptionItem.IsValid));
    }

    [Fact]
    public void IsValid_ReturnsFalse_WhenHasItemsIsFalse()
    {
        var sut = CreateSut();

        sut.IsValid.Should().BeFalse();
    }

    [Fact]
    public void IsValid_ReturnsTrue_WhenHasItemsIsTrue()
    {
        var sut = CreateSut();
        sut.Items.Add(new PrescriptionItemDto { HerbId = Guid.NewGuid() });

        sut.IsValid.Should().BeTrue();
    }

    [Fact]
    public void TotalPrice_ReturnsZero_WhenNoItems()
    {
        var sut = CreateSut();
        sut.DosageCount = 7;

        sut.TotalPrice.Should().Be(0);
    }

    [Fact]
    public void TotalPrice_CalculatesCorrectly_WithItems()
    {
        var sut = CreateSut();
        sut.DosageCount = 7;

        // Add herb: 10g * ¥5/g = ¥50 per dose
        var herb1 = new PrescriptionItemDto
        {
            HerbId = Guid.NewGuid(),
            HerbName = "当归",
            Dosage = 10,
            UnitPrice = 5m
        };
        sut.Items.Add(herb1);

        // Add herb: 15g * ¥3/g = ¥45 per dose
        var herb2 = new PrescriptionItemDto
        {
            HerbId = Guid.NewGuid(),
            HerbName = "川芎",
            Dosage = 15,
            UnitPrice = 3m
        };
        sut.Items.Add(herb2);

        // Single dose: ¥50 + ¥45 = ¥95
        // Total: ¥95 * 7 doses = ¥665
        sut.TotalPrice.Should().Be(665m);
    }

    [Fact]
    public void SingleDosePrice_CalculatesCorrectly()
    {
        var sut = CreateSut();

        var herb1 = new PrescriptionItemDto
        {
            HerbId = Guid.NewGuid(),
            HerbName = "当归",
            Dosage = 10,
            UnitPrice = 5m
        };
        sut.Items.Add(herb1);

        sut.SingleDosePrice.Should().Be(50m);
    }

    [Fact]
    public void DisplayText_ReturnsNumberAndItemCount()
    {
        var sut = CreateSut();
        sut.PrescriptionNumber = "RX-20260418-0001";
        sut.Items.Add(new PrescriptionItemDto { HerbId = Guid.NewGuid() });
        sut.Items.Add(new PrescriptionItemDto { HerbId = Guid.NewGuid() });

        sut.DisplayText.Should().Be("处方 RX-20260418-0001 - 2味药材");
    }

    [Fact]
    public void DisplayText_ReturnsNewText_WhenPrescriptionNumberIsNull()
    {
        var sut = CreateSut();
        sut.Items.Add(new PrescriptionItemDto { HerbId = Guid.NewGuid() });

        sut.DisplayText.Should().Be("处方 新建 - 1味药材");
    }

    [Fact]
    public void Validate_ReturnsFalse_WhenNoItems()
    {
        var sut = CreateSut();

        var result = sut.Validate();

        result.Should().BeFalse();
        sut.ValidationMessage.Should().Be("请添加至少一味药材");
    }

    [Fact]
    public void Validate_ReturnsTrue_WhenHasItems()
    {
        var sut = CreateSut();
        sut.Items.Add(new PrescriptionItemDto { HerbId = Guid.NewGuid() });

        var result = sut.Validate();

        result.Should().BeTrue();
        sut.ValidationMessage.Should().BeEmpty();
    }

    [Fact]
    public void Validate_ReturnsTrue_WhenValidationDisabled()
    {
        var sut = CreateSut();
        sut.ValidationEnabled = false;

        var result = sut.Validate();

        result.Should().BeTrue();
    }

    [Fact]
    public void ValidationEnabled_CanBeToggled()
    {
        var sut = CreateSut();

        sut.ValidationEnabled.Should().BeTrue();

        sut.ValidationEnabled = false;
        sut.ValidationEnabled.Should().BeFalse();

        sut.ValidationEnabled = true;
        sut.ValidationEnabled.Should().BeTrue();
    }

    [Fact]
    public void Clear_ResetsAllFieldsIncludingId()
    {
        var sut = CreateSut();
        sut.Id = Guid.NewGuid();
        sut.MedicalCaseId = Guid.NewGuid();
        sut.PrescriptionNumber = "RX-20260418-0001";
        sut.DosageCount = 14;
        sut.Items.Add(new PrescriptionItemDto { HerbId = Guid.NewGuid() });

        sut.Clear();

        sut.Id.Should().Be(Guid.Empty);
        sut.MedicalCaseId.Should().Be(Guid.Empty);
        sut.PrescriptionNumber.Should().BeNull();
        sut.DosageCount.Should().Be(7);
        sut.Items.Should().BeEmpty();
        sut.ItemCount.Should().Be(0);
        sut.HasItems.Should().BeFalse();
    }

    [Fact]
    public void Reset_ResetsEditableFields_KeepingId()
    {
        var sut = CreateSut();
        var id = Guid.NewGuid();
        var medicalCaseId = Guid.NewGuid();
        sut.Id = id;
        sut.MedicalCaseId = medicalCaseId;
        sut.DosageCount = 14;
        sut.Usage = "特殊用法";
        sut.Items.Add(new PrescriptionItemDto { HerbId = Guid.NewGuid() });

        sut.Reset();

        sut.Id.Should().Be(id);
        sut.MedicalCaseId.Should().Be(medicalCaseId);
        sut.DosageCount.Should().Be(7);
        sut.Usage.Should().Be(PrescriptionItem.DefaultUsage);
        sut.Items.Should().BeEmpty();
    }

    [Fact]
    public void NotifyItemsChanged_RaisesAllRelatedProperties()
    {
        var sut = CreateSut();
        var propertiesChanged = new List<string?>();
        sut.PropertyChanged += (_, e) => propertiesChanged.Add(e.PropertyName);

        sut.NotifyItemsChanged();

        propertiesChanged.Should().Contain(nameof(PrescriptionItem.ItemCount));
        propertiesChanged.Should().Contain(nameof(PrescriptionItem.HasItems));
        propertiesChanged.Should().Contain(nameof(PrescriptionItem.IsValid));
        propertiesChanged.Should().Contain(nameof(PrescriptionItem.TotalPrice));
        propertiesChanged.Should().Contain(nameof(PrescriptionItem.SingleDosePrice));
        propertiesChanged.Should().Contain(nameof(PrescriptionItem.DisplayText));
    }

    [Fact]
    public void GetConsultationData_ReturnsNull()
    {
        var sut = CreateSut();

        var dto = sut.GetConsultationData();

        dto.Should().BeNull();
    }

    [Fact]
    public void GetPrescriptionData_ReturnsNull_WhenNoItems()
    {
        var sut = CreateSut();

        var dto = sut.GetPrescriptionData();

        dto.Should().BeNull();
    }

    [Fact]
    public void GetPrescriptionData_ReturnsDto_WhenHasItems()
    {
        var sut = CreateSut();
        sut.Id = Guid.NewGuid();
        sut.MedicalCaseId = Guid.NewGuid();
        sut.DosageCount = 7;
        sut.Usage = "水煎服，一日一剂";
        sut.Items.Add(new PrescriptionItemDto
        {
            HerbId = Guid.NewGuid(),
            HerbName = "当归",
            Dosage = 10,
            UnitPrice = 5m
        });

        var dto = sut.GetPrescriptionData();

        dto.Should().NotBeNull();
        dto!.DosageCount.Should().Be(7);
        dto.Usage.Should().Be("水煎服，一日一剂");
        dto.Items.Should().HaveCount(1);
    }
}
