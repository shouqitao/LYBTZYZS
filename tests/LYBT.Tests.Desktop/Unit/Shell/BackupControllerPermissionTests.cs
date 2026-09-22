using System.Reflection;
using FluentAssertions;
using LYBT.Infrastructure.Constants;
using LYBT.Infrastructure.Web;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Xunit;

namespace LYBT.Tests.Desktop.Unit.Shell;

/// <summary>
/// 备份/恢复 Controller 权限与路由守卫（B-06 / US-SHELL-013）。
/// </summary>
/// <remarks>
/// <para>远程 <c>LYBT.WebAPI.Controllers.BackupController</c> 与本地 <c>LYBT.LocalWebAPI.Controllers.BackupController</c>
/// 均直接继承 <see cref="BaseBackupController"/>——路由/权限/契约唯一来源，双端不得漂移（ADR-0010/0023）。</para>
/// <para>反射语义对齐 P09（<c>GetCustomAttributes(true)</c> 取继承属性）：类级 <c>[Authorize]</c> 声明在共享基类上，
/// 派生控制器自身无授权标注——管理动作逐方法收紧 <c>SysAdminOnly</c>，<c>POST auto</c>（登录触发的
/// 自动备份，NFR-AVAIL-001）仅要求已认证。</para>
/// </remarks>
public class BackupControllerPermissionTests
{
    /// <summary>需逐方法收紧为 SysAdminOnly 的管理动作（与 BaseBackupController 动作一一对应）</summary>
    private static readonly string[] SysAdminOnlyActions =
        ["GetBackups", "GetStatus", "GetTables", "Create", "Restore", "Delete", "Cleanup"];

    /// <summary>全部动作面（管理动作 + 登录触发的 AutoBackup）</summary>
    private static readonly string[] AllActions = [.. SysAdminOnlyActions, "AutoBackup"];

    /// <summary>远程宿主路由（ApiVersion 版本段）</summary>
    private const string RemoteRouteTemplate = "api/v{version:apiVersion}/backup";

    /// <summary>本地宿主路由（固定 v1）</summary>
    private const string LocalRouteTemplate = "api/v1/backup";

    [Theory]
    [InlineData(typeof(LYBT.WebAPI.Controllers.BackupController))]
    [InlineData(typeof(LYBT.LocalWebAPI.Controllers.BackupController))]
    public void BackupController_InheritsSharedBase(Type controllerType)
    {
        controllerType.BaseType.Should().Be(typeof(BaseBackupController),
            $"{controllerType.FullName} 必须直接继承共享基类——路由/权限唯一来源，防双端漂移");
    }

    [Theory]
    [InlineData(typeof(LYBT.WebAPI.Controllers.BackupController))]
    [InlineData(typeof(LYBT.LocalWebAPI.Controllers.BackupController))]
    public void BackupController_ClassLevelAuthorize_RequiresAuthenticationOnly(Type controllerType)
    {
        // GetCustomAttributes(inherit: true)：类级 [Authorize] 继承自 BaseBackupController（P09 语义）
        var authorizes = controllerType
            .GetCustomAttributes(inherit: true)
            .OfType<AuthorizeAttribute>()
            .ToList();

        authorizes.Should().HaveCount(1,
            $"{controllerType.Name} 类级应有且仅有一个 [Authorize]（继承自 BaseBackupController）");
        authorizes[0].Policy.Should().BeNull(
            "类级 [Authorize] 仅要求已认证——POST auto 由任意登录角色发起（NFR-AVAIL-001）");
    }

    [Theory]
    [InlineData(typeof(LYBT.WebAPI.Controllers.BackupController))]
    [InlineData(typeof(LYBT.LocalWebAPI.Controllers.BackupController))]
    public void BackupController_ManagementActions_RequireSysAdminOnly(Type controllerType)
    {
        foreach (var actionName in SysAdminOnlyActions)
        {
            var method = controllerType.GetMethod(actionName);
            method.Should().NotBeNull($"{controllerType.Name} 应保留动作 {actionName}");

            var policies = method!
                .GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true)
                .Cast<AuthorizeAttribute>()
                .Select(a => a.Policy)
                .ToList();

            policies.Should().HaveCount(1,
                $"{controllerType.Name}.{actionName} 应有且仅有一个方法级 [Authorize] 收紧");
            policies[0].Should().Be(PolicyConstants.SysAdminOnly,
                $"{controllerType.Name}.{actionName} 属运维操作——仅 SuperAdmin 可执行（US-SHELL-013）");
        }
    }

    [Theory]
    [InlineData(typeof(LYBT.WebAPI.Controllers.BackupController))]
    [InlineData(typeof(LYBT.LocalWebAPI.Controllers.BackupController))]
    public void BackupController_AutoBackup_HasNoMethodLevelAuthorize(Type controllerType)
    {
        var method = controllerType.GetMethod("AutoBackup");
        method.Should().NotBeNull($"{controllerType.Name} 应保留动作 AutoBackup");

        method!
            .GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true)
            .OfType<AuthorizeAttribute>()
            .Should().BeEmpty(
                "POST auto 为登录触发的自动备份——不得方法级收紧，保持「已认证即可」（NFR-AVAIL-001）");
    }

    [Theory]
    [InlineData(typeof(LYBT.WebAPI.Controllers.BackupController))]
    [InlineData(typeof(LYBT.LocalWebAPI.Controllers.BackupController))]
    public void BackupController_ActionSurface_IsFullyClassified(Type controllerType)
    {
        // 防回归：动作面新增端点必须显式归入「管理动作（SysAdminOnly）」或「AutoBackup（仅认证）」二类之一，
        // 否则本守卫失败——避免新端点默认落到类级「仅已认证」而漏收紧。
        // 取 controllerType 的全部公开动作（含继承自共享基类的动作 + 派生控制器自身新增动作）
        var declaredActions = controllerType
            .GetMethods(BindingFlags.Instance | BindingFlags.Public)
            .Where(m => m.GetCustomAttributes(inherit: true).OfType<HttpMethodAttribute>().Any())
            .Select(m => m.Name)
            .OrderBy(n => n, StringComparer.Ordinal)
            .ToList();

        declaredActions.Should().Equal(
            AllActions.OrderBy(n => n, StringComparer.Ordinal),
            $"{controllerType.Name} 的备份动作面变更必须同步本守卫（新增端点需明确权限归类）");
    }

    [Fact]
    public void BackupControllers_RoutePrefix_IdenticalModuloVersionSegment()
    {
        GetDeclaredRoute(typeof(LYBT.WebAPI.Controllers.BackupController))
            .Should().Be(RemoteRouteTemplate, "远程宿主使用 ApiVersion 版本段路由（P09b）");
        GetDeclaredRoute(typeof(LYBT.LocalWebAPI.Controllers.BackupController))
            .Should().Be(LocalRouteTemplate, "本地宿主为固定 v1 路由（P09b）");

        NormalizeVersionSegment(RemoteRouteTemplate)
            .Should().Be(NormalizeVersionSegment(LocalRouteTemplate),
                "双端备份路由面必须一致，否则客户端本地/远程模式切换时 404");
    }

    private static string? GetDeclaredRoute(Type controllerType)
        => controllerType
            .GetCustomAttributes(typeof(RouteAttribute), inherit: false)
            .Cast<RouteAttribute>()
            .SingleOrDefault()
            ?.Template;

    private static string NormalizeVersionSegment(string route)
        => route.Replace("v{version:apiVersion}", "v1", StringComparison.Ordinal);
}
