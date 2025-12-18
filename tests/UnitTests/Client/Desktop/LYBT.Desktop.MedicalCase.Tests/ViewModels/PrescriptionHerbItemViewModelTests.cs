using System.Collections.ObjectModel;
using FluentAssertions;
using LYBT.Desktop.MedicalCase.ViewModels;
using LYBT.Shared.Models.Contracts.Herbs;
using Xunit;

namespace LYBT.Desktop.MedicalCase.Tests.ViewModels;

/// <summary>
/// PrescriptionHerbItemViewModel 单元测试
/// Issue: unify-herb-card-control - Phase 4 Task 4.1
/// 测试价格计算和剂量验证功能
/// </summary>
public class PrescriptionHerbItemViewModelTests
{
    #region Test Data

    private static HerbDetailDto CreateTestHerb(decimal price = 0.5m)
    {
        return new HerbDetailDto
        {
            Id = Guid.NewGuid(),
            Name = "当归",
            PinYinCode = "danggui",
            Unit = "g",
            Price = price
        };
    }

    private static ObservableCollection<HerbDetailDto> CreateTestHerbs()
    {
        return new ObservableCollection<HerbDetailDto>
        {
            new() { Id = Guid.NewGuid(), Name = "当归", PinYinCode = "danggui", Unit = "g", Price = 0.5m },
            new() { Id = Guid.NewGuid(), Name = "黄芪", PinYinCode = "huangqi", Unit = "g", Price = 0.8m },
            new() { Id = Guid.NewGuid(), Name = "甘草", PinYinCode = "gancao", Unit = "g", Price = 0.2m },
        };
    }

    #endregion

    #region UnitPrice - 单价测试

    /// <summary>
    /// 测试：默认单价为0
    /// </summary>
    [Fact]
    public void UnitPrice_ByDefault_ShouldBeZero()
    {
        // Arrange & Act
        var viewModel = new PrescriptionHerbItemViewModel();

        // Assert
        viewModel.UnitPrice.Should().Be(0m, "因为默认未选择药材时单价应为0");
    }

    /// <summary>
    /// 测试：选择药材后单价从HerbDetailDto.Price获取
    /// </summary>
    [Fact]
    public void UnitPrice_WhenHerbSelected_ShouldReturnHerbPrice()
    {
        // Arrange
        var viewModel = new PrescriptionHerbItemViewModel();
        var herb = CreateTestHerb(price: 0.75m);

        // Act
        viewModel.SelectedHerb = herb;

        // Assert
        viewModel.UnitPrice.Should().Be(0.75m, "因为选择药材后应使用药材的实际价格");
    }

    /// <summary>
    /// 测试：SetLoadedUnitPrice设置从DTO加载的价格
    /// </summary>
    [Fact]
    public void SetLoadedUnitPrice_ShouldSetPriceWithoutSelectedHerb()
    {
        // Arrange
        var viewModel = new PrescriptionHerbItemViewModel();

        // Act
        viewModel.SetLoadedUnitPrice(1.2m);

        // Assert
        viewModel.UnitPrice.Should().Be(1.2m, "因为应使用从DTO加载的价格");
    }

    /// <summary>
    /// 测试：SelectedHerb.Price优先于LoadedUnitPrice
    /// </summary>
    [Fact]
    public void UnitPrice_WhenBothLoaded_ShouldPreferSelectedHerbPrice()
    {
        // Arrange
        var viewModel = new PrescriptionHerbItemViewModel();
        viewModel.SetLoadedUnitPrice(1.0m);

        var herb = CreateTestHerb(price: 0.5m);

        // Act
        viewModel.SelectedHerb = herb;

        // Assert
        viewModel.UnitPrice.Should().Be(0.5m, "因为SelectedHerb.Price应优先于LoadedUnitPrice");
    }

    #endregion

    #region ItemTotal - 小计计算测试

    /// <summary>
    /// 测试：默认小计为0
    /// </summary>
    [Fact]
    public void ItemTotal_ByDefault_ShouldBeZero()
    {
        // Arrange & Act
        var viewModel = new PrescriptionHerbItemViewModel();

        // Assert
        viewModel.ItemTotal.Should().Be(0m, "因为默认剂量和单价都为0");
    }

    /// <summary>
    /// 测试：小计 = 剂量 x 单价
    /// </summary>
    [Fact]
    public void ItemTotal_ShouldBeCalculatedCorrectly()
    {
        // Arrange
        var viewModel = new PrescriptionHerbItemViewModel();
        var herb = CreateTestHerb(price: 0.5m);
        viewModel.SelectedHerb = herb;

        // Act
        viewModel.Dosage = 10m;

        // Assert
        viewModel.ItemTotal.Should().Be(5m, "因为 10g x 0.5元/g = 5元");
    }

    /// <summary>
    /// 测试：选择药材后自动更新小计
    /// </summary>
    [Fact]
    public void ItemTotal_WhenHerbSelected_ShouldRecalculate()
    {
        // Arrange
        var viewModel = new PrescriptionHerbItemViewModel();
        viewModel.Dosage = 20m;

        // Act
        var herb = CreateTestHerb(price: 0.3m);
        viewModel.SelectedHerb = herb;

        // Assert
        viewModel.ItemTotal.Should().Be(6m, "因为 20g x 0.3元/g = 6元");
    }

    /// <summary>
    /// 测试：修改剂量后自动更新小计
    /// </summary>
    [Fact]
    public void ItemTotal_WhenDosageChanged_ShouldRecalculate()
    {
        // Arrange
        var viewModel = new PrescriptionHerbItemViewModel();
        var herb = CreateTestHerb(price: 0.8m);
        viewModel.SelectedHerb = herb;
        viewModel.Dosage = 10m;
        var initialTotal = viewModel.ItemTotal;

        // Act
        viewModel.Dosage = 15m;

        // Assert
        initialTotal.Should().Be(8m, "初始小计应为 10g x 0.8元/g = 8元");
        viewModel.ItemTotal.Should().Be(12m, "修改后小计应为 15g x 0.8元/g = 12元");
    }

    /// <summary>
    /// 测试：SetLoadedUnitPrice后小计应更新
    /// </summary>
    [Fact]
    public void ItemTotal_WhenLoadedUnitPriceSet_ShouldRecalculate()
    {
        // Arrange
        var viewModel = new PrescriptionHerbItemViewModel();
        viewModel.Dosage = 10m;

        // Act
        viewModel.SetLoadedUnitPrice(2.0m);

        // Assert
        viewModel.ItemTotal.Should().Be(20m, "因为 10g x 2.0元/g = 20元");
    }

    #endregion

    #region Dosage Validation - 剂量验证测试

    /// <summary>
    /// 测试：有效剂量范围内IsDosageValid为true
    /// </summary>
    [Theory]
    [InlineData(0.1)]
    [InlineData(1)]
    [InlineData(10)]
    [InlineData(100)]
    [InlineData(500)]
    public void IsDosageValid_WhenDosageInRange_ShouldBeTrue(decimal dosage)
    {
        // Arrange
        var viewModel = new PrescriptionHerbItemViewModel();

        // Act
        viewModel.Dosage = dosage;

        // Assert
        viewModel.IsDosageValid.Should().BeTrue($"因为{dosage}g在有效范围0.1-500之内");
        viewModel.DosageValidationMessage.Should().BeEmpty();
    }

    /// <summary>
    /// 测试：剂量小于0.1时验证失败
    /// 注意：不测试dosage=0的情况，因为默认值为0，SetProperty不会触发OnDosageChanged
    /// </summary>
    [Theory]
    [InlineData(0.05)]
    [InlineData(-1)]
    public void IsDosageValid_WhenDosageTooSmall_ShouldBeFalse(decimal dosage)
    {
        // Arrange
        var viewModel = new PrescriptionHerbItemViewModel();

        // Act
        viewModel.Dosage = dosage;

        // Assert
        viewModel.IsDosageValid.Should().BeFalse($"因为{dosage}g小于最小剂量0.1g");
        viewModel.DosageValidationMessage.Should().Contain("0.1");
    }

    /// <summary>
    /// 测试：剂量大于500时验证失败
    /// </summary>
    [Theory]
    [InlineData(500.1)]
    [InlineData(600)]
    [InlineData(1000)]
    public void IsDosageValid_WhenDosageTooLarge_ShouldBeFalse(decimal dosage)
    {
        // Arrange
        var viewModel = new PrescriptionHerbItemViewModel();

        // Act
        viewModel.Dosage = dosage;

        // Assert
        viewModel.IsDosageValid.Should().BeFalse($"因为{dosage}g大于最大剂量500g");
        viewModel.DosageValidationMessage.Should().Contain("500");
    }

    #endregion

    #region PropertyChanged - 属性变更通知测试

    /// <summary>
    /// 测试：UnitPrice变更触发PropertyChanged
    /// </summary>
    [Fact]
    public void UnitPrice_WhenHerbSelected_ShouldRaisePropertyChanged()
    {
        // Arrange
        var viewModel = new PrescriptionHerbItemViewModel();
        var unitPriceChanged = false;
        viewModel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(viewModel.UnitPrice))
                unitPriceChanged = true;
        };

        // Act
        viewModel.SelectedHerb = CreateTestHerb(price: 0.5m);

        // Assert
        unitPriceChanged.Should().BeTrue("因为选择药材后应通知UnitPrice变更");
    }

    /// <summary>
    /// 测试：ItemTotal变更触发PropertyChanged
    /// </summary>
    [Fact]
    public void ItemTotal_WhenChanged_ShouldRaisePropertyChanged()
    {
        // Arrange
        var viewModel = new PrescriptionHerbItemViewModel();
        viewModel.SelectedHerb = CreateTestHerb(price: 1m);

        var itemTotalChanged = false;
        viewModel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(viewModel.ItemTotal))
                itemTotalChanged = true;
        };

        // Act
        viewModel.Dosage = 10m;

        // Assert
        itemTotalChanged.Should().BeTrue("因为剂量变更后应通知ItemTotal变更");
    }

    /// <summary>
    /// 测试：IsDosageValid变更触发PropertyChanged
    /// </summary>
    [Fact]
    public void IsDosageValid_WhenChanged_ShouldRaisePropertyChanged()
    {
        // Arrange
        var viewModel = new PrescriptionHerbItemViewModel();
        viewModel.Dosage = 10m; // 先设置有效剂量

        var isDosageValidChanged = false;
        viewModel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(viewModel.IsDosageValid))
                isDosageValidChanged = true;
        };

        // Act
        viewModel.Dosage = 0; // 设置无效剂量

        // Assert
        isDosageValidChanged.Should().BeTrue("因为剂量从有效变为无效应通知IsDosageValid变更");
    }

    #endregion

    #region Integration with Base Class - 基类集成测试

    /// <summary>
    /// 测试：继承基类的拼音码过滤功能
    /// </summary>
    [Fact]
    public void FilterHerbs_ShouldWorkFromBaseClass()
    {
        // Arrange
        var viewModel = new PrescriptionHerbItemViewModel();
        viewModel.AllHerbs = CreateTestHerbs();

        // Act
        viewModel.HerbName = "dg";

        // Assert
        viewModel.FilteredHerbs.Should().NotBeEmpty("因为应继承基类的拼音码过滤功能");
        viewModel.FilteredHerbs.Should().Contain(h => h.Name == "当归");
    }

    /// <summary>
    /// 测试：选择药材后自动填充HerbId、HerbName、Unit
    /// </summary>
    [Fact]
    public void SelectedHerb_ShouldAutoFillFromBaseClass()
    {
        // Arrange
        var viewModel = new PrescriptionHerbItemViewModel();
        var herb = CreateTestHerb();

        // Act
        viewModel.SelectedHerb = herb;

        // Assert
        viewModel.HerbId.Should().Be(herb.Id, "因为HerbId应自动填充");
        viewModel.HerbName.Should().Be("当归", "因为HerbName应自动填充");
        viewModel.Unit.Should().Be("g", "因为Unit应自动填充");
    }

    #endregion
}
