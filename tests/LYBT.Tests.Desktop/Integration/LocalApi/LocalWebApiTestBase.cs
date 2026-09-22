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
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
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
    private readonly string _dbName = $"LYBTZYZS_LocalApi_{Guid.NewGuid():N}";
    private string _connectionString = null!;
    private WebApplication? _app;

    /// <summary>本次测试宿主的数据库名（B-06：备份状态 DatabaseName 断言）</summary>
    protected string CurrentDatabaseName => _dbName;

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
        _connectionString = $@"Server=(localdb)\MSSQLLocalDB;Database={_dbName};Trusted_Connection=True;TrustServerCertificate=True";

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

        using var scope = _app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.EnsureCreatedAsync();
        await LYBT.Module.Identity.Services.IdentitySeedData.SeedRolesAndAdminAsync(scope.ServiceProvider);
        await EnsureRoleUsersAsync(scope.ServiceProvider);
        await LocalWebApiSeedData.SeedAsync(db, scope.ServiceProvider);
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

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(_connectionString)
            .Options;
        await using var context = new AppDbContext(options);
        await context.Database.EnsureDeletedAsync();

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
