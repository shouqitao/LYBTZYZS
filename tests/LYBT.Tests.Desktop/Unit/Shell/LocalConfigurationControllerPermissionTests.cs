using System.Reflection;
using FluentAssertions;
using LYBT.Infrastructure.Constants;
using Microsoft.AspNetCore.Authorization;
using Xunit;

namespace LYBT.Tests.Desktop.Unit.Shell;

/// <summary>
/// 本地 Configuration 权限收紧单测（CONFIG-PERM-FIX 双端同步——本地端同 SysAdminOnly）
/// </summary>
public class LocalConfigurationControllerPermissionTests
{
    [Fact]
    public void LocalConfigurationController_RequiresSysAdminOnly()
    {
        var authorize = typeof(LYBT.LocalWebAPI.Controllers.ConfigurationController)
            .GetCustomAttribute<AuthorizeAttribute>();
        authorize.Should().NotBeNull();
        authorize!.Policy.Should().Be(PolicyConstants.SysAdminOnly,
            "配置管理属 sysadmin 专属运维操作——Admin 业务管理员不应访问系统配置（US-SHELL-018 角色: sysadmin）");
    }

    [Fact]
    public void LocalConfigurationController_NoMethodLevelAdminOrSuperAdmin()
    {
        // 类级 SysAdminOnly 后，任何方法级 AdminOrSuperAdmin 覆盖都会重新打开缺口——防回归
        var methods = typeof(LYBT.LocalWebAPI.Controllers.ConfigurationController)
            .GetMethods(BindingFlags.Instance | BindingFlags.Public);

        foreach (var method in methods)
        {
            var authorize = method.GetCustomAttributes<AuthorizeAttribute>(inherit: true).ToList();
            foreach (var attr in authorize)
            {
                attr.Policy.Should().NotBe(PolicyConstants.AdminOrSuperAdmin,
                    $"{method.Name} 不应有方法级 AdminOrSuperAdmin 覆盖（配置管理 sysadmin 专属）");
                attr.Policy.Should().Be(PolicyConstants.SysAdminOnly,
                    $"{method.Name} 方法级授权（若有）必须为 SysAdminOnly");
            }
        }
    }
}
