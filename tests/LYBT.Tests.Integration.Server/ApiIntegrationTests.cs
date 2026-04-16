using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using LYBT.Infrastructure.Data;
using System.Net.Http.Json;

namespace LYBT.Tests.Integration.Server;

/// <summary>
/// 纯后端API集成测试 - 测试WebAPI端点（不依赖Desktop层）
/// 使用 WebApplicationFactory + SQL Server 容器
/// </summary>
[CollectionDefinition("ApiIntegration")]
public class ApiIntegrationCollection : ICollectionFixture<ApiTestFixture> { }

/// <summary>
/// API测试Fixture - 管理测试数据库和应用工厂
/// </summary>
public class ApiTestFixture : IAsyncLifetime
{
    private WebApplicationFactory<Program> _factory = null!;
    private string _databaseName = null!;

    public HttpClient Client { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        // 1. 创建唯一测试数据库
        var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
        var guidPart = Guid.NewGuid().ToString("N")[..8];
        _databaseName = $"LYBT_ApiTest_{timestamp}_{guidPart}";

        // 2. 从环境变量获取连接字符串
        var envConnectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");
        if (string.IsNullOrEmpty(envConnectionString))
        {
            throw new InvalidOperationException("ConnectionStrings__DefaultConnection environment variable is not set.");
        }

        // 3. 解析连接字符串
        var builder = new SqlConnectionStringBuilder(envConnectionString);
        var baseConnectionString = builder.ConnectionString;
        baseConnectionString = baseConnectionString.Replace($"Database={builder.InitialCatalog}", "Database=master");

        // 4. 构建测试数据库连接字符串
        var testConnectionString = envConnectionString.Replace($"Database={builder.InitialCatalog}", $"Database={_databaseName}");

        // 5. 创建数据库
        await using (var connection = new SqlConnection(baseConnectionString))
        {
            await connection.OpenAsync();
            var sql = $"CREATE DATABASE [{_databaseName}]";
            await using var command = new SqlCommand(sql, connection);
            await command.ExecuteNonQueryAsync();
        }

        // 6. 构建 WebApplicationFactory 并设置连接字符串
        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(webHost =>
            {
                webHost.UseEnvironment("Test");
                webHost.ConfigureAppConfiguration((context, config) =>
                {
                    config.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["ConnectionStrings:DefaultConnection"] = testConnectionString
                    });
                });

                // 移除后台服务，避免干扰测试
                webHost.ConfigureServices(services =>
                {
                    var hostedServices = services
                        .Where(sd => sd.ImplementationType?.Name?.Contains("HostedService") == true ||
                                     sd.ServiceType?.Name?.Contains("HostedService") == true)
                        .ToList();

                    foreach (var service in hostedServices)
                    {
                        services.Remove(service);
                    }
                });
            });

        // 7. 运行数据库迁移
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<LYBTDbContext>();
        await dbContext.Database.MigrateAsync();

        Client = _factory.CreateClient();
    }

    public async Task DisposeAsync()
    {
        // 清理：删除测试数据库
        try
        {
            var envConnectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");
            if (!string.IsNullOrEmpty(envConnectionString))
            {
                var builder = new SqlConnectionStringBuilder(envConnectionString);
                var baseConnectionString = builder.ConnectionString;
                baseConnectionString = baseConnectionString.Replace($"Database={builder.InitialCatalog}", "Database=master");

                await using var connection = new SqlConnection(baseConnectionString);
                await connection.OpenAsync();

                // 关闭所有连接
                var sql = $"ALTER DATABASE [{_databaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;";
                await using var command1 = new SqlCommand(sql, connection);
                await command1.ExecuteNonQueryAsync();

                // 删除数据库
                sql = $"DROP DATABASE [{_databaseName}]";
                await using var command2 = new SqlCommand(sql, connection);
                await command2.ExecuteNonQueryAsync();
            }
        }
        catch
        {
            // 忽略清理错误
        }
        finally
        {
            await _factory.DisposeAsync();
        }
    }
}

/// <summary>
/// 基础API测试类
/// </summary>
public class ApiTests : IClassFixture<ApiTestFixture>
{
    private readonly ApiTestFixture _fixture;

    public ApiTests(ApiTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task HealthCheck_ReturnsSuccess()
    {
        // Act
        var response = await _fixture.Client.GetAsync("/api/v1/health");

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        content.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task ProtectedEndpoint_WithoutToken_ReturnsUnauthorized()
    {
        // Act
        var response = await _fixture.Client.GetAsync("/api/v1/users");

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_WithInvalidCredentials_ReturnsUnauthorized()
    {
        // Act
        var loginRequest = new
        {
            Username = "nonexistent",
            Password = "WrongPassword"
        };

        var response = await _fixture.Client.PostAsJsonAsync("/api/v1/auth/login", loginRequest);

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.Unauthorized);
    }
}
