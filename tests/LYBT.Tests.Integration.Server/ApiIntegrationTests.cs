using LYBT.Infrastructure.Data;
using LYBT.WebAPI;
using Microsoft.Data.SqlClient;

namespace LYBT.Tests.Integration.Server;

/// <summary>
/// 纯后端API集成测试 - 测试WebAPI端点（不依赖Desktop层）
/// 使用 WebApplicationFactory + SQL Server 容器
/// </summary>
public class ApiIntegrationTests : IAsyncLifetime
{
    private WebApplicationFactory<Program> _factory = null!;
    private HttpClient _client = null!;
    private string _connectionString = null!;
    private string _databaseName = null!;

    public async Task InitializeAsync()
    {
        // 1. 创建唯一测试数据库
        var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
        var guidPart = Guid.NewGuid().ToString("N")[..8];
        _databaseName = $"LYBT_ApiTest_{timestamp}_{guidPart}";

        // 从环境变量获取SQL Server连接字符串（CI提供）
        var sqlServerHost = Environment.GetEnvironmentVariable("SQL_SERVER_HOST") ?? "localhost";
        var sqlServerPort = Environment.GetEnvironmentVariable("SQL_SERVER_PORT") ?? "1433";
        var sqlUser = Environment.GetEnvironmentVariable("SQL_USER") ?? "sa";
        var sqlPassword = Environment.GetEnvironmentVariable("SQL_PASSWORD") ?? "YourStrong@Passw0rd";

        var baseConnectionString = $"Server={sqlServerHost},{sqlServerPort};Database=master;User Id={sqlUser};Password={sqlPassword};Encrypt=False;TrustServerCertificate=True;MultipleActiveResultSets=true;";
        _connectionString = $"Server={sqlServerHost},{sqlServerPort};Database={_databaseName};User Id={sqlUser};Password={sqlPassword};Encrypt=False;TrustServerCertificate=True;MultipleActiveResultSets=true;";

        // 2. 创建数据库
        await using (var connection = new SqlConnection(baseConnectionString))
        {
            await connection.OpenAsync();
            var sql = $"CREATE DATABASE [{_databaseName}]";
            await using var command = new SqlCommand(sql, connection);
            await command.ExecuteNonQueryAsync();
        }

        // 3. 构建 WebApplicationFactory
        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Test");
                builder.UseSetting("ConnectionStrings:DefaultConnection", _connectionString);

                // 移除后台服务
                builder.ConfigureServices(services =>
                {
                    // 移除所有后台服务，避免干扰测试
                    var hostedServices = services.Where(sd => sd.ImplementationType?.Name?.Contains("HostedService") == true).ToList();
                    foreach (var service in hostedServices)
                    {
                        services.Remove(service);
                    }
                });
            });

        // 4. 运行数据库迁移
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<LYBTDbContext>();
        await dbContext.Database.MigrateAsync();

        _client = _factory.CreateClient();
    }

    public async Task DisposeAsync()
    {
        // 清理：删除测试数据库
        try
        {
            var sqlServerHost = Environment.GetEnvironmentVariable("SQL_SERVER_HOST") ?? "localhost";
            var sqlServerPort = Environment.GetEnvironmentVariable("SQL_SERVER_PORT") ?? "1433";
            var sqlUser = Environment.GetEnvironmentVariable("SQL_USER") ?? "sa";
            var sqlPassword = Environment.GetEnvironmentVariable("SQL_PASSWORD") ?? "YourStrong@Passw0rd";

            var baseConnectionString = $"Server={sqlServerHost},{sqlServerPort};Database=master;User Id={sqlUser};Password={sqlPassword};Encrypt=False;TrustServerCertificate=True;MultipleActiveResultSets=true;";

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
        catch
        {
            // 忽略清理错误
        }
        finally
        {
            await _factory.DisposeAsync();
        }
    }

    [Fact]
    public async Task HealthCheck_ReturnsSuccess()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/health");

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        content.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsToken()
    {
        // Arrange - 首先创建测试用户（如果不存在）
        await CreateTestUserIfNeeded();

        // Act
        var loginRequest = new
        {
            Username = "testadmin",
            Password = "TestAdmin2025@"
        };

        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", loginRequest);

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<JsonElement>();
        result.GetProperty("token").GetString().Should().NotBeNullOrEmpty();
        result.GetProperty("user").GetProperty("username").GetString().Should().Be("testadmin");
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

        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", loginRequest);

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ProtectedEndpoint_WithoutToken_ReturnsUnauthorized()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/users");

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.Unauthorized);
    }

    private async Task CreateTestUserIfNeeded()
    {
        // 这里可以通过直接数据库操作或调用用户注册API来创建测试用户
        // 为了简单起见，我们假设数据库迁移已经创建了初始用户
        // 实际项目中可能需要更复杂的setup
    }
}
