# Test Restructuring Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Delete all local mode and mock-heavy tests, create Desktop+Server integration tests, rewrite pure logic tests.

**Architecture:** Phase 2 creates a new LYBT.Tests.Integration project using WebApplicationFactory + Refit to test Desktop RemoteDataSource -> Server API full chain. Phase 3 cleans up Desktop tests by deleting local mode tests (DataSource layer will be removed in SYNC-D02), mock-heavy ViewModel tests, and rewriting pure logic tests. Phase 4 verifies everything passes.

**Tech Stack:** xUnit, FluentAssertions, Respawn, WebApplicationFactory, Refit, SQL Server, NSubstitute (whitelist only)

**Pre-requisites:**
- Server 1017 tests passing (Phase 1 complete)
- Branch: `feature/desktop-test-cleanup`

---

## Phase 2: Desktop + Server Integration Tests

### Task 1: Create LYBT.Tests.Integration project

**Files:**
- Create: `tests/LYBT.Tests.Integration/LYBT.Tests.Integration.csproj`
- Create: `tests/LYBT.Tests.Integration/GlobalUsings.cs`
- Modify: `LYBT.All.sln` (add project)

**Step 1: Create project directory**

Run: `mkdir -p tests/LYBT.Tests.Integration`

**Step 2: Create .csproj file**

Create `tests/LYBT.Tests.Integration/LYBT.Tests.Integration.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0-windows</TargetFramework>
    <UseWPF>true</UseWPF>
    <IsPackable>false</IsPackable>
    <IsTestProject>true</IsTestProject>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" />
    <PackageReference Include="xunit" />
    <PackageReference Include="xunit.runner.visualstudio">
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>
    <PackageReference Include="FluentAssertions" />
    <PackageReference Include="Microsoft.AspNetCore.Mvc.Testing" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" />
    <PackageReference Include="Respawn" />
    <PackageReference Include="Refit" />
    <PackageReference Include="Refit.HttpClientFactory" />
    <PackageReference Include="System.IdentityModel.Tokens.Jwt" />
  </ItemGroup>

  <ItemGroup>
    <!-- Server (WebApplicationFactory target) -->
    <ProjectReference Include="..\..\src\Server\Services\LYBT.WebAPI\LYBT.WebAPI.csproj" />
    <ProjectReference Include="..\..\src\Server\Core\LYBT.Infrastructure\LYBT.Infrastructure.csproj" />
    <ProjectReference Include="..\..\src\Server\Core\LYBT.Entities\LYBT.Entities.csproj" />
    <!-- Desktop Client layers (Refit API + DataSource) -->
    <ProjectReference Include="..\..\src\Client\Desktop\Core\LYBT.Desktop.Contracts\LYBT.Desktop.Contracts.csproj" />
    <ProjectReference Include="..\..\src\Client\Desktop\Core\LYBT.Desktop.Infrastructure\LYBT.Desktop.Infrastructure.csproj" />
    <!-- Shared -->
    <ProjectReference Include="..\..\src\Shared\LYBT.Shared.Models\LYBT.Shared.Models.csproj" />
  </ItemGroup>

  <ItemGroup>
    <None Include="..\..\src\Server\Services\LYBT.WebAPI\appsettings.Test.json" Link="appsettings.Test.json">
      <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
    </None>
  </ItemGroup>
</Project>
```

**Step 3: Create GlobalUsings.cs**

Create `tests/LYBT.Tests.Integration/GlobalUsings.cs`:

```csharp
global using FluentAssertions;
global using Xunit;
```

**Step 4: Add project to solution**

Run: `dotnet sln LYBT.All.sln add tests/LYBT.Tests.Integration/LYBT.Tests.Integration.csproj`

**Step 5: Verify build**

Run: `dotnet build tests/LYBT.Tests.Integration/`
Expected: BUILD SUCCEEDED

**Step 6: Commit**

```
test: scaffold LYBT.Tests.Integration project
```

---

### Task 2: Create IntegrationFixture

**Files:**
- Create: `tests/LYBT.Tests.Integration/_Infrastructure/IntegrationFixture.cs`
- Create: `tests/LYBT.Tests.Integration/_Infrastructure/IntegrationTestBase.cs`
- Create: `tests/LYBT.Tests.Integration/_Infrastructure/IntegrationTestCollection.cs`

**Context:**
- Reference `tests/LYBT.Tests.Server/_Infrastructure/ServerFixture.cs` for WebApplicationFactory pattern
- Reference `tests/LYBT.Tests.Server/_Infrastructure/IntegrationTestBase.cs` for base class pattern
- The fixture must: create WebApplicationFactory<Program>, manage SQL Server DB, Respawn reset, create authenticated Refit API clients

**Step 1: Create IntegrationFixture**

Create `tests/LYBT.Tests.Integration/_Infrastructure/IntegrationFixture.cs`:

```csharp
using System.Net.Http.Headers;
using System.Text.Json;
using LYBT.Desktop.Contracts.Api;
using LYBT.Infrastructure.Data;
using LYBT.Shared.Models.Auth;
using LYBT.Shared.Models.Common;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Refit;
using Respawn;

namespace LYBT.Tests.Integration;

public sealed class IntegrationFixture : IAsyncLifetime
{
    private WebApplicationFactory<Program> _factory = null!;
    private Respawner _respawner = null!;
    private string _connectionString = null!;

    // Test user credentials (same as ServerFixture)
    public static readonly Guid AdminUserId = new("00000000-0000-0000-0000-000000000001");
    public static readonly Guid DoctorUserId = new("00000000-0000-0000-0000-000000000002");
    private const string AdminUsername = "admin";
    private const string AdminPassword = "TestAdmin2025@";
    private const string DoctorUsername = "doctor";
    private const string DoctorPassword = "TestDoctor2025@";

    public HttpClient AnonymousClient { get; private set; } = null!;
    public IServiceProvider Services => _factory.Services;

    public async Task InitializeAsync()
    {
        // Use unique DB per test run
        var dbName = $"LYBT_Integration_{Guid.NewGuid():N}";
        _connectionString = $"Server=(localdb)\\MSSQLLocalDB;Database={dbName};Trusted_Connection=True;MultipleActiveResultSets=true";

        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Test");
                builder.ConfigureServices(services =>
                {
                    // Replace DB connection string
                    var descriptor = services.SingleOrDefault(
                        d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
                    if (descriptor != null) services.Remove(descriptor);

                    services.AddDbContext<AppDbContext>(options =>
                        options.UseSqlServer(_connectionString));
                });
            });

        // Trigger host build + migrate
        AnonymousClient = _factory.CreateClient();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();

        // Setup Respawner
        _respawner = await Respawner.CreateAsync(_connectionString,
            new RespawnerOptions
            {
                TablesToIgnore = ["__EFMigrationsHistory"],
                DbAdapter = DbAdapter.SqlServer
            });

        await SeedBaseDataAsync();
    }

    public async Task ResetAsync()
    {
        await _respawner.ResetAsync(_connectionString);
        await SeedBaseDataAsync();
    }

    public async Task<HttpClient> LoginAsAdminAsync()
        => await LoginAsAsync(AdminUsername, AdminPassword);

    public async Task<HttpClient> LoginAsDoctorAsync()
        => await LoginAsAsync(DoctorUsername, DoctorPassword);

    public async Task<HttpClient> LoginAsAsync(string username, string password)
    {
        var client = _factory.CreateClient();
        var loginRequest = new LoginRequest { Username = username, Password = password };
        var json = JsonSerializer.Serialize(loginRequest);
        var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
        var response = await client.PostAsync("/api/v1/auth/login", content);
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<ApiResponse<LoginResponse>>(body,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        var token = result!.Data!.Token;

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    /// <summary>
    /// Create Refit API client from authenticated HttpClient.
    /// Usage: var api = fixture.CreateApi<IPatientApi>(authenticatedClient);
    /// </summary>
    public T CreateApi<T>(HttpClient client) where T : class
        => RestService.For<T>(client, new RefitSettings
        {
            ContentSerializer = new SystemTextJsonContentSerializer(
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
        });

    public async Task WithDbContextAsync(Func<AppDbContext, Task> action)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await action(db);
    }

    public async Task<T> WithDbContextAsync<T>(Func<AppDbContext, Task<T>> action)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await action(db);
    }

    private async Task SeedBaseDataAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Upsert test users (SysAdmin, Admin, Doctor)
        // Reuse same pattern as ServerFixture.SeedBaseDataAsync()
        var sysAdmin = await db.Users.FindAsync(new Guid("00000000-0000-0000-0000-000000000099"));
        if (sysAdmin == null)
        {
            db.Users.Add(new LYBT.Entities.Models.User
            {
                Id = new Guid("00000000-0000-0000-0000-000000000099"),
                Username = "sysadmin",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("TestAdmin2025@"),
                FullName = "System Admin",
                Role = "SuperAdmin",
                IsActive = true
            });
        }

        var admin = await db.Users.FindAsync(AdminUserId);
        if (admin == null)
        {
            db.Users.Add(new LYBT.Entities.Models.User
            {
                Id = AdminUserId,
                Username = AdminUsername,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(AdminPassword),
                FullName = "Test Admin",
                Role = "Admin",
                IsActive = true
            });
        }

        var doctor = await db.Users.FindAsync(DoctorUserId);
        if (doctor == null)
        {
            db.Users.Add(new LYBT.Entities.Models.User
            {
                Id = DoctorUserId,
                Username = DoctorUsername,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(DoctorPassword),
                FullName = "Test Doctor",
                Role = "Doctor",
                IsActive = true
            });
        }

        await db.SaveChangesAsync();
    }

    public async Task DisposeAsync()
    {
        AnonymousClient?.Dispose();

        // Drop test database
        try
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await db.Database.EnsureDeletedAsync();
        }
        catch { /* best effort */ }

        await _factory.DisposeAsync();
    }
}
```

**Step 2: Create test collection**

Create `tests/LYBT.Tests.Integration/_Infrastructure/IntegrationTestCollection.cs`:

```csharp
namespace LYBT.Tests.Integration;

[CollectionDefinition("Integration")]
public class IntegrationTestCollection : ICollectionFixture<IntegrationFixture>;
```

**Step 3: Create test base class**

Create `tests/LYBT.Tests.Integration/_Infrastructure/IntegrationTestBase.cs`:

```csharp
using System.Text.Json;
using LYBT.Desktop.Contracts.Api;

namespace LYBT.Tests.Integration;

[Collection("Integration")]
public abstract class IntegrationTestBase : IAsyncLifetime
{
    protected IntegrationFixture Fixture { get; }

    protected static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    protected IntegrationTestBase(IntegrationFixture fixture)
    {
        Fixture = fixture;
    }

    public async Task InitializeAsync() => await Fixture.ResetAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    protected async Task<(HttpClient Client, T Api)> LoginAsAdminWithApiAsync<T>() where T : class
    {
        var client = await Fixture.LoginAsAdminAsync();
        var api = Fixture.CreateApi<T>(client);
        return (client, api);
    }

    protected async Task<(HttpClient Client, T Api)> LoginAsDoctorWithApiAsync<T>() where T : class
    {
        var client = await Fixture.LoginAsDoctorAsync();
        var api = Fixture.CreateApi<T>(client);
        return (client, api);
    }
}
```

**Step 4: Verify build**

Run: `dotnet build tests/LYBT.Tests.Integration/`
Expected: BUILD SUCCEEDED

**Step 5: Commit**

```
test: add IntegrationFixture with WebApplicationFactory + Refit
```

**Notes for implementer:**
- The `SeedBaseDataAsync()` method may need adjustment. Check `tests/LYBT.Tests.Server/_Infrastructure/ServerFixture.cs` for exact User entity property names and required fields.
- The `LoginRequest` and `LoginResponse` types are in `LYBT.Shared.Models.Auth`.
- If `ApiResponse<T>` structure differs, check `LYBT.Shared.Models.Common.ApiResponse<T>`.
- The connection string uses `(localdb)\\MSSQLLocalDB` -- same as ServerFixture's `LocalSqlServerProvider`.

---

### Task 3: AuthFlowTests

**Files:**
- Create: `tests/LYBT.Tests.Integration/Flows/AuthFlowTests.cs`

**Context:**
- Reference `src/Client/Desktop/Core/LYBT.Desktop.Contracts/Api/IAuthApi.cs` for interface methods
- Tests exercise: Desktop IAuthApi (Refit) -> Server AuthController -> AuthService -> DB

**Step 1: Write tests**

Create `tests/LYBT.Tests.Integration/Flows/AuthFlowTests.cs`:

```csharp
using LYBT.Desktop.Contracts.Api;
using LYBT.Shared.Models.Auth;

namespace LYBT.Tests.Integration.Flows;

public class AuthFlowTests : IntegrationTestBase
{
    public AuthFlowTests(IntegrationFixture fixture) : base(fixture) { }

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsToken()
    {
        var authApi = Fixture.CreateApi<IAuthApi>(Fixture.AnonymousClient);
        var response = await authApi.LoginAsync(new LoginRequest
        {
            Username = "admin",
            Password = "TestAdmin2025@"
        });

        response.Should().NotBeNull();
        response.Success.Should().BeTrue();
        response.Data.Should().NotBeNull();
        response.Data!.Token.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Login_WithInvalidCredentials_Fails()
    {
        var authApi = Fixture.CreateApi<IAuthApi>(Fixture.AnonymousClient);

        var act = () => authApi.LoginAsync(new LoginRequest
        {
            Username = "admin",
            Password = "WrongPassword"
        });

        // Refit throws ApiException on non-success HTTP status
        await act.Should().ThrowAsync<Refit.ApiException>();
    }

    [Fact]
    public async Task Login_ThenAccessProtectedEndpoint_Succeeds()
    {
        var client = await Fixture.LoginAsAdminAsync();
        var patientApi = Fixture.CreateApi<IPatientApi>(client);

        var response = await patientApi.GetPatientsAsync(1, 10);

        response.Should().NotBeNull();
        response.Success.Should().BeTrue();
    }

    [Fact]
    public async Task AnonymousAccess_ToProtectedEndpoint_Returns401()
    {
        var patientApi = Fixture.CreateApi<IPatientApi>(Fixture.AnonymousClient);

        var act = () => patientApi.GetPatientsAsync(1, 10);

        await act.Should().ThrowAsync<Refit.ApiException>()
            .Where(e => e.StatusCode == System.Net.HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task HealthCheck_ReturnsSuccess()
    {
        var authApi = Fixture.CreateApi<IAuthApi>(Fixture.AnonymousClient);

        var response = await authApi.HealthCheckAsync();

        response.Should().NotBeNull();
    }
}
```

**Step 2: Run tests**

Run: `dotnet test tests/LYBT.Tests.Integration/ --filter "FullyQualifiedName~AuthFlowTests" -v normal`
Expected: 5 tests, all PASS (if fixture is correct)

**Step 3: Fix any issues**

Common issues:
- `LoginRequest` property names may differ -- check the actual DTO
- `IAuthApi.LoginAsync` return type may be `ApiResponse<LoginResponse>` or `Task<ApiResponse<LoginResponse>>` -- match the actual interface
- Refit error handling: non-2xx returns may need `[Headers("Accept: application/json")]` attribute

**Step 4: Commit**

```
test: add AuthFlowTests (5 tests) - Integration
```

---

### Task 4: PatientFlowTests

**Files:**
- Create: `tests/LYBT.Tests.Integration/Flows/PatientFlowTests.cs`

**Context:**
- Reference `src/Client/Desktop/Core/LYBT.Desktop.Contracts/Api/IPatientApi.cs` for methods
- Reference `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/DataSources/Remote/RemotePatientDataSource.cs` for DataSource usage
- Tests exercise: RemotePatientDataSource -> IPatientApi (Refit) -> Server -> DB

**Step 1: Write tests**

Create `tests/LYBT.Tests.Integration/Flows/PatientFlowTests.cs`:

```csharp
using LYBT.Desktop.Contracts.Api;
using LYBT.Desktop.Infrastructure.DataSources.Remote;
using Microsoft.Extensions.Logging.Abstractions;

namespace LYBT.Tests.Integration.Flows;

public class PatientFlowTests : IntegrationTestBase
{
    public PatientFlowTests(IntegrationFixture fixture) : base(fixture) { }

    [Fact]
    public async Task Patient_CreateAndRetrieve_RoundTrips()
    {
        var (client, api) = await LoginAsAdminWithApiAsync<IPatientApi>();
        var dataSource = new RemotePatientDataSource(api,
            NullLogger<RemotePatientDataSource>.Instance);

        // Create
        var input = CreatePatientInput("张三", "13800001111");
        var created = await dataSource.CreateAsync(input, CancellationToken.None);
        created.Should().NotBeNull();
        created.Id.Should().NotBeEmpty();

        // Retrieve
        var retrieved = await dataSource.GetByIdAsync(created.Id, CancellationToken.None);
        retrieved.Should().NotBeNull();
        retrieved!.FullName.Should().Be("张三");
    }

    [Fact]
    public async Task Patient_Update_ModifiesData()
    {
        var (client, api) = await LoginAsAdminWithApiAsync<IPatientApi>();
        var dataSource = new RemotePatientDataSource(api,
            NullLogger<RemotePatientDataSource>.Instance);

        var input = CreatePatientInput("李四", "13800002222");
        var created = await dataSource.CreateAsync(input, CancellationToken.None);

        // Update - create new input with same ID, changed name
        var updateInput = CreatePatientInput("李四改", "13800002222", created.Id);
        var updated = await dataSource.UpdateAsync(updateInput, CancellationToken.None);

        updated.FullName.Should().Be("李四改");
    }

    [Fact]
    public async Task Patient_Delete_RemovesFromList()
    {
        var (client, api) = await LoginAsAdminWithApiAsync<IPatientApi>();
        var dataSource = new RemotePatientDataSource(api,
            NullLogger<RemotePatientDataSource>.Instance);

        var input = CreatePatientInput("王五", "13800003333");
        var created = await dataSource.CreateAsync(input, CancellationToken.None);

        var deleted = await dataSource.DeleteAsync(created.Id, CancellationToken.None);
        deleted.Should().BeTrue();

        var retrieved = await dataSource.GetByIdAsync(created.Id, CancellationToken.None);
        retrieved.Should().BeNull();
    }

    [Fact]
    public async Task Patient_GetPaged_ReturnsList()
    {
        var (client, api) = await LoginAsAdminWithApiAsync<IPatientApi>();
        var dataSource = new RemotePatientDataSource(api,
            NullLogger<RemotePatientDataSource>.Instance);

        // Create 3 patients
        for (int i = 0; i < 3; i++)
            await dataSource.CreateAsync(
                CreatePatientInput($"患者{i}", $"1380000{i:D4}"),
                CancellationToken.None);

        var (list, total) = await dataSource.GetPagedAsync(1, 10, null, CancellationToken.None);
        list.Should().HaveCountGreaterOrEqualTo(3);
        total.Should().BeGreaterOrEqualTo(3);
    }

    [Fact]
    public async Task Patient_Search_FiltersByKeyword()
    {
        var (client, api) = await LoginAsAdminWithApiAsync<IPatientApi>();
        var dataSource = new RemotePatientDataSource(api,
            NullLogger<RemotePatientDataSource>.Instance);

        await dataSource.CreateAsync(
            CreatePatientInput("搜索目标患者", "13800009999"),
            CancellationToken.None);

        var (list, _) = await dataSource.GetPagedAsync(1, 10, "搜索目标", CancellationToken.None);
        list.Should().ContainSingle(p => p.FullName.Contains("搜索目标"));
    }

    // Helper: adapt to actual PatientInputDto structure
    private static LYBT.Shared.Models.Patients.PatientInputDto CreatePatientInput(
        string name, string phone, Guid? id = null)
    {
        return new LYBT.Shared.Models.Patients.PatientInputDto
        {
            Id = id ?? Guid.Empty,
            FullName = name,
            Phone = phone,
            Gender = "Male",
            DateOfBirth = new DateTime(1990, 1, 1)
        };
    }
}
```

**Step 2: Run and verify**

Run: `dotnet test tests/LYBT.Tests.Integration/ --filter "FullyQualifiedName~PatientFlowTests" -v normal`
Expected: 5 tests PASS

**Notes for implementer:**
- Check actual `PatientInputDto` property names in `src/Shared/LYBT.Shared.Models/Patients/`
- Check `RemotePatientDataSource` constructor signature -- it may require `IPatientApi` + `ILogger<RemotePatientDataSource>`
- The `GetPagedAsync` return type may differ from `(List<T>, int)` -- adapt to actual signature

**Step 3: Commit**

```
test: add PatientFlowTests (5 tests) - Integration
```

---

### Task 5: HerbFlowTests

**Files:**
- Create: `tests/LYBT.Tests.Integration/Flows/HerbFlowTests.cs`

**Context:**
- Reference `src/Client/Desktop/Core/LYBT.Desktop.Contracts/Api/IHerbApi.cs`
- Reference `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/DataSources/Remote/RemoteHerbDataSource.cs`

**Step 1: Write tests**

Create `tests/LYBT.Tests.Integration/Flows/HerbFlowTests.cs`:

```csharp
using LYBT.Desktop.Contracts.Api;
using LYBT.Desktop.Infrastructure.DataSources.Remote;
using Microsoft.Extensions.Logging.Abstractions;

namespace LYBT.Tests.Integration.Flows;

public class HerbFlowTests : IntegrationTestBase
{
    public HerbFlowTests(IntegrationFixture fixture) : base(fixture) { }

    [Fact]
    public async Task Herb_CreateAndRetrieve_RoundTrips()
    {
        var (_, api) = await LoginAsAdminWithApiAsync<IHerbApi>();
        var ds = new RemoteHerbDataSource(api, NullLogger<RemoteHerbDataSource>.Instance);

        var input = CreateHerbInput("黄芪");
        var created = await ds.CreateAsync(input, CancellationToken.None);
        created.Should().NotBeNull();

        var retrieved = await ds.GetByIdAsync(created.Id, CancellationToken.None);
        retrieved.Should().NotBeNull();
        retrieved!.Name.Should().Be("黄芪");
    }

    [Fact]
    public async Task Herb_ToggleStatus_DisablesAndEnables()
    {
        var (_, api) = await LoginAsAdminWithApiAsync<IHerbApi>();
        var ds = new RemoteHerbDataSource(api, NullLogger<RemoteHerbDataSource>.Instance);

        var created = await ds.CreateAsync(CreateHerbInput("当归"), CancellationToken.None);
        created.IsActive.Should().BeTrue();

        // Disable
        await ds.ToggleStatusAsync(created.Id, CancellationToken.None);
        var disabled = await ds.GetByIdAsync(created.Id, CancellationToken.None);
        disabled!.IsActive.Should().BeFalse();

        // Re-enable
        await ds.ToggleStatusAsync(created.Id, CancellationToken.None);
        var enabled = await ds.GetByIdAsync(created.Id, CancellationToken.None);
        enabled!.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task Herb_Delete_SoftDeletes()
    {
        var (_, api) = await LoginAsAdminWithApiAsync<IHerbApi>();
        var ds = new RemoteHerbDataSource(api, NullLogger<RemoteHerbDataSource>.Instance);

        var created = await ds.CreateAsync(CreateHerbInput("甘草"), CancellationToken.None);
        await ds.DeleteAsync(created.Id, CancellationToken.None);

        var retrieved = await ds.GetByIdAsync(created.Id, CancellationToken.None);
        retrieved.Should().BeNull();
    }

    [Fact]
    public async Task Herb_GetPaged_ReturnsList()
    {
        var (_, api) = await LoginAsAdminWithApiAsync<IHerbApi>();
        var ds = new RemoteHerbDataSource(api, NullLogger<RemoteHerbDataSource>.Instance);

        await ds.CreateAsync(CreateHerbInput("白术"), CancellationToken.None);
        await ds.CreateAsync(CreateHerbInput("茯苓"), CancellationToken.None);

        var (list, total) = await ds.GetPagedAsync(1, 10, null, CancellationToken.None);
        list.Should().HaveCountGreaterOrEqualTo(2);
    }

    // Adapt to actual HerbInputDto
    private static LYBT.Shared.Models.Herbs.HerbInputDto CreateHerbInput(string name)
    {
        return new LYBT.Shared.Models.Herbs.HerbInputDto
        {
            Name = name,
            Category = "补气药",
            IsActive = true
        };
    }
}
```

**Step 2: Run and verify**

Run: `dotnet test tests/LYBT.Tests.Integration/ --filter "FullyQualifiedName~HerbFlowTests" -v normal`
Expected: 4 tests PASS

**Step 3: Commit**

```
test: add HerbFlowTests (4 tests) - Integration
```

---

### Task 6: FormulaFlowTests

**Files:**
- Create: `tests/LYBT.Tests.Integration/Flows/FormulaFlowTests.cs`

**Context:**
- Reference `src/Client/Desktop/Core/LYBT.Desktop.Contracts/Api/IFormulaApi.cs`
- Reference `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/DataSources/Remote/RemoteFormulaDataSource.cs`

**Step 1: Write tests**

Create `tests/LYBT.Tests.Integration/Flows/FormulaFlowTests.cs`:

```csharp
using LYBT.Desktop.Contracts.Api;
using LYBT.Desktop.Infrastructure.DataSources.Remote;
using Microsoft.Extensions.Logging.Abstractions;

namespace LYBT.Tests.Integration.Flows;

public class FormulaFlowTests : IntegrationTestBase
{
    public FormulaFlowTests(IntegrationFixture fixture) : base(fixture) { }

    [Fact]
    public async Task Formula_CreateWithHerbs_RoundTrips()
    {
        // First create herbs (formula depends on herbs)
        var (_, herbApi) = await LoginAsAdminWithApiAsync<IHerbApi>();
        var herbDs = new RemoteHerbDataSource(herbApi, NullLogger<RemoteHerbDataSource>.Instance);
        var herb1 = await herbDs.CreateAsync(CreateHerbInput("黄芪"), CancellationToken.None);
        var herb2 = await herbDs.CreateAsync(CreateHerbInput("当归"), CancellationToken.None);

        // Create formula with herb items
        var (_, formulaApi) = await LoginAsAdminWithApiAsync<IFormulaApi>();
        var formulaDs = new RemoteFormulaDataSource(formulaApi,
            NullLogger<RemoteFormulaDataSource>.Instance);

        var input = CreateFormulaInput("补中益气汤", herb1.Id, herb2.Id);
        var created = await formulaDs.CreateAsync(input, CancellationToken.None);

        created.Should().NotBeNull();
        created.Name.Should().Be("补中益气汤");
    }

    [Fact]
    public async Task Formula_Clone_CreatesIndependentCopy()
    {
        var (_, herbApi) = await LoginAsAdminWithApiAsync<IHerbApi>();
        var herbDs = new RemoteHerbDataSource(herbApi, NullLogger<RemoteHerbDataSource>.Instance);
        var herb = await herbDs.CreateAsync(CreateHerbInput("白术"), CancellationToken.None);

        var (_, formulaApi) = await LoginAsAdminWithApiAsync<IFormulaApi>();
        var formulaDs = new RemoteFormulaDataSource(formulaApi,
            NullLogger<RemoteFormulaDataSource>.Instance);

        var original = await formulaDs.CreateAsync(
            CreateFormulaInput("四君子汤", herb.Id), CancellationToken.None);

        var cloned = await formulaDs.CloneAsync(original.Id, CancellationToken.None);

        cloned.Should().NotBeNull();
        cloned.Id.Should().NotBe(original.Id);
    }

    [Fact]
    public async Task Formula_Delete_SoftDeletes()
    {
        var (_, formulaApi) = await LoginAsAdminWithApiAsync<IFormulaApi>();
        var formulaDs = new RemoteFormulaDataSource(formulaApi,
            NullLogger<RemoteFormulaDataSource>.Instance);

        var created = await formulaDs.CreateAsync(
            CreateFormulaInput("测试方"), CancellationToken.None);

        await formulaDs.DeleteAsync(created.Id, CancellationToken.None);
        var retrieved = await formulaDs.GetByIdAsync(created.Id, CancellationToken.None);
        retrieved.Should().BeNull();
    }

    [Fact]
    public async Task Formula_GetPaged_ReturnsList()
    {
        var (_, formulaApi) = await LoginAsAdminWithApiAsync<IFormulaApi>();
        var formulaDs = new RemoteFormulaDataSource(formulaApi,
            NullLogger<RemoteFormulaDataSource>.Instance);

        await formulaDs.CreateAsync(CreateFormulaInput("方剂A"), CancellationToken.None);
        await formulaDs.CreateAsync(CreateFormulaInput("方剂B"), CancellationToken.None);

        var (list, _) = await formulaDs.GetPagedAsync(1, 10, null, CancellationToken.None);
        list.Should().HaveCountGreaterOrEqualTo(2);
    }

    // Helpers - adapt to actual DTOs
    private static LYBT.Shared.Models.Herbs.HerbInputDto CreateHerbInput(string name)
        => new() { Name = name, Category = "补气药", IsActive = true };

    private static LYBT.Shared.Models.Formulas.FormulaInputDto CreateFormulaInput(
        string name, params Guid[] herbIds)
    {
        return new LYBT.Shared.Models.Formulas.FormulaInputDto
        {
            Name = name,
            IsActive = true,
            // Adapt HerbItems property to actual DTO structure
        };
    }
}
```

**Step 2: Run and verify**

Run: `dotnet test tests/LYBT.Tests.Integration/ --filter "FullyQualifiedName~FormulaFlowTests" -v normal`
Expected: 4 tests PASS

**Step 3: Commit**

```
test: add FormulaFlowTests (4 tests) - Integration
```

---

### Task 7: MedicalCaseFlowTests

**Files:**
- Create: `tests/LYBT.Tests.Integration/Flows/MedicalCaseFlowTests.cs`

**Context:**
- Reference `src/Client/Desktop/Core/LYBT.Desktop.Contracts/Api/IMedicalCaseApi.cs`
- Reference `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/DataSources/Remote/RemoteMedicalCaseDataSource.cs`
- MedicalCase is the DDD aggregate root (Consultation + Prescription are internal entities)
- This is the most critical test file -- verifies the core clinical workflow

**Step 1: Write tests**

Create `tests/LYBT.Tests.Integration/Flows/MedicalCaseFlowTests.cs`:

```csharp
using LYBT.Desktop.Contracts.Api;
using LYBT.Desktop.Infrastructure.DataSources.Remote;
using Microsoft.Extensions.Logging.Abstractions;

namespace LYBT.Tests.Integration.Flows;

public class MedicalCaseFlowTests : IntegrationTestBase
{
    public MedicalCaseFlowTests(IntegrationFixture fixture) : base(fixture) { }

    [Fact]
    public async Task MedicalCase_CreateForPatient_Succeeds()
    {
        // Setup: create patient first
        var (_, patientApi) = await LoginAsDoctorWithApiAsync<IPatientApi>();
        var patientDs = new RemotePatientDataSource(patientApi,
            NullLogger<RemotePatientDataSource>.Instance);
        var patient = await patientDs.CreateAsync(
            CreatePatientInput("测试患者"), CancellationToken.None);

        // Create medical case
        var (_, mcApi) = await LoginAsDoctorWithApiAsync<IMedicalCaseApi>();
        var mcDs = new RemoteMedicalCaseDataSource(mcApi,
            NullLogger<RemoteMedicalCaseDataSource>.Instance);

        var input = CreateMedicalCaseInput(patient.Id);
        var created = await mcDs.CreateAsync(input, CancellationToken.None);

        created.Should().NotBeNull();
        created.Id.Should().NotBeEmpty();
        created.PatientId.Should().Be(patient.Id);
    }

    [Fact]
    public async Task MedicalCase_SaveWithConsultation_PersistsData()
    {
        var patient = await CreateTestPatientAsync();
        var (_, mcApi) = await LoginAsDoctorWithApiAsync<IMedicalCaseApi>();
        var mcDs = new RemoteMedicalCaseDataSource(mcApi,
            NullLogger<RemoteMedicalCaseDataSource>.Instance);

        var created = await mcDs.CreateAsync(
            CreateMedicalCaseInput(patient.Id), CancellationToken.None);

        // Save with consultation data
        var saveInput = CreateSaveInput(created.Id, patient.Id,
            chiefComplaint: "头痛三天",
            diagnosis: "风寒感冒");
        var saved = await mcDs.SaveAsync(saveInput, CancellationToken.None);

        saved.Should().NotBeNull();
        // Verify consultation data persisted
        var retrieved = await mcDs.GetByIdAsync(saved.Id, CancellationToken.None);
        retrieved.Should().NotBeNull();
    }

    [Fact]
    public async Task MedicalCase_CompleteCase_ChangesStatus()
    {
        var patient = await CreateTestPatientAsync();
        var (_, mcApi) = await LoginAsDoctorWithApiAsync<IMedicalCaseApi>();
        var mcDs = new RemoteMedicalCaseDataSource(mcApi,
            NullLogger<RemoteMedicalCaseDataSource>.Instance);

        var created = await mcDs.CreateAsync(
            CreateMedicalCaseInput(patient.Id), CancellationToken.None);

        // Save with required data, then complete
        var saveInput = CreateSaveInput(created.Id, patient.Id,
            chiefComplaint: "腰痛", diagnosis: "肾虚");
        await mcDs.SaveAsync(saveInput, CancellationToken.None);

        await mcDs.CloseCaseAsync(created.Id, CancellationToken.None);

        var completed = await mcDs.GetByIdAsync(created.Id, CancellationToken.None);
        completed.Should().NotBeNull();
        // Verify status is Completed (adapt to actual status enum/string)
    }

    [Fact]
    public async Task MedicalCase_SuspendAndResume_Lifecycle()
    {
        var patient = await CreateTestPatientAsync();
        var (_, mcApi) = await LoginAsDoctorWithApiAsync<IMedicalCaseApi>();
        var mcDs = new RemoteMedicalCaseDataSource(mcApi,
            NullLogger<RemoteMedicalCaseDataSource>.Instance);

        var created = await mcDs.CreateAsync(
            CreateMedicalCaseInput(patient.Id), CancellationToken.None);

        // Suspend
        await mcDs.SuspendAsync(created.Id, CancellationToken.None);
        var suspended = await mcDs.GetByIdAsync(created.Id, CancellationToken.None);
        suspended.Should().NotBeNull();

        // Cancel suspension (resume) -- check actual API method name
        await mcDs.CancelMedicalCaseAsync(created.Id, CancellationToken.None);
    }

    [Fact]
    public async Task MedicalCase_Delete_SoftDeletes()
    {
        var patient = await CreateTestPatientAsync();
        var (_, mcApi) = await LoginAsDoctorWithApiAsync<IMedicalCaseApi>();
        var mcDs = new RemoteMedicalCaseDataSource(mcApi,
            NullLogger<RemoteMedicalCaseDataSource>.Instance);

        var created = await mcDs.CreateAsync(
            CreateMedicalCaseInput(patient.Id), CancellationToken.None);

        await mcDs.DeleteAsync(created.Id, CancellationToken.None);
        var retrieved = await mcDs.GetByIdAsync(created.Id, CancellationToken.None);
        retrieved.Should().BeNull();
    }

    [Fact]
    public async Task MedicalCase_GetPendingCases_ReturnsActiveCases()
    {
        var patient = await CreateTestPatientAsync();
        var (_, mcApi) = await LoginAsDoctorWithApiAsync<IMedicalCaseApi>();
        var mcDs = new RemoteMedicalCaseDataSource(mcApi,
            NullLogger<RemoteMedicalCaseDataSource>.Instance);

        await mcDs.CreateAsync(CreateMedicalCaseInput(patient.Id), CancellationToken.None);

        var pending = await mcDs.GetPendingCasesAsync(CancellationToken.None);
        pending.Should().NotBeEmpty();
    }

    [Fact]
    public async Task MedicalCase_GetPermissions_ReturnsDoctorPermissions()
    {
        var patient = await CreateTestPatientAsync();
        var (_, mcApi) = await LoginAsDoctorWithApiAsync<IMedicalCaseApi>();
        var mcDs = new RemoteMedicalCaseDataSource(mcApi,
            NullLogger<RemoteMedicalCaseDataSource>.Instance);

        var created = await mcDs.CreateAsync(
            CreateMedicalCaseInput(patient.Id), CancellationToken.None);

        var permissions = await mcDs.GetPermissionsAsync(created.Id, CancellationToken.None);
        permissions.Should().NotBeNull();
    }

    [Fact]
    public async Task MedicalCase_FullClinicalWorkflow_EndToEnd()
    {
        // This is the golden path test: Patient -> MedicalCase -> Consultation -> Prescription -> Complete
        var patient = await CreateTestPatientAsync();

        // Create herbs for prescription
        var (_, herbApi) = await LoginAsAdminWithApiAsync<IHerbApi>();
        var herbDs = new RemoteHerbDataSource(herbApi, NullLogger<RemoteHerbDataSource>.Instance);
        var herb = await herbDs.CreateAsync(
            new LYBT.Shared.Models.Herbs.HerbInputDto
            { Name = "黄芪", Category = "补气药", IsActive = true },
            CancellationToken.None);

        // Create + Save + Complete medical case
        var (_, mcApi) = await LoginAsDoctorWithApiAsync<IMedicalCaseApi>();
        var mcDs = new RemoteMedicalCaseDataSource(mcApi,
            NullLogger<RemoteMedicalCaseDataSource>.Instance);

        var mc = await mcDs.CreateAsync(
            CreateMedicalCaseInput(patient.Id), CancellationToken.None);

        var saveInput = CreateSaveInput(mc.Id, patient.Id,
            chiefComplaint: "气虚乏力",
            diagnosis: "气血两虚");
        // Add prescription with herb -- adapt to actual save DTO structure
        await mcDs.SaveAsync(saveInput, CancellationToken.None);
        await mcDs.CloseCaseAsync(mc.Id, CancellationToken.None);

        // Verify final state
        var final = await mcDs.GetByIdAsync(mc.Id, CancellationToken.None);
        final.Should().NotBeNull();
    }

    // -- Helpers --

    private async Task<LYBT.Shared.Models.Patients.PatientDetailDto> CreateTestPatientAsync()
    {
        var (_, patientApi) = await LoginAsDoctorWithApiAsync<IPatientApi>();
        var patientDs = new RemotePatientDataSource(patientApi,
            NullLogger<RemotePatientDataSource>.Instance);
        return await patientDs.CreateAsync(
            CreatePatientInput($"患者{Guid.NewGuid():N}"[..8]),
            CancellationToken.None);
    }

    // Adapt all helpers to actual DTO structures
    private static LYBT.Shared.Models.Patients.PatientInputDto CreatePatientInput(string name)
        => new()
        {
            FullName = name,
            Phone = $"138{Random.Shared.Next(10000000, 99999999)}",
            Gender = "Male",
            DateOfBirth = new DateTime(1990, 1, 1)
        };

    private static LYBT.Shared.Models.MedicalCases.MedicalCaseInputDto CreateMedicalCaseInput(Guid patientId)
        => new() { PatientId = patientId };

    private static LYBT.Shared.Models.MedicalCases.MedicalCaseInputDto CreateSaveInput(
        Guid caseId, Guid patientId, string chiefComplaint, string diagnosis)
        => new()
        {
            Id = caseId,
            PatientId = patientId,
            // Adapt: ChiefComplaint, Diagnosis fields
            // These may be nested in Consultation sub-object
        };
}
```

**Step 2: Run and verify**

Run: `dotnet test tests/LYBT.Tests.Integration/ --filter "FullyQualifiedName~MedicalCaseFlowTests" -v normal`
Expected: 8 tests PASS

**Notes for implementer:**
- `MedicalCaseInputDto` structure is critical -- check `src/Shared/LYBT.Shared.Models/MedicalCases/`
- `SaveAsync` on RemoteMedicalCaseDataSource may have a different signature than `CreateAsync`
- The `CloseCaseAsync`, `SuspendAsync`, `CancelMedicalCaseAsync` methods may need `CancellationToken` parameter -- check `IMedicalCaseApi`
- `GetPendingCasesAsync` may require doctorId parameter -- check actual interface

**Step 3: Run all Integration tests**

Run: `dotnet test tests/LYBT.Tests.Integration/ -v normal`
Expected: ~22 tests total, all PASS

**Step 4: Commit**

```
test: add MedicalCaseFlowTests (8 tests) - Integration
```

---

## Phase 3: Desktop Test Cleanup

### Task 8: Delete all local mode tests

**Files to delete:**

```
# LocalData DataSource tests (53 tests)
tests/LYBT.Tests.Desktop/LocalData/DataSources/LocalPatientDataSourceTests.cs    # 13 tests
tests/LYBT.Tests.Desktop/LocalData/DataSources/LocalHerbDataSourceTests.cs       # 11 tests
tests/LYBT.Tests.Desktop/LocalData/DataSources/LocalFormulaDataSourceTests.cs    # 18 tests
tests/LYBT.Tests.Desktop/LocalData/Services/LocalAuthServiceTests.cs             # 11 tests

# EndToEnd LocalMode tests (19 tests)
tests/LYBT.Tests.Desktop/EndToEnd/LocalMode/DataSourceIntegrationTests.cs        # 13 tests
tests/LYBT.Tests.Desktop/EndToEnd/LocalMode/LoginFlowIntegrationTests.cs         #  6 tests

# EndToEnd DataSource/DesktopFixture tests (56 tests)
tests/LYBT.Tests.Desktop/EndToEnd/Patients/PatientE2ETests.cs                   #  7 tests
tests/LYBT.Tests.Desktop/EndToEnd/Herbs/HerbE2ETests.cs                         #  4 tests
tests/LYBT.Tests.Desktop/EndToEnd/Formula/FormulaE2ETests.cs                    #  5 tests
tests/LYBT.Tests.Desktop/EndToEnd/MedicalCase/MedicalCaseE2ETests.cs            #  5 tests
tests/LYBT.Tests.Desktop/EndToEnd/MedicalCase/MedicalCaseAggregateE2ETests.cs   # 11 tests
tests/LYBT.Tests.Desktop/EndToEnd/MedicalCase/MedicalCaseDataSourceTests.cs     #  8 tests
tests/LYBT.Tests.Desktop/EndToEnd/Prescription/PrescriptionE2ETests.cs          #  5 tests
tests/LYBT.Tests.Desktop/EndToEnd/Users/UserE2ETests.cs                         #  3 tests
tests/LYBT.Tests.Desktop/EndToEnd/Navigation/NavigationFlowE2ETests.cs          #  4 tests
tests/LYBT.Tests.Desktop/EndToEnd/BusinessFlow/BusinessFlowE2ETests.cs          #  1 test
tests/LYBT.Tests.Desktop/EndToEnd/BusinessFlow/BusinessFlowTests.cs             #  3 tests

# Infrastructure fixtures (no longer needed after EndToEnd deletion)
tests/LYBT.Tests.Desktop/_Infrastructure/DesktopFixture.cs                       # fixture
tests/LYBT.Tests.Desktop/_Infrastructure/LocalDbContextFixture.cs                # fixture
tests/LYBT.Tests.Desktop/_Infrastructure/DesktopFixtureSmokeTests.cs             # ~3 tests
```

**Total: ~131 tests deleted + 2 fixture files**

**Step 1: Delete all files**

```bash
# LocalData
rm tests/LYBT.Tests.Desktop/LocalData/DataSources/LocalPatientDataSourceTests.cs
rm tests/LYBT.Tests.Desktop/LocalData/DataSources/LocalHerbDataSourceTests.cs
rm tests/LYBT.Tests.Desktop/LocalData/DataSources/LocalFormulaDataSourceTests.cs
rm tests/LYBT.Tests.Desktop/LocalData/Services/LocalAuthServiceTests.cs

# EndToEnd LocalMode
rm tests/LYBT.Tests.Desktop/EndToEnd/LocalMode/DataSourceIntegrationTests.cs
rm tests/LYBT.Tests.Desktop/EndToEnd/LocalMode/LoginFlowIntegrationTests.cs

# EndToEnd DesktopFixture-based
rm tests/LYBT.Tests.Desktop/EndToEnd/Patients/PatientE2ETests.cs
rm tests/LYBT.Tests.Desktop/EndToEnd/Herbs/HerbE2ETests.cs
rm tests/LYBT.Tests.Desktop/EndToEnd/Formula/FormulaE2ETests.cs
rm tests/LYBT.Tests.Desktop/EndToEnd/MedicalCase/MedicalCaseE2ETests.cs
rm tests/LYBT.Tests.Desktop/EndToEnd/MedicalCase/MedicalCaseAggregateE2ETests.cs
rm tests/LYBT.Tests.Desktop/EndToEnd/MedicalCase/MedicalCaseDataSourceTests.cs
rm tests/LYBT.Tests.Desktop/EndToEnd/Prescription/PrescriptionE2ETests.cs
rm tests/LYBT.Tests.Desktop/EndToEnd/Users/UserE2ETests.cs
rm tests/LYBT.Tests.Desktop/EndToEnd/Navigation/NavigationFlowE2ETests.cs
rm tests/LYBT.Tests.Desktop/EndToEnd/BusinessFlow/BusinessFlowE2ETests.cs
rm tests/LYBT.Tests.Desktop/EndToEnd/BusinessFlow/BusinessFlowTests.cs

# Infrastructure
rm tests/LYBT.Tests.Desktop/_Infrastructure/DesktopFixture.cs
rm tests/LYBT.Tests.Desktop/_Infrastructure/LocalDbContextFixture.cs
rm tests/LYBT.Tests.Desktop/_Infrastructure/DesktopFixtureSmokeTests.cs
```

**Step 2: Clean up empty directories**

```bash
# Remove empty LocalData/ and EndToEnd/ subdirectories
find tests/LYBT.Tests.Desktop/LocalData -type d -empty -delete
find tests/LYBT.Tests.Desktop/EndToEnd -type d -empty -delete
```

**Step 3: Verify build**

Run: `dotnet build tests/LYBT.Tests.Desktop/`
Expected: BUILD SUCCEEDED (remaining tests should not reference deleted files)

If build fails: fix compilation errors from remaining tests that may import deleted fixture types.

**Step 4: Commit**

```
test: delete 131 local mode tests and DesktopFixture infrastructure

Local mode DataSource layer will be removed in SYNC-D02 (Sprint 4).
Tests will be re-added after local mode is reimplemented.

Deleted:
- LocalData/DataSources/ (42 tests) + LocalAuthService (11 tests)
- EndToEnd/LocalMode/ (19 tests)
- EndToEnd/ DesktopFixture-based (56 tests)
- DesktopFixture + LocalDbContextFixture + SmokeTests (3 tests)
```

---

### Task 9: Delete mock-heavy ViewModel tests

**Files to delete:**

```
# ViewModel folder (39 tests, all mock-heavy)
tests/LYBT.Tests.Desktop/ViewModels/Admin/AdminHomeViewModelTests.cs             #  8 tests
tests/LYBT.Tests.Desktop/ViewModels/Clinical/ClinicalHomeViewModelTests.cs       #  8 tests
tests/LYBT.Tests.Desktop/ViewModels/Shell/Login/LoginCoordinatorTests.cs         # 23 tests

# PureLogic mock-heavy (70 tests)
tests/LYBT.Tests.Desktop/PureLogic/Clinical/PendingQueueViewModelTests.cs        #  9 tests
tests/LYBT.Tests.Desktop/PureLogic/Clinical/CardReaderViewModelTests.cs          # 19 tests
tests/LYBT.Tests.Desktop/PureLogic/MedicalCase/Workspace/MedicalCaseCommandsViewModelTests.cs  # 21 tests
tests/LYBT.Tests.Desktop/PureLogic/MedicalCase/Workspace/ConsultationEditorViewModelTests.cs   #  7 tests
tests/LYBT.Tests.Desktop/PureLogic/MedicalCase/Workspace/PrescriptionEditorViewModelTests.cs   #  9 tests
tests/LYBT.Tests.Desktop/PureLogic/Infrastructure/ChildViewModelBaseTests.cs     #  5 tests
```

**Total: ~109 tests deleted**

**Step 1: Delete files**

```bash
# ViewModel mock tests
rm tests/LYBT.Tests.Desktop/ViewModels/Admin/AdminHomeViewModelTests.cs
rm tests/LYBT.Tests.Desktop/ViewModels/Clinical/ClinicalHomeViewModelTests.cs
rm tests/LYBT.Tests.Desktop/ViewModels/Shell/Login/LoginCoordinatorTests.cs

# PureLogic mock-heavy tests
rm tests/LYBT.Tests.Desktop/PureLogic/Clinical/PendingQueueViewModelTests.cs
rm tests/LYBT.Tests.Desktop/PureLogic/Clinical/CardReaderViewModelTests.cs
rm tests/LYBT.Tests.Desktop/PureLogic/MedicalCase/Workspace/MedicalCaseCommandsViewModelTests.cs
rm tests/LYBT.Tests.Desktop/PureLogic/MedicalCase/Workspace/ConsultationEditorViewModelTests.cs
rm tests/LYBT.Tests.Desktop/PureLogic/MedicalCase/Workspace/PrescriptionEditorViewModelTests.cs
rm tests/LYBT.Tests.Desktop/PureLogic/Infrastructure/ChildViewModelBaseTests.cs
```

**Step 2: Clean empty directories**

```bash
find tests/LYBT.Tests.Desktop/ViewModels -type d -empty -delete
find tests/LYBT.Tests.Desktop/PureLogic/Clinical -type d -empty -delete
```

**Step 3: Verify build**

Run: `dotnet build tests/LYBT.Tests.Desktop/`
Expected: BUILD SUCCEEDED

**Step 4: Commit**

```
test: delete 109 mock-heavy ViewModel and PureLogic tests

Removed tests that verify mock interactions (Received/DidNotReceive)
rather than real behavior. These tests don't catch real bugs.
```

---

### Task 10: Clean up .csproj and remove unused dependencies

**Files:**
- Modify: `tests/LYBT.Tests.Desktop/LYBT.Tests.Desktop.csproj`

**Step 1: Check which references are still needed**

After deleting local mode tests, verify if these are still needed:
- `Microsoft.EntityFrameworkCore.Sqlite` -- only needed if remaining tests use SQLite
- `Microsoft.Data.Sqlite` -- same
- `LYBT.Desktop.LocalData` project reference -- only if remaining code imports from it

Run: `grep -r "LocalData\|LocalDbContext\|UseSqlite\|Microsoft.Data.Sqlite" tests/LYBT.Tests.Desktop/ --include="*.cs" -l`

**Step 2: Remove unused references from .csproj**

If no remaining .cs files reference SQLite/LocalData:

Remove from `tests/LYBT.Tests.Desktop/LYBT.Tests.Desktop.csproj`:
```xml
<!-- Remove these lines -->
<PackageReference Include="Microsoft.EntityFrameworkCore.Sqlite" />
<PackageReference Include="Microsoft.Data.Sqlite" />
<PackageReference Include="BCrypt.Net-Next" />  <!-- only if LocalAuth tests deleted -->
```

Note: Keep `LYBT.Desktop.LocalData` project reference if other Desktop projects transitively depend on it. Check build after removal.

**Step 3: Verify build**

Run: `dotnet build tests/LYBT.Tests.Desktop/`
Expected: BUILD SUCCEEDED

**Step 4: Run remaining tests to ensure nothing broke**

Run: `dotnet test tests/LYBT.Tests.Desktop/ -v normal`
Expected: All remaining tests PASS

Count the remaining tests and record.

**Step 5: Commit**

```
test: clean up Desktop test .csproj after local mode test deletion
```

---

### Task 11: Verify and categorize remaining Desktop tests

**Purpose:** Before writing new tests, confirm what remains and identify gaps.

**Step 1: Count remaining tests**

Run: `dotnet test tests/LYBT.Tests.Desktop/ --list-tests 2>&1 | tail -1`

**Step 2: List all remaining test files**

Run: `find tests/LYBT.Tests.Desktop -name "*Tests.cs" -not -path "*/obj/*" | sort`

**Expected remaining files:**

```
# PureLogic - Foundation/Security (98 tests)
PureLogic/Foundation/Security/AuthenticationStateMachineTests.cs    # 48
PureLogic/Foundation/Security/CredentialVaultTests.cs               # 20
PureLogic/Foundation/Security/LogoutServiceTests.cs                 # 19
PureLogic/Foundation/Security/LocalTokenValidatorTests.cs           # 11

# PureLogic - Shell/Startup (32 tests)
PureLogic/Shell/Startup/StartupPipelineTests.cs                    # 13
PureLogic/Shell/Startup/Steps/StartupStepsTests.cs                 # 19

# PureLogic - Infrastructure (varies)
PureLogic/Infrastructure/Services/PaginationServiceTests.cs
PureLogic/Infrastructure/Services/SelectionServiceTests.cs
PureLogic/Infrastructure/Services/LoadingStateManagerTests.cs
PureLogic/Infrastructure/Services/UserActivityTrackerTests.cs       # 21
PureLogic/Infrastructure/Services/ApplicationTickServiceTests.cs
PureLogic/Infrastructure/Controls/UnifiedManagementTableTests.cs
PureLogic/Infrastructure/Views/BaseMasterDataListViewTests.cs
PureLogic/Infrastructure/Events/PatientEventsTests.cs
PureLogic/Infrastructure/Models/Options/DisplayOptionsTests.cs
PureLogic/Infrastructure/Models/Options/PaginationOptionsTests.cs

# PureLogic - MedicalCase (20 tests)
PureLogic/MedicalCase/WorkspaceStateTests.cs                       # 12
PureLogic/MedicalCase/MedicalCaseChangeTrackerTests.cs             #  8

# PureLogic - Patients
PureLogic/Patients/Models/Display/PatientDetailDisplayModelTests.cs

# EndToEnd - Foundation (18 tests, kept - remote infra tests)
EndToEnd/Foundation/AuthenticationIntegrationTests.cs              #  4
EndToEnd/Foundation/RetryPolicyIntegrationTests.cs                 #  9
EndToEnd/Foundation/TokenRefreshHandlerIntegrationTests.cs         #  5
```

**Step 3: Record test count in progress.md**

Update `progress.md` with: "After deletion: X tests remaining in LYBT.Tests.Desktop"

---

### Task 12: Rewrite ConsultationEditor pure logic tests

**Files:**
- Create: `tests/LYBT.Tests.Desktop/PureLogic/MedicalCase/Workspace/ConsultationEditorPureTests.cs`

**Context:**
- Reference `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/ViewModels/Workspace/ConsultationEditorViewModel.cs`
- Tests should create the ViewModel directly (no mocks for context/host), test pure mapping and validation logic only

**Step 1: Write tests**

```csharp
// Test: consultation data mapping (DTO -> ViewModel properties)
// Test: validation rules (required fields, length limits)
// Test: reset clears all fields
// Test: HasChanges detection
// Test: GetData returns correct DTO
// Test: initialization for new case (empty state)
```

**Step 2: Run and verify**

Run: `dotnet test tests/LYBT.Tests.Desktop/ --filter "FullyQualifiedName~ConsultationEditorPureTests" -v normal`
Expected: ~6 tests PASS

**Step 3: Commit**

```
test: add ConsultationEditorPureTests (6 tests) - pure logic, no mocks
```

---

### Task 13: Rewrite PrescriptionEditor pure logic tests

**Files:**
- Create: `tests/LYBT.Tests.Desktop/PureLogic/MedicalCase/Workspace/PrescriptionEditorPureTests.cs`

**Context:**
- Reference `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/ViewModels/Workspace/PrescriptionEditorViewModel.cs`
- Focus on: collection operations, herb item add/remove, amount calculations, validation

**Step 1: Write tests**

```csharp
// Test: add herb item to collection
// Test: remove herb item from collection
// Test: total amount calculation
// Test: collection change notifications
// Test: validation (at least one herb required)
// Test: GetData returns correct DTO with items
// Test: reset clears items
// Test: duplicate herb detection
```

**Step 2: Run and verify**

Run: `dotnet test tests/LYBT.Tests.Desktop/ --filter "FullyQualifiedName~PrescriptionEditorPureTests" -v normal`
Expected: ~8 tests PASS

**Step 3: Commit**

```
test: add PrescriptionEditorPureTests (8 tests) - pure logic, no mocks
```

---

### Task 14: Rewrite CardReader tests (minimal mock)

**Files:**
- Create: `tests/LYBT.Tests.Desktop/PureLogic/Clinical/CardReaderPureTests.cs`

**Context:**
- Reference `src/Client/Desktop/Roles/LYBT.Desktop.Clinical/ViewModels/Workspace/CardReaderViewModel.cs` (or wherever it lives)
- Only mock `ICardReaderService` (hardware boundary) -- everything else real

**Step 1: Write tests**

```csharp
// Test: ID masking (show only last 4 digits)
// Test: auto-read toggle state
// Test: connection state reflects service state
// Test: read card returns patient info
// Test: read card with no card inserted handles gracefully
// Test: dispose unsubscribes from events
// Test: initialization state
// Test: manual read command availability
```

**Step 2: Run and verify**

Expected: ~8 tests PASS

**Step 3: Commit**

```
test: add CardReaderPureTests (8 tests) - only mock ICardReaderService
```

---

### Task 15: Supplement WorkspaceState and ChangeTracker tests

**Files:**
- Modify: `tests/LYBT.Tests.Desktop/PureLogic/MedicalCase/WorkspaceStateTests.cs`
- Modify: `tests/LYBT.Tests.Desktop/PureLogic/MedicalCase/MedicalCaseChangeTrackerTests.cs`

**Context:**
- WorkspaceState currently has 12 tests -- supplement to cover all branches of `DetermineFromContext`
- ChangeTracker currently has 8 tests -- supplement to cover all 14 tracked fields

**Step 1: Read existing tests to understand coverage gaps**

Read both test files to identify untested branches.

**Step 2: Add supplemental tests to WorkspaceState**

```csharp
// Add ~3 tests for uncovered DetermineFromContext branches
// Test: new case + doctor role -> edit mode
// Test: completed case + admin role -> readonly
// Test: suspended case + owner doctor -> specific permissions
```

**Step 3: Add supplemental tests to ChangeTracker**

```csharp
// Add ~4 tests for uncovered fields
// Test each tracked field not yet covered
// Test: set baseline then modify multiple fields -> all detected
// Test: deep copy verification for nested objects
```

**Step 4: Run and verify**

Run: `dotnet test tests/LYBT.Tests.Desktop/ --filter "FullyQualifiedName~WorkspaceState|FullyQualifiedName~ChangeTracker" -v normal`
Expected: ~27 tests total PASS

**Step 5: Commit**

```
test: supplement WorkspaceState (+3) and ChangeTracker (+4) tests
```

---

## Phase 4: Verification

### Task 16: Full test suite verification

**Step 1: Run Server tests**

Run: `dotnet test tests/LYBT.Tests.Server/ -v normal`
Expected: 1017 tests PASS (unchanged)

**Step 2: Run Integration tests**

Run: `dotnet test tests/LYBT.Tests.Integration/ -v normal`
Expected: ~22 tests PASS (new)

**Step 3: Run Desktop tests**

Run: `dotnet test tests/LYBT.Tests.Desktop/ -v normal`
Expected: ~200 tests PASS (after deletions + additions)

**Step 4: Run Architecture tests**

Run: `dotnet test tests/LYBT.Tests.Architecture/ -v normal`
Expected: 68+ tests PASS (may need AntiMockRules update if test count thresholds changed)

**Step 5: Run all**

Run: `dotnet test LYBT.All.sln --filter "FullyQualifiedName~LYBT.Tests" -v normal`
Expected: All PASS

**Step 6: Record final numbers**

Update `progress.md` with final summary table:

| Project | Before | After | Delta |
|---------|--------|-------|-------|
| Server | 1017 | 1017 | 0 |
| Integration | 0 | ~22 | +22 |
| Desktop | 494 | ~200 | -294 |
| Architecture | 68 | 68 | 0 |
| **Total** | **1579** | **~1307** | **-272** |

**Step 7: Commit**

```
test: verify full test suite after restructuring
```

---

## Summary

| Phase | Tasks | Tests Added | Tests Deleted | Net |
|-------|-------|-------------|---------------|-----|
| Phase 2 (Integration) | Tasks 1-7 | ~22 | 0 | +22 |
| Phase 3 (Desktop Cleanup) | Tasks 8-15 | ~29 | ~240 | -211 |
| Phase 4 (Verification) | Task 16 | 0 | 0 | 0 |
| **Total** | **16 tasks** | **~51** | **~240** | **-189** |

**Execution estimate:** ~3-4 hours for experienced implementer

**Risk areas:**
1. IntegrationFixture setup (Task 2) -- most complex, may need debugging
2. DTO property name mismatches in Integration tests -- check actual DTOs
3. Architecture test thresholds may need updating after Desktop test count drops
4. Some remaining PureLogic tests may have hidden DesktopFixture dependencies
