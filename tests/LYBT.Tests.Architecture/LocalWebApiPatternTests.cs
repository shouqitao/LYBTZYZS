using NetArchTest.Rules;
using Xunit;

namespace LYBT.Tests.Architecture;

/// <summary>
/// Architecture guard tests for the LocalWebAPI pattern.
/// LocalWebAPI now uses the same Service layer as Remote WebAPI (unified architecture).
/// </summary>
public class LocalWebApiPatternTests
{
    private const string LocalWebApiNamespace = "LYBT.LocalWebAPI.Controllers";

    /// <summary>
    /// P20: LocalWebAPI controllers may depend on Service interfaces, entities, and framework types.
    /// Updated for unified Service layer — controllers inject I*Service instead of DbContext.
    /// </summary>
    [Fact]
    public void P20_LocalWebAPI_Controllers_Only_Inject_Allowed_Types()
    {
        var result = Types.InAssembly(typeof(LYBT.LocalWebAPI.Controllers.HealthController).Assembly)
            .That()
            .ResideInNamespace(LocalWebApiNamespace)
            .Should()
            .OnlyHaveDependenciesOn(
                "LYBT.LocalWebAPI",
                "LYBT.Entities",
                "LYBT.Infrastructure",
                "LYBT.Module",
                "LYBT.Shared",
                "LYBT.SharedKernel",
                "FluentValidation",
                "MediatR",
                "Microsoft.AspNetCore",
                "Microsoft.EntityFrameworkCore",
                "Microsoft.Extensions",
                "Microsoft.IdentityModel",
                "Serilog",
                "System",
                "System.Threading",
                "System.Linq",
                "System.Collections",
                "System.Reflection",
                "System.Runtime",
                "System.Security"
            )
            .GetResult();

        Assert.True(result.IsSuccessful,
            $"LocalWebAPI controllers have unapproved dependencies: {string.Join(", ", result.FailingTypes?.Select(t => t.Name) ?? [])}");
    }

    /// <summary>
    /// P21: 例外审计 — LocalWebAPI 引用 Server Modules 是有意设计（ADR-0010）。
    /// 本测试验证引用列表与 ADR-0010 记录一致。新增 Server Module 引用需先更新 ADR。
    /// </summary>
    [Fact]
    public void P21_LocalWebAPI_References_Match_ADR0010()
    {
        var localWebApiAssembly = typeof(LYBT.LocalWebAPI.Controllers.HealthController).Assembly;
        var referencedServerAssemblies = localWebApiAssembly.GetReferencedAssemblies()
            .Where(a => a.Name!.StartsWith("LYBT.Module.") || a.Name == "LYBT.Entities" || a.Name == "LYBT.Infrastructure")
            .Select(a => a.Name!)
            .OrderBy(n => n)
            .ToList();

        // ADR-0010 记录的允许引用列表
        var expectedReferences = new[]
        {
            "LYBT.Entities",
            "LYBT.Infrastructure",
            "LYBT.Module.Catalog",
            "LYBT.Module.Identity",
            "LYBT.Module.MedicalCases",
            "LYBT.Module.Patients",
            "LYBT.Module.Registrations",
            "LYBT.Module.Reports"
        }.OrderBy(n => n).ToList();

        Assert.Equal(expectedReferences, referencedServerAssemblies);
    }

    /// <summary>
    /// P1-29: 双端授权策略一致性——RegistrationsController 等同名控制器在 Local 与 Remote 的 Authorize Policy 必须一致。
    /// 对齐 SSOT docs/02-requirements/04-permissions.md：Create=DoctorOrReceptionist、StartVisit=DoctorOnly、Cancel=ReceptionistOnly。
    /// </summary>
    [Fact]
    public void Should_Have_Same_Auth_Policy_As_Remote()
    {
        // 仅校验 Registrations 核心三方法（Create/StartVisit/Cancel）的 Policy 一致性，避免全量反射受基类干扰
        var localType = typeof(LYBT.LocalWebAPI.Controllers.RegistrationsController);
        var remoteType = typeof(LYBT.WebAPI.Controllers.RegistrationsController);

        foreach (var methodName in new[] { "Create", "StartVisit", "Cancel" })
        {
            var localMethod = localType.GetMethod(methodName);
            var remoteMethod = remoteType.GetMethod(methodName);
            Assert.NotNull(localMethod);
            Assert.NotNull(remoteMethod);
            var localPolicy = GetPolicy(localMethod);
            var remotePolicy = GetPolicy(remoteMethod);
            Assert.Equal(remotePolicy, localPolicy);
        }

        static string? GetPolicy(System.Reflection.MethodInfo m)
        {
            var attr = m.GetCustomAttributes(typeof(Microsoft.AspNetCore.Authorization.AuthorizeAttribute), true)
                .Cast<Microsoft.AspNetCore.Authorization.AuthorizeAttribute>().FirstOrDefault();
            return attr?.Policy;
        }
    }

    /// <summary>
    /// P22: All LocalWebAPI controllers must be decorated with [ApiController].
    /// This ensures consistent ASP.NET Core behavior: automatic model validation,
    /// binding source inference, and problem details responses.
    /// </summary>
    [Fact]
    public void P22_LocalWebAPI_Controllers_Must_Have_ApiController()
    {
        var controllerTypes = Types.InAssembly(typeof(LYBT.LocalWebAPI.Controllers.HealthController).Assembly)
            .That()
            .ResideInNamespace(LocalWebApiNamespace)
            .And()
            .Inherit(typeof(Microsoft.AspNetCore.Mvc.ControllerBase))
            .GetTypes();

        foreach (var type in controllerTypes)
        {
            var hasAttribute = type.GetCustomAttributes(typeof(Microsoft.AspNetCore.Mvc.ApiControllerAttribute), true).Any();
            Assert.True(hasAttribute, $"{type.Name} must have [ApiController] attribute");
        }
    }
}
