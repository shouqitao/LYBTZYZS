using System.Reflection;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Refit;
using Xunit;

namespace LYBT.Tests.Desktop.Unit.Shell;

/// <summary>
/// 导入/导出 契约-端点路由对齐守卫（P1 修复，desktop-deep-review-2026-08-19 发现）：
/// Desktop Refit 契约此前调 `GET /api/v1/herbs/export` 而 Remote 服务端只有 `/export-all`、
/// LocalWebAPI 完全缺失药材 export/import-template 端点 → 双模式 404（测试盲区）。
/// 本守卫：客户端 Refit 契约的导入/导出路径必须同时存在于 Remote（LYBT.WebAPI）与 Local（LYBT.LocalWebAPI）
/// 两端路由表；且服务器端导入/导出路由面双端一致（防「Remote 有、Local 缺」类回归）。
/// 纯反射，无需运行中的 WebAPI。
/// </summary>
public class ImportExportRouteParityTests
{
    private static readonly Type[] ClientContracts =
    {
        typeof(LYBT.Desktop.Contracts.Api.IHerbApi),
        typeof(LYBT.Desktop.Contracts.Api.IPatientApi),
        typeof(LYBT.Desktop.Contracts.Api.IFormulaApi),
    };

    private static readonly Type[] RemoteControllers =
    {
        typeof(LYBT.WebAPI.Controllers.CatalogController),
        typeof(LYBT.WebAPI.Controllers.PatientsController),
    };

    private static readonly Type[] LocalControllers =
    {
        typeof(LYBT.LocalWebAPI.Controllers.CatalogController),
        typeof(LYBT.LocalWebAPI.Controllers.PatientsController),
    };

    [Fact]
    public void ImportExportClientPaths_ExistOnBothRemoteAndLocal()
    {
        var clientPaths = GetClientImportExportPaths();
        clientPaths.Should().NotBeEmpty("客户端契约应包含批量导入/导出端点路径");

        var remoteRoutes = BuildRoutes(RemoteControllers);
        var localRoutes = BuildRoutes(LocalControllers);

        foreach (var path in clientPaths)
        {
            remoteRoutes.Should().Contain(path, $"Remote 服务端缺少客户端契约路径 {path}");
            localRoutes.Should().Contain(path, $"LocalWebAPI 缺少客户端契约路径 {path}");
        }
    }

    [Fact]
    public void ImportExportServerRouteSurface_IsConsistentAcrossModes()
    {
        // 双端一致：Remote 有解析的导入/导出路由，Local 也必须具备（防「Remote 有、Local 缺」回归）
        var remoteImportExport = BuildRoutes(RemoteControllers).Where(IsImportExportRoute).ToList();
        var localRoutes = BuildRoutes(LocalControllers);

        remoteImportExport.Should().NotBeEmpty("远程服务端应包含导入/导出路由");
        foreach (var route in remoteImportExport)
        {
            localRoutes.Should().Contain(route, $"LocalWebAPI 缺少与 Remote 一致的导入/导出路由 {route}");
        }
    }

    private static List<string> GetClientImportExportPaths()
    {
        var paths = new List<string>();
        foreach (var api in ClientContracts)
        {
            foreach (var method in api.GetMethods(BindingFlags.Public | BindingFlags.Instance))
            {
                var template = GetRefitTemplate(method);
                if (template == null || !IsImportExportRoute(template))
                    continue;
                paths.Add(Normalize(template, api.Name.ToLowerInvariant()));
            }
        }
        return paths.Distinct().OrderBy(p => p).ToList();
    }

    private static string? GetRefitTemplate(MethodInfo method)
    {
        var get = method.GetCustomAttribute<GetAttribute>();
        if (get != null) return get.Path;
        var post = method.GetCustomAttribute<PostAttribute>();
        return post?.Path;
    }

    private static HashSet<string> BuildRoutes(IEnumerable<Type> controllers)
    {
        var routes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var controller in controllers)
        {
            var classRoute = controller.GetCustomAttribute<RouteAttribute>()?.Template ?? "";
            foreach (var method in controller.GetMethods(BindingFlags.Public | BindingFlags.Instance))
            {
                var httpGet = method.GetCustomAttribute<HttpGetAttribute>();
                var httpPost = method.GetCustomAttribute<HttpPostAttribute>();
                var actionRoute = httpGet?.Template ?? httpPost?.Template;
                if (actionRoute == null) continue;

                var combined = actionRoute.StartsWith("/")
                    ? actionRoute
                    : $"{classRoute}/{actionRoute}";
                routes.Add(Normalize(combined, controller.Name.Replace("Controller", "").ToLowerInvariant()));
            }
        }
        return routes;
    }

    private static bool IsImportExportRoute(string normalizedRoute) =>
        normalizedRoute.Contains("/batch-import")
        || normalizedRoute.Contains("/import-template")
        || normalizedRoute.EndsWith("/export");

    private static string Normalize(string route, string controllerName)
    {
        var r = route.Trim().ToLowerInvariant();
        r = r.Replace("{version:apiversion}", "1");
        r = r.Replace("{version}", "1");
        r = r.Replace("[controller]", controllerName);
        r = r.TrimStart('/');
        return r;
    }
}