using Asp.Versioning;
using Asp.Versioning.ApiExplorer;
using FluentAssertions;
using LYBT.Infrastructure.Constants;
using LYBT.WebAPI.Configuration;
using LYBT.WebAPI.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Globalization;
using System.Reflection;
using Xunit;

namespace LYBT.Tests.Server;

/// <summary>
/// F-07 API 版本化守卫（ADR-0015: URL Path Versioning）——防「未来 v2 切换」静默破坏：
/// <list type="bullet">
/// <item>路由只用 URL 段版本（<c>api/v{version:apiVersion}/…</c>），不引入 header/query/媒体类型版本</item>
/// <item>控制器声明的版本必须来自 <see cref="ApiVersionConstants"/>（禁止硬编码 "2.0" 之类子版本）</item>
/// <item>DI 装配的版本读取器只有 <see cref="UrlSegmentApiVersionReader"/>，且 ReportApiVersions 开启</item>
/// <item>Swagger 文档集合 = 被发现的版本集合（v2 切换后自动多一份文档，无需改注册代码）</item>
/// </list>
/// </summary>
public class ApiVersioningGuardTests
{
    /// <summary>非版本化路由的白名单——下载主页/安装包不是 REST API 契约（ADR-0015 只约束 /api/**）</summary>
    private static readonly string[] NonVersionedRouteAllowList = ["LYBT.WebAPI.Controllers.DownloadController"];

    /// <summary>承载控制器的程序集（WebAPI + 各模块——模块只放抽象基类，此处防未来在模块内加具体控制器）</summary>
    private static readonly Assembly[] ServerAssemblies =
    [
        typeof(LYBT.WebAPI.Controllers.HealthController).Assembly,
        typeof(LYBT.Module.Identity.Controllers.BaseUsersController).Assembly,
        typeof(LYBT.Module.MedicalCases.Controllers.BaseMedicalCasesController).Assembly,
        typeof(LYBT.Module.Registrations.Controllers.BaseRegistrationsController).Assembly
    ];

    [Fact]
    public void EveryControllerRoute_UsesUrlSegmentVersion()
    {
        var controllers = FindControllers().ToList();
        controllers.Should().NotBeEmpty("守卫必须真的扫到控制器，否则形同虚设");

        foreach (var controller in controllers)
        {
            var routes = controller.GetCustomAttributes<RouteAttribute>(inherit: false).Select(a => a.Template).ToList();
            routes.Should().NotBeEmpty($"{controller.Name} 必须显式声明路由");

            foreach (var route in routes)
            {
                if (NonVersionedRouteAllowList.Contains(controller.FullName!))
                    continue;

                // ADR-0015: 版本只出现在 URL 段，且用 Asp.Versioning 的 apiVersion 约束（客户端裸 /api/x 不可路由）
                route.Should().StartWith("api/v{version:apiVersion}/",
                    $"{controller.Name} 的版本化路由必须是 api/v{{version:apiVersion}}/…（URL Path Versioning）");
                route.Split("{version:apiVersion}").Length.Should().Be(2,
                    $"{controller.Name} 的路由 {route} 应恰好包含一个版本占位符");
            }
        }
    }

    [Fact]
    public void DeclaredApiVersions_ComeFromApiVersionConstants()
    {
        var allowed = ApiVersionConstants_Values().Select(ToApiVersion).ToHashSet();

        var declared = new List<ApiVersion>();
        foreach (var controller in FindControllers())
        {
            var attributes = controller.GetCustomAttributes<ApiVersionAttribute>(inherit: false)
                .Concat(controller.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                    .SelectMany(m => m.GetCustomAttributes<ApiVersionAttribute>(inherit: false)));

            foreach (var attribute in attributes)
            {
                foreach (var version in attribute.Versions)
                {
                    declared.Add(version);
                }
            }
        }

        declared.Should().NotBeEmpty("至少有一个控制器声明了版本");
        declared.Should().OnlyContain(v => allowed.Contains(v),
            "控制器声明的版本必须来自 ApiVersionConstants（ADR-0015: 整数递增，禁止子版本）");
        declared.Should().Contain(new ApiVersion(1, 0), "v1 是当前对外版本，必须仍被声明");

        // V2 为保留值：切换 v2 时必须同步更新本守卫与 ADR-0015 切换清单（有意触发，不是回归）
        declared.Should().NotContain(new ApiVersion(2, 0),
            "V2 尚未启用（ApiVersionConstants.V2 为保留值）——切 v2 前不应有控制器声明它");
    }

    [Fact]
    public void ApiVersionConstants_ArePlainIntegers()
    {
        var values = ApiVersionConstants_Values();

        values.Should().Contain(ApiVersionConstants.V1);
        foreach (var value in values)
        {
            // ADR-0015: 版本号格式为整数递增（v1 → v2），无 v1.1 等子版本
            value.Should().MatchRegex("^[1-9][0-9]*$");
            ToApiVersion(value).MajorVersion.Should().Be(int.Parse(value, CultureInfo.InvariantCulture));
        }
    }

    [Fact]
    public void ApiVersioningOptions_UrlSegmentReaderOnly_AndReportsVersions()
    {
        using var provider = BuildContainer();

        var options = provider.GetRequiredService<IOptions<ApiVersioningOptions>>().Value;

        options.ApiVersionReader.Should().BeOfType<UrlSegmentApiVersionReader>(
            "ADR-0015 只提供 URL Path 版本；header/query/媒体类型读取器会改变未指定版本请求的解析结果");
        options.ReportApiVersions.Should().BeTrue();
        options.AssumeDefaultVersionWhenUnspecified.Should().BeTrue();
        options.DefaultApiVersion.Should().Be(new ApiVersion(1, 0));
    }

    [Fact]
    public void SwaggerDocuments_MatchDiscoveredApiVersions()
    {
        using var provider = BuildContainer();

        var descriptions = provider.GetRequiredService<IApiVersionDescriptionProvider>().ApiVersionDescriptions;
        descriptions.Should().NotBeEmpty();
        descriptions.Select(d => d.GroupName).Should().Contain("v1",
            "GroupNameFormat 'v'VVV 决定文档名与 URL 段（/swagger/v1/swagger.json）");

        var swaggerDocs = provider.GetRequiredService<IOptions<SwaggerGenOptions>>().Value
            .SwaggerGeneratorOptions.SwaggerDocs;

        swaggerDocs.Keys.Should().BeEquivalentTo(descriptions.Select(d => d.GroupName),
            "每个被发现的版本必须恰好产出一份 Swagger 文档（v2 切换后自动 +1，无需改 Swagger 注册代码）");
    }

    [Fact]
    public void ConfigureSwaggerOptions_ProducesOneDocumentPerDiscoveredVersion()
    {
        var configuration = BuildConfiguration();
        var versionProvider = new FakeApiVersionDescriptionProvider(
            new ApiVersionDescription(new ApiVersion(1, 0), "v1"),
            new ApiVersionDescription(new ApiVersion(2, 0), "v2"));

        var options = new SwaggerGenOptions();
        new ConfigureSwaggerOptions(versionProvider, configuration).Configure(options);

        options.SwaggerGeneratorOptions.SwaggerDocs.Keys.Should().Equal("v1", "v2");
        options.SwaggerGeneratorOptions.SwaggerDocs["v1"].Title.Should().Be("凌隐宝堂中医诊所 API");
        options.SwaggerGeneratorOptions.SwaggerDocs["v1"].Version.Should().Be("v1");
    }

    #region 辅助

    private static IEnumerable<Type> FindControllers() =>
        ServerAssemblies
            .SelectMany(a => a.GetTypes())
            .Where(t => t is { IsClass: true, IsAbstract: false })
            .Where(t => t.GetCustomAttributes<ApiControllerAttribute>(inherit: false).Any());

    /// <summary>ApiVersionConstants 的全部常量值（反射取 const——新增 vN 无需改本守卫）</summary>
    private static IReadOnlyList<string> ApiVersionConstants_Values() =>
        typeof(ApiVersionConstants)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(f => f is { IsLiteral: true } && f.FieldType == typeof(string))
            .Select(f => (string)f.GetRawConstantValue()!)
            .ToList();

    /// <summary>常量值 → ApiVersion（常量是纯整数，如 "1"）</summary>
    private static ApiVersion ToApiVersion(string value) =>
        new(int.Parse(value, CultureInfo.InvariantCulture));

    private static IConfiguration BuildConfiguration() =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Swagger:Title"] = "凌隐宝堂中医诊所 API",
                ["Swagger:EnableXmlComments"] = "false"
            })
            .Build();

    /// <summary>按生产装配方式（RegisterApiServices）构建容器，验证版本化与 Swagger 的实际配置</summary>
    private static ServiceProvider BuildContainer()
    {
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.SetMinimumLevel(LogLevel.Warning));
        var configuration = BuildConfiguration();
        // 生产宿主由 HostBuilder 注册 IConfiguration；此处补上（ConfigureSwaggerOptions 依赖它读取 SwaggerOptions）
        services.AddSingleton<IConfiguration>(configuration);
        services.RegisterApiServices(configuration);
        return services.BuildServiceProvider();
    }

    /// <summary>手写版本描述提供器（零 mock——仅替换「发现到的版本」这一输入）</summary>
    private sealed class FakeApiVersionDescriptionProvider : IApiVersionDescriptionProvider
    {
        public FakeApiVersionDescriptionProvider(params ApiVersionDescription[] descriptions) =>
            ApiVersionDescriptions = descriptions;

        public IReadOnlyList<ApiVersionDescription> ApiVersionDescriptions { get; }
    }

    #endregion
}
