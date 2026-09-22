using System.IO;
using System.Text.Json;
using FluentAssertions;
using LYBT.Desktop.Contracts.Enums;
using LYBT.Desktop.Contracts.Models;
using LYBT.Desktop.Infrastructure.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace LYBT.Tests.Desktop;

/// <summary>
/// B-07 本地数据库配置服务单测：默认值 / 落盘回读 / 口令 DPAPI 保护 / 损坏文件降级 / 连接串形态。
/// 真实文件系统（临时目录，绝不触碰开发机真实的 %LOCALAPPDATA%\LYBT\Desktop\local-database.json）
/// + 真实 LocalDB（master 库）；Dispose 清理临时目录。
/// </summary>
public class LocalDatabaseSettingsServiceTests : IDisposable
{
    /// <summary>嵌入式 LocalWebAPI 历史硬编码连接串——默认配置必须逐字一致（行为不回退）</summary>
    private const string LegacyEmbeddedConnectionString =
        "Server=(localdb)\\MSSQLLocalDB;Database=LYBTDesktop;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True";

    /// <summary>LocalDB 自动实例的 master 库（连接测试用，无需预建库）</summary>
    private static readonly LocalDatabaseProfile LocalDbMasterProfile = new()
    {
        Provider = LocalDatabaseProvider.LocalDb,
        Server = LocalDatabaseProfile.DefaultLocalDbServer,
        Database = "master",
        UseWindowsAuthentication = true
    };

    private readonly string _tempDirectory;

    public LocalDatabaseSettingsServiceTests()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), "lybt-localdb-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDirectory);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_tempDirectory))
                Directory.Delete(_tempDirectory, recursive: true);
        }
        catch (Exception)
        {
            // 临时目录清理为 best-effort（SQL 连接池可能短暂持有文件句柄）
        }
    }

    /// <summary>测试接缝：数据目录指向临时目录（<see cref="LocalDatabaseSettingsService.DataDirectory"/> 为 protected virtual）</summary>
    private sealed class TempDirectorySettingsService : LocalDatabaseSettingsService
    {
        private readonly string _directory;

        public TempDirectorySettingsService(string directory)
            : base(NullLogger<LocalDatabaseSettingsService>.Instance)
        {
            _directory = directory;
        }

        protected override string DataDirectory => _directory;
    }

    private LocalDatabaseSettingsService CreateService() => new TempDirectorySettingsService(_tempDirectory);

    [Fact]
    public void Current_WithNoConfigFile_ReturnsDefaultProfileAndDoesNotCreateFile()
    {
        var service = CreateService();

        service.Current.Should().Be(LocalDatabaseProfile.Default);
        service.Current.Provider.Should().Be(LocalDatabaseProvider.LocalDb);
        service.Current.Server.Should().Be(LocalDatabaseProfile.DefaultLocalDbServer);
        service.Current.Database.Should().Be(LocalDatabaseProfile.DefaultDatabase);
        service.Current.UseWindowsAuthentication.Should().BeTrue();
        service.Current.Password.Should().BeNull();
        File.Exists(service.SettingsFilePath).Should().BeFalse();
    }

    [Fact]
    public void SettingsFilePath_LivesInDataDirectory()
    {
        var service = CreateService();

        service.SettingsFilePath.Should().Be(Path.Combine(_tempDirectory, "local-database.json"));
    }

    [Fact]
    public void BuildConnectionString_WithDefaultProfile_EqualsLegacyEmbeddedConnectionString()
    {
        var service = CreateService();

        service.BuildConnectionString().Should().Be(LegacyEmbeddedConnectionString);
    }

    [Fact]
    public async Task SaveAsync_ThenReloadInNewInstance_RoundTripsProfile()
    {
        var profile = new LocalDatabaseProfile
        {
            Provider = LocalDatabaseProvider.SqlServer,
            Server = @"srv01\SQLEXPRESS",
            Database = "ClinicDb",
            UseWindowsAuthentication = true
        };

        var saved = await CreateService().SaveAsync(profile);

        saved.Success.Should().BeTrue();
        saved.Error.Should().BeNull();

        var reloaded = CreateService().Current;
        reloaded.Provider.Should().Be(LocalDatabaseProvider.SqlServer);
        reloaded.Server.Should().Be(@"srv01\SQLEXPRESS");
        reloaded.Database.Should().Be("ClinicDb");
        reloaded.UseWindowsAuthentication.Should().BeTrue();
        reloaded.UserId.Should().BeNull();
        reloaded.Password.Should().BeNull();
    }

    [Fact]
    public async Task SaveAsync_ThenCurrent_ReflectsProfileWithoutReload()
    {
        var service = CreateService();
        var profile = new LocalDatabaseProfile
        {
            Provider = LocalDatabaseProvider.SqlServer,
            Server = "srv02",
            Database = "ClinicDb2",
            UseWindowsAuthentication = true
        };

        await service.SaveAsync(profile);

        service.Current.Should().Be(profile);
    }

    [Fact]
    public async Task SaveAsync_WritesProviderAsEnumNameAndRedactedPasswordFlag()
    {
        var service = CreateService();
        await service.SaveAsync(new LocalDatabaseProfile
        {
            Provider = LocalDatabaseProvider.SqlServer,
            Server = "srv03",
            Database = "ClinicDb3",
            UseWindowsAuthentication = true
        });

        var root = JsonDocument.Parse(await File.ReadAllTextAsync(service.SettingsFilePath)).RootElement;

        root.GetProperty("Provider").GetString().Should().Be("SqlServer", "枚举必须以名称而非序号落盘");
        root.GetProperty("Server").GetString().Should().Be("srv03");
        root.GetProperty("Database").GetString().Should().Be("ClinicDb3");
        root.GetProperty("UseWindowsAuthentication").GetBoolean().Should().BeTrue();
        root.GetProperty("HasPassword").GetBoolean().Should().BeFalse();
        root.TryGetProperty("Password", out _).Should().BeFalse("明文口令字段不存在");
    }

    [Fact]
    public async Task SaveAsync_WithSqlPassword_KeepsPlaintextOutOfFileButReturnsItAfterReload()
    {
        const string password = "S3cret!Passw0rd_凌隐宝堂";
        var service = CreateService();

        var saved = await service.SaveAsync(new LocalDatabaseProfile
        {
            Provider = LocalDatabaseProvider.SqlServer,
            Server = "srv04",
            Database = "ClinicDb4",
            UseWindowsAuthentication = false,
            UserId = "sa",
            Password = password
        });

        saved.Success.Should().BeTrue();

        var rawJson = await File.ReadAllTextAsync(service.SettingsFilePath);
        rawJson.Should().NotContain(password, "口令只能以 DPAPI 密文落盘");
        rawJson.Should().Contain("PasswordProtected");
        JsonDocument.Parse(rawJson).RootElement.GetProperty("HasPassword").GetBoolean().Should().BeTrue();

        var reloaded = CreateService().Current;
        reloaded.Password.Should().Be(password, "DPAPI(CurrentUser) 可解密回明文供连接使用");
        reloaded.UserId.Should().Be("sa");
        service.BuildConnectionString().Should().Contain($"Password={password}");
    }

    [Fact]
    public void Current_WithCorruptJson_FallsBackToDefaultWithoutThrowing()
    {
        var service = CreateService();
        File.WriteAllText(service.SettingsFilePath, "{ \"Provider\": this is not json");

        var act = () => service.Current;

        act.Should().NotThrow();
        service.Current.Should().Be(LocalDatabaseProfile.Default);
    }

    [Fact]
    public void Current_WithWrongTypedField_FallsBackToDefaultWithoutThrowing()
    {
        var service = CreateService();
        File.WriteAllText(service.SettingsFilePath, "{ \"UseWindowsAuthentication\": \"yes\" }");

        var act = () => service.Current;

        act.Should().NotThrow();
        service.Current.Should().Be(LocalDatabaseProfile.Default);
    }

    [Fact]
    public async Task Reload_PicksUpChangeWrittenByAnotherInstance()
    {
        var service = CreateService();
        service.Current.Database.Should().Be(LocalDatabaseProfile.DefaultDatabase);

        await CreateService().SaveAsync(new LocalDatabaseProfile
        {
            Provider = LocalDatabaseProvider.LocalDb,
            Server = LocalDatabaseProfile.DefaultLocalDbServer,
            Database = "ExternalDb",
            UseWindowsAuthentication = true
        });

        service.Reload();

        service.Current.Database.Should().Be("ExternalDb");
    }

    [Fact]
    public async Task BuildConnectionString_WithSqlServerWindowsAuth_ContainsTrustedConnectionAndEncrypt()
    {
        var service = CreateService();
        await service.SaveAsync(new LocalDatabaseProfile
        {
            Provider = LocalDatabaseProvider.SqlServer,
            Server = "sqlhost,1433",
            Database = "ClinicDb5",
            UseWindowsAuthentication = true
        });

        var connectionString = service.BuildConnectionString();

        connectionString.Should().Contain("Server=sqlhost,1433");
        connectionString.Should().Contain("Trusted_Connection=True");
        connectionString.Should().Contain("Encrypt=True");
        connectionString.Should().NotContain("User ID=");
    }

    [Fact]
    public async Task BuildConnectionString_WithSqlServerSqlAuth_ContainsUserIdAndPassword()
    {
        var service = CreateService();
        await service.SaveAsync(new LocalDatabaseProfile
        {
            Provider = LocalDatabaseProvider.SqlServer,
            Server = "sqlhost",
            Database = "ClinicDb6",
            UseWindowsAuthentication = false,
            UserId = "app_login",
            Password = "p@ss"
        });

        var connectionString = service.BuildConnectionString();

        connectionString.Should().Contain("User ID=app_login");
        connectionString.Should().Contain("Password=p@ss");
        connectionString.Should().Contain("Encrypt=True");
        connectionString.Should().NotContain("Trusted_Connection=True");
    }

    [Fact]
    public async Task TestAsync_AgainstLocalDbMaster_Succeeds()
    {
        var result = await CreateService().TestAsync(LocalDbMasterProfile);

        result.Success.Should().BeTrue($"LocalDB master 库应可连接：{result.Error}");
        result.Error.Should().BeNull();
    }

    [Fact]
    public async Task TestAsync_AgainstMissingDatabase_FailsWithServerMessage()
    {
        var missing = LocalDbMasterProfile with { Database = $"LYBT_Missing_{Guid.NewGuid():N}" };

        var result = await CreateService().TestAsync(missing);

        result.Success.Should().BeFalse();
        result.Error.Should().NotBeNullOrWhiteSpace();
    }
}
