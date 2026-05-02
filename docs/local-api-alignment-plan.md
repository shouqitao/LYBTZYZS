# 本地API对齐实现计划

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 对齐本地Refit接口与远程Refit接口的方法签名、参数和返回类型，消除离线模式下的行为差异。

**Architecture:** 按优先级分3个阶段：P0修复功能正确性问题、P1统一批量操作参数、P2补充缺失方法。每阶段独立可测试。

**Tech Stack:** C# / .NET 8 / Refit / ASP.NET Core Minimal APIs

---

## 阶段一：P0 — 修复功能正确性问题

### Task 1: 修复 ILocalUserApi.ResetPasswordAsync 缺少参数

**问题:** 远程接口需要 `ResetPasswordRequestDto`，本地接口缺少此参数，导致离线模式下无法传递重置密码信息。

**Files:**
- Modify: `src\Client\Desktop\Core\LYBT.Desktop.Contracts\Api\ILocalUserApi.cs`
- Modify: `src\Client\Desktop\LocalWebAPI\Controllers\UsersController.cs`
- Modify: `src\Client\Desktop\Modules\LYBT.Desktop.Users\Repositories\UserRepository.cs`

- [x] **Step 1: 修改 ILocalUserApi.ResetPasswordAsync 签名**

```csharp
// 修改前:
[Refit.Post("/api/users/{id}/reset-password")]
Task<ResetPasswordResponseDto> ResetPasswordAsync(Guid id);

// 修改后:
[Refit.Post("/api/users/{id}/reset-password")]
Task<ResetPasswordResponseDto> ResetPasswordAsync(Guid id, [Refit.Body] ResetPasswordRequestDto request);
```

- [x] **Step 2: 修改 LocalWebAPI UsersController.ResetPassword 接收参数**

```csharp
// 修改前:
[HttpPost("{id:guid}/reset-password")]
public async Task<IActionResult> ResetPassword(Guid id)

// 修改后:
[HttpPost("{id:guid}/reset-password")]
public async Task<IActionResult> ResetPassword(Guid id, [FromBody] ResetPasswordRequestDto request)
```

- [x] **Step 3: 修改 UserRepository 调用传递参数**

在 `UserRepository.cs` 中找到 `ResetPasswordAsync` 的离线分支，确保传递 `request` 参数。

- [x] **Step 4: 编译验证**

Run: `dotnet build LYBT.Desktop.sln`
Expected: 0 errors

---

### Task 2: 对齐 ILocalAuthApi 方法签名

**问题:** Auth模块方法名、参数、返回类型全面不一致。Repository层需要适配。

**Files:**
- Modify: `src\Client\Desktop\Core\LYBT.Desktop.Contracts\Api\ILocalAuthApi.cs`
- Modify: `src\Client\Desktop\LocalWebAPI\Controllers\AuthController.cs`

- [x] **Step 1: 统一 ILocalAuthApi 方法签名**

```csharp
// 修改后:
public interface ILocalAuthApi
{
    [Refit.Post("/api/auth/login")]
    Task<LoginResponse> LoginAsync([Refit.Body] LoginRequest request);

    [Refit.Post("/api/auth/auto-login")]
    Task<LoginResponse> AutoLoginAsync([Refit.Body] AutoLoginRequest request);

    [Refit.Post("/api/auth/logout")]
    Task LogoutAsync([Refit.Body] LogoutRequest request);

    [Refit.Post("/api/auth/refresh")]
    Task<LoginResponse> RefreshAsync([Refit.Body] RefreshTokenRequest request);

    [Refit.Get("/api/auth/validate")]
    Task<ValidateTokenResponse> ValidateTokenAsync([Refit.Body] ValidateTokenRequest request);

    [Refit.Get("/api/auth/validate")]
    Task<object> ValidateTokenFromHeaderAsync();
}
```

- [x] **Step 2: 修改 LocalWebAPI AuthController 接收参数**

确保 `Logout` 和 `Refresh` 端点接收正确的请求体。

- [x] **Step 3: 编译验证**

Run: `dotnet build LYBT.Desktop.sln`
Expected: 0 errors

---

### Task 3: 对齐 MedicalCases Suspend/Cancel 方法

**问题:** 方法名不同，且本地缺少可选的 request 参数。

**Files:**
- Modify: `src\Client\Desktop\Core\LYBT.Desktop.Contracts\Api\ILocalMedicalCaseApi.cs`
- Modify: `src\Client\Desktop\LocalWebAPI\Controllers\MedicalCasesController.cs`

- [x] **Step 1: 统一方法名和参数**

```csharp
// 修改前:
[Refit.Put("/api/medicalcases/{id}/suspend")]
Task<MedicalCaseDetailDto> SuspendCaseAsync(Guid id);

[Refit.Put("/api/medicalcases/{id}/cancel")]
Task CancelCaseAsync(Guid id);

// 修改后:
[Refit.Put("/api/medicalcases/{id}/suspend")]
Task<MedicalCaseDetailDto> SuspendAsync(Guid id, [Refit.Body] ConsultationInputDto? request = null);

[Refit.Put("/api/medicalcases/{id}/cancel")]
Task CancelMedicalCaseAsync(Guid id, [Refit.Body] CancelMedicalCaseRequestDto? request = null);
```

- [x] **Step 2: 修改 LocalWebAPI MedicalCasesController**

确保 `Suspend` 和 `Cancel` 端点接收可选的请求体。

- [x] **Step 3: 修改 MedicalCaseRepository 调用**

更新 Repository 中的方法调用以匹配新签名。

- [x] **Step 4: 编译验证**

Run: `dotnet build LYBT.Desktop.sln`
Expected: 0 errors

---

### Task 4: 对齐 MedicalCases GetMedicalCases/Query/Search 方法名

**问题:** 方法名不一致，影响 Repository 层代码可读性。

**Files:**
- Modify: `src\Client\Desktop\Core\LYBT.Desktop.Contracts\Api\ILocalMedicalCaseApi.cs`

- [x] **Step 1: 统一方法名**

```csharp
// 修改前:
Task<List<MedicalCaseListDto>> GetMedicalCasesAsync([Refit.Query] Guid? patientId = null);
Task<PagedResult<MedicalCaseListDto>> QueryAsync(...);
Task<PagedResult<MedicalCaseDetailDto>> SearchAsync(...);

// 修改后:
Task<List<MedicalCaseListDto>> GetMedicalCasesAsync([Refit.Query] Guid? patientId = null);
Task<PagedResult<MedicalCaseListDto>> QueryMedicalCasesAsync(...);  // 与远程一致
Task<PagedResult<MedicalCaseDetailDto>> SearchMedicalCasesAsync(...);  // 与远程一致
```

- [x] **Step 2: 更新 Repository 中的方法调用**

- [x] **Step 3: 编译验证**

Run: `dotnet build LYBT.Desktop.sln`
Expected: 0 errors

---

## 阶段二：P1 — 统一批量操作参数

### Task 5: 统一 Users 批量操作参数

**问题:** 远程用 `BatchDeleteInputDto`，本地用 `List<Guid>`。Repository 需要适配两种类型。

**Files:**
- Modify: `src\Client\Desktop\Core\LYBT.Desktop.Contracts\Api\ILocalUserApi.cs`
- Modify: `src\Client\Desktop\LocalWebAPI\Controllers\UsersController.cs`

- [x] **Step 1: 修改 ILocalUserApi 批量方法参数**

```csharp
// 修改前:
Task<BatchOperationResultDto> BatchDeleteAsync([Refit.Body] List<Guid> ids);
Task<BatchOperationResultDto> BatchEnableAsync([Refit.Body] List<Guid> ids);
Task<BatchOperationResultDto> BatchDisableAsync([Refit.Body] List<Guid> ids);

// 修改后:
Task<BatchOperationResultDto> BatchDeleteAsync([Refit.Body] BatchDeleteInputDto request);
Task<BatchOperationResultDto> BatchEnableAsync([Refit.Body] BatchDeleteInputDto request);
Task<BatchOperationResultDto> BatchDisableAsync([Refit.Body] BatchDeleteInputDto request);
```

- [x] **Step 2: 修改 LocalWebAPI UsersController 接收 BatchDeleteInputDto**

```csharp
// 修改前:
[HttpPost("batch-delete")]
public async Task<IActionResult> BatchDelete([FromBody] List<Guid> ids)

// 修改后:
[HttpPost("batch-delete")]
public async Task<IActionResult> BatchDelete([FromBody] BatchDeleteInputDto request)
```

- [x] **Step 3: 编译验证**

Run: `dotnet build LYBT.Desktop.sln`
Expected: 0 errors

---

### Task 6: 统一 Patients 批量操作参数

**Files:**
- Modify: `src\Client\Desktop\Core\LYBT.Desktop.Contracts\Api\ILocalPatientApi.cs`
- Modify: `src\Client\Desktop\LocalWebAPI\Controllers\PatientsController.cs`

- [x] **Step 1: 修改 ILocalPatientApi.BatchDeleteAsync 参数**

```csharp
// 修改前:
Task<BatchOperationResultDto> BatchDeleteAsync([Refit.Body] List<Guid> ids);

// 修改后:
Task<BatchOperationResultDto> BatchDeleteAsync([Refit.Body] BatchDeleteInputDto request);
```

- [x] **Step 2: 修改 LocalWebAPI PatientsController**

- [x] **Step 3: 编译验证**

Run: `dotnet build LYBT.Desktop.sln`
Expected: 0 errors

---

### Task 7: 统一 Herbs 批量操作参数

**Files:**
- Modify: `src\Client\Desktop\Core\LYBT.Desktop.Contracts\Api\ILocalHerbApi.cs`
- Modify: `src\Client\Desktop\LocalWebAPI\Controllers\HerbsController.cs`

- [x] **Step 1: 修改 ILocalHerbApi 批量方法参数**

```csharp
// 修改前:
Task<BatchOperationResultDto> BatchDeleteAsync([Refit.Body] List<Guid> ids);
Task<BatchOperationResultDto> BatchEnableAsync([Refit.Body] List<Guid> ids);
Task<BatchOperationResultDto> BatchDisableAsync([Refit.Body] List<Guid> ids);

// 修改后:
Task<BatchOperationResultDto> BatchDeleteAsync([Refit.Body] BatchDeleteInputDto request);
Task<BatchOperationResultDto> BatchEnableAsync([Refit.Body] BatchDeleteInputDto request);
Task<BatchOperationResultDto> BatchDisableAsync([Refit.Body] BatchDeleteInputDto request);
```

- [x] **Step 2: 修改 LocalWebAPI HerbsController**

- [x] **Step 3: 编译验证**

Run: `dotnet build LYBT.Desktop.sln`
Expected: 0 errors

---

### Task 8: 统一 Formulas 批量操作参数

**Files:**
- Modify: `src\Client\Desktop\Core\LYBT.Desktop.Contracts\Api\ILocalFormulaApi.cs`
- Modify: `src\Client\Desktop\LocalWebAPI\Controllers\FormulasController.cs`

- [x] **Step 1: 修改 ILocalFormulaApi 批量方法参数**

```csharp
// 修改前:
Task<BatchOperationResultDto> BatchDeleteAsync([Refit.Body] List<Guid> ids);
Task<BatchOperationResultDto> BatchEnableAsync([Refit.Body] List<Guid> ids);
Task<BatchOperationResultDto> BatchDisableAsync([Refit.Body] List<Guid> ids);

// 修改后:
Task<BatchOperationResultDto> BatchDeleteAsync([Refit.Body] BatchDeleteInputDto request);
Task<BatchOperationResultDto> BatchEnableAsync([Refit.Body] BatchDeleteInputDto request);
Task<BatchOperationResultDto> BatchDisableAsync([Refit.Body] BatchDeleteInputDto request);
```

- [x] **Step 2: 修改 LocalWebAPI FormulasController**

- [x] **Step 3: 编译验证**

Run: `dotnet build LYBT.Desktop.sln`
Expected: 0 errors

---

### Task 9: 统一 MedicalCases 批量操作参数

**Files:**
- Modify: `src\Client\Desktop\Core\LYBT.Desktop.Contracts\Api\ILocalMedicalCaseApi.cs`
- Modify: `src\Client\Desktop\LocalWebAPI\Controllers\MedicalCasesController.cs`

- [x] **Step 1: 修改 ILocalMedicalCaseApi 批量方法参数**

```csharp
// 修改前:
Task<List<MedicalCaseDetailDto>> GetBatchDetailsAsync([Refit.Body] List<Guid> ids);
Task<BatchOperationResultDto> BatchDeleteAsync([Refit.Body] List<Guid> ids);

// 修改后:
Task<List<MedicalCaseDetailDto>> GetBatchDetailsAsync([Refit.Body] BatchDetailQueryDto request);
Task<BatchOperationResultDto> BatchDeleteAsync([Refit.Body] BatchDeleteInputDto request);
```

- [x] **Step 2: 修改 LocalWebAPI MedicalCasesController**

- [x] **Step 3: 编译验证**

Run: `dotnet build LYBT.Desktop.sln`
Expected: 0 errors

---

## 阶段三：P2 — 补充缺失方法

### Task 10: 添加 Herbs/Formulas 分类过滤参数

**问题:** 本地接口缺少 `category` 过滤参数。

**Files:**
- Modify: `src\Client\Desktop\Core\LYBT.Desktop.Contracts\Api\ILocalHerbApi.cs`
- Modify: `src\Client\Desktop\Core\LYBT.Desktop.Contracts\Api\ILocalFormulaApi.cs`
- Modify: `src\Client\Desktop\LocalWebAPI\Controllers\HerbsController.cs`
- Modify: `src\Client\Desktop\LocalWebAPI\Controllers\FormulasController.cs`

- [x] **Step 1: 添加 category 参数到 ILocalHerbApi**

```csharp
// 修改前:
[Refit.Get("/api/herbs")]
Task<List<HerbListDto>> GetHerbsAsync([Refit.Query] string? keyword = null);

// 修改后:
[Refit.Get("/api/herbs")]
Task<List<HerbListDto>> GetHerbsAsync(
    [Refit.Query] string? keyword = null,
    [Refit.Query] string? category = null);
```

- [x] **Step 2: 添加 category 参数到 ILocalFormulaApi**

```csharp
// 修改前:
[Refit.Get("/api/formulas")]
Task<List<FormulaListDto>> GetFormulasAsync([Refit.Query] string? keyword = null);

// 修改后:
[Refit.Get("/api/formulas")]
Task<List<FormulaListDto>> GetFormulasAsync(
    [Refit.Query] string? keyword = null,
    [Refit.Query] string? category = null);
```

- [x] **Step 3: 修改 LocalWebAPI Controllers 接收 category 参数**

- [x] **Step 4: 编译验证**

Run: `dotnet build LYBT.Desktop.sln`
Expected: 0 errors

---

### Task 11: 添加 Herbs/Formulas categories 端点

**问题:** 远程有获取分类列表的端点，本地缺失。

**Files:**
- Modify: `src\Client\Desktop\Core\LYBT.Desktop.Contracts\Api\ILocalHerbApi.cs`
- Modify: `src\Client\Desktop\Core\LYBT.Desktop.Contracts\Api\ILocalFormulaApi.cs`
- Modify: `src\Client\Desktop\LocalWebAPI\Controllers\HerbsController.cs`
- Modify: `src\Client\Desktop\LocalWebAPI\Controllers\FormulasController.cs`

- [x] **Step 1: 添加 GetCategoriesAsync 到 ILocalHerbApi**

```csharp
[Refit.Get("/api/herbs/categories")]
Task<List<string>> GetCategoriesAsync();
```

- [x] **Step 2: 添加 GetCategoriesAsync 到 ILocalFormulaApi**

```csharp
[Refit.Get("/api/formulas/categories")]
Task<List<string>> GetCategoriesAsync();
```

- [x] **Step 3: 实现 LocalWebAPI Controllers 端点**

- [x] **Step 4: 编译验证**

Run: `dotnet build LYBT.Desktop.sln`
Expected: 0 errors

---

### Task 12: 添加 Patients toggle-status 端点到远程 Refit

**问题:** 本地有 `ToggleStatusAsync`，但远程 Refit 接口 `IPatientApi` 缺失（Controller 已有）。

**Files:**
- Modify: `src\Client\Desktop\Core\LYBT.Desktop.Contracts\Api\IPatientApi.cs`

- [x] **Step 1: 添加 ToggleStatusAsync 到 IPatientApi**

```csharp
/// <summary>
/// 切换患者状态（启用/禁用）
/// </summary>
[Refit.Post("/api/v1/patients/{id}/toggle-status")]
Task<ApiResponse<PatientDetailDto>> ToggleStatusAsync(Guid id);
```

- [x] **Step 2: 编译验证**

Run: `dotnet build LYBT.Desktop.sln`
Expected: 0 errors

---

### Task 13: 添加 MedicalCases 缺失端点 (GetPendingCases, GetAuditLogs, AddPrintLog)

**问题:** 远程有3个端点本地缺失。

**Files:**
- Modify: `src\Client\Desktop\Core\LYBT.Desktop.Contracts\Api\ILocalMedicalCaseApi.cs`
- Modify: `src\Client\Desktop\LocalWebAPI\Controllers\MedicalCasesController.cs`

- [x] **Step 1: 添加 GetPendingCasesAsync 到 ILocalMedicalCaseApi**

```csharp
[Refit.Get("/api/medicalcases/pending")]
Task<List<PendingMedicalCaseDto>> GetPendingCasesAsync([Refit.Query] Guid? patientId = null);
```

- [x] **Step 2: 添加 GetAuditLogsAsync 到 ILocalMedicalCaseApi**

```csharp
[Refit.Get("/api/medicalcases/{id}/audit-logs")]
Task<MedicalCaseAuditLogPagedResultDto> GetAuditLogsAsync(
    Guid id,
    [Refit.Query] int page = 1,
    [Refit.Query] int pageSize = 20);
```

- [x] **Step 3: 添加 AddPrintLogAsync 到 ILocalMedicalCaseApi**

```csharp
[Refit.Post("/api/medicalcases/{id}/print-logs")]
Task<object> AddPrintLogAsync(Guid id, [Refit.Body] PrintLogInputDto request);
```

- [x] **Step 4: 实现 LocalWebAPI MedicalCasesController 端点**

- [x] **Step 5: 编译验证**

Run: `dotnet build LYBT.Desktop.sln`
Expected: 0 errors

---

### Task 14: 添加 Registrations quick-visit 端点到本地

**问题:** 远程有 `QuickVisit` 端点，本地缺失。

**Files:**
- Modify: `src\Client\Desktop\Core\LYBT.Desktop.Contracts\Api\ILocalRegistrationApi.cs`
- Modify: `src\Client\Desktop\LocalWebAPI\Controllers\RegistrationsController.cs`

- [x] **Step 1: 添加 QuickVisitAsync 到 ILocalRegistrationApi**

```csharp
[Refit.Post("/api/registrations/quick-visit")]
Task<RegistrationDetailDto> QuickVisitAsync([Refit.Body] QuickVisitRequestDto request);
```

- [x] **Step 2: 实现 LocalWebAPI RegistrationsController 端点**

- [x] **Step 3: 编译验证**

Run: `dotnet build LYBT.Desktop.sln`
Expected: 0 errors

---

## 阶段四：验证

### Task 15: 全量编译验证

- [x] **Step 1: 编译完整解决方案**

Run: `dotnet build LYBTZYZS.sln`
Expected: 0 errors, 0 warnings

- [ ] **Step 2: 运行测试**

Run: `dotnet test LYBTZYZS.sln --filter "FullyQualifiedName~LYBT.Tests"`
Expected: 所有测试通过（E2E失败为预期内）

---

## 总结

| 阶段 | 任务数 | 优先级 | 说明 |
|------|--------|--------|------|
| 一 | 4 | P0 | 修复功能正确性（ResetPassword参数、Auth对齐、MedicalCases方法名） |
| 二 | 5 | P1 | 统一批量操作参数（5个模块的BatchDelete/Enable/Disable） |
| 三 | 5 | P2 | 补充缺失方法（categories、toggle-status、pending/audit/print-log、quick-visit） |
| 四 | 1 | 验证 | 全量编译+测试 |
| **合计** | **15** | | |
