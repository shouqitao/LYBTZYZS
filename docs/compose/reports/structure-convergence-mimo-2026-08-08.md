# 收敛审查报告（Mimo Code 独立分析）

> 任务：A-26 蓝图对齐 + 模式收敛 + 定义收敛（全面审查）
> 派发基线：`docs/compose/specs/task-a26-blueprint-convergence-2026-08-08.md`（v1.0）
> 审查者：Mimo Code（独立分析者 #2，与技术总监报告区分）
> 只读审查：未修改任何代码文件；报告由技术总监统一提交。

---

## 0. 方法说明

| 项 | 值 |
|---|---|
| 审查时间点 | 2026-08-08 |
| 基线 commit | `fae082ae7`（文档规范收敛）＋ 任务书 commit `1a8dc4fd2` |
| 范围 | `src/Shared`（5 项目）+ `src/Server`（10 项目）+ `src/Client/Desktop`（16 项目）；排除 tests/docs/Tools/Migrations |
| 设计态基准 | `docs/03-architecture/14-structure-design-blueprint.md` v1.2（SSOT） |
| 符号级工具 | serena（find_symbol / find_referencing_symbols / get_diagnostics）+ codebase-memory 知识图谱（28,122 节点） |
| 初筛工具 | ripgrep / PowerShell 文件统计（仅初筛，所有关键发现均经符号级复核） |
| 文件数口径 | `.cs` 文件数，排除 `bin/obj`（蓝图 §1/§2/§3 表格的「文件数」列按此口径比对） |
| 已知前置 | A-16~A-25、Q-01~Q-04 报告（任务书 §背景）；已定案决策不重新评判（双轨/模块自治/MedicalCase Service 化/用户自主切换） |

**工具可信度说明**：codebase-memory 图谱索引存在**过期**（索引内含 A-21/A-23 已删除的 Mapper 文件，如 `LYBT.Desktop.Users/Mappers/UserMapper.cs`、`LYBT.Desktop.Patients/Repositories/PatientRepository.cs` 中的 `PatientListToDetailMapper`、Infrastructure `ApiService.cs` 等，文件系统已不存在，git 历史 876581164/4db419c11 可证）。本报告所有「代码实际」均以**文件系统 + git 历史 + serena 符号解析**为准，图谱仅用于调用链/继承链交叉验证。

---

## 1. 蓝图对齐偏差清单（维度 1）

### 1.1 项目存在性与文件数（蓝图 §1/§2/§3 表格）

**结论先行：34 项目全部存在；Server 层 15 项目文件数与蓝图完全一致（100%）；Shared 层 4/5 一致；Desktop 层 9/16 一致，7 处小幅偏差（均为「蓝图多于实际」，且多数可归因于 A-21/A-23/A-24 清理后蓝图未回写）。**

| 项目 | 蓝图声明 | 实际（排除 bin/obj） | 偏差 | 偏差类型 | 严重度 |
|---|---|---|---|---|---|
| LYBT.Entities | 18 | 18 | 0 | — | — |
| LYBT.Shared.Models | 120 | 120 | 0 | — | — |
| LYBT.Shared.Configuration | 25 | 25 | 0 | — | — |
| LYBT.Shared.ExceptionHandling | 7 | 7 | 0 | — | — |
| LYBT.Shared.Logging | 9 | 8 | -1 | 蓝图过时（A-24 机制残留清理后未回写，见蓝图 v1.2 变更记录 §6 的 -1013 行清理） | P2 |
| LYBT.Infrastructure | 87 | 87 | 0 | — | — |
| LYBT.Module.Auth | 26 | 26 | 0 | — | — |
| LYBT.Module.Users | 29 | 29 | 0 | — | — |
| LYBT.Module.Patients | 23 | 23 | 0 | — | — |
| LYBT.Module.Herbs | 22 | 22 | 0 | — | — |
| LYBT.Module.Formula | 21 | 21 | 0 | — | — |
| LYBT.Module.MedicalCase | 22 | 22 | 0 | — | — |
| LYBT.Module.Registration | 27 | 27 | 0 | — | — |
| LYBT.Module.Reports | 7 | 7 | 0 | — | — |
| LYBT.WebAPI | 30 | 30 | 0 | — | — |
| LYBT.Desktop.Contracts | 79 | 78 | -1 | 蓝图过时（A-21 下沉/清理） | P2 |
| LYBT.Desktop.Foundation | 72 | 70 | -2 | 蓝图过时（A-18 P1-4 / A-24 清理） | P2 |
| LYBT.Desktop.Infrastructure | 101 | 92 | **-9** | 蓝图过时（A-23c 孤儿类 D=29 清理 + A-21/C1 LocalData 废弃，commit 4db419c11） | P2 |
| LYBT.Desktop.Controls | 42 | 41 | -1 | 蓝图过时 | P2 |
| LYBT.Desktop.Printing | 12 | 12 | 0 | — | — |
| LYBT.LocalWebAPI | 25 | 25 | 0 | — | — |
| LYBT.Desktop.Auth | 10 | 10 | 0 | — | — |
| LYBT.Desktop.Users | 15 | 15 | 0 | — | — |
| LYBT.Desktop.Patients | 25 | 18 | **-7** | 蓝图过时（A-23c 清理，commit 4db419c11） | P2 |
| LYBT.Desktop.Herbs | 13 | 13 | 0 | — | — |
| LYBT.Desktop.Formula | 16 | 14 | -2 | 蓝图过时（A-21/M2 映射清理） | P2 |
| LYBT.Desktop.MedicalCase | 50 | 49 | -1 | 蓝图过时 | P2 |
| LYBT.Desktop.Registration | 9 | 9 | 0（但项目名已复数化，见 1.2） | 蓝图过时（§3.2 表格仍写单数 `LYBT.Desktop.Registration`，实际 `LYBT.Desktop.Registrations`） | P2 |
| LYBT.Desktop.Admin | —（§3.3 未给文件数） | 17 | — | 蓝图表格缺文件数列 | P2 |
| LYBT.Desktop.Clinical | — | 21 | — | 同上 | P2 |
| LYBT.Desktop.Shell | 49 | 49 | 0 | — | — |

**推理链**：Server 侧文件数 100% 命中说明蓝图 §2 表格维护良好；Desktop 侧偏差集中，根因是蓝图 v1.2（2026-08-08）只回写了 A-24 成果，未回写同日更早的 A-23c（commit 4db419c11，-29 孤儿类）与 A-21（M2 映射清理、C1 LocalData 废弃）对文件数的实际影响——蓝图 §6 变更记录与 §1/§3 文件数表格不同步。

### 1.2 命名空间单复数（蓝图 §3.2 vs 代码实际）

| 项目名（实际） | 项目内命名空间 | 一致性 |
|---|---|---|
| `LYBT.Module.Registration`（单数，csproj 无显式 AssemblyName，默认项目名） | `LYBT.Module.Registrations.*`（27 处全复数） | ❌ 项目名 vs 命名空间不一致 |
| `LYBT.Module.MedicalCase`（单数） | `LYBT.Module.MedicalCases.*`（MedicalCaseModule.cs:15 等） | ❌ 项目名 vs 命名空间不一致 |
| `LYBT.Module.Formula`（单数） | `LYBT.Module.Formulas.*`（FormulaModule.cs:13 等） | ❌ 项目名 vs 命名空间不一致 |
| `LYBT.Desktop.MedicalCase` / `LYBT.Desktop.Formula` | `LYBT.Desktop.MedicalCase.*` / `LYBT.Desktop.Formula.*`（单数） | ⚠️ 与 Server 复数命名空间跨层不一致 |
| `LYBT.Desktop.Registrations`（复数，A-25 已改） | `LYBT.Desktop.Registrations.*`（复数） | ✅ |

- **推理链**：A-25「项目名统一复数」只执行了一半——Desktop.Registration→Registrations 已改，但 Server 3 模块（Registration/MedicalCase/Formula）项目名仍单数，命名空间却是复数（A-24 期间统一命名空间时把 namespace 改复数、未同步改 csproj 项目名）。蓝图 v1.2 变更记录 §6 已注明「Registration 命名空间复数漂移不改记录 P2」，但 **MedicalCase/Formula 两处同样的漂移未记录**。且蓝图 §3.2 表格本身仍写 `LYBT.Desktop.Registration`（单数），与代码 `Registrations` 不符——蓝图过时。
- **建议**：全仓统一规则「项目名 = 根命名空间」（Server 3 模块项目名改复数：`LYBT.Module.Registrations`/`LYBT.Module.MedicalCases`/`LYBT.Module.Formulas`，或以蓝图/架构测试允许的最小成本决定方向后一次性对齐）；蓝图 §3.2 同步改 `LYBT.Desktop.Registrations`。方向建议：**以复数命名空间为准改项目名**（因命名空间已复数且 A-25 已确立复数惯例）。

### 1.3 模块结构 vs 蓝图 §2.4 / §3.2 分层规则

| 蓝图声明 | 代码实际 | 偏差类型 | 严重度 |
|---|---|---|---|
| §2.4：Server 每模块含 `Controllers/ Application/ Domain/ Infrastructure/ Interfaces/ Services/ Mappers/` 七目录 | **8 模块全部无 `Domain/` 目录**（实体全部下沉 LYBT.Entities，2026-08-02 决策）；`Application/` 仅存在于 Auth/Users/Patients/Herbs/Formula/Registration（CQRS 模块），MedicalCase 无 Application（Service 化）、Reports 无 Application；`Mappers/` 仅 MedicalCase/Registration 有（其他模块 Mapper 在 `Application/Mappers/`） | 蓝图 §2.4 是「理想模板」，与 §2.2 表格实际结构矛盾 → **蓝图模糊需澄清** | P2 |
| §2.4：`Infrastructure/` = Repository | MedicalCase 用 `Repositories/`（4 文件）+ `Infrastructure/` 只放 DbContext；其他模块 `Infrastructure/` 放 Repository+DbContext | 代码偏离（同职责两种目录名） | **P1** |
| §3.2 Users「全目录（Controls/Mappers/Models/Repositories/Services/ViewModels）」 | 无 `Mappers/` 目录（git 876581164「删 4 零引用 Mapper」后消失），无任何 Mapper 类 | 代码偏离（删除后未补目录或蓝图过时——Desktop Users 现在直接用 DTO 无 Model 层，见 §2.5） | P1 |
| §3.2 Patients「全目录 + Interfaces（D1 观察项）」 | 无 `Interfaces/`、无 `Mappers/`；`PatientRepository` 裸实现不继承任何基类（PatientRepository.cs:14） | 蓝图过时（D1 观察项未落地）+ 代码偏离（仓储裸实现，见 §2.4） | P2 |
| §3.2 Herbs「全目录」 | 无 `Mappers/` 目录 | 代码偏离（无映射层，Repository 直接用 DTO） | P2 |
| §3.2 MedicalCase「目录最全（6 子目录）」 | 实际 11 目录（Controls/Dialogs/Extensions/Interfaces/Mappers/Models/Reports/Repositories/Services/ViewModels/Views） | 蓝图过时（§3.2 未列全） | P2 |
| §3.2 Registrations「精简（Dialogs/Events/Repositories/Services/ViewModels）」 | 实际还有 `Views/`（9 文件含 XAML） | 蓝图过时 | P2 |
| §2.3 WebAPI「12 个 Controller」 | 12 个（Auth/Configuration/Deploy/Diagnostics/Formulas/Health/Herbs/MedicalCases/Patients/Registrations/Reports/Users） | ✅ 一致 | — |
| §3.1 LocalWebAPI「薄 ASP.NET Core + 复用 Server 8 模块」 | 实际含完整 12 个 Controller（与 WebAPI 同名同构）——AGENTS.md 声称「Controllers delegate to I\*Service — zero parallel implementation」，但代码是 Controller 级复制（如 LocalWebAPI/Controllers/PatientsController.cs:21 与 WebAPI/Controllers/PatientsController.cs:21 两套） | 蓝图 §3.1 描述「薄」与实际 12 Controller 不符；AGENTS.md「10 controllers」也不符（实际 12）→ **蓝图/文档模糊需澄清** | P2 |

**推理链（§2.4 核心矛盾）**：蓝图 §2.4 的七目录模板从未在任何模块完整落地（全无 Domain、Mappers 位置两种放法），而 §2.2 表格（CQRS/Service 化/只读聚合三态）才是代码实际。§2.4 应为「理想态声明」并标注例外，或直接改为三态模板；当前状态是同一蓝图内部自相矛盾。

### 1.4 依赖规则 8 条（蓝图 §0.3）逐条验证

| 规则 | 验证结果 | 证据 |
|---|---|---|
| 单向依赖 Desktop → Shared / Server → Shared | ✅ 无违反（架构测试守卫） | — |
| Shared 零反向 | ✅ 无违反 | — |
| Server 模块隔离（P07） | ✅ 无模块间 csproj 直引；跨模块全走 `Infrastructure/Services/CrossModule/` 的 `ICrossModuleService` 族（ICrossModuleService.cs:10 + IPatientCrossModuleService 等 6 个域接口） | explore-2 全量 csproj 检查 |
| Desktop 模块隔离（DP07） | ✅ Admin/Clinical 引用模块符合 §3.3 | csproj 核对 |
| **Service 禁注入 AppDbContext（P10）** | ⚠️ **守卫盲区**：`UserCrossModuleService`（UserCrossModuleService.cs:18 `_context = dbAccessor.Context`）与 `HerbCrossModuleService`（HerbCrossModuleService.cs:17）通过 **`IDbContextAccessor` 间接持有 AppDbContext 直接查询**（`_context.Users.FirstOrDefaultAsync` 等）。P10 守卫（ServerArchTests.cs:543-571）只检查**构造函数参数类型**含 AppDbContext，不检查 `IDbContextAccessor` 注入 → 守卫未覆盖 | **P1** |
| Repository 自治（A-20 后注入自己模块 DbContext） | ⚠️ 7 模块有独立 DbContext（Auth/Users/Patients/Herbs/Formula/MedicalCase/Registration），Reports 复用 AppDbContext（蓝图 §2.2 表格一致 ✅）。但 `UserCrossModuleService`/`HerbCrossModuleService` 直查 AppDbContext（见上）→ 跨模块服务绕过模块 DbContext | P1（同上） |
| Controller 继承 Base\* | ⚠️ 5 条路径而非蓝图 §2.1 声明的 3 条：BaseApiController（Auth/Configuration/Deploy/Diagnostics/Health/Reports）×6、BaseCrudController（Formulas/Herbs/Patients）×3、BaseMedicalCasesController（MedicalCases）、BaseRegistrationsController（Registrations）、BaseUsersController（Users）。模块级 Base\*（Users/Registrations/MedicalCases 三处）蓝图 §2.1 未记录 | 蓝图过时需补记（P2） |
| MVVM VM 不直连 IApiClient | ✅ 未发现 VM 直连 IApiClient（A-21 M5 已修复） | — |

### 1.5 设计原则 6 条（蓝图 §0.4）逐条验证

| 原则 | 验证结果 | 证据/备注 |
|---|---|---|
| 模块自治（ADR-0017） | ✅ 每模块三态结构完整 + 独立 DbContext | 但跨模块服务注入 AppDbContext（§1.4）侵蚀自治边界 → P1 |
| 契约单一（A-18） | ✅ `IApiClient` 唯一面；Refit 特性接口已 internal 化 | 未发现双套契约面 |
| 双轨设计（ADR-0002/0009/0010） | ✅ 双宿主共用 Service 层 | 已定案不评 |
| 映射单一（Mapperly） | ⚠️ 基本落地，但见 §2.5 手写映射残留 | — |
| 共享单源（Gender 样板） | ✅ 枚举无重复（见 §3.4） | — |
| 拒绝屎山 | ✅ 未见兼容层 | — |

---

## 2. 模式收敛清单（维度 2）

### 2.1 Controller 继承（已统一，但蓝图漏记 2 条路径）

- **现状**：12 WebAPI Controller + 12 LocalWebAPI Controller 全部挂 Base 链，无裸 Controller。链：`BaseApiController:ControllerBase`（BaseApiController.cs:15）→ `BaseCrudController`（BaseCrudController.cs:13）→ 模块级基类 `BaseUsersController`（BaseUsersController.cs:23）/`BaseRegistrationsController`（BaseRegistrationsController.cs:16）/`BaseMedicalCasesController`（BaseMedicalCasesController.cs:18）。
- **统一方向**：5 条路径本身是合理的「三层继承」设计（基础 → CRUD → 模块特化），无需合并。**蓝图 §2.1 补记 BaseUsersController/BaseRegistrationsController 两条模块级路径**（A-14 文档只记了 3 条）。严重度 **P2**（文档）。

### 2.2 Service/Handler 模式（❌ 5 个 CQRS 模块「读走 Service + 写走 Handler」双轨混用）

- **现状**（符号级核实，逐 Controller 注入+调用链）：

| 模块 | Handler 数 | Service 类 | 实际调用模式 |
|---|---|---|---|
| Auth | 6 | JwtService/SecurityAuditService/AuthCrossModuleService | 纯 MediatR（AuthController.cs:52 `_sender.Send`），Service 仅基础设施 → **单轨** ✅ |
| Users | 9 | UserService（457 行） | 读走 Service（BaseUsersController.cs:45 `_userService.GetPagedAsync`），写走 Handler → **双轨** ⚠️ |
| Patients | 7 | PatientService | 读/改走 Service（PatientsController.cs:49/65/107/182），建/删/导入走 Send（:81/134/224）→ **双轨** ⚠️ |
| Herbs | 5 | HerbService | 读/改走 Service（HerbsController.cs:47/61/110/164/184），建/删/导入走 Send（:78/138/222）→ **双轨** ⚠️ |
| Formula | 6 | FormulaService（156 行） | 读/改走 Service（FormulasController.cs:47/61/110/164），建/删/导入走 Send（:83/137/224）→ **双轨** ⚠️ |
| MedicalCase | 0 | 5 Service（Command/Query/State/Prescription/CrossModule） | 纯 Service ✅（MedicalCaseModule.cs:46-52） |
| Registration | 7 | NotificationService（SignalR 基础设施） | 纯 Handler ✅ |
| Reports | 0 | ReportService | 纯 Service ✅（ReportsController.cs:22） |

- **根因**：A-03 只把 MedicalCase 服务化；Users/Patients/Herbs/Formula 在做 CRUD 简化时**逐方法**把「简单操作」改走 Service、保留「复杂命令」走 Handler（FormulaService.cs:11 注释「替代 trivial MediatR Handler」可见意图），但 Controller 层同时注入 `ISender` + `IXxxService` 两种入口，没有统一的「何时 Service 何时 Handler」规则。
- **统一方向**（关键建议）：两种候选——
  1. **全 Service 化**（对齐 MedicalCase）：Users/Patients/Herbs/Formula 的 Controller 全走 Service，Handler 删除或下沉为 Service 内部私有方法；
  2. **全 Handler 化**（回归 CQRS）：Service 收敛为跨模块服务 + 查询服务，业务写操作全走 MediatR。
  推荐 **方案 1（全 Service 化）**：与已定案 MedicalCase 方向一致、减少一层转发、符合 A-21「3 VM 越层改走 Service」的方向惯性；但需技术总监确认（涉及已定案 CQRS 模块改向）。严重度 **P1**。

### 2.3 跨模块通信（✅ 已统一）

- `ICrossModuleService`（Infrastructure/Services/CrossModule/ICrossModuleService.cs:10）统一接口 + 域接口（IPatientCrossModuleService/IUserCrossModuleService/IHerbCrossModuleService/IAuthCrossModuleService/IRegistrationCrossModuleService/IMedicalCaseCrossModuleService）并存（D5-1 演进，接口隔离合理）；实现分散在各模块 Services/。无模块间直引例外。→ **无需收敛**。

### 2.4 仓储模式（❌ BaseRepository 仅 3/12 继承，9 个裸实现）

- **现状**：泛型基类 `BaseRepository<TEntity,TDbContext>`（BaseRepository.cs:14）继承者仅 3：MedicalCaseRepository（MedicalCaseRepository.cs:19）、MedicalCaseReferenceRepository（:12）、HerbReferenceRepository（HerbReferenceRepository.cs:15）。裸实现接口的 9 个：FormulaRepository/HerbRepository/UserRepository/AuthSessionRepository/SecurityAuditRepository/RegistrationRepository/PatientRepository/ReportRepository/SystemLogRepository。
- **根因**：A-06 泛型化「方案 B 收敛版」（commit d379f4a9d）把 Patient/Formula/Herb/User 标准 CRUD 收敛进了 `EntityApiClientRepositoryBase`（Desktop 侧），但 **Server 侧 BaseRepository 泛型化没有推广**——各模块 Repository 各自实现 IRepository<T> 接口方法（Interface 已泛型化：IRepository.cs:24 `IRepository<T>`，但实现类没继承 BaseRepository）。
- **统一方向**：Server 侧 CRUD 仓储收敛到 `BaseRepository<TEntity,TDbContext>`（接口保留模块特化方法）；Reports/SystemLog 等非实体聚合仓储保持裸实现并**在蓝图 §2.1 注明例外**。严重度 **P1**。

### 2.5 映射（❌ 手写映射残留 2 处 + Desktop 3 模块无映射层）

- **现状**：Mapperly 覆盖 Server 7 个（Patient/Herb/Formula/User/AuthUser/MedicalCase/Registration）+ Desktop 5 个；AutoMapper 已全移除。但：
  1. `UserCrossModuleService.cs:38-56` `new UserBasicDto { ... }` 16 属性手写映射 + `:68-87` `new UserCredentialDto { ... }` → **手写映射残留**（违反蓝图 0.4-4 映射单一）；
  2. Desktop Users/Patients/Herbs **无 Model 层、无 Mapper**，Repository 直接操作 DTO（UserRepository.cs:44 `response.Data.Items.ToList()` 直接用 `UserListDto`）——与 MedicalCase/Formula 的「DTO→Model + Mapper」模式不一致。
- **统一方向**：① `UserBasicDto`/`UserCredentialDto` 映射改为 Mapperly（在 Users 模块补 UserCrossModuleMapper）；② Desktop 全模块统一「直用 DTO」或「DTO→Model」二选一（推荐**直用 DTO**，与 A-21/M2 删 Mapper 的方向一致，删掉的正是无消费 Mapper）。严重度 **P1**（①）+ **P2**（②）。

### 2.6 DbContext（✅ 与蓝图 §2.2 完全一致）

- 7 模块独立 DbContext（AuthDbContext/UsersDbContext/PatientsDbContext/HerbsDbContext/FormulaDbContext/MedicalCaseDbContext/RegistrationDbContext）+ Reports 复用 AppDbContext——与蓝图 §2.2 表格逐格吻合。任务书「5 独立 + 4 复用」的候选已过时，实际是 **7 独立 + 1 复用**（A-20 后 Herbs/Formula/Users/Auth 也新建了 DbContext）。→ 蓝图 §2.2 表格本身准确；任务书候选信息无需处理。唯一问题见 §1.4 跨模块服务直查 AppDbContext。

### 2.7 批处理（❌ 两套骨架并存）

- **现状**：`BatchOperationHandlerBase<TEntity>`（BatchOperationHandlerBase.cs:10）6 继承者（BatchDelete×3 + BatchEnable/Disable/Delete Users）；**BatchImport×3（Formula/Herbs/Patients）不继承**，独立实现各自的批量导入模板（BatchImportFormulasCommandHandler.cs:11 等）。
- **推理链**：Q-01 泛型化只覆盖了「按 ID 批量操作」（删除/启停），「批量导入」因参数/返回形状不同（导入策略、成功计数）没有进基类。
- **统一方向**：BatchImport 可演进为 `BatchImportHandlerBase<TEntity, TRowDto>`（成功/失败计数 + 策略枚举为泛型参数），或**保持独立并写入蓝图 §2.1 作为第二模板**。二选一，避免第三个手写导入出现。严重度 **P2**。

### 2.8 验证（✅ 已统一入口）

- `ValidationBehavior<TRequest,TResponse>`（ValidationBehavior.cs:10）在 6 个 CQRS 模块注册（Auth/Users/Patients/Herbs/Formula/Registration）；MedicalCase/Reports 无 MediatR 故无管道（Service 内验证，合理）。→ **已统一**。问题在定义重复（见 §3.3 LoginRequestValidator 双份）。

### 2.9 异常映射（✅ 单点 + ⚠️ 3 处绕过）

- `SystemExceptionHandler`（SystemExceptionHandler.cs:13）+ `BusinessExceptionHandler`（:14）为唯二 `IExceptionHandler`，WebAPI 统一 `UseExceptionHandler`（UnifiedMiddlewareConfiguration.cs:24），无 IExceptionFilter 并存 → **映射单点已统一**。
- **绕过**：MedicalCase 3 处裸抛 `KeyNotFoundException`（MedicalCaseServiceHelper.cs:71/82、MedicalCaseRepository.Update.cs:194）——虽被 SystemExceptionHandler 兜底映射 404，但绕过 `NotFoundException` 业务异常层次（Shared.ExceptionHandling 7 文件有 NotFoundException.cs）。**统一方向**：改抛 `NotFoundException`。严重度 **P2**（行为正确、层次不统一）。

### 2.10 响应信封（✅ 已统一）

- 所有 Controller 方法经 `Success/SuccessPaged/BusinessFail/HandleResult` 包装（BaseApiController.cs:60-95），无 `return Ok(...)` 裸返回；AuthController 的 StatusCode/Unauthorized 均包 `ApiResponse<T>`。→ **已统一**。

---

## 3. 定义收敛清单（维度 3）

### 3.1 命名空间（❌ 见 §1.2，此处汇总）

- Server 3 模块（Registration/MedicalCase/Formula）项目名单数 vs 命名空间复数；Desktop 与 Server 跨层单复数不一致；蓝图 §3.2 表格仍写 `LYBT.Desktop.Registration` 单数。
- **统一定义**：全仓「项目名 = 根命名空间 = 复数」（对齐 A-25 复数惯例）；蓝图 §3.2 同步。严重度 **P1**。

### 3.2 类型命名（✅ 基本统一 + 2 处小不一致）

- 后缀分布：Service 129 / Handler 67 / Repository 42 / Command 35 / Validator 25 / Manager 24 / Model 57 / Provider 10 / Helper 8 / Factory 6 / Mapper 13。**无 Dto/DTO 大小写混用**（全 `Dto`）；Manager 全部在 Desktop 基础设施层（SessionManager/NavigationManager/DesktopCacheManager/StatusBarManager），业务模块用 Service → 分层清晰。
- ⚠️ 请求/响应命名不齐：`LoginRequest`（Contracts/Auth/LoginRequest.cs:10，无后缀）vs `ResetPasswordRequestDto`（Contracts/Users/ResetPasswordRequestDto.cs:13，有后缀）——同一「输入契约」两种后缀。
- **统一定义**：桌面 API 输入契约统一 `XxxRequest`（对齐 LoginRequest/LogoutRequest，Server Command 命名独立），或统一 `XxxRequestDto`——推荐前者（更短且 Refit 接口天然 `XxxRequest`）。严重度 **P2**。
- ⚠️ `IMedicalCase*` 接口家族 8 种后缀（CommandService/QueryService/StateService/Repository/Service/DataProvider/WorkspaceContext/LifecycleService）——医案聚合根特化，接口隔离合理但需在蓝图记录依据。严重度 P2（文档）。

### 3.3 DTO/契约（❌ 4 处重复/游离）

| 发现 | 证据 | 统一方向 | 严重度 |
|---|---|---|---|
| `LoginRequestValidator` **双份定义 + 双注册**：Shared `Validators/Auth/LoginRequestValidator.cs`（验证 `LoginRequest`）+ Auth 模块 `Application/Validators/LoginRequestValidator.cs`（验证 `LoginCommand`，规则几乎相同），AuthModule.cs:58-59 **同时注册两个程序集** | 双份验证器，同规则两处维护 | 保留 Shared 版（验证 LoginRequest，契约层），删 Auth 模块版——或反过来，二选一；推荐 Shared 版为 SSOT | **P1** |
| `UserBasicDto` 游离：独居 `Shared.Models/DTOs/Users/UserBasicDto.cs`，其余 User DTO 全在 `Contracts/Users/` | DTOs/ 目录仅此 1 文件 | 迁入 `Contracts/Users/`，删除 `DTOs/` 目录 | P2 |
| Reports DTO 分裂：Shared `Contracts/Reports/DoctorPerformanceDto.cs` 等 + Server 模块内 `Infrastructure/ReportQueryModels.cs:6-26` 定义 4 个私有 record（ReportDayValueDto 等） | 同域 DTO 两处 | 明确「ReportQueryModels 为仓储返回形状（私有）」，对外 DTO 在 Shared——已在文件头注释，建议蓝图补记该约定 | P2 |
| `RestartConfirmDto` 定义在 Controller 内（DeployController.cs:13） | 契约类寄生 Controller | 下沉 Shared.Models（或在蓝图注明「部署端点本地契约例外」） | P2 |

### 3.4 枚举/常量（✅ 已收敛）

- 63 个枚举无同名重复；`Gender`/`CommonStatus`/`MedicalCaseStatus`/`RegistrationStatus` 均在 `Shared.Models/Enums/` 唯一；未发现魔法数字裸写。→ **SSOT 达成**（蓝图 0.4-5 样板健康）。

### 3.5 接口命名（⚠️ 三层命名并存，属设计内）

- 并存：`IXxxApi`（Refit，Contracts/Api/，A-18 后 internal）+ `IApiClientXxx`（Contracts/ApiClient/，唯一对外面）+ `IXxxService`（Contracts/Services/）。这是 A-18 契约单一的既定三层结构 → **设计内**，但命名上 `IXxxApi` vs `IXxxService` 易混，建议在蓝图 §3.1 记录「三层命名矩阵」。
- 跨层同名镜像：`IFormulaRepository/IHerbRepository/IUserRepository/IRegistrationRepository/IMedicalCaseRepository/IFormulaService` 等 Server 与 Desktop.Contracts 各一份（同名字不同程序集）——设计内镜像（Server=EF 仓储，Desktop=API 客户端仓储），建议蓝图注明镜像关系防误改。严重度 **P2**（文档）。

### 3.6 错误码/异常（✅ SSOT + ⚠️ 3 处绕过，同 §2.9）

- `ErrorCode` 唯一：`Shared.Models/Primitives/ErrorCodes/ErrorCode.cs:17`；AppException 7 类层次唯一（AppException/Business/Conflict/NotFound/Validation/Unauthorized/Api）；BusinessExceptionHandler 映射 ErrorCode→HTTP 单点。→ SSOT 达成。
- 绕过：`KeyNotFoundException` ×3（§2.9）。**统一方向**：改 `NotFoundException`。P2。

### 3.7 状态/术语（✅ 英文标识符唯一 + ⚠️ Status/State 语义边界）

- `RegistrationStatus`/`MedicalCaseStatus` 唯一；医案=MedicalCase、处方=Prescription、挂号=Registration 无一词双名；`Consultation` 452 处 vs 会诊仅注释 2 处。→ **术语收敛**。
- `Status`（域枚举 MedicalCaseStatus/RegistrationStatus）vs `State`（客户端 WorkspaceEditState/EditState/AuthState/SessionState/TokenLifecycleState）语义并存——职责不同但边界未文档化。**统一方向**：蓝图/术语表记录「域内状态用 Status 枚举；客户端 UI/会话状态用 State 枚举」边界。P2。

### 3.8 配置文件（❌ Jwt Section 分叉 + 结构不同型）

| 发现 | 证据 | 统一方向 | 严重度 |
|---|---|---|---|
| Jwt Section 命名分叉：LocalWebAPI 用 `LocalJwt`（LocalJwtConfig.cs:22、LocalWebApiProgram.cs:104），Server/Shell 用 `Jwt` | 同概念双 Section 名 | 统一为 `Jwt`（LocalWebAPI 内做配置转换），或蓝图注明 `LocalJwt` 为 Local 专用例外——推荐统一 `Jwt` | **P1**（配置契约分裂） |
| Shell `appsettings.json` 的 `Jwt` 缺 `AccessTokenExpirationMinutes/RefreshTokenExpirationDays`（Server 有） | 同 Section 不同结构型 | Shell Jwt 补齐字段或改用公共 Options 类型校验 | P2 |

---

## 4. 收敛任务清单汇总（可直接派发）

### P0
无（未发现红线违反或设计态/当前态严重背离——架构守卫健康、无模块直引、响应信封/异常映射/枚举 SSOT 已达成）。

### P1（应修）
| # | 任务 | 类型 | 证据 |
|---|---|---|---|
| 1 | **统一 5 模块请求处理模式**：Users/Patients/Herbs/Formula 的 Controller 消除「读走 Service + 写走 Handler」混用，统一为 Service 化（推荐，对齐 MedicalCase）或全 Handler，技术总监定夺后派发 | 模式收敛 | §2.2 |
| 2 | **P10 守卫盲区修复**：`UserCrossModuleService`/`HerbCrossModuleService` 通过 `IDbContextAccessor` 直查 AppDbContext，违反 P10 精神与模块 DbContext 自治；守卫补查 IDbContextAccessor 注入 | 蓝图对齐 | §1.4 |
| 3 | **手写映射清除**：`UserCrossModuleService.cs:38-56/68-87` 改 Mapperly | 模式收敛 | §2.5 |
| 4 | **LoginRequestValidator 双份收敛**：Shared/Auth 双份双注册 → 保留 Shared 版 SSOT | 定义收敛 | §3.3 |
| 5 | **命名空间单复数全仓对齐**：Server 3 模块（Registration/MedicalCase/Formula）项目名 vs 命名空间统一（建议项目名改复数）+ 蓝图 §3.2 表格更新 | 定义收敛 | §1.2 |
| 6 | **Server 仓储收敛**：9 个裸 Repository 中 CRUD 型收敛到 `BaseRepository<TEntity,TDbContext>`；例外（Reports/SystemLog）蓝图注明 | 模式收敛 | §2.4 |
| 7 | **Jwt 配置 Section 统一**：`LocalJwt` → `Jwt`（或蓝图注明例外） | 定义收敛 | §3.8 |

### P2（可后议）
| # | 任务 | 类型 | 证据 |
|---|---|---|---|
| 8 | 蓝图 §1/§3 文件数表格回写（Infrastructure 92/Patients 18/Formula 14/Foundation 70/Contracts 78/Logging 8 等 7 处 + Admin/Clinical 补文件数列） | 蓝图维护 | §1.1 |
| 9 | 蓝图 §2.1 补记 BaseUsersController/BaseRegistrationsController 两条 Controller 路径 | 蓝图维护 | §2.1 |
| 10 | 蓝图 §2.4 与 §2.2 结构矛盾澄清（三态模板 vs 七目录模板；MedicalCase `Repositories/` vs 其他 `Infrastructure/`） | 蓝图维护 | §1.3 |
| 11 | Desktop Users/Patients/Herbs 无映射层：统一「直用 DTO」或补 Model+Mapper（推荐前者）；蓝图 §3.2 同步 | 模式收敛 | §2.5 |
| 12 | BatchImport×3 与 BatchOperationHandlerBase 二套骨架：演进泛型导入基类或蓝图记录第二模板 | 模式收敛 | §2.7 |
| 13 | `KeyNotFoundException`×3 → `NotFoundException` | 模式收敛 | §2.9/§3.6 |
| 14 | `LoginRequest` vs `ResetPasswordRequestDto` 后缀统一 | 定义收敛 | §3.2 |
| 15 | `UserBasicDto` 迁入 `Contracts/Users/`；`RestartConfirmDto` 下沉或注明例外 | 定义收敛 | §3.3 |
| 16 | 蓝图记录三层接口命名矩阵（IXxxApi/IApiClientXxx/IXxxService）+ 跨层镜像接口清单 | 蓝图维护 | §3.5 |
| 17 | 蓝图/术语表记录 Status vs State 语义边界 | 定义收敛 | §3.7 |
| 18 | Shell Jwt 结构补齐/校验；Reports DTO 契约约定补记 | 定义收敛 | §3.8 |

---

## 5. 顺带发现附录（不评级）

1. **codebase-memory 图谱索引过期**：包含 A-21/A-23 已删除文件（Desktop Users/Patients Mapper、Infrastructure ApiService 等）。若继续使用该 MCP 需重建索引，否则符号级查询会产出幽灵证据。本次审查已用 git + 文件系统规避。
2. **AGENTS.md 陈旧信息多处**：`src/Server/AGENTS.md` 仍列 `LYBT.Module.Sync`（已不存在）；`Modules/AGENTS.md` 提到 `SharedKernel`（已坍缩）；LocalWebAPI AGENTS.md 写「10 controllers」（实际 12）、依赖图含 `LYBT.Module.Sync`；Desktop AGENTS.md 仍写 `LocalData/CardReader`（已废弃）。建议后续统一由文档收敛任务处理。
3. **`Shared.Models/DTOs/` 目录仅剩 1 文件**（UserBasicDto），为事实上的死目录，建议随 §3.3 一并清理。
4. **架构守卫计数口径**：蓝图 §4 写「85 条/86 条」，tests 目录实测 `[Fact]/[Theory]` 标记 79 个（含 Theory 多用例，单方法计 1）——口径可能不同（Theory 数据展开后多于 79），建议蓝图注明计数口径，避免后续审计误解。
5. **`BaseRegistrationsController.Update` 未加 `override` 但重写基类同名方法**（BaseRegistrationsController.cs:69）——若基类方法非 virtual 则编译警告（CS0114 类），且与同文件其他 override 方法风格不一致；因只读审查未深挖，交修复阶段确认。

---

## 6. 统计汇总

| 指标 | 数值 |
|---|---|
| 蓝图项目核对 | 34/34 存在（100%）；文件数：Server 15/15 命中、Shared 4/5、Desktop 9/16（7 处 -1~-9 偏差） |
| 蓝图 §1/§2/§3/§4 核对 | §1：5 项全核；§2：10 项目逐核；§3：16 项目逐核；§4：3 项目（未深审测试内容，仅存在性） |
| 蓝图偏差清单（维度 1） | 14 条（P1×2、P2×12，含 1.2~1.5 合并条目） |
| 模式收敛清单（维度 2） | 10 族：已统一 4（跨模块/验证/异常映射/响应信封）+ 需收敛 5（Service-Handler 双轨/仓储/映射/批处理/Controller 蓝图记录）+ DbContext 一致 1 |
| 定义收敛清单（维度 3） | 8 族：已收敛 2（枚举/状态术语）+ 需收敛 6（命名空间/类型后缀/DTO 契约/接口命名文档/错误码绕过/配置文件） |
| 收敛任务清单 | P0：0；P1：7；P2：11；共 18 项 |
| 顺带发现 | 5 条（不评级） |

**总体评价**：系统整体健康——依赖规则、响应信封、异常映射、枚举/术语 SSOT、DbContext 布局均达成蓝图设计态；主要风险集中在「5 个 CQRS 模块 Service/Handler 双轨」与「跨模块服务绕过模块 DbContext/守卫盲区」两类结构性问题上，均属 P1 可派发范畴，无 P0。
