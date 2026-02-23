using System.Collections.ObjectModel;
using FluentAssertions;
using LYBT.Desktop.Infrastructure.Controls.HerbItem;
using LYBT.Shared.Models.Contracts.Herbs;
using Xunit;

namespace LYBT.Desktop.MedicalCase.Tests.Integration;

/// <summary>
/// 处方编辑流程集成测试
/// Issue: unify-herb-card-control - Phase 4 Task 4.2
/// OpenSpec: unify-frontend-backend-types Phase 8.4 - 类型重命名
/// OpenSpec: unify-control-data-binding - PrescriptionHerbItem已删除，由HerbItemControlViewModel替代
/// 验证处方药材添加、价格计算、剂量修改的完整流程
/// </summary>
public class PrescriptionEditFlowTests
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

    /// <summary>
    /// 计算小计金额 (UnitPrice * Dosage)
    /// HerbItemControlViewModel不再包含ItemTotal属性，改为在DTO层计算
    /// 测试中直接计算以验证核心逻辑
    /// </summary>
    private static decimal CalculateItemTotal(HerbItemControlViewModel vm) => vm.UnitPrice * vm.Dosage;

    #endregion

    #region Complete Workflow Tests - 完整流程测试

    /// <summary>
    /// 测试：完整的处方编辑流程 - 添加多味药材并计算总价
    /// 模拟用户操作：输入拼音码 - 选择药材 - 输入剂量 - 查看价格
    /// </summary>
    [Fact]
    public void PrescriptionEditFlow_AddMultipleHerbs_ShouldCalculateTotalCorrectly()
    {
        // Arrange - 准备药材目录
        var herbCatalog = CreateHerbCatalog();
        var herbItems = new List<HerbItemControlViewModel>();

        // Act - Step 1: 添加当归 10g (0.5元/g = 5元)
        var item1 = new HerbItemControlViewModel { AllHerbs = herbCatalog };
        item1.HerbName = "dangg"; // 输入拼音码前缀
        item1.FilteredHerbs.Should().Contain(h => h.Name == "当归", "应能通过拼音码找到当归");
        item1.SelectedHerb = item1.FilteredHerbs.First(h => h.Name == "当归");
        item1.Dosage = 10;
        herbItems.Add(item1);

        // Act - Step 2: 添加黄芪 15g (0.8元/g = 12元)
        var item2 = new HerbItemControlViewModel { AllHerbs = herbCatalog };
        item2.HerbName = "huangq";
        item2.SelectedHerb = item2.FilteredHerbs.First(h => h.Name == "黄芪");
        item2.Dosage = 15;
        herbItems.Add(item2);

        // Act - Step 3: 添加甘草 6g (0.2元/g = 1.2元)
        var item3 = new HerbItemControlViewModel { AllHerbs = herbCatalog };
        item3.HerbName = "gancao";
        item3.SelectedHerb = item3.FilteredHerbs.First(h => h.Name == "甘草");
        item3.Dosage = 6;
        herbItems.Add(item3);

        // Assert - 验证各项小计
        CalculateItemTotal(item1).Should().Be(5m, "当归 10g x 0.5元/g = 5元");
        CalculateItemTotal(item2).Should().Be(12m, "黄芪 15g x 0.8元/g = 12元");
        CalculateItemTotal(item3).Should().Be(1.2m, "甘草 6g x 0.2元/g = 1.2元");

        // Assert - 验证总价
        var totalPrice = herbItems.Sum(h => CalculateItemTotal(h));
        totalPrice.Should().Be(18.2m, "总价应为 5 + 12 + 1.2 = 18.2元");
    }

    /// <summary>
    /// 测试：修改剂量后价格自动更新
    /// </summary>
    [Fact]
    public void PrescriptionEditFlow_ModifyDosage_ShouldUpdatePriceAutomatically()
    {
        // Arrange
        var herbCatalog = CreateHerbCatalog();
        var item = new HerbItemControlViewModel { AllHerbs = herbCatalog };
        item.SelectedHerb = herbCatalog.First(h => h.Name == "当归");
        item.Dosage = 10;
        var initialTotal = CalculateItemTotal(item);

        // Act - 修改剂量
        item.Dosage = 20;

        // Assert
        initialTotal.Should().Be(5m, "初始小计应为 10g x 0.5元/g = 5元");
        CalculateItemTotal(item).Should().Be(10m, "修改后小计应为 20g x 0.5元/g = 10元");
    }

    /// <summary>
    /// 测试：更换药材后价格自动更新
    /// </summary>
    [Fact]
    public void PrescriptionEditFlow_ChangeHerb_ShouldUpdatePriceAutomatically()
    {
        // Arrange
        var herbCatalog = CreateHerbCatalog();
        var item = new HerbItemControlViewModel { AllHerbs = herbCatalog };
        item.SelectedHerb = herbCatalog.First(h => h.Name == "当归"); // 0.5元/g
        item.Dosage = 10;
        var initialTotal = CalculateItemTotal(item);

        // Act - 更换为黄芪 (0.8元/g)
        item.SelectedHerb = herbCatalog.First(h => h.Name == "黄芪");

        // Assert
        initialTotal.Should().Be(5m, "当归 10g x 0.5元/g = 5元");
        CalculateItemTotal(item).Should().Be(8m, "黄芪 10g x 0.8元/g = 8元");
    }

    #endregion

    #region Pinyin Search Flow Tests - 拼音搜索流程测试

    /// <summary>
    /// 测试：拼音码搜索流程 - 验证搜索结果包含匹配项
    /// HerbItemControlViewModel使用Contains匹配，需要输入拼音码的连续子串
    /// </summary>
    [Fact]
    public void PinyinSearchFlow_ProgressiveInput_ShouldFindMatchingHerbs()
    {
        // Arrange
        var herbCatalog = CreateHerbCatalog();
        var item = new HerbItemControlViewModel { AllHerbs = herbCatalog };

        // Act - 输入 "dangg" 应匹配当归(danggui)
        item.HerbName = "dangg";

        // Assert - 验证搜索结果
        item.FilteredHerbs.Should().NotBeEmpty("输入'dangg'应返回搜索结果");
        item.FilteredHerbs.Should().Contain(h => h.Name == "当归", "搜索结果应包含当归");
    }

    /// <summary>
    /// 测试：中文名搜索流程
    /// </summary>
    [Fact]
    public void ChineseNameSearchFlow_ShouldFindMatchingHerbs()
    {
        // Arrange
        var herbCatalog = CreateHerbCatalog();
        var item = new HerbItemControlViewModel { AllHerbs = herbCatalog };

        // Act - 输入中文名前缀
        item.HerbName = "当";

        // Assert
        item.FilteredHerbs.Should().Contain(h => h.Name == "当归", "应能通过中文名前缀找到当归");
    }

    #endregion

    #region Validation Flow Tests - 验证流程测试

    /// <summary>
    /// 测试：剂量验证流程 - 无效剂量应标记为无效
    /// </summary>
    [Fact]
    public void ValidationFlow_InvalidDosage_ShouldShowError()
    {
        // Arrange
        var herbCatalog = CreateHerbCatalog();
        var item = new HerbItemControlViewModel { AllHerbs = herbCatalog };
        item.SelectedHerb = herbCatalog.First();

        // Act - 设置有效剂量
        item.Dosage = 10;
        var isValidAfterNormalDosage = item.IsDosageValid;

        // Act - 设置超大剂量
        item.Dosage = 600;
        var isValidAfterLargeDosage = item.IsDosageValid;

        // Assert
        isValidAfterNormalDosage.Should().BeTrue("10g是有效剂量");
        isValidAfterLargeDosage.Should().BeFalse("600g超过最大剂量500g");
        item.ValidationMessage.Should().Contain("500", "应提示最大剂量限制");
    }

    #endregion

    #region Multi-Item Workflow Tests - 多药材工作流测试

    /// <summary>
    /// 测试：N+1行原则 - 模拟批量添加药材
    /// </summary>
    [Fact]
    public void MultiItemWorkflow_BatchAdd_ShouldMaintainConsistency()
    {
        // Arrange
        var herbCatalog = CreateHerbCatalog();
        var herbItems = new ObservableCollection<HerbItemControlViewModel>();

        // 模拟N+1行原则：始终保持4个空槽位
        for (int i = 0; i < 4; i++)
        {
            herbItems.Add(new HerbItemControlViewModel { AllHerbs = herbCatalog });
        }

        // Act - 填充第一个槽位
        herbItems[0].SelectedHerb = herbCatalog.First(h => h.Name == "当归");
        herbItems[0].Dosage = 10;

        // Act - 填充第二个槽位
        herbItems[1].SelectedHerb = herbCatalog.First(h => h.Name == "黄芪");
        herbItems[1].Dosage = 15;

        // Assert - 验证已填充项有价格，空项价格为0
        CalculateItemTotal(herbItems[0]).Should().Be(5m);
        CalculateItemTotal(herbItems[1]).Should().Be(12m);
        CalculateItemTotal(herbItems[2]).Should().Be(0m, "空槽位价格应为0");
        CalculateItemTotal(herbItems[3]).Should().Be(0m, "空槽位价格应为0");

        // Assert - 计算有效药材总价
        var validItems = herbItems.Where(h => h.HerbId != Guid.Empty);
        var totalPrice = validItems.Sum(h => CalculateItemTotal(h));
        totalPrice.Should().Be(17m, "有效药材总价为 5 + 12 = 17元");
    }

    #endregion
}
