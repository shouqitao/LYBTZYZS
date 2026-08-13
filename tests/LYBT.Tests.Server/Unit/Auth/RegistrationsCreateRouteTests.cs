using System.Reflection;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace LYBT.Tests.Server;

/// <summary>
/// 恢复验证（restore-create-registration 2026-08-13——a99619f47 误删 POST /Registrations 创建端点，
/// 真机 405 证实——swagger 显示无 POST）。恢复后路由特性断言。
/// </summary>
public class RegistrationsCreateRouteTests
{
    private static readonly Type ControllerType =
        typeof(LYBT.WebAPI.Controllers.RegistrationsController);

    [Fact]
    public void Create_Endpoint_Restored_WithPostRoute()
    {
        var method = ControllerType.GetMethod("Create");
        method.Should().NotBeNull("Create 端点必须恢复（QuickVisit 两步第 1 步）");

        var httpPost = method!.GetCustomAttribute<HttpPostAttribute>();
        httpPost.Should().NotBeNull("Create 必须有 [HttpPost]（真机 405 根因——无 POST 路由）");
        httpPost!.Template.Should().BeNull("Create 应绑定类级路由 POST /api/v1/Registrations");

        var authorize = method!.GetCustomAttribute<AuthorizeAttribute>();
        authorize.Should().NotBeNull();
        authorize!.Policy.Should().Be(LYBT.Infrastructure.Constants.PolicyConstants.DoctorOrReceptionist,
            "医生/前台均可建号（Source 区分）");
    }

    [Fact]
    public void QuickVisit_Endpoint_RemainsRemoved()
    {
        var method = ControllerType.GetMethod("QuickVisit");
        method.Should().BeNull("quick-visit 端点已按产品决策删除——不得恢复");
    }
}
