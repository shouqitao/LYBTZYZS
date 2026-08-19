using System.Reflection;
using FluentAssertions;
using LYBT.Infrastructure.Constants;
using Microsoft.AspNetCore.Authorization;
using Xunit;

namespace LYBT.Tests.Server;

/// <summary>
/// Catalog 权限收紧守卫（I-5 / F-L4-01：药材/验方 batch-import、import-template、export（及 export-all）必须 AdminOrSuperAdmin）
/// Remote 端：LYBT.WebAPI.Controllers.CatalogController
/// </summary>
public class CatalogPermissionTests
{
    private static readonly string[] HerbBatchImportLike = new[]
    {
        "HerbImportTemplate", // GET /herbs/import-template
        "HerbExport",         // GET /herbs/export
        "HerbExportAll",      // GET /herbs/export-all
        "BatchImport"         // POST /herbs/batch-import
    };

    private static readonly string[] FormulaBatchImportLike = new[]
    {
        "FormulaImportTemplate", // GET /formulas/import-template
        "FormulaExport",         // GET /formulas/export
        "ImportFormulas"         // POST /formulas/batch-import
    };

    [Fact]
    public void RemoteHerbImportExportEndpoints_RequireAdminOrSuperAdmin()
    {
        var type = typeof(LYBT.WebAPI.Controllers.CatalogController);
        foreach (var methodName in HerbBatchImportLike)
        {
            var method = type.GetMethod(methodName, BindingFlags.Public | BindingFlags.Instance);
            method.Should().NotBeNull($"{methodName} 应存在");
            var auth = method!.GetCustomAttribute<AuthorizeAttribute>();
            auth.Should().NotBeNull($"{methodName} 应显式标注 AdminOrSuperAdmin（类级 DoctorOrAdmin 需收紧）");
            auth!.Policy.Should().Be(PolicyConstants.AdminOrSuperAdmin,
                $"{methodName} 必须 AdminOrSuperAdmin（对齐患者 batch-import）");
        }
    }

    [Fact]
    public void RemoteFormulaImportExportEndpoints_RequireAdminOrSuperAdmin()
    {
        var type = typeof(LYBT.WebAPI.Controllers.CatalogController);
        foreach (var methodName in FormulaBatchImportLike)
        {
            var method = type.GetMethod(methodName, BindingFlags.Public | BindingFlags.Instance);
            method.Should().NotBeNull($"{methodName} 应存在");
            var auth = method!.GetCustomAttribute<AuthorizeAttribute>();
            auth.Should().NotBeNull($"{methodName} 应显式标注 AdminOrSuperAdmin");
            auth!.Policy.Should().Be(PolicyConstants.AdminOrSuperAdmin,
                $"{methodName} 必须 AdminOrSuperAdmin");
        }
    }

    [Fact]
    public void RemoteCatalogController_ClassLevel_IsDoctorOrAdmin_ButMethodLevelOverrides()
    {
        var type = typeof(LYBT.WebAPI.Controllers.CatalogController);
        var classAuth = type.GetCustomAttribute<AuthorizeAttribute>();
        classAuth.Should().NotBeNull("类级应有 DoctorOrAdmin（只读列表/详情仍 Doctor 可查）");
        classAuth!.Policy.Should().Be(PolicyConstants.DoctorOrAdmin);
    }
}
