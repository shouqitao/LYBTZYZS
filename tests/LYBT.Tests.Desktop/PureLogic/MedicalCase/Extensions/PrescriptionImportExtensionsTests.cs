using LYBT.Desktop.MedicalCase.Extensions;
using LYBT.Shared.Models.Contracts.Formula;
using LYBT.Shared.Models.Contracts.Prescriptions;

namespace LYBT.Tests.Desktop.PureLogic.MedicalCase.Extensions;

/// <summary>
/// PrescriptionImportExtensions 单元测试
/// CODE-08: 验证验方导入和历史复制时价格同步
/// </summary>
public class PrescriptionImportExtensionsTests
{
    private static readonly Guid HerbId1 = Guid.NewGuid();
    private static readonly Guid HerbId2 = Guid.NewGuid();

    private static Dictionary<Guid, decimal> CreatePriceLookup() => new()
    {
        [HerbId1] = 12.5m,
        [HerbId2] = 8.0m
    };

    #region Formula Import (US-MC-016)

    [Fact]
    public void ToPrescriptionItemDtos_FormulaImport_WithPrices_ShouldFillUnitPrice()
    {
        // Arrange
        var formula = new FormulaDetailDto { Id = Guid.NewGuid(), Name = "四物汤" };
        var herbs = new List<FormulaHerbItemDto>
        {
            new() { HerbId = HerbId1, HerbName = "当归", Dosage = 10 },
            new() { HerbId = HerbId2, HerbName = "川芎", Dosage = 6 }
        };
        var prices = CreatePriceLookup();

        // Act
        var result = formula.ToPrescriptionItemDtos(herbs, prices);

        // Assert
        result.Should().HaveCount(2);
        result[0].UnitPrice.Should().Be(12.5m, "当归价格应从 herbPrices 查表填入");
        result[1].UnitPrice.Should().Be(8.0m, "川芎价格应从 herbPrices 查表填入");
    }

    [Fact]
    public void ToPrescriptionItemDtos_FormulaImport_WithoutPrices_ShouldLeaveZero()
    {
        // Arrange
        var formula = new FormulaDetailDto { Id = Guid.NewGuid(), Name = "四物汤" };
        var herbs = new List<FormulaHerbItemDto>
        {
            new() { HerbId = HerbId1, HerbName = "当归", Dosage = 10 }
        };

        // Act - no price lookup provided
        var result = formula.ToPrescriptionItemDtos(herbs);

        // Assert
        result.Should().HaveCount(1);
        result[0].UnitPrice.Should().Be(0m, "无价格查表时应为 0");
    }

    [Fact]
    public void ToPrescriptionItemDtos_FormulaImport_HerbNotInPriceLookup_ShouldLeaveZero()
    {
        // Arrange
        var unknownHerbId = Guid.NewGuid();
        var formula = new FormulaDetailDto { Id = Guid.NewGuid(), Name = "测试方" };
        var herbs = new List<FormulaHerbItemDto>
        {
            new() { HerbId = unknownHerbId, HerbName = "未知药", Dosage = 5 }
        };
        var prices = CreatePriceLookup();

        // Act
        var result = formula.ToPrescriptionItemDtos(herbs, prices);

        // Assert
        result.Should().HaveCount(1);
        result[0].UnitPrice.Should().Be(0m, "查表中不存在的药材价格应为 0");
    }

    #endregion

    #region History Copy (US-MC-018)

    [Fact]
    public void ToPrescriptionItemDtos_HistoryCopy_WithPrices_ShouldRefreshToCurrentPrice()
    {
        // Arrange - historical items with old prices
        var items = new List<PrescriptionItemDto>
        {
            new() { HerbId = HerbId1, HerbName = "当归", Dosage = 10, UnitPrice = 5.0m },
            new() { HerbId = HerbId2, HerbName = "川芎", Dosage = 6, UnitPrice = 3.0m }
        };
        var currentPrices = CreatePriceLookup();

        // Act
        var result = items.ToPrescriptionItemDtos(currentPrices);

        // Assert
        result.Should().HaveCount(2);
        result[0].UnitPrice.Should().Be(12.5m, "CODE-08: 复制处方应刷新为当前价格 12.5");
        result[1].UnitPrice.Should().Be(8.0m, "CODE-08: 复制处方应刷新为当前价格 8.0");
    }

    [Fact]
    public void ToPrescriptionItemDtos_HistoryCopy_WithoutPrices_ShouldKeepOriginalPrice()
    {
        // Arrange
        var items = new List<PrescriptionItemDto>
        {
            new() { HerbId = HerbId1, HerbName = "当归", Dosage = 10, UnitPrice = 5.0m }
        };

        // Act - no price lookup, preserve original
        var result = items.ToPrescriptionItemDtos();

        // Assert
        result.Should().HaveCount(1);
        result[0].UnitPrice.Should().Be(5.0m, "无价格查表时应保持原价");
    }

    #endregion
}
