using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using LYBT.Infrastructure.Data;
using LYBT.LocalWebAPI;
using LYBT.LocalWebAPI.Data;
using LYBT.Entities.Users;
using LYBT.Shared.Models.Contracts.Auth;
using LYBT.Shared.Models.Enums;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Respawn;
using Respawn.Graph;
using Xunit;

namespace LYBT.Tests.Desktop.Integration.LocalApi;

[CollectionDefinition("LocalApi", DisableParallelization = true)]
public class LocalApiCollection
{
}

// 方案 D：LocalWebApiTestBase 为唯一 E2E 基类（合并 LocalApiTestBase + LocalWebApiControllerTestBase，统一经 SharedHost）

/// <summary>
/// LocalAPI E2E 基类 — WebApplication 内存启动 LocalWebAPI（LocalDB）
/// 复用 LocalWebApiProgram.CreateApplication 确保与生产注册一致，解决 appsettings.json 路径问题
/// </summary>
public abstract class LocalWebApiTestBase : IAsyncLifetime
{
    // ── 共享 LocalDB（每测试进程一库，LocalApi/E2ELocal collection 串行复用）──
    // 原实现每测试方法都建库+建表+种子（约 11s/测试，Desktop 集成层 6m12s）。
    // 现改为：库+架构+身份种子只在进程内首次建立；每测试类首次运行时 Respawn 清业务表并恢复业务种子；
    // 每个测试仍启动独立宿主（独立限流桶/内存状态），只共享数据库。
    // collection 已 DisableParallelization => 无并发访问（门闩仅防御同进程内的串行切换）。
    private static readonly SemaphoreSlim SharedDbGate = new(1, 1);
    private static string? _sharedDbName;
    private static string? _sharedConnectionString;
    private static Type? _lastPreparedTestClass;
    private static Respawner? _respawner;

    private string _connectionString = null!;
    private WebApplication? _app;

    /// <summary>本次测试宿主的数据库名（B-06：备份状态 DatabaseName 断言）</summary>
    protected string CurrentDatabaseName => _sharedDbName ?? throw new InvalidOperationException("共享数据库尚未初始化");

    /// <summary>本次测试宿主的临时备份目录（B-06：InitializeAsync 生成，DisposeAsync 清理）</summary>
    protected string BackupDirectoryPath { get; private set; } = string.Empty;

    private const string TestJwtSecret = "LYBT-LocalWebAPI-Secret-Key-2024-DoNotUseInProduction";

    protected HttpClient Client { get; private set; } = null!;

    protected static JsonSerializerOptions Json { get; } = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = null
    };

    public async Task InitializeAsync()
    {
        await EnsureSharedDatabaseAsync();
        _connectionString = _sharedConnectionString!;

        Environment.SetEnvironmentVariable("ASPNETCORE_URLS", "http://127.0.0.1:0");
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Test");

        var builder = LocalWebApiProgram.CreateBuilder();

        // 覆盖连接串与 JWT 密钥（测试专用）
        builder.Configuration["ConnectionStrings:DefaultConnection"] = _connectionString;
        builder.Configuration["Jwt:SecretKey"] = TestJwtSecret;
        builder.Configuration["DefaultPasswords:SysAdminPassword"] = "SysAdmin@2026!";
        builder.Configuration["DefaultPasswords:AdminPassword"] = "Admin@123456";
        builder.Configuration["DefaultPasswords:NewUserPassword"] = "User@123456";
        builder.Configuration["DefaultPasswords:ForceChangeOnFirstLogin"] = "false";

        // B-06：备份目录隔离——覆盖 Backup:Directory 到本次测试专属临时目录，
        // 避免测试写入开发者真实 %LOCALAPPDATA%\LYBT\Desktop\Backup（DisposeAsync 尽力清理）
        BackupDirectoryPath = Path.Combine(Path.GetTempPath(), "lybt_test_backup_" + Guid.NewGuid().ToString("N"));
        builder.Configuration["Backup:Directory"] = BackupDirectoryPath;

        builder.WebHost.ConfigureKestrel(options =>
        {
            options.Listen(System.Net.IPAddress.Loopback, 0);
        });

        _app = LocalWebApiProgram.CreateApplication(builder, _connectionString);

        await _app.StartAsync();

        var port = new Uri(_app.Urls.First()).Port;
        Client = new HttpClient { BaseAddress = new Uri($"http://127.0.0.1:{port}") };

        await PrepareSharedDataAsync(GetType());
    }

    /// <summary>
    /// 建立（进程内首次）共享数据库：建库 + 建表。身份/业务种子由首个测试宿主完成（见 PrepareSharedDataAsync）。
    /// </summary>
    private static async Task EnsureSharedDatabaseAsync()
    {
        if (_sharedConnectionString is not null)
            return;

        await SharedDbGate.WaitAsync();
        try
        {
            if (_sharedConnectionString is not null)
                return;

            var dbName = $"LYBTZYZS_LocalApiShared_{Environment.ProcessId}";
            var connectionString = $@"Server=(localdb)\MSSQLLocalDB;Database={dbName};Trusted_Connection=True;TrustServerCertificate=True";

            // 同进程号残留库（上次异常退出）先删除，保证本次全新
            await DropDatabaseAsync(connectionString);

            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlServer(connectionString)
                .Options;
            await using (var context = new AppDbContext(options))
            {
                await context.Database.EnsureCreatedAsync();
            }

            _sharedDbName = dbName;
            _sharedConnectionString = connectionString;

            // 进程退出时删除共享库（尽力而为；异常退出由下次同名删除兜底）
            AppDomain.CurrentDomain.ProcessExit += (_, _) =>
            {
                try
                {
                    DropDatabaseAsync(connectionString).GetAwaiter().GetResult();
                }
                catch
                {
                    // 进程退出清理失败不影响测试结论
                }
            };
        }
        finally
        {
            SharedDbGate.Release();
        }
    }

    /// <summary>
    /// 首个测试：写入身份 + 业务种子；同进程内后续测试类：Respawn 清业务表并恢复业务种子
    /// （身份表在 TablesToIgnore 中保留）。同一测试类内不重复清理，类内数据互相可见（测试已用唯一数据辅助隔离）。
    /// </summary>
    private async Task PrepareSharedDataAsync(Type testClass)
    {
        await SharedDbGate.WaitAsync();
        try
        {
            if (_lastPreparedTestClass == testClass)
                return;

            using var scope = _app!.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            if (_lastPreparedTestClass is not null)
            {
                // 清空业务数据（含 Identity 表——Respawn 的 TablesToIgnore 按 schema+name 匹配，
                // 依赖忽略名单不可靠；改为清空后确定性重建身份种子）
                await ResetDataAsync();
            }

            await LYBT.Module.Identity.Services.IdentitySeedData.SeedRolesAndAdminAsync(scope.ServiceProvider);
            await EnsureRoleUsersAsync(scope.ServiceProvider);
            await LocalWebApiSeedData.SeedAsync(db, scope.ServiceProvider);
            _lastPreparedTestClass = testClass;
        }
        finally
        {
            SharedDbGate.Release();
        }
    }

    /// <summary>Respawn 清空全部业务/身份表（保留 EF 迁移历史），随后由调用方重建种子。</summary>
    private static async Task ResetDataAsync()
    {
        _respawner ??= await CreateRespawnerAsync();

        await using var connection = new SqlConnection(_sharedConnectionString);
        await connection.OpenAsync();
        await _respawner.ResetAsync(connection);
    }

    private static async Task<Respawner> CreateRespawnerAsync()
    {
        await using var connection = new SqlConnection(_sharedConnectionString);
        await connection.OpenAsync();

        return await Respawner.CreateAsync(connection, new RespawnerOptions
        {
            TablesToIgnore = new Table[] { new("dbo", "__EFMigrationsHistory") },
            WithReseed = true
        });
    }

    private static async Task DropDatabaseAsync(string connectionString)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(connectionString)
            .Options;
        await using var context = new AppDbContext(options);
        await context.Database.EnsureDeletedAsync();
    }

    private static async Task EnsureRoleUsersAsync(IServiceProvider serviceProvider)
    {
        var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        await EnsureUserAsync(userManager, "admin", "管理员", "admin@lybtzyzs.local", "Admin@123456", LYBT.Infrastructure.Constants.RoleConstants.Admin, UserRole.Admin);
        await EnsureUserAsync(userManager, "doctor", "医生", "doctor@lybtzyzs.local", "Doctor@123456", LYBT.Infrastructure.Constants.RoleConstants.Doctor, UserRole.Doctor);
        await EnsureUserAsync(userManager, "receptionist", "前台", "receptionist@lybtzyzs.local", "Receptionist@123456", LYBT.Infrastructure.Constants.RoleConstants.Receptionist, UserRole.Receptionist);
    }

    private static async Task EnsureUserAsync(UserManager<ApplicationUser> userManager, string userName, string realName, string email, string password, string role, UserRole userRole)
    {
        if (await userManager.FindByNameAsync(userName) != null)
            return;

        var user = new ApplicationUser
        {
            UserName = userName,
            RealName = realName,
            Email = email,
            Role = userRole,
            Status = CommonStatus.Enabled,
            CreatedAt = DateTime.UtcNow
        };
        var result = await userManager.CreateAsync(user, password);
        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(user, role);
        }
    }

    public async Task DisposeAsync()
    {
        Client?.Dispose();

        if (_app != null)
        {
            await _app.StopAsync();
            await _app.DisposeAsync();
        }

        Environment.SetEnvironmentVariable("ASPNETCORE_URLS", null);
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", null);

        // 共享库由进程级清理（ProcessExit）负责，此处不再删除
        // B-06：清理本次测试的临时备份目录（尽力而为——句柄占用/权限失败不影响测试结论）
        if (!string.IsNullOrEmpty(BackupDirectoryPath))
        {
            try
            {
                if (Directory.Exists(BackupDirectoryPath))
                    Directory.Delete(BackupDirectoryPath, recursive: true);
            }
            catch
            {
                // 清理失败交由系统临时目录兜底
            }
        }
    }

    protected async Task<string> GetTokenAsync(string userName, string password)
    {
        var request = new LoginRequest
        {
            UserName = userName,
            Password = password
        };

        var response = await Client.PostAsJsonAsync("/api/v1/auth/login", request);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadFromJsonAsync<JsonElement>(Json);
        return json.GetProperty("data").GetProperty("token").GetString()!;
    }

    protected Task<string> GetSysadminTokenAsync() => GetTokenAsync("sysadmin", "SysAdmin@2026!");
    protected Task<string> GetAdminTokenAsync() => GetTokenAsync("admin", "Admin@123456");
    protected Task<string> GetDoctorTokenAsync() => GetTokenAsync("doctor", "Doctor@123456");
    protected Task<string> GetReceptionistTokenAsync() => GetTokenAsync("receptionist", "Receptionist@123456");

    protected void SetAuthHeader(string token)
    {
        Client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
    }

    protected void ClearAuthHeader()
    {
        Client.DefaultRequestHeaders.Authorization = null;
    }

    protected static string UniquePhone() => $"138{Random.Shared.Next(10000000, 99999999)}";
    protected static string UniqueIdNumber() => $"110101{DateTime.UtcNow:yyyyMMdd}{Random.Shared.Next(1000, 9999)}";
    protected static string UniqueUsername() => $"e2e_{Guid.NewGuid():N}"[..12];
    protected static string UniqueName(string prefix) => $"{prefix}_{DateTime.UtcNow:HHmmss}_{Guid.NewGuid():N}"[..20];
}
