# 测试驱动开发实施计划

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.
> **TDD 原则:** RED (失败测试) → GREEN (通过实现) → REFACTOR (重构) → REPEAT

**Goal:** 通过 TDD 确保权限矩阵缺陷修复和 Journey Test 重构按设计完成，实现 80%+ 测试覆盖率

**Architecture:** 采用三层测试架构 (Journey Test Layer A → Feature Test Layer B → PureLogic Test)，基于 ServerFixture 的真实 SQL Server 集成测试

**Tech Stack:** xUnit, FluentAssertions, ASP.NET Core TestServer, SQL Server, Respawn

---

## Phase 1: 权限矩阵缺陷修复验证 (v1.3)

### Task 1.1: D-4 Receptionist 医案可见性测试

**目标:** 验证 Receptionist 无直接访问 /medicalcases 权限，仅能通过 /registrations/queue 间接获取就诊状态

**Files:**
- Create: `tests/LYBT.Tests.Server/UserJourneys/ReceptionistJourneyTests.cs`
- Test: `tests/LYBT.Tests.Server/UserJourneys/ReceptionistJourneyTests.cs`

**Step 1: 编写失败测试 (RED)**

```csharp
using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Registration;
using LYBT.Tests.Server.UserJourneys;
using Xunit;

namespace LYBT.Tests.Server.UserJourneys;

/// <summary>
/// UAT: Receptionist 工作流程 - 挂号、队列查看、取消
/// 验证：Receptionist 无直接医案访问权限，仅通过挂号队列间接查看
/// </summary>
[Collection("Clinical")]
public sealed class ReceptionistJourneyTests : JourneyTestBase<ClinicalFixture>
{
    public ReceptionistJourneyTests(ClinicalFixture fixture) : base(fixture) { }

    [Fact]
    public async Task Receptionist_AccessMedicalCases_Direct_Returns403()
    {
        // Arrange
        await ResetForJourneyAsync();
        var receptionist = await LoginAsReceptionistAsync();

        // Act
        var response = await receptionist.GetAsync("/api/v1/medicalcases");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden,
            "Receptionist should not have direct access to medical cases endpoint");
    }
}
```

**Step 2: 运行测试验证失败**

运行: `dotnet test tests/LYBT.Tests.Server --filter "FullyQualifiedName~ReceptionistJourneyTests" -v n`
预期: FAIL (LoginAsReceptionistAsync 方法不存在)

**Step 3: 添加测试辅助方法 (GREEN)**

修改: `tests/LYBT.Tests.Server/_Infrastructure/ServerFixture.cs`

```csharp
public async Task<HttpClient> LoginAsReceptionistAsync()
{
    var loginRequest = new LoginRequest { UserName = "receptionist", Password = "TestReceptionist2025@" };
    var loginResponse = await AnonymousClient.PostAsJsonAsync("/api/v1/auth/login", loginRequest);
    loginResponse.EnsureSuccessStatusCode();

    var body = await loginResponse.Content.ReadFromJsonAsync<ApiResponse<LoginResponse>>(JsonOptions);
    var client = new HttpClient();
    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body!.Data!.Token);
    return client;
}
```

**Step 4: 运行测试验证通过**

运行: `dotnet test tests/LYBT.Tests.Server --filter "FullyQualifiedName~ReceptionistJourneyTests.Receptionist_AccessMedicalCases_Direct_Returns403" -v n`
预期: PASS

**Step 5: 提交**

```bash
git add tests/LYBT.Tests.Server/UserJourneys/ReceptionistJourneyTests.cs
git commit -m "test: add D-4 Receptionist medical case access test (RED-GREEN)"
```

---

### Task 1.2: D-5 归属字段一致性测试

**目标:** 验证 MedicalCase CreatedBy vs UserId 语义统一

**Files:**
- Modify: `tests/LYBT.Tests.Server/PureLogic/Entities/MedicalCase/MedicalCaseModelTests.cs`

**Step 1: 编写失败测试**

```csharp
[Fact]
public void MedicalCase_UserId_Equals_CreatedBy_Semantically()
{
    // Arrange
    var userId = Guid.NewGuid();
    var medicalCase = new MedicalCase { PatientId = Guid.NewGuid(), UserId = userId };

    // Act
    var createdByProperty = typeof(MedicalCase).GetProperty("CreatedBy");
    var userIdProperty = typeof(MedicalCase).GetProperty("UserId");

    // Assert
    createdByProperty.Should().NotBeNull("CreatedBy should be accessible");
    userIdProperty.Should().NotBeNull("UserId should be accessible");

    // Verify UserId is the authoritative field for ownership
    medicalCase.UserId.Should().Be(userId, "UserId should be set correctly");
}
```

**Step 2: 运行测试验证失败**

运行: `dotnet test tests/LYBT.Tests.Server --filter "FullyQualifiedName~MedicalCaseModelTests" -v n`
预期: FAIL (可能 CreatedBy 字段不存在或逻辑不一致)

**Step 3: 修复实体定义**

修改: `src/Shared/Models/Entities/MedicalCase.cs`

```csharp
public class MedicalCase : BaseEntity
{
    // UserId 是归属字段 (业务字段，不可变)
    public Guid UserId { get; set; }

    // CreatedBy 是审计字段 (BaseEntity 继承，自动填充)
    // 注意：UserId 和 CreatedBy 始终一致 - UserId 是权威来源
}
```

**Step 4: 运行测试验证通过**

运行: `dotnet test tests/LYBT.Tests.Server --filter "FullyQualifiedName~MedicalCaseModelTests" -v n`
预期: PASS

**Step 5: 提交**

```bash
git add src/Shared/Models/Entities/MedicalCase.cs tests/LYBT.Tests.Server/PureLogic/Entities/MedicalCase/MedicalCaseModelTests.cs
git commit -m "fix: unify MedicalCase UserId/CreatedBy semantics (D-5)"
```

---

### Task 1.3: I-2 当天可编辑边界条件测试

**目标:** 验证 04:00 边界条件的医案编辑权限 (当天 03:59 可编辑，隔天 04:01 锁定)

**Files:**
- Modify: `tests/LYBT.Tests.Server/UserJourneys/MedicalCaseEditJourneyTests.cs`

**Step 1: 编写失败测试**

```csharp
[Fact]
public async Task MedicalCase_Edit_Completed_SameDay_Before0400_Allowed()
{
    // Arrange
    await ResetForJourneyAsync();
    var doctor = await LoginAsDoctorAsync();
    var admin = await LoginAsAdminAsync();

    // Create patient + case
    var (_, patient) = await PostAsync<PatientDetailDto>(admin, "/api/v1/patients", new PatientInputDto
    {
        Name = UniqueName("当天编辑"), Gender = Gender.Male,
        BirthDate = new DateTime(1980, 1, 1), PhoneNumber = UniquePhone(),
        IdNumber = $"32010119800101{Random.Shared.Next(1000, 9999)}", Address = "测试"
    });

    var (_, doctorData) = await GetAsync<UserDetailDto>(doctor, "/api/v1/users/current");
    var doctorUserId = doctorData!.Id;

    var (_, createdCase) = await PostAsync<MedicalCaseDetailDto>(doctor, "/api/v1/medicalcases",
        new MedicalCaseInputDto { PatientId = patient!.Id, UserId = doctorUserId });

    // Complete case
    await doctor.PutAsJsonAsync($"/api/v1/medicalcases/{createdCase!.Id}/status",
        new MedicalCaseStatusInputDto { Status = MedicalCaseStatus.Completed });

    // Act: Edit within same day (before 04:00 boundary)
    var editResponse = await doctor.PutAsJsonAsync($"/api/v1/medicalcases/{createdCase.Id}", new MedicalCaseInputDto
    {
        Id = createdCase.Id, PatientId = patient.Id, UserId = doctorUserId,
        EditReason = "修正诊断",
        Consultation = new ConsultationInputDto { TcmDiagnosis = "修正后的诊断" }
    });

    // Assert
    editResponse.StatusCode.Should().Be(HttpStatusCode.OK,
        "Completed case on same day should be editable with EditReason");
}

[Fact]
public async Task MedicalCase_Edit_Completed_NextDay_After0400_Denied()
{
    // Arrange: Complete case, then simulate next day after 04:00
    await ResetForJourneyAsync();

    // Use TimeProvider to simulate next day 04:01
    // Note: May need to mock IsLocked logic

    // Act: Attempt edit next day
    // Expect 403 for Doctor (IsLocked = true)

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.Forbidden,
        "Completed case after 04:00 next day should be locked for Doctor");
}
```

**Step 2: 运行测试验证失败**

运行: `dotnet test tests/LYBT.Tests.Server --filter "FullyQualifiedName~MedicalCaseEditJourneyTests" -v n`
预期: FAIL (边界条件逻辑可能不正确)

**Step 3: 修复 IsLocked 逻辑**

修改: `src/Server/Domain/MedicalCase.cs` 或 Service 层锁定逻辑

```csharp
public bool IsLocked(DateTimeOffset currentTime)
{
    // 锁定规则：已完成 + 隔天 (04:00 边界)
    if (Status != MedicalCaseStatus.Completed) return false;

    var completedDate = CompletedAt?.Date ?? DateTimeOffset.Now.Date;
    var today = currentTime.Date;

    // 04:00 边界逻辑：当前时间 < 04:00 -> 仍算"当天"
    if (currentTime.Hour < 4)
    {
        today = today.AddDays(-1);
    }

    return completedDate < today; // 隔天锁定
}
```

**Step 4: 运行测试验证通过**

运行: `dotnet test tests/LYBT.Tests.Server --filter "FullyQualifiedName~MedicalCaseEditJourneyTests" -v n`
预期: PASS

**Step 5: 提交**

```bash
git add tests/LYBT.Tests.Server/UserJourneys/MedicalCaseEditJourneyTests.cs
git commit -m "test: add I-2 same-day edit boundary test (04:00 rule)"
```

---

### Task 1.4: G-9 Registration 回退流程测试

**目标:** 验证医案取消后挂号状态回退 (Source=Receptionist 时回退 Waiting 并保留 MedicalCaseId)

**Files:**
- Modify: `tests/LYBT.Tests.Server/UserJourneys/ReturnVisitJourneyTests.cs`
- Create: `tests/LYBT.Tests.Server/UserJourneys/RegistrationJourneyTests.cs`

**Step 1: 编写失败测试**

```csharp
[Fact]
public async Task Registration_Cancel_ByReceptionist_RevertsToWaiting()
{
    // Arrange: Create registration + medical case
    await ResetForJourneyAsync();
    var receptionist = await LoginAsReceptionistAsync();
    var doctor = await LoginAsDoctorAsync();
    var admin = await LoginAsAdminAsync();

    // Create patient
    var (_, patient) = await PostAsync<PatientDetailDto>(admin, "/api/v1/patients", new PatientInputDto
    {
        Name = UniqueName("回退测试"), Gender = Gender.Male,
        BirthDate = new DateTime(1980, 1, 1), PhoneNumber = UniquePhone(),
        IdNumber = $"32010119800101{Random.Shared.Next(1000, 9999)}", Address = "测试"
    });

    // Create registration (Source=Receptionist, Status=Waiting)
    var (_, registration) = await PostAsync<RegistrationDetailDto>(receptionist, "/api/v1/registrations",
        new RegistrationInputDto { PatientId = patient!.Id, DoctorId = /* doctor user id */ });

    registration!.Status.Should().Be(RegistrationStatus.Waiting);

    // Act 1: Cancel registration (Receptionist)
    var cancelResponse = await receptionist.PutAsJsonAsync($"/api/v1/registrations/{registration.Id}/cancel",
        new CancelRegistrationRequest { Reason = "患者取消" });

    // Assert 1: Registration reverted to Waiting
    cancelResponse.StatusCode.Should().Be(HttpStatusCode.OK);

    var (_, updatedReg) = await GetAsync<RegistrationDetailDto>(receptionist, $"/api/v1/registrations/{registration.Id}");
    updatedReg!.Status.Should().Be(RegistrationStatus.Waiting,
        "Receptionist cancel should revert to Waiting for potential recovery");
    updatedReg.MedicalCaseId.Should().NotBeNull(
        "MedicalCaseId should be preserved for recovery");
}
```

**Step 2: 运行测试验证失败**

运行: `dotnet test tests/LYBT.Tests.Server --filter "FullyQualifiedName~RegistrationJourneyTests" -v n`
预期: FAIL (回退逻辑可能不正确)

**Step 3: 修复取消逻辑**

修改: `src/Server/Services/RegistrationService.cs`

```csharp
public async Task<Registration> CancelAsync(Guid id, string cancelledBy)
{
    var registration = await _repo.GetByIdAsync(id);

    if (registration.Source == RegistrationSource.Receptionist)
    {
        // 回退 Waiting，保留 MedicalCaseId (用于恢复)
        registration.Status = RegistrationStatus.Waiting;
        // MedicalCaseId 保持不变
    }
    else if (registration.Source == RegistrationSource.Doctor)
    {
        // 闭环取消
        registration.Status = RegistrationStatus.Cancelled;
        registration.MedicalCaseId = null;
    }

    await _repo.UpdateAsync(registration);
    return registration;
}
```

**Step 4: 运行测试验证通过**

运行: `dotnet test tests/LYBT.Tests.Server --filter "FullyQualifiedName~RegistrationJourneyTests" -v n`
预期: PASS

**Step 5: 提交**

```bash
git add tests/LYBT.Tests.Server/UserJourneys/RegistrationJourneyTests.cs src/Server/Services/RegistrationService.cs
git commit -m "fix: G-9 registration cancel revert to Waiting (Receptionist source)"
```

---

### Task 1.5: G-11 医生禁用后挂号处理测试

**目标:** 验证医生禁用后 Waiting 状态挂号自动取消 (先清后禁策略)

**Files:**
- Create: `tests/LYBT.Tests.Server/UserJourneys/DoctorDisableJourneyTests.cs`

**Step 1: 编写失败测试**

```csharp
using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Tests.Server.UserJourneys;
using Xunit;

namespace LYBT.Tests.Server.UserJourneys;

/// <summary>
/// UAT: 医生禁用场景 - 验证 Waiting 挂号处理
/// </summary>
[Collection("Clinical")]
public sealed class DoctorDisableJourneyTests : JourneyTestBase<ClinicalFixture>
{
    public DoctorDisableJourneyTests(ClinicalFixture fixture) : base(fixture) { }

    [Fact]
    public async Task DoctorDisable_HasWaitingRegistrations_BlocksDisable()
    {
        // Arrange
        await ResetForJourneyAsync();
        var admin = await LoginAsAdminAsync();
        var receptionist = await LoginAsReceptionistAsync();

        // Create patient + registration (Waiting)
        var (_, patient) = await PostAsync<PatientDetailDto>(admin, "/api/v1/patients", new PatientInputDto
        {
            Name = UniqueName("挂号患者"), Gender = Gender.Male,
            BirthDate = new DateTime(1980, 1, 1), PhoneNumber = UniquePhone(),
            IdNumber = $"32010119800101{Random.Shared.Next(1000, 9999)}", Address = "测试"
        });

        var (_, registration) = await PostAsync<RegistrationDetailDto>(receptionist, "/api/v1/registrations",
            new RegistrationInputDto { PatientId = patient!.Id, DoctorId = /* doctor user id */ });

        registration!.Status.Should().Be(RegistrationStatus.Waiting);

        // Act: Admin attempts to disable doctor with Waiting registrations
        var disableResponse = await admin.PutAsJsonAsync($"/api/v1/users/{doctorId}/status",
            new UserStatusInputDto { Status = UserStatus.Disabled });

        // Assert
        disableResponse.IsSuccessStatusCode.Should().BeFalse(
            "Doctor with Waiting registrations should block disable");
        disableResponse.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity,
            "Should return 422 with error message");

        var (errorMsg, _) = await ReadErrorAsync(disableResponse);
        errorMsg.Should().Contain("Waiting", "Error should mention Waiting registrations");
    }

    [Fact]
    public async Task DoctorDisable_NoWaitingRegistrations_Succeeds()
    {
        // Arrange: Doctor with no Waiting registrations
        // Act: Admin disables doctor
        // Assert: Success
    }
}
```

**Step 2: 运行测试验证失败**

运行: `dotnet test tests/LYBT.Tests.Server --filter "FullyQualifiedName~DoctorDisableJourneyTests" -v n`
预期: FAIL (禁用逻辑可能缺少 Waiting 检查)

**Step 3: 修复禁用逻辑**

修改: `src/Server/Services/UserService.cs`

```csharp
public async Task DisableUserAsync(Guid id)
{
    var user = await _repo.GetByIdAsync(id);

    if (user.Role == UserRole.Doctor)
    {
        // Check for Waiting registrations
        var waitingCount = await _registrationRepo.CountWaitingByDoctorAsync(id);
        if (waitingCount > 0)
        {
            throw new BusinessException(
                "医生有待接诊挂号，需先由前台取消所有 Waiting 挂号后再禁用",
                ErrorCode.DoctorHasWaitingRegistrations);
        }
    }

    user.Status = UserStatus.Disabled;
    await _repo.UpdateAsync(user);
}
```

**Step 4: 运行测试验证通过**

运行: `dotnet test tests/LYBT.Tests.Server --filter "FullyQualifiedName~DoctorDisableJourneyTests" -v n`
预期: PASS

**Step 5: 提交**

```bash
git add tests/LYBT.Tests.Server/UserJourneys/DoctorDisableJourneyTests.cs src/Server/Services/UserService.cs
git commit -m "fix: G-11 block doctor disable with Waiting registrations (先清后禁)"
```

---

## Phase 2: Journey Test 重构 (Chapter 0-8)

### Task 2.1: Chapter 0 - Auth & Security 重构

**目标:** 补全 AuthJourneyTests 负面场景

**Files:**
- Modify: `tests/LYBT.Tests.Server/UserJourneys/AuthJourneyTests.cs`

**Step 1: 添加负面测试场景**

```csharp
[Fact]
public async Task Auth_Login_NonExistentUser_Returns401()
{
    // Arrange
    await ResetForJourneyAsync();
    var loginRequest = new LoginRequest { UserName = "nonexistent", Password = "Test123!" };

    // Act
    var response = await AnonymousClient.PostAsJsonAsync("/api/v1/auth/login", loginRequest);

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
}

[Fact]
public async Task Auth_Login_DisabledUser_Returns401()
{
    // Arrange
    await ResetForJourneyAsync();
    var admin = await LoginAsAdminAsync();

    // Disable a user first
    // Then attempt login

    // Act + Assert
    response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
}

[Fact]
public async Task Auth_Login_EmptyCredentials_Returns400()
{
    // Arrange + Act
    var response = await AnonymousClient.PostAsJsonAsync("/api/v1/auth/login",
        new { UserName = "", Password = "" });

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
}
```

**Step 2-5: 运行验证 + 提交**

---

### Task 2.2: Chapter 1 - System Bootstrap 重构

**Files:** `BootstrapJourneyTests.cs`

---

### Task 2.3: Chapter 2 - Admin Setup 重构

**Files:** `AdminSetupJourneyTests.cs`

---

### Task 2.4: Chapter 3 - Master Data 重构

**Files:** `HerbFormulaManagementJourneyTests.cs`

---

### Task 2.5: Chapter 4 - Patient Management 重构

**Files:** `PatientManagementJourneyTests.cs`

---

### Task 2.6: Chapter 5 - First Visit 重构

**Files:** `FirstVisitJourneyTests.cs`

**决策:** 保留 FirstVisitJourneyTests，删除 DoctorClinicalJourneyTests (高度重叠)

---

### Task 2.7: Chapter 6 - Return Visit 重构

**Files:** `ReturnVisitJourneyTests.cs`

---

### Task 2.8: Chapter 7 - Medical Case Edit 重构

**Files:** `MedicalCaseEditJourneyTests.cs`

---

### Task 2.9: Chapter 8 - Cross-Narrative Guard 重构

**Files:** `CrossNarrativeValidationTests.cs`

---

### Task 2.10: 删除冗余测试

**Files:**
- Delete: `tests/LYBT.Tests.Server/UserJourneys/DoctorClinicalJourneyTests.cs`
- Move: `tests/LYBT.Tests.Server/Features/*/` Layer B tests -> `_Deferred/`

---

## Phase 3: PRD 驱动测试对齐 (45 Must Have US)

### Task 3.1: AUTH 模块 Must Have US 覆盖 (12 US)

**Files:** `tests/LYBT.Tests.Server/Features/Auth/US_Auth_MustHaveTests.cs`

**Step 1: 检查现有覆盖**

```bash
grep -n "US-AUTH" tests/LYBT.Tests.Server/Features/Auth/US_Auth_MustHaveTests.cs
```

**Step 2: 补全缺失测试**

每个 US 一个测试方法，命名规范：
```csharp
[Fact]
public async Task US_AUTH_001_Login_WithValidCredentials_ShouldReturnToken()
[Fact]
public async Task US_AUTH_002_Logout_WithValidToken_ShouldRevokeToken()
// ...
```

---

### Task 3.2-3.8: 其他模块 Must Have US 覆盖

| 模块 | US 数 | 测试文件 |
|------|------|---------|
| USER | 8 | `US_User_MustHaveTests.cs` |
| HERB | 5 | `US_Herb_MustHaveTests.cs` |
| FORM | 6 | `US_Formula_MustHaveTests.cs` |
| PAT | 4 | `US_Patient_MustHaveTests.cs` |
| MC | 7 | `US_MedicalCase_MustHaveTests.cs` |
| REG | 6 | `US_Registration_MustHaveTests.cs` |
| SYNC | 3 | `US_Sync_MustHaveTests.cs` |

---

## Phase 4: 验证与完成

### Task 4.1: 全量测试运行

```bash
# Server tests
dotnet test tests/LYBT.Tests.Server --filter "FullyQualifiedName~LYBT.Tests.Server" --logger "console;verbosity=detailed"

# Desktop tests
dotnet test tests/LYBT.Tests.Desktop --filter "FullyQualifiedName~LYBT.Tests.Desktop"

# Architecture tests
dotnet test tests/LYBT.Tests.Architecture --filter "FullyQualifiedName~LYBT.Tests.Architecture"
```

**验收标准:**
- Server: > 95% 通过
- Desktop: > 95% 通过
- Architecture: 100% 通过

---

### Task 4.2: 测试覆盖率验证

```bash
# 生成覆盖率报告
dotnet test LYBTZYZS.sln --collect:"XPlat Code Coverage" --settings coverlet.runsettings

# 查看覆盖率摘要
dotnet tool install --global dotnet-coverage
dotnet-coverage report coverage.cobertura.xml -f html -o coverage-report
```

**目标:** 80%+ 覆盖率 (Must Have US 100%)

---

### Task 4.3: 文档同步

**Files:**
- Update: `docs/02-requirements/role-permission-matrix.md` (测试验证状态)
- Create: `docs/06-operations/test-coverage-baseline.md`

---

## 风险与缓解

| 风险 | 缓解措施 |
|------|---------|
| 测试执行时间长 | 6 Collection 并行，目标<5 分钟 |
| 测试数据库污染 | Respawn 清理 + 独立 Database 每 Collection |
| Mock 过度使用 | AntiMockRuleTests 强制 Server 零 mock |
| PRD 变更不同步 | 测试命名包含 US 编号 |

---

## 下一步行动

1. **确认优先级**: Phase 1 (权限矩阵缺陷) or Phase 2 (Journey Test 重构)
2. **开始执行**: 使用 `superpowers:executing-plans` 按 Task 逐条执行
3. **持续验证**: 每完成一个 Phase 运行测试验证
