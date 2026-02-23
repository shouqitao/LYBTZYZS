using System.Collections.ObjectModel;
using FluentAssertions;
using LYBT.Desktop.Infrastructure.ViewModels.Base;
using LYBT.Shared.Models.Contracts.Herbs;
using Xunit;

namespace LYBT.Desktop.Herbs.Tests.ViewModels.Base;

/// <summary>
/// HerbItemViewModelBase 单元测试
/// Issue: unify-herb-card-control - Phase 4 Task 4.1
/// 测试拼音码过滤逻辑和药材选择功能
/// OpenSpec: optimize-desktop-core - 迁移到Herbs.Tests
/// </summary>
public class HerbItemViewModelBaseTests
{
    #region Test Implementation

    /// <summary>
    /// 测试用具体ViewModel类（继承HerbItemViewModelBase）
    /// </summary>
    private class TestHerbItemViewModel : HerbItemViewModelBase
    {
        private readonly decimal _unitPrice;

        public TestHerbItemViewModel(decimal unitPrice = 0m)
        {
            _unitPrice = unitPrice;
        }

        public override decimal UnitPrice => _unitPrice;

        // 暴露受保护方法供测试
        public void TestFilterHerbs() => FilterHerbs();
    }

    #endregion

    #region Test Data

    private static ObservableCollection<HerbListDto> CreateTestHerbs()
    {
        return new ObservableCollection<HerbListDto>
        {
            new() { Id = Guid.NewGuid(), Name = "当归", PinYinCode = "danggui", Unit = "g", Price = 0.5m },
            new() { Id = Guid.NewGuid(), Name = "黄芪", PinYinCode = "huangqi", Unit = "g", Price = 0.8m },
            new() { Id = Guid.NewGuid(), Name = "党参", PinYinCode = "dangshen", Unit = "g", Price = 0.6m },
            new() { Id = Guid.NewGuid(), Name = "白术", PinYinCode = "baizhu", Unit = "g", Price = 0.4m },
            new() { Id = Guid.NewGuid(), Name = "茯苓", PinYinCode = "fuling", Unit = "g", Price = 0.3m },
            new() { Id = Guid.NewGuid(), Name = "甘草", PinYinCode = "gancao", Unit = "g", Price = 0.2m },
            new() { Id = Guid.NewGuid(), Name = "大枣", PinYinCode = "dazao", Unit = "g", Price = 0.15m },
            new() { Id = Guid.NewGuid(), Name = "生姜", PinYinCode = "shengjiang", Unit = "g", Price = 0.1m },
        };
    }

    #endregion

    #region FilterHerbs - 拼音码过滤测试

    /// <summary>
    /// 测试：拼音码前缀匹配 - 输入"dg"应匹配"danggui"(当归)
    /// </summary>
    [Fact]
    public void FilterHerbs_WhenPinyinPrefixMatch_ShouldReturnMatchingHerbs()
    {
        // Arrange
        var viewModel = new TestHerbItemViewModel();
        viewModel.AllHerbs = CreateTestHerbs();

        // Act
        viewModel.HerbName = "dg";

        // Assert
        viewModel.FilteredHerbs.Should().NotBeEmpty("因为'dg'应匹配拼音码以'dg'开头的药材");
        viewModel.FilteredHerbs.Should().Contain(h => h.Name == "当归", "因为'danggui'以'dg'开头");
    }

    /// <summary>
    /// 测试：拼音码完全匹配 - 输入"danggui"应返回"当归"（评分90）
    /// </summary>
    [Fact]
    public void FilterHerbs_WhenPinyinExactMatch_ShouldReturnMatchingHerb()
    {
        // Arrange
        var viewModel = new TestHerbItemViewModel();
        viewModel.AllHerbs = CreateTestHerbs();

        // Act
        viewModel.HerbName = "danggui";

        // Assert
        viewModel.FilteredHerbs.Should().NotBeEmpty("因为拼音码精确匹配仍返回建议结果");
        viewModel.FilteredHerbs.First().Name.Should().Be("当归", "因为'danggui'精确匹配'当归'的拼音码");
    }

    /// <summary>
    /// 测试：中文名称精确匹配 - 输入"当归"应返回空列表（用户已选择药材）
    /// </summary>
    [Fact]
    public void FilterHerbs_WhenChineseNameExactMatch_ShouldReturnEmptyList()
    {
        // Arrange
        var viewModel = new TestHerbItemViewModel();
        viewModel.AllHerbs = CreateTestHerbs();

        // Act
        viewModel.HerbName = "当归";

        // Assert
        viewModel.FilteredHerbs.Should().BeEmpty("因为中文名称精确匹配后不显示建议列表，避免Popup一直显示");
    }

    /// <summary>
    /// 测试：中文名称前缀匹配 - 输入"当"应匹配"当归"和"党参"
    /// </summary>
    [Fact]
    public void FilterHerbs_WhenChineseNamePrefixMatch_ShouldReturnMatchingHerbs()
    {
        // Arrange
        var viewModel = new TestHerbItemViewModel();
        viewModel.AllHerbs = CreateTestHerbs();

        // Act
        viewModel.HerbName = "当";

        // Assert
        viewModel.FilteredHerbs.Should().NotBeEmpty("因为'当'应匹配名称以'当'开头的药材");
        viewModel.FilteredHerbs.Should().Contain(h => h.Name == "当归", "因为'当归'以'当'开头");
    }

    /// <summary>
    /// 测试：中文名称包含匹配 - 输入"归"应匹配"当归"
    /// </summary>
    [Fact]
    public void FilterHerbs_WhenChineseNameContainsMatch_ShouldReturnMatchingHerbs()
    {
        // Arrange
        var viewModel = new TestHerbItemViewModel();
        viewModel.AllHerbs = CreateTestHerbs();

        // Act
        viewModel.HerbName = "归";

        // Assert
        viewModel.FilteredHerbs.Should().NotBeEmpty("因为'归'应匹配名称包含'归'的药材");
        viewModel.FilteredHerbs.Should().Contain(h => h.Name == "当归", "因为'当归'包含'归'");
    }

    /// <summary>
    /// 测试：无匹配结果 - 输入不存在的字符应返回空列表
    /// </summary>
    [Fact]
    public void FilterHerbs_WhenNoMatch_ShouldReturnEmptyList()
    {
        // Arrange
        var viewModel = new TestHerbItemViewModel();
        viewModel.AllHerbs = CreateTestHerbs();

        // Act
        viewModel.HerbName = "xyz";

        // Assert
        viewModel.FilteredHerbs.Should().BeEmpty("因为'xyz'不匹配任何药材");
    }

    /// <summary>
    /// 测试：AllHerbs为空时不应报错
    /// </summary>
    [Fact]
    public void FilterHerbs_WhenAllHerbsIsNull_ShouldNotThrow()
    {
        // Arrange
        var viewModel = new TestHerbItemViewModel();
        viewModel.AllHerbs = null;

        // Act
        var act = () => viewModel.HerbName = "test";

        // Assert
        act.Should().NotThrow("因为AllHerbs为null时应安全处理");
        viewModel.FilteredHerbs.Should().BeEmpty();
    }

    /// <summary>
    /// 测试：HerbName为空时不应过滤
    /// </summary>
    [Fact]
    public void FilterHerbs_WhenHerbNameIsEmpty_ShouldReturnEmptyList()
    {
        // Arrange
        var viewModel = new TestHerbItemViewModel();
        viewModel.AllHerbs = CreateTestHerbs();

        // Act
        viewModel.HerbName = "";

        // Assert
        viewModel.FilteredHerbs.Should().BeEmpty("因为HerbName为空时不应显示建议");
    }

    /// <summary>
    /// 测试：结果最多返回5个
    /// </summary>
    [Fact]
    public void FilterHerbs_ShouldReturnMaximum5Results()
    {
        // Arrange
        var viewModel = new TestHerbItemViewModel();
        viewModel.AllHerbs = CreateTestHerbs(); // 8个药材

        // Act
        viewModel.HerbName = "g"; // 匹配多个药材（danggui, huangqi, dangshen, fuling, gancao, shengjiang都包含g）

        // Assert
        viewModel.FilteredHerbs.Count.Should().BeLessOrEqualTo(5, "因为最多只显示5个建议结果");
    }

    #endregion

    #region SelectedHerb - 药材选择测试

    /// <summary>
    /// 测试：选择药材后自动填充HerbId
    /// </summary>
    [Fact]
    public void SelectedHerb_WhenSet_ShouldUpdateHerbId()
    {
        // Arrange
        var viewModel = new TestHerbItemViewModel();
        var herb = new HerbListDto { Id = Guid.NewGuid(), Name = "当归", PinYinCode = "danggui", Unit = "g" };

        // Act
        viewModel.SelectedHerb = herb;

        // Assert
        viewModel.HerbId.Should().Be(herb.Id, "因为选择药材后应自动填充HerbId");
    }

    /// <summary>
    /// 测试：选择药材后自动填充HerbName
    /// </summary>
    [Fact]
    public void SelectedHerb_WhenSet_ShouldUpdateHerbName()
    {
        // Arrange
        var viewModel = new TestHerbItemViewModel();
        var herb = new HerbListDto { Id = Guid.NewGuid(), Name = "当归", PinYinCode = "danggui", Unit = "g" };

        // Act
        viewModel.SelectedHerb = herb;

        // Assert
        viewModel.HerbName.Should().Be("当归", "因为选择药材后应自动填充HerbName");
    }

    /// <summary>
    /// 测试：选择药材后自动填充Unit
    /// </summary>
    [Fact]
    public void SelectedHerb_WhenSet_ShouldUpdateUnit()
    {
        // Arrange
        var viewModel = new TestHerbItemViewModel();
        var herb = new HerbListDto { Id = Guid.NewGuid(), Name = "当归", PinYinCode = "danggui", Unit = "克" };

        // Act
        viewModel.SelectedHerb = herb;

        // Assert
        viewModel.Unit.Should().Be("克", "因为选择药材后应自动填充Unit");
    }

    /// <summary>
    /// 测试：设置null不应报错
    /// </summary>
    [Fact]
    public void SelectedHerb_WhenSetToNull_ShouldNotThrow()
    {
        // Arrange
        var viewModel = new TestHerbItemViewModel();
        viewModel.SelectedHerb = new HerbListDto { Id = Guid.NewGuid(), Name = "当归", Unit = "g" };

        // Act
        var act = () => viewModel.SelectedHerb = null;

        // Assert
        act.Should().NotThrow("因为设置null应安全处理");
    }

    #endregion

    #region Property Change Tests

    /// <summary>
    /// 测试：HerbName变更触发PropertyChanged
    /// </summary>
    [Fact]
    public void HerbName_WhenChanged_ShouldRaisePropertyChanged()
    {
        // Arrange
        var viewModel = new TestHerbItemViewModel();
        var propertyChangedRaised = false;
        viewModel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(viewModel.HerbName))
                propertyChangedRaised = true;
        };

        // Act
        viewModel.HerbName = "当归";

        // Assert
        propertyChangedRaised.Should().BeTrue("因为HerbName变更应触发PropertyChanged");
    }

    /// <summary>
    /// 测试：Dosage变更触发PropertyChanged
    /// </summary>
    [Fact]
    public void Dosage_WhenChanged_ShouldRaisePropertyChanged()
    {
        // Arrange
        var viewModel = new TestHerbItemViewModel();
        var propertyChangedRaised = false;
        viewModel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(viewModel.Dosage))
                propertyChangedRaised = true;
        };

        // Act
        viewModel.Dosage = 10;

        // Assert
        propertyChangedRaised.Should().BeTrue("因为Dosage变更应触发PropertyChanged");
    }

    #endregion

    #region Default Values Tests

    /// <summary>
    /// 测试：默认Unit为空字符串（由SelectedHerb赋值时获取）
    /// OpenSpec: unify-herb-list-controls - Unit默认为空，由药材数据赋值
    /// </summary>
    [Fact]
    public void Unit_ByDefault_ShouldBeEmpty()
    {
        // Arrange & Act
        var viewModel = new TestHerbItemViewModel();

        // Assert
        viewModel.Unit.Should().BeEmpty("因为默认单位为空，由SelectedHerb赋值时从药材数据获取");
    }

    /// <summary>
    /// 测试：默认HerbId为Guid.Empty
    /// </summary>
    [Fact]
    public void HerbId_ByDefault_ShouldBeEmpty()
    {
        // Arrange & Act
        var viewModel = new TestHerbItemViewModel();

        // Assert
        viewModel.HerbId.Should().Be(Guid.Empty, "因为默认HerbId应为空");
    }

    /// <summary>
    /// 测试：默认Dosage为0
    /// </summary>
    [Fact]
    public void Dosage_ByDefault_ShouldBeZero()
    {
        // Arrange & Act
        var viewModel = new TestHerbItemViewModel();

        // Assert
        viewModel.Dosage.Should().Be(0, "因为默认剂量应为0");
    }

    #endregion
}
