# Task 2.3 + 2.4: Server 单元测试统一 Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** 将 13 个分散的 Server 单元测试项目 (~717 tests) 合并到 `tests/LYBT.Tests.Unit/`，去重 16 个测试，消除 13 个项目。

**Architecture:** 纯机械迁移 -- 复制文件到统一目录结构，更新 namespace 前缀为 `LYBT.Tests.Unit.*`，添加缺失的 csproj 依赖。不修改测试逻辑。2 个已确认重复文件 (BaseServiceTests + SensitiveDataJsonConverterTests) 跳过迁移。

**Tech Stack:** xunit + FluentAssertions + NSubstitute + .NET 8

---

## Namespace 转换规则

所有迁移文件的 namespace 按以下规则转换:

| 源 Namespace | 目标 Namespace | 目标目录 |
|-------------|---------------|---------|
| `LYBT.Shared.Validators.Tests.{Sub}` | `LYBT.Tests.Unit.Shared.Validators.{Sub}` | `Shared/Validators/{Sub}/` |
| `LYBT.Shared.ExceptionHandling.Tests.{Sub}` | `LYBT.Tests.Unit.Shared.ExceptionHandling.{Sub}` | `Shared/ExceptionHandling/{Sub}/` |
| `LYBT.Shared.Configuration.Tests.{Sub}` | `LYBT.Tests.Unit.Shared.Configuration.{Sub}` | `Shared/Configuration/{Sub}/` |
| `LYBT.Shared.Models.Tests` | `LYBT.Tests.Unit.Shared.Models` | `Shared/Models/` |
| `LYBT.Infrastructure.Tests.{Sub}` | `LYBT.Tests.Unit.Infrastructure.{Sub}` | `Infrastructure/{Sub}/` |
| `LYBT.Module.Auth.Tests.{Sub}` | `LYBT.Tests.Unit.Modules.Auth.{Sub}` | `Modules/Auth/{Sub}/` |
| `LYBT.Module.Users.Tests.{Sub}` | `LYBT.Tests.Unit.Modules.Users.{Sub}` | `Modules/Users/{Sub}/` |
| `LYBT.Module.Herbs.Tests.{Sub}` | `LYBT.Tests.Unit.Modules.Herbs.{Sub}` | `Modules/Herbs/{Sub}/` |
| `LYBT.Module.Patients.Tests.{Sub}` | `LYBT.Tests.Unit.Modules.Patients.{Sub}` | `Modules/Patients/{Sub}/` |
| `LYBT.Module.MedicalCases.Tests.{Sub}` | `LYBT.Tests.Unit.Modules.MedicalCases.{Sub}` | `Modules/MedicalCases/{Sub}/` |
| `LYBT.Module.Formulas.Tests.{Sub}` | `LYBT.Tests.Unit.Modules.Formulas.{Sub}` | `Modules/Formulas/{Sub}/` |
| `LYBT.Module.Sync.Tests.{Sub}` | `LYBT.Tests.Unit.Modules.Sync.{Sub}` | `Modules/Sync/{Sub}/` |
| `LYBT.WebAPI.Tests.{Sub}` | `LYBT.Tests.Unit.WebAPI.{Sub}` | `WebAPI/{Sub}/` |

**注意**: MedicalCase 和 Formula 的 RootNamespace 使用复数形式 (MedicalCases/Formulas)，保持与源项目一致。

---

## Task 1: 更新 Tests.Unit.csproj 依赖

**Files:**
- Modify: `tests/LYBT.Tests.Unit/LYBT.Tests.Unit.csproj`

添加 Task 2.3 + 2.4 所需的全部依赖:

```xml
<!-- 新增 PackageReference (Task 2.3) -->
<PackageReference Include="FluentValidation" />
<PackageReference Include="Microsoft.Extensions.Configuration" />
<PackageReference Include="Microsoft.Extensions.Configuration.Json" />
<PackageReference Include="Microsoft.Extensions.Configuration.EnvironmentVariables" />

<!-- 新增 PackageReference (Task 2.4) -->
<PackageReference Include="Bogus" />

<!-- 新增 ProjectReference (Task 2.3: Shared) -->
<ProjectReference Include="..\..\src\Shared\LYBT.Shared.Validators\LYBT.Shared.Validators.csproj" />
<ProjectReference Include="..\..\src\Shared\LYBT.Shared.ExceptionHandling\LYBT.Shared.ExceptionHandling.csproj" />
<ProjectReference Include="..\..\src\Shared\LYBT.Shared.Primitives\LYBT.Shared.Primitives.csproj" />
<ProjectReference Include="..\..\src\Shared\LYBT.Shared.Configuration\LYBT.Shared.Configuration.csproj" />

<!-- 新增 ProjectReference (Task 2.4: Server Modules) -->
<ProjectReference Include="..\..\src\Server\Modules\LYBT.Module.Auth\LYBT.Module.Auth.csproj" />
<ProjectReference Include="..\..\src\Server\Modules\LYBT.Module.Users\LYBT.Module.Users.csproj" />
<ProjectReference Include="..\..\src\Server\Modules\LYBT.Module.Herbs\LYBT.Module.Herbs.csproj" />
<ProjectReference Include="..\..\src\Server\Modules\LYBT.Module.Patients\LYBT.Module.Patients.csproj" />
<ProjectReference Include="..\..\src\Server\Modules\LYBT.Module.MedicalCase\LYBT.Module.MedicalCase.csproj" />
<ProjectReference Include="..\..\src\Server\Modules\LYBT.Module.Formula\LYBT.Module.Formula.csproj" />
<ProjectReference Include="..\..\src\Server\Modules\LYBT.Module.Sync\LYBT.Module.Sync.csproj" />
<ProjectReference Include="..\..\src\Server\Services\LYBT.WebAPI\LYBT.WebAPI.csproj" />
<ProjectReference Include="..\TestConfiguration\LYBT.Tests.Configuration.csproj" />
```

**Build check:** `dotnet build tests/LYBT.Tests.Unit/`
Expected: 0 errors

---

## Task 2: 迁移 Shared.Validators.Tests (9 files, 126 tests)

**Source:** `tests/UnitTests/Shared/LYBT.Shared.Validators.Tests/`

| 源文件 | 目标文件 | Tests |
|--------|---------|-------|
| `Auth/LoginRequestValidatorTests.cs` | `Shared/Validators/Auth/LoginRequestValidatorTests.cs` | 9 |
| `Auth/ChangePasswordRequestValidatorTests.cs` | `Shared/Validators/Auth/ChangePasswordRequestValidatorTests.cs` | 10 |
| `Auth/SuperAdminLoginRequestValidatorTests.cs` | `Shared/Validators/Auth/SuperAdminLoginRequestValidatorTests.cs` | 3 |
| `Patients/PatientInputDtoValidatorTests.cs` | `Shared/Validators/Patients/PatientInputDtoValidatorTests.cs` | 23 |
| `MedicalCase/MedicalCaseInputDtoValidatorTests.cs` | `Shared/Validators/MedicalCase/MedicalCaseInputDtoValidatorTests.cs` | 9 |
| `Consultation/ConsultationInputDtoValidatorTests.cs` | `Shared/Validators/Consultation/ConsultationInputDtoValidatorTests.cs` | 7 |
| `Prescriptions/PrescriptionInputDtoValidatorTests.cs` | `Shared/Validators/Prescriptions/PrescriptionInputDtoValidatorTests.cs` | 22 |
| `Formula/FormulaInputDtoValidatorTests.cs` | `Shared/Validators/Formula/FormulaInputDtoValidatorTests.cs` | 22 |
| `Herbs/HerbInputDtoValidatorTests.cs` | `Shared/Validators/Herbs/HerbInputDtoValidatorTests.cs` | 21 |

**Namespace change:** `LYBT.Shared.Validators.Tests.{Sub}` → `LYBT.Tests.Unit.Shared.Validators.{Sub}`

**Process for each file:**
1. Read source file
2. Create target file with updated namespace
3. No other changes to file content

**Build check:** `dotnet build tests/LYBT.Tests.Unit/`
**Test check:** `dotnet test tests/LYBT.Tests.Unit/ --filter "FullyQualifiedName~Shared.Validators" -v n`
Expected: 126 passed

---

## Task 3: 迁移 Shared.ExceptionHandling.Tests (4 files, 70 tests)

**Source:** `tests/UnitTests/Shared/LYBT.Shared.ExceptionHandling.Tests/`

| 源文件 | 目标文件 | Tests |
|--------|---------|-------|
| `Exceptions/AppExceptionTests.cs` | `Shared/ExceptionHandling/Exceptions/AppExceptionTests.cs` | 9 |
| `Exceptions/BusinessExceptionTests.cs` | `Shared/ExceptionHandling/Exceptions/BusinessExceptionTests.cs` | 16 |
| `ErrorCodes/ErrorCodeTests.cs` | `Shared/ExceptionHandling/ErrorCodes/ErrorCodeTests.cs` | 34 |
| `ProblemDetails/ProblemDetailsFactoryTests.cs` | `Shared/ExceptionHandling/ProblemDetails/ProblemDetailsFactoryTests.cs` | 11 |

**Namespace change:** `LYBT.Shared.ExceptionHandling.Tests.{Sub}` → `LYBT.Tests.Unit.Shared.ExceptionHandling.{Sub}`

**Build check:** `dotnet build tests/LYBT.Tests.Unit/`
**Test check:** `dotnet test tests/LYBT.Tests.Unit/ --filter "FullyQualifiedName~Shared.ExceptionHandling" -v n`
Expected: 70 passed

---

## Task 4: 迁移 Shared.Configuration.Tests + Models.Tests (7 files, 47 tests)

**Source:** `tests/UnitTests/Shared/LYBT.Shared.Configuration.Tests/` + `tests/UnitTests/Shared/LYBT.Shared.Models.Tests/`

| 源文件 | 目标文件 | Tests |
|--------|---------|-------|
| `Options/JwtOptionsTests.cs` | `Shared/Configuration/Options/JwtOptionsTests.cs` | 9 |
| `Options/ApiClientOptionsTests.cs` | `Shared/Configuration/Options/ApiClientOptionsTests.cs` | 7 |
| `Validation/JwtOptionsValidatorTests.cs` | `Shared/Configuration/Validation/JwtOptionsValidatorTests.cs` | 7 |
| `Extensions/ServerConfigurationExtensionsTests.cs` | `Shared/Configuration/Extensions/ServerConfigurationExtensionsTests.cs` | 4 |
| `Integration/ConfigurationLoadingTests.cs` | `Shared/Configuration/Integration/ConfigurationLoadingTests.cs` | 7 |
| `Integration/ValidateOnStartTests.cs` | `Shared/Configuration/Integration/ValidateOnStartTests.cs` | 9 |
| `PagedQueryBaseDtoTests.cs` (Models) | `Shared/Models/PagedQueryBaseDtoTests.cs` | 4 |

**Namespace changes:**
- `LYBT.Shared.Configuration.Tests.{Sub}` → `LYBT.Tests.Unit.Shared.Configuration.{Sub}`
- `LYBT.Shared.Models.Tests` → `LYBT.Tests.Unit.Shared.Models`

**Configuration.Tests 特殊注意**: 可能包含 appsettings.json 测试配置文件需一同复制。检查源目录是否有 .json 文件，如有需复制到对应目标目录并更新 csproj `<Content>` 节点。

**Build + Test check:** `dotnet test tests/LYBT.Tests.Unit/ --filter "FullyQualifiedName~Shared.Configuration or FullyQualifiedName~Shared.Models" -v n`
Expected: 47 passed

---

## Task 5: Task 2.3 全量验证

Run: `dotnet test tests/LYBT.Tests.Unit/ -v n`
Expected: ~835 passed (原 592 + 新 243), 0 failed

---

## Task 6: 迁移 Infrastructure.Tests (2 独有文件, 78 tests + 跳过 2 重复)

**Source:** `tests/UnitTests/Server/Core/LYBT.Infrastructure.Tests/`

**跳过 (已存在于 Tests.Unit):**
- `BaseServiceTests.cs` (12 tests) → 已有 `Infrastructure/Services/BaseServiceTests.cs`
- `Serialization/SensitiveDataJsonConverterTests.cs` (4 tests) → 已有 `Infrastructure/Serialization/SensitiveDataJsonConverterTests.cs`

**迁移:**

| 源文件 | 目标文件 | Tests |
|--------|---------|-------|
| `Repositories/BaseRepositoryTests.cs` | `Infrastructure/Repositories/BaseRepositoryTests.cs` | 65 |
| `Services/CrossModuleQueryServiceTests.cs` | `Infrastructure/Services/CrossModuleQueryServiceTests.cs` | 13 |

**Namespace change:** `LYBT.Infrastructure.Tests.{Sub}` → `LYBT.Tests.Unit.Infrastructure.{Sub}`

**Build + Test check:** `dotnet test tests/LYBT.Tests.Unit/ --filter "FullyQualifiedName~Infrastructure" -v n`
Expected: ~94 passed (12 existing BaseService + 4 existing Converter + 8 existing DbInit + 65 BaseRepo + 13 CrossModule - 8 DbInit 可能 namespace 不匹配)

---

## Task 7: 迁移 Module Tests Batch 1 - Auth + Users + Herbs (9 files, 155 tests)

**Auth (6 files, 69 tests):**

| 源文件 | 目标文件 | Tests |
|--------|---------|-------|
| `Security/JwtOptionsValidationTests.cs` | `Modules/Auth/Security/JwtOptionsValidationTests.cs` | 7 |
| `Services/AuthServiceTests.cs` | `Modules/Auth/Services/AuthServiceTests.cs` | 17 |
| `Services/JwtServiceTests.cs` | `Modules/Auth/Services/JwtServiceTests.cs` | 23 |
| `Services/SecurityAuditCleanupServiceTests.cs` | `Modules/Auth/Services/SecurityAuditCleanupServiceTests.cs` | 4 |
| `Services/SecurityAuditServiceTests.cs` | `Modules/Auth/Services/SecurityAuditServiceTests.cs` | 9 |
| `Services/TokenRevocationServiceTests.cs` | `Modules/Auth/Services/TokenRevocationServiceTests.cs` | 6 |

**Namespace change:** `LYBT.Module.Auth.Tests.{Sub}` → `LYBT.Tests.Unit.Modules.Auth.{Sub}`

**Users (1 file, 34 tests):**

| 源文件 | 目标文件 | Tests |
|--------|---------|-------|
| `Services/UserServiceTests.cs` | `Modules/Users/Services/UserServiceTests.cs` | 34 |

**Namespace change:** `LYBT.Module.Users.Tests.{Sub}` → `LYBT.Tests.Unit.Modules.Users.{Sub}`

**Herbs (2 files, 52 tests):**

| 源文件 | 目标文件 | Tests |
|--------|---------|-------|
| `Repositories/HerbRepositoryTests.cs` | `Modules/Herbs/Repositories/HerbRepositoryTests.cs` | 22 |
| `Services/HerbServiceTests.cs` | `Modules/Herbs/Services/HerbServiceTests.cs` | 30 |

**Namespace change:** `LYBT.Module.Herbs.Tests.{Sub}` → `LYBT.Tests.Unit.Modules.Herbs.{Sub}`

**Build + Test check:** `dotnet test tests/LYBT.Tests.Unit/ --filter "FullyQualifiedName~Modules.Auth or FullyQualifiedName~Modules.Users or FullyQualifiedName~Modules.Herbs" -v n`
Expected: 155 passed

---

## Task 8: 迁移 Module Tests Batch 2 - Patients + MedicalCase + Formula + Sync (10 files, 187 tests)

**Patients (3 files, 47 tests):**

| 源文件 | 目标文件 | Tests |
|--------|---------|-------|
| `Controllers/PatientsControllerTests.cs` | `Modules/Patients/Controllers/PatientsControllerTests.cs` | 12 |
| `Repositories/PatientRepositoryTests.cs` | `Modules/Patients/Repositories/PatientRepositoryTests.cs` | 5 |
| `Services/PatientServiceTests.cs` | `Modules/Patients/Services/PatientServiceTests.cs` | 30 |

**Namespace change:** `LYBT.Module.Patients.Tests.{Sub}` → `LYBT.Tests.Unit.Modules.Patients.{Sub}`

**MedicalCase (4 files, 49 tests):**

| 源文件 | 目标文件 | Tests |
|--------|---------|-------|
| `Services/MedicalCaseCommandServiceTests.cs` | `Modules/MedicalCases/Services/MedicalCaseCommandServiceTests.cs` | 10 |
| `Services/MedicalCasePrintServiceTests.cs` | `Modules/MedicalCases/Services/MedicalCasePrintServiceTests.cs` | 6 |
| `Services/MedicalCaseQueryServiceTests.cs` | `Modules/MedicalCases/Services/MedicalCaseQueryServiceTests.cs` | 10 |
| `Services/MedicalCaseStateServiceTests.cs` | `Modules/MedicalCases/Services/MedicalCaseStateServiceTests.cs` | 13 |

**Namespace change:** `LYBT.Module.MedicalCases.Tests` → `LYBT.Tests.Unit.Modules.MedicalCases` (注意复数形式)

**Formula (1 file, 28 tests):**

| 源文件 | 目标文件 | Tests |
|--------|---------|-------|
| `Services/FormulaServiceTests.cs` | `Modules/Formulas/Services/FormulaServiceTests.cs` | 28 |

**Namespace change:** `LYBT.Module.Formulas.Tests` → `LYBT.Tests.Unit.Modules.Formulas` (注意复数形式)

**Sync (2 files, 63 tests):**

| 源文件 | 目标文件 | Tests |
|--------|---------|-------|
| `Services/ChecksumHelperTests.cs` | `Modules/Sync/Services/ChecksumHelperTests.cs` | 35 |
| `Services/SyncServiceTests.cs` | `Modules/Sync/Services/SyncServiceTests.cs` | 28 |

**Namespace change:** `LYBT.Module.Sync.Tests.{Sub}` → `LYBT.Tests.Unit.Modules.Sync.{Sub}`

**Build + Test check:** `dotnet test tests/LYBT.Tests.Unit/ --filter "FullyQualifiedName~Modules.Patients or FullyQualifiedName~Modules.MedicalCases or FullyQualifiedName~Modules.Formulas or FullyQualifiedName~Modules.Sync" -v n`
Expected: 187 passed

---

## Task 9: 迁移 WebAPI.Tests (5 files, 38 tests)

**Source:** `tests/UnitTests/Server/WebAPI/`

| 源文件 | 目标文件 | Tests |
|--------|---------|-------|
| `Controllers/DiagnosticsControllerTests.cs` | `WebAPI/Controllers/DiagnosticsControllerTests.cs` | 16 |
| `Extensions/DatabaseServiceCollectionExtensionsTests.cs` | `WebAPI/Extensions/DatabaseServiceCollectionExtensionsTests.cs` | 2 |
| `Middleware/BusinessExceptionHandlerTests.cs` | `WebAPI/Middleware/BusinessExceptionHandlerTests.cs` | 5 |
| `Middleware/CorrelationIdMiddlewareTests.cs` | `WebAPI/Middleware/CorrelationIdMiddlewareTests.cs` | 6 |
| `Middleware/SystemExceptionHandlerTests.cs` | `WebAPI/Middleware/SystemExceptionHandlerTests.cs` | 5 |

**Namespace change:** `LYBT.WebAPI.Tests.{Sub}` → `LYBT.Tests.Unit.WebAPI.{Sub}`

**Build + Test check:** `dotnet test tests/LYBT.Tests.Unit/ --filter "FullyQualifiedName~WebAPI" -v n`
Expected: ~38 passed

---

## Task 10: 全量验证

Run: `dotnet test tests/LYBT.Tests.Unit/ -v n`
Expected: ~1,293 passed (592 existing + 717 migrated - 16 dedup), 0 failed

**验证清单:**
- [ ] 编译 0 错误
- [ ] 所有迁移的测试通过
- [ ] 原有测试无回归
- [ ] 没有重复测试 (BaseServiceTests + SensitiveDataJsonConverter 不重复迁移)

---

## 风险与缓解

| 风险 | 缓解 |
|------|------|
| csproj 依赖膨胀导致编译变慢 | 监控编译时间，>30s 则评估拆分 |
| 某些源测试依赖 appsettings.json | 检查并复制配置文件到目标 |
| EF Core InMemory vs SqlServer 差异 | 源项目已使用 InMemory，无需转换 |
| Namespace 冲突 | 每个源项目有独立 namespace 前缀，不会冲突 |
| Patients.Tests 异常引用 WebAPI | WebAPI 已在新增引用中，自然解决 |
| AutoMapper 遗留 (MedicalCase.Tests) | 检查是否仍需要，如不需要则跳过该包引用 |

---

## 预计时间

| Task | 文件数 | 预计 |
|------|--------|------|
| Task 1 (csproj) | 1 | 3 min |
| Task 2 (Validators) | 9 | 8 min |
| Task 3 (ExceptionHandling) | 4 | 5 min |
| Task 4 (Config + Models) | 7 | 5 min |
| Task 5 (验证 2.3) | - | 3 min |
| Task 6 (Infrastructure) | 2 | 3 min |
| Task 7 (Auth+Users+Herbs) | 9 | 8 min |
| Task 8 (Patients+MC+Formula+Sync) | 10 | 8 min |
| Task 9 (WebAPI) | 5 | 5 min |
| Task 10 (全量验证) | - | 5 min |
| **Total** | **47** | **~53 min** |
