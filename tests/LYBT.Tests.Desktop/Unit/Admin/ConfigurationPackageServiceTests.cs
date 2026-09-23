using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using LYBT.Desktop.Admin.Services;
using LYBT.Desktop.Admin.Sysadmin.Services;
using LYBT.Desktop.Contracts.Enums;
using LYBT.Desktop.Contracts.Models;
using LYBT.Desktop.Contracts.Results;
using LYBT.Desktop.Contracts.Roles;
using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Infrastructure.Interfaces;
using LYBT.Desktop.Infrastructure.Services.FeatureToggle;
using LYBT.Shared.Configuration.Options.Client;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Logging.Abstractions;

namespace LYBT.Tests.Desktop.Unit.Admin;

/// <summary>
/// US-SHELL-016 配置包服务单测：真实临时文件往返（导出 → 导入 → 应用面断言）、
/// 格式校验拒绝（不触达任何配置服务）、安全项不落包、角色权限快照一致性校验。
/// Dispose 清理临时目录。
/// </summary>
public class ConfigurationPackageServiceTests : IDisposable
{
    private const string RealDbPassword = "s3cr3t-db-password";
    private const string RemoteUrl = "http://192.168.190.248:5000";

    private readonly string _tempDirectory;
    private readonly IClinicSettingsService _clinicSettings = Substitute.For<IClinicSettingsService>();
    private readonly IConnectionSettingsService _connectionSettings = Substitute.For<IConnectionSettingsService>();
    private readonly ILocalDatabaseSettingsService _localDatabaseSettings = Substitute.For<ILocalDatabaseSettingsService>();
    private readonly IClientConfigurationStore _configurationStore = Substitute.For<IClientConfigurationStore>();
    private readonly IFeatureToggleService _featureToggles = Substitute.For<IFeatureToggleService>();
    private readonly IRoleRegistry _roleRegistry = Substitute.For<IRoleRegistry>();

    public ConfigurationPackageServiceTests()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), "lybt-cfg-pkg-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDirectory);

        // 导出源（当前运行配置）
        _clinicSettings.GetSettings().Returns(new ClinicSettingsOptions
        {
            Name = "凌隐宝堂中医诊所",
            Department = "中医科",
            Address = "杭州市西湖区",
            Phone = "0571-88888888",
            Email = "clinic@example.com",
            LicenseNumber = "ZYY-2026-0001",
            Timezone = "Asia/Shanghai"
        });

        _connectionSettings.RemoteUrl.Returns(RemoteUrl);
        _connectionSettings.PreferredMode.Returns("Remote");
        _connectionSettings.IsValidUrl(Arg.Any<string>()).Returns(true);

        _localDatabaseSettings.Current.Returns(new LocalDatabaseProfile
        {
            Provider = LocalDatabaseProvider.SqlServer,
            Server = @"SRV\SQLEXPRESS",
            Database = "LYBT",
            UseWindowsAuthentication = false,
            UserId = "sa",
            Password = RealDbPassword
        });
        _localDatabaseSettings.SaveAsync(Arg.Any<LocalDatabaseProfile>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(CommandResult<bool>.Succeeded(true)));
        _configurationStore.SaveSectionAsync(Arg.Any<string>(), Arg.Any<IReadOnlyDictionary<string, object>>())
            .Returns(Task.FromResult(true));
        _clinicSettings.SaveSettingsAsync(Arg.Any<ClinicSettingsOptions>()).Returns(Task.FromResult(true));

        _featureToggles.IsEnabled("OverwriteConflicts").Returns(true);
        _featureToggles.GetValue("DuplicateHerbMergeStrategy").Returns("Max");

        _roleRegistry.GetHomeViewName(UserRole.SuperAdmin).Returns("SysadminHomeView");
        _roleRegistry.GetHomeViewName(UserRole.Admin).Returns("AdminHomeView");
        _roleRegistry.GetHomeViewName(UserRole.Doctor).Returns("ClinicalHomeView");
        _roleRegistry.GetHomeViewName(UserRole.Receptionist).Returns("ReceptionistHomeView");
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_tempDirectory))
                Directory.Delete(_tempDirectory, recursive: true);
        }
        catch (IOException)
        {
            // 清理失败不影响断言结果
        }
        GC.SuppressFinalize(this);
    }

    private ConfigurationPackageService CreateService() => new(
        _clinicSettings,
        _connectionSettings,
        _localDatabaseSettings,
        _configurationStore,
        _featureToggles,
        _roleRegistry,
        NullLogger<ConfigurationPackageService>.Instance);

    private string TempFile(string name) => Path.Combine(_tempDirectory, name);

    #region 导出

    [Fact]
    public async Task ExportAsync_WritesPackageWithSectionsAndSize()
    {
        var path = TempFile("export.json");

        var result = await CreateService().ExportAsync(path);

        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        var data = result.Data!;
        data.FilePath.Should().Be(Path.GetFullPath(path));
        data.FileSizeBytes.Should().Be(new FileInfo(path).Length).And.BeGreaterThan(0);
        data.Sections.Should().BeEquivalentTo("clinicSettings", "connection", "featureToggles", "rolePermissions");
        File.Exists(path).Should().BeTrue();
    }

    [Fact]
    public async Task ExportAsync_WritesCamelCaseShapeAndNeverWritesSecrets()
    {
        var path = TempFile("secrets.json");
        await CreateService().ExportAsync(path);

        var json = await File.ReadAllTextAsync(path);

        // 结构：camelCase 属性 + 枚举成员名 + 缩进
        json.Should().Contain("\"packageVersion\": \"1.0\"");
        json.Should().Contain("\"mode\": \"Remote\"");
        json.Should().Contain("\"provider\": \"SqlServer\"");
        json.Should().Contain("\"useWindowsAuthentication\": false");
        json.Should().Contain("\"userId\": \"sa\"");
        json.Should().Contain("\"overwriteConflicts\": true");
        json.Should().Contain("\"duplicateHerbMergeStrategy\": \"Max\"");
        json.Should().Contain("\"homeView\": \"SysadminHomeView\"");

        // 安全项：口令只落占位符，绝不落真实值；不含 JWT 密钥/默认密码/令牌
        json.Should().Contain("\"password\": \"***\"");
        json.Should().NotContain(RealDbPassword);
        json.Should().NotContain("SecretKey");
        json.Should().NotContain("Jwt");
        json.Should().NotContain("DefaultPassword");
        json.Should().NotContain("AccessToken");
    }

    [Fact]
    public async Task ExportAsync_RoleSnapshot_TracksRuntimePermissionBaseline()
    {
        var path = TempFile("roles.json");
        await CreateService().ExportAsync(path);

        using var document = JsonDocument.Parse(await File.ReadAllTextAsync(path));
        var superAdmin = document.RootElement
            .GetProperty("rolePermissions")
            .EnumerateArray()
            .Single(entry => entry.GetProperty("role").GetString() == nameof(UserRole.SuperAdmin));

        superAdmin.GetProperty("homeView").GetString().Should().Be("SysadminHomeView");
        var allowedViews = superAdmin.GetProperty("allowedViews").EnumerateArray().Select(v => v.GetString()).ToList();
        allowedViews.Should().Contain("ConfigExportImportView");
        allowedViews.Should().BeInAscendingOrder(StringComparer.Ordinal);
    }

    #endregion

    #region 导入

    [Fact]
    public async Task RoundTrip_AppliesClinicConnectionAndFeatureToggles()
    {
        var path = TempFile("roundtrip.json");
        var service = CreateService();
        (await service.ExportAsync(path)).Success.Should().BeTrue();

        // 导入前把「当前值」改成与包内不同，以便验证确实按包应用
        _connectionSettings.RemoteUrl.Returns("http://10.0.0.9:5000");
        _connectionSettings.PreferredMode.Returns("Local");
        _localDatabaseSettings.Current.Returns(new LocalDatabaseProfile
        {
            Provider = LocalDatabaseProvider.LocalDb,
            Server = @"(localdb)\MSSQLLocalDB",
            Database = "LYBTDesktop",
            Password = "current-password"
        });

        var result = await service.ImportAsync(path);

        result.Success.Should().BeTrue();
        var report = result.Data!;
        report.RequiresRestart.Should().BeTrue();
        report.AppliedSections.Should().BeEquivalentTo(
            "clinicSettings", "connection.remoteUrl", "connection.mode", "connection.localDatabase", "featureToggles");

        // 诊所信息：按包内容应用，且时区沿用当前值（节级覆盖不得静默丢失）
        await _clinicSettings.Received(1).SaveSettingsAsync(Arg.Is<ClinicSettingsOptions>(settings =>
            settings.Name == "凌隐宝堂中医诊所"
            && settings.Department == "中医科"
            && settings.Address == "杭州市西湖区"
            && settings.Phone == "0571-88888888"
            && settings.Email == "clinic@example.com"
            && settings.LicenseNumber == "ZYY-2026-0001"
            && settings.Timezone == "Asia/Shanghai"));

        // 连接：远程地址 + 模式按包应用
        await _connectionSettings.Received(1).SaveRemoteUrlAsync(RemoteUrl);
        await _connectionSettings.Received(1).SavePreferredModeAsync("Remote");

        // 本地数据库：按包应用元数据，口令保留当前值（永不从包导入）
        await _localDatabaseSettings.Received(1).SaveAsync(
            Arg.Is<LocalDatabaseProfile>(profile =>
                profile.Provider == LocalDatabaseProvider.SqlServer
                && profile.Server == @"SRV\SQLEXPRESS"
                && profile.Database == "LYBT"
                && profile.UserId == "sa"
                && profile.Password == "current-password"),
            Arg.Any<CancellationToken>());

        // 功能开关：camelCase → 配置文件 PascalCase
        await _configurationStore.Received(1).SaveSectionAsync(
            "FeatureToggles",
            Arg.Is<IReadOnlyDictionary<string, object>>(values =>
                (bool)values["OverwriteConflicts"] && (string)values["DuplicateHerbMergeStrategy"] == "Max"));

        // 角色权限：只校验不应用，且本机快照与包一致
        report.SkippedItems.Should().Contain(item => item.Section == "rolePermissions");
        report.Notes.Should().Contain("角色权限快照与当前程序一致");
    }

    [Fact]
    public async Task ImportAsync_InvalidJson_IsRejectedWithoutTouchingAnyService()
    {
        var path = TempFile("broken.json");
        await File.WriteAllTextAsync(path, "{ this is not json ]");

        var result = await CreateService().ImportAsync(path);

        result.Success.Should().BeFalse();
        result.Error.Should().Contain("格式错误");
        await AssertNoConfigServiceCallsAsync();
    }

    [Fact]
    public async Task ImportAsync_MissingPackageVersion_IsRejected()
    {
        var path = TempFile("no-version.json");
        await File.WriteAllTextAsync(path, """{ "clinicSettings": { "name": "X" } }""");

        var result = await CreateService().ImportAsync(path);

        result.Success.Should().BeFalse();
        result.Error.Should().Contain("packageVersion");
        await AssertNoConfigServiceCallsAsync();
    }

    [Fact]
    public async Task ImportAsync_UnsupportedPackageVersion_IsRejected()
    {
        var path = TempFile("future-version.json");
        await File.WriteAllTextAsync(path, """{ "packageVersion": "9.9", "clinicSettings": { "name": "X" } }""");

        var result = await CreateService().ImportAsync(path);

        result.Success.Should().BeFalse();
        result.Error.Should().Contain("不支持的配置包版本");
        await AssertNoConfigServiceCallsAsync();
    }

    [Fact]
    public async Task ImportAsync_RoleSnapshotMismatch_IsReportedAndNotApplied()
    {
        var path = TempFile("mismatch.json");
        var service = CreateService();
        (await service.ExportAsync(path)).Success.Should().BeTrue();

        // 篡改快照：把 SuperAdmin 的主页视图与可访问视图改成与当前程序不一致
        var root = JsonNode.Parse(await File.ReadAllTextAsync(path))!;
        var superAdmin = root["rolePermissions"]!.AsArray()
            .Single(entry => entry!["role"]!.GetValue<string>() == nameof(UserRole.SuperAdmin));
        superAdmin!["homeView"] = "OtherHomeView";
        superAdmin!["allowedViews"] = new JsonArray("SomeOtherView");
        await File.WriteAllTextAsync(path, root.ToJsonString());

        var result = await service.ImportAsync(path);

        result.Success.Should().BeTrue();
        var report = result.Data!;
        report.Notes.Should().Contain(note => note.Contains("角色权限不一致") && note.Contains("SuperAdmin"));
        report.SkippedItems.Should().Contain(item => item.Section == "rolePermissions");
        // 角色权限只报告不应用
        _roleRegistry.DidNotReceiveWithAnyArgs().Register(default!);
    }

    [Fact]
    public async Task ImportAsync_MissingFile_IsRejected()
    {
        var result = await CreateService().ImportAsync(TempFile("does-not-exist.json"));

        result.Success.Should().BeFalse();
        result.Error.Should().Contain("不存在");
        await AssertNoConfigServiceCallsAsync();
    }

    #endregion

    /// <summary>格式校验失败时不得触达任何配置写入服务（US-SHELL-016：格式错误拒绝）</summary>
    private async Task AssertNoConfigServiceCallsAsync()
    {
        await _clinicSettings.DidNotReceiveWithAnyArgs().SaveSettingsAsync(default!);
        await _connectionSettings.DidNotReceiveWithAnyArgs().SaveRemoteUrlAsync(default!);
        await _connectionSettings.DidNotReceiveWithAnyArgs().SavePreferredModeAsync(default!);
        await _localDatabaseSettings.DidNotReceiveWithAnyArgs().SaveAsync(default!, default);
        await _configurationStore.DidNotReceiveWithAnyArgs().SaveSectionAsync(default!, default!);
    }
}
