using FluentAssertions;
using System.Reflection;
using Xunit;

namespace LYBT.Tests.Server;

/// <summary>
/// API 单一职能守卫（P1-1/4/5/6 2026-08-14: 业务逻辑移出 Controller——防回归）：
/// - P1-1 ConfigurationController 不再有 TryAcquireRestartSlot（限频移入 Service）
/// - P1-4 DeployController 注入 IDeployService（上传逻辑移出）
/// - P1-6 DownloadController 注入 IDownloadService（ExtractVersion/BuildHtml/FormatSize 移出）
/// </summary>
public class ApiSingleResponsibilityGuardTests
{
    [Fact]
    public void ConfigurationController_NoRestartRateLimitLogic()
    {
        typeof(LYBT.WebAPI.Controllers.ConfigurationController)
            .GetMethod("TryAcquireRestartSlot", BindingFlags.NonPublic | BindingFlags.Static)
            .Should().BeNull("重启限频已移入 SystemConfigurationService.ScheduleRestartAsync（P1-1）");
    }

    [Fact]
    public void DeployController_Injects_DeployService()
    {
        var ctor = typeof(LYBT.WebAPI.Controllers.DeployController).GetConstructors().Single();
        var types = ctor.GetParameters().Select(p => p.ParameterType).ToList();
        types.Should().Contain(t => t.Name == "IDeployService",
            "DeployController 必须注入 IDeployService（上传逻辑移出——P1-4）");
        // 不再有 FileStream/目录创建逻辑（构造不直接依赖 IO——编译期通过 IDeployService 抽象）
        types.Should().NotContain(t => t == typeof(Microsoft.AspNetCore.Hosting.IWebHostEnvironment));
    }

    [Fact]
    public void DownloadController_Injects_DownloadService()
    {
        var ctor = typeof(LYBT.WebAPI.Controllers.DownloadController).GetConstructors().Single();
        var types = ctor.GetParameters().Select(p => p.ParameterType).ToList();
        types.Should().Contain(t => t.Name == "IDownloadService",
            "DownloadController 必须注入 IDownloadService（HTML 生成移出——P1-6）");
        types.Should().NotContain(t => t == typeof(Microsoft.Extensions.Configuration.IConfiguration),
            "DownloadController 不应再直接依赖 IConfiguration（文件扫描/版本提取已移入 Service）");
    }
}
