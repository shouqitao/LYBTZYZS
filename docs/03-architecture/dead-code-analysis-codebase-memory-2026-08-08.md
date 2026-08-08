# 死代码交叉验证报告（codebase-memory 独立分析）

> 独立分析者 #2 —— 工具：codebase-memory 知识图谱 + Roslyn(IDE0005) + 文本引用计数
> 本报告与 Serena 分析完全独立，未参考 `dead-code-analysis-2026-08-08.md`

## 扫描概览

- 扫描工具：codebase-memory（知识图谱 CALLS/USAGE/IMPORTS 边）+ `dotnet build -p:EnforceCodeStyleInBuild`（Roslyn IDE0005 权威判定）+ 全仓文本引用计数
- 扫描范围：src/Server + src/Shared（644 个 .cs 文件，539 个类型）
- 总类型数：539（567 Class 含嵌套 + 39 Interface + 21 Enum，去重后 539 个顶层类型）
- 确认死代码：**8 个死类型 + 19 个独立死方法 + 60 条未使用 using + 0 个未使用字段**

## 方法说明

| 判定项 | 方法 | 可靠性 |
|--------|------|--------|
| 死类/死接口/死枚举 | codebase-memory 引用数=0 + 全仓 `\bTypeName\b` 文本计数=0 + DI/DbSet/反射注册复核 | 高（三重交叉验证） |
| 死方法 | 全仓方法名出现次数≤1 + codebase-memory CALLS 入边=0 + 排除框架回调（路由/接口实现/反射注册） | 高 |
| 未使用 using | Roslyn IDE0005（`EnforceCodeStyleInBuild=true` + `GenerateDocumentationFile=true` 全量编译） | 权威 |
| 未使用字段 | 文本扫描 private readonly 声明次数 vs 使用次数 | 高 |

**关键排除项**（codebase-memory 显示 0 引用但**非死代码**，已人工复核）：
- MediatR Handlers（39 个）：`RegisterServicesFromAssembly` 反射注册，无静态引用
- EF EntityTypeConfiguration（14 个）：`ApplyConfigurationsFromAssembly` 反射注册
- Controller Actions：`[Http*]` 路由特性，框架调用
- Middleware/扩展方法类：`Use*`/`Add*` 扩展方法调用（调用点不写类名）
- `AddFormulaModuleDDD`：被 `AddFormulaModule` 内部调用（非死）

---

## 死类/死接口/死枚举

| 类型 | 文件 | 行号 | 引用数 | DI注册 | DbSet | 判定 |
|------|------|------|--------|--------|-------|------|
| `TraceContext` | src/Shared/LYBT.Shared.Logging/TraceContext.cs | 15 | 0（图+文本双证） | 无 | 无 | **死** |
| `ExceptionFactory` | src/Shared/LYBT.Shared.ExceptionHandling/Exceptions/Factory/ExceptionFactory.cs | 9 | 0（图+文本双证） | 无 | 无 | **死** |
| `LocalEndpoints` | src/Shared/LYBT.Shared.Configuration/Constants/LocalEndpoints.cs | 6 | 0（图+文本双证） | 无 | 无 | **死** |
| `DailyConsultation` (record) | src/Server/Modules/LYBT.Module.Reports/Domain/DailyConsultation.cs | 6 | 0（同文件 USAGE 已排除） | 无 | 无 | **死** |
| `DoctorCount` (record) | src/Server/Modules/LYBT.Module.Reports/Domain/DailyConsultation.cs | 11 | 0 | 无 | 无 | **死** |
| `DailyHerbUsage` (record) | src/Server/Modules/LYBT.Module.Reports/Domain/DailyHerbUsage.cs | 6 | 0 | 无 | 无 | **死** |
| `HerbUsageItem` (record) | src/Server/Modules/LYBT.Module.Reports/Domain/DailyHerbUsage.cs | 11 | 0 | 无 | 无 | **死** |
| `DailyIncome` (record) | src/Server/Modules/LYBT.Module.Reports/Domain/DailyIncome.cs | 6 | 0 | 无 | 无 | **死** |

**证据补充**：
- `TraceContext`：全仓仅定义文件出现（Client 注释提到的 "TraceContext标准" 是 W3C 规范名，非代码引用）；graph 中 USAGE 入边=0
- `ExceptionFactory`：全仓仅定义文件；无 `ExceptionFactory.*` 调用点；graph USAGE 入边=0
- `LocalEndpoints`：3 个常量 `LocalWebApiBaseUrl`/`RemoteWebApiBaseUrl`/`RemoteWebApiHttpsUrl` 全仓零使用（URL 实际散落硬编码在 Client/Server 各自配置）
- Reports Domain 4 个 record：ReportService/ReportRepository 实际返回的是 `*Dto`（Shared.Models.Contracts.Reports），Domain records 是 DDD 重构残留；graph 中唯一 USAGE 边来自同文件（`DailyHerbUsage`→`HerbUsageItem`），排除后 0 引用

## 死方法

| 方法 | 文件 | 行号 | 引用数 | 调用者也死？ |
|------|------|------|--------|-------------|
| `SelectAsync<TEntity,TResult>` | src/Server/Core/LYBT.Infrastructure/Extensions/QueryablePagingExtensions.cs | 34 | 0 | 否（类活，另一方法 GetPagedResultAsync 在用） |
| `RemoveHerb` | src/Shared/LYBT.Entities/Formulas/FormulaModel.cs | 158 | 0 | 否（实体仍被 DbSet 使用） |
| `MarkShared` | src/Shared/LYBT.Entities/Formulas/FormulaModel.cs | 174 | 0 | 否 |
| `UpdatePrice` | src/Shared/LYBT.Entities/Herbs/HerbModel.cs | 180 | 0 | 否 |
| `GetHerbNamesList` | src/Shared/LYBT.Shared.Models/Contracts/Formula/FormulaDetailDto.cs | 113 | 0 | 否（DTO 仍被使用） |
| `GetCurrentUserRole` | src/Server/Core/LYBT.Infrastructure/Web/BaseClaimsHelper.cs | 31 | 0 | 否（类活，GetCurrentUserId 在用） |
| `MapUserRoleToString` | src/Server/Core/LYBT.Infrastructure/Web/BaseClaimsHelper.cs | 56 | 0 | 否 |
| `CanManageUser` | src/Server/Core/LYBT.Infrastructure/Web/BaseClaimsHelper.cs | 71 | 0 | 否 |
| `WriteToConsoleWithTemplate` | src/Shared/LYBT.Shared.Logging/Extensions/LoggerConfigurationExtensions.cs | 69 | 0 | 否（类活，UseSharedLogging 在用） |
| `WriteToFileWithTemplate` | src/Shared/LYBT.Shared.Logging/Extensions/LoggerConfigurationExtensions.cs | 92 | 0 | 否 |
| `AddAsyncLocalCorrelationIdProvider` | src/Shared/LYBT.Shared.Logging/Extensions/ServiceCollectionExtensions.cs | 61 | 0 | 否（Server 用 Activity 实现，未调用此扩展） |
| `ConfigureGracefulShutdown` | src/Server/Services/LYBT.WebAPI/Extensions/UnifiedApplicationInitialization.cs | 199 | 0 | 否 |
| `ExecuteBatchStatusAsync` | src/Server/Core/LYBT.Infrastructure/Web/BaseCrudController.cs | 99 | 0 | 否（protected，无子类调用） |
| `UseStatusCodePagesWithProblemDetails` | src/Server/Services/LYBT.WebAPI/Configuration/ProblemDetailsConfiguration.cs | 55 | 0 | 否（AddProblemDetailsConfiguration 在用） |
| `GetCorrelationIdOrDefault` | src/Shared/LYBT.Shared.Logging/Abstractions/AsyncLocalCorrelationIdProvider.cs | 32 | 0 | 否（类被 DI 注册） |
| `GetOrNew` | src/Shared/LYBT.Shared.Logging/Abstractions/AsyncLocalCorrelationIdProvider.cs | 41 | 0 | 否 |
| `Clear` | src/Shared/LYBT.Shared.Logging/Abstractions/AsyncLocalCorrelationIdProvider.cs | 52 | 0 | 否 |
| `GenerateAndSet` | src/Shared/LYBT.Shared.Logging/Abstractions/AsyncLocalCorrelationIdProvider.cs | 58 | 1（仅 GetOrNew 调用，GetOrNew 本身死） | **是（断链）** |
| `GetCorrelationIdOrNew` | src/Shared/LYBT.Shared.Logging/Abstractions/ActivityCorrelationIdProvider.cs | 33 | 0 | 否 |

**断链方法（所属类已死，随类删除）**：
- `TraceContext` 全部 5 个成员（CurrentTraceId/TraceIdOrNew/CurrentSpanId/StartActivity/HasActiveTrace）
- `ExceptionFactory` 全部 ~20 个工厂方法（User.NotFound/Patient.NotFound/Herb.NameExists 等）
- Reports Domain 4 个 record 的构造器

**排除项**（0 静态引用但活）：
- `CreateDbContext`（AppDbContextFactory）：`IDesignTimeDbContextFactory` 接口实现，EF 工具反射调用
- `BuildModel`（ModelSnapshot）：EF 框架回调
- `CreateConverter`（SensitiveDataJsonConverterFactory）：`JsonConverterFactory` 虚方法
- `OnActionExecutionAsync`（ApiLoggingFilter）：`IAsyncActionFilter` 框架回调
- `TryDestructure`（SensitiveDataDestructuringPolicy）：`IDestructuringPolicy` 框架回调
- `GetCorrelationId`/`SetCorrelationId`（两个 Provider）：`ICorrelationIdProvider` 接口实现
- Controller actions（BaseMedicalCasesController 8 个、AuthController 2 个、ReportsController 5 个等）：`[Http*]` 路由

## 未使用 using（Roslyn IDE0005 权威）

扫描命令：`dotnet build src/Server/Services/LYBT.WebAPI/LYBT.WebAPI.csproj -p:EnforceCodeStyleInBuild=true -p:GenerateDocumentationFile=true --no-incremental`

**共 67 条 IDE0005，其中：**
- **60 条业务文件**（真实未使用，见下表）
- 6 条 Migrations 生成文件（`using System;`，EF 生成代码，不建议手改）
- 1 条 `GlobalUsings.cs` 的 `global using System.ComponentModel;`（全局 using 未使用，可安全删）

### 业务文件未使用 using（60 条）

| 文件 | 行 | 未使用 using |
|------|----|-------------|
| Contracts/Formula/ValidateFormulaHerbInputDto.cs | 1 | `System` |
| Contracts/Herbs/IHerbItemEditable.cs | 2 | `LYBT.Shared.Models.Contracts.Herbs` |
| Validators/MedicalCase/MedicalCaseInputDtoValidator.cs | 3 | `LYBT.Shared.Models.Primitives.Validation` |
| Options/Client/CardReaderOptions.cs | 2 | `System.IO` |
| Configuration/Services/ISystemConfigurationService.cs | 1 | `System.Collections.Generic` |
| Configuration/Services/SystemConfigurationService.cs | 3 | `System` |
| Data/DatabaseInitializationService.cs | 1 | `LYBT.Entities.Users` |
| Data/DatabaseInitializationService.cs | 5 | `LYBT.Shared.Models.Utilities.Security` |
| Logging/LogCleanupService.cs | 3 | `Microsoft.Data.SqlClient` |
| Interfaces/IRepository.cs | 1 | `LYBT.Shared.Models.Contracts.Common` |
| Services/CrossModule/ICrossModuleService.cs | 1 | `System.Threading` |
| Services/CrossModule/IHerbCrossModuleService.cs | 1 | `System.Threading` |
| Web/BaseApiController.cs | 4 | `LYBT.Shared.Models.Primitives.ErrorCodes` |
| Module.Formula/.../BatchImportFormulasCommandHandler.cs | 6 | `LYBT.Shared.Models.Primitives.ErrorCodes` |
| Module.Patients/PatientsModule.cs | 4 | `LYBT.Infrastructure.Data` |
| Module.Patients/PatientsModule.cs | 7 | `LYBT.Module.Patients.Infrastructure` |
| Module.Patients/.../CreatePatientCommandHandler.cs | 6 | `LYBT.Entities.Patients` |
| Module.Patients/.../DeletePatientCommandHandler.cs | 6 | `LYBT.Entities.Patients` |
| Module.Patients/.../TogglePatientStatusCommandHandler.cs | 7 | `LYBT.Entities.Patients` |
| Module.Patients/.../BatchCheckPatientReferenceQueryHandler.cs | 3 | `LYBT.Shared.Models.Primitives.ErrorCodes` |
| Module.Users/.../DeleteUserCommandHandler.cs | 7 | `LYBT.Entities.Users` |
| Module.Users/.../RestoreUserCommandHandler.cs | 8 | `Microsoft.EntityFrameworkCore` |
| Module.Herbs/.../BatchImportHerbsCommandHandler.cs | 6 | `LYBT.Entities.Herbs` |
| Module.Herbs/.../CreateHerbCommandHandler.cs | 6 | `LYBT.Entities.Herbs` |
| Module.Herbs/.../DeleteHerbCommandHandler.cs | 5 | `LYBT.Entities.Herbs` |
| Module.Registration/.../CreateRegistrationCommandHandler.cs | 8 | `LYBT.Shared.Models.Primitives.ErrorCodes` |
| Module.Auth/.../LoginCommandHandler.cs | 12 | `LYBT.Shared.Models.Contracts.Users` |
| Module.Registration/Interfaces/IRegistrationRepository.cs | 3 | `LYBT.Shared.Models.Enums` |
| Module.Registration/RegistrationModule.cs | 4 | `LYBT.Infrastructure.Data` |
| Module.Auth/.../LogoutCommandHandler.cs | 7 | `LYBT.Shared.Models.Primitives.ErrorCodes` |
| Module.Auth/AuthModule.cs | 4 | `LYBT.Module.Auth.Interfaces` |
| Module.Registration/RegistrationModule.cs | 8 | `LYBT.Module.Registrations.Infrastructure` |
| Module.MedicalCase/Interfaces/IMedicalCaseStateService.cs | 1 | `System.Threading` |
| Module.MedicalCase/Controllers/BaseMedicalCasesController.cs | 5 | `LYBT.Shared.Models.Contracts.Common` |
| Module.MedicalCase/Controllers/BaseMedicalCasesController.cs | 8 | `LYBT.Shared.Models.Contracts.Prescriptions` |
| Module.MedicalCase/Interfaces/IMedicalCaseRepository.cs | 1 | `System.Threading` |
| Module.MedicalCase/Interfaces/IMedicalCaseCommandService.cs | 1 | `System.Threading` |
| Module.MedicalCase/Services/MedicalCaseQueryService.cs | 12 | `System.Threading` |
| Module.MedicalCase/Services/MedicalCaseServiceHelper.cs | 1 | `System.Threading` |
| Module.MedicalCase/Services/MedicalCasePrescriptionService.cs | 9 | `System.Threading` |
| Module.MedicalCase/Services/MedicalCaseStateService.cs | 11 | `System.Threading` |
| Module.MedicalCase/Services/PrescriptionItemService.cs | 7 | `System.Threading` |
| Module.MedicalCase/Repositories/MedicalCaseRepository.PendingCases.cs | 1 | `System.Threading` |
| Module.MedicalCase/Repositories/MedicalCaseRepository.AuditLogs.cs | 1 | `System.Threading` |
| Module.MedicalCase/Repositories/MedicalCaseRepository.Update.cs | 1 | `System.Threading` |
| Module.MedicalCase/Repositories/MedicalCaseRepository.cs | 1 | `System.Threading` |
| Module.MedicalCase/Services/MedicalCaseCommandService.cs | 15 | `System.Threading` |
| Module.MedicalCase/Services/MedicalCaseCommandService.Deletion.cs | 1 | `System.Threading` |
| Module.MedicalCase/Interfaces/IMedicalCaseQueryService.cs | 1 | `System.Threading` |
| WebAPI/Controllers/AuthController.cs | 2 | `LYBT.Infrastructure.Constants` |
| WebAPI/Controllers/ConfigurationController.cs | 7 | `Microsoft.AspNetCore.Http` |
| WebAPI/Extensions/DatabaseServiceCollectionExtensions.cs | 9 | `Microsoft.Extensions.Caching.Memory` |
| WebAPI/Extensions/EnvironmentAwareHosting.cs | 2 | `System.Runtime.InteropServices` |
| WebAPI/Extensions/ServiceCollectionExtensions.cs | 15 | `Microsoft.AspNetCore.HttpsPolicy` |
| WebAPI/Extensions/UnifiedMiddlewareConfiguration.cs | 1 | `LYBT.WebAPI.Configuration` |
| WebAPI/Middleware/CorrelationIdMiddleware.cs | 1 | `System.Diagnostics` |
| WebAPI/Controllers/HealthController.cs | 8 | `Microsoft.AspNetCore.Http` |
| WebAPI/Controllers/RegistrationsController.cs | 3 | `LYBT.Infrastructure.Web` |
| WebAPI/Controllers/MedicalCasesController.cs | 3 | `LYBT.Infrastructure.Web` |
| WebAPI/Controllers/UsersController.cs | 2 | `LYBT.Infrastructure.Web` |

**误报排除记录**（启发式曾误判、Roslyn 判定为使用中）：`LoginCommandHandler` 的 `Contracts.Users`/`DTOs.Users` 曾疑似（var 推断返回类型），IDE0005 最终确认 `Contracts.Users` 为未使用、`DTOs.Users` 为使用中。

## 未使用私有字段

| 字段 | 文件 | 行号 | 引用数 |
|------|------|------|--------|
| （无） | — | — | — |

**结论：0 个未使用私有字段。** 210 个 `private readonly` 字段全部有 ≥2 次出现（声明 + 至少一次读取），无只写不读字段。

## 统计

- 死类：**6 个**（TraceContext / ExceptionFactory / LocalEndpoints / DailyConsultation / DailyHerbUsage / DailyIncome）
- 死接口：0 个
- 死枚举：0 个
- 死 record：**2 个**（DoctorCount / HerbUsageItem —— 已计入上方 8 个死类型总数）
- 死方法：**19 个独立 + 5 个断链方法**
- 未使用 using：**60 条业务 + 1 条 global using**（另有 6 条 Migrations 生成文件，不建议改）
- 未使用字段：**0 个**
- 预估可删除行数：**~473 行**（死类型 202 行 + 死方法 ~271 行；不含 using 单行）

## 风险提示

1. **Reports Domain records 删除**：需确认 DDD 重构不再需要这些值对象（当前 ReportService 用 DTO，删除安全）
2. **`ExceptionFactory` 删除**：若有外部脚本/文档依赖其错误码构造约定，删除前需确认（当前代码库零调用）
3. **`LocalEndpoints` 常量删除**：URL 常量散落各处，建议重构时统一到配置，删除前保留基线
4. **未使用 using 清理**：IDE0005 是 suggestion 级，不阻塞构建；MedicalCase 模块 `System.Threading` 未使用说明该模块已有 `global using System.Threading` 或隐式 using
5. codebase-memory 索引存在过期文件（如已删除的 `Module.Formula/Domain/Formula.cs`），本报告所有判定均以**当前工作区文件系统**为准
