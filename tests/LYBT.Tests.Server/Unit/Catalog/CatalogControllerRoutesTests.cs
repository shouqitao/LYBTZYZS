using System.Reflection;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Routing;
using Xunit;

namespace LYBT.Tests.Server;

/// <summary>
/// CatalogController 路由单测（CATALOG-ROUTE-FIX 回归守卫：formulas 动作路由必须为
/// 绝对路径（/ 开头）——类级 Route("api/v{version}/herbs") + 相对完整前缀会拼接出
/// version 参数两次 → ControllerActionDescriptorProvider 启动崩溃）
/// </summary>
public class CatalogControllerRoutesTests
{
    private static readonly Type ControllerType =
        typeof(LYBT.WebAPI.Controllers.CatalogController);

    [Fact]
    public void ClassRoute_UsesHerbsPrefix_WithSingleVersion()
    {
        var route = ControllerType.GetCustomAttribute<Microsoft.AspNetCore.Mvc.RouteAttribute>();
        route.Should().NotBeNull();
        route!.Template.Should().Be("api/v{version:apiVersion}/herbs");
    }

    [Fact]
    public void FormulaActionRoutes_UseAbsolutePath_NoDoubleVersion()
    {
        var methods = ControllerType.GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Where(m => m.GetCustomAttributes(true)
                .OfType<HttpMethodAttribute>()
                .Any(a => a.Template?.Contains("formulas") == true))
            .ToList();

        methods.Should().NotBeEmpty("formulas 动作路由应存在");

        foreach (var method in methods)
        {
            foreach (var attr in method.GetCustomAttributes(true).OfType<HttpMethodAttribute>())
            {
                if (attr.Template?.Contains("formulas") != true) continue;

                // 绝对路径（/ 开头）——覆盖类级 herbs 前缀，version 参数仅一次
                attr.Template.Should().StartWith("/api/v{version:apiVersion}/formulas",
                    $"{method.Name} 路由必须为绝对路径（防 version 参数重复启动崩溃）");
                attr.Template!.Split("{version", StringSplitOptions.None).Length.Should().BeLessThanOrEqualTo(2,
                    $"{method.Name} 路由不得出现 version 参数重复（{attr.Template}）");
            }
        }
    }
}
