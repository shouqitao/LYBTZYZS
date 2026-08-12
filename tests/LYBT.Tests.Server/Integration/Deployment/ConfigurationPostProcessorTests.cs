using FluentAssertions;
using LYBT.WebAPI.Configuration;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace LYBT.Tests.Server.Integration.Deployment;

/// <summary>
/// 配置后处理器单测（CFG-BATCH2 边界决策 2/3：占位符未展开/空串 → 视为无效 → 回退下一级）
/// </summary>
public class ConfigurationPostProcessorTests
{
    [Theory]
    [InlineData("real-value", true)]
    [InlineData("", false)]
    [InlineData("   ", false)]
    [InlineData("${Jwt__SecretKey}", false)]
    [InlineData("${SYSADMIN_PASSWORD}", false)]
    [InlineData(null, false)]
    public void IsValidValue_JudgesCorrectly(string? value, bool expected)
        => ConfigurationPostProcessor.IsValidValue(value).Should().Be(expected);

    [Fact]
    public void Process_EnvOverridesConfigFile_KeepsEnvValue()
    {
        // 优先级：env > runtime-overrides > json——env 有效时最高优先（无需回退）
        var root = BuildRoot(
            new Dictionary<string, string?> { ["Jwt:SecretKey"] = "${Jwt__SecretKey}" },      // json 占位
            new Dictionary<string, string?> { ["Jwt:SecretKey"] = "env-real-value" });          // env 有效

        ConfigurationPostProcessor.Process(root);

        root["Jwt:SecretKey"].Should().Be("env-real-value", "env 最高优先");
    }

    [Fact]
    public void Process_EnvEmptyString_FallsBackToConfigFile()
    {
        // 边界决策 3: env 空串 → 视为缺失 → 回退配置文件有效值
        var root = BuildRoot(
            new Dictionary<string, string?> { ["Jwt:SecretKey"] = "config-real-value" },        // json 有效
            new Dictionary<string, string?> { ["Jwt:SecretKey"] = "" });                        // env 空串

        ConfigurationPostProcessor.Process(root);

        root["Jwt:SecretKey"].Should().Be("config-real-value", "空串回退到配置文件有效值");
    }

    [Fact]
    public void Process_EnvPlaceholder_FallsBackToConfigFile()
    {
        // 边界决策 2: env 未展开占位 → 回退配置文件
        var root = BuildRoot(
            new Dictionary<string, string?> { ["ConnectionStrings:DefaultConnection"] = "Server=real;Database=DB" },
            new Dictionary<string, string?> { ["ConnectionStrings:DefaultConnection"] = "${ConnectionStrings__DefaultConnection}" });

        ConfigurationPostProcessor.Process(root);

        root["ConnectionStrings:DefaultConnection"].Should().Be("Server=real;Database=DB");
    }

    [Fact]
    public void Process_AllInvalid_KeepsInvalid_ForValidator()
    {
        // 全部无效 → 保持原样——由配置校验器拦截提示（不回退伪造值）
        var root = BuildRoot(
            new Dictionary<string, string?> { ["Jwt:SecretKey"] = "${Jwt__SecretKey}" },
            new Dictionary<string, string?> { ["Jwt:SecretKey"] = "" });

        ConfigurationPostProcessor.Process(root);

        // 最高 provider（env 空串）——Process 不注入有效值时保持无效（校验器拦截语义）
        string.IsNullOrWhiteSpace(root["Jwt:SecretKey"]).Should().BeTrue();
    }

    [Fact]
    public void Process_UnknownKeys_AreNotTouched()
    {
        // 只处理已知键清单——未知键不动
        var root = BuildRoot(
            new Dictionary<string, string?> { ["Some:Custom:Key"] = "${placeholder}" },
            new Dictionary<string, string?>());

        ConfigurationPostProcessor.Process(root);

        root["Some:Custom:Key"].Should().Be("${placeholder}", "未知键不参与清理（避免误删合法配置）");
    }

    private static IConfigurationRoot BuildRoot(params Dictionary<string, string?>[] layers)
    {
        // ConfigurationManager = 生产真实类型（IConfigurationRoot + IConfigurationBuilder——Process 可注入覆盖层）
        var builder = new ConfigurationManager();
        foreach (var layer in layers)
            builder.AddInMemoryCollection(layer);
        return builder;
    }
}
