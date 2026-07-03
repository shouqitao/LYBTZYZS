# WebAPI 完善实施计划

> **For agentic workers:** REQUIRED SUB-SKILL: Use compose:subagent (recommended) or compose:execute to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 消除 WebAPI 15 个改进项，提升安全、性能、一致性和可维护性。

**Architecture:** 逐项修复，不改变端点 URL/行为。CORS 从配置读取，Validator 遵循 FluentValidation 模式，OutputCache 按端点特性应用。

**Tech Stack:** ASP.NET Core, FluentValidation, Serilog, OutputCache, RateLimiting, Swagger

## Global Constraints

- 不改变任何端点 URL 或响应结构（纯内部改进）
- 所有修复通过 `dotnet build` + 架构测试 + 集成测试验证
- CORS 配置从 `appsettings.json` 读取
- 新 Validator 遵循已有 FluentValidation 模式
- 编码规范：中文业务注释，英文标识符

---

### Task 1: CORS 配置 + BuildServiceProvider 修复

**Covers:** [S2]

**Files:**
- Modify: `src/Server/Services/LYBT.WebAPI/Extensions/ApiServiceCollectionExtensions.cs`
- Modify: `src/Server/Services/LYBT.WebAPI/Program.cs`

- [ ] **Step 1: 修复 BuildServiceProvider 反模式**

Read `src/Server/Services/LYBT.WebAPI/Extensions/ApiServiceCollectionExtensions.cs` around line 59.

Replace `services.BuildServiceProvider()` with proper pattern — use `IServiceCollection` extension that defers provider construction.

```csharp
// Before (line 59):
var serviceProvider = services.BuildServiceProvider();

// After:
// Remove BuildServiceProvider entirely. Use SwaggerDoc with action-based registration:
services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "LYBT WebAPI", Version = "v1" });
    // XML comments handled by IncludeXmlComments
    var xmlFiles = Directory.GetFiles(AppContext.BaseDirectory, "*.xml", SearchOption.TopDirectoryOnly);
    foreach (var xmlFile in xmlFiles)
        options.IncludeXmlComments(xmlFile);
});
```

- [ ] **Step 2: 添加 CORS 配置**

Read `src/Server/Services/LYBT.WebAPI/appsettings.json` and `appsettings.Production.json` for the `Cors` section.

Add to `ServiceCollectionExtensions.cs` or `Program.cs`:

```csharp
// 在 services 注册中
var corsSettings = configuration.GetSection("Cors");
if (corsSettings.Exists())
{
    services.AddCors(options =>
    {
        options.AddPolicy("AllowConfiguredOrigins", builder =>
        {
            var origins = corsSettings.GetSection("AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
            builder.WithOrigins(origins)
                .WithMethods(corsSettings.GetSection("AllowedMethods").Get<string[]>() ?? new[] { "GET", "POST", "PUT", "DELETE" })
                .WithHeaders(corsSettings.GetSection("AllowedHeaders").Get<string[]>() ?? Array.Empty<string>())
                .AllowCredentials();
        });
    });
}

// 在 middleware pipeline 中，UseRouting 之后 UseAuthorization 之前
app.UseCors("AllowConfiguredOrigins");
```

- [ ] **Step 3: 编译验证**

Run: `dotnet build LYBTZYZS.sln`

---

### Task 2: 响应格式统一

**Covers:** [S3]

**Files:**
- Modify: `src/Server/Services/LYBT.WebAPI/Controllers/FormulasController.cs`
- Modify: `src/Server/Services/LYBT.WebAPI/Controllers/MedicalCaseProcessingController.cs`

- [ ] **Step 1: 修复 FormulasController.Forbid()**

Read `src/Server/Services/LYBT.WebAPI/Controllers/FormulasController.cs` around the GetById method.

Replace bare `Forbid()` with `ApiResponse` 格式:

```csharp
// Before:
if (operatorRole == UserRole.Doctor && result.Value.CreatedBy != operatorId && !result.Value.IsShared)
    return Forbid();

// After:
if (operatorRole == UserRole.Doctor && result.Value.CreatedBy != operatorId && !result.Value.IsShared)
    return StatusCode(403, ApiResponse<object>.CreateFail("无权限查看此验方"));
```

- [ ] **Step 2: 修复 CancelMedicalCase NoContent**

Read `src/Server/Services/LYBT.WebAPI/Controllers/MedicalCaseProcessingController.cs` around the Cancel method.

Replace `NoContent()` with ApiResponse:

```csharp
// Before:
return NoContent();

// After:
return Success(true, "医案已取消");
```

- [ ] **Step 3: 编译验证**

---

### Task 3: OutputCache + RateLimiting 属性应用

**Covers:** [S3]

**Files:**
- Modify: `src/Server/Services/LYBT.WebAPI/Controllers/FormulasController.cs`
- Modify: `src/Server/Services/LYBT.WebAPI/Controllers/MedicalCasesController.cs`
- Modify: `src/Server/Services/LYBT.WebAPI/Controllers/HerbsController.cs`
- Modify: `src/Server/Services/LYBT.WebAPI/Controllers/ReportsController.cs`

- [ ] **Step 1: FormulasController 添加 OutputCache**

```csharp
[HttpGet]
[OutputCache(PolicyName = "FormulasCache")]
public async Task<IActionResult> GetList(...) { ... }
```

- [ ] **Step 2: MedicalCasesController 添加 OutputCache**

```csharp
[HttpGet]
[OutputCache(PolicyName = "MedicalCaseCache")]
public async Task<IActionResult> GetList(...) { ... }
```

- [ ] **Step 3: 应用 ApiCalls RateLimiting**

在写端点 (POST/PUT/DELETE) 添加 `[EnableRateLimiting("ApiCalls")]`:

```csharp
// HerbsController, FormulasController, MedicalCasesController 的 Create/Update/Delete/Batch 端点
[HttpPost]
[EnableRateLimiting("ApiCalls")]
public async Task<IActionResult> Create(...) { ... }
```

- [ ] **Step 4: 编译验证**

---

### Task 4: Serilog 请求日志 + ProducesResponseType

**Covers:** [S4]

**Files:**
- Modify: `src/Server/Services/LYBT.WebAPI/Extensions/UnifiedMiddlewareConfiguration.cs`
- Modify: 多个 Controller 文件（添加 ProducesResponseType）

- [ ] **Step 1: 添加 Serilog 请求日志**

Read `src/Server/Services/LYBT.WebAPI/Extensions/UnifiedMiddlewareConfiguration.cs`.

在 `UseRouting()` 之后添加:

```csharp
app.UseSerilogRequestLogging(options =>
{
    options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000}ms";
    options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
    {
        diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value);
        diagnosticContext.Set("UserAgent", httpContext.Request.Headers.UserAgent.ToString());
    };
});
```

- [ ] **Step 2: 添加 ProducesResponseType 到无文档端点**

在 DiagnosticsController, ConfigurationController, ReportsController, HealthController 的每个 action 添加:

```csharp
[HttpGet("status")]
[ProducesResponseType(typeof(ApiResponse<LoggingStatusDto>), 200)]
public async Task<IActionResult> GetStatus() { ... }
```

- [ ] **Step 3: 编译验证**

---

### Task 5: FluentValidation Validators

**Covers:** [S4]

**Files:**
- Create: `src/Server/Modules/LYBT.Module.MedicalCase/Application/Validators/UpdateMedicalCaseCommandValidator.cs`
- Create: `src/Server/Modules/LYBT.Module.Formula/Application/Validators/FormulaBatchImportCommandValidator.cs`
- Create: `src/Server/Modules/LYBT.Module.Herbs/Application/Validators/HerbBatchImportCommandValidator.cs`
- Create: `src/Server/Modules/LYBT.Module.Registration/Application/Validators/QuickVisitCommandValidator.cs`

- [ ] **Step 1: 创建 UpdateMedicalCaseCommandValidator**

```csharp
using FluentValidation;

namespace LYBT.Module.MedicalCase.Application.Validators;

public class UpdateMedicalCaseCommandValidator : AbstractValidator<UpdateMedicalCaseCommand>
{
    public UpdateMedicalCaseCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.PatientId).NotEmpty();
        RuleFor(x => x.UserId).NotEmpty();
    }
}
```

- [ ] **Step 2: 创建 FormulaBatchImportCommandValidator**

验证 formulas 列表非空，每个 formula 的 name/effect/usage 非空。

- [ ] **Step 3: 创建 HerbBatchImportCommandValidator**

验证 herbs 列表非空，每个 herb 的 name/price 有效。

- [ ] **Step 4: 创建 QuickVisitCommandValidator**

验证 patientId/patientName 非空。

- [ ] **Step 5: 编译验证**

---

### Task 6: DatabaseStartupDiagnostics 日志修复 + Redis 死代码清理

**Covers:** [S4]

**Files:**
- Modify: `src/Server/Services/LYBT.WebAPI/HealthCheck/DatabaseStartupDiagnostics.cs`
- Modify: `src/Server/Services/LYBT.WebAPI/Extensions/ApiServiceCollectionExtensions.cs`

- [ ] **Step 1: 修复字符串插值日志**

```csharp
// Before:
_logger.LogInformation($"数据库诊断: ...");

// After:
_logger.LogInformation("数据库诊断: 耗时 {ElapsedMs}ms, 状态: {Status}", elapsed, status);
```

- [ ] **Step 2: 删除 Redis AddCachingServices 死代码**

Read `src/Server/Services/LYBT.WebAPI/Extensions/ApiServiceCollectionExtensions.cs`. Find `AddCachingServices` method. If it has no callers, delete it.

- [ ] **Step 3: 编译验证**

---

### Task 7: 两个 Result<T> 类型适配清理

**Covers:** [S4]

**Files:**
- Modify: `src/Server/Services/LYBT.WebAPI/Controllers/PatientsController.cs`

- [ ] **Step 1: 统一 PatientsController 的 Result 类型**

Read `src/Server/Services/LYBT.WebAPI/Controllers/PatientsController.cs`. Find the `GetByIdNumber` endpoint that uses `_patientService.SearchAsync()` which returns `Result<T>` (Shared.Models version) while MediatR returns `Result<T>` (SharedKernel version).

If the endpoint was already migrated to MediatR (`SearchPatientByIdNumberQuery`), verify the Result type is consistent.

If adapters exist, simplify them.

- [ ] **Step 2: 编译验证**

---

### Task 8: Swagger XML 注释 + 健康检查去重

**Covers:** [S4, S5]

**Files:**
- Modify: `src/Server/Services/LYBT.WebAPI/Extensions/ApiServiceCollectionExtensions.cs`
- Modify: `src/Server/Services/LYBT.WebAPI/Controllers/HealthController.cs`

- [ ] **Step 1: 确保 Swagger XML 注释工作**

Read `src/Server/Services/LYBT.WebAPI/LYBT.WebAPI.csproj`. Check if `<GenerateDocumentationFile>true</GenerateDocumentationFile>` is set.

Verify `ApiServiceCollectionExtensions.cs` calls `options.IncludeXmlComments()` for all XML files.

- [ ] **Step 2: HealthController 标记为与 /health 中间件互补**

在 HealthController 添加注释说明：`/health` 中间件端点用于外部监控 (anonymous)，`/api/v1/health` 用于内部认证查询。

- [ ] **Step 3: 编译验证**

---

### Task 9: 验证全部修复

**Covers:** 全部

- [ ] **Step 1: 全量构建**

Run: `dotnet build LYBTZYZS.sln`
Expected: 0 errors

- [ ] **Step 2: 架构测试**

Run: `dotnet test tests/LYBT.Tests.Architecture/`
Expected: 81+ pass

- [ ] **Step 3: 集成测试**

Run: `dotnet test tests/LYBT.Tests.Desktop/ --filter "FullyQualifiedName~WorkflowIntegrationTests"`
Expected: 3+ pass

---

## 统计

| Task | 修复项 | 工作量 |
|------|--------|--------|
| T1 | CORS + BuildServiceProvider | 0.5天 |
| T2 | 响应格式统一 | 0.5天 |
| T3 | OutputCache + RateLimiting | 0.5天 |
| T4 | Serilog + ProducesResponseType | 0.5天 |
| T5 | Validators | 0.5天 |
| T6 | 日志修复 + 死代码 | 0.5天 |
| T7 | Result 类型适配 | 0.5天 |
| T8 | Swagger + 健康检查 | 0.5天 |
| T9 | 验证 | 0.5天 |
| **合计** | **15 项改进** | **4.5天** |
