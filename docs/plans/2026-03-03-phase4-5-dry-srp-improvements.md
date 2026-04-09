# Phase 4-5: DRY + API 一致性改进 实施计划

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** 提取魔法常量到集中常量类、统一 HTTP 状态码用法、标准化日志级别

**Architecture:** 在 LYBT.Infrastructure 和 LYBT.Shared.Configuration 中创建常量类，替换 Server 层中分散的硬编码字符串/数值。同时修复 2 处 HTTP 状态码绕过和 15 处日志级别不当。

**Tech Stack:** C# 12, ASP.NET Core 8, Serilog

---

## 范围决策

### 执行项

| Task | 内容 | 风险 | 预估 |
|------|------|------|------|
| 1 | 创建 RoleConstants + PolicyConstants | 低 | 10 min |
| 2 | 创建 HttpHeaderConstants | 低 | 5 min |
| 3 | 替换 Server 层魔法常量引用 | 低 | 15 min |
| 4 | HTTP 状态码一致性修复 (2 文件) | 低 | 5 min |
| 5 | 日志级别标准化 (7 文件, 15 处) | 低 | 15 min |

### 跳过项 (YAGNI)

| Task | 原因 |
|------|------|
| Task 4.2: Guard 工具类 | 70+ 处 null 检查模式各异 (return null / throw BusinessException / return Result.Failure)，统一 Guard 类反而增加复杂度。.NET 8 已内置 `ArgumentNullException.ThrowIfNull()`。YAGNI。 |
| Task 5.1: ViewModel Handler 提取 | ViewModel 第 52 行注释: `MedicalCaseNavigationHandler已删除，逻辑内联到ViewModel`。导航逻辑与 ViewModel 状态耦合过深，前期已尝试提取并回退。当前 1,275 行虽超标但已通过 3 个 Handler 分解了 1,197 行功能，核心协调逻辑无法进一步无损提取。 |

---

## Task 1: 创建 RoleConstants + PolicyConstants

**Files:**
- Create: `src/Server/Core/LYBT.Infrastructure/Constants/RoleConstants.cs`
- Create: `src/Server/Core/LYBT.Infrastructure/Constants/PolicyConstants.cs`

### Step 1: 创建 RoleConstants.cs

```csharp
// src/Server/Core/LYBT.Infrastructure/Constants/RoleConstants.cs
namespace LYBT.Infrastructure.Constants;

/// <summary>
/// 系统角色名称常量
/// </summary>
public static class RoleConstants
{
    public const string SuperAdmin = "SuperAdmin";
    public const string Admin = "Admin";
    public const string Doctor = "Doctor";
    public const string Receptionist = "Receptionist";

    /// <summary>
    /// SuperAdmin 的 userType 小写形式 (用于 JWT Claims)
    /// </summary>
    public const string SuperAdminUserType = "superadmin";

    /// <summary>
    /// 默认 userType
    /// </summary>
    public const string DefaultUserType = "user";
}
```

### Step 2: 创建 PolicyConstants.cs

```csharp
// src/Server/Core/LYBT.Infrastructure/Constants/PolicyConstants.cs
namespace LYBT.Infrastructure.Constants;

/// <summary>
/// 授权策略名称常量
/// </summary>
public static class PolicyConstants
{
    public const string AdminOnly = "AdminOnly";
    public const string DoctorOrAdmin = "DoctorOrAdmin";
    public const string PatientAccess = "PatientAccess";
    public const string SuperAdminOnly = "SuperAdminOnly";
}
```

### Step 3: 验证编译

Run: `dotnet build src/Server/Core/LYBT.Infrastructure/LYBT.Infrastructure.csproj`
Expected: 0 errors

---

## Task 2: 创建 HttpHeaderConstants

**Files:**
- Create: `src/Server/Core/LYBT.Infrastructure/Constants/HttpHeaderConstants.cs`

### Step 1: 创建 HttpHeaderConstants.cs

```csharp
// src/Server/Core/LYBT.Infrastructure/Constants/HttpHeaderConstants.cs
namespace LYBT.Infrastructure.Constants;

/// <summary>
/// HTTP 头部和认证相关常量
/// </summary>
public static class HttpHeaderConstants
{
    public const string CorrelationId = "X-Correlation-ID";
    public const string Traceparent = "traceparent";
    public const string CorrelationIdItemKey = "CorrelationId";
    public const string TraceIdKey = "traceId";
    public const string BearerScheme = "Bearer";
}
```

### Step 2: 验证编译

Run: `dotnet build src/Server/Core/LYBT.Infrastructure/LYBT.Infrastructure.csproj`
Expected: 0 errors

---

## Task 3: 替换 Server 层魔法常量引用

**Files:**
- Modify: `src/Server/Services/LYBT.WebAPI/Extensions/AuthenticationServiceCollectionExtensions.cs`
- Modify: `src/Server/Services/LYBT.WebAPI/Controllers/DiagnosticsController.cs`
- Modify: `src/Server/Services/LYBT.WebAPI/Controllers/MedicalCaseController.cs`
- Modify: `src/Server/Services/LYBT.WebAPI/Controllers/PatientsController.cs`
- Modify: `src/Server/Services/LYBT.WebAPI/Controllers/UsersController.cs`
- Modify: `src/Server/Services/LYBT.WebAPI/Controllers/FormulasController.cs`
- Modify: `src/Server/Services/LYBT.WebAPI/Controllers/HerbsController.cs`
- Modify: `src/Server/Services/LYBT.WebAPI/Controllers/SyncController.cs`
- Modify: `src/Server/Core/LYBT.Infrastructure/Web/BaseApiController.cs`
- Modify: `src/Server/Core/LYBT.Infrastructure/Services/BaseService.cs`
- Modify: `src/Server/Services/LYBT.WebAPI/Middleware/CorrelationIdMiddleware.cs`
- Modify: `src/Server/Services/LYBT.WebAPI/Middleware/ProblemDetailsConfiguration.cs`

### Step 1: 替换 AuthenticationServiceCollectionExtensions.cs 中的角色和策略字符串

替换规则:
```
"SuperAdmin"    → RoleConstants.SuperAdmin
"Admin"         → RoleConstants.Admin
"Doctor"        → RoleConstants.Doctor
"Receptionist"  → RoleConstants.Receptionist
"AdminOnly"     → PolicyConstants.AdminOnly
"DoctorOrAdmin" → PolicyConstants.DoctorOrAdmin
"PatientAccess" → PolicyConstants.PatientAccess
"SuperAdminOnly"→ PolicyConstants.SuperAdminOnly
```

添加 using:
```csharp
using LYBT.Infrastructure.Constants;
```

### Step 2: 替换 Controllers 中的 [Authorize] 属性字符串

| Controller | 行号 | 原值 | 替换 |
|-----------|------|------|------|
| DiagnosticsController.cs | 17 | `[Authorize(Roles = "SuperAdmin")]` | `[Authorize(Roles = RoleConstants.SuperAdmin)]` |
| MedicalCaseController.cs | 55 | `[Authorize(Roles = "Doctor")]` | `[Authorize(Roles = RoleConstants.Doctor)]` |
| MedicalCaseController.cs | 28 | `[Authorize(Policy = "DoctorOrAdmin")]` | `[Authorize(Policy = PolicyConstants.DoctorOrAdmin)]` |
| PatientsController.cs | 22 | `[Authorize(Policy = "PatientAccess")]` | `[Authorize(Policy = PolicyConstants.PatientAccess)]` |
| PatientsController.cs | 52 | `User?.IsInRole("Admin")` / `User?.IsInRole("SuperAdmin")` | `User?.IsInRole(RoleConstants.Admin)` / `User?.IsInRole(RoleConstants.SuperAdmin)` |
| UsersController.cs | 多处 | `[Authorize(Policy = "AdminOnly")]` | `[Authorize(Policy = PolicyConstants.AdminOnly)]` |
| UsersController.cs | 192,285 | `[Authorize(Policy = "SuperAdminOnly")]` | `[Authorize(Policy = PolicyConstants.SuperAdminOnly)]` |
| FormulasController.cs | 21 | `[Authorize(Policy = "DoctorOrAdmin")]` | `[Authorize(Policy = PolicyConstants.DoctorOrAdmin)]` |
| HerbsController.cs | 20 | `[Authorize(Policy = "DoctorOrAdmin")]` | `[Authorize(Policy = PolicyConstants.DoctorOrAdmin)]` |
| SyncController.cs | 18 | `[Authorize(Policy = "DoctorOrAdmin")]` | `[Authorize(Policy = PolicyConstants.DoctorOrAdmin)]` |

每个文件添加 `using LYBT.Infrastructure.Constants;`

### Step 3: 替换 BaseApiController.cs 中的角色字符串

| 行号 | 原值 | 替换 |
|------|------|------|
| 50 | `User?.FindFirst("Admin")?.Value` | `User?.FindFirst(RoleConstants.Admin)?.Value` |
| 74 | `roleStr = "SuperAdmin"` | `roleStr = RoleConstants.SuperAdmin` |

### Step 4: 替换 BaseService.cs 中的角色字符串

| 行号 | 原值 | 替换 |
|------|------|------|
| 111 | `role.Contains("Admin", ...)` | `role.Contains(RoleConstants.Admin, ...)` |
| 205 | `r.Contains("Admin", ...)` | `r.Contains(RoleConstants.Admin, ...)` |
| 206 | `r.Contains("Doctor", ...)` | `r.Contains(RoleConstants.Doctor, ...)` |

### Step 5: 替换 Middleware 中的 Header 常量

**CorrelationIdMiddleware.cs:**
```
"X-Correlation-ID" → HttpHeaderConstants.CorrelationId
"traceparent"       → HttpHeaderConstants.Traceparent
"CorrelationId"     → HttpHeaderConstants.CorrelationIdItemKey
```

**ProblemDetailsConfiguration.cs:**
```
"traceId"       → HttpHeaderConstants.TraceIdKey
"CorrelationId" → HttpHeaderConstants.CorrelationIdItemKey
```

### Step 6: 编译验证

Run: `dotnet build LYBTZYZS.sln`
Expected: 0 errors

### Step 7: 运行测试验证 (行为不变)

Run: `dotnet test tests/LYBT.Tests.Server.Integration/ --no-build`
Expected: 266 passed, 0 failed

---

## Task 4: HTTP 状态码一致性修复

**Files:**
- Modify: `src/Server/Services/LYBT.WebAPI/Controllers/PatientsController.cs:163`
- Modify: `src/Server/Services/LYBT.WebAPI/Controllers/HerbsController.cs:134`

### Step 1: 修复 PatientsController.cs

**原代码 (line 163):**
```csharp
return UnprocessableEntity(new ApiResponse { Success = false, Message = result.ErrorMessage });
```

**替换为:**
```csharp
return BusinessFail(result.ErrorMessage ?? "无法删除，存在关联医案记录");
```

**理由:** `BusinessFail()` 同样返回 422，但额外设置 `RequestId` 并使用统一响应格式。

### Step 2: 修复 HerbsController.cs

**原代码 (line 134):**
```csharp
return UnprocessableEntity(new ApiResponse { Success = false, Message = result.ErrorMessage });
```

**替换为:**
```csharp
return BusinessFail(result.ErrorMessage ?? "无法删除，存在处方引用");
```

### Step 3: 编译验证

Run: `dotnet build src/Server/Services/LYBT.WebAPI/LYBT.WebAPI.csproj`
Expected: 0 errors

### Step 4: 运行集成测试验证

Run: `dotnet test tests/LYBT.Tests.Server.Integration/ --no-build`
Expected: 266 passed, 0 failed (状态码不变，仍为 422)

---

## Task 5: 日志级别标准化

**日志级别规范:**
| 级别 | 使用场景 |
|------|----------|
| LogDebug | 开发调试，生产不启用 |
| LogInformation | 业务流程记录 (成功和预期内的失败，如权限拒绝、资源不存在) |
| LogWarning | 可恢复的系统级问题 (重试、缓存失效、非关键审计失败) |
| LogError | 不可恢复的异常 (数据库异常、业务逻辑异常) |

**Files:**
- Modify: `src/Server/Modules/LYBT.Module.Auth/Services/AuthService.cs`
- Modify: `src/Server/Modules/LYBT.Module.Auth/Services/TokenRevocationService.cs`
- Modify: `src/Server/Core/LYBT.Infrastructure/Services/BaseService.cs`
- Modify: `src/Server/Modules/LYBT.Module.MedicalCase/Services/MedicalCaseCommandService.cs`
- Modify: `src/Server/Modules/LYBT.Module.Patients/Services/PatientService.cs`
- Modify: `src/Server/Modules/LYBT.Module.Sync/Services/SyncService.cs`
- Modify: `src/Server/Modules/LYBT.Module.Auth/Services/TokenManagementService.cs`

### Step 1: AuthService.cs - 异常处理 LogWarning -> LogError (2 处)

**line 200:**
```csharp
// 原: _logger.LogWarning(ex, "[SVC] Auth.Login -> RevokeOldTokensFailed - UserId={UserId}", userDto.Id);
// 改: _logger.LogError(ex, "[SVC] Auth.Login -> RevokeOldTokensFailed - UserId={UserId}", userDto.Id);
```

**line 222:**
```csharp
// 原: _logger.LogWarning(ex, "[SVC] Auth.Login -> RevokeOldAutoTokensFailed - UserId={UserId}", userDto.Id);
// 改: _logger.LogError(ex, "[SVC] Auth.Login -> RevokeOldAutoTokensFailed - UserId={UserId}", userDto.Id);
```

**理由:** 异常 (Exception) 应始终用 LogError 记录，即使流程可以继续。LogWarning 用于预期内的非理想状态。

### Step 2: TokenRevocationService.cs - 非关键审计失败 LogError -> LogWarning (1 处)

**line 72:**
```csharp
// 原: _logger.LogError(auditEx, "[SVC] Token.Revoke -> AuditFailed - TokenId={TokenId}", tokenRecord.Id);
// 改: _logger.LogWarning(auditEx, "[SVC] Token.Revoke -> AuditFailed - TokenId={TokenId}", tokenRecord.Id);
```

**理由:** 代码注释明确说明"审计日志失败不影响主操作"，是可恢复的非关键错误，适合 LogWarning。

### Step 3: BaseService.cs - 权限拒绝 LogWarning -> LogInformation (2 处)

**line 60-61:**
```csharp
// 原: _logger.LogWarning("权限验证失败 - 非本人创建: ...");
// 改: _logger.LogInformation("权限验证失败 - 非本人创建: ...");
```

**line 71-72:**
```csharp
// 原: _logger.LogWarning("权限验证失败 - 非当天创建: ...");
// 改: _logger.LogInformation("权限验证失败 - 非当天创建: ...");
```

**理由:** 权限拒绝是业务规则的正常分支，不是系统问题。属于审计记录级别。

### Step 4: MedicalCaseCommandService.cs - 业务验证 LogWarning -> LogInformation (3 处)

**line 125:**
```csharp
// 原: _logger.LogWarning("[SVC] MedicalCase.Create -> TcmDiagnosisEmpty");
// 改: _logger.LogInformation("[SVC] MedicalCase.Create -> TcmDiagnosisEmpty");
```

**line 176:**
```csharp
// 原: _logger.LogWarning("医案不存在，MedicalCaseId: {MedicalCaseId}", medicalCaseId);
// 改: _logger.LogInformation("[SVC] MedicalCase.UpdateConsultation -> NotFound - MedicalCaseId={MedicalCaseId}", medicalCaseId);
```

**line 195 (如存在类似 Warning):**
```csharp
// 原: _logger.LogWarning("[SVC] MedicalCase.UpdateConsultation -> ConsultationNotFound...");
// 改: _logger.LogInformation("[SVC] MedicalCase.UpdateConsultation -> ConsultationNotFound...");
```

**理由:** 资源不存在和业务验证失败是预期内的业务流程分支。

### Step 5: PatientService.cs - 结构化日志改进 (2 处)

**line 137:**
```csharp
// 原: _logger.LogWarning("[SVC] Patient.Create -> ValidationFailed - Errors={Errors}", string.Join("; ", errors));
// 改: _logger.LogInformation("[SVC] Patient.Create -> ValidationFailed - ErrorCount={ErrorCount} Errors={@Errors}", errors.Count, errors);
```

**line 188:**
```csharp
// 原: _logger.LogWarning("[SVC] Patient.Update -> ValidationFailed - PatientId={PatientId} Errors={Errors}", id, string.Join("; ", errors));
// 改: _logger.LogInformation("[SVC] Patient.Update -> ValidationFailed - PatientId={PatientId} ErrorCount={ErrorCount} Errors={@Errors}", id, errors.Count, errors);
```

**理由:** 1) 验证失败是业务流程，降级为 Information；2) `@Errors` 保留结构化数据，便于日志聚合系统解析。

### Step 6: SyncService.cs - 添加结构化日志参数 (3 处)

**line ~399:**
```csharp
// 原: _logger.LogError(ex, "上传 Herb 失败");
// 改: _logger.LogError(ex, "[SVC] Sync.UploadHerb -> Failed");
```

**line ~439:**
```csharp
// 原: _logger.LogError(ex, "上传 Patient 失败");
// 改: _logger.LogError(ex, "[SVC] Sync.UploadPatient -> Failed");
```

**line ~492:**
```csharp
// 原: _logger.LogError(ex, "上传 Formula 失败");
// 改: _logger.LogError(ex, "[SVC] Sync.UploadFormula -> Failed");
```

**理由:** 统一日志前缀格式 `[SVC] Module.Operation -> Result`，提高日志可搜索性。

### Step 7: TokenManagementService.cs - 添加缺失的异常日志 (1 处)

查找 ValidateTokenAsync 方法中的空 catch 块，添加日志:
```csharp
catch (Exception ex)
{
    _logger.LogWarning(ex, "[SVC] TokenMgmt.ValidateToken -> Failed");
    return Result<bool>.Failure(GenericErrorCode.AuthTokenInvalid);
}
```

**理由:** Token 验证失败可能是过期、签名错误等预期情况，LogWarning 足够。

### Step 8: 编译验证

Run: `dotnet build LYBTZYZS.sln`
Expected: 0 errors

### Step 9: 运行全量测试

Run: `dotnet test LYBTZYZS.sln --filter "FullyQualifiedName~LYBT.Tests"`
Expected: 2370 passed, 0 failed (纯日志级别变更，不影响行为)

---

## 验证清单

| 检查项 | 命令 |
|--------|------|
| 编译 | `dotnet build LYBTZYZS.sln` -> 0 errors |
| 单元测试 | `dotnet test tests/LYBT.Tests.Unit/` -> 1302 passed |
| Desktop 单元测试 | `dotnet test tests/LYBT.Tests.Desktop.Unit/` -> 633 passed |
| 集成测试 | `dotnet test tests/LYBT.Tests.Server.Integration/` -> 266 passed |
| Desktop 集成 | `dotnet test tests/LYBT.Tests.Desktop.Integration/` -> 95 passed |
| 架构测试 | `dotnet test tests/LYBT.Tests.Architecture/` -> 74 passed |
| 无残留硬编码 | `grep -rn '"Doctor"\|"Admin"\|"SuperAdmin"' src/Server/` -> 仅 config JSON 和无法替换处 |
