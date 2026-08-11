using System.Reflection;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace LYBT.Tests.Server;

/// <summary>
/// 远程 IdentityController 路由单测（AC-TEST P0#1 闭环：
/// T4 修复「/api/v1/users/api/v1/auth/* 双重前缀」后仍无测试守护——本测试防路由回归）
/// </summary>
public class IdentityControllerRoutesTests
{
    private static readonly Type ControllerType =
        typeof(LYBT.WebAPI.Controllers.IdentityController);

    [Fact]
    public void ClassRoute_UsesUsersPrefix()
    {
        var route = ControllerType.GetCustomAttribute<RouteAttribute>();
        route.Should().NotBeNull();
        route!.Template.Should().Be("api/v{version:apiVersion}/users");
    }

    [Theory]
    [InlineData("LoginAsync", "POST", "/api/v{version:apiVersion}/auth/login")]
    [InlineData("LogoutAsync", "POST", "/api/v{version:apiVersion}/auth/logout")]
    [InlineData("RefreshTokenAsync", "POST", "/api/v{version:apiVersion}/auth/refresh")]
    [InlineData("AutoLoginAsync", "POST", "/api/v{version:apiVersion}/auth/auto-login")]
    [InlineData("ValidateTokenFromHeaderAsync", "GET", "/api/v{version:apiVersion}/auth/validate")]
    public void AuthEndpoints_UseAbsolutePath_NotUsersPrefix(string methodName, string httpMethod, string expectedRoute)
    {
        var method = ControllerType.GetMethod(methodName, BindingFlags.Public | BindingFlags.Instance);
        method.Should().NotBeNull($"method {methodName} should exist");

        // 收集 action 的 Http 动词路由模板（HttpPost/HttpGet/HttpPut/HttpDelete/HttpPatch）
        var templates = method!.GetCustomAttributes(true)
            .OfType<Microsoft.AspNetCore.Mvc.Routing.HttpMethodAttribute>()
            .Select(a => (a.HttpMethods.Contains(httpMethod), a.Template))
            .Where(t => t.Item1)
            .Select(t => t.Template)
            .ToList();

        templates.Should().Contain(expectedRoute, $"endpoint {methodName} must route to {expectedRoute}");

        // P0#1 回归守卫：绝对路径（/ 开头）——组合路由不得出现 "users/api/v1/auth"
        var fullTemplate = templates.First(t => t == expectedRoute);
        fullTemplate.Should().StartWith("/", "T4 修复要求绝对路径，避免类前缀拼接双重前缀");
        fullTemplate.Should().NotContain("users/", "双重前缀回归守卫");
    }

    [Fact]
    public void AuthEndpoints_DoNotCarryUsersPrefix_InCombinedRoute()
    {
        var classTemplate = ControllerType.GetCustomAttribute<RouteAttribute>()!.Template;

        // 类前缀 "api/v{version:apiVersion}/users" + 绝对路径 action 模板 → 组合 = action 模板本身（绝对路径覆盖类前缀）
        foreach (var methodName in new[] { "LoginAsync", "LogoutAsync", "RefreshTokenAsync", "AutoLoginAsync", "ValidateTokenFromHeaderAsync" })
        {
            var method = ControllerType.GetMethod(methodName, BindingFlags.Public | BindingFlags.Instance)!;
            var actionTemplate = method.GetCustomAttributes(true)
                .OfType<Microsoft.AspNetCore.Mvc.Routing.HttpMethodAttribute>()
                .Select(a => a.Template)
                .First()!;

            // 组合路由（类前缀 + action）：绝对路径 action 忽略类前缀——最终为 action 模板
            var combined = actionTemplate.StartsWith('/') ? actionTemplate : $"{classTemplate}/{actionTemplate}";
            combined.Should().NotContain("users/api/v1", $"{methodName} 组合路由不得出现双重前缀");
            combined.Should().NotContain("users/", $"{methodName} 不得携带 users 类前缀");
            combined.Should().Contain("auth/", $"{methodName} 应解析为独立 auth 路由");
        }
    }
}
