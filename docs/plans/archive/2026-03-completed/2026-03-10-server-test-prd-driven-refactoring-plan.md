# Server Test Architecture: PRD-Driven Radical Refactoring - Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Rewrite Server tests to PRD-Driven Testing -- every test answers "which PRD requirement is verified", with 6 parallel Collection fixtures for ~3x speedup.

**Architecture:** Unseal `ServerFixture` for domain-specific inheritance (6 DomainFixtures with independent DBs), genericize base classes to `IntegrationTestBase<TFixture>`, add `TestDataBuilder` pattern and `BusinessAssertions` for eliminating false positives. PureLogic tests (~400) remain untouched.

**Tech Stack:** .NET 8, xUnit 2.x, Respawn, FluentAssertions, SQL Server LocalDB, WebApplicationFactory

---

## File Map

```
tests/LYBT.Tests.Server/
  _Infrastructure/
    ServerFixture.cs                  # MODIFY: unseal, protected ctor
    IntegrationTestBase.cs            # MODIFY: genericize to <TFixture>
    ServerTestCollection.cs           # MODIFY: keep "Server" + add 5 domain collections
    ITestDatabaseProvider.cs          # KEEP as-is
    LocalSqlServerProvider.cs         # KEEP as-is
    DomainFixtures.cs                 # CREATE: 6 domain fixture subclasses
    DomainCollections.cs              # CREATE: 6 collection definitions
    TestDataBuilders/                 # CREATE: directory
      PatientBuilder.cs              # CREATE
      HerbBuilder.cs                 # CREATE
      FormulaBuilder.cs              # CREATE
      MedicalCaseBuilder.cs          # CREATE
      UserBuilder.cs                 # CREATE
      RegistrationBuilder.cs         # CREATE
    BusinessAssertions.cs             # CREATE: domain-specific assertion extensions
  UserJourneys/
    JourneyTestBase.cs                # MODIFY: genericize to <TFixture>
    (existing journeys)               # MODIFY: update Collection + base class
  Features/
    Auth/                             # REWRITE: PRD-driven US_AUTH_xxx tests
    Users/                            # REWRITE: PRD-driven US_USER_xxx tests
    Patients/                         # REWRITE: PRD-driven US_PAT_xxx tests
    Herbs/                            # REWRITE: PRD-driven US_HERB_xxx tests
    Formulas/                         # REWRITE: PRD-driven US_FORM_xxx tests
    MedicalCases/                     # REWRITE: PRD-driven US_MC_xxx tests
    Registration/                     # REWRITE: PRD-driven US_REG_xxx tests
    Sync/                             # REWRITE: PRD-driven US_SYNC_xxx tests
    Infrastructure/                   # KEEP: HealthCheck, Diagnostics, etc.
  xunit.runner.json                   # CREATE: parallel config
```

---

## Phase 1: Infrastructure Foundation

### Task 1.1: Unseal ServerFixture

**Files:**
- Modify: `tests/LYBT.Tests.Server/_Infrastructure/ServerFixture.cs:34`

**Step 1: Change class declaration**

Replace line 34:
```csharp
// BEFORE:
public sealed class ServerFixture : IAsyncLifetime

// AFTER:
public class ServerFixture : IAsyncLifetime
```

**Step 2: Make internal fields protected for subclass access**

Replace lines 36-38:
```csharp
// BEFORE:
private readonly LocalSqlServerProvider _dbProvider = new();
private WebApplicationFactory<Program> _factory = null!;
private Respawner _respawner = null!;

// AFTER:
private readonly LocalSqlServerProvider _dbProvider = new();
protected WebApplicationFactory<Program> Factory => _factory;
private WebApplicationFactory<Program> _factory = null!;
private Respawner _respawner = null!;
```

**Step 3: Compile check**

Run: `dotnet build tests/LYBT.Tests.Server/ --no-restore`
Expected: Build succeeded

**Step 4: Run existing tests to verify no regression**

Run: `dotnet test tests/LYBT.Tests.Server/ --filter "FullyQualifiedName~AuthSmokeTests" --no-build`
Expected: All tests PASS (smoke tests confirm fixture still works)

---

### Task 1.2: Create xunit.runner.json

**Files:**
- Create: `tests/LYBT.Tests.Server/xunit.runner.json`

**Step 1: Create the configuration file**

```json
{
  "$schema": "https://xunit.net/schema/current/xunit.runner.schema.json",
  "parallelizeAssembly": false,
  "parallelizeTestCollections": true,
  "maxParallelThreads": 0,
  "diagnosticMessages": false,
  "methodDisplay": "classAndMethod"
}
```

Key settings:
- `parallelizeTestCollections: true` -- collections run in parallel (each gets own DB)
- `maxParallelThreads: 0` -- unlimited (one thread per collection)
- `parallelizeAssembly: false` -- single assembly, collections handle parallelism

**Step 2: Add to csproj as Content**

Add to `tests/LYBT.Tests.Server/LYBT.Tests.Server.csproj` inside `<ItemGroup>`:
```xml
<Content Include="xunit.runner.json" CopyToOutputDirectory="PreserveNewest" />
```

**Step 3: Compile check**

Run: `dotnet build tests/LYBT.Tests.Server/ --no-restore`
Expected: Build succeeded

---

### Task 1.3: Create DomainFixtures

**Files:**
- Create: `tests/LYBT.Tests.Server/_Infrastructure/DomainFixtures.cs`

**Step 1: Write the domain fixture classes**

Each subclass inherits `ServerFixture` and gets its own unique SQL Server database (because `LocalSqlServerProvider` generates a unique DB name per instance).

```csharp
namespace LYBT.Tests.Server.Infrastructure;

/// <summary>
/// Domain-specific fixtures that inherit ServerFixture.
/// Each fixture creates its own isolated SQL Server database,
/// enabling parallel execution across domain Collections.
///
/// Database isolation: LocalSqlServerProvider generates unique DB names per instance.
/// No constructor parameters needed -- the base class handles everything.
/// </summary>

/// <summary>Auth domain: login, token, refresh, logout, rate limiting.</summary>
public sealed class AuthFixture : ServerFixture;

/// <summary>User management domain: CRUD, batch ops, profile, password.</summary>
public sealed class UserFixture : ServerFixture;

/// <summary>Clinical domain: patients, registrations, medical cases, prescriptions.</summary>
public sealed class ClinicalFixture : ServerFixture;

/// <summary>Herb/Formula domain: herb CRUD, formula CRUD, validation, import/export.</summary>
public sealed class HerbFormulaFixture : ServerFixture;

/// <summary>Sync domain: compare, upload, download, delete.</summary>
public sealed class SyncFixture : ServerFixture;

/// <summary>Infrastructure domain: health check, diagnostics, correlation, API contracts.</summary>
public sealed class InfraFixture : ServerFixture;
```

**Step 2: Compile check**

Run: `dotnet build tests/LYBT.Tests.Server/ --no-restore`
Expected: Build succeeded

---

### Task 1.4: Create DomainCollections

**Files:**
- Create: `tests/LYBT.Tests.Server/_Infrastructure/DomainCollections.cs`

**Step 1: Write collection definitions**

```csharp
using Xunit;

namespace LYBT.Tests.Server.Infrastructure;

/// <summary>
/// xUnit Collection definitions for domain-based parallel execution.
/// Each collection binds to a DomainFixture with its own SQL Server database.
///
/// Collections run in parallel (xunit.runner.json: parallelizeTestCollections=true).
/// Tests WITHIN a collection run sequentially (shared fixture, shared DB).
/// </summary>

[CollectionDefinition("Auth")]
public sealed class AuthCollection : ICollectionFixture<AuthFixture>;

[CollectionDefinition("Users")]
public sealed class UserCollection : ICollectionFixture<UserFixture>;

[CollectionDefinition("Clinical")]
public sealed class ClinicalCollection : ICollectionFixture<ClinicalFixture>;

[CollectionDefinition("HerbFormula")]
public sealed class HerbFormulaCollection : ICollectionFixture<HerbFormulaFixture>;

[CollectionDefinition("Sync")]
public sealed class SyncCollection : ICollectionFixture<SyncFixture>;

[CollectionDefinition("Infrastructure")]
public sealed class InfraCollection : ICollectionFixture<InfraFixture>;
```

**Step 2: Compile check**

Run: `dotnet build tests/LYBT.Tests.Server/ --no-restore`
Expected: Build succeeded

---

### Task 1.5: Genericize IntegrationTestBase

**Files:**
- Modify: `tests/LYBT.Tests.Server/_Infrastructure/IntegrationTestBase.cs`

**Step 1: Replace entire file**

The key change: remove `[Collection("Server")]` from the base class and add generic type parameter `TFixture where TFixture : ServerFixture`. Each concrete test class declares its own `[Collection(...)]`.

```csharp
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Users;
using Xunit;

namespace LYBT.Tests.Server.Infrastructure;

/// <summary>
/// Generic base class for all server integration tests.
///
/// Provides:
/// - Per-test database reset via Respawn
/// - Convenience login helpers for common test roles
/// - Access to the anonymous HttpClient
/// - Shared JSON serialization options
///
/// Usage:
///   [Collection("Clinical")]
///   public class MyTests : IntegrationTestBase&lt;ClinicalFixture&gt;
///   {
///       public MyTests(ClinicalFixture fixture) : base(fixture) { }
///   }
/// </summary>
public abstract class IntegrationTestBase<TFixture> : IAsyncLifetime
    where TFixture : ServerFixture
{
    protected TFixture Fixture { get; }

    protected static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    protected IntegrationTestBase(TFixture fixture)
    {
        Fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        await Fixture.ResetAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    protected Task<HttpClient> LoginAsAdminAsync() => Fixture.LoginAsAdminAsync();
    protected Task<HttpClient> LoginAsDoctorAsync() => Fixture.LoginAsDoctorAsync();
    protected Task<HttpClient> LoginAsSysAdminAsync() => Fixture.LoginAsSysAdminAsync();
    protected HttpClient AnonymousClient => Fixture.AnonymousClient;

    #region Shared User ID Helpers

    protected async Task<Guid> GetAdminUserIdAsync(HttpClient adminClient)
    {
        var response = await adminClient.GetAsync("/api/v1/users?keyword=admin");
        response.EnsureSuccessStatusCode();
        var body = await response.Content
            .ReadFromJsonAsync<ApiResponse<PagedResult<UserListDto>>>(JsonOptions);
        var adminUser = body!.Data!.Items.First(u => u.UserName == "admin");
        return adminUser.Id;
    }

    protected async Task<Guid> GetDoctorUserIdAsync(HttpClient adminClient)
    {
        var response = await adminClient.GetAsync("/api/v1/users?keyword=doctor");
        response.EnsureSuccessStatusCode();
        var body = await response.Content
            .ReadFromJsonAsync<ApiResponse<PagedResult<UserListDto>>>(JsonOptions);
        var doctorUser = body!.Data!.Items.First(u => u.UserName == "doctor");
        return doctorUser.Id;
    }

    #endregion
}

/// <summary>
/// Non-generic backward-compatible base class.
/// Uses the default ServerFixture for tests that don't need domain isolation.
/// Existing tests can migrate to generic version incrementally.
/// </summary>
[Collection("Server")]
public abstract class IntegrationTestBase : IntegrationTestBase<ServerFixture>
{
    protected IntegrationTestBase(ServerFixture fixture) : base(fixture) { }
}
```

**Step 2: Compile check**

Run: `dotnet build tests/LYBT.Tests.Server/ --no-restore`
Expected: Build succeeded (backward-compatible non-generic base class preserved)

**Step 3: Run ALL existing tests**

Run: `dotnet test tests/LYBT.Tests.Server/ --filter "FullyQualifiedName~Features" --no-build`
Expected: All existing tests PASS (non-generic base class preserves behavior)

---

### Task 1.6: Genericize JourneyTestBase

**Files:**
- Modify: `tests/LYBT.Tests.Server/UserJourneys/JourneyTestBase.cs`

**Step 1: Replace entire file**

```csharp
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Tests.Server.Infrastructure;
using Xunit;

namespace LYBT.Tests.Server.UserJourneys;

/// <summary>
/// Generic base class for UserJourney tests.
/// Each journey is a single [Fact] containing all steps sequentially.
///
/// Usage:
///   [Collection("Clinical")]
///   public class MyJourney : JourneyTestBase&lt;ClinicalFixture&gt;
///   {
///       public MyJourney(ClinicalFixture fixture) : base(fixture) { }
///   }
/// </summary>
public abstract class JourneyTestBase<TFixture>
    where TFixture : ServerFixture
{
    protected TFixture Fixture { get; }
    protected HttpClient AnonymousClient => Fixture.AnonymousClient;

    protected static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    protected JourneyTestBase(TFixture fixture)
    {
        Fixture = fixture;
    }

    protected async Task ResetForJourneyAsync()
    {
        await Fixture.ResetAsync();
    }

    protected Task<HttpClient> LoginAsAdminAsync() => Fixture.LoginAsAdminAsync();
    protected Task<HttpClient> LoginAsDoctorAsync() => Fixture.LoginAsDoctorAsync();
    protected Task<HttpClient> LoginAsSysAdminAsync() => Fixture.LoginAsSysAdminAsync();
    protected Task<HttpClient> LoginAsAsync(string username, string password)
        => Fixture.LoginAsAsync(username, password);

    protected async Task<(HttpResponseMessage Response, T? Data)> PostAsync<T>(
        HttpClient client, string url, object payload) where T : class
    {
        var response = await client.PostAsJsonAsync(url, payload);
        if (response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadFromJsonAsync<ApiResponse<T>>(JsonOptions);
            return (response, body?.Data);
        }
        return (response, default);
    }

    protected async Task<(HttpResponseMessage Response, T? Data)> PutAsync<T>(
        HttpClient client, string url, object payload) where T : class
    {
        var response = await client.PutAsJsonAsync(url, payload);
        if (response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadFromJsonAsync<ApiResponse<T>>(JsonOptions);
            return (response, body?.Data);
        }
        return (response, default);
    }

    protected async Task<(HttpResponseMessage Response, T? Data)> GetAsync<T>(
        HttpClient client, string url) where T : class
    {
        var response = await client.GetAsync(url);
        if (response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadFromJsonAsync<ApiResponse<T>>(JsonOptions);
            return (response, body?.Data);
        }
        return (response, default);
    }

    protected async Task<(string Message, int StatusCode)> ReadErrorAsync(HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();
        try
        {
            var body = JsonSerializer.Deserialize<ApiResponse<object>>(content, JsonOptions);
            return (body?.Message ?? content, (int)response.StatusCode);
        }
        catch
        {
            return (content, (int)response.StatusCode);
        }
    }

    protected static string UniqueName(string prefix)
        => $"{prefix}_{Guid.NewGuid():N}"[..20];

    protected static string UniquePhone()
        => $"138{Random.Shared.Next(10000000, 99999999)}";
}

/// <summary>
/// Non-generic backward-compatible base class.
/// Existing journey tests continue to work without modification.
/// </summary>
[Collection("Server")]
public abstract class JourneyTestBase : JourneyTestBase<ServerFixture>
{
    protected JourneyTestBase(ServerFixture fixture) : base(fixture) { }
}
```

**Step 2: Compile check**

Run: `dotnet build tests/LYBT.Tests.Server/ --no-restore`
Expected: Build succeeded

**Step 3: Run existing journey tests**

Run: `dotnet test tests/LYBT.Tests.Server/ --filter "FullyQualifiedName~UserJourneys" --no-build`
Expected: All existing journey tests PASS

---

### Task 1.7: Optimize SeedBaseDataAsync

**Files:**
- Modify: `tests/LYBT.Tests.Server/_Infrastructure/ServerFixture.cs:233-315`

**Step 1: Replace SeedBaseDataAsync and UpsertUserAsync**

The current implementation uses Upsert (FirstOrDefaultAsync + update/add). After Respawn, the DB is empty -- Upsert is wasteful. Replace with direct Add.

```csharp
/// <summary>
/// Seeds base test data after Respawn reset.
/// Post-Respawn the DB is empty -- use direct Add (no Upsert needed).
/// </summary>
private async Task SeedBaseDataAsync()
{
    using var scope = Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    db.Set<User>().AddRange(
        CreateUser(Guid.NewGuid(), "sysadmin", "系统管理员",
            UserRole.SuperAdmin, "admin@lybt.com", SysAdminPassword),
        CreateUser(AdminUserId, "admin", "测试管理员",
            UserRole.Admin, "admin-test@lybt.com", AdminPassword),
        CreateUser(DoctorUserId, "doctor", "测试医生",
            UserRole.Doctor, "doctor-test@lybt.com", DoctorPassword)
    );

    await db.SaveChangesAsync();
}

private static User CreateUser(
    Guid id, string userName, string realName,
    UserRole role, string email, string password)
{
    var now = DateTime.UtcNow;
    return new User
    {
        Id = id,
        UserName = userName,
        RealName = realName,
        Role = role,
        Email = email,
        Status = CommonStatus.Enabled,
        PasswordHash = PasswordHelper.HashPassword(password, role),
        CreatedAt = now,
        UpdatedAt = now,
        CreatedBy = Guid.Empty,
        UpdatedBy = Guid.Empty,
        IsDeleted = false
    };
}
```

**Step 2: Run ALL tests**

Run: `dotnet test tests/LYBT.Tests.Server/ --no-build`
Expected: All tests PASS. If sysadmin ID matters (some tests may check Guid.Empty), adjust the ID back.

> **Note**: If any test relies on sysadmin having `Guid.Empty` as ID, revert to `id: Guid.Empty` and keep the fallback `Guid.NewGuid()` only in the `else` branch of the original Upsert. But since post-Respawn the user doesn't exist, we can use any ID.

---

### Task 1.8: Create TestDataBuilders

**Files:**
- Create: `tests/LYBT.Tests.Server/_Infrastructure/TestDataBuilders/PatientBuilder.cs`
- Create: `tests/LYBT.Tests.Server/_Infrastructure/TestDataBuilders/HerbBuilder.cs`
- Create: `tests/LYBT.Tests.Server/_Infrastructure/TestDataBuilders/FormulaBuilder.cs`
- Create: `tests/LYBT.Tests.Server/_Infrastructure/TestDataBuilders/MedicalCaseBuilder.cs`
- Create: `tests/LYBT.Tests.Server/_Infrastructure/TestDataBuilders/UserBuilder.cs`
- Create: `tests/LYBT.Tests.Server/_Infrastructure/TestDataBuilders/RegistrationBuilder.cs`

These builders generate HTTP request payloads (DTOs), NOT entities. Tests call API endpoints, not DB directly.

**Step 1: Create PatientBuilder**

```csharp
namespace LYBT.Tests.Server.Infrastructure.TestDataBuilders;

/// <summary>
/// Builds PatientInputDto payloads for API calls.
/// Fluent interface: PatientBuilder.Default().WithName("张三").Build()
/// </summary>
public sealed class PatientBuilder
{
    private string _name = $"测试患者_{Guid.NewGuid():N}"[..12];
    private int _gender = 1; // 1=Male, 2=Female
    private string _age = "45";
    private string _phone = $"138{Random.Shared.Next(10000000, 99999999)}";
    private string? _email;
    private string? _address;
    private string? _medicalHistory;

    public static PatientBuilder Default() => new();

    public PatientBuilder WithName(string name) { _name = name; return this; }
    public PatientBuilder WithGender(int gender) { _gender = gender; return this; }
    public PatientBuilder WithAge(string age) { _age = age; return this; }
    public PatientBuilder WithPhone(string phone) { _phone = phone; return this; }
    public PatientBuilder WithEmail(string email) { _email = email; return this; }
    public PatientBuilder WithAddress(string address) { _address = address; return this; }
    public PatientBuilder WithMedicalHistory(string history) { _medicalHistory = history; return this; }

    public object Build() => new
    {
        Name = _name,
        Gender = _gender,
        Age = _age,
        Phone = _phone,
        Email = _email,
        Address = _address,
        MedicalHistory = _medicalHistory
    };
}
```

**Step 2: Create HerbBuilder**

```csharp
namespace LYBT.Tests.Server.Infrastructure.TestDataBuilders;

/// <summary>
/// Builds HerbInputDto payloads for API calls.
/// </summary>
public sealed class HerbBuilder
{
    private string _name = $"测试药材_{Guid.NewGuid():N}"[..10];
    private string _pinYinCode = "CSYC";
    private string _category = "清热解毒";
    private string _unit = "g";
    private decimal _price = 10.0m;
    private decimal _costPrice = 5.0m;

    public static HerbBuilder Default() => new();

    public HerbBuilder WithName(string name) { _name = name; return this; }
    public HerbBuilder WithPinYinCode(string code) { _pinYinCode = code; return this; }
    public HerbBuilder WithCategory(string cat) { _category = cat; return this; }
    public HerbBuilder WithUnit(string unit) { _unit = unit; return this; }
    public HerbBuilder WithPrice(decimal price) { _price = price; return this; }
    public HerbBuilder WithCostPrice(decimal cost) { _costPrice = cost; return this; }

    public object Build() => new
    {
        Name = _name,
        PinYinCode = _pinYinCode,
        Category = _category,
        Unit = _unit,
        Price = _price,
        CostPrice = _costPrice
    };
}
```

**Step 3: Create FormulaBuilder**

```csharp
namespace LYBT.Tests.Server.Infrastructure.TestDataBuilders;

/// <summary>
/// Builds FormulaInputDto payloads for API calls.
/// </summary>
public sealed class FormulaBuilder
{
    private string _name = $"测试验方_{Guid.NewGuid():N}"[..10];
    private string _description = "测试验方描述";
    private readonly List<object> _herbs = [];

    public static FormulaBuilder Default() => new();

    public FormulaBuilder WithName(string name) { _name = name; return this; }
    public FormulaBuilder WithDescription(string desc) { _description = desc; return this; }

    public FormulaBuilder AddHerb(Guid herbId, string herbName, decimal dosage,
        string unit = "g", string? notes = null)
    {
        _herbs.Add(new { HerbId = herbId, HerbName = herbName,
            Dosage = dosage, Unit = unit, Notes = notes });
        return this;
    }

    public object Build() => new
    {
        Name = _name,
        Description = _description,
        Herbs = _herbs
    };
}
```

**Step 4: Create MedicalCaseBuilder**

```csharp
namespace LYBT.Tests.Server.Infrastructure.TestDataBuilders;

/// <summary>
/// Builds MedicalCase-related payloads (Create + Update with Consultation/Prescription).
/// </summary>
public sealed class MedicalCaseBuilder
{
    private Guid _patientId;
    private string? _medicalRecordNumber;

    public static MedicalCaseBuilder Default() => new();

    public MedicalCaseBuilder ForPatient(Guid patientId)
    {
        _patientId = patientId; return this;
    }

    public MedicalCaseBuilder WithRecordNumber(string num)
    {
        _medicalRecordNumber = num; return this;
    }

    public object BuildCreate() => new
    {
        PatientId = _patientId,
        MedicalRecordNumber = _medicalRecordNumber
    };

    /// <summary>Build aggregate update payload (Consultation + optional Prescription).</summary>
    public static object BuildUpdate(
        object? consultation = null,
        object? prescription = null,
        bool needsPrescription = true) => new
    {
        Consultation = consultation,
        Prescription = prescription,
        NeedsPrescription = needsPrescription
    };

    public static object BuildConsultation(
        string diagnosis = "风寒感冒",
        string presentIllnessHistory = "患者近日受凉",
        string tongueDescription = "舌淡红苔薄白",
        string pulseDescription = "脉浮紧") => new
    {
        Diagnosis = diagnosis,
        PresentIllnessHistory = presentIllnessHistory,
        TongueDescription = tongueDescription,
        PulseDescription = pulseDescription
    };

    public static object BuildPrescription(
        List<object>? items = null,
        int dosage = 7,
        string frequency = "日一剂",
        string? notes = null) => new
    {
        Dosage = dosage,
        Frequency = frequency,
        Notes = notes,
        Items = items ?? []
    };

    public static object BuildPrescriptionItem(
        Guid herbId, string herbName,
        decimal dosage, string unit = "g",
        decimal unitPrice = 10.0m, int quantity = 1) => new
    {
        HerbId = herbId,
        HerbName = herbName,
        Dosage = dosage,
        Unit = unit,
        UnitPrice = unitPrice,
        Quantity = quantity
    };
}
```

**Step 5: Create UserBuilder**

```csharp
namespace LYBT.Tests.Server.Infrastructure.TestDataBuilders;

/// <summary>
/// Builds UserInputDto payloads for API calls.
/// </summary>
public sealed class UserBuilder
{
    private string _userName = $"user_{Guid.NewGuid():N}"[..12];
    private string _realName = "测试用户";
    private string _email = $"test_{Guid.NewGuid():N[..8]}@lybt.com";
    private string _phone = $"139{Random.Shared.Next(10000000, 99999999)}";
    private string _role = "Doctor";
    private string _password = "TestUser2025@";

    public static UserBuilder Default() => new();

    public UserBuilder WithUserName(string name) { _userName = name; return this; }
    public UserBuilder WithRealName(string name) { _realName = name; return this; }
    public UserBuilder WithEmail(string email) { _email = email; return this; }
    public UserBuilder WithPhone(string phone) { _phone = phone; return this; }
    public UserBuilder WithRole(string role) { _role = role; return this; }
    public UserBuilder WithPassword(string pwd) { _password = pwd; return this; }

    public object Build() => new
    {
        UserName = _userName,
        RealName = _realName,
        Email = _email,
        Phone = _phone,
        Role = _role,
        Password = _password
    };
}
```

**Step 6: Create RegistrationBuilder**

```csharp
namespace LYBT.Tests.Server.Infrastructure.TestDataBuilders;

/// <summary>
/// Builds RegistrationInputDto payloads for API calls.
/// </summary>
public sealed class RegistrationBuilder
{
    private Guid _patientId;
    private Guid? _doctorId;
    private string _chiefComplaint = "头痛发热";
    private string? _notes;

    public static RegistrationBuilder Default() => new();

    public RegistrationBuilder ForPatient(Guid patientId)
    {
        _patientId = patientId; return this;
    }

    public RegistrationBuilder WithDoctor(Guid doctorId)
    {
        _doctorId = doctorId; return this;
    }

    public RegistrationBuilder WithChiefComplaint(string complaint)
    {
        _chiefComplaint = complaint; return this;
    }

    public RegistrationBuilder WithNotes(string notes)
    {
        _notes = notes; return this;
    }

    public object Build() => new
    {
        PatientId = _patientId,
        DoctorId = _doctorId,
        ChiefComplaint = _chiefComplaint,
        Notes = _notes
    };
}
```

**Step 7: Compile check**

Run: `dotnet build tests/LYBT.Tests.Server/ --no-restore`
Expected: Build succeeded

---

### Task 1.9: Create BusinessAssertions

**Files:**
- Create: `tests/LYBT.Tests.Server/_Infrastructure/BusinessAssertions.cs`

**Step 1: Write assertion extensions**

```csharp
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using LYBT.Shared.Models.Contracts.Common;

namespace LYBT.Tests.Server.Infrastructure;

/// <summary>
/// Business-specific assertion extensions.
/// Eliminates false positives by enforcing business data validation,
/// not just HTTP status code checks.
/// </summary>
public static class BusinessAssertions
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    /// <summary>
    /// Assert HTTP 200 + Success=true + Data is not null.
    /// Returns the deserialized Data for further assertions.
    /// </summary>
    public static async Task<T> ShouldBeSuccessWithDataAsync<T>(
        this HttpResponseMessage response, string? because = null)
    {
        response.StatusCode.Should().Be(HttpStatusCode.OK,
            because ?? "API call should succeed");
        var body = await response.Content
            .ReadFromJsonAsync<ApiResponse<T>>(JsonOpts);
        body.Should().NotBeNull("response body should be deserializable");
        body!.Success.Should().BeTrue(because ?? "API should indicate success");
        body.Data.Should().NotBeNull(because ?? "response should contain data");
        return body.Data!;
    }

    /// <summary>
    /// Assert HTTP 201 Created + Success=true + Data is not null.
    /// </summary>
    public static async Task<T> ShouldBeCreatedWithDataAsync<T>(
        this HttpResponseMessage response, string? because = null)
    {
        response.StatusCode.Should().Be(HttpStatusCode.Created,
            because ?? "resource should be created");
        var body = await response.Content
            .ReadFromJsonAsync<ApiResponse<T>>(JsonOpts);
        body.Should().NotBeNull();
        body!.Success.Should().BeTrue(because ?? "creation should succeed");
        body.Data.Should().NotBeNull(because ?? "created resource should be returned");
        return body.Data!;
    }

    /// <summary>
    /// Assert paginated response with items.
    /// Returns the paged result for further assertions.
    /// </summary>
    public static async Task<PagedResult<T>> ShouldBePagedResultAsync<T>(
        this HttpResponseMessage response,
        int? expectedMinCount = null,
        string? because = null)
    {
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content
            .ReadFromJsonAsync<ApiResponse<PagedResult<T>>>(JsonOpts);
        body.Should().NotBeNull();
        body!.Success.Should().BeTrue();
        body.Data.Should().NotBeNull();
        body.Data!.Items.Should().NotBeNull();

        if (expectedMinCount.HasValue)
        {
            body.Data.Items.Should().HaveCountGreaterOrEqualTo(
                expectedMinCount.Value, because);
        }

        return body.Data;
    }

    /// <summary>
    /// Assert business error with expected status code and message contains.
    /// </summary>
    public static async Task ShouldBeBusinessErrorAsync(
        this HttpResponseMessage response,
        HttpStatusCode expectedStatus,
        string? messageContains = null)
    {
        response.StatusCode.Should().Be(expectedStatus);
        var content = await response.Content.ReadAsStringAsync();
        var body = JsonSerializer.Deserialize<ApiResponse<object>>(content, JsonOpts);
        body.Should().NotBeNull();
        body!.Success.Should().BeFalse("business error should indicate failure");

        if (messageContains != null)
        {
            body.Message.Should().Contain(messageContains,
                $"error message should contain '{messageContains}'");
        }
    }

    /// <summary>
    /// Assert HTTP 401 Unauthorized.
    /// </summary>
    public static void ShouldBeUnauthorized(this HttpResponseMessage response)
    {
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    /// <summary>
    /// Assert HTTP 403 Forbidden.
    /// </summary>
    public static void ShouldBeForbidden(this HttpResponseMessage response)
    {
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    /// <summary>
    /// Assert HTTP 404 Not Found with business error.
    /// </summary>
    public static async Task ShouldBeNotFoundAsync(
        this HttpResponseMessage response, string? messageContains = null)
    {
        await response.ShouldBeBusinessErrorAsync(
            HttpStatusCode.NotFound, messageContains);
    }
}
```

**Step 2: Compile check**

Run: `dotnet build tests/LYBT.Tests.Server/ --no-restore`
Expected: Build succeeded

---

### Task 1.10: Full Compile + Smoke Test

**Step 1: Full build**

Run: `dotnet build LYBTZYZS.sln`
Expected: Build succeeded

**Step 2: Run ALL existing tests**

Run: `dotnet test tests/LYBT.Tests.Server/ --verbosity normal`
Expected: All ~1185 tests PASS (backward-compatible changes only)

**Step 3: Commit**

```bash
git add tests/LYBT.Tests.Server/
git commit -m "refactor: infrastructure foundation for PRD-driven test architecture

- Unseal ServerFixture for domain fixture inheritance
- Create 6 DomainFixtures (Auth/User/Clinical/HerbFormula/Sync/Infra)
- Create 6 DomainCollections for parallel execution
- Genericize IntegrationTestBase<TFixture> and JourneyTestBase<TFixture>
- Add xunit.runner.json for parallel collection execution
- Optimize SeedBaseDataAsync (direct Add, no Upsert)
- Create TestDataBuilders (Patient/Herb/Formula/MedicalCase/User/Registration)
- Create BusinessAssertions extension methods
- Backward-compatible: non-generic base classes preserved"
```

---

## Phase 2: Must Have US Tests (51 US)

> **Pattern**: Each module gets a new test file using domain-specific Collection + generic base class.
> Old test files are kept during this phase; cleanup happens in Phase 5.

### Domain-to-Collection Mapping

| Module | Collection | Fixture | Test File |
|--------|-----------|---------|-----------|
| Auth (US-AUTH-001~013) | Auth | AuthFixture | `Features/Auth/US_Auth_MustHaveTests.cs` |
| Users (US-USER-001~005) | Users | UserFixture | `Features/Users/US_User_MustHaveTests.cs` |
| Patients (US-PAT-001~004) | Clinical | ClinicalFixture | `Features/Patients/US_Patient_MustHaveTests.cs` |
| Herbs (US-HERB-001~005) | HerbFormula | HerbFormulaFixture | `Features/Herbs/US_Herb_MustHaveTests.cs` |
| Formulas (US-FORM-001~006) | HerbFormula | HerbFormulaFixture | `Features/Formulas/US_Formula_MustHaveTests.cs` |
| MedicalCases (US-MC-001~009+013) | Clinical | ClinicalFixture | `Features/MedicalCases/US_MedicalCase_MustHaveTests.cs` |
| Registration (US-REG-001~006) | Clinical | ClinicalFixture | `Features/Registration/US_Registration_MustHaveTests.cs` |
| Config (US-CFG-001~002) | Infrastructure | InfraFixture | `Features/Infrastructure/US_Config_MustHaveTests.cs` |
| Sync (US-SYNC-008) | Sync | SyncFixture | `Features/Sync/US_Sync_MustHaveTests.cs` |

### Test Naming Convention

```
US_{MODULE}_{NNN}_{ShortDescription}
```

Examples:
- `US_AUTH_001_LoginWithValidCredentials`
- `US_PAT_002_UpdatePatientInfo`
- `US_MC_005_CompleteConsultationAndPrescription`

### Task 2.1: Auth Module Must Have Tests

**Files:**
- Create: `tests/LYBT.Tests.Server/Features/Auth/US_Auth_MustHaveTests.cs`

**Template (first test fully written, others follow pattern):**

```csharp
using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using LYBT.Shared.Models.Contracts.Auth;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Tests.Server.Infrastructure;
using Xunit;

namespace LYBT.Tests.Server.Features.Auth;

/// <summary>
/// Must Have User Stories for Auth module.
/// PRD: US-AUTH-001 ~ US-AUTH-013 (8 Must Have)
/// Collection: Auth (isolated DB, parallel with other domains)
/// </summary>
[Collection("Auth")]
public sealed class US_Auth_MustHaveTests : IntegrationTestBase<AuthFixture>
{
    public US_Auth_MustHaveTests(AuthFixture fixture) : base(fixture) { }

    #region US-AUTH-001: User login with username and password

    [Fact]
    public async Task US_AUTH_001_LoginWithValidCredentials_ReturnsTokenAndUserInfo()
    {
        // Arrange
        var request = new LoginRequest { UserName = "admin", Password = "TestAdmin2025@" };

        // Act
        var response = await AnonymousClient.PostAsJsonAsync("/api/v1/auth/login", request);

        // Assert - business data validation, not just HTTP 200
        var data = await response.ShouldBeSuccessWithDataAsync<LoginResponse>(
            "US-AUTH-001: valid credentials should return token");
        data.Token.Should().NotBeNullOrWhiteSpace("JWT token must be present");
        data.RefreshToken.Should().NotBeNullOrWhiteSpace("refresh token must be present");
        data.ExpiresAt.Should().BeAfter(DateTime.UtcNow, "token must not be expired");
        data.User.Should().NotBeNull("user info must be returned");
        data.User!.UserName.Should().Be("admin");
    }

    [Fact]
    public async Task US_AUTH_001_LoginWithInvalidPassword_Returns401()
    {
        var request = new LoginRequest { UserName = "admin", Password = "WrongPassword1!" };
        var response = await AnonymousClient.PostAsJsonAsync("/api/v1/auth/login", request);
        response.ShouldBeUnauthorized();
    }

    [Fact]
    public async Task US_AUTH_001_LoginWithNonexistentUser_Returns401()
    {
        var request = new LoginRequest { UserName = "nobody", Password = "Test2025@" };
        var response = await AnonymousClient.PostAsJsonAsync("/api/v1/auth/login", request);
        response.ShouldBeUnauthorized();
    }

    #endregion

    #region US-AUTH-002: Token-based authentication for API access

    [Fact]
    public async Task US_AUTH_002_AuthenticatedRequest_CanAccessProtectedEndpoint()
    {
        // Arrange
        var client = await LoginAsAdminAsync();

        // Act
        var response = await client.GetAsync("/api/v1/users/current");

        // Assert
        var data = await response.ShouldBeSuccessWithDataAsync<object>(
            "US-AUTH-002: authenticated user should access protected endpoint");
    }

    [Fact]
    public async Task US_AUTH_002_UnauthenticatedRequest_Returns401()
    {
        var response = await AnonymousClient.GetAsync("/api/v1/users/current");
        response.ShouldBeUnauthorized();
    }

    #endregion

    #region US-AUTH-003: Token refresh mechanism

    [Fact]
    public async Task US_AUTH_003_RefreshWithValidToken_ReturnsNewTokenPair()
    {
        // Arrange - login to get refresh token
        var loginRequest = new LoginRequest { UserName = "admin", Password = "TestAdmin2025@" };
        var loginResponse = await AnonymousClient.PostAsJsonAsync("/api/v1/auth/login", loginRequest);
        var loginData = await loginResponse.ShouldBeSuccessWithDataAsync<LoginResponse>();
        var refreshToken = loginData.RefreshToken;

        // Act
        var refreshRequest = new RefreshTokenRequest { RefreshToken = refreshToken! };
        var response = await AnonymousClient.PostAsJsonAsync("/api/v1/auth/refresh", refreshRequest);

        // Assert
        var data = await response.ShouldBeSuccessWithDataAsync<LoginResponse>(
            "US-AUTH-003: valid refresh token should return new token pair");
        data.Token.Should().NotBeNullOrWhiteSpace();
        data.RefreshToken.Should().NotBeNullOrWhiteSpace();
        data.Token.Should().NotBe(loginData.Token, "new token should differ from old");
    }

    #endregion

    #region US-AUTH-005: Logout functionality

    [Fact]
    public async Task US_AUTH_005_Logout_InvalidatesRefreshToken()
    {
        // Arrange
        var loginRequest = new LoginRequest { UserName = "doctor", Password = "TestDoctor2025@" };
        var loginResponse = await AnonymousClient.PostAsJsonAsync("/api/v1/auth/login", loginRequest);
        var loginData = await loginResponse.ShouldBeSuccessWithDataAsync<LoginResponse>();

        // Act - logout
        var logoutRequest = new { RefreshToken = loginData.RefreshToken };
        var logoutResponse = await AnonymousClient.PostAsJsonAsync("/api/v1/auth/logout", logoutRequest);
        logoutResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Assert - refresh should fail after logout
        var refreshRequest = new RefreshTokenRequest { RefreshToken = loginData.RefreshToken! };
        var refreshResponse = await AnonymousClient.PostAsJsonAsync("/api/v1/auth/refresh", refreshRequest);
        refreshResponse.StatusCode.Should().NotBe(HttpStatusCode.OK,
            "US-AUTH-005: refresh token should be invalid after logout");
    }

    #endregion

    // US-AUTH-007: Token validation endpoint
    // US-AUTH-008: Role-based access control (BR-002)
    // US-AUTH-009: Password policy enforcement
    // US-AUTH-010: Auto-login token mechanism
    // ... follow same pattern: Arrange -> Act -> Assert with BusinessAssertions
}
```

**Run after completion:**

```bash
dotnet test tests/LYBT.Tests.Server/ --filter "FullyQualifiedName~US_Auth_MustHaveTests" -v normal
```

**Commit:**
```bash
git add tests/LYBT.Tests.Server/Features/Auth/US_Auth_MustHaveTests.cs
git commit -m "test: add PRD-driven Must Have tests for Auth module (US-AUTH-001~013)"
```

---

### Task 2.2 ~ 2.10: Remaining Modules

Each follows the identical pattern as Task 2.1. Key differences per module:

**Task 2.2: Users Module** (`[Collection("Users")]`, `UserFixture`)
- US-USER-001: Create user (AdminOnly)
- US-USER-002: List users with pagination
- US-USER-003: Update user info
- US-USER-004: Delete user (soft)
- US-USER-005: Reset password
- Use `UserBuilder.Default().WithRole("Doctor").Build()` for payloads
- Assert: check user role, status, created fields

**Task 2.3: Patients Module** (`[Collection("Clinical")]`, `ClinicalFixture`)
- US-PAT-001: Create patient
- US-PAT-002: Update patient
- US-PAT-003: Search patients (name/phone/pinyin)
- US-PAT-004: Delete patient (with reference check)
- Use `PatientBuilder.Default().WithName("张三").Build()`
- Assert: check patient data matches input, reference check prevents deletion

**Task 2.4: Herbs Module** (`[Collection("HerbFormula")]`, `HerbFormulaFixture`)
- US-HERB-001: Create herb
- US-HERB-002: Update herb
- US-HERB-003: Search herbs
- US-HERB-004: Delete herb (with reference check)
- US-HERB-005: Import herbs (Excel/JSON)
- Use `HerbBuilder.Default().WithName("黄芪").Build()`

**Task 2.5: Formulas Module** (`[Collection("HerbFormula")]`, `HerbFormulaFixture`)
- US-FORM-001: Create formula with herb items
- US-FORM-002: Update formula
- US-FORM-003: List formulas (ownership filtered)
- US-FORM-004: Delete formula
- US-FORM-005: Share formula
- US-FORM-006: Validate formula herbs
- Use `FormulaBuilder.Default().AddHerb(herbId, "黄芪", 15).Build()`

**Task 2.6: MedicalCases Module** (`[Collection("Clinical")]`, `ClinicalFixture`)
- US-MC-001: Create medical case
- US-MC-002: Add consultation (diagnosis)
- US-MC-003: Add prescription with items
- US-MC-004: Complete case (status transition)
- US-MC-005: Cancel case with reason (BR-006)
- US-MC-006: Case status machine (BR-004)
- US-MC-007: Print completion flag (BR-003)
- US-MC-009: Single active case per patient (BR-001)
- US-MC-013: Audit log for changes
- Use `MedicalCaseBuilder` for complex payloads
- **Critical BR tests**: BR-001 (single active), BR-003 (print block), BR-004 (state machine), BR-006 (cancel reason)

**Task 2.7: Registration Module** (`[Collection("Clinical")]`, `ClinicalFixture`)
- US-REG-001: Create registration
- US-REG-002: View queue
- US-REG-003: Start visit
- US-REG-004: Cancel registration
- US-REG-005: List registrations
- US-REG-006: Filter by doctor

**Task 2.8: Config Module** (`[Collection("Infrastructure")]`, `InfraFixture`)
- US-CFG-001: Health check endpoint
- US-CFG-002: Diagnostics endpoint (SuperAdmin only)

**Task 2.9: Skip** (Desktop Shell tests -- not testable via Server API)

**Task 2.10: Sync Module** (`[Collection("Sync")]`, `SyncFixture`)
- US-SYNC-008: Get entity types and metadata

**Commit after each module:**
```bash
git commit -m "test: add PRD-driven Must Have tests for {Module} module (US-{PREFIX}-xxx)"
```

---

## Phase 3: Should Have US Tests (54 US)

Same pattern as Phase 2, but for Should-Have priority US.

### Task 3.1 ~ 3.11: Should Have Tests

Create companion files:
- `Features/Auth/US_Auth_ShouldHaveTests.cs`
- `Features/Users/US_User_ShouldHaveTests.cs`
- `Features/Patients/US_Patient_ShouldHaveTests.cs`
- etc.

Key Should-Have tests with HIGH business value:

| US | Description | Why Important |
|----|-------------|---------------|
| US-AUTH-004 | Auto-login | Desktop integration |
| US-AUTH-008 | Login lockout (BR-008) | Security: 5-fail lockout |
| US-MC-008 | Concurrent prescription modification | Data integrity |
| US-MC-010 | Case search (diagnosis/treatment) | Core UX |
| US-SYNC-001~007 | Full sync workflow | Dual-mode critical path |
| US-ERR-001~006 | Error handling validation | False positive elimination |

---

## Phase 4: UserJourneys Rewrite (4 Narratives)

### Task 4.1: Narrative 1 - First Visit Journey

**Files:**
- Create: `tests/LYBT.Tests.Server/UserJourneys/Narrative1_FirstVisitJourneyTests.cs`

**Template:**

```csharp
using LYBT.Tests.Server.Infrastructure;
using LYBT.Tests.Server.Infrastructure.TestDataBuilders;
using Xunit;

namespace LYBT.Tests.Server.UserJourneys;

/// <summary>
/// Narrative 1: 首诊完整流程
/// Maps to User Story Map Narrative 1.
///
/// Flow: 挂号 -> 接诊 -> 诊断(四诊) -> 开方 -> 打印 -> 完成
/// Covers: US-REG-001, US-MC-001~005, US-MC-007, BR-001, BR-003, BR-005
/// </summary>
[Collection("Clinical")]
public sealed class Narrative1_FirstVisitJourneyTests : JourneyTestBase<ClinicalFixture>
{
    public Narrative1_FirstVisitJourneyTests(ClinicalFixture fixture) : base(fixture) { }

    [Fact]
    public async Task FirstVisit_HappyPath_PatientRegistrationThroughPrescriptionPrint()
    {
        await ResetForJourneyAsync();
        var adminClient = await LoginAsAdminAsync();
        var doctorClient = await LoginAsDoctorAsync();

        // Step 1: Create patient
        var patientPayload = PatientBuilder.Default()
            .WithName("首诊测试患者").WithPhone("13800000001").Build();
        var (patientResp, patient) = await PostAsync<dynamic>(
            doctorClient, "/api/v1/patients", patientPayload);
        patientResp.StatusCode.Should().Be(System.Net.HttpStatusCode.Created);
        var patientId = (Guid)patient!.Id;

        // Step 2: Create registration
        var regPayload = RegistrationBuilder.Default()
            .ForPatient(patientId).WithChiefComplaint("头痛三天").Build();
        var (regResp, reg) = await PostAsync<dynamic>(
            doctorClient, "/api/v1/registrations", regPayload);

        // Step 3: Start visit
        // Step 4: Create medical case
        // Step 5: Add consultation
        // Step 6: Add prescription with herbs
        // Step 7: Print completion
        // Step 8: Verify case status = Completed
        // ... each step uses previous step's output data
    }

    [Fact]
    public async Task FirstVisit_BR001_CannotCreateSecondActiveCaseForSamePatient()
    {
        // Verifies BR-001: Single active case per patient
    }

    [Fact]
    public async Task FirstVisit_BR003_CannotModifyPrintedCase()
    {
        // Verifies BR-003: Printed case becomes read-only
    }
}
```

### Task 4.2: Narrative 2 - Return Visit Journey

**File:** `UserJourneys/Narrative2_ReturnVisitJourneyTests.cs`
- Flow: 复诊挂号 -> 查历史医案 -> 新诊断 -> 修改处方 -> 完成
- Covers: US-MC-010 (search), US-MC-011 (history), previous case reference

### Task 4.3: Narrative 3 - Herb/Formula Management Journey

**File:** `UserJourneys/Narrative3_HerbFormulaManagementJourneyTests.cs`
- Flow: 创建药材 -> 创建验方 -> 验证药材绑定 -> 处方使用验方 -> 价格计算
- Covers: US-HERB-001~005, US-FORM-001~006, US-FORM-009

### Task 4.4: Narrative 4 - System Management Journey

**File:** `UserJourneys/Narrative4_SystemManagementJourneyTests.cs`
- Flow: 创建用户 -> 分配角色 -> 权限验证 -> 禁用/启用 -> 密码重置
- Covers: US-USER-001~005, US-AUTH-008, US-CFG-001~002

---

## Phase 5: Cleanup & Documentation

### Task 5.1: Create Coverage Matrix

**Files:**
- Create: `tests/LYBT.Tests.Server/_CoverageMatrix.md`

Format:
```markdown
| US ID | Description | Test Class | Test Method | Status |
|-------|-------------|-----------|-------------|--------|
| US-AUTH-001 | Login | US_Auth_MustHaveTests | US_AUTH_001_Login* | COVERED |
```

### Task 5.2: Delete Old Test Files

**Files to delete** (replaced by US_* tests):
- `Features/Auth/AuthIntegrationTests.cs` (replaced by US_Auth_MustHaveTests)
- `Features/Users/UserIntegrationTests.cs` (replaced by US_User_MustHaveTests)
- `Features/Patients/PatientIntegrationTests.cs`
- `Features/Herbs/HerbIntegrationTests.cs`
- `Features/Formulas/FormulaIntegrationTests.cs`
- `Features/MedicalCases/MedicalCaseIntegrationTests.cs`
- `Features/Registration/RegistrationIntegrationTests.cs`
- `Features/Sync/SyncIntegrationTests.cs`
- Old UserJourney files (replaced by Narrative_* tests)

**KEEP:**
- `Features/Auth/AuthSmokeTests.cs` (quick validation)
- `Features/Auth/AuthTokenAdvancedIntegrationTests.cs` (deep token tests)
- `Features/MedicalCases/PrescriptionAggregateTests.cs` (aggregate-specific)
- `Features/MedicalCases/MedicalCasePermissionAndFilterTests.cs`
- `Features/Infrastructure/*` (health, diagnostics, correlation, API contracts)
- All `PureLogic/*` tests (untouched)
- `RateLimiting/*` tests

### Task 5.3: Full Test Run

```bash
dotnet test LYBTZYZS.sln --filter "FullyQualifiedName~LYBT.Tests" --verbosity normal
```

Expected: All tests pass, parallel collections reduce execution time from ~280s to ~80s.

### Task 5.4: Update Documentation

- Update `MEMORY.md` with new test architecture
- Write Serena memory: `server-test-prd-driven-architecture`
- Update `docs/05-development/` testing guide

**Final commit:**
```bash
git commit -m "refactor: complete PRD-driven test architecture migration

- 6 parallel domain Collections (Auth/Users/Clinical/HerbFormula/Sync/Infra)
- TestDataBuilders for all domain entities
- BusinessAssertions eliminating false positives
- Coverage matrix: 105 US mapped to tests
- Old test files removed, PureLogic untouched
- ~3x execution speedup via parallel collections"
```

---

## Execution Dependencies

```
Phase 1 (Foundation)
  ├── Task 1.1-1.9 can be batched (no dependencies between tasks)
  └── Task 1.10: depends on all 1.1-1.9

Phase 2 (Must Have)
  ├── Task 2.1 (Auth): independent
  ├── Task 2.2 (Users): independent
  ├── Task 2.3 (Patients): independent
  ├── Task 2.4 (Herbs): independent
  ├── Task 2.5 (Formulas): depends on 2.4 (needs herb IDs for formula items)
  ├── Task 2.6 (MedicalCases): depends on 2.3+2.4 (needs patient+herb for full flow)
  ├── Task 2.7 (Registration): depends on 2.3 (needs patient)
  ├── Task 2.8 (Config): independent
  └── Task 2.10 (Sync): depends on 2.3+2.4 (needs entities to sync)

Phase 3 (Should Have): depends on Phase 2 completion
Phase 4 (UserJourneys): depends on Phase 2 completion
Phase 5 (Cleanup): depends on Phase 3+4 completion
```

## Risk Mitigations

| Risk | Mitigation |
|------|------------|
| Parallel DB contention on local SQL Server | Each fixture gets unique DB; LocalDB handles 6 concurrent connections |
| Backward compatibility during migration | Non-generic base classes preserved; old + new tests coexist |
| SeedBaseDataAsync optimization breaks tests | Smoke test after each change; revert to Upsert if needed |
| DTO changes between modules | TestDataBuilders use anonymous types; resilient to DTO evolution |
| Test execution time regression | xunit.runner.json enables parallel; benchmark before/after |
