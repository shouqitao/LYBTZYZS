using FluentAssertions;
using Xunit;

namespace LYBT.Tests.Desktop.Unit.Shell;

/// <summary>
/// Local 所有权检查分层守卫（P1-7/8/9 2026-08-14 双端同步——Controller 不再做
/// Get+ValidateOwnership 前置检查，已移入 CommandHandler）
/// </summary>
public class LocalOwnershipCheckLayeringTests
{
    [Theory]
    [InlineData(typeof(LYBT.LocalWebAPI.Controllers.HerbsController))]
    [InlineData(typeof(LYBT.LocalWebAPI.Controllers.FormulasController))]
    [InlineData(typeof(LYBT.LocalWebAPI.Controllers.PatientsController))]
    public void LocalControllers_DoNotDefineCheckOwnershipHelper(Type controllerType)
    {
        controllerType
            .GetMethod(
                "CheckOwnershipAsync",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance
            )
            .Should()
            .BeNull($"{controllerType.Name} 不得再有 CheckOwnershipAsync（P1-7/8/9 已移入 Handler）");
    }

    [Fact]
    public void LocalControllers_UpdateDeleteToggle_StillExposeEndpoints()
    {
        // P1-24 拆分后：Herbs/Patients 用 Update/Delete/ToggleStatus；Formulas 用独立名
        foreach (var type in new[]
        {
            typeof(LYBT.LocalWebAPI.Controllers.HerbsController),
            typeof(LYBT.LocalWebAPI.Controllers.PatientsController)
        })
        {
            foreach (var name in new[] { "Update", "Delete", "ToggleStatus" })
            {
                type.GetMethod(name).Should().NotBeNull($"{type.Name}.{name} 应存在");
            }
        }
    }

    [Fact]
    public void LocalFormulasController_UpdateDeleteToggle_StillExposeEndpoints()
    {
        foreach (var name in new[] { "UpdateFormula", "DeleteFormula", "ToggleFormulaStatus" })
        {
            typeof(LYBT.LocalWebAPI.Controllers.FormulasController).GetMethod(name)
                .Should().NotBeNull($"FormulasController.{name} 应存在");
        }
    }
}
