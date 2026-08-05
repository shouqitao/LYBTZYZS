# WebApi 架构修复计划（03-server.md 与代码对齐）

> **For agentic workers:** 经技术总监确认后，按本计划 Task 逐步执行。每个 Task 完成后立即 `dotnet build LYBTZYZS.sln --no-incremental` 验证，最后统一提交。
>
> 状态：⏳ 待技术总监确认（2026-08-05 勘察完成，14 项问题全部验证属实）

**Goal:** 修复 `docs/03-architecture/03-server.md` 与代码之间的 14 处脱节（P0×2, P1×8, P2×4），并修复唯一代码违规（ReportsController 直连 Repository）。

**Architecture:** 文档先行——先更新 03-server.md 反映真实架构，再修复 P0-1 代码违规（新增 ReportService，Controller 注入 Service 接口）。只改必需文件，不做兼容层，不顺手改无关内容。

**Tech Stack:** .NET 8 / ASP.NET Core / EF Core / MediatR / xUnit + NetArchTest（架构测试）

---

## 勘察结论（14 项问题全部验证属实）

### P0-1  ReportsController 直接注入 Repository —— ✅ 确认
- 文档规则：`03-server.md:225`「Controller 注入 Service 接口，禁止注入 Repository 或 DbContext」
- 实际：`src/Server/Services/LYBT.WebAPI/Controllers/ReportsController.cs:21` 直接注入 `IReportRepository`，L44/45/70/71/95 直调 repository 方法
- Reports 模块（`LYBT.Module.Reports`）只有 `Interfaces/IReportRepository.cs` + `Infrastructure/ReportRepository.cs` + `Infrastructure/ReportsDbContext.cs` + `Domain/`（3 个 record），**无 Service 层**
- 补充发现：`tests/LYBT.Tests.Architecture/ServerArchTests.cs` 的 `ServerAssemblies` 数组**缺少 `LYBT.Module.Reports` 与 `LYBT.Module.Registration`**——这是该违规未被架构测试捕获的直接原因，修复时应同步补上（见 Task A4）

### P0-2  架构模式整体描述与代码脱节 —— ✅ 确认
- 文档：`03-server.md:5`「简单模块使用传统三层模式，复杂模块 (MedicalCase) 使用 CQRS」
- 实际（逐模块核实）：
  | 模块 | 实际模式 | 证据 |
  |------|---------|------|
  | Auth | MediatR CQRS | `Application/Commands/LoginCommandHandler.cs` 等 4 Handler；`AuthModule.cs:44` AddMediatR |
  | Users | Service + MediatR 混合 | `Services/UserService.cs`（CRUD）+ `Application/Commands/` 10 个 Handler；`UsersModule.cs:48` AddMediatR |
  | Patients | Service + MediatR 混合 | `Services/PatientService.cs` + `Application/Commands|Queries/` 8 个 Handler；`PatientsModule.cs:40` AddMediatR |
  | Herbs | Service + MediatR 混合 | `Services/HerbService.cs` + `Application/Commands|Queries/` 6 个 Handler；`HerbsModule.cs:52` AddMediatR |
  | Formula | Service + MediatR 混合 | `Services/FormulaService.cs` + `Application/Commands|Queries/` 7 个 Handler；`FormulaModule.cs:52` AddMediatR |
  | Registration | 纯 MediatR CQRS | `Application/Commands|Queries/` 8 个 Handler，无 Service 层；`RegistrationModule.cs:34` AddMediatR |
  | MedicalCase | Service 拆分（Command/Query/State） | `Services/MedicalCaseCommandService|QueryService|StateService.cs`，无 MediatR Handler |
  | Reports | Controller → Repository 直连 | `ReportsController.cs:21` |

### P1-3  模块标准目录结构不一致 —— ✅ 确认
- 文档：`03-server.md:139-150` 标准结构 `Repositories/ Services/ Mapping/ Validators/`
- 实际：8 个模块互不统一
  - Auth/Users/Patients/Herbs/Formula/Registration：`Application/ Domain/ Infrastructure/ Interfaces/ Services/`（+ 各自 Controllers/Mappers/Models 变体）
  - MedicalCase：`Controllers/ Interfaces/ Mappers/ Repositories/ Services/`
  - Reports：`Domain/ Infrastructure/ Interfaces/`

### P1-4  BaseRepository 已精简 —— ✅ 确认
- 文档：`03-server.md:128`「BaseRepository 公开方法 (21 个)」
- 实际：`src/Server/Core/LYBT.Infrastructure/Repositories/BaseRepository.cs:10-14` 注释「精简版本，只保留核心CRUD操作」，仅 5 个公开方法：`GetByIdAsync / AddAsync / UpdateAsync / DeleteAsync / SaveChangesAsync`（另 `SaveChangesAsync` 为 protected）

### P1-5  LYBT.Entities 已移出 Core 层 —— ✅ 确认
- 文档：`03-server.md:26-29` 架构图将 LYBT.Entities 列为 Core 层
- 实际：`src/Shared/LYBT.Entities/`（目录 `Auth/ Common/ Consultations/ Formulas/ Herbs/ MedicalCases/ Patients/ Prescriptions/ Registrations/ Users/`，与文档列出的目录结构也有差异——多了 `Registrations/`）
- Core 层实际只剩 `LYBT.Infrastructure`

### P1-6  MedicalCase CQRS 服务清单与实际不符 —— ✅ 确认
- 文档：`03-server.md:171-180` 列出 7 项（Command/Query/State/Permission/Audit/Rules/ServiceHelper）
- 实际：`Services/` 目录 7 个文件 = `MedicalCaseCommandService(+.Deletion partial) / MedicalCaseQueryService / MedicalCaseStateService / MedicalCaseCrossModuleService / MedicalCasePrescriptionService / MedicalCaseServiceHelper / PrescriptionItemService`；`Interfaces/` 5 个 = `IMedicalCaseCommandService / IMedicalCaseQueryService / IMedicalCaseStateService / IMedicalCaseReferenceRepository / IMedicalCaseRepository`。**Permission/Audit/Rules 不存在**

### P1-7  模块清单「跨模块通信」列全部过时 —— ✅ 确认
- 文档：`03-server.md:156` Auth→IUserService、L161 MedicalCase→IPatientService
- 实际：
  - Auth：`LoginCommandHandler.cs:35` 注入 `ICrossModuleService`（+ `IUserCrossModuleService` 由 `UsersModule.cs:45` 注册，供 MedicalCase + Auth 使用）
  - MedicalCase：`MedicalCaseCommandService.cs:37` 注入 `IRegistrationCrossModuleService` + `ICrossModuleService`；`MedicalCaseStateService.cs:38` 同
  - Patients：`CheckPatientReferenceQueryHandler.cs:15` 注入 `IMedicalCaseCrossModuleService`

### P1-8  跨模块服务「新旧方向」颠倒 —— ✅ 确认
- 文档：`03-server.md:94` ICrossModuleService「旧统一接口 [Obsolete]」
- 实际：`src/Server/Core/LYBT.Infrastructure/Services/CrossModule/ICrossModuleService.cs:7-9` 无 `[Obsolete]`，注释「跨模块通信服务 — 统一接口，替代旧的 IPatientCrossModuleService/IHerbCrossModuleService/IUserCrossModuleService」
- 补充：`CrossModule/` 目录现存 8 个文件：`ICrossModuleService / CrossModuleService / IPatientCrossModuleService / IHerbCrossModuleService / IUserCrossModuleService / IMedicalCaseCrossModuleService / IRegistrationCrossModuleService / ReferenceCheckResult.cs`——**旧接口文件仍在，但不再是 [Obsolete] 角色，而是被统一接口委托/并存**（如 `CrossModuleService` 委托给各域 CrossModuleService）

### P1-9  ApiErrorCodes.cs 不存在 —— ✅ 确认
- 文档：`03-server.md:124` 列 `Web/ApiErrorCodes.cs`
- 实际：不存在。错误码在 `src/Shared/LYBT.Shared.Models/Primitives/ErrorCodes/`：`ErrorCode.cs`（枚举 682 行）/ `ErrorCategory.cs` / `ErrorMessages.cs` / `ErrorCodeExtensions.cs`
- 补充：`ErrorCode.cs:15` 分区注释为 0xxxx 通用 / 1xxxx 用户 / 2xxxx 患者 / 3xxxx 医案 / 4xxxx 处方 / 5xxxx 草药 / 6xxxx 配方 / **8xxxx 挂号**——文档 `03-server.md:317-326` 错误码表列了「7xxxx 数据同步」但没有 8xxxx 挂号，且 7xxxx 在代码中不存在（Sync 模块不在 v1.0），需一并修正

### P1-10  BaseService 层次结构名存实亡 —— ✅ 确认
- 文档：`03-server.md:334-350` 所有 Service 统一继承 BaseService，含 `ExecuteAsync/ValidateAsync` 能力
- 实际：
  - `src/Server/Core/LYBT.Infrastructure/Services/BaseService.cs` 仅提供 `ILogger` 注入（非泛型 + 泛型 `BaseService<T>`），**无 ExecuteAsync/ValidateAsync**
  - 仅 MedicalCase 的 Command/Query/State 3 个 Service 继承 `BaseService<MedicalCase>`；UserService/PatientService/HerbService/FormulaService 等全部直接实现接口

### P2-11  Controller 规范示例过时 —— ✅ 确认
- 文档：`03-server.md:203-222` 示例 `[Route("api/[controller]")]` + 直接注入 Service
- 实际：`ReportsController.cs` 用 `[ApiVersion("1")] + [Route("api/v{version:apiVersion}/reports")]`；CRUD Controller 继承 `BaseCrudController`（注入 `ISender` 供 MediatR 使用），如 `BaseUsersController.cs`

### P2-12  模块独立 DbContext 设计未在文档体现 —— ✅ 确认
- 文档：完全未提及模块级 DbContext
- 实际：5 个模块有独立 DbContext（`AuthDbContext / UsersDbContext / HerbsDbContext / FormulaDbContext / ReportsDbContext`，均用 `ConnectionStringResolver.GetEffectiveConnectionString`）；**Patients / MedicalCase / Registration 3 个模块复用共享 `AppDbContext`**（`PatientsModule.cs:25`、`RegistrationModule.cs:25` 注释「使用AppDbContext」）
- 架构测试 P02/P10 的 `usesOwnDbContext` 豁免逻辑与此设计相呼应

### P2-13  Infrastructure 目录结构漂移 —— ✅ 确认
- 文档：`03-server.md:103-126` 目录结构（Data/Configurations/Base、Validation/、Web/ApiErrorCodes.cs 等）
- 实际 `LYBT.Infrastructure/` 目录：`Caching/ Configuration/ Constants/ Data/ ExceptionHandling/ Extensions/ Interfaces/ Logging/ Migrations/ Repositories/ Serialization/ Services/ SharedKernel/ Web/`（Web/ 下为 `BaseApiController.cs / BaseClaimsHelper.cs / BaseCrudController.cs / ControllerBaseExtensions.cs / OperatorAccessor.cs / README.md`，无 ApiErrorCodes.cs）

### P2-14  MedicalCaseModel 方法清单小差异 —— ✅ 确认
- 文档：`03-server.md:68` 列 `Complete()/SaveAsDraft()/SoftDelete()/UpdateConsultation()`
- 实际：`src/Shared/LYBT.Entities/MedicalCases/MedicalCaseModel.cs` 方法为 `Complete() / Suspend() / SoftDelete() / UpdateConsultation()`（**SaveAsDraft 不存在，实际是 Suspend**）；另有计算属性 `IsLocked / IsActive / IsCompleted`

---

## 修复原则（技术总监指令，不可违反）

1. **禁止兼容层/中间过渡状态**：发现错误直接重写，不做「新旧都支持」的兼容层
2. **Surgical Changes**：只改必需代码，不顺手改无关内容（各模块 AGENTS.md 过时内容属 A-01/C-05 范畴，不在本计划）
3. **文档优先**：先更新文档，再修复代码
4. **0 错误 0 警告**：`dotnet build LYBTZYZS.sln --no-incremental` 必须通过

## Global Constraints

- 构建验证命令（所有 Task）：`dotnet build LYBTZYZS.sln --no-incremental`，期望 0 错误 0 警告
- 架构测试：`dotnet test tests/LYBT.Tests.Architecture/`，期望全部通过
- 唯一代码变更 = Reports 模块（P0-1）；其余 13 项为纯文档更新，不得触碰任何其他 .cs 文件
- 提交信息：英文，`fix(webapi): ...` / `docs(server): ...` 前缀
- 文档更新须与 `13-project-master-plan.md` §九 的决策记录一致（A-03 MediatR 简化、A-05 实体源统一等）

---

## Task A：P0-1 代码修复（Reports 模块）

### Task A1: 新增 IReportService 接口

**Files:**
- Create: `src/Server/Modules/LYBT.Module.Reports/Interfaces/IReportService.cs`

**内容**（对照 `IReportRepository.cs` 现有 5 个方法，Service 层返回 DTO）：

```csharp
using LYBT.Shared.Models.Contracts.Reports;

namespace LYBT.Module.Reports.Interfaces;

/// <summary>
/// 报表服务接口 — 封装报表只读聚合查询。
/// </summary>
public interface IReportService
{
    Task<DailyIncomeDto> GetDailyIncomeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);
    Task<DailyConsultationDto> GetDailyConsultationsAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);
    Task<DailyHerbUsageDto> GetDailyHerbUsageAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);
}
```

**验证**: `dotnet build LYBTZYZS.sln --no-incremental` 0 错误 0 警告

---

### Task A2: 新增 ReportService 实现

**Files:**
- Create: `src/Server/Modules/LYBT.Module.Reports/Services/ReportService.cs`

**内容**（组合 repository 查询 → DTO；`DailyIncomeDto/DoctorCountDto/HerbUsageItemDto` 位于 `LYBT.Shared.Models.Contracts.Reports`）：

```csharp
using LYBT.Module.Reports.Interfaces;
using LYBT.Shared.Models.Contracts.Reports;

namespace LYBT.Module.Reports.Services;

/// <summary>
/// 报表服务实现 — 聚合仓库查询并组装报表 DTO。
/// </summary>
internal class ReportService : IReportService
{
    private readonly IReportRepository _reportRepository;

    public ReportService(IReportRepository reportRepository)
    {
        _reportRepository = reportRepository ?? throw new ArgumentNullException(nameof(reportRepository));
    }

    public async Task<DailyIncomeDto> GetDailyIncomeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        var registrationFeeTotal = await _reportRepository.GetRegistrationFeeTotalAsync(startDate, endDate, cancellationToken);
        var medicineFeeTotal = await _reportRepository.GetMedicineFeeTotalAsync(startDate, endDate, cancellationToken);

        return new DailyIncomeDto
        {
            TotalIncome = registrationFeeTotal + medicineFeeTotal,
            RegistrationFeeTotal = registrationFeeTotal,
            MedicineFeeTotal = medicineFeeTotal
        };
    }

    public async Task<DailyConsultationDto> GetDailyConsultationsAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        var totalCount = await _reportRepository.GetConsultationCountAsync(startDate, endDate, cancellationToken);
        var byDoctor = await _reportRepository.GetConsultationsByDoctorAsync(startDate, endDate, cancellationToken);

        return new DailyConsultationDto
        {
            TotalCount = totalCount,
            ByDoctor = byDoctor
        };
    }

    public async Task<DailyHerbUsageDto> GetDailyHerbUsageAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        var items = await _reportRepository.GetHerbUsageAsync(startDate, endDate, cancellationToken);

        return new DailyHerbUsageDto { Items = items };
    }
}
```

> 说明：`ReportService` 为 `internal` 与 `ReportRepository` 一致（模块内可见，`InternalsVisibleTo` 已存在则测试可见）；不继承 `BaseService`——与 Users/Patients/Herbs/Formula 模块的 Service 现状一致（BaseService 仅 MedicalCase 使用）。

**验证**: `dotnet build LYBTZYZS.sln --no-incremental` 0 错误 0 警告

---

### Task A3: ReportsModule 注册 IReportService

**Files:**
- Modify: `src/Server/Modules/LYBT.Module.Reports/ReportsModule.cs:33`（仓储注册之后追加）

**变更**：在 `services.AddScoped<IReportRepository, ...>()` 之后新增一行：

```csharp
// 服务层
services.AddScoped<IReportService, Services.ReportService>();
```

**验证**: `dotnet build LYBTZYZS.sln --no-incremental` 0 错误 0 警告

---

### Task A4: ReportsController 改注入 IReportService

**Files:**
- Modify: `src/Server/Services/LYBT.WebAPI/Controllers/ReportsController.cs`

**变更**：
1. using 不变（`LYBT.Module.Reports.Interfaces` 已含 IReportService）
2. L21 `IReportRepository _reportRepository` → `IReportService _reportService`
3. L23-29 构造参数 `IReportRepository reportRepository` → `IReportService reportService`，字段赋值同步
4. L44-45 两个 repository 调用 → `_reportService.GetDailyIncomeAsync(start, end, cancellationToken)`（Service 返回 `DailyIncomeDto`，删除本地组装逻辑）
5. L70-71 → `_reportService.GetDailyConsultationsAsync(start, end, cancellationToken)`
6. L95 → `_reportService.GetDailyHerbUsageAsync(start, end, cancellationToken)`

**验证**: `dotnet build LYBTZYZS.sln --no-incremental` 0 错误 0 警告

---

### Task A5: 架构测试补全（可选但强烈建议，防止回归）

> 依据：ServerArchTests `ServerAssemblies` 缺少 Reports/Registration 是 P0-1 漏检的直接原因。

**Files:**
- Modify: `tests/LYBT.Tests.Architecture/ServerArchTests.cs`

**变更**：`ServerAssemblies` 数组追加两行：

```csharp
Assembly.Load("LYBT.Module.Reports"),
Assembly.Load("LYBT.Module.Registration"),
```

> ⚠️ 注意：Registration 模块的 `RegistrationModule.cs` 不注册任何 Service（纯 MediatR），`Services_Should_Have_Service_Suffix` / `P10_Services_Should_Not_Directly_Inject_AppDbContext` 等测试仅扫描 Services 命名空间类，不受影响。若加入后出现既有测试失败，需逐一判定是否为「测试需同步更新」而非「代码违规」。

**验证**: `dotnet test tests/LYBT.Tests.Architecture/` 全部通过

---

## Task B：03-server.md 文档更新（13 项纯文档）

> 统一在 `docs/03-architecture/03-server.md` 执行。逐节修改，最后追加变更记录。

### Task B1: §概述 + 架构图（P0-2, P1-5）

**Files:** Modify `docs/03-architecture/03-server.md`

- L5 概述改写为：模块化单体架构；8 个业务模块；6 个模块（Auth/Users/Patients/Herbs/Formula/Registration）使用 MediatR CQRS（部分模块 Service 处理 trivial CRUD + MediatR 处理复杂命令），MedicalCase 使用 Command/Query/State 三 Service 拆分，Reports 为只读聚合查询模块。Prescriptions 移除说明保留。
- 架构图（L9-34）：
  - Core 层移除 `Entities` 节点（改为在 Shared 层标注 `Shared/LYBT.Entities`，实体已移出 Core）
  - 在模块旁标注实际架构模式（如 Auth* CQRS、MedicalCase* Service 拆分、Reports* 直连查询）
  - `Services` 层节点名保持 `LYBT.WebAPI`

### Task B2: §Core 层 LYBT.Entities（P1-5）

- L59-84：补一句「实际位置 `src/Shared/LYBT.Entities/`（2026-08 实体源统一后移出 Core）」；目录结构更新为实际 10 个目录（Auth/Common/Consultations/Formulas/Herbs/MedicalCases/Patients/Prescriptions/Registrations/Users）
- L68 例外段：`MedicalCaseModel` 域方法改为 `Complete()/Suspend()/SoftDelete()/UpdateConsultation()`（P2-14）

### Task B3: §Core 层 LYBT.Infrastructure（P1-4, P1-8, P1-9, P2-13）

- L92 BaseRepository 描述：21 个公开方法 → 5 个核心方法（GetByIdAsync/AddAsync/UpdateAsync/DeleteAsync/SaveChangesAsync），并注明「复杂查询由各模块 Repository 自定义」
- L93-98 跨模块服务清单重写：
  - `ICrossModuleService` —— **统一接口**（非 [Obsolete]），替代旧的 `IPatientCrossModuleService/IHerbCrossModuleService/IUserCrossModuleService`；实现 `CrossModuleService` 委托各域服务
  - `IPatientCrossModuleService / IHerbCrossModuleService / IUserCrossModuleService` —— 域接口（旧接口文件保留，由统一接口委托/并存）
  - `IMedicalCaseCrossModuleService` —— 医案域接口（供 Patients 引用检查）
  - `IRegistrationCrossModuleService` —— 挂号域接口（供 MedicalCase）
  - `ICrossModuleAuthService` —— **代码中不存在**（grep 验证 0 匹配），从文档删除该行
- L103-126 目录结构更新为实际 14 目录（Caching/Configuration/Constants/Data/ExceptionHandling/Extensions/Interfaces/Logging/Migrations/Repositories/Serialization/Services/SharedKernel/Web）
- L124 `Web/ApiErrorCodes.cs` → 删除，改为指向 `LYBT.Shared.Models/Primitives/ErrorCodes/`（ErrorCode.cs 枚举 + ErrorCategory + ErrorMessages + Extensions）
- L128 BaseRepository 21 方法清单 → 替换为 5 方法
- L132-133 分页模板方法段 → **删除**（grep 验证 `ApplyKeywordFilter/ApplyDefaultOrdering` 0 匹配，模板方法模式已不存在）

### Task B4: §Module 层标准目录结构（P1-3）

- L139-150 标准结构重写为实际两种形态：
  - CQRS 模块：`Application/ Domain/ Infrastructure/ Interfaces/ Services/`
  - MedicalCase：`Controllers/ Interfaces/ Mappers/ Repositories/ Services/`
  - Reports：`Domain/ Infrastructure/ Interfaces/`

### Task B5: §Module 层模块清单（P0-2, P1-7）

- L154-163 表格整列更新：

| 模块 | 架构模式 | 跨模块通信 |
|------|----------|------------|
| Auth | MediatR CQRS | ICrossModuleService |
| Users | Service + MediatR | IUserCrossModuleService（供 MedicalCase/Auth） |
| Patients | Service + MediatR | IMedicalCaseCrossModuleService（引用检查） |
| Herbs | Service + MediatR | IHerbCrossModuleService |
| Formula | Service + MediatR | ICrossModuleService |
| MedicalCase | Service 拆分（Command/Query/State） | IRegistrationCrossModuleService + ICrossModuleService |
| Registration | 纯 MediatR CQRS | IRegistrationCrossModuleService |
| Reports | Controller → Repository（只读聚合） | - |

### Task B6: §MedicalCase CQRS 服务清单（P1-6）

- L171-180 表格替换为实际 5 接口：`IMedicalCaseCommandService（含 Prescription 内部操作）/ IMedicalCaseQueryService / IMedicalCaseStateService / IMedicalCaseReferenceRepository / IMedicalCaseRepository`；删除 Permission/Audit/Rules/ServiceHelper 行（ServiceHelper 存在但为文件非接口，可注明）
- L181 适用标准段保留但补充「MedicalCase 采用 Service 拆分而非 MediatR（2026-08-02 决策：MediatR 保留用于复杂业务，trivial CRUD 直接注入）」

### Task B7: §Controller 规范示例（P2-11）

- L203-222 示例更新为实际模式：`[ApiVersion("1")] + [Route("api/v{version:apiVersion}/...")]`；CRUD 控制器继承 `BaseCrudController`（注入 ISender 用于 MediatR 命令），并附 `ReportsController` 注入 Service 接口的正确示例
- L225 规则「Controller 注入 Service 接口，禁止注入 Repository 或 DbContext」**保留**（这是 P0-1 修复后应满足的规则）

### Task B8: §模块独立 DbContext（P2-12，新增小节）

- 在「依赖注入」或「数据库约定」前新增小节：5 个模块独立 DbContext（Auth/Users/Herbs/Formula/Reports，经 `ConnectionStringResolver` 三级回退）；Patients/MedicalCase/Registration 复用共享 `AppDbContext`；架构测试 P02/P10 豁免自有 DbContext 的模块内 Repository

### Task B9: §错误码表（P1-9 补充）

- L317-326 表格：删除「7xxxx 数据同步」行（代码中不存在），新增「8xxxx 挂号管理 801xx~803xx ~3」；注明错误码枚举实际位于 `LYBT.Shared.Models/Primitives/ErrorCodes/ErrorCode.cs`

### Task B10: §BaseService 层次（P1-10）

- L334-350 重写为实际状态：`BaseService` 仅提供 ILogger 注入；仅 MedicalCase 的 Command/Query/State 继承 `BaseService<MedicalCase>`；其余模块 Service 直接实现接口；删除 ExecuteAsync/ValidateAsync 能力描述
- L352-362 返回值类型 `Result<T>` 描述保留（代码确实返回 Result<T>）

### Task B11: §变更记录（追加 v2.3）

- 变更记录表追加一行：`2026-08-05 | v2.3 | 文档与代码全面对齐（14 项：架构模式/目录结构/BaseRepository 5 方法/Entities 位置/跨模块方向/错误码位置/BaseService 状态/模块级 DbContext 等）`

---

## Task C：验证与提交

### Task C1: 全量验证

- [ ] `dotnet build LYBTZYZS.sln --no-incremental` → 0 错误 0 警告
- [ ] `dotnet test tests/LYBT.Tests.Architecture/` → 全部通过
- [ ] 逐条核对 03-server.md 与代码（验收标准 3）

### Task C2: 提交

```bash
git add src/Server/Modules/LYBT.Module.Reports/Interfaces/IReportService.cs \
        src/Server/Modules/LYBT.Module.Reports/Services/ReportService.cs \
        src/Server/Modules/LYBT.Module.Reports/ReportsModule.cs \
        src/Server/Services/LYBT.WebAPI/Controllers/ReportsController.cs \
        tests/LYBT.Tests.Architecture/ServerArchTests.cs \
        docs/03-architecture/03-server.md \
        docs/03-architecture/webapi-arch-fix-plan.md
git commit -m "fix(webapi): align 03-server.md with actual architecture and add ReportService"

# 若 A5 单独提交（建议分两个 commit：文档与代码分开）
git commit -m "docs(server): sync 03-server.md with actual architecture (14 findings)"
git commit -m "fix(webapi): ReportsController injects IReportService instead of repository"
```

### Task C3: 总账更新

- [ ] `docs/03-architecture/13-project-master-plan.md` §八 追加完成行（Commit SHA）+ §九 追加决策记录一行

---

## 风险与说明

1. **A5 加入 Registration 到 ServerAssemblies**：Registration 无 Service 层，可能触发 `Services_Should_Have_Service_Suffix` 之外无影响；若有既有测试失败，逐条判定后同步更新测试（不是放宽约束）
2. **不触碰**：各模块 AGENTS.md、其他 03-*.md 文档、Desktop 代码——属 A-01/C-05 文档同步范畴，另行处理
