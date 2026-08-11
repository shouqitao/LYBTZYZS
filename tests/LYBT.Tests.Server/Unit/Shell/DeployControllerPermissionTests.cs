using System.Reflection;
using FluentAssertions;
using LYBT.Infrastructure.Constants;
using Microsoft.AspNetCore.Authorization;
using Xunit;

namespace LYBT.Tests.Server;

/// <summary>
/// Deploy 权限收紧单测（DEPLOY-PERM: 部署属运维操作——Admin 业务管理员无部署能力，
/// 类级策略必须为 SysAdminOnly——防回归守卫）
/// </summary>
public class DeployControllerPermissionTests
{
    [Fact]
    public void RemoteDeployController_RequiresSysAdminOnly()
        => AssertSysAdminOnly(typeof(LYBT.WebAPI.Controllers.DeployController));

    private static void AssertSysAdminOnly(Type controllerType)
    {
        var authorize = controllerType.GetCustomAttribute<AuthorizeAttribute>();
        authorize.Should().NotBeNull($"{controllerType.Name} 应有类级授权策略");
        authorize!.Policy.Should().Be(PolicyConstants.SysAdminOnly,
            "部署属运维操作——Admin 业务管理员不应有部署能力（US-SHELL-020 AC 注）");
    }
}
