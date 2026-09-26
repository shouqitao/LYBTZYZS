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
/// <para>
/// US-SHELL-021 扩展：路由存在还不够——**请求体 DTO 绑定**也必须一致。
/// 历史缺陷：Local `POST /formulas/batch-import` 绑定 `List&lt;FormulaImportItemDto&gt;`，
/// 而 Remote 与 Desktop `IFormulaApi.BatchImportAsync`（Refit [Body]）均发送 `FormulaBatchImportInputDto` 信封
/// → 直连原始 HTTP 的客户端 400。本守卫按路由比对双端 <c>[FromBody]</c> 参数类型，
/// 并要求其与客户端 Refit <c>[Body]</c> 类型一致，且客户端期望的响应 DTO 与 Remote 声明的 200 响应类型一致。
/// </para>
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
        typeof(LYBT.WebAPI.Controllers.HerbsController),
        typeof(LYBT.WebAPI.Controllers.FormulasController),
        typeof(LYBT.WebAPI.Controllers.PatientsController),
    };

    private static readonly Type[] LocalControllers =
    {
        typeof(LYBT.LocalWebAPI.Controllers.HerbsController),
        typeof(LYBT.LocalWebAPI.Controllers.FormulasController),
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

    /// <summary>
    /// US-SHELL-021：导入/导出端点的请求体 DTO 绑定双端一致，且与客户端 Refit [Body] 一致；
    /// 客户端期望的 200 响应 DTO 与 Remote 声明一致（防「路由在、绑定错」类回归）。
    /// </summary>
    [Fact]
    public void ImportExportBodyBindings_AreIdenticalAcrossModes_AndMatchClientContracts()
    {
        var remoteBindings = BuildImportExportBodyBindings(RemoteControllers);
        var localBindings = BuildImportExportBodyBindings(LocalControllers);

        remoteBindings.Should().NotBeEmpty("远程服务端导入/导出端点应声明请求体绑定");

        // 双端请求体 DTO 必须逐路由一致
        foreach (var (route, remoteType) in remoteBindings)
        {
            localBindings.Should().ContainKey(route, $"LocalWebAPI 缺少 {route} 的请求体绑定（或端点缺失）");
            localBindings[route].Should().Be(remoteType, $"{route} 的 [FromBody] DTO 双端必须一致");
        }

        // 客户端 Refit [Body] DTO 必须与双端 [FromBody] 一致
        var clientBindings = BuildClientBodyBindings();
        clientBindings.Should().NotBeEmpty("客户端契约应包含导入/导出端点的请求体");

        foreach (var (route, clientType) in clientBindings)
        {
            remoteBindings.Should().ContainKey(route, $"Remote 缺少 {route} 的请求体绑定");
            remoteBindings[route].Should().Be(clientType, $"Remote {route} 的 [FromBody] 必须与客户端 Refit Body DTO 一致");
            localBindings[route].Should().Be(clientType, $"Local {route} 的 [FromBody] 必须与客户端 Refit Body DTO 一致");
        }

        // 客户端期望的 200 响应 DTO 必须与 Remote 声明的 200 响应类型一致
        foreach (var (route, method) in EnumerateClientMethods().Where(x => IsImportExportRoute(x.Route)))
        {
            var expected = GetApiResponseDto(method.ReturnType);
            if (expected == null)
                continue;

            var remoteAction = EnumerateHttpActions(RemoteControllers)
                .First(x => string.Equals(x.Route, route, StringComparison.OrdinalIgnoreCase))
                .Method;
            var declared = remoteAction
                .GetCustomAttributes<ProducesResponseTypeAttribute>()
                .Where(a => a.StatusCode == 200 && a.Type != null)
                .Select(a => GetApiResponseDto(a.Type!))
                .FirstOrDefault(t => t != null);

            declared.Should().Be(expected, $"Remote {route} 声明的 200 响应 DTO 必须与客户端期望一致");
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
        foreach (var action in EnumerateHttpActions(controllers))
            routes.Add(action.Route);
        return routes;
    }

    /// <summary>规范化路由 → 导入/导出端点的 [FromBody] 参数类型（无请求体的端点不入表）。</summary>
    private static Dictionary<string, Type> BuildImportExportBodyBindings(IEnumerable<Type> controllers)
    {
        var bindings = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase);
        foreach (var (route, method, _) in EnumerateHttpActions(controllers))
        {
            if (!IsImportExportRoute(route))
                continue;

            var body = method.GetParameters()
                .FirstOrDefault(p => p.GetCustomAttribute<FromBodyAttribute>() != null);
            if (body != null)
                bindings[route] = body.ParameterType;
        }
        return bindings;
    }

    /// <summary>规范化路由 → 客户端 Refit [Body] 参数类型（无请求体的端点不入表）。</summary>
    private static Dictionary<string, Type> BuildClientBodyBindings()
    {
        var bindings = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase);
        foreach (var (route, method) in EnumerateClientMethods().Where(x => IsImportExportRoute(x.Route)))
        {
            var body = method.GetParameters()
                .FirstOrDefault(p => p.GetCustomAttribute<BodyAttribute>() != null);
            if (body != null)
                bindings[route] = body.ParameterType;
        }
        return bindings;
    }

    private static IEnumerable<(string Route, MethodInfo Method)> EnumerateClientMethods()
    {
        foreach (var api in ClientContracts)
        {
            foreach (var method in api.GetMethods(BindingFlags.Public | BindingFlags.Instance))
            {
                var template = GetRefitTemplate(method);
                if (template == null)
                    continue;
                yield return (Normalize(template, api.Name.ToLowerInvariant()), method);
            }
        }
    }

    private static IEnumerable<(string Route, MethodInfo Method, Type Controller)> EnumerateHttpActions(
        IEnumerable<Type> controllers)
    {
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
                yield return (
                    Normalize(combined, controller.Name.Replace("Controller", "").ToLowerInvariant()),
                    method,
                    controller);
            }
        }
    }

    /// <summary>取 <c>Task&lt;ApiResponse&lt;T&gt;&gt;</c> / <c>ApiResponse&lt;T&gt;</c> 的 T；非该形状返回 null。</summary>
    private static Type? GetApiResponseDto(Type type)
    {
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Task<>))
            type = type.GetGenericArguments()[0];

        // 全限定名：避免与 Refit.ApiResponse&lt;T&gt; 混淆（Refit 命名空间已在本文件导入）
        if (!type.IsGenericType
            || type.GetGenericTypeDefinition() != typeof(LYBT.Shared.Models.Contracts.Common.ApiResponse<>))
            return null;

        return type.GetGenericArguments()[0];
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
