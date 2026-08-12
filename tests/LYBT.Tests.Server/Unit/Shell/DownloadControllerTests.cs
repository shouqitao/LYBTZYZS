using System.Reflection;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Xunit;

namespace LYBT.Tests.Server;

/// <summary>
/// 下载主页公开性单测（SHELL-010 决策 A: GET / 公开可访问——无需认证——
/// 反射断言 AllowAnonymous + 路由，防 FallbackPolicy 误拦截回归）
/// </summary>
public class DownloadControllerTests
{
    private static readonly Type ControllerType =
        typeof(LYBT.WebAPI.Controllers.DownloadController);

    [Fact]
    public void Index_IsAllowAnonymous()
    {
        var method = ControllerType.GetMethod("Index")!;
        method.GetCustomAttribute<AllowAnonymousAttribute>()
            .Should().NotBeNull("下载页必须公开可访问（无需认证）——US-SHELL-010 决策 A");
    }

    [Fact]
    public void Index_RoutesToRoot_WithHttpGet()
    {
        var method = ControllerType.GetMethod("Index")!;
        var get = method.GetCustomAttribute<HttpGetAttribute>();
        get.Should().NotBeNull();
        get!.Template.Should().Be("/");
    }

    [Fact]
    public void Controller_HasEmptyRoutePrefix()
    {
        var route = ControllerType.GetCustomAttribute<RouteAttribute>();
        route.Should().NotBeNull();
        route!.Template.Should().Be("");
    }
}
