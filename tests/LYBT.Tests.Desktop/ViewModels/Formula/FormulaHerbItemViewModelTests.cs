using System.Collections.ObjectModel;
using FluentAssertions;
using LYBT.Desktop.Formula.ViewModels;
using LYBT.Shared.Models.Contracts.Herbs;
using Xunit;

namespace LYBT.Tests.Desktop.ViewModels.Formula;

/// <summary>
/// FormulaHerbItemViewModel 单元测试
/// Issue: unify-herb-card-control - Phase 4 Task 4.1
/// 测试经验方药材ViewModel（价格始终为0）
/// </summary>
public class FormulaHerbItemViewModelTests
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
        };
    }

    #endregion

    #region UnitPrice - 价格始终为0测试

    /// <summary>
    /// 测试：UnitPrice始终为0（经验方不涉及价格）
    /// </summary>
    [Fact]
    public void UnitPrice_ShouldAlwaysBeZero()
    {
        // Arrange & Act
        var viewModel = new FormulaHerbItemViewModel();

        // Assert
        viewModel.UnitPrice.Should().Be(0m, "因为经验方不涉及价格计算");
    }

    /// <summary>
    /// 测试：即使选择有价格的药材，UnitPrice仍为0
    /// </summary>
    [Fact]
    public void UnitPrice_WhenHerbSelected_ShouldStillBeZero()
    {
        // Arrange
        var viewModel = new FormulaHerbItemViewModel();
        var herb = CreateTestHerb(price: 1.5m);

        // Act
        viewModel.SelectedHerb = herb;

        // Assert
        viewModel.UnitPrice.Should().Be(0m, "因为经验方模块忽略药材价格");
    }

    /// <summary>
    /// 测试：不同剂量下UnitPrice仍为0
    /// </summary>
    [Theory]
    [InlineData(1)]
    [InlineData(10)]
    [InlineData(100)]
    public void UnitPrice_WithAnyDosage_ShouldStillBeZero(int dosage)
    {
        // Arrange
        var viewModel = new FormulaHerbItemViewModel();
        viewModel.SelectedHerb = CreateTestHerb(price: 2.0m);

        // Act
        viewModel.Dosage = dosage;

        // Assert
        viewModel.UnitPrice.Should().Be(0m, $"因为经验方UnitPrice始终为0，不受剂量{dosage}影响");
    }

    #endregion

    #region Remark - 备注属性测试

    /// <summary>
    /// 测试：默认Remark为null
    /// </summary>
    [Fact]
    public void Remark_ByDefault_ShouldBeNull()
    {
        // Arrange & Act
        var viewModel = new FormulaHerbItemViewModel();

        // Assert
        viewModel.Remark.Should().BeNull("因为默认没有备注");
    }

    /// <summary>
    /// 测试：设置Remark属性
    /// </summary>
    [Fact]
    public void Remark_WhenSet_ShouldUpdateValue()
    {
        // Arrange
        var viewModel = new FormulaHerbItemViewModel();

        // Act
        viewModel.Remark = "先煎";

        // Assert
        viewModel.Remark.Should().Be("先煎", "因为应保存备注信息");
    }

    /// <summary>
    /// 测试：Remark变更触发PropertyChanged
    /// </summary>
    [Fact]
    public void Remark_WhenChanged_ShouldRaisePropertyChanged()
    {
        // Arrange
        var viewModel = new FormulaHerbItemViewModel();
        var propertyChangedRaised = false;
        viewModel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(viewModel.Remark))
                propertyChangedRaised = true;
        };

        // Act
        viewModel.Remark = "后下";

        // Assert
        propertyChangedRaised.Should().BeTrue("因为Remark变更应通知UI");
    }

    #endregion

    #region ToDto - 转换为DTO测试

    /// <summary>
    /// 测试：ToDto正确映射HerbId
    /// </summary>
    [Fact]
    public void ToDto_ShouldMapHerbId()
    {
        // Arrange
        var viewModel = new FormulaHerbItemViewModel();
        var herb = CreateTestHerb();
        viewModel.SelectedHerb = herb;

        // Act
        var dto = viewModel.ToDto();

        // Assert
        dto.HerbId.Should().Be(herb.Id, "因为应映射HerbId");
    }

    /// <summary>
    /// 测试：ToDto当HerbId为Empty时返回null
    /// </summary>
    [Fact]
    public void ToDto_WhenHerbIdIsEmpty_ShouldReturnNullHerbId()
    {
        // Arrange
        var viewModel = new FormulaHerbItemViewModel();
        viewModel.HerbName = "自定义药材";

        // Act
        var dto = viewModel.ToDto();

        // Assert
        dto.HerbId.Should().BeNull("因为Guid.Empty应转换为null");
    }

    /// <summary>
    /// 测试：ToDto正确映射HerbName
    /// </summary>
    [Fact]
    public void ToDto_ShouldMapHerbName()
    {
        // Arrange
        var viewModel = new FormulaHerbItemViewModel();
        viewModel.SelectedHerb = CreateTestHerb();

        // Act
        var dto = viewModel.ToDto();

        // Assert
        dto.HerbName.Should().Be("当归", "因为应映射HerbName");
    }

    /// <summary>
    /// 测试：ToDto正确映射Dosage到Quantity
    /// </summary>
    [Fact]
    public void ToDto_ShouldMapDosageToQuantity()
    {
        // Arrange
        var viewModel = new FormulaHerbItemViewModel();
        viewModel.Dosage = 15;

        // Act
        var dto = viewModel.ToDto();

        // Assert
        dto.Dosage.Should().Be(15, "因为Dosage应映射到Quantity");
    }

    /// <summary>
    /// 测试：ToDto正确映射Unit
    /// </summary>
    [Fact]
    public void ToDto_ShouldMapUnit()
    {
        // Arrange
        var viewModel = new FormulaHerbItemViewModel();
        viewModel.Unit = "克";

        // Act
        var dto = viewModel.ToDto();

        // Assert
        dto.Unit.Should().Be("克", "因为应映射Unit");
    }

    /// <summary>
    /// 测试：ToDto正确映射Remark到ProcessingMethod
    /// </summary>
    [Fact]
    public void ToDto_ShouldMapRemarkToProcessingMethod()
    {
        // Arrange
        var viewModel = new FormulaHerbItemViewModel();
        viewModel.Remark = "先煎30分钟";

        // Act
        var dto = viewModel.ToDto();

        // Assert
        dto.ProcessingMethod.Should().Be("先煎30分钟", "因为Remark应映射到ProcessingMethod");
    }

    #endregion

    #region Base Class Integration - 基类集成测试

    /// <summary>
    /// 测试：继承基类的拼音码过滤功能
    /// </summary>
    [Fact]
    public void FilterHerbs_ShouldWorkFromBaseClass()
    {
        // Arrange
        var viewModel = new FormulaHerbItemViewModel();
        viewModel.AllHerbs = CreateTestHerbs();

        // Act
        viewModel.HerbName = "hq";

        // Assert
        viewModel.FilteredHerbs.Should().NotBeEmpty("因为应继承基类的拼音码过滤功能");
        viewModel.FilteredHerbs.Should().Contain(h => h.Name == "黄芪");
    }

    /// <summary>
    /// 测试：选择药材后自动填充属性
    /// </summary>
    [Fact]
    public void SelectedHerb_ShouldAutoFillProperties()
    {
        // Arrange
        var viewModel = new FormulaHerbItemViewModel();
        var herb = CreateTestHerb();

        // Act
        viewModel.SelectedHerb = herb;

        // Assert
        viewModel.HerbId.Should().Be(herb.Id, "因为HerbId应自动填充");
        viewModel.HerbName.Should().Be("当归", "因为HerbName应自动填充");
        viewModel.Unit.Should().Be("g", "因为Unit应自动填充");
    }

    /// <summary>
    /// 测试：默认Unit为空字符串（由SelectedHerb赋值时从药材数据获取）
    /// OpenSpec: unify-herb-list-controls
    /// </summary>
    [Fact]
    public void Unit_ByDefault_ShouldBeEmpty()
    {
        // Arrange & Act
        var viewModel = new FormulaHerbItemViewModel();

        // Assert
        viewModel.Unit.Should().BeEmpty("因为默认Unit为空，由SelectedHerb赋值时从药材数据获取");
    }

    #endregion
}
