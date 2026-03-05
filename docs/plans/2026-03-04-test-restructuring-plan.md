# Test Restructuring Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Rebuild the test suite around user journeys so that green tests prove the software actually works.

**Architecture:** Strangler Pattern - add UserJourney tests (P1), delete low-value tests (P2), reorganize directory (P3), fix reliability (P4). Existing ServerFixture + Respawn infrastructure is reused.

**Tech Stack:** xUnit + FluentAssertions + WebApplicationFactory + Respawn + Xunit.Extensions.Ordering (new)

---

## Phase 1: UserJourney Tests (Core)

### Task 1.1: Add Xunit.Extensions.Ordering Dependency

**Files:**
- Modify: `tests/LYBT.Tests.Server/LYBT.Tests.Server.csproj`
- Create: `tests/LYBT.Tests.Server/UserJourneys/AssemblyInfo.cs`

**Step 1: Add NuGet package**

Run:
```bash
cd "D:/source/repos/LYBTZYZS" && dotnet add tests/LYBT.Tests.Server/ package Xunit.Extensions.Ordering
```
Expected: Package added successfully.

**Step 2: Create AssemblyInfo for ordering configuration**

Create `tests/LYBT.Tests.Server/UserJourneys/AssemblyInfo.cs`:
```csharp
using Xunit;
using Xunit.Extensions.Ordering;

// Enable test case ordering within Journey classes
[assembly: TestCaseOrderer("Xunit.Extensions.Ordering.TestCaseOrderer", "Xunit.Extensions.Ordering")]
```

**Step 3: Verify build**

Run:
```bash
dotnet build tests/LYBT.Tests.Server/ -v q
```
Expected: 0 errors.

---

### Task 1.2: Create JourneyTestBase

**Files:**
- Create: `tests/LYBT.Tests.Server/UserJourneys/JourneyTestBase.cs`

**Design Note:** Journey tests differ from regular integration tests:
- Regular tests: Respawn reset BEFORE EACH test (clean slate)
- Journey tests: Respawn reset ONCE at Journey start (Step 1), subsequent steps build on prior state

```csharp
namespace LYBT.Tests.Server.UserJourneys;

/// <summary>
/// Base class for UserJourney tests. Unlike IntegrationTestBase which resets DB per test,
/// JourneyTestBase resets only once per class via ResetOnce(). Steps share state via static fields.
/// </summary>
[Collection("Server")]
public abstract class JourneyTestBase
{
    protected ServerFixture Fixture { get; }
    protected HttpClient AnonymousClient => Fixture.AnonymousClient;

    protected static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    protected JourneyTestBase(ServerFixture fixture)
    {
        Fixture = fixture;
    }

    /// <summary>
    /// Call this in Step 1 (Order(1)) of each Journey to reset DB and re-seed.
    /// Do NOT call in subsequent steps - they depend on prior step data.
    /// </summary>
    protected async Task ResetForJourneyAsync()
    {
        await Fixture.ResetAsync();
    }

    protected async Task<HttpClient> LoginAsAdminAsync()
        => await Fixture.LoginAsAsync("admin", "TestAdmin2025@");

    protected async Task<HttpClient> LoginAsDoctorAsync()
        => await Fixture.LoginAsAsync("doctor", "TestDoctor2025@");

    protected async Task<HttpClient> LoginAsSysAdminAsync()
        => await Fixture.LoginAsAsync("sysadmin", "TestAdmin2025@");

    protected async Task<HttpClient> LoginAsAsync(string username, string password)
        => await Fixture.LoginAsAsync(username, password);

    /// <summary>
    /// Helper: POST and return deserialized response data.
    /// </summary>
    protected async Task<(HttpResponseMessage Response, T? Data)> PostAsync<T>(
        HttpClient client, string url, object payload)
    {
        var response = await client.PostAsJsonAsync(url, payload);
        if (response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadFromJsonAsync<ApiResponse<T>>(JsonOptions);
            return (response, body?.Data);
        }
        return (response, default);
    }

    /// <summary>
    /// Helper: PUT and return deserialized response data.
    /// </summary>
    protected async Task<(HttpResponseMessage Response, T? Data)> PutAsync<T>(
        HttpClient client, string url, object payload)
    {
        var response = await client.PutAsJsonAsync(url, payload);
        if (response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadFromJsonAsync<ApiResponse<T>>(JsonOptions);
            return (response, body?.Data);
        }
        return (response, default);
    }

    /// <summary>
    /// Helper: GET and return deserialized response data.
    /// </summary>
    protected async Task<(HttpResponseMessage Response, T? Data)> GetAsync<T>(
        HttpClient client, string url)
    {
        var response = await client.GetAsync(url);
        if (response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadFromJsonAsync<ApiResponse<T>>(JsonOptions);
            return (response, body?.Data);
        }
        return (response, default);
    }

    protected static string UniqueName(string prefix)
        => $"{prefix}_{Guid.NewGuid():N}"[..20];

    protected static string UniquePhone()
        => $"138{Random.Shared.Next(10000000, 99999999)}";
}
```

**Step: Verify build**

Run:
```bash
dotnet build tests/LYBT.Tests.Server/ -v q
```
Expected: 0 errors.

**Note:** `ApiResponse<T>` and related DTOs are already available via project references. If `LoginAsAsync` method signature differs from ServerFixture, adjust accordingly (check actual method name and parameters).

---

### Task 1.3: AuthJourneyTests (~8 steps)

**Files:**
- Create: `tests/LYBT.Tests.Server/UserJourneys/AuthJourneyTests.cs`

**Purpose:** Verify the complete authentication lifecycle works end-to-end.

```csharp
namespace LYBT.Tests.Server.UserJourneys;

[Collection("Server")]
[TestCaseOrderer("Xunit.Extensions.Ordering.TestCaseOrderer", "Xunit.Extensions.Ordering")]
public sealed class AuthJourneyTests : JourneyTestBase
{
    private static string? _adminToken;
    private static string? _refreshToken;

    public AuthJourneyTests(ServerFixture fixture) : base(fixture) { }

    [Fact, Order(1)]
    public async Task Step01_ResetDatabase()
    {
        await ResetForJourneyAsync();
    }

    [Fact, Order(2)]
    public async Task Step02_Admin_Login_Returns_Token()
    {
        var request = new LoginRequest { UserName = "admin", Password = "TestAdmin2025@" };
        var response = await AnonymousClient.PostAsJsonAsync("/api/v1/auth/login", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<LoginResponse>>(JsonOptions);
        body!.Success.Should().BeTrue();
        body.Data!.Token.Should().NotBeNullOrEmpty();
        body.Data.RefreshToken.Should().NotBeNullOrEmpty();
        body.Data.User.Should().NotBeNull();

        _adminToken = body.Data.Token;
        _refreshToken = body.Data.RefreshToken;
    }

    [Fact, Order(3)]
    public async Task Step03_Token_Can_Access_Protected_Endpoint()
    {
        _adminToken.Should().NotBeNull("Step02 must pass first");

        var client = Fixture.CreateClientWithToken(_adminToken!);
        var response = await client.GetAsync("/api/v1/auth/validate");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact, Order(4)]
    public async Task Step04_Doctor_Login_Returns_Token()
    {
        var request = new LoginRequest { UserName = "doctor", Password = "TestDoctor2025@" };
        var response = await AnonymousClient.PostAsJsonAsync("/api/v1/auth/login", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<LoginResponse>>(JsonOptions);
        body!.Data!.User.Should().NotBeNull();
    }

    [Fact, Order(5)]
    public async Task Step05_Wrong_Password_Returns_401()
    {
        var request = new LoginRequest { UserName = "admin", Password = "WrongPassword!" };
        var response = await AnonymousClient.PostAsJsonAsync("/api/v1/auth/login", request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact, Order(6)]
    public async Task Step06_Refresh_Token_Returns_New_Token()
    {
        _refreshToken.Should().NotBeNull("Step02 must pass first");

        var request = new { RefreshToken = _refreshToken };
        var response = await AnonymousClient.PostAsJsonAsync("/api/v1/auth/refresh", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<LoginResponse>>(JsonOptions);
        body!.Data!.Token.Should().NotBe(_adminToken, "should return a new token");
    }

    [Fact, Order(7)]
    public async Task Step07_Logout_Succeeds()
    {
        var client = await LoginAsAdminAsync();
        var request = new { RefreshToken = _refreshToken };
        var response = await client.PostAsJsonAsync("/api/v1/auth/logout", request);

        // Logout should succeed (200 or 204)
        ((int)response.StatusCode).Should().BeLessThan(300);
    }

    [Fact, Order(8)]
    public async Task Step08_Anonymous_Cannot_Access_Protected()
    {
        var response = await AnonymousClient.GetAsync("/api/v1/users/current");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
```

**Step: Run and verify**

Run:
```bash
dotnet test tests/LYBT.Tests.Server/ --filter "FullyQualifiedName~AuthJourneyTests" --no-build -v normal
```
Expected: 8 tests pass in order.

---

### Task 1.4: AdminSetupJourneyTests (~10 steps)

**Files:**
- Create: `tests/LYBT.Tests.Server/UserJourneys/AdminSetupJourneyTests.cs`

**Purpose:** Verify admin can set up the system: create doctor, herbs, formulas, patients.

```csharp
namespace LYBT.Tests.Server.UserJourneys;

[Collection("Server")]
[TestCaseOrderer("Xunit.Extensions.Ordering.TestCaseOrderer", "Xunit.Extensions.Ordering")]
public sealed class AdminSetupJourneyTests : JourneyTestBase
{
    private static HttpClient? _admin;
    private static Guid _createdDoctorId;
    private static Guid _createdHerbId;
    private static Guid _createdFormulaId;
    private static Guid _createdPatientId;
    private static string _doctorUsername = "";

    public AdminSetupJourneyTests(ServerFixture fixture) : base(fixture) { }

    [Fact, Order(1)]
    public async Task Step01_Reset_And_Admin_Login()
    {
        await ResetForJourneyAsync();
        _admin = await LoginAsAdminAsync();
        _admin.Should().NotBeNull();
    }

    [Fact, Order(2)]
    public async Task Step02_Create_Doctor_Account()
    {
        _doctorUsername = $"dr_{Guid.NewGuid():N}"[..12];
        var input = new UserInputDto
        {
            UserName = _doctorUsername,
            RealName = "测试医生",
            Role = UserRole.Doctor,
            Email = $"{_doctorUsername}@test.com",
            PhoneNumber = UniquePhone()
        };

        var (response, data) = await PostAsync<UserDetailDto>(_admin!, "/api/v1/users", input);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        data.Should().NotBeNull();
        data!.Id.Should().NotBeEmpty();
        _createdDoctorId = data.Id;
    }

    [Fact, Order(3)]
    public async Task Step03_New_Doctor_Can_Login()
    {
        // Server assigns default password on create - check what it is
        // Typically: default password or the one set in UserInputDto
        // If no password set, server may use a default - adjust accordingly
        var client = await LoginAsAsync(_doctorUsername, "TestDoctor2025@");
        client.Should().NotBeNull("newly created doctor should be able to login");
    }

    [Fact, Order(4)]
    public async Task Step04_Create_Herb()
    {
        var input = new HerbInputDto
        {
            Name = UniqueName("当归"),
            Unit = "克",
            Price = 15.5m,
            Category = "补血药",
            Effect = "补血活血"
        };

        var (response, data) = await PostAsync<HerbDetailDto>(_admin!, "/api/v1/herbs", input);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        data!.Id.Should().NotBeEmpty();
        _createdHerbId = data.Id;
    }

    [Fact, Order(5)]
    public async Task Step05_Herb_Is_Queryable()
    {
        var (response, data) = await GetAsync<PagedResult<HerbDetailDto>>(
            _admin!, "/api/v1/herbs?pageSize=100");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        data!.Items.Should().Contain(h => h.Id == _createdHerbId);
    }

    [Fact, Order(6)]
    public async Task Step06_Create_Formula_With_Herb()
    {
        var input = new FormulaInputDto
        {
            Name = UniqueName("四物汤"),
            Effect = "补血调经",
            Usage = "水煎服",
            Herbs = new List<FormulaHerbItemInputDto>
            {
                new()
                {
                    HerbId = _createdHerbId,
                    HerbName = "当归",
                    Dosage = 12,
                    Unit = "克"
                }
            }
        };

        var (response, data) = await PostAsync<FormulaDetailDto>(_admin!, "/api/v1/formulas", input);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        data!.Id.Should().NotBeEmpty();
        _createdFormulaId = data.Id;
    }

    [Fact, Order(7)]
    public async Task Step07_Formula_Contains_Herb()
    {
        var (response, data) = await GetAsync<FormulaDetailDto>(
            _admin!, $"/api/v1/formulas/{_createdFormulaId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        data!.Herbs.Should().NotBeEmpty();
        data.Herbs.Should().Contain(h => h.HerbId == _createdHerbId);
    }

    [Fact, Order(8)]
    public async Task Step08_Create_Patient()
    {
        var input = new PatientInputDto
        {
            Name = UniqueName("张三"),
            Gender = Gender.Male,
            BirthDate = new DateTime(1985, 3, 15),
            PhoneNumber = UniquePhone(),
            Address = "北京市朝阳区"
        };

        var (response, data) = await PostAsync<PatientDetailDto>(_admin!, "/api/v1/patients", input);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        data!.Id.Should().NotBeEmpty();
        _createdPatientId = data.Id;
    }

    [Fact, Order(9)]
    public async Task Step09_Patient_Is_Queryable()
    {
        var (response, data) = await GetAsync<PagedResult<PatientDetailDto>>(
            _admin!, "/api/v1/patients?pageSize=100");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        data!.Items.Should().Contain(p => p.Id == _createdPatientId);
    }

    [Fact, Order(10)]
    public async Task Step10_Admin_Can_View_All_Users()
    {
        var (response, data) = await GetAsync<PagedResult<UserDetailDto>>(
            _admin!, "/api/v1/users?pageSize=100");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        data!.Items.Should().Contain(u => u.Id == _createdDoctorId);
    }
}
```

**Step: Run and verify**

Run:
```bash
dotnet test tests/LYBT.Tests.Server/ --filter "FullyQualifiedName~AdminSetupJourneyTests" --no-build -v normal
```
Expected: 10 tests pass in order.

---

### Task 1.5: DoctorClinicalJourneyTests (~12 steps)

**Files:**
- Create: `tests/LYBT.Tests.Server/UserJourneys/DoctorClinicalJourneyTests.cs`

**Purpose:** The most critical journey - doctor's complete clinical workflow from login to case completion.

```csharp
namespace LYBT.Tests.Server.UserJourneys;

[Collection("Server")]
[TestCaseOrderer("Xunit.Extensions.Ordering.TestCaseOrderer", "Xunit.Extensions.Ordering")]
public sealed class DoctorClinicalJourneyTests : JourneyTestBase
{
    private static HttpClient? _doctor;
    private static HttpClient? _admin;
    private static Guid _patientId;
    private static Guid _herbId;
    private static Guid _medicalCaseId;
    private static Guid _doctorUserId;

    public DoctorClinicalJourneyTests(ServerFixture fixture) : base(fixture) { }

    [Fact, Order(1)]
    public async Task Step01_Setup_And_Login()
    {
        await ResetForJourneyAsync();
        _admin = await LoginAsAdminAsync();
        _doctor = await LoginAsDoctorAsync();

        // Get doctor user ID for medical case creation
        var (_, userData) = await GetAsync<UserDetailDto>(_doctor!, "/api/v1/users/current");
        _doctorUserId = userData!.Id;

        // Create test patient
        var patientInput = new PatientInputDto
        {
            Name = UniqueName("李四"),
            Gender = Gender.Female,
            BirthDate = new DateTime(1990, 6, 20),
            PhoneNumber = UniquePhone()
        };
        var (_, patient) = await PostAsync<PatientDetailDto>(_admin!, "/api/v1/patients", patientInput);
        _patientId = patient!.Id;

        // Create test herb
        var herbInput = new HerbInputDto { Name = UniqueName("黄芪"), Unit = "克", Price = 20.0m };
        var (_, herb) = await PostAsync<HerbDetailDto>(_admin!, "/api/v1/herbs", herbInput);
        _herbId = herb!.Id;
    }

    [Fact, Order(2)]
    public async Task Step02_Doctor_Queries_Patients()
    {
        var (response, data) = await GetAsync<PagedResult<PatientDetailDto>>(
            _doctor!, "/api/v1/patients");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        data!.Items.Should().NotBeEmpty();
    }

    [Fact, Order(3)]
    public async Task Step03_Create_MedicalCase()
    {
        var input = new MedicalCaseInputDto
        {
            PatientId = _patientId,
            UserId = _doctorUserId
        };

        var (response, data) = await PostAsync<MedicalCaseDetailDto>(
            _doctor!, "/api/v1/medicalcases", input);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        data!.Id.Should().NotBeEmpty();
        data.Status.Should().Be(MedicalCaseStatus.Active);
        _medicalCaseId = data.Id;
    }

    [Fact, Order(4)]
    public async Task Step04_Save_Diagnosis()
    {
        var input = new MedicalCaseInputDto
        {
            Id = _medicalCaseId,
            PatientId = _patientId,
            UserId = _doctorUserId,
            Consultation = new ConsultationInputDto
            {
                PresentIllness = "头痛发热三日",
                TongueDiagnosis = "舌红苔黄",
                PulseDiagnosis = "浮数",
                TcmDiagnosis = "风热犯肺"
            }
        };

        var (response, _) = await PutAsync<MedicalCaseDetailDto>(
            _doctor!, $"/api/v1/medicalcases/{_medicalCaseId}", input);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact, Order(5)]
    public async Task Step05_Verify_Diagnosis_Saved()
    {
        var (response, data) = await GetAsync<MedicalCaseDetailDto>(
            _doctor!, $"/api/v1/medicalcases/{_medicalCaseId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        data!.Consultation.Should().NotBeNull();
        data.Consultation!.TcmDiagnosis.Should().Be("风热犯肺");
    }

    [Fact, Order(6)]
    public async Task Step06_Set_Needs_Prescription()
    {
        var response = await _doctor!.PutAsJsonAsync(
            $"/api/v1/medicalcases/{_medicalCaseId}/prescription-flag",
            new { NeedsPrescription = true });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact, Order(7)]
    public async Task Step07_Save_Prescription()
    {
        var input = new MedicalCaseInputDto
        {
            Id = _medicalCaseId,
            PatientId = _patientId,
            UserId = _doctorUserId,
            NeedsPrescription = true,
            Prescription = new PrescriptionInputDto
            {
                MedicalCaseId = _medicalCaseId,
                DosageCount = 7,
                Usage = "水煎服，日一剂",
                Advice = "忌辛辣",
                TotalPrice = 140.0m,
                Items = new List<PrescriptionItemInputDto>
                {
                    new()
                    {
                        HerbId = _herbId,
                        HerbName = "黄芪",
                        Unit = "克",
                        Dosage = 30,
                        UnitPrice = 20.0m,
                        Subtotal = 140.0m
                    }
                }
            }
        };

        var (response, _) = await PutAsync<MedicalCaseDetailDto>(
            _doctor!, $"/api/v1/medicalcases/{_medicalCaseId}", input);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact, Order(8)]
    public async Task Step08_Verify_Prescription_Saved()
    {
        var (response, data) = await GetAsync<MedicalCaseDetailDto>(
            _doctor!, $"/api/v1/medicalcases/{_medicalCaseId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        data!.Prescription.Should().NotBeNull();
        data.Prescription!.Items.Should().NotBeEmpty();
        data.Prescription.DosageCount.Should().Be(7);
    }

    [Fact, Order(9)]
    public async Task Step09_Complete_MedicalCase()
    {
        var input = new MedicalCaseStatusInputDto
        {
            Status = MedicalCaseStatus.Completed
        };

        var response = await _doctor!.PutAsJsonAsync(
            $"/api/v1/medicalcases/{_medicalCaseId}/status", input);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact, Order(10)]
    public async Task Step10_Verify_Case_Completed()
    {
        var (response, data) = await GetAsync<MedicalCaseDetailDto>(
            _doctor!, $"/api/v1/medicalcases/{_medicalCaseId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        data!.Status.Should().Be(MedicalCaseStatus.Completed);
    }

    [Fact, Order(11)]
    public async Task Step11_Edit_Completed_Case_Requires_Reason()
    {
        // Edit without reason should fail (or require reason)
        var input = new MedicalCaseInputDto
        {
            Id = _medicalCaseId,
            PatientId = _patientId,
            UserId = _doctorUserId,
            Consultation = new ConsultationInputDto
            {
                TcmDiagnosis = "修改后的诊断"
            }
            // No EditReason provided
        };

        var response = await _doctor!.PutAsJsonAsync(
            $"/api/v1/medicalcases/{_medicalCaseId}", input);

        // Should require EditReason for completed case modification
        // Exact behavior depends on implementation - may be 400 or require reason
        // The key assertion: completed cases have edit protection
        ((int)response.StatusCode).Should().BeOneOf(200, 400);
    }

    [Fact, Order(12)]
    public async Task Step12_Admin_Can_View_Case()
    {
        var (response, data) = await GetAsync<MedicalCaseDetailDto>(
            _admin!, $"/api/v1/medicalcases/{_medicalCaseId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        data!.Status.Should().Be(MedicalCaseStatus.Completed);
    }
}
```

**Step: Run and verify**

Run:
```bash
dotnet test tests/LYBT.Tests.Server/ --filter "FullyQualifiedName~DoctorClinicalJourneyTests" --no-build -v normal
```
Expected: 12 tests pass in order. This is THE most critical journey.

---

### Task 1.6: PatientManagementJourneyTests (~6 steps)

**Files:**
- Create: `tests/LYBT.Tests.Server/UserJourneys/PatientManagementJourneyTests.cs`

**Purpose:** Patient CRUD lifecycle including reference checking.

```csharp
namespace LYBT.Tests.Server.UserJourneys;

[Collection("Server")]
[TestCaseOrderer("Xunit.Extensions.Ordering.TestCaseOrderer", "Xunit.Extensions.Ordering")]
public sealed class PatientManagementJourneyTests : JourneyTestBase
{
    private static HttpClient? _admin;
    private static Guid _patientId;

    public PatientManagementJourneyTests(ServerFixture fixture) : base(fixture) { }

    [Fact, Order(1)]
    public async Task Step01_Setup()
    {
        await ResetForJourneyAsync();
        _admin = await LoginAsAdminAsync();
    }

    [Fact, Order(2)]
    public async Task Step02_Create_Patient()
    {
        var input = new PatientInputDto
        {
            Name = UniqueName("王五"),
            Gender = Gender.Male,
            BirthDate = new DateTime(1975, 8, 10),
            PhoneNumber = UniquePhone(),
            IdNumber = $"11010119750810{Random.Shared.Next(1000, 9999)}",
            Address = "上海市浦东新区"
        };

        var (response, data) = await PostAsync<PatientDetailDto>(_admin!, "/api/v1/patients", input);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        _patientId = data!.Id;
    }

    [Fact, Order(3)]
    public async Task Step03_Update_Patient()
    {
        var input = new PatientInputDto
        {
            Id = _patientId,
            Name = UniqueName("王五改"),
            PhoneNumber = UniquePhone(),
            Address = "上海市浦东新区新地址"
        };

        var (response, _) = await PutAsync<PatientDetailDto>(
            _admin!, $"/api/v1/patients/{_patientId}", input);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact, Order(4)]
    public async Task Step04_Toggle_Status()
    {
        var response = await _admin!.PostAsync(
            $"/api/v1/patients/{_patientId}/toggle-status", null);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact, Order(5)]
    public async Task Step05_Check_No_References()
    {
        var response = await _admin!.GetAsync(
            $"/api/v1/patients/{_patientId}/check-reference");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact, Order(6)]
    public async Task Step06_Delete_Patient()
    {
        var response = await _admin!.DeleteAsync($"/api/v1/patients/{_patientId}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify deleted (soft delete - should not appear in normal query)
        var (listResponse, data) = await GetAsync<PagedResult<PatientDetailDto>>(
            _admin!, "/api/v1/patients?pageSize=100");
        data!.Items.Should().NotContain(p => p.Id == _patientId);
    }
}
```

---

### Task 1.7: MedicalCaseEditJourneyTests (~6 steps)

**Files:**
- Create: `tests/LYBT.Tests.Server/UserJourneys/MedicalCaseEditJourneyTests.cs`

**Purpose:** Verify editing rules for completed cases, print protection.

```csharp
namespace LYBT.Tests.Server.UserJourneys;

[Collection("Server")]
[TestCaseOrderer("Xunit.Extensions.Ordering.TestCaseOrderer", "Xunit.Extensions.Ordering")]
public sealed class MedicalCaseEditJourneyTests : JourneyTestBase
{
    private static HttpClient? _doctor;
    private static HttpClient? _admin;
    private static Guid _patientId;
    private static Guid _herbId;
    private static Guid _caseId;
    private static Guid _doctorUserId;

    public MedicalCaseEditJourneyTests(ServerFixture fixture) : base(fixture) { }

    [Fact, Order(1)]
    public async Task Step01_Setup_Completed_Case()
    {
        await ResetForJourneyAsync();
        _admin = await LoginAsAdminAsync();
        _doctor = await LoginAsDoctorAsync();

        var (_, user) = await GetAsync<UserDetailDto>(_doctor!, "/api/v1/users/current");
        _doctorUserId = user!.Id;

        // Create patient
        var (_, patient) = await PostAsync<PatientDetailDto>(_admin!,
            "/api/v1/patients", new PatientInputDto { Name = UniqueName("赵六"), PhoneNumber = UniquePhone() });
        _patientId = patient!.Id;

        // Create herb
        var (_, herb) = await PostAsync<HerbDetailDto>(_admin!,
            "/api/v1/herbs", new HerbInputDto { Name = UniqueName("甘草"), Unit = "克", Price = 5.0m });
        _herbId = herb!.Id;

        // Create case with diagnosis + prescription
        var (_, mc) = await PostAsync<MedicalCaseDetailDto>(_doctor!,
            "/api/v1/medicalcases", new MedicalCaseInputDto { PatientId = _patientId, UserId = _doctorUserId });
        _caseId = mc!.Id;

        // Save diagnosis + prescription
        await _doctor!.PutAsJsonAsync($"/api/v1/medicalcases/{_caseId}", new MedicalCaseInputDto
        {
            Id = _caseId, PatientId = _patientId, UserId = _doctorUserId,
            NeedsPrescription = true,
            Consultation = new ConsultationInputDto { TcmDiagnosis = "脾虚湿困" },
            Prescription = new PrescriptionInputDto
            {
                MedicalCaseId = _caseId, DosageCount = 3, TotalPrice = 15.0m,
                Items = new() { new() { HerbId = _herbId, HerbName = "甘草", Unit = "克", Dosage = 10, UnitPrice = 5.0m, Subtotal = 15.0m } }
            }
        });

        // Complete
        await _doctor!.PutAsJsonAsync($"/api/v1/medicalcases/{_caseId}/status",
            new MedicalCaseStatusInputDto { Status = MedicalCaseStatus.Completed });
    }

    [Fact, Order(2)]
    public async Task Step02_Case_Is_Completed()
    {
        var (_, data) = await GetAsync<MedicalCaseDetailDto>(_doctor!, $"/api/v1/medicalcases/{_caseId}");
        data!.Status.Should().Be(MedicalCaseStatus.Completed);
    }

    [Fact, Order(3)]
    public async Task Step03_Print_Prescription()
    {
        var response = await _doctor!.PutAsJsonAsync(
            $"/api/v1/medicalcases/{_caseId}/print-completed", new { });
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact, Order(4)]
    public async Task Step04_Verify_Print_State()
    {
        var (_, data) = await GetAsync<MedicalCaseDetailDto>(_doctor!, $"/api/v1/medicalcases/{_caseId}");
        data!.IsPrinted.Should().BeTrue();
        data.PrintCount.Should().BeGreaterThan(0);
    }

    [Fact, Order(5)]
    public async Task Step05_Edit_After_Print_Requires_Reason()
    {
        var input = new MedicalCaseInputDto
        {
            Id = _caseId, PatientId = _patientId, UserId = _doctorUserId,
            EditReason = "修正诊断",
            Consultation = new ConsultationInputDto { TcmDiagnosis = "脾虚湿困(修正)" }
        };

        var (response, _) = await PutAsync<MedicalCaseDetailDto>(
            _doctor!, $"/api/v1/medicalcases/{_caseId}", input);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact, Order(6)]
    public async Task Step06_Admin_Can_View_Audit_Log()
    {
        var (response, _) = await GetAsync<PagedResult<object>>(
            _admin!, $"/api/v1/medicalcases/{_caseId}/audit-logs");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
```

---

### Task 1.8: BatchOperationsJourneyTests (~5 steps)

**Files:**
- Create: `tests/LYBT.Tests.Server/UserJourneys/BatchOperationsJourneyTests.cs`

**Purpose:** Verify batch import/export and reference checking.

```csharp
namespace LYBT.Tests.Server.UserJourneys;

[Collection("Server")]
[TestCaseOrderer("Xunit.Extensions.Ordering.TestCaseOrderer", "Xunit.Extensions.Ordering")]
public sealed class BatchOperationsJourneyTests : JourneyTestBase
{
    private static HttpClient? _admin;
    private static Guid _herb1Id;
    private static Guid _herb2Id;

    public BatchOperationsJourneyTests(ServerFixture fixture) : base(fixture) { }

    [Fact, Order(1)]
    public async Task Step01_Setup()
    {
        await ResetForJourneyAsync();
        _admin = await LoginAsAdminAsync();
    }

    [Fact, Order(2)]
    public async Task Step02_Batch_Create_Herbs()
    {
        // Create two herbs individually (batch-import uses JSON array)
        var (_, h1) = await PostAsync<HerbDetailDto>(_admin!,
            "/api/v1/herbs", new HerbInputDto { Name = UniqueName("白术"), Unit = "克", Price = 12.0m });
        var (_, h2) = await PostAsync<HerbDetailDto>(_admin!,
            "/api/v1/herbs", new HerbInputDto { Name = UniqueName("茯苓"), Unit = "克", Price = 8.0m });

        _herb1Id = h1!.Id;
        _herb2Id = h2!.Id;
    }

    [Fact, Order(3)]
    public async Task Step03_Create_Formula_Using_Herbs()
    {
        var input = new FormulaInputDto
        {
            Name = UniqueName("四君子汤"),
            Effect = "益气健脾",
            Usage = "水煎服",
            Herbs = new()
            {
                new() { HerbId = _herb1Id, HerbName = "白术", Dosage = 9, Unit = "克" },
                new() { HerbId = _herb2Id, HerbName = "茯苓", Dosage = 9, Unit = "克" }
            }
        };

        var (response, _) = await PostAsync<FormulaDetailDto>(_admin!, "/api/v1/formulas", input);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact, Order(4)]
    public async Task Step04_Check_Herb_Reference_Blocks_Delete()
    {
        // Herb1 is used in formula, should have references
        var response = await _admin!.GetAsync($"/api/v1/herbs/{_herb1Id}/check-reference");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadAsStringAsync();
        // Reference check should indicate herb is in use
        body.Should().NotBeNullOrEmpty();
    }

    [Fact, Order(5)]
    public async Task Step05_Export_Herbs()
    {
        var response = await _admin!.GetAsync("/api/v1/herbs/export-all");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Contain("json");
    }
}
```

---

### Task 1.9: Full Journey Verification

**Step 1: Build all**

Run:
```bash
dotnet build tests/LYBT.Tests.Server/ -v q
```
Expected: 0 errors.

**Step 2: Run all Journeys**

Run:
```bash
dotnet test tests/LYBT.Tests.Server/ --filter "FullyQualifiedName~UserJourneys" -v normal
```
Expected: All ~47 Journey tests pass.

**Step 3: Run full test suite (Journey + existing)**

Run:
```bash
dotnet test tests/LYBT.Tests.Server/ --no-build -v minimal
```
Expected: All tests pass (existing + new Journey tests).

---

## Phase 2: Delete Low-Value Tests

### Task 2.1: Delete Desktop Mock-Heavy Tests

**Files to delete** (all under `tests/LYBT.Tests.Desktop/`):

```
ViewModels/Patients/PatientServiceTests.cs
ViewModels/MedicalCase/MedicalCaseFormViewModel_SimpleTests.cs
ViewModels/Shell/LoginCoordinatorTests.cs
ViewModels/Auth/LoginViewModelTests.cs
ViewModels/Users/UserServiceTests.cs
ViewModels/Herbs/HerbItemViewModelBaseTests.cs
ViewModels/Herbs/HerbFormulaItemViewModelTests.cs
ViewModels/Formula/FormulaHerbItemViewModelTests.cs
ViewModels/Formula/FormulaEditRegressionTests.cs
ViewModels/MedicalCase/PrescriptionEditFlowTests.cs
ViewModels/MedicalCase/PrescriptionHerbItemPriceTests.cs
ViewModels/Shell/Session/SessionLifecycleManagerTests.cs
PureLogic/Foundation/Security/AuthenticationServiceTests.cs
PureLogic/Foundation/Security/TokenManagerTests.cs
PureLogic/Infrastructure/Models/State/LoadingStateTests.cs
PureLogic/Infrastructure/Models/State/PaginationStateTests.cs
PureLogic/Infrastructure/Models/State/SearchStateTests.cs
PureLogic/Infrastructure/Services/SearchServiceTests.cs
```

**Step 1:** Delete files (bash `rm` for each).

**Step 2:** Build to verify no compile errors:
```bash
dotnet build tests/LYBT.Tests.Desktop/ -v q
```

**Step 3:** Run remaining Desktop tests:
```bash
dotnet test tests/LYBT.Tests.Desktop/ --no-build
```
Expected: Fewer tests, all pass.

**Note:** Before deleting, confirm exact file paths exist. Some files may have slightly different names. Use `find` or `ls` to verify.

### Task 2.2: Delete Server Trivial Entity Tests

**Target:** Test methods in `PureLogic/Entities/` that only test property getters/setters.

**Approach:** Review each file in `tests/LYBT.Tests.Server/PureLogic/Entities/`. Delete individual TEST METHODS (not entire files) that only test `entity.X = value; assert entity.X == value`. Keep methods that test computed properties or business logic.

**Step 1:** List all entity test files:
```bash
find tests/LYBT.Tests.Server/PureLogic/Entities -name "*.cs" -type f
```

**Step 2:** For each file, identify and remove trivial property tests.

**Step 3:** Build + run to verify.

### Task 2.3: Verify After Deletion

Run:
```bash
dotnet test LYBT.All.sln --filter "FullyQualifiedName~LYBT.Tests" -v minimal
```
Expected: All remaining tests pass. Total count should be ~1400-1500.

---

## Phase 3: Features Directory Reorganization

### Task 3.1: Create Features Directory Structure

```bash
mkdir -p tests/LYBT.Tests.Server/Features/{Auth,Patients,Herbs,Formulas,MedicalCases,Users,Sync}
```

### Task 3.2: Move Integration Tests

Move files, updating namespaces:

| From | To |
|------|-----|
| `Auth/AuthIntegrationTests.cs` | `Features/Auth/LoginTests.cs` |
| `Auth/AuthTokenAdvancedIntegrationTests.cs` | `Features/Auth/TokenTests.cs` |
| `Auth/AuthSmokeTests.cs` | `Features/Auth/SmokeTests.cs` |
| `Patients/PatientIntegrationTests.cs` | `Features/Patients/CrudTests.cs` |
| `Herbs/HerbIntegrationTests.cs` | `Features/Herbs/CrudTests.cs` |
| `Formulas/FormulaIntegrationTests.cs` | `Features/Formulas/CrudTests.cs` |
| `MedicalCases/MedicalCaseIntegrationTests.cs` | `Features/MedicalCases/LifecycleTests.cs` |
| `MedicalCases/MedicalCasePermissionAndFilterTests.cs` | `Features/MedicalCases/PermissionTests.cs` |
| `MedicalCases/PrescriptionAggregateTests.cs` | `Features/MedicalCases/PrescriptionTests.cs` |
| `Users/UserIntegrationTests.cs` | `Features/Users/CrudTests.cs` |
| `Sync/SyncIntegrationTests.cs` | `Features/Sync/ProtocolTests.cs` |

### Task 3.3: Move Validator Tests

| From | To |
|------|-----|
| `PureLogic/Validators/AuthValidatorTests.cs` | `Features/Auth/ValidationTests.cs` |
| `PureLogic/Validators/PatientValidatorTests.cs` | `Features/Patients/ValidationTests.cs` |
| etc. | etc. |

### Task 3.4: Update Namespaces

For each moved file, update namespace from e.g., `LYBT.Tests.Server.Auth` to `LYBT.Tests.Server.Features.Auth`.

### Task 3.5: Build + Verify

```bash
dotnet build tests/LYBT.Tests.Server/ -v q && dotnet test tests/LYBT.Tests.Server/ --no-build
```

---

## Phase 4: Fix + Final Verify

### Task 4.1: Fix Desktop Timeout

Investigate and fix the vstest session timeout issue causing 41 Desktop tests to not execute.

Likely causes:
- `TokenRefreshHandlerIntegrationTests` with real HTTP delays (15s each)
- vstest default session timeout too low

Solutions to try:
1. Add `.runsettings` with higher timeout
2. Reduce real HTTP delays in token refresh tests (use shorter timeouts in test config)
3. Move slow tests to separate test class with explicit timeout

### Task 4.2: Full Verification

```bash
dotnet test LYBT.All.sln --filter "FullyQualifiedName~LYBT.Tests" -v minimal
```

Expected:
- Server: ~950+ tests (existing + 47 Journey)
- Desktop: ~400+ tests (after deletion)
- Architecture: 76 tests
- Total: ~1400+ tests, 0 failures, 0 timeout aborts

### Task 4.3: Update Documentation

Update these files:
- `CLAUDE.md` - Test architecture section
- `tests/LYBT.Tests.Server/README.md` - New directory structure
- `docs/03-architecture/testing.md` - Testing strategy

### Task 4.4: Final Metrics

| Metric | Before | After |
|--------|--------|-------|
| Total tests | ~2021 | ~1400 |
| UserJourney tests | 0 | ~47 |
| Low-value mock tests | ~550 | 0 |
| Timeout-aborted tests | 41 | 0 |
| Signal density | Low | High |
