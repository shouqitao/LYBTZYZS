using System.Reflection;
using FluentAssertions;
using LYBT.Infrastructure.Constants;
using Microsoft.AspNetCore.Authorization;
using Xunit;

namespace LYBT.Tests.Server;

/// <summary>
/// Configuration 权限隔离单测（CONFIG-PERM-FIX: 配置管理属 sysadmin 专属运维操作——
/// 业务管理员（Admin）不应访问系统配置（App/ConnectionStrings/Jwt 等敏感配置），
/// 类级策略必须为 SysAdminOnly——防回归守卫）
/// </summary>
public class ConfigurationControllerPermissionTests
{
    [Fact]
    public void RemoteConfigurationController_RequiresSysAdminOnly()
        => AssertSysAdminOnly(typeof(LYBT.WebAPI.Controllers.ConfigurationController));

    [Fact]
    public void RemoteConfigurationController_NoMethodLevelAdminOrSuperAdmin()
    {
        // 类级 SysAdminOnly 后，任何方法级 AdminOrSuperAdmin 覆盖都会重新打开缺口——防回归
        var methods = typeof(LYBT.WebAPI.Controllers.ConfigurationController)
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

    private static void AssertSysAdminOnly(Type controllerType)
    {
        var authorize = controllerType.GetCustomAttribute<AuthorizeAttribute>();
        authorize.Should().NotBeNull($"{controllerType.Name} 应有类级授权策略");
        authorize!.Policy.Should().Be(PolicyConstants.SysAdminOnly,
            "配置管理属 sysadmin 专属运维操作——Admin 业务管理员不应访问系统配置（US-SHELL-018 角色: sysadmin）");
    }
}
