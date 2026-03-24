using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using LYBT.Entities.Users;
using LYBT.Infrastructure.Data;
using LYBT.Shared.Models.Contracts.Auth;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Enums;
using LYBT.Shared.Utilities.Security;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace LYBT.Tests.Desktop.Integration.Fixtures;

/// <summary>
/// WebApi 集成测试装置
/// 
/// 设计决策：
/// - 使用 WebApplicationFactory 启动真实 WebApi
/// - 替换 SQL Server 为 SQLite In-Memory 数据库
/// - 预置测试用户 (test_doctor / password123)
/// - 实现 IAsyncLifetime 接口进行资源管理
/// - 提供 HttpClient 和 IServiceProvider 访问
/// </summary>
public class WebApiFixture : IAsyncLifetime
{
    /// <summary>
    /// 序列化并发 WebApplicationFactory 创建，防止 "logger is already frozen" 竞争条件
    /// </summary>
    private static readonly SemaphoreSlim InitGate = new(1, 1);

    private SqliteConnection? _sqliteConnection;

    /// <summary>
    /// WebApplicationFactory 实例
    /// </summary>
    public WebApplicationFactory<Program> Factory { get; private set; } = null!;

    /// <summary>
    /// HTTP 客户端（匿名未认证）
    /// </summary>
    public HttpClient ApiClient { get; private set; } = null!;

    /// <summary>
    /// 服务提供者
    /// </summary>
    public IServiceProvider Services => Factory.Services;

    // 测试用户固定 ID
    private static readonly Guid TestDoctorId = Guid.Parse("00000000-0000-0000-0000-000000000001");
    private static readonly Guid TestAdminId = Guid.Parse("00000000-0000-0000-0000-000000000002");

    // 测试凭据
    public const string TestDoctorUsername = "test_doctor";
    public const string TestDoctorPassword = "password123";
    public const string TestAdminUsername = "test_admin";
    public const string TestAdminPassword = "admin123";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public async Task InitializeAsync()
    {
        // 1. 创建 SQLite In-Memory 连接（保持打开状态以维持数据库）
        _sqliteConnection = new SqliteConnection("DataSource=:memory:");
        await _sqliteConnection.OpenAsync();

        // 2. 序列化 WebApplicationFactory 创建
        await InitGate.WaitAsync();
        try
        {
            Factory = new WebApplicationFactory<Program>()
                .WithWebHostBuilder(builder =>
                {
                    builder.UseEnvironment("Test");

                    // 注入 SQLite 连接字符串
                    builder.UseSetting(
                        "ConnectionStrings:DefaultConnection",
                        _sqliteConnection.ConnectionString);

                    builder.ConfigureServices(services =>
                    {
                        // 移除现有的 DbContext 注册
                        var descriptor = services.SingleOrDefault(
                            d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
                        if (descriptor != null)
                        {
                            services.Remove(descriptor);
                        }

                        // 注册 SQLite DbContext
                        services.AddDbContext<AppDbContext>(options =>
                        {
                            options.UseSqlite(_sqliteConnection);
                            options.EnableSensitiveDataLogging(false);
                            options.EnableDetailedErrors(true);
                        });

                        // 移除后台服务避免干扰
                        RemoveHostedServices(services);
                    });
                });

            // 3. 运行迁移
            await MigrateAsync();
        }
        finally
        {
            InitGate.Release();
        }

        // 4. 预置测试数据
        await SeedTestDataAsync();

        // 5. 创建匿名客户端
        ApiClient = Factory.CreateClient();
    }

    public async Task DisposeAsync()
    {
        ApiClient?.Dispose();

        if (Factory != null)
        {
            await Factory.DisposeAsync();
        }

        if (_sqliteConnection != null)
        {
            await _sqliteConnection.CloseAsync();
            await _sqliteConnection.DisposeAsync();
        }
    }

    /// <summary>
    /// 以测试医生身份登录，返回已认证的 HttpClient
    /// </summary>
    public Task<HttpClient> LoginAsDoctorAsync()
        => LoginAsAsync(TestDoctorUsername, TestDoctorPassword);

    /// <summary>
    /// 以测试管理员身份登录，返回已认证的 HttpClient
    /// </summary>
    public Task<HttpClient> LoginAsAdminAsync()
        => LoginAsAsync(TestAdminUsername, TestAdminPassword);

    /// <summary>
    /// 使用真实登录端点进行认证
    /// </summary>
    public async Task<HttpClient> LoginAsAsync(string username, string password)
    {
        using var loginClient = Factory.CreateClient();

        var loginRequest = new LoginRequest
        {
            UserName = username,
            Password = password
        };

        var response = await loginClient.PostAsJsonAsync("/api/v1/auth/login", loginRequest);
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();
        var apiResponse = JsonSerializer.Deserialize<ApiResponse<LoginResponse>>(content, JsonOptions);

        if (apiResponse?.Success != true || string.IsNullOrEmpty(apiResponse.Data?.Token))
        {
            throw new InvalidOperationException(
                $"登录失败，用户: '{username}'。响应: {content}");
        }

        var authenticatedClient = Factory.CreateClient();
        authenticatedClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", apiResponse.Data.Token);

        return authenticatedClient;
    }

    /// <summary>
    /// 在新的作用域中执行数据库操作
    /// </summary>
    public async Task<T> WithDbContextAsync<T>(Func<AppDbContext, Task<T>> action)
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await action(db);
    }

    /// <summary>
    /// 在新的作用域中执行数据库操作（无返回值）
    /// </summary>
    public async Task WithDbContextAsync(Func<AppDbContext, Task> action)
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await action(db);
    }

    /// <summary>
    /// 重置数据库状态（清除所有数据并重新预置）
    /// </summary>
    public async Task ResetAsync()
    {
        await WithDbContextAsync(async db =>
        {
            // 清除所有表数据
            await db.Database.ExecuteSqlRawAsync(@"
                DELETE FROM PrescriptionItems;
                DELETE FROM Prescriptions;
                DELETE FROM Consultations;
                DELETE FROM MedicalCases;
                DELETE FROM Patients;
                DELETE FROM Users;
            ");
        });

        await SeedTestDataAsync();
    }

    #region Private Methods

    private async Task MigrateAsync()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();
    }

    /// <summary>
    /// 预置测试数据
    /// </summary>
    private async Task SeedTestDataAsync()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var now = DateTime.UtcNow;

        // 添加测试医生
        db.Set<User>().Add(new User
        {
            Id = TestDoctorId,
            UserName = TestDoctorUsername,
            RealName = "测试医生",
            PinYinCode = "CSYS",
            Role = UserRole.Doctor,
            Email = "test_doctor@lybt.com",
            Status = CommonStatus.Enabled,
            PasswordHash = PasswordHelper.HashPassword(TestDoctorPassword, UserRole.Doctor),
            CreatedAt = now,
            UpdatedAt = now,
            CreatedBy = Guid.Empty,
            UpdatedBy = Guid.Empty,
            IsDeleted = false
        });

        // 添加测试管理员
        db.Set<User>().Add(new User
        {
            Id = TestAdminId,
            UserName = TestAdminUsername,
            RealName = "测试管理员",
            PinYinCode = "CSGLY",
            Role = UserRole.Admin,
            Email = "test_admin@lybt.com",
            Status = CommonStatus.Enabled,
            PasswordHash = PasswordHelper.HashPassword(TestAdminPassword, UserRole.Admin),
            CreatedAt = now,
            UpdatedAt = now,
            CreatedBy = Guid.Empty,
            UpdatedBy = Guid.Empty,
            IsDeleted = false
        });

        await db.SaveChangesAsync();
    }

    private static void RemoveHostedServices(IServiceCollection services)
    {
        var hostedServices = services
            .Where(d => d.ServiceType == typeof(IHostedService))
            .ToList();

        foreach (var svc in hostedServices)
        {
            services.Remove(svc);
        }
    }

    #endregion
}
