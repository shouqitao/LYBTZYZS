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
    public void P20_LocalWebAPI_Controllers_Should_Only_Inject_Allowed_Types()
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
                "Microsoft.AspNetCore",
                "Microsoft.EntityFrameworkCore",
                "Microsoft.Extensions",
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
            "LocalWebAPI controllers may depend on Service interfaces, entities, and framework types");
    }

    /// <summary>
    /// P21: 例外审计 — LocalWebAPI 引用 Server Modules 是有意设计（ADR-0010）。
    /// 本测试验证引用列表与 ADR-0010 记录一致。新增 Server Module 引用需先更新 ADR。
    /// </summary>
    [Fact]
    public void P21_LocalWebAPI_ServerModule_References_Match_ADR0010()
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
            "LYBT.Module.Auth",
            "LYBT.Module.Formulas",
            "LYBT.Module.Herbs",
            "LYBT.Module.MedicalCases",
            "LYBT.Module.Patients",
            "LYBT.Module.Registration",
            "LYBT.Module.Reports",
            "LYBT.Module.Users"
        }.OrderBy(n => n).ToList();

        Assert.Equal(expectedReferences, referencedServerAssemblies);
    }

    /// <summary>
    /// P22: All LocalWebAPI controllers must be decorated with [ApiController].
    /// This ensures consistent ASP.NET Core behavior: automatic model validation,
    /// binding source inference, and problem details responses.
    /// </summary>
    [Fact]
    public void P22_LocalWebAPI_Controllers_Must_Have_ApiController_Attribute()
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

    /// <summary>
    /// P23: All Desktop repository interfaces must have HttpRepository implementations.
    /// This ensures 100% coverage for LocalWebAPI mode.
    /// </summary>
    [Fact]
    public void P23_All_Repository_Interfaces_Must_Have_HttpRepository_Implementations()
    {
        var repositoryAssembly = typeof(LYBT.Desktop.Contracts.Repositories.IPatientRepository).Assembly;
        var httpRepoAssembly = typeof(LYBT.LocalWebAPI.Repositories.HttpPatientRepository).Assembly;

        var repoInterfaces = repositoryAssembly.GetTypes()
            .Where(t => t.IsInterface && t.Namespace == "LYBT.Desktop.Contracts.Repositories")
            .ToList();

        var httpRepoTypes = httpRepoAssembly.GetTypes()
            .Where(t => t.IsClass && t.Namespace == "LYBT.LocalWebAPI.Repositories" && t.Name.StartsWith("Http"))
            .ToList();

        foreach (var repoInterface in repoInterfaces)
        {
            var expectedName = repoInterface.Name.Replace("I", "").Replace("Repository", "");
            var hasImplementation = httpRepoTypes.Any(t => t.Name.Contains(expectedName));
            Assert.True(hasImplementation, 
                $"Repository interface {repoInterface.Name} must have HttpRepository implementation");
        }
    }
}
