# Server 端业务模块类设计扫描报告

> **日期**: 2026-08-10
> **范围**: `src/Server/Modules/` 全部 10 个目录 + `src/Server/Core/LYBT.Infrastructure/`（基类，不含 Migrations）
> **方法**: 并行只读扫描（7 scout 分模块深扫）+ 行数脚本复核 + sln/csproj 交叉验证
> **结论性质**: 事实清单 + 可统一项建议，未修改任何代码

---

## 〇、总览

| 项目 | 文件数(.cs) | 行数 | 分层结构 | Mapper | Validator | 超大类型(>500) |
|---|---|---|---|---|---|---|
| LYBT.Module.Catalog | 53 | 2,264 | Application+Infra+Interfaces+Services | Mapperly×1 | FluentValidation×14 | 无 |
| LYBT.Module.Identity | 54 | 2,931 | Application+Controllers+Infra+Services | Mapperly×1 | FluentValidation×3 | 无 |
| LYBT.Module.MedicalCase | 22 | 3,410 | **无 Application 层**（Services+Repositories+Controllers 根层） | Mapperly×1 | 外置 Shared | 无* |
| LYBT.Module.Patients | 29 | 1,027 | Application+Infra+Interfaces+Services | Mapperly×1 | FluentValidation×3 | 无 |
| LYBT.Module.Registration | 27 | 1,236 | Application+Controllers+Hubs+Infra+Services | Mapperly×1 | FluentValidation×2 | 无 |
| LYBT.Module.Reports | 7 | 522 | **仅 Interfaces+Services+Infra**（无 Application） | 无（内联 Select） | 无 | 无 |
| LYBT.Infrastructure | 58 | 3,728 | 基类层（Data/Repositories/Services/Web/…） | 无 | ValidationBehavior | 无 |
| **合计** | **250** | **15,118** | | | | |
| LYBT.WebAPI（宿主） | 26 | — | 10 控制器 + 6 扩展 + 中间件 | — | — | 无 |

> \* MedicalCaseRepository 4 个 partial 片段合计 662 行，若按类型合并计则超 500（按文件拆分设计，单文件 ≤240 行）。

**4 个旧模块目录（Auth/Users/Herbs/Formula）不在上表**——见 §二，全部为仅剩 bin/obj 的空壳。

---

## 一、活跃模块类清单与分层职责

### 1.1 LYBT.Module.Catalog（53 文件 / 2,264 行）— Herbs+Formulas 合并产物

| 层 | 文件数 | 行数 | 关键类型 |
|---|---|---|---|
| Application/Commands | 21 | 943 | 泛型命令已合并（`CatalogCommands.cs`：Create/Update/Delete/Restore/ToggleEntityCommand<TEntity,TDetail>）；**Handler 未合并**：`HerbCommandHandler`(123)+`FormulaCommandHandler`(117) 逐方法孪生；8 个 Batch\*Handler（Enable/Disable/Delete × Herbs/Formulas）孪生；2 个 BatchImport Handler（109/128）孪生 |
| Application/Mappers | 1 | 118 | `CatalogDtoMapper`（Mapperly 合并 HerbDtoMapper/FormulaDtoMapper） |
| Application/Queries | 4 | 152 | CheckHerbReference(+Batch)、GetPendingValidation |
| Application/Validators | 14 | 353 | 7 对 Herb/Formula 孪生验证器 |
| Services | 2 | 155 | `CatalogQueryService<TEntity,TListDto,TDetailDto>`（泛型合并）+ `CatalogCrossModuleService` |
| Interfaces | 5 | 101 | `ICatalogRepository<T>`（泛型合并）+ `IHerbRepository`/`IFormulaRepository`（仍分轨）+ `IHerbReferenceRepository`（public 接口 / internal 实现） |
| Infrastructure | 5 | 366 | `CatalogDbContext`（合并）、`CatalogRepositoryBase<T>`（合并）、`HerbRepository`/`FormulaRepository`（仍分轨）、`HerbReferenceRepository`（internal） |
| 根 | 1 | 76 | CatalogModule |

**职责**：药材（Herb）+ 验方（Formula）目录管理。无 Controllers/Hubs（纯领域模块，仅注册服务；控制器在宿主 WebAPI 继承）。

### 1.2 LYBT.Module.Identity（54 文件 / 2,931 行）— Auth+Users 合并产物

| 层 | 文件数 | 行数 | 关键类型 |
|---|---|---|---|
| Application/Commands | 32 | 1,307 | 16 命令+16 Handler（Login/Logout/RefreshToken/AutoLogin/ValidateToken=Auth 轨；Create/Update/Delete/Toggle/Restore/Change\*/Reset/Batch\*User=Users 轨）；3 个 Batch Handler 继承 `BatchOperationHandlerBase<ApplicationUser>` |
| Application/Queries | 3 | 93 | ValidateTokenQuery+Handler+Result |
| Application/Validators | 3 | 89 | Create/Update/ChangeProfileUserValidator（16 命令仅 3 个有验证器） |
| Application/Mappers | 1 | 87 | `IdentityMapper`（Mapperly 合并 AuthUserMapper+UserMapper+UserCrossModuleMapper） |
| Controllers | 1 | 304 | `BaseUsersController : BaseCrudController`（抽象，宿主继承） |
| Services | 4 | 676 | `JwtService`(378, 模块最大)、`UserService`、`SecurityAuditService`、`IdentitySeedData` |
| Infrastructure | 4 | 250 | `IdentityDbContext`（合并 Auth+Users DbContext）、3 仓储 |
| Interfaces | 5 | 117 | 模块内 5 接口；**IUserService 定义在 LYBT.Infrastructure.Services.CrossModule** |

**职责**：认证（JWT/会话/审计）+ 用户管理。命名空间已统一 `LYBT.Module.Identity.*`，类型级 Auth/Users 双轨仍可分辨（见 §三）。

### 1.3 LYBT.Module.MedicalCase（22 文件 / 3,410 行）— **结构差异最大**

| 层 | 文件数 | 行数 | 关键类型 |
|---|---|---|---|
| Services（根层） | 8 | 1,796 | `MedicalCaseQueryService`(406)/`CommandService`(355, partial×2)/`StateService`(353) 方法级 CQRS 拆分；`PrescriptionItemService`/`MedicalCasePrescriptionService`（无接口）；`MedicalCaseServiceHelper`(200)；`MedicalCaseCrossModuleService` |
| Repositories（根层） | 5 | 715 | `MedicalCaseRepository`(internal partial×4：主/Update/PendingCases/AuditLogs)+`MedicalCaseReferenceRepository` |
| Controllers | 1 | 158 | `BaseMedicalCasesController` |
| Interfaces | 5 | 468 | 5 接口（无 CrossModule 接口，`IMedicalCaseCrossModuleService` 在 Infrastructure） |
| Mappers | 1 | 173 | `MedicalCaseMapper`（Mapperly+手动 Enrich 混合） |
| Infrastructure | 1 | 63 | `MedicalCaseDbContext` |

**职责**：医案三态流转（暂存/进行中/完成）+ 处方条目 + 审计。
**结构差异**：无 Application/ 层、无 MediatR IRequest 类型（MediatR 包引而不用）；CQRS 以 Service 方法级拆分；partial 按职责拆文件。

### 1.4 LYBT.Module.Patients（29 文件 / 1,027 行）

| 层 | 文件数 | 行数 | 关键类型 |
|---|---|---|---|
| Application/Commands | 14 | 512 | 7 命令+7 Handler（Create/Update/Delete/Restore/Toggle/BatchDelete/BatchImport） |
| Application/Queries | 4 | 116 | CheckPatientReference(+Batch) |
| Application/Validators | 3 | 74 | Create/Update/Restore（4 命令无验证器：Delete/Batch\*/Toggle） |
| Application/Mappers | 1 | 38 | `PatientMapper`（Mapperly+手写 ToEntity 工厂） |
| Services | 2 | 95 | `PatientService`(internal 只读) + `PatientCrossModuleService` |
| Infrastructure | 2 | 110 | `PatientRepository` + `PatientsDbContext` |
| Interfaces | 2 | 53 | IPatientRepository + IPatientService |

**职责**：患者 CRUD/软删除/批量导入/引用检查。无 Controller（宿主经 MediatR 调用）。

### 1.5 LYBT.Module.Registration（27 文件 / 1,236 行）

| 层 | 文件数 | 行数 | 关键类型 |
|---|---|---|---|
| Application/Commands | 8 | 346 | Create/StartVisit/QuickVisit/Cancel + Handler（QuickVisit 主构造函数注入，其余传统注入） |
| Application/Queries | 6 | 158 | GetRegistrations/GetRegistration/GetWaitingQueue + Handler |
| Application/Validators | 2 | 54 | CreateRegistrationValidator + QuickVisitCommandValidator（命名风格不一致） |
| Controllers | 1 | 146 | `BaseRegistrationsController` |
| Hubs | 2 | 97 | `RegistrationHub` + `RegistrationConnectionManager`（SignalR 医生分组推送） |
| Services | 2 | 126 | `RegistrationCrossModuleService` + `NotificationService` |
| Infrastructure | 2 | 169 | `RegistrationRepository` + `RegistrationDbContext` |
| Mappers | 1 | 25 | `RegistrationMapper`（Mapperly） |

**职责**：挂号/接诊/等待队列 + SignalR 实时推送。

### 1.6 LYBT.Module.Reports（7 文件 / 522 行）— **最简化结构**

| 层 | 文件数 | 行数 | 关键类型 |
|---|---|---|---|
| Interfaces | 2 | 84 | IReportService + IReportRepository |
| Services | 2 | 176 | `ReportService`(124) + `ReportTimeBuckets`(internal) |
| Infrastructure | 2 | 241 | `ReportRepository`(214) + 4 个中间 record |
| 根 | 1 | 28 | ReportsModule |

**职责**：只读报表聚合。无 Application/Controllers/Validators/Mappers；DTO 组装内联 `Select/Zip`。**csproj 引用 MediatR 包但模块内无任何 IRequest 使用**（疑似冗余引用）。

---

## 二、新旧模块并存核查（重点：旧目录是否删净）

### 2.1 结论：**旧模块源码已删净，但 4 个空壳目录残留（仅 bin/obj 构建缓存）**

| 旧模块 | sln 中 | 磁盘源码 | 磁盘残留 | git 追踪 | 引用方 |
|---|---|---|---|---|---|
| LYBT.Module.Auth | ❌ 不在 | ❌ 无 | 仅 bin/ + obj/ | ❌ 未追踪 | 无 |
| LYBT.Module.Users | ❌ 不在 | ❌ 无 | 仅 bin/ + obj/ | ❌ 未追踪 | 无 |
| LYBT.Module.Herbs | ❌ 不在 | ❌ 无 | 仅 bin/ + obj/ | ❌ 未追踪 | 无 |
| LYBT.Module.Formula | ❌ 不在 | ❌ 无 | 仅 bin/ + obj/ | ❌ 未追踪 | 无 |

**证据**：
- `LYBTZYZS.sln` 中 `Module.Auth|Users|Herbs|Formula` 命中数 = **0**
- 4 个目录 `ls -la` 只有 `bin/` `obj/` 两个子目录，无 .csproj、无源码
- `git ls-files src/Server/Modules/` 无任何 Auth/Users/Herbs/Formula 路径（git 未追踪空壳）
- 全仓库 csproj 无引用旧模块（仅引用 6 个新模块）

**新模块合并映射**（类型级双轨仍可分辨，见 §三）：
- Auth + Users → `LYBT.Module.Identity`（程序集名 `LYBT.Module.Identity`；IdentityModule 注释 "A-31-C3a 合并 AuthModule + UsersModule"）
- Herbs + Formulas → `LYBT.Module.Catalog`（A-31-C3b）
- 命名空间已统一为 `LYBT.Module.Identity.*` / `LYBT.Module.Catalog.*`

### 2.2 待清理建议（本次未执行）

删除 4 个空壳目录（仅含构建产物，git 未追踪，删除零风险）：
```bash
rm -rf src/Server/Modules/LYBT.Module.Auth src/Server/Modules/LYBT.Module.Users \
       src/Server/Modules/LYBT.Module.Herbs src/Server/Modules/LYBT.Module.Formula
```

---

## 三、类设计模式一致性

### 3.1 Repository 命名/结构

| 观察 | 详情 | 影响 |
|---|---|---|
| ✅ 基类统一 | `BaseRepository<TEntity,TDbContext>`（Infrastructure，169 行）单份定义；模块派生：CatalogRepositoryBase<T>、PatientRepository、MedicalCaseRepository、RegistrationRepository | 良好收敛 |
| ⚠️ 目录位置不一致 | Patients/Registration/Catalog 仓储在 `Infrastructure/`；MedicalCase 仓储在根层 `Repositories/` | 结构漂移 |
| ⚠️ 可见性不一致 | MedicalCase/HerbReference 仓储 `internal`，其余 `public` | 命名审计困惑 |
| ⚠️ Catalog 双轨 | `HerbRepository`+`IHerbRepository` 与 `FormulaRepository`+`IFormulaRepository` 并存（共享 CatalogRepositoryBase 但分页/查重/GetById 各写一遍） | 合并残留，最直接消重点 |
| ⚠️ CatalogRepositoryBase 桥接 | 重声明 GetPagedAsync（已有 `QueryablePagingExtensions` 实现）/ExistsAsync（已有 protected BaseRepository.ExistsAsync） | 可上收候选 |
| ✅ 接口收敛 | 全部 `I*Repository : IRepository<T>`（IRepository 仅 4 方法：GetById/Add/Update/Delete） | 一致 |

### 3.2 Service 命名（XxxService / XxxManager / XxxRepository 混用）

| 观察 | 详情 |
|---|---|
| ✅ Service 为主 | 各模块统一 `XxxService` + `IXxxService`（Query/Command/State/CrossModule 语义后缀） |
| ✅ 无 Manager 混用 | 全 Server 仅 `RegistrationConnectionManager`（SignalR 连接映射，用途贴切） |
| ⚠️ MedicalCase 例外 | `MedicalCaseServiceHelper`（static Helper 后缀）；`PrescriptionItemService`/`MedicalCasePrescriptionService` 无接口直接注册具体类 |
| ⚠️ 跨模块接口命名中途态 | Infrastructure CrossModule 混用：`ICatalogService`/`IUserService`（已去 CrossModule 后缀）vs `IPatientCrossModuleService`/`IMedicalCaseCrossModuleService`/`IRegistrationCrossModuleService`（保留旧名） |
| ⚠️ 接口位置不一致 | `IUserService`/`IMedicalCaseCrossModuleService`/`IPatientCrossModuleService` 等跨模块接口定义在 **LYBT.Infrastructure.Services.CrossModule**，实现在各模块 Services/ —— 接口与实现跨程序集分离（有意的跨模块通道 A-31-C8，但搜索代码时易困惑） |

### 3.3 Command/Query Handler 结构

| 模式 | 采用模块 | 未采用模块 |
|---|---|---|
| MediatR `IRequest` + `IRequestHandler`（record 命令+Handler 平铺） | Identity / Patients / Registration / Catalog（部分） | **MedicalCase**（无 IRequest，方法级 Service 拆分；MediatR 包引而不用） |
| Service 方法级 CQRS（CommandService/QueryService/StateService） | MedicalCase | 其余 |
| **Reports** | 无 Command/Query（只读，Service 直查） | — |

- ✅ 命令/Handler 一一对应（Identity 16/16、Patients 7/7、Registration 4/4）
- ⚠️ **Catalog 合并中途态最突出**：命令已泛型合并（`CatalogCommands.cs`），Handler 未合并（`HerbCommandHandler`/`FormulaCommandHandler` 逐方法孪生，123/117 行）；构造函数风格混用（传统字段注入 vs 主构造函数）
- ⚠️ Handler 命名：Catalog `CheckHerbReferenceQueryHandler`/`GetPendingValidationQueryHandler` 是查询处理器但名中无 Query

### 3.4 Validator 存在性

| 模块 | 数量 | 覆盖率 | 备注 |
|---|---|---|---|
| Catalog | 14 | 全覆盖（7 对孪生） | 双源注册（Shared DTO 级 + 模块命令级）；命名三种风格混用（CreateHerbValidator / HerbBatchImportCommandValidator / BatchEnableHerbsValidator） |
| Identity | 3 | 16 命令仅 3 个有（Create/Update/ChangeProfile） | Login/Logout/Refresh/AutoLogin/Reset/Batch*/Toggle 无验证器 |
| Patients | 3 | 7 命令仅 3 个有 | Delete/Batch*/Toggle 无 |
| Registration | 2 | 4 命令仅 2 个有 | StartVisit/Cancel 仅 Guid 参数，可接受；命名不一致（CreateRegistrationValidator vs QuickVisitCommandValidator） |
| MedicalCase | 0（模块内） | 验证器外置 Shared（`MedicalCaseInputDtoValidator`） | FluentValidation 包引而模块内无验证器 |
| Reports | 0 | 无验证需求 | — |

框架统一 FluentValidation，经 `ValidationBehavior<TRequest,TResponse>`（Infrastructure）接入 MediatR 管道 ✅。

### 3.5 Mapper 用法（Mapperly vs 手写）

| 模块 | Mapper | 模式 |
|---|---|---|
| Catalog | `CatalogDtoMapper` | **Mapperly** + 4 个手写方法（ToEntity 走领域工厂、ToFormulaDetailDto/ToHerbItemDto `[UserMapping(Default=false)]`） |
| Identity | `IdentityMapper` | **Mapperly** + 2 个手写 `[UserMapping]`；未设 `AutoUserMappings=false`（与其余模块不一致） |
| MedicalCase | `MedicalCaseMapper` | **Mapperly** + 手动 Enrich（嵌套/计算字段） |
| Patients | `PatientMapper` | **Mapperly** + 手写 ToEntity 工厂 |
| Registration | `RegistrationMapper` | **Mapperly** |
| Reports | 无 | 内联 Select/Zip |

**结论**：Mapperly 已全模块统一（AutoMapper 已移除，ADR-0011），手写仅保留含业务逻辑的 ToEntity/Enrich。无手写静态映射类残留。✅ 一致性良好，唯一差异是 IdentityMapper 未设 `AutoUserMappings=false`。

---

## 四、行数统计与超大类型（>500 行）

### 4.1 模块行数（.cs，排除 obj/bin；Infrastructure 排除 Migrations）

| 模块 | 文件数 | 行数 | 最大文件 | 行数 |
|---|---|---|---|---|
| MedicalCase | 22 | 3,410 | MedicalCaseQueryService.cs | 406 |
| Identity | 54 | 2,931 | JwtService.cs | 378 |
| Catalog | 53 | 2,264 | BatchImportFormulasCommandHandler.cs | 128 |
| Registration | 27 | 1,236 | BaseRegistrationsController.cs | 146 |
| Patients | 29 | 1,027 | BatchImportPatientsCommandHandler.cs | 121 |
| Reports | 7 | 522 | ReportRepository.cs | 214 |
| Infrastructure | 58 | 3,728 | ProductionConfigurationValidator.cs | 436 |

### 4.2 超大类型

**扫描范围内无单个 >500 行文件**。接近阈值：
- `ProductionConfigurationValidator.cs`（Infrastructure，436 行，含 6 个类型）
- `MedicalCaseQueryService.cs`（406）/ `JwtService.cs`（378）/ `MedicalCaseCommandService.cs`（355）/ `MedicalCaseStateService.cs`（353）
- ⚠️ `MedicalCaseRepository` 4 个 partial 片段合计 658 行（按类型合并计超 500，按文件拆分 ≤239）——partial 拆分使其单文件可控，但类型总体量需注意

---

## 五、可统一项汇总（按优先级）

### P0 — 结构性不一致（影响模块间一致性理解）

1. **Catalog Handler/Validator/批量层双轨**：`HerbCommandHandler`/`FormulaCommandHandler` 逐方法孪生（命令已泛型化）；14 验证器 7 对孪生；8 个 Batch Handler 孪生（共享 BatchOperationHandlerBase 但 Enable/Disable/Delete 各写一遍）→ 泛型化或抽 TEntity 基类
2. **MedicalCase 分层独树一帜**：无 Application/ 层、无 MediatR IRequest、仓储在根层 Repositories/、partial 拆文件 → 决定是否对齐标准分层（或文档明确其差异化理由）
3. **旧模块空壳目录**：Auth/Users/Herbs/Formula 仅剩 bin/obj，git 未追踪 → 直接删除（零风险）
4. **命名空间 vs 项目名**：`LYBT.Module.MedicalCase`（目录/项目名单数）vs 命名空间/程序集 `LYBT.Module.MedicalCases`（复数）；Reports 同理（`LYBT.Module.Reports` 命名空间复数但一致性无问题）→ 统一

### P1 — 命名/风格不一致

5. **跨模块接口后缀**：`ICatalogService`/`IUserService` vs `IPatientCrossModuleService`/`IMedicalCaseCrossModuleService`/`IRegistrationCrossModuleService` → 统一去/留 CrossModule 后缀
6. **Validator 命名**：三种风格（XxxValidator / XxxCommandValidator / BatchEnableXxxsValidator）；MedicalCase 验证器外置 Shared vs 其余模块内 Application/Validators
7. **Handler 构造函数风格**：主构造函数（Registration QuickVisit、Catalog BatchImport）与字段注入（多数）混用
8. **仓储目录/可见性**：Infrastructure/ vs 根层 Repositories/；internal vs public
9. **Identity 验证器覆盖缺口**：16 命令仅 3 验证器（登录等认证命令无验证器，LoginRequestValidator 在 Shared 兜底）

### P2 — 基类层（Infrastructure）微优化

10. **UserConfiguration 手工复制 BaseEntityConfiguration** 整块审计/并发/软删配置（ApplicationUser 非 BaseEntity 无法继承）→ 提取 ConfigureAuditFields helper 或 IAuditableEntity 契约
11. **CreatedBy 必填三处重复**（Consultation/MedicalCase/Prescription Configuration）→ BaseEntityConfiguration 可覆写开关
12. **枚举 HasConversion<int> 六处重复**（AuthSession/Formula/Herb/Patient/Registration/User）→ EF Core 8 默认枚举→int，可移除或集中扩展
13. **Reports csproj 引用 MediatR 但模块无 IRequest**（疑似冗余包引用）；MedicalCase 同理（MediatR 引而不用）

### P3 — 文档债务（与代码脱节，不影响编译）

14. Infrastructure README（637 行）描述已删除的 IBaseRepository/Options/Security/ApiErrorCodes 等
15. Patients/Registration 模块 README/AGENTS 描述旧三层结构（PatientService 600/987 行、Repositories//Mapping/ 目录均不存在）
16. Catalog 注释残留 HerbsModule/FormulaModule、HerbDtoMapper/FormulaDtoMapper 等旧名（仅注释层）

---

## 六、依赖方向核对

- Infrastructure → Shared（Entities/Configuration/Models/Logging），**不引用任何模块** ✅（模块 → Infrastructure 单向）
- 模块 → Infrastructure + Entities + Shared.Models（+ 个别 Shared.ExceptionHandling：MedicalCase/Registration）
- Patients csproj 中旧 `LYBT.Module.MedicalCase` 引用已注释（Phase 4 经 `IMedicalCaseCrossModuleService` 解耦）✅
- 跨模块通信唯一通道 = Infrastructure/Services/CrossModule 接口（A-31-C8）✅

---

## 附：WebAPI 宿主层（参考）

`LYBT.WebAPI`：10 控制器（Identity/Users/Patients/Catalog/MedicalCases/Registrations/Reports/Health/Diagnostics/Configuration + Deploy）+ 6 个 Extensions + 2 Middleware + Program。控制器多为模块内 Base\*Controller 的继承实现。本地模式 LocalWebAPI 复用同一批 Server 模块（ADR-0010，6 模块）。

---

*本报告为只读扫描产物，未修改任何代码/文档。行数经 Python 脚本复核（utf-8 逐行计数）。*
