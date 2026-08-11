using System.Reflection;
using FluentAssertions;
using LYBT.Infrastructure.Constants;
using Microsoft.AspNetCore.Authorization;
using Xunit;

namespace LYBT.Tests.Desktop.Unit.Shell;

/// <summary>
/// 本地 Deploy 权限收紧单测（DEPLOY-PERM 双端同步——本地端同 SysAdminOnly）
/// </summary>
public class LocalDeployControllerPermissionTests
{
    [Fact]
    public void LocalDeployController_RequiresSysAdminOnly()
    {
        var authorize = typeof(LYBT.LocalWebAPI.Controllers.DeployController)
            .GetCustomAttribute<AuthorizeAttribute>();
        authorize.Should().NotBeNull();
        authorize!.Policy.Should().Be(PolicyConstants.SysAdminOnly,
            "部署属运维操作——Admin 业务管理员不应有部署能力（US-SHELL-020 AC 注）");
    }
}
