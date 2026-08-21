using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace LYBT.Tests.Server;

/// <summary>
/// 所有权检查分层守卫（P1-7/8/9 2026-08-14: Get+ValidateOwnership+SendCommand 模式
/// 从 Controller 移入 CommandHandler——Controller 不再直连 Service 做存在性/所有权前置检查，
/// 防回归到分层违规）
/// </summary>
public class OwnershipCheckLayeringTests
{
    [Theory]
    [InlineData(typeof(LYBT.WebAPI.Controllers.HerbsController))]
    [InlineData(typeof(LYBT.WebAPI.Controllers.FormulasController))]
    [InlineData(typeof(LYBT.WebAPI.Controllers.PatientsController))]
    public void RemoteControllers_DoNotDefineCheckOwnershipHelper(Type controllerType)
    {
        // P1-9 已移入 Handler——私有 CheckOwnershipAsync 辅助应删除（死代码清除）
        controllerType
            .GetMethod(
                "CheckOwnershipAsync",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance
            )
            .Should()
            .BeNull($"{controllerType.Name} 不得再有 CheckOwnershipAsync（P1-9 已移入 Handler）");
    }

    [Theory]
    [InlineData(typeof(LYBT.WebAPI.Controllers.HerbsController))]
    [InlineData(typeof(LYBT.WebAPI.Controllers.PatientsController))]
    public void RemoteControllers_UpdateDeleteToggle_StillExposeEndpoints(Type controllerType)
    {
        // 编排端点仍存在（SendCommand 模式——P1-7/8/9 仅移所有权检查，端点保留）
        foreach (var name in new[] { "Update", "Delete", "ToggleStatus" })
        {
            var m = controllerType.GetMethod(name);
            m.Should().NotBeNull($"{controllerType.Name}.{name} 应存在（P1-7/8/9 仅移所有权检查）");
            // async 端点返回 Task<IActionResult>
            m!.ReturnType.Should().BeAssignableTo(typeof(Task<IActionResult>));
        }
    }

    [Fact]
    public void FormulasController_UpdateDeleteToggle_StillExposeEndpoints()
    {
        // P1-24 拆分后 FormulasController 用独立方法名（UpdateFormula/DeleteFormula/ToggleFormulaStatus）
        foreach (var name in new[] { "UpdateFormula", "DeleteFormula", "ToggleFormulaStatus" })
        {
            var m = typeof(LYBT.WebAPI.Controllers.FormulasController).GetMethod(name);
            m.Should().NotBeNull($"FormulasController.{name} 应存在（P1-7/8/9 仅移所有权检查）");
            m!.ReturnType.Should().BeAssignableTo(typeof(Task<IActionResult>));
        }
    }

    [Fact]
    public void RemotePatientsController_DoesNotDefineCheckOwnershipHelper()
    {
        // P1-9 已移入 Handler——私有 CheckOwnershipAsync 辅助应删除（死代码清除）
        typeof(LYBT.WebAPI.Controllers.PatientsController)
            .GetMethod(
                "CheckOwnershipAsync",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance
            )
            .Should()
            .BeNull("PatientsController 不得再有 CheckOwnershipAsync（P1-9 已移入 Handler）");
    }
}
