# Phase 1 权限矩阵缺陷修复 Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** 修复权限矩阵中的3个高/中优先级缺陷: I-2 "当天可编辑"锁定逻辑、G-9 Registration回退流程、G-11 医生禁用后挂号处理

**Architecture:**
- **I-2**: 在MedicalCase查询层动态计算IsLocked字段，Service层实现3层权限检查(状态->锁定->角色)
- **G-9**: MedicalCase取消时分流处理: Receptionist来源回退到Waiting保留MedicalCaseId; Doctor来源直接Cancelled闭环
- **G-11**: User禁用前置校验，查询该Doctor的Waiting状态Registration，存在则阻止禁用(422)

**Tech Stack:** .NET 8, EF Core, FluentAssertions, xUnit, Respawn (测试隔离)

---

## Task 1: I-2 MedicalCase "当天可编辑" IsLocked 计算

**Files:**
- Modify: `src/Shared/LYBT.Shared.Models/Contracts/MedicalCase/MedicalCaseDetailDto.cs`
- Modify: `src/Server/Modules/LYBT.Module.MedicalCase/Services/MedicalCaseQueryService.cs`
- Modify: `src/Server/Modules/LYBT.Module.MedicalCase/Services/MedicalCaseCommandService.cs`
- Test: `tests/LYBT.Tests.Server/Features/US_MedicalCase_MustHaveTests.cs`

**Step 1: 添加 IsLocked 属性到 DTO**

```csharp
// In MedicalCaseDetailDto.cs, add property:
/// <summary>
/// 是否已锁定(隔天不可编辑). 动态计算: CompletedAt.Date < Today
/// </summary>
public bool IsLocked => CaseStatus == MedicalCaseStatus.Completed &&
                        CompletedAt.HasValue &&
                        CompletedAt.Value.Date < DateTime.Today;
```

**Step 2: 运行编译验证**

```bash
dotnet build src/Shared/LYBT.Shared.Models/LYBT.Shared.Models.csproj --no-restore -v quiet
```
Expected: 成功，0错误

**Step 3: 添加权限检查到 Update 方法**

```csharp
// In MedicalCaseCommandService.cs UpdateFromInputAsync method
// After existing status checks, add:

// I-2: 锁定检查 - 仅限制Doctor，Admin/SuperAdmin不受限
if (existing.CaseStatus == MedicalCaseStatus.Completed)
{
    var isLocked = existing.CompletedAt?.Date < DateTime.Today;
    var currentUserRole = GetCurrentUserRole(); // 从context获取

    if (isLocked && currentUserRole == UserRole.Doctor)
    {
        throw new InvalidOperationException("ERR-30201: 该医案已锁定(隔天)，无法编辑");
    }

    // 所有已完成医案编辑需要EditReason
    if (string.IsNullOrWhiteSpace(request.EditReason))
    {
        throw new ValidationException("编辑已完成医案必须提供编辑原因");
    }
}
```

**Step 4: 编写 IsLocked 计算测试**

```csharp
[Fact]
public async Task I2_CompletedCase_SameDay_NotLocked()
{
    // Arrange - 创建并完成医案
    var doctorClient = await LoginAsDoctorAsync();
    var patientId = await CreatePatientAsync(doctorClient);
    var caseId = await CreateCompleteCaseAsync(doctorClient, patientId);

    // Act - 获取医案详情
    var response = await doctorClient.GetAsync($"/api/v1/medicalcases/{caseId}");

    // Assert - 当天不应锁定
    var data = await response.ShouldBeSuccessWithDataAsync<MedicalCaseDetailDto>();
    data.CaseStatus.Should().Be(MedicalCaseStatus.Completed);
    data.IsLocked.Should().BeFalse("当天完成的医案不应锁定");
}
```

**Step 5: 运行测试验证失败**

```bash
dotnet test tests/LYBT.Tests.Server/ --filter "FullyQualifiedName~I2_CompletedCase_SameDay_NotLocked" -v n
```
Expected: 编译成功，测试通过(IsLocked计算正确)

**Step 6: 编写 Doctor 隔天锁定测试**

```csharp
[Fact(Skip = "需要模拟时间，暂不实现")]
public async Task I2_CompletedCase_NextDay_LockedForDoctor()
{
    // 测试隔天Doctor无法编辑
}
```

**Step 7: 提交**

```bash
git add src/Shared/LYBT.Shared.Models/Contracts/MedicalCase/MedicalCaseDetailDto.cs
git add src/Server/Modules/LYBT.Module.MedicalCase/Services/MedicalCaseCommandService.cs
git add tests/LYBT.Tests.Server/Features/US_MedicalCase_MustHaveTests.cs
git commit -m "feat(permission): I-2 MedicalCase IsLocked calculation for same-day editing"
```

---

## Task 2: G-9 Registration 回退后流程

**Files:**
- Modify: `src/Server/Modules/LYBT.Module.MedicalCase/Services/MedicalCaseCommandService.cs`
- Create: `src/Server/Modules/LYBT.Module.Registration/Enums/RegistrationSource.cs` (if not exists)
- Modify: `src/Server/Modules/LYBT.Module.Registration/Interfaces/IRegistrationRepository.cs`
- Modify: `src/Server/Modules/LYBT.Module.Registration/Repositories/RegistrationRepository.cs`
- Test: `tests/LYBT.Tests.Server/Features/US_Registration_MustHaveTests.cs` (create if not exists)

**Step 1: 检查 RegistrationSource 枚举是否存在**

```bash
grep -r "RegistrationSource" src/Server/Modules/LYBT.Module.Registration/
```

If exists, skip to Step 3. Otherwise continue Step 2.

**Step 2: 创建 RegistrationSource 枚举 (如需要)**

```csharp
// src/Server/Modules/LYBT.Module.Registration/Enums/RegistrationSource.cs
namespace LYBT.Module.Registration.Enums;

public enum RegistrationSource
{
    Receptionist = 0,  // 前台挂号
    Doctor = 1         // 医生快速看诊
}
```

**Step 3: 修改 Cancel 方法实现回退逻辑**

```csharp
// In MedicalCaseCommandService.cs CancelAsync method
// Replace existing logic with:

// G-9: 根据Registration.Source分流处理回退
var registration = await _registrationRepository.GetByMedicalCaseIdAsync(id);
if (registration != null)
{
    if (registration.Source == RegistrationSource.Receptionist)
    {
        // 前台挂号: 回退到Waiting，保留MedicalCaseId用于恢复
        registration.Status = RegistrationStatus.Waiting;
        registration.UpdatedAt = DateTime.UtcNow;
        // MedicalCaseId 保留不清空!
        await _registrationRepository.UpdateAsync(registration);
        _logger.LogInformation("[SVC] MedicalCase.Cancel -> Registration rolled back to Waiting - RegistrationId={RegistrationId}",
            registration.Id);
    }
    else if (registration.Source == RegistrationSource.Doctor)
    {
        // 医生快速看诊: 自动Cancelled闭环
        registration.Status = RegistrationStatus.Cancelled;
        registration.UpdatedAt = DateTime.UtcNow;
        await _registrationRepository.UpdateAsync(registration);
        _logger.LogInformation("[SVC] MedicalCase.Cancel -> Registration auto-closed - RegistrationId={RegistrationId}",
            registration.Id);
    }
}
```

**Step 4: 添加 GetByMedicalCaseIdAsync 到 Repository**

```csharp
// In IRegistrationRepository.cs
Task<Registration?> GetByMedicalCaseIdAsync(Guid medicalCaseId, CancellationToken ct = default);

// In RegistrationRepository.cs
public async Task<Registration?> GetByMedicalCaseIdAsync(Guid medicalCaseId, CancellationToken ct = default)
{
    return await _context.Registrations
        .IgnoreQueryFilters() // 包含软删除，用于取消时查找
        .FirstOrDefaultAsync(r => r.MedicalCaseId == medicalCaseId && !r.IsDeleted, ct);
}
```

**Step 5: 编写 Receptionist 来源回退测试**

```csharp
[Fact]
public async Task G9_CancelMedicalCase_FromReceptionist_RollbackToWaiting()
{
    // Arrange - 前台挂号 -> 创建医案 -> 取消医案
    var receptionistClient = await LoginAsReceptionistAsync();
    var doctorClient = await LoginAsDoctorAsync();
    var patientId = await CreatePatientAsync(doctorClient);
    var doctorId = await GetDoctorUserIdAsync(await LoginAsAdminAsync());

    // 前台挂号
    var regPayload = new { PatientId = patientId, DoctorId = doctorId, RegistrationType = 0 };
    var regResponse = await receptionistClient.PostAsJsonAsync("/api/v1/registrations", regPayload);
    var regId = await GetIdFromResponseAsync(regResponse);

    // 医生接诊并创建医案
    // ... 接诊流程

    // 取消医案
    var cancelPayload = new { Reason = "患者要求取消" };
    await doctorClient.PutAsJsonAsync($"/api/v1/medicalcases/{caseId}/cancel", cancelPayload);

    // Assert - Registration应回退到Waiting，MedicalCaseId保留
    var getRegResp = await receptionistClient.GetAsync($"/api/v1/registrations/{regId}");
    var regDetail = await getRegResp.ShouldBeSuccessWithDataAsync<RegistrationDetailDto>();
    regDetail.Status.Should().Be(RegistrationStatus.Waiting);
    regDetail.MedicalCaseId.Should().Be(caseId); // 保留!
}
```

**Step 6: 运行测试验证**

```bash
dotnet test tests/LYBT.Tests.Server/ --filter "FullyQualifiedName~G9_CancelMedicalCase" -v n
```

**Step 7: 提交**

```bash
git add src/Server/Modules/LYBT.Module.Registration/
git add src/Server/Modules/LYBT.Module.MedicalCase/Services/MedicalCaseCommandService.cs
git add tests/LYBT.Tests.Server/Features/US_Registration_MustHaveTests.cs
git commit -m "feat(permission): G-9 Registration rollback flow after MedicalCase cancellation"
```

---

## Task 3: G-11 医生禁用前 Waiting 挂号检查

**Files:**
- Modify: `src/Server/Modules/LYBT.Module.User/Services/UserCommandService.cs`
- Modify: `src/Server/Modules/LYBT.Module.Registration/Interfaces/IRegistrationRepository.cs`
- Modify: `src/Server/Modules/LYBT.Module.Registration/Repositories/RegistrationRepository.cs`
- Test: `tests/LYBT.Tests.Server/Features/US_User_MustHaveTests.cs` (add test)

**Step 1: 添加 GetWaitingCountByDoctorAsync 到 Repository**

```csharp
// In IRegistrationRepository.cs
Task<int> GetWaitingCountByDoctorAsync(Guid doctorId, CancellationToken ct = default);

// In RegistrationRepository.cs
public async Task<int> GetWaitingCountByDoctorAsync(Guid doctorId, CancellationToken ct = default)
{
    return await _context.Registrations
        .CountAsync(r => r.DoctorId == doctorId &&
                         r.Status == RegistrationStatus.Waiting &&
                         !r.IsDeleted, ct);
}
```

**Step 2: 修改 User 禁用方法添加前置校验**

```csharp
// In UserCommandService.cs ToggleStatusAsync or DisableUserAsync method
// Before toggling to Disabled:

if (user.Role == UserRole.Doctor && !user.IsActive) // 即将禁用
{
    // G-11: 检查是否有Waiting状态的挂号
    var waitingCount = await _registrationRepository.GetWaitingCountByDoctorAsync(user.Id);
    if (waitingCount > 0)
    {
        throw new ValidationException($"该医生有 {waitingCount} 条等待中的挂号记录，请先由前台取消后再禁用");
    }
}
```

**Step 3: 更新 Service 依赖注入**

```csharp
// In UserCommandService.cs constructor
private readonly IRegistrationRepository _registrationRepository;

public UserCommandService(
    IUserRepository repository,
    IRegistrationRepository registrationRepository, // 新增
    // ... other deps
)
{
    _repository = repository;
    _registrationRepository = registrationRepository;
    // ...
}
```

**Step 4: 编写禁用前置校验测试**

```csharp
[Fact]
public async Task G11_DisableDoctor_WithWaitingRegistration_Returns422()
{
    // Arrange - 创建医生、患者、挂号
    var adminClient = await LoginAsAdminAsync();
    var doctorClient = await LoginAsDoctorAsync();
    var doctorId = await GetDoctorUserIdAsync(adminClient);

    var receptionistClient = await LoginAsReceptionistAsync();
    var patientId = await CreatePatientAsync(doctorClient);

    // 前台挂号到该医生
    var regPayload = new { PatientId = patientId, DoctorId = doctorId, RegistrationType = 0 };
    await receptionistClient.PostAsJsonAsync("/api/v1/registrations", regPayload);

    // Act - 尝试禁用医生
    var response = await adminClient.PutAsJsonAsync($"/api/v1/users/{doctorId}/toggle-status", new { });

    // Assert - 应返回422
    response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    var content = await response.Content.ReadAsStringAsync();
    content.Should().Contain("等待中的挂号");
}

[Fact]
public async Task G11_DisableDoctor_NoWaitingRegistration_Succeeds()
{
    // Arrange - 创建医生，无挂号
    var adminClient = await LoginAsAdminAsync();
    var doctorId = await CreateDoctorUserAsync(adminClient); // helper创建新医生

    // Act - 禁用医生
    var response = await adminClient.PutAsJsonAsync($"/api/v1/users/{doctorId}/toggle-status", new { });

    // Assert - 应成功
    response.StatusCode.Should().Be(HttpStatusCode.OK);
}
```

**Step 5: 运行测试验证**

```bash
dotnet test tests/LYBT.Tests.Server/ --filter "FullyQualifiedName~G11_DisableDoctor" -v n
```

**Step 6: 提交**

```bash
git add src/Server/Modules/LYBT.Module.User/Services/UserCommandService.cs
git add src/Server/Modules/LYBT.Module.Registration/
git add tests/LYBT.Tests.Server/Features/US_User_MustHaveTests.cs
git commit -m "feat(permission): G-11 prevent disabling doctor with waiting registrations"
```

---

## Task 4: 全量测试运行

**Step 1: 运行相关模块测试**

```bash
# MedicalCase 测试
dotnet test tests/LYBT.Tests.Server/ --filter "FullyQualifiedName~MedicalCase" -v n

# Registration 测试
dotnet test tests/LYBT.Tests.Server/ --filter "FullyQualifiedName~Registration" -v n

# User 测试
dotnet test tests/LYBT.Tests.Server/ --filter "FullyQualifiedName~User" -v n
```

**Step 2: 运行全量测试**

```bash
dotnet test tests/LYBT.Tests.Server/ -v n --no-build
```

Expected: 现有测试全部通过，新增测试通过

**Step 3: 提交测试更新**

```bash
git add tests/
git commit -m "test(permission): add tests for I-2, G-9, G-11 permission matrix fixes"
```

---

## Summary

| Task | 组件 | 关键变更 |
|------|------|---------|
| I-2 | MedicalCase | IsLocked 动态计算 + 角色分层权限检查 |
| G-9 | Registration + MedicalCase | 取消分流: Receptionist回退/Doctor闭环 |
| G-11 | User + Registration | 禁用前置校验: 检查Waiting挂号数量 |

---

## References

- Design: `docs/plans/2026-03-10-permission-matrix-defect-remediation-design.md`
- Matrix: `docs/02-requirements/role-permission-matrix.md`
- PRD MedicalCase: `docs/02-requirements/medical-cases.md`
- PRD Registration: `docs/02-requirements/registration.md`

