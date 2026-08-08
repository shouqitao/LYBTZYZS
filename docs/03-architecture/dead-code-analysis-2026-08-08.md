# 精准死代码分析报告（Serena 符号引用）

> 版本: v1.0 | 日期: 2026-08-08 | 方法: Serena LSP 符号引用 + 编译器验证
> 背景: Q-01 泛型化 / Q-02 别名清理 / Q-03 命名空间统一 / 异常映射修复 后的死代码复扫（A-02 已完成过一轮，本次为复查新增）

## 扫描概览

- **扫描范围**: `src/Server`（LYBT.Infrastructure + 8 个模块 + LYBT.WebAPI）+ `src/Shared`（LYBT.Shared.Models / Configuration / Logging / Entities / ExceptionHandling）
- **扫描类型数**: 493 个 top-level 类型（class / interface / enum / record / struct）
- **判定方法（三级证据链）**:
  1. 初筛: 词边界正则 `\bName\b` 全仓检索（排除 obj/bin/docs/README/AGENTS）——0 命中 ⇒ 文本层无任何出现 ⇒ 引用必为 0（数学单调）
  2. 复核: **Serena `find_referencing_symbols` = 0**（报告的每个死代码项均以此为准）
  3. 编译器: `dotnet build --no-incremental`（0 警告）验证未使用字段；`EnforceCodeStyleInBuild + GenerateDocumentationFile` 验证未使用 using（IDE0005）
- **基线构建**: `dotnet build LYBTZYZS.sln --no-incremental` → **0 错误 0 警告**
- **确认死代码**: 4 个类型（含 5 个嵌套工厂类）+ 2 个方法 + 62 处未使用 using

---

## P1 死类 / 死接口 / 死枚举（确认死）

| 类型 | 文件 | 行号 | Serena 引用数 | DI 注册 | DbSet | 判定 |
|------|------|------|--------------|---------|-------|------|
| `ExceptionFactory`（含嵌套 `User`/`Patient`/`Herb`/`MedicalCase`/`Formula` 工厂类） | `src/Shared/LYBT.Shared.ExceptionHandling/Exceptions/Factory/ExceptionFactory.cs` | 8 | **0** | 无 | 无 | ✅ **确认死** |
| `TraceContext` | `src/Shared/LYBT.Shared.Logging/TraceContext.cs` | 14 | **0** | 无 | 无 | ✅ **确认死** |
| `LocalEndpoints` | `src/Shared/LYBT.Shared.Configuration/Constants/LocalEndpoints.cs` | 5 | **0** | 无 | 无 | ✅ **确认死** |
| `DailyConsultation`（Reports 领域 record） | `src/Server/Modules/LYBT.Module.Reports/Domain/DailyConsultation.cs` | 6 | **0** | 无 | 无 | ✅ **确认死** |

> 证据说明:
> - `ExceptionFactory`: `\bExceptionFactory\b` 全仓 0 命中（仅声明文件自身）；Serena `find_referencing_symbols` = `{}`。业务层抛异常均直接 `throw new NotFoundException(...)`，未走工厂。
> - `TraceContext`: `TraceContext\.` 全仓 0 使用；Serena = `{}`。日志/追踪已改用 `CorrelationIdEnricher` + Serilog Enrich 体系。
> - `LocalEndpoints`: `LocalEndpoints\.` 全仓 0 使用；Serena = `{}`。连接地址已收敛至配置项（`ApiClientOptions` 等）。
> - `DailyConsultation`: 报表查询改用 Shared 契约 `DailyConsultationDto`（ReportService 直接构造），领域 record `DailyConsultation` 无任何消费者；Serena = `{}`。

---

## P2 1:1 接口消除候选

**无候选。** 扫描范围内全部接口（`IRepository<T>`、`IUserRepository`、`IHerbService`、各 `I*CrossModuleService`、`IDomainEventDispatcher` 等约 40 个）均满足:
- 有 DI 注册（模块 `AddScoped<I, Impl>` 或 `AddMediatR` 扫描）且
- 有实现类注入消费

未发现「1 实现 + 0-1 注入 + 0 DI 注册」的接口，故无需运行架构测试验证（`tests/LYBT.Tests.Architecture` 当前无需为此变更）。

---

## P3 死方法（0 调用者）

| 方法 | 文件 | 行号 | 调用者数 | 调用者也死？ | 链深度 |
|------|------|------|----------|-------------|--------|
| `QueryablePagingExtensions.SelectAsync<TEntity,TResult>` | `src/Server/Core/LYBT.Infrastructure/Extensions/QueryablePagingExtensions.cs` | 28 | **0**（`\.SelectAsync\(` 全仓 0） | 同类 `GetPagedResultAsync` 有 5 处调用（MedicalCaseRepository）→ 类存活，仅此方法断链 | 1（断） |
| `UnifiedApplicationInitialization.ConfigureGracefulShutdown` | `src/Server/Services/LYBT.WebAPI/Extensions/UnifiedApplicationInitialization.cs` | 199 | **0**（Serena = `{}`） | 同文件 `InitializeAllApplicationServices`/`DisplayDatabaseStatusAsync` 由 Program.cs:236-237 调用 → 类存活，仅此方法断链 | 1（断） |

> 说明: 扩展方法类的**类名**不会出现在调用点（`services.Xxx()` 语法），LSP 对扩展方法调用的类引用索引为 0 属正常现象；因此扩展类内方法的判定采用「调用语法正则 + Serena 复核」双证据。`GetPagedResultAsync` 5 处调用证明类存活、`SelectAsync` 为重构（"从 BaseRepository 提取"）后残留。

---

## P4 未使用 using（编译器 IDE0005 验证，62 处 / 61 文件）

> 证据: `dotnet build -p:EnforceCodeStyleInBuild=true -p:GenerateDocumentationFile=true` 输出 IDE0005 警告（默认构建不启用该规则，故基线 0 警告不受影响）。

| 文件 | 未使用 using |
|------|-------------|
| `src/Server/Modules/LYBT.Module.MedicalCase/`（16 处，均冗余） | `using System.Threading;` × 16（Interfaces 4 + Repositories 4 + Services 8，已被全局 using 覆盖） |
| `src/Server/Core/LYBT.Infrastructure/Services/CrossModule/ICrossModuleService.cs:1`、`IHerbCrossModuleService.cs:1` | `using System.Threading;` |
| `src/Server/Modules/LYBT.Module.Herbs/Application/Commands/BatchImportHerbsCommandHandler.cs:6`、`CreateHerbCommandHandler.cs:6`、`DeleteHerbCommandHandler.cs:5` | `using LYBT.Entities.Herbs;`（改用 HerbDtoMapper 后残留） |
| `src/Server/Modules/LYBT.Module.Patients/Application/Commands/CreatePatientCommandHandler.cs:6`、`DeletePatientCommandHandler.cs:6`、`TogglePatientStatusCommandHandler.cs:7` | `using LYBT.Entities.Patients;` |
| `src/Server/Modules/LYBT.Module.Users/Application/Commands/DeleteUserCommandHandler.cs:7` | `using LYBT.Entities.Users;` |
| `src/Server/Modules/LYBT.Module.Users/Application/Commands/RestoreUserCommandHandler.cs:8` | `using Microsoft.EntityFrameworkCore;` |
| `src/Server/Modules/LYBT.Module.Auth/AuthModule.cs:4` | `using LYBT.Module.Auth.Interfaces;` |
| `src/Server/Modules/LYBT.Module.Auth/Application/Commands/LoginCommandHandler.cs:12` | `using LYBT.Shared.Models.Contracts.Users;` |
| `src/Server/Modules/LYBT.Module.Auth/Application/Commands/LogoutCommandHandler.cs:7` | `using LYBT.Shared.Models.Primitives.ErrorCodes;` |
| `src/Server/Modules/LYBT.Module.Formula/Application/Commands/BatchImportFormulasCommandHandler.cs:6` | `using LYBT.Shared.Models.Primitives.ErrorCodes;` |
| `src/Server/Modules/LYBT.Module.Patients/Application/Queries/BatchCheckPatientReferenceQueryHandler.cs:3` | `using LYBT.Shared.Models.Primitives.ErrorCodes;` |
| `src/Server/Modules/LYBT.Module.Registration/Application/Commands/CreateRegistrationCommandHandler.cs:8` | `using LYBT.Shared.Models.Primitives.ErrorCodes;` |
| `src/Server/Modules/LYBT.Module.Patients/PatientsModule.cs:4`、`src/Server/Modules/LYBT.Module.Registration/RegistrationModule.cs:4` | `using LYBT.Infrastructure.Data;` |
| `src/Server/Modules/LYBT.Module.Patients/PatientsModule.cs:7` | `using LYBT.Module.Patients.Infrastructure;` |
| `src/Server/Modules/LYBT.Module.Registration/RegistrationModule.cs:8` | `using LYBT.Module.Registrations.Infrastructure;` |
| `src/Server/Modules/LYBT.Module.Registration/Interfaces/IRegistrationRepository.cs:3` | `using LYBT.Shared.Models.Enums;` |
| `src/Server/Modules/LYBT.Module.MedicalCase/Controllers/BaseMedicalCasesController.cs:5` | `using LYBT.Shared.Models.Contracts.Common;` |
| `src/Server/Modules/LYBT.Module.MedicalCase/Controllers/BaseMedicalCasesController.cs:8` | `using LYBT.Shared.Models.Contracts.Prescriptions;` |
| `src/Server/Services/LYBT.WebAPI/Controllers/AuthController.cs:2` | `using LYBT.Infrastructure.Constants;` |
| `src/Server/Services/LYBT.WebAPI/Controllers/UsersController.cs:2`、`MedicalCasesController.cs:3`、`RegistrationsController.cs:3` | `using LYBT.Infrastructure.Web;` |
| `src/Server/Services/LYBT.WebAPI/Controllers/ConfigurationController.cs:7`、`HealthController.cs:8` | `using Microsoft.AspNetCore.Http;` |
| `src/Server/Services/LYBT.WebAPI/Extensions/DatabaseServiceCollectionExtensions.cs:9` | `using Microsoft.Extensions.Caching.Memory;` |
| `src/Server/Services/LYBT.WebAPI/Extensions/EnvironmentAwareHosting.cs:2` | `using System.Runtime.InteropServices;` |
| `src/Server/Services/LYBT.WebAPI/Extensions/ServiceCollectionExtensions.cs:15` | `using Microsoft.AspNetCore.HttpsPolicy;` |
| `src/Server/Services/LYBT.WebAPI/Extensions/UnifiedMiddlewareConfiguration.cs:1` | `using LYBT.WebAPI.Configuration;` |
| `src/Server/Services/LYBT.WebAPI/Middleware/CorrelationIdMiddleware.cs:1` | `using System.Diagnostics;` |
| `src/Server/Core/LYBT.Infrastructure/Configuration/Services/ISystemConfigurationService.cs:1` | `using System.Collections.Generic;` |
| `src/Server/Core/LYBT.Infrastructure/Configuration/Services/SystemConfigurationService.cs:3` | `using System;` |
| `src/Server/Core/LYBT.Infrastructure/Data/DatabaseInitializationService.cs:1` | `using LYBT.Entities.Users;` |
| `src/Server/Core/LYBT.Infrastructure/Data/DatabaseInitializationService.cs:5` | `using LYBT.Shared.Models.Utilities.Security;` |
| `src/Server/Core/LYBT.Infrastructure/Interfaces/IRepository.cs:1` | `using LYBT.Shared.Models.Contracts.Common;` |
| `src/Server/Core/LYBT.Infrastructure/Logging/LogCleanupService.cs:3` | `using Microsoft.Data.SqlClient;` |
| `src/Server/Core/LYBT.Infrastructure/SharedKernel/GlobalUsings.cs:1` | `global using System.ComponentModel;` |
| `src/Server/Core/LYBT.Infrastructure/Web/BaseApiController.cs:4` | `using LYBT.Shared.Models.Primitives.ErrorCodes;` |
| `src/Shared/LYBT.Shared.Configuration/Options/Client/CardReaderOptions.cs:2` | `using System.IO;` |
| `src/Shared/LYBT.Shared.Models/Contracts/Formula/ValidateFormulaHerbInputDto.cs:1` | `using System;` |
| `src/Shared/LYBT.Shared.Models/Contracts/Herbs/IHerbItemEditable.cs:2` | `using LYBT.Shared.Models.Contracts.Herbs;`（同命名空间自引用） |
| `src/Shared/LYBT.Shared.Models/Validators/MedicalCase/MedicalCaseInputDtoValidator.cs:3` | `using LYBT.Shared.Models.Primitives.Validation;` |

> 注: 迁移文件（`Migrations/*.cs`，7 处）亦有未使用 using，属生成代码，不计入。Desktop 与 tests 目录超出本次范围（同样存在大量 IDE0005，可另开批次）。

---

## P5 未使用私有字段

**0 项。** 编译器证据: `dotnet build --no-incremental` **0 警告**（CS0169「字段从未使用」/ CS0414「赋值但从未使用」均未触发）；未使用局部函数（CS8321）同样为 0。

---

## 排除项（防误报核查记录）

以下类型初筛为「0 类名引用」，但经机制核查确认**存活**，不报告:

| 类别 | 类型 | 存活机制 |
|------|------|----------|
| MediatR Handler（62 个） | 各模块 `*Command/QueryHandler` | `AddMediatR(cfg.RegisterServicesFromAssembly(...))` 程序集扫描 |
| FluentValidation Validator | `FormulaHerbItemInputDtoValidator`、`PrescriptionItemInputDtoValidator` 等 | 模块 `AddValidatorsFromAssemblyContaining<Shared.XValidator>()` 扫描 **Shared 程序集**（含全部 Shared Validator） |
| EF 实体配置（14 个） | `HerbConfiguration`、`PatientConfiguration` 等 | `AppDbContext.OnModelCreating → ApplyConfigurationsFromAssembly` |
| Options 子类（9 个） | `ConnectionPoolOptions`、`MonitoringOptions`、`RetryPolicyOptions`、`LogCleanupOptions`、`RateLimit*`、`AccountLockoutOptions` | 父 `DatabaseOptions`/`SecurityOptions`/`LoggingOptions` 属性绑定（`AddOptions<T>().Bind()` 约定） |
| 中间件 + 扩展（6 组） | `SecurityHeadersMiddleware` 等 + `*MiddlewareExtensions` | `UnifiedMiddlewareConfiguration` 扩展方法调用（Program.cs:251 → `ConfigureAllMiddleware`） |
| 控制器（12） | `AuthController` 等 | ASP.NET Core 反射发现 |
| Extension 方法类 | `DtoConversionExtensions`、`CacheExtensions`、`ErrorCodeExtensions`、`QueryablePagingExtensions`、`CrossModuleServiceExtensions`、`Client/ServerConfigurationExtensions`、`LoggerConfigurationExtensions`、`CorrelationIdEnricher(Extensions)` | 扩展方法调用不产生类名引用；方法级核查均有调用点（`.ToInputDto`/`.RemoveByPrefix`/`ToFormattedString`/`AddLybtServerConfiguration`/`AddCrossModuleService` 等） |
| 设计时工厂 | `AppDbContextFactory` | EF Core 设计时工具反射调用 |
| 改名残留 | `HerbMapper.cs`/`FormulaMapper.cs`（类名为 `HerbDtoMapper`/`FormulaDtoMapper`） | Q-02 改名后类名与文件名不符，但类本身被服务/Handler 大量调用 |

---

## 统计

- 死类/死类型: **4**（`ExceptionFactory` + 嵌套 5 工厂类、`TraceContext`、`LocalEndpoints`、`DailyConsultation`）
- 死接口: **0**
- 死方法: **2**（`SelectAsync`、`ConfigureGracefulShutdown`）
- 未使用 using: **62** 处（61 文件）
- 未使用私有字段: **0**
- 预估可删除行数: **~270 行**
  - ExceptionFactory.cs ~100 + TraceContext.cs ~34 + LocalEndpoints.cs ~17 + DailyConsultation.cs ~3 + SelectAsync ~15 + ConfigureGracefulShutdown ~35 + using ×62 ≈ **266 行**

## 方法学限制

1. LSP（Roslyn）对**扩展方法调用**不产生类名引用，故扩展类的存活判定使用「调用语法正则 + Serena 复核」双证据（已在 P3 标注）。
2. `find_referencing_symbols` 对泛型扩展方法（`SelectAsync`）无法索引方法级符号，方法级证据以调用语法正则为准。
3. 初始类型枚举依赖正则定位声明行（Serena `find_symbol` 无法按目录通配枚举），引用判定全部为 Serena 符号引用，声明行定位不影响结论。
4. IDE0005 仅对 `GenerateDocumentationFile=true` 的项目生效——Shared/Modules/Infrastructure 项目在本次命令行参数下同样被覆盖（已在命令中统一开启），故 62 处覆盖全部范围内项目。
