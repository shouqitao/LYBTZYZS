# 测试体系完整重构 Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** 将 37 个测试项目重构为 4 个核心项目，采用集成优先策略，用真实组件替代过度 Mock，覆盖全部核心业务流程。

**Architecture:** Server 端通过 WebApplicationFactory 测试完整 HTTP 管线，使用开发环境 SQL Server (LYBT_Test 数据库)。Desktop 端通过 SQLite InMemory 测试 ViewModel -> DataSource -> DB 链路。单元测试仅覆盖纯逻辑。NSubstitute 统一 Mock 框架。

**Tech Stack:** .NET 8, xUnit 2.9.3, NSubstitute 5.3.0, FluentAssertions 6.12.0, Microsoft.AspNetCore.Mvc.Testing 8.0.20, EF Core 8.0.20, SQLite, SQL Server

---

## Task 1: 创建 LYBT.Tests.Server.Integration 项目骨架

**Files:**
- Create: `tests/LYBT.Tests.Server.Integration/LYBT.Tests.Server.Integration.csproj`
- Create: `tests/LYBT.Tests.Server.Integration/Fixtures/WebApiFixture.cs`
- Create: `tests/LYBT.Tests.Server.Integration/Fixtures/TestAuthHandler.cs`
- Create: `tests/LYBT.Tests.Server.Integration/GlobalUsings.cs`
- Create: `tests/LYBT.Tests.Server.Integration/xunit.runner.json`
- Create: `tests/LYBT.Tests.Server.Integration/appsettings.Test.json`
- Modify: `LYBTZYZS.sln` (add project)

**Step 1: 创建 csproj**

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <IsPackable>false</IsPackable>
    <IsTestProject>true</IsTestProject>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" />
    <PackageReference Include="xunit" />
    <PackageReference Include="xunit.runner.visualstudio" />
    <PackageReference Include="FluentAssertions" />
    <PackageReference Include="NSubstitute" />
    <PackageReference Include="Microsoft.AspNetCore.Mvc.Testing" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" />
    <PackageReference Include="coverlet.collector" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\src\Server\Services\LYBT.WebAPI\LYBT.WebAPI.csproj" />
    <ProjectReference Include="..\..\src\Server\Core\LYBT.Infrastructure\LYBT.Infrastructure.csproj" />
    <ProjectReference Include="..\..\src\Server\Core\LYBT.Entities\LYBT.Entities.csproj" />
    <ProjectReference Include="..\..\src\Shared\LYBT.Shared.Models\LYBT.Shared.Models.csproj" />
  </ItemGroup>

  <ItemGroup>
    <Content Include="appsettings.Test.json">
      <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
    </Content>
  </ItemGroup>
</Project>
```

**Step 2: 创建 WebApiFixture**

WebApiFixture 是 Server 端所有集成测试的基础。使用开发环境 SQL Server，专用测试数据库 LYBT_Test。每个测试类通过 IClassFixture 共享同一个 WebApplicationFactory，每个测试用事务隔离。

```csharp
// tests/LYBT.Tests.Server.Integration/Fixtures/WebApiFixture.cs
using System.Data.Common;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using LYBT.Entities.Users;
using LYBT.Infrastructure.Data;

namespace LYBT.Tests.Server.Integration.Fixtures;

/// <summary>
/// Server端集成测试Fixture。
/// 使用开发环境SQL Server + LYBT_Test数据库。
/// 每个测试类共享同一个WebApplicationFactory实例。
/// </summary>
public class WebApiFixture : IAsyncLifetime
{
    private WebApplicationFactory<Program> _factory = null!;

    /// <summary>带Admin权限的默认HttpClient</summary>
    public HttpClient AdminClient { get; private set; } = null!;

    /// <summary>无认证的HttpClient</summary>
    public HttpClient AnonymousClient { get; private set; } = null!;

    public IServiceProvider Services => _factory.Services;

    // 固定测试用户ID
    public static readonly Guid AdminUserId = Guid.Parse("00000000-0000-0000-0000-000000000001");
    public static readonly Guid DoctorUserId = Guid.Parse("00000000-0000-0000-0000-000000000002");

    private const string JwtSecret = "VGVzdFNlY3JldEtleV9NaW5MZW5ndGgzMkNoYXJzX0ZvckpXVFRva2VuR2VuX0xZQlRfMTIzNDU2";

    public async Task InitializeAsync()
    {
        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Test");
                builder.ConfigureServices(services =>
                {
                    // 替换数据库连接为测试数据库
                    var descriptor = services.SingleOrDefault(
                        d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
                    if (descriptor != null)
                        services.Remove(descriptor);

                    services.AddDbContext<AppDbContext>(options =>
                    {
                        options.UseSqlServer(
                            "Server=localhost;Database=LYBT_Test;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true");
                    });

                    // 移除长运行后台服务
                    var hostedServices = services
                        .Where(d => d.ServiceType == typeof(Microsoft.Extensions.Hosting.IHostedService))
                        .ToList();
                    foreach (var svc in hostedServices)
                        services.Remove(svc);
                });
            });

        // 初始化测试数据库
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.EnsureDeletedAsync();
        await db.Database.EnsureCreatedAsync();

        // 种子管理员和医生
        await SeedDefaultUsers(db);

        // 创建客户端
        AdminClient = CreateAuthenticatedClient(UserRole.Admin, AdminUserId, "admin");
        AnonymousClient = _factory.CreateClient();
    }

    public async Task DisposeAsync()
    {
        AdminClient?.Dispose();
        AnonymousClient?.Dispose();

        // 清理测试数据库
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.EnsureDeletedAsync();

        await _factory.DisposeAsync();
    }

    /// <summary>以指定角色创建HttpClient</summary>
    public HttpClient CreateClientAs(UserRole role, Guid userId, string username = "testuser")
    {
        return CreateAuthenticatedClient(role, userId, username);
    }

    /// <summary>直接操作DbContext种子数据</summary>
    public async Task<T> SeedAsync<T>(Func<AppDbContext, Task<T>> seedAction)
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await seedAction(db);
    }

    /// <summary>直接操作DbContext种子数据(无返回值)</summary>
    public async Task SeedAsync(Func<AppDbContext, Task> seedAction)
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await seedAction(db);
    }

    /// <summary>获取Service实例</summary>
    public T GetService<T>() where T : notnull
    {
        var scope = Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<T>();
    }

    private HttpClient CreateAuthenticatedClient(UserRole role, Guid userId, string username)
    {
        var client = _factory.CreateClient();
        var token = GenerateJwtToken(role, userId, username);
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    private string GenerateJwtToken(UserRole role, Guid userId, string username)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(JwtSecret);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Name, username),
                new Claim(ClaimTypes.Role, role.ToString()),
            }),
            Expires = DateTime.UtcNow.AddHours(1),
            Issuer = "LYBT.WebAPI.Tests",
            Audience = "LYBT.Client.Tests",
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(key),
                SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    private static async Task SeedDefaultUsers(AppDbContext db)
    {
        var admin = new User
        {
            Id = AdminUserId,
            UserName = "admin",
            RealName = "系统管理员",
            Role = UserRole.Admin,
            Status = LYBT.Entities.Common.CommonStatus.Enabled,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("TestAdmin2025@"),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var doctor = new User
        {
            Id = DoctorUserId,
            UserName = "doctor",
            RealName = "测试医生",
            Role = UserRole.Doctor,
            Status = LYBT.Entities.Common.CommonStatus.Enabled,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("TestDoctor2025@"),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        db.Set<User>().AddRange(admin, doctor);
        await db.SaveChangesAsync();
    }
}
```

**Step 3: 创建 GlobalUsings.cs**

```csharp
// tests/LYBT.Tests.Server.Integration/GlobalUsings.cs
global using Xunit;
global using FluentAssertions;
global using NSubstitute;
global using System.Net;
global using System.Net.Http.Json;
```

**Step 4: 创建 xunit.runner.json**

```json
{
  "parallelizeAssembly": false,
  "parallelizeTestCollections": false,
  "maxParallelThreads": 1
}
```

注意: Server集成测试不并行执行，因为共享同一个SQL Server测试数据库。

**Step 5: 创建 appsettings.Test.json**

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=LYBT_Test;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true"
  },
  "Jwt": {
    "SecretKey": "VGVzdFNlY3JldEtleV9NaW5MZW5ndGgzMkNoYXJzX0ZvckpXVFRva2VuR2VuX0xZQlRfMTIzNDU2",
    "Issuer": "LYBT.WebAPI.Tests",
    "Audience": "LYBT.Client.Tests",
    "AccessTokenExpirationMinutes": 60
  },
  "Database": {
    "AutoMigrate": false,
    "EnsureCreatedInDevelopment": false
  },
  "DefaultPasswords": {
    "SysAdminPassword": "TestAdmin2025@",
    "NewUserPassword": "Test2025@"
  },
  "Security": {
    "RateLimiting": {
      "Enabled": false
    }
  }
}
```

**Step 6: 添加到 sln 并验证编译**

Run: `dotnet sln LYBTZYZS.sln add tests/LYBT.Tests.Server.Integration/LYBT.Tests.Server.Integration.csproj`
Run: `dotnet build tests/LYBT.Tests.Server.Integration/LYBT.Tests.Server.Integration.csproj`
Expected: 编译成功，0 errors

---

## Task 2: 创建 LYBT.Tests.Desktop.Integration 项目骨架

**Files:**
- Create: `tests/LYBT.Tests.Desktop.Integration/LYBT.Tests.Desktop.Integration.csproj`
- Create: `tests/LYBT.Tests.Desktop.Integration/Fixtures/DesktopFixture.cs`
- Create: `tests/LYBT.Tests.Desktop.Integration/GlobalUsings.cs`
- Create: `tests/LYBT.Tests.Desktop.Integration/xunit.runner.json`
- Modify: `LYBTZYZS.sln`

**Step 1: 创建 csproj**

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0-windows</TargetFramework>
    <UseWPF>true</UseWPF>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <IsPackable>false</IsPackable>
    <IsTestProject>true</IsTestProject>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" />
    <PackageReference Include="xunit" />
    <PackageReference Include="xunit.runner.visualstudio" />
    <PackageReference Include="FluentAssertions" />
    <PackageReference Include="NSubstitute" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.Sqlite" />
    <PackageReference Include="coverlet.collector" />
  </ItemGroup>

  <ItemGroup>
    <!-- Desktop Core -->
    <ProjectReference Include="..\..\src\Client\Desktop\Core\LYBT.Desktop.LocalData\LYBT.Desktop.LocalData.csproj" />
    <ProjectReference Include="..\..\src\Client\Desktop\Core\LYBT.Desktop.Foundation\LYBT.Desktop.Foundation.csproj" />
    <ProjectReference Include="..\..\src\Client\Desktop\Core\LYBT.Desktop.Infrastructure\LYBT.Desktop.Infrastructure.csproj" />
    <ProjectReference Include="..\..\src\Client\Desktop\Core\LYBT.Desktop.Models\LYBT.Desktop.Models.csproj" />
    <ProjectReference Include="..\..\src\Client\Desktop\Core\LYBT.Desktop.Contracts\LYBT.Desktop.Contracts.csproj" />
    <!-- Desktop Modules -->
    <ProjectReference Include="..\..\src\Client\Desktop\Modules\LYBT.Desktop.MedicalCase\LYBT.Desktop.MedicalCase.csproj" />
    <ProjectReference Include="..\..\src\Client\Desktop\Modules\LYBT.Desktop.Patients\LYBT.Desktop.Patients.csproj" />
    <ProjectReference Include="..\..\src\Client\Desktop\Modules\LYBT.Desktop.Herbs\LYBT.Desktop.Herbs.csproj" />
    <ProjectReference Include="..\..\src\Client\Desktop\Modules\LYBT.Desktop.Formula\LYBT.Desktop.Formula.csproj" />
    <ProjectReference Include="..\..\src\Client\Desktop\Modules\LYBT.Desktop.Auth\LYBT.Desktop.Auth.csproj" />
    <ProjectReference Include="..\..\src\Client\Desktop\Modules\LYBT.Desktop.Users\LYBT.Desktop.Users.csproj" />
    <ProjectReference Include="..\..\src\Client\Desktop\Modules\LYBT.Desktop.Sync\LYBT.Desktop.Sync.csproj" />
    <!-- Shared -->
    <ProjectReference Include="..\..\src\Shared\LYBT.Shared.Models\LYBT.Shared.Models.csproj" />
  </ItemGroup>
</Project>
```

**Step 2: 创建 DesktopFixture**

```csharp
// tests/LYBT.Tests.Desktop.Integration/Fixtures/DesktopFixture.cs
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using LYBT.Desktop.LocalData.Context;

namespace LYBT.Tests.Desktop.Integration.Fixtures;

/// <summary>
/// Desktop端集成测试Fixture。
/// 使用SQLite InMemory数据库，注册全部真实DataSource。
/// 仅Mock Prism基础设施。
/// </summary>
public class DesktopFixture : IAsyncLifetime
{
    private readonly List<SqliteConnection> _connections = new();
    private IServiceProvider _services = null!;

    public IServiceProvider Services => _services;

    public async Task InitializeAsync()
    {
        var services = new ServiceCollection();

        // SQLite InMemory 数据库
        var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        _connections.Add(connection);

        services.AddDbContext<LocalDbContext>(options =>
        {
            options.UseSqlite(connection);
        }, ServiceLifetime.Transient);

        // 日志
        services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));

        // 注册真实 DataSource (从实际项目中获取类型注册)
        RegisterRealDataSources(services);

        // Mock Prism 基础设施 (最小范围)
        RegisterPrismMocks(services);

        _services = services.BuildServiceProvider();

        // 初始化数据库
        var db = _services.GetRequiredService<LocalDbContext>();
        await db.Database.EnsureCreatedAsync();
    }

    public async Task DisposeAsync()
    {
        foreach (var conn in _connections)
        {
            await conn.CloseAsync();
            await conn.DisposeAsync();
        }
    }

    /// <summary>创建新的ServiceProvider (独立数据库连接)</summary>
    public async Task<IServiceProvider> CreateIsolatedProviderAsync()
    {
        var services = new ServiceCollection();

        var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        _connections.Add(connection);

        services.AddDbContext<LocalDbContext>(options =>
            options.UseSqlite(connection), ServiceLifetime.Transient);

        services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));

        RegisterRealDataSources(services);
        RegisterPrismMocks(services);

        var provider = services.BuildServiceProvider();
        var db = provider.GetRequiredService<LocalDbContext>();
        await db.Database.EnsureCreatedAsync();

        return provider;
    }

    public T GetService<T>() where T : notnull
        => _services.GetRequiredService<T>();

    public LocalDbContext CreateDbContext()
        => _services.GetRequiredService<LocalDbContext>();

    private static void RegisterRealDataSources(IServiceCollection services)
    {
        // TODO: 在Task实施时，从现有E2E Fixture中提取真实的DataSource注册
        // 示例模式:
        // services.AddTransient<IPatientDataSource, LocalPatientDataSource>();
        // services.AddTransient<IHerbDataSource, LocalHerbDataSource>();
        // services.AddTransient<IFormulaDataSource, LocalFormulaDataSource>();
        // services.AddTransient<IMedicalCaseDataSource, LocalMedicalCaseDataSource>();
        // services.AddTransient<IUserDataSource, LocalUserDataSource>();
    }

    private static void RegisterPrismMocks(IServiceCollection services)
    {
        // TODO: 在Task实施时，从现有E2E Fixture中提取Prism Mock注册
        // 仅Mock Prism框架基础设施，不Mock业务组件
        // services.AddSingleton(Substitute.For<IRegionManager>());
        // services.AddSingleton(Substitute.For<IDialogService>());
        // services.AddSingleton(Substitute.For<IEventAggregator>());
        // services.AddSingleton(Substitute.For<INavigationCoordinator>());
    }
}
```

**Step 3: 创建 GlobalUsings.cs 和 xunit.runner.json**

GlobalUsings.cs:
```csharp
global using Xunit;
global using FluentAssertions;
global using NSubstitute;
```

xunit.runner.json:
```json
{
  "parallelizeAssembly": true,
  "parallelizeTestCollections": true,
  "maxParallelThreads": 0
}
```

Desktop集成测试可以并行（每个测试有独立的SQLite连接）。

**Step 4: 添加到 sln 并验证编译**

Run: `dotnet sln LYBTZYZS.sln add tests/LYBT.Tests.Desktop.Integration/LYBT.Tests.Desktop.Integration.csproj`
Run: `dotnet build tests/LYBT.Tests.Desktop.Integration/LYBT.Tests.Desktop.Integration.csproj`
Expected: 编译成功

---

## Task 3: 创建 LYBT.Tests.Unit 项目骨架

**Files:**
- Create: `tests/LYBT.Tests.Unit/LYBT.Tests.Unit.csproj`
- Create: `tests/LYBT.Tests.Unit/GlobalUsings.cs`
- Modify: `LYBTZYZS.sln`

**Step 1: 创建 csproj**

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <IsPackable>false</IsPackable>
    <IsTestProject>true</IsTestProject>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" />
    <PackageReference Include="xunit" />
    <PackageReference Include="xunit.runner.visualstudio" />
    <PackageReference Include="FluentAssertions" />
    <PackageReference Include="coverlet.collector" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\src\Server\Core\LYBT.Entities\LYBT.Entities.csproj" />
    <ProjectReference Include="..\..\src\Shared\LYBT.Shared.Utilities\LYBT.Shared.Utilities.csproj" />
    <ProjectReference Include="..\..\src\Shared\LYBT.Shared.Models\LYBT.Shared.Models.csproj" />
  </ItemGroup>
</Project>
```

注意: 单元测试项目不引用 NSubstitute -- 纯逻辑测试不需要 Mock。

**Step 2: 添加到 sln 并验证编译**

Run: `dotnet sln LYBTZYZS.sln add tests/LYBT.Tests.Unit/LYBT.Tests.Unit.csproj`
Run: `dotnet build tests/LYBT.Tests.Unit/LYBT.Tests.Unit.csproj`

---

## Task 4: 创建 LYBT.Tests.Architecture 项目骨架

**Files:**
- Create: `tests/LYBT.Tests.Architecture/LYBT.Tests.Architecture.csproj`
- Create: `tests/LYBT.Tests.Architecture/GlobalUsings.cs`
- Modify: `LYBTZYZS.sln`

**Step 1: 创建 csproj**

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <IsPackable>false</IsPackable>
    <IsTestProject>true</IsTestProject>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" />
    <PackageReference Include="xunit" />
    <PackageReference Include="xunit.runner.visualstudio" />
    <PackageReference Include="FluentAssertions" />
    <PackageReference Include="NetArchTest.Rules" />
    <PackageReference Include="coverlet.collector" />
  </ItemGroup>

  <ItemGroup>
    <!-- 引用所有需要验证架构约束的程序集 -->
    <ProjectReference Include="..\..\src\Server\Core\LYBT.Entities\LYBT.Entities.csproj" />
    <ProjectReference Include="..\..\src\Server\Core\LYBT.Infrastructure\LYBT.Infrastructure.csproj" />
    <ProjectReference Include="..\..\src\Server\Services\LYBT.WebAPI\LYBT.WebAPI.csproj" />
    <ProjectReference Include="..\..\src\Shared\LYBT.Shared.Models\LYBT.Shared.Models.csproj" />
  </ItemGroup>
</Project>
```

**Step 2: 添加到 sln 并验证编译**

Run: `dotnet sln LYBTZYZS.sln add tests/LYBT.Tests.Architecture/LYBT.Tests.Architecture.csproj`
Run: `dotnet build tests/LYBT.Tests.Architecture/LYBT.Tests.Architecture.csproj`

---

## Task 5: Server 集成测试 - Auth 模块

**Files:**
- Create: `tests/LYBT.Tests.Server.Integration/Auth/AuthIntegrationTests.cs`

**Step 1: 编写测试**

```csharp
// tests/LYBT.Tests.Server.Integration/Auth/AuthIntegrationTests.cs
using LYBT.Tests.Server.Integration.Fixtures;

namespace LYBT.Tests.Server.Integration.Auth;

/// <summary>
/// 认证模块集成测试。
/// 验证完整的登录流程、Token生成、权限控制。
/// </summary>
public class AuthIntegrationTests : IClassFixture<WebApiFixture>
{
    private readonly WebApiFixture _fixture;

    public AuthIntegrationTests(WebApiFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Login_WithValidAdminCredentials_ShouldReturnToken()
    {
        // Arrange
        var request = new { userName = "admin", password = "TestAdmin2025@" };

        // Act
        var response = await _fixture.AnonymousClient
            .PostAsJsonAsync("/api/v1/auth/login", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<dynamic>();
        // Token应存在且非空
    }

    [Fact]
    public async Task Login_WithInvalidCredentials_ShouldReturn401()
    {
        var request = new { userName = "admin", password = "wrong_password" };

        var response = await _fixture.AnonymousClient
            .PostAsJsonAsync("/api/v1/auth/login", request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ProtectedEndpoint_WithoutToken_ShouldReturn401()
    {
        var response = await _fixture.AnonymousClient
            .GetAsync("/api/v1/users");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ProtectedEndpoint_WithValidToken_ShouldReturn200()
    {
        var response = await _fixture.AdminClient
            .GetAsync("/api/v1/users");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task AdminEndpoint_WithDoctorToken_ShouldReturn403()
    {
        using var doctorClient = _fixture.CreateClientAs(
            LYBT.Entities.Users.UserRole.Doctor,
            WebApiFixture.DoctorUserId,
            "doctor");

        // 假设创建用户需要Admin权限
        var request = new
        {
            userName = "newuser",
            realName = "新用户",
            password = "Test2025@",
            role = "Doctor"
        };

        var response = await doctorClient
            .PostAsJsonAsync("/api/v1/users", request);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
```

**Step 2: 运行测试验证**

Run: `dotnet test tests/LYBT.Tests.Server.Integration --filter "FullyQualifiedName~Auth" -v n`
Expected: 全部通过（需要根据实际API路由调整）

---

## Task 6: Server 集成测试 - Users 模块

**Files:**
- Create: `tests/LYBT.Tests.Server.Integration/Users/UserIntegrationTests.cs`

测试场景:
- CreateUser_WithValidData_ShouldPersistToDb
- CreateUser_DuplicateUsername_ShouldReturn400
- GetUsers_ShouldReturnPagedList
- GetUser_ById_ShouldReturnDetail
- UpdateUser_ShouldModifyFields
- DeleteUser_ShouldSoftDelete

---

## Task 7: Server 集成测试 - Patients 模块

**Files:**
- Create: `tests/LYBT.Tests.Server.Integration/Patients/PatientIntegrationTests.cs`

测试场景:
- CreatePatient_ShouldPersistToDb
- GetPatients_ShouldReturnPagedList
- SearchPatient_ByName_ShouldReturnMatches
- UpdatePatient_ShouldModifyAllFields
- DeletePatient_ShouldSoftDelete

---

## Task 8: Server 集成测试 - Herbs 模块

**Files:**
- Create: `tests/LYBT.Tests.Server.Integration/Herbs/HerbIntegrationTests.cs`

测试场景:
- CreateHerb_WithPrice_ShouldPersistToDb
- GetHerbs_ShouldReturnPagedList
- UpdateHerb_Price_ShouldPersist
- DeleteHerb_ShouldSoftDelete

---

## Task 9: Server 集成测试 - Formulas 模块

**Files:**
- Create: `tests/LYBT.Tests.Server.Integration/Formulas/FormulaIntegrationTests.cs`

测试场景:
- CreateFormula_WithHerbItems_ShouldPersistAll
- GetFormula_ShouldIncludeHerbItems
- UpdateFormula_ShouldReplaceHerbItems
- DeleteFormula_ShouldSoftDelete

---

## Task 10: Server 集成测试 - MedicalCases 模块 (核心)

**Files:**
- Create: `tests/LYBT.Tests.Server.Integration/MedicalCases/MedicalCaseIntegrationTests.cs`

这是最关键的测试，覆盖聚合根完整流程:

测试场景:
- CreateMedicalCase_WithConsultation_ShouldSharePrimaryKey
- CreateMedicalCase_WithPrescription_ShouldPersistHerbItems
- CreateMedicalCase_FullAggregate_ShouldPersistAll
- GetMedicalCase_ShouldIncludeAllNavigationProperties
- UpdateConsultation_ShouldPersistDiagnosisFields
- UpdatePrescription_ShouldReplaceHerbItems
- CaseNumber_ShouldAutoGenerate_WithCorrectFormat
- CompleteMedicalCase_ShouldChangeStatus
- FullBusinessFlow_CreatePatient_CreateHerbs_CreateCase_Complete

---

## Task 11: Server 集成测试 - Sync 模块

**Files:**
- Create: `tests/LYBT.Tests.Server.Integration/Sync/SyncIntegrationTests.cs`

测试场景:
- SyncPull_ShouldReturnChangedEntities
- SyncPush_ShouldPersistLocalChanges

---

## Task 12: Desktop 集成测试 - 完善 Fixture 注册

**Files:**
- Modify: `tests/LYBT.Tests.Desktop.Integration/Fixtures/DesktopFixture.cs`

从现有 `DesktopE2ETestFixture.cs` 和 `LocalModeTestFixture.cs` 提取:
- 所有真实 DataSource 注册
- Prism Mock 注册
- ICurrentUserProvider Mock
- ISessionManager Mock
- IDialogManager Mock (避免死锁)

---

## Task 13: Desktop 集成测试 - LocalMode DataSource

**Files:**
- Create: `tests/LYBT.Tests.Desktop.Integration/LocalMode/DataSourceIntegrationTests.cs`

测试场景:
- PatientDataSource_CRUD_ShouldPersistToSqlite
- HerbDataSource_CRUD_ShouldPersistToSqlite
- FormulaDataSource_WithHerbItems_ShouldPersistAll
- MedicalCaseDataSource_WithAggregate_ShouldPersistAll
- UserDataSource_CRUD_ShouldPersistToSqlite

---

## Task 14: Desktop 集成测试 - MedicalCase ViewModel

**Files:**
- Create: `tests/LYBT.Tests.Desktop.Integration/MedicalCases/MedicalCaseViewModelTests.cs`

测试场景:
- CreateMedicalCase_ViewModel_ShouldPersistToSqlite
- ConsultationPanel_FillDiagnosis_ShouldSave
- PrescriptionPanel_AddHerbItems_ShouldCalculatePrice
- FormulaImport_ShouldConvertToPrescriptionItems
- MedicalCaseList_LoadCommand_ShouldReturnAll

---

## Task 15: Desktop 集成测试 - 完整业务流程

**Files:**
- Create: `tests/LYBT.Tests.Desktop.Integration/BusinessFlow/FullBusinessFlowTests.cs`

从 `BusinessFlowE2ETests.cs` 迁移并增强:
- FullBusinessFlow_FromClinicOpeningToFirstCompletedMedicalCase
- MultiplePatients_MultipleCases_ShouldAllPersist
- FormulaImport_InMedicalCase_ShouldCreatePrescriptionItems

---

## Task 16: 单元测试迁移 - 验证器

**Files:**
- Create: `tests/LYBT.Tests.Unit/Validators/`

从现有 `LYBT.Shared.Validators.Tests` (214 tests) 迁移。
这些是纯逻辑测试，无 Mock，直接迁移。

---

## Task 17: 单元测试迁移 - 工具类

**Files:**
- Create: `tests/LYBT.Tests.Unit/Utilities/`

从现有 `LYBT.Shared.Utilities.Tests` (303 tests) 迁移。

---

## Task 18: 单元测试迁移 - 实体模型

**Files:**
- Create: `tests/LYBT.Tests.Unit/Entities/`

从现有 `LYBT.Entities.Tests` 中迁移纯逻辑部分:
- 实体构造验证
- 计算属性测试
- 编号生成逻辑

---

## Task 19: 架构测试迁移

**Files:**
- Create: `tests/LYBT.Tests.Architecture/DependencyTests.cs`
- Create: `tests/LYBT.Tests.Architecture/NamingConventionTests.cs`
- Create: `tests/LYBT.Tests.Architecture/LayerBoundaryTests.cs`
- Create: `tests/LYBT.Tests.Architecture/AggregateRootTests.cs`

从现有 `LYBT.ArchTests` (43 tests) 和 `LYBT.Server.ArchTests` 合并迁移。

---

## Task 20: 全量验证

**Step 1:** 编译全部 4 个新项目

Run: `dotnet build tests/LYBT.Tests.Unit && dotnet build tests/LYBT.Tests.Server.Integration && dotnet build tests/LYBT.Tests.Desktop.Integration && dotnet build tests/LYBT.Tests.Architecture`
Expected: 0 errors

**Step 2:** 运行全部新测试

Run: `dotnet test tests/LYBT.Tests.Unit -v n`
Run: `dotnet test tests/LYBT.Tests.Server.Integration -v n`
Run: `dotnet test tests/LYBT.Tests.Desktop.Integration -v n`
Run: `dotnet test tests/LYBT.Tests.Architecture -v n`
Expected: 全部通过

---

## Task 21: 旧项目清理

**Step 1:** 从 sln 移除旧测试项目

移除 37 个旧项目（逐个 `dotnet sln remove`）

**Step 2:** 删除旧测试目录

删除:
- `tests/UnitTests/` (全部)
- `tests/IntegrationTests/` (全部)
- `tests/Architecture/` (全部)
- `tests/CompatibilityTests/` (全部)
- `tests/BenchmarkTests/` (全部)
- `tests/PerformanceTests/` (全部)
- `tests/TestConfiguration/` (已集成到新项目)

保留:
- `tests/LYBT.Tests.Unit/`
- `tests/LYBT.Tests.Server.Integration/`
- `tests/LYBT.Tests.Desktop.Integration/`
- `tests/LYBT.Tests.Architecture/`

**Step 3:** 验证全量编译和测试

Run: `dotnet build LYBTZYZS.sln`
Run: `dotnet test LYBTZYZS.sln`
Expected: 0 errors, 全部测试通过

---

## Task 22: 文档更新

**Files:**
- Modify: `docs/reference/how-to/quality/test-layer-strategy.md` (升级到 v3.0)
- Update: `progress.md` (最终结果)
- Update: `findings.md` (最终决策)
- Update: `task_plan.md` (全部 Phase complete)

---

**Plan complete and saved to `docs/plans/2026-02-08-test-restructure-plan.md`.**
