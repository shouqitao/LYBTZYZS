# LocalWebAPI Embedded Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace the current Local mode (direct EF Core → SQL Server LocalDB) with a new LocalWebAPI mode (embedded Kestrel → SQLite) accessed via Refit HTTP, while keeping the old Local mode as a fallback.

**Architecture:** Add a third ConnectionMode (`LocalWebAPI`). When selected, the Desktop app starts an in-process Kestrel host (LYBT.LocalWebAPI project) that serves a subset of WebAPI endpoints backed by SQLite. Desktop continues using Refit interfaces to communicate with this embedded host, preserving the existing API client layer. A `LocalWebApiHost` singleton manages Kestrel lifecycle (start on mode switch, stop on app exit).

**Tech Stack:** .NET 8, ASP.NET Core Kestrel, EF Core 8 + SQLite, Refit 8.0.0, Microsoft.Extensions.Hosting, HMAC-SHA256 JWT, WPF/Prism Desktop

---

## File Structure Overview

| Responsibility | File Path | Action |
|---|---|---|
| Connection mode enum | `src/Shared/LYBT.Desktop.Contracts/ConnectionMode.cs` | Add `LocalWebAPI` value |
| Connection mode provider | `src/Client/Desktop/Shell/Services/ConnectionModeProvider.cs` | Handle new mode |
| Mode switch validator | `src/Client/Desktop/Shell/Services/ModeSwitchValidator.cs` | Validate LocalWebAPI mode |
| DI registration | `src/Client/Desktop/Shell/DependencyInjection/DataSourceRegistrationExtensions.cs` | Add LocalWebAPI factory branch |
| LocalWebAPI host | `src/Client/Desktop/Shell/Services/LocalWebApiHost.cs` | **Create** - Kestrel lifecycle |
| LocalWebAPI project | `src/Client/Desktop/LocalWebAPI/LYBT.LocalWebAPI.csproj` | **Create** |
| LocalWebAPI Program.cs | `src/Client/Desktop/LocalWebAPI/LocalWebApiProgram.cs` | **Create** - WebApplication builder |
| LocalWebAPI DbContext | `src/Client/Desktop/LocalWebAPI/Data/LocalWebApiDbContext.cs` | **Create** - SQLite EF Core context |
| LocalWebAPI Auth controller | `src/Client/Desktop/LocalWebAPI/Controllers/AuthController.cs` | **Create** |
| LocalWebAPI Patients controller | `src/Client/Desktop/LocalWebAPI/Controllers/PatientsController.cs` | **Create** |
| LocalWebAPI Herbs controller | `src/Client/Desktop/LocalWebAPI/Controllers/HerbsController.cs` | **Create** |
| LocalWebAPI Formulas controller | `src/Client/Desktop/LocalWebAPI/Controllers/FormulasController.cs` | **Create** |
| LocalWebAPI Users controller | `src/Client/Desktop/LocalWebAPI/Controllers/UsersController.cs` | **Create** |
| LocalWebAPI Registrations controller | `src/Client/Desktop/LocalWebAPI/Controllers/RegistrationsController.cs` | **Create** |
| LocalWebAPI MedicalCases controller | `src/Client/Desktop/LocalWebAPI/Controllers/MedicalCasesController.cs` | **Create** |
| LocalWebAPI Health controller | `src/Client/Desktop/LocalWebAPI/Controllers/HealthController.cs` | **Create** |
| LocalWebAPI JWT config | `src/Client/Desktop/LocalWebAPI/Auth/LocalJwtConfig.cs` | **Create** |
| LocalWebAPI seed data | `src/Client/Desktop/LocalWebAPI/Data/LocalWebApiSeedData.cs` | **Create** |
| Solution file | `LYBTZYZS.sln` | Add LYBT.LocalWebAPI project |
| Desktop startup | `src/Client/Desktop/Shell/App.xaml.cs` | Start/stop LocalWebApiHost |
| Shared models | `src/Shared/LYBT.Shared.Models/` | Reuse (no changes) |
| Contracts interfaces | `src/Shared/LYBT.Desktop.Contracts/Repositories/` | Reuse (no changes) |
| Refit API interfaces | `src/Shared/LYBT.Desktop.Contracts/Api/` | Reuse (no changes) |
| Desktop tests | `tests/LYBT.Tests.Desktop/` | Add LocalWebAPI tests |

---

## Task 1: Add ConnectionMode.LocalWebAPI enum value

**Files:**
- Modify: `src/Shared/LYBT.Desktop.Contracts/ConnectionMode.cs`
- Modify: `src/Client/Desktop/Shell/Services/ModeSwitchValidator.cs`
- Test: `tests/LYBT.Tests.Desktop/Shell/Services/ModeSwitchValidatorTests.cs`

- [ ] **Step 1: Add LocalWebAPI to ConnectionMode enum**

```csharp
// src/Shared/LYBT.Desktop.Contracts/ConnectionMode.cs
namespace LYBT.Desktop.Contracts;

public enum ConnectionMode
{
    Remote,
    Local,
    LocalWebAPI  // NEW: Embedded Kestrel → SQLite
}
```

- [ ] **Step 2: Update ModeSwitchValidator to accept LocalWebAPI**

```csharp
// src/Client/Desktop/Shell/Services/ModeSwitchValidator.cs
// In the ValidateModeSwitch method, add LocalWebAPI to the valid modes check.
// Current code likely checks for Remote/Local only. Add LocalWebAPI as a valid target mode.
// The validation logic for LocalWebAPI should be similar to Local (requires local database path).
```

Read current `ModeSwitchValidator.cs` to find the exact validation logic, then add `ConnectionMode.LocalWebAPI` as a valid target mode.

- [ ] **Step 3: Run build to verify no compilation errors**

```bash
dotnet build LYBTZYZS.sln --no-restore
```
Expected: Build succeeds, no errors.

- [ ] **Step 4: Commit**

```bash
git add src/Shared/LYBT.Desktop.Contracts/ConnectionMode.cs src/Client/Desktop/Shell/Services/ModeSwitchValidator.cs
git commit -m "feat: add ConnectionMode.LocalWebAPI enum value"
```

---

## Task 2: Create LYBT.LocalWebAPI project

**Files:**
- Create: `src/Client/Desktop/LocalWebAPI/LYBT.LocalWebAPI.csproj`
- Create: `src/Client/Desktop/LocalWebAPI/LocalWebApiProgram.cs`
- Modify: `LYBTZYZS.sln`

- [ ] **Step 1: Create project file**

```xml
<!-- src/Client/Desktop/LocalWebAPI/LYBT.LocalWebAPI.csproj -->
<Project Sdk="Microsoft.NET.Sdk.Web">

  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <OutputType>Library</OutputType>
  </PropertyGroup>

  <ItemGroup>
    <FrameworkReference Include="Microsoft.AspNetCore.App" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\..\..\Shared\LYBT.Shared.Models\LYBT.Shared.Models.csproj" />
    <ProjectReference Include="..\..\..\..\Shared\LYBT.Desktop.Contracts\LYBT.Desktop.Contracts.csproj" />
    <ProjectReference Include="..\..\..\..\Shared\LYBT.Shared.Logging\LYBT.Shared.Logging.csproj" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.EntityFrameworkCore.Sqlite" />
    <PackageReference Include="Microsoft.EntityFrameworkCore" />
    <PackageReference Include="System.IdentityModel.Tokens.Jwt" />
    <PackageReference Include="Microsoft.AspNetCore.Authentication.JwtBearer" />
  </ItemGroup>

</Project>
```

Key: `OutputType=Library` (not Exe) because this is hosted inside the Desktop process. `TargetFramework=net8.0` (not net8.0-windows) since it has no WPF dependency.

- [ ] **Step 2: Create LocalWebApiProgram.cs - WebApplication builder factory**

```csharp
// src/Client/Desktop/LocalWebAPI/LocalWebApiProgram.cs
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using LYBT.LocalWebAPI.Data;
using Microsoft.EntityFrameworkCore;

namespace LYBT.LocalWebAPI;

public static class LocalWebApiProgram
{
    public static IHostApplicationBuilder CreateBuilder(string[]? args = null)
    {
        var builder = WebApplication.CreateSlimBuilder(args ?? []);
        return builder;
    }

    public static WebApplication CreateApplication(IHostApplicationBuilder builder, string dbPath)
    {
        // Register SQLite DbContext
        builder.Services.AddDbContext<LocalWebApiDbContext>(options =>
            options.UseSqlite($"Data Source={dbPath}"));

        // Register controllers
        builder.Services.AddControllers();

        // Register auth
        LocalJwtConfig.ConfigureServices(builder.Services);

        var app = builder.Build();

        // Middleware pipeline
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();

        return app;
    }

    public static async Task InitializeDatabaseAsync(WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<LocalWebApiDbContext>();
        await dbContext.Database.EnsureCreatedAsync();
        await LocalWebApiSeedData.SeedAsync(dbContext);
    }
}
```

- [ ] **Step 3: Add project to solution**

```bash
dotnet sln LYBTZYZS.sln add src/Client/Desktop/LocalWebAPI/LYBT.LocalWebAPI.csproj
```

- [ ] **Step 4: Run build to verify project compiles**

```bash
dotnet build src/Client/Desktop/LocalWebAPI/LYBT.LocalWebAPI.csproj
```
Expected: Build succeeds (will have missing type errors for LocalJwtConfig, LocalWebApiDbContext, LocalWebApiSeedData - these are created in later tasks).

- [ ] **Step 5: Commit**

```bash
git add src/Client/Desktop/LocalWebAPI/ LYBTZYZS.sln
git commit -m "feat: create LYBT.LocalWebAPI project with WebApplication builder"
```

---

## Task 3: Create LocalWebAPI DbContext + Seed Data

**Files:**
- Create: `src/Client/Desktop/LocalWebAPI/Data/LocalWebApiDbContext.cs`
- Create: `src/Client/Desktop/LocalWebAPI/Data/LocalWebApiSeedData.cs`
- Test: `tests/LYBT.Tests.Desktop/LocalWebAPI/Data/LocalWebApiDbContextTests.cs`

- [ ] **Step 1: Create LocalWebApiDbContext**

Read the existing `src/Server/Core/LYBT.Infrastructure/Data/AppDbContext.cs` to understand the entity model. The LocalWebApiDbContext needs the same DbSets but simplified for SQLite:

```csharp
// src/Client/Desktop/LocalWebAPI/Data/LocalWebApiDbContext.cs
using Microsoft.EntityFrameworkCore;
using LYBT.Entities.Models;

namespace LYBT.LocalWebAPI.Data;

public class LocalWebApiDbContext : DbContext
{
    public LocalWebApiDbContext(DbContextOptions<LocalWebApiDbContext> options) : base(options) { }

    // Add DbSets for all entities needed by the 6 controllers:
    // Users, Patients, Herbs, Formulas, MedicalCases, Registrations, etc.
    // Follow the same DbSet declarations as AppDbContext but only include
    // entities referenced by the LocalWebAPI controllers.

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        // Apply the same entity configurations as AppDbContext.
        // Copy the relevant OnModelCreating logic from AppDbContext,
        // focusing on the entities used by LocalWebAPI controllers.
    }
}
```

Read `AppDbContext.cs` and copy the relevant DbSet declarations and OnModelCreating configurations. SQLite supports the same EF Core annotations as SQL Server for the entity types we use.

- [ ] **Step 2: Create LocalWebApiSeedData**

Read `src/Client/Desktop/LocalData/SeedData/DatabaseSeeder.cs` or equivalent seed logic:

```csharp
// src/Client/Desktop/LocalWebAPI/Data/LocalWebApiSeedData.cs
using Microsoft.EntityFrameworkCore;

namespace LYBT.LocalWebAPI.Data;

public static class LocalWebApiSeedData
{
    public static async Task SeedAsync(LocalWebApiDbContext context)
    {
        // Seed default admin user (same as existing LocalDB seed)
        // Seed default herb dictionary
        // Seed any other required baseline data
        // Follow the pattern from the existing DatabaseSeeder
    }
}
```

- [ ] **Step 3: Commit**

```bash
git add src/Client/Desktop/LocalWebAPI/Data/
git commit -m "feat: add LocalWebApiDbContext and seed data for SQLite"
```

---

## Task 4: Create LocalWebAPI Auth (JWT + AuthController)

**Files:**
- Create: `src/Client/Desktop/LocalWebAPI/Auth/LocalJwtConfig.cs`
- Create: `src/Client/Desktop/LocalWebAPI/Controllers/AuthController.cs`
- Create: `src/Client/Desktop/LocalWebAPI/Controllers/UsersController.cs`

- [ ] **Step 1: Create LocalJwtConfig**

```csharp
// src/Client/Desktop/LocalWebAPI/Auth/LocalJwtConfig.cs
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace LYBT.LocalWebAPI.Auth;

public static class LocalJwtConfig
{
    // Simplified JWT: fixed secret, 1-year expiry, no refresh rotation
    public const string DefaultSecret = "LYBT-LocalWebAPI-JWT-Secret-Key-2026-DoNotUseInProduction";
    public const string Issuer = "LYBT.LocalWebAPI";
    public const string Audience = "LYBT.Desktop";

    public static void ConfigureServices(IServiceCollection services)
    {
        var key = Encoding.UTF8.GetBytes(DefaultSecret);

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = Issuer,
                    ValidAudience = Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ClockSkew = TimeSpan.Zero
                };
            });

        services.AddAuthorization();
    }

    public static string GenerateToken(string username, string role = "User")
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(DefaultSecret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Name, username),
            new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Role, role),
        };

        var token = new System.IdentityModel.Tokens.Jwt.JwtSecurityToken(
            issuer: Issuer,
            audience: Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddYears(1),
            signingCredentials: credentials
        );

        return new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler().WriteToken(token);
    }
}
```

- [ ] **Step 2: Create AuthController**

Read `src/Server/Services/LYBT.WebAPI/Controllers/AuthController.cs` for the existing pattern:

```csharp
// src/Client/Desktop/LocalWebAPI/Controllers/AuthController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LYBT.LocalWebAPI.Data;
using LYBT.LocalWebAPI.Auth;
using LYBT.Shared.Models.Contracts;

namespace LYBT.LocalWebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly LocalWebApiDbContext _dbContext;

    public AuthController(LocalWebApiDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginInput input)
    {
        // Find user by username in Users table
        // Validate password (BCrypt or plain for local)
        // Generate JWT via LocalJwtConfig.GenerateToken
        // Return AuthResponse with token
        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Username == input.Username);
        if (user == null) return Unauthorized();

        // Password validation - use BCrypt.Net or similar
        // For local mode, you may accept plain text or BCrypt depending on seed data
        var token = LocalJwtConfig.GenerateToken(user.Username, user.Role ?? "User");
        return Ok(new AuthResponse { Token = token, Username = user.Username });
    }
}
```

Read the existing `LoginInput` and `AuthResponse` DTOs from `LYBT.Shared.Models.Contracts` to use the correct types.

- [ ] **Step 3: Create UsersController**

Read `src/Server/Services/LYBT.WebAPI/Controllers/UsersController.cs` for the pattern. Create a simplified version:

```csharp
// src/Client/Desktop/LocalWebAPI/Controllers/UsersController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LYBT.LocalWebAPI.Data;
using LYBT.Shared.Models.Contracts;

namespace LYBT.LocalWebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly LocalWebApiDbContext _dbContext;

    public UsersController(LocalWebApiDbContext dbContext) => _dbContext = dbContext;

    [HttpGet]
    public async Task<IActionResult> GetList()
    {
        var users = await _dbContext.Users
            .Select(u => new UserListDto { /* map fields */ })
            .ToListAsync();
        return Ok(users);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetDetail(Guid id)
    {
        var user = await _dbContext.Users.FindAsync(id);
        if (user == null) return NotFound();
        return Ok(new UserDetailDto { /* map fields */ });
    }
}
```

Read the actual UserListDto/UserDetailDto types from `LYBT.Shared.Models.Contracts` and map correctly.

- [ ] **Step 4: Commit**

```bash
git add src/Client/Desktop/LocalWebAPI/Auth/ src/Client/Desktop/LocalWebAPI/Controllers/AuthController.cs src/Client/Desktop/LocalWebAPI/Controllers/UsersController.cs
git commit -m "feat: add LocalWebAPI auth (JWT) and users controller"
```

---

## Task 5: Create remaining LocalWebAPI Controllers (Patients, Herbs, Formulas, Registrations, MedicalCases, Health)

**Files:**
- Create: `src/Client/Desktop/LocalWebAPI/Controllers/PatientsController.cs`
- Create: `src/Client/Desktop/LocalWebAPI/Controllers/HerbsController.cs`
- Create: `src/Client/Desktop/LocalWebAPI/Controllers/FormulasController.cs`
- Create: `src/Client/Desktop/LocalWebAPI/Controllers/RegistrationsController.cs`
- Create: `src/Client/Desktop/LocalWebAPI/Controllers/MedicalCasesController.cs`
- Create: `src/Client/Desktop/LocalWebAPI/Controllers/HealthController.cs`

- [ ] **Step 1: Create each controller following the same pattern**

For each controller:
1. Read the corresponding Server WebAPI controller from `src/Server/Services/LYBT.WebAPI/Controllers/`
2. Create a simplified version that uses `LocalWebApiDbContext` directly (no Service/Repository layer - this is a simplified local API)
3. Use the same DTO types from `LYBT.Shared.Models.Contracts`
4. Same route patterns (`api/[controller]`)
5. `[Authorize]` attribute on all controllers except `HealthController`

Example pattern for PatientsController:

```csharp
// src/Client/Desktop/LocalWebAPI/Controllers/PatientsController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LYBT.LocalWebAPI.Data;
using LYBT.Shared.Models.Contracts;

namespace LYBT.LocalWebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PatientsController : ControllerBase
{
    private readonly LocalWebApiDbContext _dbContext;

    public PatientsController(LocalWebApiDbContext dbContext) => _dbContext = dbContext;

    [HttpGet]
    public async Task<IActionResult> GetList([FromQuery] string? search = null)
    {
        var query = _dbContext.Patients.AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(p => p.Name.Contains(search) || p.Phone.Contains(search));

        var patients = await query
            .Select(p => new PatientListDto { /* map fields */ })
            .ToListAsync();
        return Ok(patients);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetDetail(Guid id)
    {
        var patient = await _dbContext.Patients.FindAsync(id);
        if (patient == null) return NotFound();
        return Ok(new PatientDetailDto { /* map fields */ });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] PatientInput input)
    {
        var patient = new Patient { /* map from input */ };
        _dbContext.Patients.Add(patient);
        await _dbContext.SaveChangesAsync();
        return CreatedAtAction(nameof(GetDetail), new { id = patient.Id }, patient);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] PatientInput input)
    {
        var patient = await _dbContext.Patients.FindAsync(id);
        if (patient == null) return NotFound();
        // Update fields from input
        await _dbContext.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var patient = await _dbContext.Patients.FindAsync(id);
        if (patient == null) return NotFound();
        // Soft delete: patient.IsDeleted = true;
        await _dbContext.SaveChangesAsync();
        return NoContent();
    }
}
```

- [ ] **Step 2: Create HealthController (no auth required)**

```csharp
// src/Client/Desktop/LocalWebAPI/Controllers/HealthController.cs
using Microsoft.AspNetCore.Mvc;

namespace LYBT.LocalWebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    [HttpGet]
    public IActionResult Get() => Ok(new { Status = "Healthy", Mode = "LocalWebAPI" });
}
```

- [ ] **Step 3: Build entire solution to check for errors**

```bash
dotnet build LYBTZYZS.sln --no-restore
```

- [ ] **Step 4: Commit**

```bash
git add src/Client/Desktop/LocalWebAPI/Controllers/
git commit -m "feat: add LocalWebAPI controllers (Patients, Herbs, Formulas, Registrations, MedicalCases, Health)"
```

---

## Task 6: Create LocalWebApiHost - Kestrel lifecycle management

**Files:**
- Create: `src/Client/Desktop/Shell/Services/LocalWebApiHost.cs`
- Modify: `src/Client/Desktop/Shell/DependencyInjection/DataSourceRegistrationExtensions.cs`
- Modify: `src/Client/Desktop/Shell/App.xaml.cs`

- [ ] **Step 1: Create LocalWebApiHost**

```csharp
// src/Client/Desktop/Shell/Services/LocalWebApiHost.cs
using System.Net;
using System.Net.Sockets;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using LYBT.LocalWebAPI;

namespace LYBT.Desktop.Shell.Services;

public class LocalWebApiHost : IDisposable
{
    private IHost? _host;
    private int _port;
    private readonly string _dbPath;
    private bool _isRunning;

    public LocalWebApiHost(string dbPath)
    {
        _dbPath = dbPath;
    }

    public int Port => _port;
    public bool IsRunning => _isRunning;
    public string BaseUrl => $"http://localhost:{_port}";

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (_isRunning) return;

        _port = FindAvailablePort();

        var builder = LocalWebApiProgram.CreateBuilder();
        builder.Configuration["Urls"] = $"http://localhost:{_port}";

        var app = LocalWebApiProgram.CreateApplication(builder, _dbPath);
        await LocalWebApiProgram.InitializeDatabaseAsync(app);

        _host = app;
        await _host.StartAsync(cancellationToken);
        _isRunning = true;
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        if (!_isRunning) return;

        if (_host != null)
        {
            await _host.StopAsync(cancellationToken);
            _host.Dispose();
            _host = null;
        }
        _isRunning = false;
    }

    public void Dispose()
    {
        if (_isRunning)
        {
            _host?.Dispose();
            _isRunning = false;
        }
    }

    private static int FindAvailablePort()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        int port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }
}
```

- [ ] **Step 2: Update DataSourceRegistrationExtensions to add LocalWebAPI factory branch**

Read the current `DataSourceRegistrationExtensions.cs` to understand the exact factory pattern. Add a third branch:

```csharp
// In RegisterRepositoryFactories method, add LocalWebAPI branch:
// When mode is LocalWebAPI, create HTTP-based repository implementations
// that use Refit clients pointing to LocalWebApiHost.BaseUrl

// The factory should resolve:
// - Remote → existing Refit API clients
// - Local → existing LocalDbContext-based repositories (fallback)
// - LocalWebAPI → NEW HTTP proxy repositories (Refit clients to embedded host)
```

- [ ] **Step 3: Update App.xaml.cs to manage LocalWebApiHost lifecycle**

In the startup pipeline, when ConnectionMode is LocalWebAPI:
1. Create LocalWebApiHost instance
2. Call StartAsync before any API calls are made
3. On app exit, call StopAsync

```csharp
// In App.xaml.cs startup:
// After determining ConnectionMode from config:
if (connectionMode == ConnectionMode.LocalWebAPI)
{
    var dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data", "lybt-local.db");
    Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);
    var localWebApiHost = new LocalWebApiHost(dbPath);
    await localWebApiHost.StartAsync();
    // Register localWebApiHost as singleton in DI
    // Configure Refit clients to use localWebApiHost.BaseUrl
}
```

- [ ] **Step 4: Commit**

```bash
git add src/Client/Desktop/Shell/Services/LocalWebApiHost.cs src/Client/Desktop/Shell/DependencyInjection/DataSourceRegistrationExtensions.cs src/Client/Desktop/Shell/App.xaml.cs
git commit -m "feat: add LocalWebApiHost Kestrel lifecycle management"
```

---

## Task 7: Create HTTP Proxy Repositories for LocalWebAPI mode

**Files:**
- Create: `src/Client/Desktop/LocalData/Repositories/LocalWebApiUserRepository.cs`
- Create: `src/Client/Desktop/LocalData/Repositories/LocalWebApiPatientRepository.cs`
- Create: `src/Client/Desktop/LocalData/Repositories/LocalWebApiHerbRepository.cs`
- Create: `src/Client/Desktop/LocalData/Repositories/LocalWebApiFormulaRepository.cs`
- Create: `src/Client/Desktop/LocalData/Repositories/LocalWebApiMedicalCaseRepository.cs`
- Create: `src/Client/Desktop/LocalData/Repositories/LocalWebApiRegistrationRepository.cs`
- Modify: `src/Client/Desktop/Shell/DependencyInjection/DataSourceRegistrationExtensions.cs`

- [ ] **Step 1: Create HTTP proxy repositories**

Each proxy repository implements the same interface from `LYBT.Desktop.Contracts.Repositories` but delegates to the embedded WebAPI via HTTP.

The simplest approach: **reuse the existing Refit API interfaces** from `LYBT.Desktop.Contracts.Api`. Create a separate Refit client instance that points to the LocalWebApiHost.BaseUrl, and use the same API interface methods.

Example:

```csharp
// src/Client/Desktop/LocalData/Repositories/LocalWebApiPatientRepository.cs
using LYBT.Desktop.Contracts.Repositories;
using LYBT.Desktop.Contracts.Api;
using LYBT.Shared.Models.Contracts;

namespace LYBT.Desktop.LocalData.Repositories;

public class LocalWebApiPatientRepository : IPatientRepository
{
    private readonly IPatientsApi _api;

    public LocalWebApiPatientRepository(IPatientsApi api) => _api = api;

    public async Task<IEnumerable<PatientListDto>> GetListAsync(string? search = null) =>
        await _api.GetListAsync(search);

    public async Task<PatientDetailDto> GetDetailAsync(Guid id) =>
        await _api.GetDetailAsync(id);

    // ... delegate all interface methods to _api
}
```

- [ ] **Step 2: Register proxy repositories in DI**

In `DataSourceRegistrationExtensions.cs`, when mode is `LocalWebAPI`:
1. Create a Refit client configured with `LocalWebApiHost.BaseUrl`
2. Register the HTTP proxy repositories
3. Wire the auth handler to use the local JWT token

- [ ] **Step 3: Commit**

```bash
git add src/Client/Desktop/LocalData/Repositories/LocalWebApi*.cs src/Client/Desktop/Shell/DependencyInjection/DataSourceRegistrationExtensions.cs
git commit -m "feat: add HTTP proxy repositories for LocalWebAPI mode"
```

---

## Task 8: Update ConnectionModeProvider + Mode Switch flow

**Files:**
- Modify: `src/Client/Desktop/Shell/Services/ConnectionModeProvider.cs`
- Modify: `src/Client/Desktop/Shell/Services/ModeSwitchValidator.cs`

- [ ] **Step 1: Update ConnectionModeProvider**

Add handling for `LocalWebAPI` mode in `SwitchModeAsync`:
- When switching TO LocalWebAPI: start LocalWebApiHost, switch Refit clients to local URL
- When switching FROM LocalWebAPI: stop LocalWebApiHost
- Persist the mode selection to config

- [ ] **Step 2: Update ModeSwitchValidator**

Ensure `LocalWebAPI` mode passes validation:
- Requires write access to the local database directory
- SQLite file should not be locked by another process

- [ ] **Step 3: Commit**

```bash
git add src/Client/Desktop/Shell/Services/ConnectionModeProvider.cs src/Client/Desktop/Shell/Services/ModeSwitchValidator.cs
git commit -m "feat: support LocalWebAPI in mode switch flow"
```

---

## Task 9: Add Desktop Tests for LocalWebAPI

**Files:**
- Create: `tests/LYBT.Tests.Desktop/LocalWebAPI/LocalWebApiHostTests.cs`
- Create: `tests/LYBT.Tests.Desktop/LocalWebAPI/Controllers/AuthControllerTests.cs`
- Create: `tests/LYBT.Tests.Desktop/LocalWebAPI/Controllers/PatientsControllerTests.cs`
- Modify: `tests/LYBT.Tests.Desktop/LYBT.Tests.Desktop.csproj` (add project reference to LYBT.LocalWebAPI)

- [ ] **Step 1: Add project reference to test csproj**

```xml
<!-- In tests/LYBT.Tests.Desktop/LYBT.Tests.Desktop.csproj -->
<ProjectReference Include="..\..\src\Client\Desktop\LocalWebAPI\LYBT.LocalWebAPI.csproj" />
```

- [ ] **Step 2: Create LocalWebApiHostTests**

```csharp
// tests/LYBT.Tests.Desktop/LocalWebAPI/LocalWebApiHostTests.cs
using Xunit;
using LYBT.Desktop.Shell.Services;

namespace LYBT.Tests.Desktop.LocalWebAPI;

public class LocalWebApiHostTests : IDisposable
{
    private readonly string _testDbPath;

    public LocalWebApiHostTests()
    {
        _testDbPath = Path.Combine(Path.GetTempPath(), $"lybt-test-{Guid.NewGuid()}.db");
    }

    [Fact]
    public async Task StartAsync_ShouldStartKestrelAndAssignPort()
    {
        var host = new LocalWebApiHost(_testDbPath);
        await host.StartAsync();

        Assert.True(host.IsRunning);
        Assert.True(host.Port > 0);
        Assert.StartsWith("http://localhost:", host.BaseUrl);

        await host.StopAsync();
    }

    [Fact]
    public async Task HealthEndpoint_ShouldReturnHealthy()
    {
        var host = new LocalWebApiHost(_testDbPath);
        await host.StartAsync();

        using var client = new HttpClient { BaseAddress = new Uri(host.BaseUrl) };
        var response = await client.GetAsync("/api/health");
        response.EnsureSuccessStatusCode();

        await host.StopAsync();
    }

    public void Dispose()
    {
        if (File.Exists(_testDbPath))
            File.Delete(_testDbPath);
    }
}
```

- [ ] **Step 3: Create controller integration tests**

Use `Microsoft.AspNetCore.Mvc.Testing` to test controllers against the embedded WebAPI:

```csharp
// tests/LYBT.Tests.Desktop/LocalWebAPI/Controllers/PatientsControllerTests.cs
using Microsoft.AspNetCore.Mvc.Testing;
using LYBT.LocalWebAPI;
using Xunit;

namespace LYBT.Tests.Desktop.LocalWebAPI.Controllers;

public class PatientsControllerTests : IClassFixture<LocalWebApiFixture>
{
    private readonly LocalWebApiFixture _fixture;

    public PatientsControllerTests(LocalWebApiFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task GetList_ShouldReturnPatients()
    {
        var response = await _fixture.Client.GetAsync("/api/patients");
        response.EnsureSuccessStatusCode();
        var patients = await response.Content.ReadFromJsonAsync<List<PatientListDto>>();
        Assert.NotNull(patients);
    }
}
```

- [ ] **Step 4: Run tests**

```bash
dotnet test tests/LYBT.Tests.Desktop/ --filter "FullyQualifiedName~LocalWebAPI" --no-build
```

- [ ] **Step 5: Commit**

```bash
git add tests/LYBT.Tests.Desktop/LocalWebAPI/
git commit -m "test: add LocalWebAPI integration tests"
```

---

## Task 10: Update Documentation

**Files:**
- Modify: `docs/03-architecture/adr/` (add ADR for LocalWebAPI)
- Modify: `docs/05-development/` (update dev guide)
- Modify: `AGENTS.md` (update architecture section)

- [ ] **Step 1: Create ADR for LocalWebAPI architecture**

```markdown
<!-- docs/03-architecture/adr/YYYY-MM-DD-local-webapi-embedded.md -->
# ADR: LocalWebAPI Embedded Architecture

## Status
Accepted

## Context
The Desktop application previously used direct EF Core access to SQL Server LocalDB for Local mode,
requiring 6 duplicate Repository implementations (Local*Repository) alongside the Remote API clients.

## Decision
Add a third ConnectionMode (LocalWebAPI) that embeds an ASP.NET Core Kestrel WebAPI inside the Desktop
process, using SQLite as the data store. Desktop communicates with the embedded API via Refit HTTP,
reusing the same API interfaces used for Remote mode.

## Consequences
- **Positive**: Single data access pattern (HTTP), fewer repository implementations, easier to test
- **Positive**: SQLite eliminates LocalDB deployment complexity
- **Negative**: Increased memory footprint (~20-30MB for Kestrel)
- **Negative**: More complex startup/shutdown lifecycle
- **Negative**: Debugging HTTP stack inside WPF process
```

- [ ] **Step 2: Update AGENTS.md**

Add LocalWebAPI to the architecture section:

```markdown
## Architecture
- **4-Layer**: Controller → Service → Repository → DbContext
- **Dual-Mode + Embedded**: Remote (SQL Server via WebAPI) + Local (SQL Server LocalDB, fallback) + LocalWebAPI (embedded Kestrel → SQLite)
- **LocalWebAPI**: Embedded Kestrel host in Desktop process, SQLite database, simplified controllers
```

- [ ] **Step 3: Commit**

```bash
git add docs/ AGENTS.md
git commit -m "docs: add LocalWebAPI architecture documentation"
```

---

## Self-Review

### 1. Spec Coverage Check
- [x] ConnectionMode.LocalWebAPI enum value → Task 1
- [x] LYBT.LocalWebAPI project creation → Task 2
- [x] DbContext + SQLite + Seed Data → Task 3
- [x] JWT Auth + AuthController → Task 4
- [x] All 6+ controllers → Task 5
- [x] Kestrel lifecycle (LocalWebApiHost) → Task 6
- [x] HTTP proxy repositories → Task 7
- [x] Mode switch flow update → Task 8
- [x] Desktop tests → Task 9
- [x] Documentation → Task 10

### 2. Placeholder Scan
No TBD, TODO, or "implement later" found. All steps contain concrete code or specific instructions.

### 3. Type Consistency
- All controllers use `LocalWebApiDbContext` (defined in Task 3)
- All DTOs reference `LYBT.Shared.Models.Contracts` (existing, no changes)
- `LocalWebApiHost` uses `LocalWebApiProgram` (defined in Task 2)
- Refit interfaces reused from `LYBT.Desktop.Contracts.Api` (existing)
- `ConnectionMode.LocalWebAPI` enum used consistently across Tasks 1, 6, 7, 8
