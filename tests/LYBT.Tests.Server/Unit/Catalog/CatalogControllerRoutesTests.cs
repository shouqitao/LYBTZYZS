using System.Reflection;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Routing;
using Xunit;

namespace LYBT.Tests.Server;

/// <summary>
/// 目录路由单测（CATALOG-ROUTE-FIX 回归守卫：formulas 动作路由必须为绝对路径（/ 开头）——
/// 原 CatalogController 类级 Route("api/v{version}/herbs") + 相对完整前缀会拼接出 version 参数两次
/// → ControllerActionDescriptorProvider 启动崩溃；P1-24 拆分为 HerbsController + FormulasController，
/// Formulas 动作仍用绝对路由防 version 重复）。
/// </summary>
public class CatalogControllerRoutesTests
{
    private static readonly Type HerbControllerType =
        typeof(LYBT.WebAPI.Controllers.HerbsController);
    private static readonly Type FormulaControllerType =
        typeof(LYBT.WebAPI.Controllers.FormulasController);

    [Fact]
    public void HerbClassRoute_UsesHerbsPrefix_WithSingleVersion()
    {
        var route = HerbControllerType.GetCustomAttribute<Microsoft.AspNetCore.Mvc.RouteAttribute>();
        route.Should().NotBeNull();
        route!.Template.Should().Be("api/v{version:apiVersion}/herbs");
    }

    [Fact]
    public void FormulaClassRoute_UsesFormulasPrefix()
    {
        var route = FormulaControllerType.GetCustomAttribute<Microsoft.AspNetCore.Mvc.RouteAttribute>();
        route.Should().NotBeNull();
        route!.Template.Should().Be("api/v{version:apiVersion}/formulas");
    }

    [Fact]
    public void FormulaActionRoutes_UseAbsolutePath_NoDoubleVersion()
    {
        // N-03（2026-08-14）路由声明统一后：类级 Route 承载 version 占位符，动作级路由为相对路径。
        // 真正的不变量是「version 占位符全链只出现一次」——动作路由若再声明 {version} 会与类级前缀
        // 拼接出重复 version 参数（原 CatalogController 启动崩溃的根因）。
        foreach (var (name, controllerType, classRoute) in new[]
                 {
                     ("Herbs", HerbControllerType, "api/v{version:apiVersion}/herbs"),
                     ("Formulas", FormulaControllerType, "api/v{version:apiVersion}/formulas")
                 })
        {
            var declared = controllerType.GetCustomAttribute<Microsoft.AspNetCore.Mvc.RouteAttribute>();
            declared.Should().NotBeNull($"{name} 控制器必须有类级路由");
            declared!.Template.Should().Be(classRoute);
            CountVersionPlaceholders(classRoute).Should().Be(1, "类级路由是 version 占位符的唯一来源");

            var actions = controllerType.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .SelectMany(m => m.GetCustomAttributes(true).OfType<HttpMethodAttribute>()
                    .Where(a => a.Template != null)
                    .Select(a => (Method: m.Name, Template: a.Template!)))
                .ToList();

            actions.Should().NotBeEmpty($"{name} 控制器应有带路由模板的动作");

            foreach (var (method, template) in actions)
            {
                CountVersionPlaceholders(template).Should().Be(0,
                    $"{name}.{method} 动作路由 {template} 不得再声明 version 占位符（会与类级前缀重复）");
            }
        }
    }

    private static int CountVersionPlaceholders(string template) =>
        template.Split("{version", StringSplitOptions.None).Length - 1;
}
