using System.Collections.ObjectModel;
using FluentAssertions;
using LYBT.Desktop.Infrastructure.Controls.HerbItem;
using LYBT.Shared.Models.Contracts.Herbs;
using Xunit;

namespace LYBT.Desktop.MedicalCase.Tests.ViewModels;

/// <summary>
/// HerbItemControlViewModel 单元测试 (原 PrescriptionHerbItem)
/// Issue: unify-herb-card-control - Phase 4 Task 4.1
/// OpenSpec: unify-frontend-backend-types Phase 8.4 - 类型重命名
/// OpenSpec: unify-control-data-binding - PrescriptionHerbItem已删除，由HerbItemControlViewModel替代
/// 测试价格计算和剂量验证功能
/// </summary>
public class PrescriptionHerbItemPriceTests
{
    #region Test Data

    private static HerbListDto CreateTestHerb(decimal price = 0.5m)
    {
        return new HerbListDto
        {
            Id = Guid.NewGuid(),
            Name = "当归",
            PinYinCode = "danggui",
            Unit = "g",
            Price = price
        };
    }

    private static ObservableCollection<HerbListDto> CreateTestHerbs()
    {
        return new ObservableCollection<HerbListDto>
        {
            new() { Id = Guid.NewGuid(), Name = "当归", PinYinCode = "danggui", Unit = "g", Price = 0.5m },
            new() { Id = Guid.NewGuid(), Name = "黄芪", PinYinCode = "huangqi", Unit = "g", Price = 0.8m },
            new() { Id = Guid.NewGuid(), Name = "甘草", PinYinCode = "gancao", Unit = "g", Price = 0.2m },
        };
    }

    /// <summary>
    /// 计算小计金额 (UnitPrice * Dosage)
    /// HerbItemControlViewModel不再包含ItemTotal属性，改为在DTO层计算
    /// 测试中直接计算以验证核心逻辑
    /// </summary>
    private static decimal CalculateItemTotal(HerbItemControlViewModel vm) => vm.UnitPrice * vm.Dosage;

    #endregion

    #region UnitPrice - 单价测试

    /// <summary>
    /// 测试：默认单价为0
    /// </summary>
    [Fact]
    public void UnitPrice_ByDefault_ShouldBeZero()
    {
        // Arrange & Act
        var viewModel = new HerbItemControlViewModel();

        // Assert
        viewModel.UnitPrice.Should().Be(0m, "因为默认未选择药材时单价应为0");
    }

    /// <summary>
    /// 测试：选择药材后单价从HerbListDto.Price获取
    /// </summary>
    [Fact]
    public void UnitPrice_WhenHerbSelected_ShouldReturnHerbPrice()
    {
        // Arrange
        var viewModel = new HerbItemControlViewModel();
        var herb = CreateTestHerb(price: 0.75m);

        // Act
        viewModel.SelectedHerb = herb;

        // Assert
        viewModel.UnitPrice.Should().Be(0.75m, "因为选择药材后应使用药材的实际价格");
    }

    /// <summary>
    /// 测试：直接设置UnitPrice（模拟从DTO加载的价格）
    /// </summary>
    [Fact]
    public void UnitPrice_WhenSetDirectly_ShouldUpdatePrice()
    {
        // Arrange
        var viewModel = new HerbItemControlViewModel();

        // Act
        viewModel.UnitPrice = 1.2m;

        // Assert
        viewModel.UnitPrice.Should().Be(1.2m, "因为应使用直接设置的价格");
    }

    /// <summary>
    /// 测试：SelectedHerb.Price 覆盖已设置的UnitPrice
    /// </summary>
    [Fact]
    public void UnitPrice_WhenHerbSelectedAfterDirectSet_ShouldPreferSelectedHerbPrice()
    {
        // Arrange
        var viewModel = new HerbItemControlViewModel();
        viewModel.UnitPrice = 1.0m;

        var herb = CreateTestHerb(price: 0.5m);

        // Act
        viewModel.SelectedHerb = herb;

        // Assert
        viewModel.UnitPrice.Should().Be(0.5m, "因为SelectedHerb.Price应覆盖直接设置的UnitPrice");
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
        var viewModel = new HerbItemControlViewModel();

        // Assert
        CalculateItemTotal(viewModel).Should().Be(0m, "因为默认剂量和单价都为0");
    }

    /// <summary>
    /// 测试：小计 = 剂量 x 单价
    /// </summary>
    [Fact]
    public void ItemTotal_ShouldBeCalculatedCorrectly()
    {
        // Arrange
        var viewModel = new HerbItemControlViewModel();
        var herb = CreateTestHerb(price: 0.5m);
        viewModel.SelectedHerb = herb;

        // Act
        viewModel.Dosage = 10;

        // Assert
        CalculateItemTotal(viewModel).Should().Be(5m, "因为 10g x 0.5元/g = 5元");
    }

    /// <summary>
    /// 测试：选择药材后自动更新小计
    /// </summary>
    [Fact]
    public void ItemTotal_WhenHerbSelected_ShouldRecalculate()
    {
        // Arrange
        var viewModel = new HerbItemControlViewModel();
        viewModel.Dosage = 20;

        // Act
        var herb = CreateTestHerb(price: 0.3m);
        viewModel.SelectedHerb = herb;

        // Assert
        CalculateItemTotal(viewModel).Should().Be(6m, "因为 20g x 0.3元/g = 6元");
    }

    /// <summary>
    /// 测试：修改剂量后自动更新小计
    /// </summary>
    [Fact]
    public void ItemTotal_WhenDosageChanged_ShouldRecalculate()
    {
        // Arrange
        var viewModel = new HerbItemControlViewModel();
        var herb = CreateTestHerb(price: 0.8m);
        viewModel.SelectedHerb = herb;
        viewModel.Dosage = 10;
        var initialTotal = CalculateItemTotal(viewModel);

        // Act
        viewModel.Dosage = 15;

        // Assert
        initialTotal.Should().Be(8m, "初始小计应为 10g x 0.8元/g = 8元");
        CalculateItemTotal(viewModel).Should().Be(12m, "修改后小计应为 15g x 0.8元/g = 12元");
    }

    /// <summary>
    /// 测试：直接设置UnitPrice后小计应更新
    /// </summary>
    [Fact]
    public void ItemTotal_WhenUnitPriceSetDirectly_ShouldRecalculate()
    {
        // Arrange
        var viewModel = new HerbItemControlViewModel();
        viewModel.Dosage = 10;

        // Act
        viewModel.UnitPrice = 2.0m;

        // Assert
        CalculateItemTotal(viewModel).Should().Be(20m, "因为 10g x 2.0元/g = 20元");
    }

    #endregion

    #region Dosage Validation - 剂量验证测试

    /// <summary>
    /// 测试：有效剂量范围内IsDosageValid为true
    /// </summary>
    [Theory]
    [InlineData(1)]
    [InlineData(10)]
    [InlineData(100)]
    [InlineData(500)]
    public void IsDosageValid_WhenDosageInRange_ShouldBeTrue(int dosage)
    {
        // Arrange
        var viewModel = new HerbItemControlViewModel();
        viewModel.SelectedHerb = CreateTestHerb(); // 需要先选择药材使其非空行

        // Act
        viewModel.Dosage = dosage;

        // Assert
        viewModel.IsDosageValid.Should().BeTrue($"因为{dosage}g在有效范围1-500之内");
        viewModel.ValidationMessage.Should().BeEmpty();
    }

    /// <summary>
    /// 测试：剂量小于等于0时验证失败
    /// 注意：HerbItemControlViewModel对非空行（已选药材）的0和负数剂量均判定无效
    /// </summary>
    [Theory]
    [InlineData(-1)]
    [InlineData(-10)]
    public void IsDosageValid_WhenDosageTooSmall_ShouldBeFalse(int dosage)
    {
        // Arrange
        var viewModel = new HerbItemControlViewModel();
        viewModel.SelectedHerb = CreateTestHerb(); // 需要先选择药材使其非空行

        // Act
        viewModel.Dosage = dosage;

        // Assert
        viewModel.IsDosageValid.Should().BeFalse($"因为{dosage}g小于最小有效剂量");
    }

    /// <summary>
    /// 测试：剂量大于500时验证失败
    /// </summary>
    [Theory]
    [InlineData(501)]
    [InlineData(600)]
    [InlineData(1000)]
    public void IsDosageValid_WhenDosageTooLarge_ShouldBeFalse(int dosage)
    {
        // Arrange
        var viewModel = new HerbItemControlViewModel();
        viewModel.SelectedHerb = CreateTestHerb(); // 需要先选择药材使其非空行

        // Act
        viewModel.Dosage = dosage;

        // Assert
        viewModel.IsDosageValid.Should().BeFalse($"因为{dosage}g大于最大剂量500g");
        viewModel.ValidationMessage.Should().Contain("500");
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
        var viewModel = new HerbItemControlViewModel();
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
    /// 测试：IsDosageValid变更触发PropertyChanged
    /// </summary>
    [Fact]
    public void IsDosageValid_WhenChanged_ShouldRaisePropertyChanged()
    {
        // Arrange
        var viewModel = new HerbItemControlViewModel();
        viewModel.SelectedHerb = CreateTestHerb(); // 需要先选择药材使其非空行
        viewModel.Dosage = 10; // 先设置有效剂量（IsDosageValid = true）

        var isDosageValidChanged = false;
        viewModel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(viewModel.IsDosageValid))
                isDosageValidChanged = true;
        };

        // Act
        viewModel.Dosage = -1; // 设置无效剂量（触发IsDosageValid从true变为false）

        // Assert
        isDosageValidChanged.Should().BeTrue("因为剂量从有效变为无效应通知IsDosageValid变更");
    }

    #endregion

    #region Integration with Pinyin Filter - 拼音码过滤集成测试

    /// <summary>
    /// 测试：拼音码过滤功能
    /// HerbItemControlViewModel使用Contains匹配，需要输入拼音码的连续子串
    /// </summary>
    [Fact]
    public void FilterHerbs_ShouldFilterByPinyinCode()
    {
        // Arrange
        var viewModel = new HerbItemControlViewModel();
        viewModel.AllHerbs = CreateTestHerbs();

        // Act
        viewModel.HerbName = "dangg";

        // Assert
        viewModel.FilteredHerbs.Should().NotBeEmpty("因为应能通过拼音码过滤药材");
        viewModel.FilteredHerbs.Should().Contain(h => h.Name == "当归");
    }

    /// <summary>
    /// 测试：选择药材后自动填充HerbId、HerbName、Unit
    /// </summary>
    [Fact]
    public void SelectedHerb_ShouldAutoFillProperties()
    {
        // Arrange
        var viewModel = new HerbItemControlViewModel();
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
