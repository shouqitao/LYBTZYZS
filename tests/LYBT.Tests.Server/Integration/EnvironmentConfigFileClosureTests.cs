using System.Reflection;
using FluentAssertions;
using Xunit;

namespace LYBT.Tests.Server.Integration;

/// <summary>
/// 配置闭环单测（2026-08-12: 环境配置文件缺失时自动生成）——
/// 反射调用 Program.EnsureEnvironmentConfigFiles——临时 CWD 验证生成 + 不覆盖已存在
/// </summary>
[CollectionDefinition("ConfigClosure", DisableParallelization = true)]
public class ConfigClosureCollection
{
}

[Collection("ConfigClosure")]
public class EnvironmentConfigFileClosureTests : IDisposable
{
    private readonly string _tempDir;
    private readonly string _originalCwd;

    public EnvironmentConfigFileClosureTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "lybt-config-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
        _originalCwd = Directory.GetCurrentDirectory();
        Directory.SetCurrentDirectory(_tempDir);
    }

    public void Dispose()
    {
        Directory.SetCurrentDirectory(_originalCwd);
        try { Directory.Delete(_tempDir, recursive: true); } catch { /* 清理尽力 */ }
    }

    private static void InvokeEnsure(string environment)
    {
        var method = typeof(Program).GetMethod("EnsureEnvironmentConfigFiles",
            BindingFlags.NonPublic | BindingFlags.Static)!;
        method.Invoke(null, new object[] { environment });
    }

    [Fact]
    public void MissingFiles_AreGenerated_WithPlaceholders()
    {
        InvokeEnsure("Production");

        File.Exists(Path.Combine(_tempDir, "appsettings.json")).Should().BeTrue("缺失 appsettings.json 应自动生成");
        File.Exists(Path.Combine(_tempDir, "appsettings.Production.json")).Should().BeTrue("缺失环境文件应自动生成");

        var appJson = File.ReadAllText(Path.Combine(_tempDir, "appsettings.json"));
        appJson.Should().Contain("${Jwt__SecretKey}", "占位符 = 运维注入名（双下划线——配置唯一化）");
        appJson.Should().Contain("${DefaultPasswords__SysAdminPassword}");
        appJson.Should().Contain("${SystemAdmin__InitialSetupToken}");

        var prodJson = File.ReadAllText(Path.Combine(_tempDir, "appsettings.Production.json"));
        prodJson.Should().Contain("http://0.0.0.0:5000", "Production 模板含 Kestrel 端口");
    }

    [Fact]
    public void ExistingFiles_AreNotOverwritten()
    {
        // 已存在文件不被覆盖（用户自定义配置优先）
        var customPath = Path.Combine(_tempDir, "appsettings.json");
        File.WriteAllText(customPath, "{ \"custom\": true }");

        InvokeEnsure("Production");

        File.ReadAllText(customPath).Should().Be("{ \"custom\": true }", "已存在文件不得覆盖");
    }

    [Fact]
    public void DevelopmentEnvironment_UsesDevelopmentTemplate()
    {
        InvokeEnsure("Development");

        File.Exists(Path.Combine(_tempDir, "appsettings.Development.json")).Should().BeTrue();
        var devJson = File.ReadAllText(Path.Combine(_tempDir, "appsettings.Development.json"));
        devJson.Should().Contain("Development");
    }
}
