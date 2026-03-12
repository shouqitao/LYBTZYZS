using FluentAssertions;
using LYBT.Entities.Formulas;
using Xunit;
using FormulaEntity = LYBT.Entities.Formulas.Formula;

namespace LYBT.Tests.Server.Unit.Entities.Formula;

/// <summary>
/// Formula entity tests - collection navigation property behavior only.
/// Trivial property getter/setter tests removed (test restructuring 2026-03-05).
/// </summary>
public class FormulaModelTests
{
    [Fact]
    public void Herbs_ShouldBeInitializedAsEmptyList()
    {
        var formula = new FormulaEntity();

        formula.Herbs.Should().NotBeNull();
        formula.Herbs.Should().BeEmpty();
    }

    [Fact]
    public void Herbs_ListOperations_ShouldWork()
    {
        var formula = new FormulaEntity();
        var herb1 = new FormulaHerbItem { HerbName = "当归", Dosage = 12 };
        var herb2 = new FormulaHerbItem { HerbName = "白芍", Dosage = 12 };

        formula.Herbs.Add(herb1);
        formula.Herbs.Add(herb2);

        formula.Herbs.Should().HaveCount(2);
        formula.Herbs.Should().Contain(herb1);

        formula.Herbs.Remove(herb1);

        formula.Herbs.Should().HaveCount(1);
        formula.Herbs.Should().NotContain(herb1);
        formula.Herbs.Should().Contain(herb2);
    }
}
