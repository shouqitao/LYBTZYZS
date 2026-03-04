# Testing Trophy Redesign Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** 消灭 mock，重新设计测试架构为 Testing Trophy 模式，5 个测试项目合并为 3 个。

**Architecture:** 本地 SQL Server + Respawn 每测试重置 + 真实 HTTP 管线 + 真实登录。Server 零 mock，Desktop 仅保留 5 个 WPF 边界 mock。

**Tech Stack:** xunit, Respawn 7.x, FluentAssertions, WebApplicationFactory, SQL Server 2012 (local), SQLite InMemory (Desktop)

**Design Document:** docs/plans/2026-03-03-testing-trophy-redesign-design.md

---

## Phase 1: 基础设施搭建

### Task 1.1: 创建 LYBT.Tests.Server 项目

**Files:**
- Create: `tests/LYBT.Tests.Server/LYBT.Tests.Server.csproj`
- Modify: `LYBT.All.sln` (添加新项目)

**Step 1: 创建项目文件**

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <IsPackable>false</IsPackable>
    <IsTestProject>true</IsTestProject>
    <RootNamespace>LYBT.Tests.Server</RootNamespace>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.*" />
    <PackageReference Include="xunit" Version="2.*" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.*" />
    <PackageReference Include="FluentAssertions" Version="6.*" />
    <PackageReference Include="Microsoft.AspNetCore.Mvc.Testing" Version="8.*" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" Version="8.*" />
    <PackageReference Include="System.IdentityModel.Tokens.Jwt" Version="7.*" />
    <PackageReference Include="Microsoft.IdentityModel.Tokens" Version="7.*" />
    <PackageReference Include="BCrypt.Net-Next" Version="4.*" />
    <PackageReference Include="Respawn" Version="7.*" />
    <PackageReference Include="FluentValidation" Version="11.*" />
    <PackageReference Include="Bogus" Version="35.*" />
    <!-- 注意: 不引用 NSubstitute -->
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\src\Server\Services\LYBT.WebAPI\LYBT.WebAPI.csproj" />
    <ProjectReference Include="..\..\src\Server\Core\LYBT.Entities\LYBT.Entities.csproj" />
    <ProjectReference Include="..\..\src\Server\Core\LYBT.Infrastructure\LYBT.Infrastructure.csproj" />
    <ProjectReference Include="..\..\src\Shared\LYBT.Shared.Models\LYBT.Shared.Models.csproj" />
    <ProjectReference Include="..\..\src\Shared\LYBT.Shared.Utilities\LYBT.Shared.Utilities.csproj" />
    <ProjectReference Include="..\..\src\Shared\LYBT.Shared.Validators\LYBT.Shared.Validators.csproj" />
    <ProjectReference Include="..\..\src\Shared\LYBT.Shared.ExceptionHandling\LYBT.Shared.ExceptionHandling.csproj" />
    <ProjectReference Include="..\..\src\Shared\LYBT.Shared.Configuration\LYBT.Shared.Configuration.csproj" />
    <ProjectReference Include="..\..\src\Shared\LYBT.Shared.Logging\LYBT.Shared.Logging.csproj" />
    <ProjectReference Include="..\..\src\Shared\LYBT.Shared.Primitives\LYBT.Shared.Primitives.csproj" />
    <ProjectReference Include="..\..\src\Server\Modules\LYBT.Module.Auth\LYBT.Module.Auth.csproj" />
    <ProjectReference Include="..\..\src\Server\Modules\LYBT.Module.Users\LYBT.Module.Users.csproj" />
    <ProjectReference Include="..\..\src\Server\Modules\LYBT.Module.Patients\LYBT.Module.Patients.csproj" />
    <ProjectReference Include="..\..\src\Server\Modules\LYBT.Module.Herbs\LYBT.Module.Herbs.csproj" />
    <ProjectReference Include="..\..\src\Server\Modules\LYBT.Module.Formula\LYBT.Module.Formula.csproj" />
    <ProjectReference Include="..\..\src\Server\Modules\LYBT.Module.MedicalCase\LYBT.Module.MedicalCase.csproj" />
    <ProjectReference Include="..\..\src\Server\Modules\LYBT.Module.Sync\LYBT.Module.Sync.csproj" />
    <ProjectReference Include="..\TestConfiguration\LYBT.Tests.Configuration.csproj" />
  </ItemGroup>

  <ItemGroup>
    <Content Include="appsettings.Test.json">
      <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
    </Content>
  </ItemGroup>
</Project>
```

**Step 2: 创建目录结构**

```
tests/LYBT.Tests.Server/
  _Infrastructure/
  Auth/
  Users/
  Patients/
  MedicalCases/
  Herbs/
  Formulas/
  Sync/
  RateLimiting/
  PureLogic/
    Entities/
    Validators/
    Utilities/
    Infrastructure/
    Shared/
  appsettings.Test.json
```

**Step 3: 复制 appsettings.Test.json**

从 `tests/LYBT.Tests.Server.Integration/appsettings.Test.json` 复制，不做修改 (后续 Task 中调整)。

**Step 4: 添加到解决方案**

Run: `dotnet sln LYBT.All.sln add tests/LYBT.Tests.Server/LYBT.Tests.Server.csproj`
Expected: Project added successfully

**Step 5: 验证编译**

Run: `dotnet build tests/LYBT.Tests.Server/LYBT.Tests.Server.csproj`
Expected: Build succeeded

**Step 6: Commit**

```bash
git add tests/LYBT.Tests.Server/ LYBT.All.sln
git commit -m "test: scaffold LYBT.Tests.Server project (Testing Trophy Phase 1)"
```

---

### Task 1.2: 实现 ITestDatabaseProvider + LocalSqlServerProvider

**Files:**
- Create: `tests/LYBT.Tests.Server/_Infrastructure/ITestDatabaseProvider.cs`
- Create: `tests/LYBT.Tests.Server/_Infrastructure/LocalSqlServerProvider.cs`

**Step 1: 创建数据库提供者接口**

```csharp
// tests/LYBT.Tests.Server/_Infrastructure/ITestDatabaseProvider.cs
namespace LYBT.Tests.Server.Infrastructure;

/// <summary>
/// 测试数据库提供者抽象。当前使用本地 SQL Server，未来可切换 Testcontainers。
/// </summary>
public interface ITestDatabaseProvider : IAsyncLifetime
{
    string ConnectionString { get; }
}
```

**Step 2: 创建本地 SQL Server 提供者**

```csharp
// tests/LYBT.Tests.Server/_Infrastructure/LocalSqlServerProvider.cs
using Microsoft.Data.SqlClient;

namespace LYBT.Tests.Server.Infrastructure;

/// <summary>
/// 使用本地 SQL Server 实例作为测试数据库。
/// 每次测试套件启动创建独立数据库，结束时删除。
/// </summary>
public sealed class LocalSqlServerProvider : ITestDatabaseProvider
{
    private readonly string _databaseName;
    private readonly string _masterConnectionString;

    public LocalSqlServerProvider()
    {
        _databaseName = $"LYBT_Test_{DateTime.UtcNow:yyyyMMdd_HHmmss}_{Guid.NewGuid().ToString("N")[..8]}";
        _masterConnectionString = "Server=localhost;Database=master;Trusted_Connection=True;TrustServerCertificate=True";
    }

    public string ConnectionString =>
        $"Server=localhost;Database={_databaseName};Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true";

    public async Task InitializeAsync()
    {
        await using var connection = new SqlConnection(_masterConnectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = $"CREATE DATABASE [{_databaseName}]";
        await command.ExecuteNonQueryAsync();
    }

    public async Task DisposeAsync()
    {
        try
        {
            await using var connection = new SqlConnection(_masterConnectionString);
            await connection.OpenAsync();
            await using var command = connection.CreateCommand();
            // 强制断开所有连接后删除
            command.CommandText = $"""
                ALTER DATABASE [{_databaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
                DROP DATABASE [{_databaseName}];
                """;
            await command.ExecuteNonQueryAsync();
        }
        catch
        {
            // 清理失败不应阻止测试完成
        }
    }
}
```

**Step 3: 验证编译**

Run: `dotnet build tests/LYBT.Tests.Server/LYBT.Tests.Server.csproj`
Expected: Build succeeded

---

### Task 1.3: 实现 ServerFixture (核心)

**Files:**
- Create: `tests/LYBT.Tests.Server/_Infrastructure/ServerFixture.cs`
- Create: `tests/LYBT.Tests.Server/_Infrastructure/ServerTestCollection.cs`

**Step 1: 创建 ServerFixture**

```csharp
// tests/LYBT.Tests.Server/_Infrastructure/ServerFixture.cs
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Respawn;
using LYBT.Infrastructure.Data;
using LYBT.Shared.Models.Auth;

namespace LYBT.Tests.Server.Infrastructure;

/// <summary>
/// 服务端集成测试的核心 Fixture。
/// - Respawn 实现每测试数据隔离
/// - 真实登录获取 JWT token
/// - 通过 API 调用种子数据
/// </summary>
public sealed class ServerFixture : IAsyncLifetime
{
    private readonly LocalSqlServerProvider _dbProvider = new();
    private WebApplicationFactory<Program> _factory = null!;
    private Respawner _respawner = null!;

    // 默认用户凭证 (与 appsettings.Test.json 对齐)
    public const string AdminUsername = "admin";
    public const string AdminPassword = "TestAdmin2025@";
    public const string DoctorUsername = "doctor";
    public const string DoctorPassword = "TestDoctor2025@";
    public const string SysAdminUsername = "sysadmin";
    public const string SysAdminPassword = "TestAdmin2025@";

    public HttpClient AnonymousClient { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        // 1. 创建独立测试数据库
        await _dbProvider.InitializeAsync();

        // 2. 创建 WebApplicationFactory
        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Test");

                builder.UseSetting("ConnectionStrings:DefaultConnection", _dbProvider.ConnectionString);
                builder.UseSetting("Security:RateLimiting:Enabled", "false");

                builder.ConfigureServices(services =>
                {
                    // 替换 DbContext 连接字符串
                    var descriptor = services.SingleOrDefault(
                        d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
                    if (descriptor != null) services.Remove(descriptor);

                    services.AddDbContext<AppDbContext>(options =>
                        options.UseSqlServer(_dbProvider.ConnectionString));

                    // 移除后台服务 (避免干扰测试)
                    var hostedServices = services
                        .Where(d => d.ServiceType == typeof(IHostedService))
                        .ToList();
                    foreach (var service in hostedServices)
                        services.Remove(service);
                });
            });

        // 3. 执行数据库迁移 (一次性)
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await dbContext.Database.MigrateAsync();

        // 4. 创建 Respawner
        await using var connection = new SqlConnection(_dbProvider.ConnectionString);
        await connection.OpenAsync();
        _respawner = await Respawner.CreateAsync(connection, new RespawnerOptions
        {
            TablesToIgnore = [new Respawn.Graph.Table("__EFMigrationsHistory")],
            DbAdapter = DbAdapter.SqlServer
        });

        // 5. 创建匿名客户端
        AnonymousClient = _factory.CreateClient();

        // 6. 初始种子数据
        await SeedBaseDataAsync();
    }

    /// <summary>
    /// 每个测试前调用：重置数据 + 重新种子
    /// </summary>
    public async Task ResetAsync()
    {
        await using var connection = new SqlConnection(_dbProvider.ConnectionString);
        await connection.OpenAsync();
        await _respawner.ResetAsync(connection);
        await SeedBaseDataAsync();
    }

    /// <summary>
    /// 真实登录获取已认证的 HttpClient
    /// </summary>
    public async Task<HttpClient> LoginAsAsync(string username, string password)
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            UserName = username,
            Password = password
        });

        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<LoginResponse>();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", result!.AccessToken);

        return client;
    }

    /// <summary>
    /// 快捷方法：以管理员登录
    /// </summary>
    public Task<HttpClient> LoginAsAdminAsync()
        => LoginAsAsync(AdminUsername, AdminPassword);

    /// <summary>
    /// 快捷方法：以医生登录
    /// </summary>
    public Task<HttpClient> LoginAsDoctorAsync()
        => LoginAsAsync(DoctorUsername, DoctorPassword);

    /// <summary>
    /// 快捷方法：以超级管理员登录
    /// </summary>
    public Task<HttpClient> LoginAsSysAdminAsync()
        => LoginAsAsync(SysAdminUsername, SysAdminPassword);

    /// <summary>
    /// 获取 DI scope (用于直接数据库操作，仅限种子/验证)
    /// </summary>
    public IServiceScope CreateScope() => _factory.Services.CreateScope();

    public async Task DisposeAsync()
    {
        AnonymousClient?.Dispose();
        await _factory.DisposeAsync();
        await _dbProvider.DisposeAsync();
    }

    /// <summary>
    /// 通过 API 创建基础种子用户。走完整的生产路径:
    /// DatabaseInitializationService 创建 sysadmin，
    /// 然后用 sysadmin 通过 API 创建 admin 和 doctor。
    /// </summary>
    private async Task SeedBaseDataAsync()
    {
        // sysadmin 由 DatabaseInitializationService 的 AutoCreateOnStartup 自动创建
        // 配置在 appsettings.Test.json: SystemAdmin.AutoCreateOnStartup = true
        // 但因为我们移除了 HostedService，需要手动触发初始化
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // 检查是否已有 sysadmin (Respawn 清除后需要重新创建)
        var hasSysAdmin = await dbContext.Users
            .IgnoreQueryFilters()
            .AnyAsync(u => u.UserName == SysAdminUsername);

        if (!hasSysAdmin)
        {
            // 通过 DatabaseInitializationService 的逻辑创建 sysadmin
            // 复用生产代码路径
            var initService = scope.ServiceProvider
                .GetRequiredService<DatabaseInitializationService>();
            await initService.EnsureSystemAdminExistsAsync();
        }

        // 用 sysadmin 登录，通过 API 创建 admin 和 doctor
        var sysAdminClient = await LoginAsAsync(SysAdminUsername, SysAdminPassword);

        // 创建 admin
        var adminExists = await dbContext.Users.AnyAsync(u => u.UserName == AdminUsername);
        if (!adminExists)
        {
            await sysAdminClient.PostAsJsonAsync("/api/v1/users", new
            {
                UserName = AdminUsername,
                Password = AdminPassword,
                RealName = "测试管理员",
                Role = "Admin",
                Email = "admin@test.com"
            });
        }

        // 创建 doctor
        var doctorExists = await dbContext.Users.AnyAsync(u => u.UserName == DoctorUsername);
        if (!doctorExists)
        {
            await sysAdminClient.PostAsJsonAsync("/api/v1/users", new
            {
                UserName = DoctorUsername,
                Password = DoctorPassword,
                RealName = "测试医生",
                Role = "Doctor",
                Email = "doctor@test.com"
            });
        }

        sysAdminClient.Dispose();
    }

    // 内部响应模型 (仅用于反序列化登录响应)
    private sealed record LoginResponse(
        string AccessToken,
        string RefreshToken,
        string UserName,
        string Role);
}
```

**Step 2: 创建 Collection Definition**

```csharp
// tests/LYBT.Tests.Server/_Infrastructure/ServerTestCollection.cs
namespace LYBT.Tests.Server.Infrastructure;

[CollectionDefinition("Server")]
public sealed class ServerTestCollection : ICollectionFixture<ServerFixture>;
```

**Step 3: 验证编译**

Run: `dotnet build tests/LYBT.Tests.Server/LYBT.Tests.Server.csproj`
Expected: Build succeeded

---

### Task 1.4: 实现 IntegrationTestBase

**Files:**
- Create: `tests/LYBT.Tests.Server/_Infrastructure/IntegrationTestBase.cs`

**Step 1: 创建集成测试基类**

```csharp
// tests/LYBT.Tests.Server/_Infrastructure/IntegrationTestBase.cs
namespace LYBT.Tests.Server.Infrastructure;

/// <summary>
/// 所有服务端集成测试的基类。
/// 每个测试前自动调用 Respawn 重置数据库。
/// </summary>
[Collection("Server")]
public abstract class IntegrationTestBase : IAsyncLifetime
{
    protected ServerFixture Fixture { get; }

    protected IntegrationTestBase(ServerFixture fixture)
    {
        Fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        // 每个测试前重置数据库到干净状态
        await Fixture.ResetAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    // 便捷方法
    protected Task<HttpClient> LoginAsAdminAsync() => Fixture.LoginAsAdminAsync();
    protected Task<HttpClient> LoginAsDoctorAsync() => Fixture.LoginAsDoctorAsync();
    protected Task<HttpClient> LoginAsSysAdminAsync() => Fixture.LoginAsSysAdminAsync();
    protected HttpClient AnonymousClient => Fixture.AnonymousClient;
}
```

**Step 2: 验证编译**

Run: `dotnet build tests/LYBT.Tests.Server/LYBT.Tests.Server.csproj`
Expected: Build succeeded

---

### Task 1.5: 烟雾测试

**Files:**
- Create: `tests/LYBT.Tests.Server/Auth/AuthSmokeTests.cs`

**Step 1: 写烟雾测试**

```csharp
// tests/LYBT.Tests.Server/Auth/AuthSmokeTests.cs
using System.Net;
using System.Net.Http.Json;
using FluentAssertions;

namespace LYBT.Tests.Server.Auth;

public sealed class AuthSmokeTests : Infrastructure.IntegrationTestBase
{
    public AuthSmokeTests(Infrastructure.ServerFixture fixture) : base(fixture) { }

    [Fact]
    public async Task Login_WithValidAdmin_ShouldReturnToken()
    {
        // Act: 真实登录
        var client = await LoginAsAdminAsync();

        // Assert: 用返回的 token 访问受保护端点
        var response = await client.GetAsync("/api/v1/auth/validate");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Login_WithInvalidPassword_ShouldReturn401()
    {
        // Act
        var response = await AnonymousClient.PostAsJsonAsync("/api/v1/auth/login", new
        {
            UserName = "admin",
            Password = "WrongPassword123@"
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ProtectedEndpoint_WithoutToken_ShouldReturn401()
    {
        // Act
        var response = await AnonymousClient.GetAsync("/api/v1/users");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
```

**Step 2: 运行测试**

Run: `dotnet test tests/LYBT.Tests.Server/ --filter "AuthSmokeTests" -v normal`
Expected: 3 tests passed

**注意**: 如果失败，需要排查:
- SQL Server 是否可连接
- DatabaseInitializationService.EnsureSystemAdminExistsAsync() 是否可用
- 用户创建 API 的请求格式是否正确
- LoginResponse 的 JSON 属性名是否匹配

根据实际 API 响应格式调整 LoginResponse 和 SeedBaseDataAsync 中的请求体。

**Step 3: Commit**

```bash
git add tests/LYBT.Tests.Server/
git commit -m "test: implement ServerFixture with Respawn + real login (Testing Trophy Phase 1)"
```

---

## Phase 2: 服务端测试迁移

### Task 2.1: 迁移 Auth 集成测试

**Files:**
- Copy from: `tests/LYBT.Tests.Server.Integration/Auth/AuthIntegrationTests.cs` (15 tests)
- Copy from: `tests/LYBT.Tests.Server.Integration/Auth/AuthTokenAdvancedIntegrationTests.cs` (3 tests)
- Create: `tests/LYBT.Tests.Server/Auth/AuthIntegrationTests.cs`
- Create: `tests/LYBT.Tests.Server/Auth/AuthTokenAdvancedTests.cs`
- Create: `tests/LYBT.Tests.Server/Auth/AuthPasswordPolicyTests.cs` (NEW)

**Step 1: 迁移现有测试**

从 `tests/LYBT.Tests.Server.Integration/Auth/` 复制所有测试文件到 `tests/LYBT.Tests.Server/Auth/`。
修改:
- namespace: `LYBT.Tests.Server.Integration.Auth` → `LYBT.Tests.Server.Auth`
- 基类: 实现 `IClassFixture<WebApiFixture>` → 继承 `IntegrationTestBase`
- Fixture 引用: `_fixture.AdminClient` → `await LoginAsAdminAsync()`
- Collection: `[Collection("ServerIntegration")]` → 移除 (基类已有 `[Collection("Server")]`)

**迁移模式** (所有模块通用):
```csharp
// 旧:
[Collection("ServerIntegration")]
public class AuthIntegrationTests
{
    private readonly WebApiFixture _fixture;
    public AuthIntegrationTests(WebApiFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task Login_ValidAdmin_ReturnsToken()
    {
        var response = await _fixture.AdminClient.PostAsJsonAsync(...);
    }
}

// 新:
public sealed class AuthIntegrationTests : IntegrationTestBase
{
    public AuthIntegrationTests(ServerFixture fixture) : base(fixture) { }

    [Fact]
    public async Task Login_ValidAdmin_ReturnsToken()
    {
        var client = await LoginAsAdminAsync();
        var response = await client.PostAsJsonAsync(...);
    }
}
```

**Step 2: 新增密码策略集成测试** (替代 AuthServiceTests 中的 mock 测试)

```csharp
// tests/LYBT.Tests.Server/Auth/AuthPasswordPolicyTests.cs
namespace LYBT.Tests.Server.Auth;

public sealed class AuthPasswordPolicyTests : Infrastructure.IntegrationTestBase
{
    public AuthPasswordPolicyTests(Infrastructure.ServerFixture fixture) : base(fixture) { }

    [Theory]
    [InlineData("short")]           // 太短
    [InlineData("nouppercase1@")]   // 无大写
    [InlineData("NOLOWERCASE1@")]   // 无小写
    [InlineData("NoDigits@@@@")]    // 无数字
    public async Task CreateUser_WithWeakPassword_ShouldReturn400(string weakPassword)
    {
        var admin = await LoginAsAdminAsync();
        var response = await admin.PostAsJsonAsync("/api/v1/users", new
        {
            UserName = $"weakpwd_{Guid.NewGuid():N}",
            Password = weakPassword,
            RealName = "测试",
            Role = "Doctor",
            Email = $"test_{Guid.NewGuid():N}@test.com"
        });
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ChangePassword_WithSameAsOld_ShouldBeRejected()
    {
        // 用 admin 创建用户
        var admin = await LoginAsAdminAsync();
        var username = $"changepwd_{Guid.NewGuid():N}";
        var password = "InitialPass1@";
        await admin.PostAsJsonAsync("/api/v1/users", new
        {
            UserName = username,
            Password = password,
            RealName = "改密测试",
            Role = "Doctor",
            Email = $"{username}@test.com"
        });

        // 登录该用户
        var user = await Fixture.LoginAsAsync(username, password);

        // 尝试改为相同密码
        var response = await user.PutAsJsonAsync("/api/v1/users/change-password", new
        {
            OldPassword = password,
            NewPassword = password,
            ConfirmPassword = password
        });
        // 应被拒绝 (具体状态码取决于业务实现)
        response.IsSuccessStatusCode.Should().BeFalse();
    }
}
```

**Step 3: 运行测试**

Run: `dotnet test tests/LYBT.Tests.Server/ --filter "FullyQualifiedName~Auth" -v normal`
Expected: All auth tests pass

**Step 4: Commit**

```bash
git add tests/LYBT.Tests.Server/Auth/
git commit -m "test: migrate Auth integration tests to Testing Trophy"
```

---

### Task 2.2: 迁移 Users 集成测试

**Files:**
- Copy from: `tests/LYBT.Tests.Server.Integration/Users/UserIntegrationTests.cs` (28 tests)
- Create: `tests/LYBT.Tests.Server/Users/UserIntegrationTests.cs`
- Create: `tests/LYBT.Tests.Server/Users/UserPaginationTests.cs` (NEW)

**Step 1: 迁移 UserIntegrationTests**

同 Task 2.1 的迁移模式: 更新 namespace、基类、Fixture 引用。

**Step 2: 新增分页边界测试** (替代 UserServiceTests 中的 mock 测试)

```csharp
// tests/LYBT.Tests.Server/Users/UserPaginationTests.cs
namespace LYBT.Tests.Server.Users;

public sealed class UserPaginationTests : Infrastructure.IntegrationTestBase
{
    public UserPaginationTests(Infrastructure.ServerFixture fixture) : base(fixture) { }

    [Theory]
    [InlineData(0, 10)]    // page=0 无效
    [InlineData(-1, 10)]   // page 负数
    [InlineData(1, 0)]     // pageSize=0
    [InlineData(1, -1)]    // pageSize 负数
    public async Task GetUsers_WithInvalidPagination_ShouldReturn400(int page, int pageSize)
    {
        var admin = await LoginAsAdminAsync();
        var response = await admin.GetAsync($"/api/v1/users?page={page}&pageSize={pageSize}");
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetUsers_WithLargePage_ShouldReturnEmptyList()
    {
        var admin = await LoginAsAdminAsync();
        var response = await admin.GetAsync("/api/v1/users?page=9999&pageSize=10");
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        // 页码超出范围应返回空列表，不是错误
    }
}
```

**Step 3: 运行 + Commit**

Run: `dotnet test tests/LYBT.Tests.Server/ --filter "FullyQualifiedName~Users" -v normal`

```bash
git add tests/LYBT.Tests.Server/Users/
git commit -m "test: migrate Users integration tests to Testing Trophy"
```

---

### Task 2.3: 迁移 Patients 集成测试

**Files:**
- Copy from: `tests/LYBT.Tests.Server.Integration/Patients/PatientIntegrationTests.cs` (~24 tests)
- Create: `tests/LYBT.Tests.Server/Patients/PatientIntegrationTests.cs`

**Step 1: 迁移 + 更新** (同 Task 2.1 模式)

**Step 2: 运行 + Commit**

Run: `dotnet test tests/LYBT.Tests.Server/ --filter "FullyQualifiedName~Patients" -v normal`

```bash
git add tests/LYBT.Tests.Server/Patients/
git commit -m "test: migrate Patients integration tests to Testing Trophy"
```

---

### Task 2.4: 迁移 MedicalCases 集成测试

**Files:**
- Copy from: `tests/LYBT.Tests.Server.Integration/MedicalCases/*.cs` (~39+ tests)
- Create: `tests/LYBT.Tests.Server/MedicalCases/MedicalCaseIntegrationTests.cs`
- Create: `tests/LYBT.Tests.Server/MedicalCases/MedicalCasePermissionTests.cs`
- Create: `tests/LYBT.Tests.Server/MedicalCases/PrescriptionAggregateTests.cs`
- Create: `tests/LYBT.Tests.Server/MedicalCases/MedicalCaseStateMachineTests.cs` (NEW)

**Step 1: 迁移现有测试** (同 Task 2.1 模式)

**Step 2: 新增状态机集成测试** (替代 MedicalCaseCommandServiceTests + MedicalCaseStateServiceTests)

```csharp
// tests/LYBT.Tests.Server/MedicalCases/MedicalCaseStateMachineTests.cs
namespace LYBT.Tests.Server.MedicalCases;

public sealed class MedicalCaseStateMachineTests : Infrastructure.IntegrationTestBase
{
    public MedicalCaseStateMachineTests(Infrastructure.ServerFixture fixture) : base(fixture) { }

    [Fact]
    public async Task FullLifecycle_Active_Suspend_Resume_Complete()
    {
        var doctor = await LoginAsDoctorAsync();

        // 1. 创建患者 (前置条件)
        var patientResponse = await doctor.PostAsJsonAsync("/api/v1/patients", new
        {
            Name = "状态机测试患者",
            Gender = "Male",
            BirthDate = "1990-01-01"
        });
        patientResponse.EnsureSuccessStatusCode();
        var patient = await patientResponse.Content.ReadFromJsonAsync<dynamic>();
        var patientId = (string)patient!.data.id;

        // 2. 创建医案 (Active)
        var createResponse = await doctor.PostAsJsonAsync("/api/v1/medicalcases", new
        {
            PatientId = patientId,
            Consultation = new { ChiefComplaint = "状态机测试", Diagnosis = "测试诊断" }
        });
        createResponse.EnsureSuccessStatusCode();
        var medicalCase = await createResponse.Content.ReadFromJsonAsync<dynamic>();
        var caseId = (string)medicalCase!.data.id;

        // 3. 挂起 (Active -> Suspended)
        var suspendResponse = await doctor.PutAsync($"/api/v1/medicalcases/{caseId}/suspend", null);
        suspendResponse.EnsureSuccessStatusCode();

        // 4. 恢复 (Suspended -> Active)
        var resumeResponse = await doctor.PutAsync($"/api/v1/medicalcases/{caseId}/resume", null);
        resumeResponse.EnsureSuccessStatusCode();

        // 5. 关闭 (Active -> Completed)
        var closeResponse = await doctor.PutAsync($"/api/v1/medicalcases/{caseId}/close", null);
        closeResponse.EnsureSuccessStatusCode();

        // 6. 验证最终状态
        var getResponse = await doctor.GetAsync($"/api/v1/medicalcases/{caseId}");
        getResponse.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task CreateWhenActiveCase_ShouldReturn400()
    {
        var doctor = await LoginAsDoctorAsync();

        // 创建患者
        var patientResponse = await doctor.PostAsJsonAsync("/api/v1/patients", new
        {
            Name = "重复医案患者",
            Gender = "Female",
            BirthDate = "1985-06-15"
        });
        var patient = await patientResponse.Content.ReadFromJsonAsync<dynamic>();
        var patientId = (string)patient!.data.id;

        // 第一次创建 (成功)
        var firstCreate = await doctor.PostAsJsonAsync("/api/v1/medicalcases", new
        {
            PatientId = patientId,
            Consultation = new { ChiefComplaint = "第一次", Diagnosis = "诊断1" }
        });
        firstCreate.EnsureSuccessStatusCode();

        // 第二次创建 (应失败: 已有 Active 医案)
        var secondCreate = await doctor.PostAsJsonAsync("/api/v1/medicalcases", new
        {
            PatientId = patientId,
            Consultation = new { ChiefComplaint = "第二次", Diagnosis = "诊断2" }
        });
        secondCreate.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);
    }
}
```

**Step 3: 运行 + Commit**

Run: `dotnet test tests/LYBT.Tests.Server/ --filter "FullyQualifiedName~MedicalCases" -v normal`

```bash
git add tests/LYBT.Tests.Server/MedicalCases/
git commit -m "test: migrate MedicalCases integration tests + state machine to Testing Trophy"
```

---

### Task 2.5: 迁移 Herbs 集成测试

**Files:**
- Copy from: `tests/LYBT.Tests.Server.Integration/Herbs/HerbIntegrationTests.cs` (~17 tests)
- Create: `tests/LYBT.Tests.Server/Herbs/HerbIntegrationTests.cs`
- Create: `tests/LYBT.Tests.Server/Herbs/HerbReferenceProtectionTests.cs` (NEW)

**Step 1: 迁移现有测试** (同 Task 2.1 模式)

**Step 2: 新增引用保护测试** (替代 HerbServiceTests 中的 mock 测试)

```csharp
// tests/LYBT.Tests.Server/Herbs/HerbReferenceProtectionTests.cs
namespace LYBT.Tests.Server.Herbs;

/// <summary>
/// 验证: 被处方引用的药材不能被删除。
/// 替代旧的 HerbServiceTests.DeleteAsync_WithReferences_ShouldThrow mock 测试。
/// </summary>
public sealed class HerbReferenceProtectionTests : Infrastructure.IntegrationTestBase
{
    public HerbReferenceProtectionTests(Infrastructure.ServerFixture fixture) : base(fixture) { }

    [Fact]
    public async Task DeleteHerb_WhenReferencedByPrescription_ShouldBeRejected()
    {
        var admin = await LoginAsAdminAsync();
        var doctor = await LoginAsDoctorAsync();

        // 1. 创建药材
        var herbResponse = await admin.PostAsJsonAsync("/api/v1/herbs", new
        {
            Name = "引用保护测试药材",
            Category = "补益药",
            UnitPrice = 10.0
        });
        herbResponse.EnsureSuccessStatusCode();
        var herb = await herbResponse.Content.ReadFromJsonAsync<dynamic>();
        var herbId = (string)herb!.data.id;

        // 2. 创建患者 + 医案 + 处方 (引用该药材)
        var patientResponse = await doctor.PostAsJsonAsync("/api/v1/patients", new
        {
            Name = "引用保护患者",
            Gender = "Male",
            BirthDate = "1990-01-01"
        });
        var patient = await patientResponse.Content.ReadFromJsonAsync<dynamic>();
        var patientId = (string)patient!.data.id;

        var caseResponse = await doctor.PostAsJsonAsync("/api/v1/medicalcases", new
        {
            PatientId = patientId,
            Consultation = new { ChiefComplaint = "测试", Diagnosis = "测试" }
        });
        var medicalCase = await caseResponse.Content.ReadFromJsonAsync<dynamic>();
        var caseId = (string)medicalCase!.data.id;

        // 保存处方 (引用药材)
        await doctor.PutAsJsonAsync($"/api/v1/medicalcases/{caseId}", new
        {
            Prescription = new
            {
                Herbs = new[] { new { HerbId = herbId, Dosage = 10.0, Unit = "g" } }
            }
        });

        // 3. 尝试删除被引用的药材 (应被拒绝)
        var deleteResponse = await admin.DeleteAsync($"/api/v1/herbs/{herbId}");
        deleteResponse.IsSuccessStatusCode.Should().BeFalse();
    }
}
```

**Step 3: 运行 + Commit**

Run: `dotnet test tests/LYBT.Tests.Server/ --filter "FullyQualifiedName~Herbs" -v normal`

```bash
git add tests/LYBT.Tests.Server/Herbs/
git commit -m "test: migrate Herbs integration tests + reference protection to Testing Trophy"
```

---

### Task 2.6: 迁移 Formulas + 其他集成测试

**Files:**
- Copy from: `tests/LYBT.Tests.Server.Integration/Formulas/*.cs`
- Copy from: `tests/LYBT.Tests.Server.Integration/Sync/*.cs`
- Copy from: `tests/LYBT.Tests.Server.Integration/Health/*.cs`
- Copy from: `tests/LYBT.Tests.Server.Integration/Middleware/*.cs`
- Copy from: `tests/LYBT.Tests.Server.Integration/Diagnostics/*.cs`
- Copy from: `tests/LYBT.Tests.Server.Integration/Performance/*.cs`
- Copy from: `tests/LYBT.Tests.Server.Integration/Compatibility/*.cs`

**Step 1: 迁移所有剩余集成测试** (同 Task 2.1 模式)

每个模块:
- 更新 namespace
- 更新基类为 IntegrationTestBase
- 更新 Fixture 引用
- 登录方式改为 `await LoginAsXxxAsync()`

**Step 2: 迁移 RateLimiting** (独立 Fixture)

RateLimiting 需要独立的 Fixture (启用速率限制)。创建:
- `tests/LYBT.Tests.Server/RateLimiting/RateLimitingFixture.cs` (从旧 Fixture 迁移)
- `tests/LYBT.Tests.Server/RateLimiting/RateLimitingCollection.cs`
- `tests/LYBT.Tests.Server/RateLimiting/RateLimitingTests.cs`

**Step 3: 运行全部 + Commit**

Run: `dotnet test tests/LYBT.Tests.Server/ -v normal`
Expected: All tests pass

```bash
git add tests/LYBT.Tests.Server/
git commit -m "test: migrate remaining integration tests (Formulas, Sync, etc.) to Testing Trophy"
```

---

### Task 2.7: 迁移纯逻辑单元测试

**Files:**
- Copy from: `tests/LYBT.Tests.Unit/Entities/**` → `tests/LYBT.Tests.Server/PureLogic/Entities/`
- Copy from: `tests/LYBT.Tests.Unit/Shared/Validators/**` → `tests/LYBT.Tests.Server/PureLogic/Validators/`
- Copy from: `tests/LYBT.Tests.Unit/Utilities/**` → `tests/LYBT.Tests.Server/PureLogic/Utilities/`
- Copy from: `tests/LYBT.Tests.Unit/Shared/ExceptionHandling/**` → `tests/LYBT.Tests.Server/PureLogic/Shared/`
- Copy from: `tests/LYBT.Tests.Unit/Shared/Logging/**` → `tests/LYBT.Tests.Server/PureLogic/Shared/`
- Copy from: `tests/LYBT.Tests.Unit/Shared/Models/**` → `tests/LYBT.Tests.Server/PureLogic/Shared/`
- Copy from: `tests/LYBT.Tests.Unit/Shared/Configuration/**` → `tests/LYBT.Tests.Server/PureLogic/Shared/`
- Copy from: `tests/LYBT.Tests.Unit/Infrastructure/Serialization/**` → `tests/LYBT.Tests.Server/PureLogic/Infrastructure/`

这些测试零 mock 或极少 mock (NullLogger 等)，保持原样迁移即可。

**不迁移** (将在 Task 2.8 中删除):
- `tests/LYBT.Tests.Unit/Modules/Auth/Services/AuthServiceTests.cs` (mock-heavy)
- `tests/LYBT.Tests.Unit/Modules/Users/Services/UserServiceTests.cs` (mock-heavy)
- `tests/LYBT.Tests.Unit/Modules/Patients/Services/PatientServiceTests.cs` (mock-heavy)
- `tests/LYBT.Tests.Unit/Modules/MedicalCases/Services/MedicalCaseCommandServiceTests.cs` (mock-heavy)
- `tests/LYBT.Tests.Unit/Modules/Herbs/Services/HerbServiceTests.cs` (mock-heavy)
- `tests/LYBT.Tests.Unit/Modules/Formulas/Services/FormulaServiceTests.cs` (mock-heavy)
- `tests/LYBT.Tests.Unit/Modules/Patients/Controllers/PatientsControllerTests.cs` (mock-heavy)
- `tests/LYBT.Tests.Unit/Infrastructure/Repositories/BaseRepositoryTests.cs` (InMemory DB)
- `tests/LYBT.Tests.Unit/Infrastructure/Services/BaseServiceTests.cs` (mock)
- `tests/LYBT.Tests.Unit/Infrastructure/Services/CrossModuleQueryServiceTests.cs` (mock)

**需要决策** (保留或迁移):
- `tests/LYBT.Tests.Unit/Modules/Auth/Services/JwtServiceTests.cs` → 保留 (测试 JWT 生成逻辑，低 mock)
- `tests/LYBT.Tests.Unit/Modules/Auth/Services/TokenRevocationServiceTests.cs` → 保留 (SQLite DB)
- `tests/LYBT.Tests.Unit/Modules/Auth/Services/SecurityAuditServiceTests.cs` → 保留 (SQLite DB)
- `tests/LYBT.Tests.Unit/Modules/Auth/Services/SecurityAuditCleanupServiceTests.cs` → 保留 (SQLite DB)
- `tests/LYBT.Tests.Unit/Modules/Auth/Security/JwtOptionsValidationTests.cs` → 保留 (纯逻辑)
- `tests/LYBT.Tests.Unit/Modules/Herbs/Repositories/HerbRepositoryTests.cs` → 保留 (InMemory DB)
- `tests/LYBT.Tests.Unit/Modules/Patients/Repositories/PatientRepositoryTests.cs` → 保留
- `tests/LYBT.Tests.Unit/Modules/Sync/**` → 保留 (ChecksumHelper 纯逻辑)
- `tests/LYBT.Tests.Unit/WebAPI/**` → 保留 (中间件测试，低 mock)
- `tests/LYBT.Tests.Unit/Modules/MedicalCases/Services/MedicalCaseQueryServiceTests.cs` → 决策
- `tests/LYBT.Tests.Unit/Modules/MedicalCases/Services/MedicalCaseStateServiceTests.cs` → 决策
- `tests/LYBT.Tests.Unit/Modules/MedicalCases/Services/MedicalCasePrintServiceTests.cs` → 决策
- `tests/LYBT.Tests.Unit/Infrastructure/Data/DatabaseInitializationServiceTests.cs` → 保留 (SQLite)

**规则**: 如果测试用 `Substitute.For<IXxxRepository>()` 或 `Substitute.For<IXxxService>()`，则归类为 mock-heavy，由集成测试替代。如果只 mock Logger 或用真实 DB (SQLite/InMemory)，则保留。

**Step 1: 迁移纯逻辑测试** (更新 namespace)

**Step 2: 迁移低 mock 测试** (保留现有 mock 方式，因为它们 mock 的是 Logger 而非业务接口)

**Step 3: 运行 + Commit**

Run: `dotnet test tests/LYBT.Tests.Server/ -v normal`

```bash
git add tests/LYBT.Tests.Server/PureLogic/
git commit -m "test: migrate pure logic tests to Testing Trophy"
```

---

### Task 2.8: 验证服务端迁移完整性

**Step 1: 全量运行新测试**

Run: `dotnet test tests/LYBT.Tests.Server/ -v normal --logger "console;verbosity=detailed"`
Expected: All tests pass

**Step 2: 对比测试覆盖**

对比旧项目和新项目的测试覆盖情况:
- 旧: LYBT.Tests.Unit (~592) + LYBT.Tests.Server.Integration (~267) = ~859
- 新: LYBT.Tests.Server = 应 >= 500 (集成测试合并了 mock 单元测试覆盖的场景)

**Step 3: Commit**

```bash
git commit -m "test: complete server test migration to Testing Trophy"
```

---

## Phase 3: Desktop 测试重构

### Task 3.1: 创建 LYBT.Tests.Desktop 项目

**Files:**
- Create: `tests/LYBT.Tests.Desktop/LYBT.Tests.Desktop.csproj`
- Modify: `LYBT.All.sln`

**Step 1: 创建项目文件**

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0-windows</TargetFramework>
    <UseWPF>true</UseWPF>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <IsPackable>false</IsPackable>
    <IsTestProject>true</IsTestProject>
    <RootNamespace>LYBT.Tests.Desktop</RootNamespace>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.*" />
    <PackageReference Include="xunit" Version="2.*" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.*" />
    <PackageReference Include="FluentAssertions" Version="6.*" />
    <PackageReference Include="NSubstitute" Version="5.*" />
    <PackageReference Include="Xunit.StaFact" Version="1.*" />
    <PackageReference Include="System.IdentityModel.Tokens.Jwt" Version="7.*" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.Sqlite" Version="8.*" />
    <PackageReference Include="Microsoft.Data.Sqlite" Version="8.*" />
    <PackageReference Include="CommunityToolkit.Mvvm" Version="8.*" />
    <PackageReference Include="BCrypt.Net-Next" Version="4.*" />
    <PackageReference Include="Microsoft.Extensions.DependencyInjection" Version="8.*" />
    <PackageReference Include="Microsoft.Extensions.Logging" Version="8.*" />
    <PackageReference Include="Microsoft.Extensions.Configuration.Json" Version="8.*" />
    <!-- NSubstitute: 仅用于 WPF 边界 mock (5 个接口) -->
  </ItemGroup>

  <ItemGroup>
    <!-- Desktop 项目引用 -->
    <ProjectReference Include="..\..\src\Desktop\**\*.csproj" />
    <ProjectReference Include="..\..\src\Shared\**\*.csproj" />
    <ProjectReference Include="..\TestConfiguration\LYBT.Tests.Configuration.csproj" />
  </ItemGroup>
</Project>
```

注意: `<ProjectReference Include="..\..\src\Desktop\**\*.csproj" />` 需要根据实际 Desktop 项目路径展开为具体引用。在实施时检查 `src/Desktop/` 下的实际项目结构。

**Step 2: 创建目录结构**

```
tests/LYBT.Tests.Desktop/
  _Infrastructure/
    DesktopFixture.cs
    ViewModelTestBase.cs
  ViewModels/
  LocalData/
  PureLogic/
```

**Step 3: 添加到解决方案 + 验证编译**

Run: `dotnet sln LYBT.All.sln add tests/LYBT.Tests.Desktop/LYBT.Tests.Desktop.csproj`
Run: `dotnet build tests/LYBT.Tests.Desktop/LYBT.Tests.Desktop.csproj`

---

### Task 3.2: 实现 DesktopFixture

**Files:**
- Create: `tests/LYBT.Tests.Desktop/_Infrastructure/DesktopFixture.cs`

**设计**: 从 `DesktopE2ETestFixture` 演化而来，但:
- 消除 Repository/Service mock
- 使用真实 Repository + SQLite
- 仅保留 WPF 边界 mock (IRegionManager, IDialogService, IDialogManager, ICurrentUserProvider)
- 使用真实 EventAggregator (Prism)

具体实现参考现有 `tests/LYBT.Tests.Desktop.Integration/EndToEnd/Fixtures/DesktopE2ETestFixture.cs`，修改:
1. 移除所有 `Substitute.For<IXxxRepository>()` → 注册真实 Repository
2. 移除所有 `Substitute.For<IXxxService>()` → 注册真实 Service
3. 保留 `Substitute.For<IRegionManager>()` / `IDialogService` / `IDialogManager` / `ICurrentUserProvider`
4. `IEventAggregator` → `new EventAggregator()` (Prism 真实实现)

---

### Task 3.3: 迁移 Desktop 测试

**迁移映射:**

| 源 | 目标 | 处理 |
|----|------|------|
| `LYBT.Tests.Desktop.Unit/Auth/` | `LYBT.Tests.Desktop/ViewModels/Auth/` | 更新基类 |
| `LYBT.Tests.Desktop.Unit/Foundation/` | `LYBT.Tests.Desktop/PureLogic/Foundation/` | 保持不变 |
| `LYBT.Tests.Desktop.Unit/Infrastructure/` | `LYBT.Tests.Desktop/PureLogic/Infrastructure/` | 保持不变 |
| `LYBT.Tests.Desktop.Unit/LocalData/` | `LYBT.Tests.Desktop/LocalData/` | 保持不变 |
| `LYBT.Tests.Desktop.Unit/Shell/` | `LYBT.Tests.Desktop/ViewModels/Shell/` | 更新基类 |
| `LYBT.Tests.Desktop.Unit/MedicalCase/` | `LYBT.Tests.Desktop/ViewModels/MedicalCase/` | 更新基类 |
| `LYBT.Tests.Desktop.Unit/Patients/` | `LYBT.Tests.Desktop/ViewModels/Patients/` | 消除 repo mock |
| `LYBT.Tests.Desktop.Unit/Users/` | `LYBT.Tests.Desktop/ViewModels/Users/` | 消除 repo mock |
| `LYBT.Tests.Desktop.Unit/Herbs/` | `LYBT.Tests.Desktop/ViewModels/Herbs/` | 更新基类 |
| `LYBT.Tests.Desktop.Unit/Formula/` | `LYBT.Tests.Desktop/ViewModels/Formula/` | 更新基类 |
| `LYBT.Tests.Desktop.Integration/**` | `LYBT.Tests.Desktop/LocalData/` + `LYBT.Tests.Desktop/E2E/` | 合并 |

**ViewModel 测试改造模式:**

```csharp
// 旧:
public class PatientServiceTests
{
    private readonly IPatientRepository _repo = Substitute.For<IPatientRepository>();
    private readonly PatientService _service;

    [Fact]
    public async Task GetPagedAsync_ShouldReturnResults()
    {
        _repo.GetPagedAsync(Arg.Any<...>()).Returns(new PagedResult<...>(...));
        var result = await _service.GetPagedAsync(...);
        result.Items.Should().HaveCount(5);
    }
}

// 新:
public class PatientServiceTests : IClassFixture<DesktopFixture>
{
    private readonly DesktopFixture _fixture;

    [Fact]
    public async Task GetPagedAsync_ShouldReturnResults()
    {
        // Seed 5 patients via real DataSource
        await _fixture.SeedPatients(5);
        var service = _fixture.Resolve<PatientService>();
        var result = await service.GetPagedAsync(...);
        result.Items.Should().HaveCount(5);
    }
}
```

---

### Task 3.4: 验证 Desktop 迁移 + Commit

Run: `dotnet test tests/LYBT.Tests.Desktop/ -v normal`
Expected: All tests pass

```bash
git add tests/LYBT.Tests.Desktop/
git commit -m "test: create LYBT.Tests.Desktop with minimal mock (Testing Trophy Phase 3)"
```

---

## Phase 4: 清理 + 防护

### Task 4.1: 删除旧测试项目

**Files:**
- Delete: `tests/LYBT.Tests.Unit/` (整个目录)
- Delete: `tests/LYBT.Tests.Server.Integration/` (整个目录)
- Delete: `tests/LYBT.Tests.Desktop.Unit/` (整个目录)
- Delete: `tests/LYBT.Tests.Desktop.Integration/` (整个目录)
- Modify: `LYBT.All.sln` (移除旧项目引用)

**Step 1: 从解决方案移除**

```bash
dotnet sln LYBT.All.sln remove tests/LYBT.Tests.Unit/LYBT.Tests.Unit.csproj
dotnet sln LYBT.All.sln remove tests/LYBT.Tests.Server.Integration/LYBT.Tests.Server.Integration.csproj
dotnet sln LYBT.All.sln remove tests/LYBT.Tests.Desktop.Unit/LYBT.Tests.Desktop.Unit.csproj
dotnet sln LYBT.All.sln remove tests/LYBT.Tests.Desktop.Integration/LYBT.Tests.Desktop.Integration.csproj
```

**Step 2: 删除目录**

```bash
rm -rf tests/LYBT.Tests.Unit
rm -rf tests/LYBT.Tests.Server.Integration
rm -rf tests/LYBT.Tests.Desktop.Unit
rm -rf tests/LYBT.Tests.Desktop.Integration
```

**Step 3: 验证编译**

Run: `dotnet build LYBT.All.sln`
Expected: Build succeeded

---

### Task 4.2: 精简 TestConfiguration

**Files:**
- Delete or mark obsolete: `tests/TestConfiguration/TestBase.cs` (CreateMock<T>)
- Delete: `tests/TestConfiguration/IntegrationTestBase.cs` (已被 ServerFixture 替代)
- Delete: `tests/TestConfiguration/SqlServerTestDbContextFactory.cs` (已被 LocalSqlServerProvider 替代)
- Keep: `tests/TestConfiguration/AssertionHelpers/TestAssertions.cs`
- Keep: `tests/TestConfiguration/TestDataBuilders/BaseTestDataBuilder.cs`
- Keep: `tests/TestConfiguration/Database/SqliteTestDatabaseFactory.cs` (Desktop 仍需要)
- Keep: `tests/TestConfiguration/Wpf/WpfTestCollection.cs`
- Keep: `tests/TestConfiguration/ClientRepositoryTestBase.cs` (Desktop 可能需要)

---

### Task 4.3: 新增 AntiMockRuleTests

**Files:**
- Create: `tests/LYBT.Tests.Architecture/AntiMockRuleTests.cs`

```csharp
// tests/LYBT.Tests.Architecture/AntiMockRuleTests.cs
using System.Reflection;
using FluentAssertions;
using NetArchTest.Rules;

namespace LYBT.Tests.Architecture;

public sealed class AntiMockRuleTests
{
    [Fact]
    public void ServerTestProject_ShouldNotReference_NSubstitute()
    {
        // LYBT.Tests.Server 项目不应引用 NSubstitute
        var serverTestAssembly = Assembly.Load("LYBT.Tests.Server");
        var referencedAssemblies = serverTestAssembly.GetReferencedAssemblies();

        referencedAssemblies
            .Should().NotContain(a => a.Name == "NSubstitute",
                "Server tests must not use mocks - use real HTTP integration tests instead");
    }

    [Fact]
    public void ServerTestProject_ShouldNotContain_SubstituteFor()
    {
        // 双重检查: 代码中不应出现 Substitute.For
        var serverTestAssembly = Assembly.Load("LYBT.Tests.Server");

        var types = Types.InAssembly(serverTestAssembly)
            .That().HaveDependencyOn("NSubstitute")
            .GetTypes();

        types.Should().BeEmpty(
            "No class in LYBT.Tests.Server should reference NSubstitute");
    }
}
```

---

### Task 4.4: 更新文档

**Files:**
- Modify: `CLAUDE.md` - 更新测试命令、项目列表
- Modify: 各模块 README (如有)

**CLAUDE.md 测试段更新为:**

```markdown
## 构建与测试

```bash
# 编译
dotnet build LYBT.All.sln

# 测试 (3个测试项目)
dotnet test tests/LYBT.Tests.Server/           # 服务端 (集成 + 纯逻辑)
dotnet test tests/LYBT.Tests.Desktop/          # Desktop (ViewModel + 本地数据)
dotnet test tests/LYBT.Tests.Architecture/     # 架构约束

# 全量测试
dotnet test LYBT.All.sln --filter "FullyQualifiedName~LYBT.Tests"
```
```

---

### Task 4.5: 全量验证

**Step 1: 全量编译**

Run: `dotnet build LYBT.All.sln`
Expected: 0 errors, 0 warnings

**Step 2: 全量测试**

Run: `dotnet test LYBT.All.sln --filter "FullyQualifiedName~LYBT.Tests" -v normal`
Expected: All tests pass

**Step 3: 最终 Commit**

```bash
git add -A
git commit -m "test: complete Testing Trophy redesign - 5 projects merged to 3, zero server mocks"
```

---

## Summary

| Phase | Tasks | Estimated Time |
|-------|-------|---------------|
| Phase 1: Infrastructure | 5 tasks | 2 days |
| Phase 2: Server Migration | 8 tasks | 3 days |
| Phase 3: Desktop Refactoring | 4 tasks | 2 days |
| Phase 4: Cleanup + Prevention | 5 tasks | 1 day |
| **Total** | **22 tasks** | **~8 days** |

## Critical Path

```
Phase 1 (Infrastructure) -> Phase 2 (Server)
Phase 1 (Infrastructure) -> Phase 3 (Desktop) [可与 Phase 2 并行]
Phase 2 + Phase 3 -> Phase 4 (Cleanup)
```

## Rollback Plan

每个 Phase 结束有独立 commit。如果某个 Phase 失败:
1. `git revert` 该 Phase 的所有 commits
2. 旧项目仍存在 (Phase 4 之前不删除)
3. 可以回退到任意 Phase 的完成状态
