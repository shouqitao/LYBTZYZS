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
}
