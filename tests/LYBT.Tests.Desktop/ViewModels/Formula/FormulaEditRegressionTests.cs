using System.Collections.ObjectModel;
using FluentAssertions;
using LYBT.Desktop.Formula.ViewModels;
using LYBT.Shared.Models.Contracts.Herbs;
using Xunit;

namespace LYBT.Tests.Desktop.ViewModels.Formula;

/// <summary>
/// 经验方编辑回归测试
/// Issue: unify-herb-card-control - Phase 4 Task 4.2
/// 验证经验方编辑流程在复用共享控件后无功能回归
/// </summary>
public class FormulaEditRegressionTests
{
    #region Test Data

    private static ObservableCollection<HerbListDto> CreateHerbCatalog()
    {
        return new ObservableCollection<HerbListDto>
        {
            new() { Id = Guid.NewGuid(), Name = "当归", PinYinCode = "danggui", Unit = "g", Price = 0.5m },
            new() { Id = Guid.NewGuid(), Name = "黄芪", PinYinCode = "huangqi", Unit = "g", Price = 0.8m },
            new() { Id = Guid.NewGuid(), Name = "党参", PinYinCode = "dangshen", Unit = "g", Price = 0.6m },
            new() { Id = Guid.NewGuid(), Name = "白术", PinYinCode = "baizhu", Unit = "g", Price = 0.4m },
            new() { Id = Guid.NewGuid(), Name = "茯苓", PinYinCode = "fuling", Unit = "g", Price = 0.3m },
            new() { Id = Guid.NewGuid(), Name = "甘草", PinYinCode = "gancao", Unit = "g", Price = 0.2m },
        };
    }

    #endregion

    #region Regression Tests - 回归测试

    /// <summary>
    /// 回归测试：经验方不显示价格（UnitPrice始终为0）
    /// 即使药材有价格属性，经验方编辑时也不应显示
    /// </summary>
    [Fact]
    public void FormulaEdit_Regression_ShouldNotShowPrice()
    {
        // Arrange
        var herbCatalog = CreateHerbCatalog();
        var herbItems = new List<FormulaHerbItemViewModel>();

        // Act - 添加多味药材（药材本身有价格）
        var item1 = new FormulaHerbItemViewModel { AllHerbs = herbCatalog };
        item1.SelectedHerb = herbCatalog.First(h => h.Name == "当归"); // Price = 0.5
        item1.Dosage = 10;
        herbItems.Add(item1);

        var item2 = new FormulaHerbItemViewModel { AllHerbs = herbCatalog };
        item2.SelectedHerb = herbCatalog.First(h => h.Name == "黄芪"); // Price = 0.8
        item2.Dosage = 15;
        herbItems.Add(item2);

        // Assert - 所有药材的UnitPrice都应为0
        foreach (var item in herbItems)
        {
            item.UnitPrice.Should().Be(0m, "经验方不涉及价格计算");
        }
    }

    /// <summary>
    /// 回归测试：拼音码过滤功能正常工作
    /// </summary>
    [Fact]
    public void FormulaEdit_Regression_PinyinFilterShouldWork()
    {
        // Arrange
        var herbCatalog = CreateHerbCatalog();
        var item = new FormulaHerbItemViewModel { AllHerbs = herbCatalog };

        // Act & Assert - 测试各种拼音码搜索
        item.HerbName = "dg";
        item.FilteredHerbs.Should().Contain(h => h.Name == "当归", "拼音码'dg'应匹配'当归'");

        item.HerbName = "hq";
        item.FilteredHerbs.Should().Contain(h => h.Name == "黄芪", "拼音码'hq'应匹配'黄芪'");

        item.HerbName = "fl";
        item.FilteredHerbs.Should().Contain(h => h.Name == "茯苓", "拼音码'fl'应匹配'茯苓'");
    }

    /// <summary>
    /// 回归测试：选择药材后自动填充属性
    /// </summary>
    [Fact]
    public void FormulaEdit_Regression_SelectedHerbShouldAutoFill()
    {
        // Arrange
        var herbCatalog = CreateHerbCatalog();
        var targetHerb = herbCatalog.First(h => h.Name == "党参");
        var item = new FormulaHerbItemViewModel { AllHerbs = herbCatalog };

        // Act
        item.SelectedHerb = targetHerb;

        // Assert
        item.HerbId.Should().Be(targetHerb.Id, "HerbId应自动填充");
        item.HerbName.Should().Be("党参", "HerbName应自动填充");
        item.Unit.Should().Be("g", "Unit应自动填充");
    }

    /// <summary>
    /// 回归测试：Remark属性正常工作
    /// </summary>
    [Fact]
    public void FormulaEdit_Regression_RemarkShouldWork()
    {
        // Arrange
        var item = new FormulaHerbItemViewModel();

        // Act
        item.Remark = "先煎30分钟";

        // Assert
        item.Remark.Should().Be("先煎30分钟", "备注应正确保存");
    }

    /// <summary>
    /// 回归测试：ToDto正确转换
    /// </summary>
    [Fact]
    public void FormulaEdit_Regression_ToDtoShouldWork()
    {
        // Arrange
        var herbCatalog = CreateHerbCatalog();
        var targetHerb = herbCatalog.First(h => h.Name == "白术");
        var item = new FormulaHerbItemViewModel { AllHerbs = herbCatalog };
        item.SelectedHerb = targetHerb;
        item.Dosage = 12;
        item.Remark = "炒用";

        // Act
        var dto = item.ToDto();

        // Assert
        dto.HerbId.Should().Be(targetHerb.Id);
        dto.HerbName.Should().Be("白术");
        dto.Dosage.Should().Be(12);
        dto.Unit.Should().Be("g");
        dto.ProcessingMethod.Should().Be("炒用");
    }

    #endregion

    #region Complete Workflow Tests - 完整流程测试

    /// <summary>
    /// 测试：完整的经验方编辑流程
    /// 模拟创建四君子汤：人参、白术、茯苓、甘草
    /// </summary>
    [Fact]
    public void FormulaEditFlow_CreateSijunziTang_ShouldSucceed()
    {
        // Arrange - 使用测试数据（实际四君子汤用人参，这里用党参替代）
        var herbCatalog = CreateHerbCatalog();
        var formulaHerbs = new List<FormulaHerbItemViewModel>();

        // Act - 添加四味药材
        // 党参（替代人参）9g
        var item1 = new FormulaHerbItemViewModel { AllHerbs = herbCatalog };
        item1.HerbName = "ds";
        item1.SelectedHerb = item1.FilteredHerbs.FirstOrDefault(h => h.Name == "党参");
        item1.Dosage = 9;
        formulaHerbs.Add(item1);

        // 白术 9g
        var item2 = new FormulaHerbItemViewModel { AllHerbs = herbCatalog };
        item2.HerbName = "bz";
        item2.SelectedHerb = item2.FilteredHerbs.FirstOrDefault(h => h.Name == "白术");
        item2.Dosage = 9;
        formulaHerbs.Add(item2);

        // 茯苓 9g
        var item3 = new FormulaHerbItemViewModel { AllHerbs = herbCatalog };
        item3.HerbName = "fl";
        item3.SelectedHerb = item3.FilteredHerbs.FirstOrDefault(h => h.Name == "茯苓");
        item3.Dosage = 9;
        formulaHerbs.Add(item3);

        // 甘草 6g（炙用）
        var item4 = new FormulaHerbItemViewModel { AllHerbs = herbCatalog };
        item4.HerbName = "gc";
        item4.SelectedHerb = item4.FilteredHerbs.FirstOrDefault(h => h.Name == "甘草");
        item4.Dosage = 6;
        item4.Remark = "炙用";
        formulaHerbs.Add(item4);

        // Assert - 验证药材信息
        formulaHerbs.Should().HaveCount(4, "四君子汤应有4味药");
        formulaHerbs.All(h => h.HerbId != Guid.Empty).Should().BeTrue("所有药材应已选择");
        formulaHerbs.All(h => h.UnitPrice == 0m).Should().BeTrue("经验方不显示价格");

        // Assert - 验证各药材
        formulaHerbs[0].HerbName.Should().Be("党参");
        formulaHerbs[1].HerbName.Should().Be("白术");
        formulaHerbs[2].HerbName.Should().Be("茯苓");
        formulaHerbs[3].HerbName.Should().Be("甘草");
        formulaHerbs[3].Remark.Should().Be("炙用");

        // Assert - 转换为DTO验证
        var dtos = formulaHerbs.Select(h => h.ToDto()).ToList();
        dtos.Should().HaveCount(4);
        dtos.Sum(d => d.Dosage).Should().Be(33, "总剂量为 9+9+9+6 = 33g");
    }

    /// <summary>
    /// 测试：修改经验方药材流程
    /// </summary>
    [Fact]
    public void FormulaEditFlow_ModifyHerb_ShouldSucceed()
    {
        // Arrange
        var herbCatalog = CreateHerbCatalog();
        var item = new FormulaHerbItemViewModel { AllHerbs = herbCatalog };
        item.SelectedHerb = herbCatalog.First(h => h.Name == "当归");
        item.Dosage = 10;
        var originalHerbId = item.HerbId;

        // Act - 更换药材
        item.SelectedHerb = herbCatalog.First(h => h.Name == "黄芪");

        // Assert
        item.HerbId.Should().NotBe(originalHerbId, "HerbId应已更新");
        item.HerbName.Should().Be("黄芪", "HerbName应已更新");
        item.UnitPrice.Should().Be(0m, "更换药材后UnitPrice仍为0");
    }

    #endregion

    #region Shared Control Behavior Tests - 共享控件行为测试

    /// <summary>
    /// 测试：中文名精确匹配后清空建议列表（避免Popup一直显示）
    /// </summary>
    [Fact]
    public void SharedControlBehavior_ExactChineseNameMatch_ShouldClearSuggestions()
    {
        // Arrange
        var herbCatalog = CreateHerbCatalog();
        var item = new FormulaHerbItemViewModel { AllHerbs = herbCatalog };

        // Act - 精确输入药材名称
        item.HerbName = "当归";

        // Assert - 精确匹配后不显示建议
        item.FilteredHerbs.Should().BeEmpty("精确匹配中文名后不显示建议列表");
    }

    /// <summary>
    /// 测试：最多显示5个搜索结果
    /// </summary>
    [Fact]
    public void SharedControlBehavior_ShouldLimitTo5Results()
    {
        // Arrange
        var herbCatalog = CreateHerbCatalog();
        var item = new FormulaHerbItemViewModel { AllHerbs = herbCatalog };

        // Act - 输入匹配多个药材的字符
        item.HerbName = "g"; // 匹配多个拼音码包含g的药材

        // Assert
        item.FilteredHerbs.Count.Should().BeLessOrEqualTo(5, "最多显示5个建议结果");
    }

    #endregion
}
