using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace LYBT.Tests.Server;

/// <summary>
/// Kestrel 双端点配置守卫（P2-09 US-SHELL-025 2026-08-14: Http 5000 默认开 + Https 5001 默认关——\n/// 配置开关读取语义验证）
/// </summary>
public class KestrelEndpointConfigTests
{
    [Fact]
    public void DefaultConfig_HttpEnabled_HttpsDisabled()
    {
        // Production 模板默认值（无显式开关时）
        var config = BuildConfig(new Dictionary<string, string?>
        {
            ["Server:Endpoints:Http:Url"] = "http://0.0.0.0:5000",
            ["Server:Endpoints:Https:Url"] = "https://0.0.0.0:5001"
        });

        config.GetSection("Server:Endpoints").GetValue<bool>("Http:Enabled", true).Should().BeTrue("Http 默认开启");
        config.GetSection("Server:Endpoints").GetValue<bool>("Https:Enabled", false).Should().BeFalse("Https 默认关闭");
    }

    [Fact]
    public void HttpsEnabled_WithCert_ReadsCertPath()
    {
        var config = BuildConfig(new Dictionary<string, string?>
        {
            ["Server:Endpoints:Https:Enabled"] = "true",
            ["Server:Endpoints:Https:Certificate:Path"] = "/etc/ssl/lybt.pfx",
            ["Server:Endpoints:Https:Certificate:Password"] = "secret"
        });

        config.GetSection("Server:Endpoints").GetValue<bool>("Https:Enabled", false).Should().BeTrue();
        config["Server:Endpoints:Https:Certificate:Path"].Should().Be("/etc/ssl/lybt.pfx");
        config["Server:Endpoints:Https:Certificate:Password"].Should().Be("secret");
    }

    [Fact]
    public void ProductionTemplate_ContainsHttpsEndpoint()
    {
        // 内嵌 Production 模板必须含 Https 段（配置闭环自动生成时默认关）
        var assembly = typeof(Program).Assembly;
        var templateField = typeof(Program).GetField(
            "ProductionTemplate",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        templateField.Should().NotBeNull();
        var template = (string)templateField!.GetValue(null)!;
        template.Should().Contain("Https");
        template.Should().Contain("\"Enabled\": false");
        template.Should().Contain("https://0.0.0.0:5001");
    }

    private static IConfiguration BuildConfig(Dictionary<string, string?> values)
        => new ConfigurationBuilder().AddInMemoryCollection(values).Build();
}
