using System.Reflection;
using FluentAssertions;
using LYBT.Infrastructure.Constants;
using Microsoft.AspNetCore.Authorization;
using Xunit;

namespace LYBT.Tests.Server;

/// <summary>
/// Catalog 权限收紧守卫（I-5 / F-L4-01：药材/验方 batch-import、import-template、export 必须 AdminOrSuperAdmin）
/// Remote 端：P1-24 拆分后 HerbsController（药材）/ FormulasController（验方）
/// P2-12: export-all 已合并入 export
/// </summary>
public class CatalogPermissionTests
{
    private static readonly string[] HerbBatchImportLike = new[]
    {
        "HerbImportTemplate", // GET /herbs/import-template
        "HerbExport",         // GET /herbs/export（P2-12 合并 export-all）
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
        var type = typeof(LYBT.WebAPI.Controllers.HerbsController);
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
        var type = typeof(LYBT.WebAPI.Controllers.FormulasController);
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

[Theory]
    [InlineData(typeof(LYBT.WebAPI.Controllers.HerbsController))]
    [InlineData(typeof(LYBT.WebAPI.Controllers.FormulasController))]
    public void RemoteCatalogController_ClassLevel_IsDoctorOrAdmin_ButMethodLevelOverrides(Type type)
    {
        var classAuth = type.GetCustomAttribute<AuthorizeAttribute>();
        classAuth.Should().NotBeNull($"{type.Name} 类级应有 DoctorOrAdmin（只读列表/详情仍 Doctor 可查）");
        classAuth!.Policy.Should().Be(PolicyConstants.DoctorOrAdmin);
    }
}
