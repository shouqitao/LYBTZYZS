# 结构审计报告（Mimo Code 独立分析）

> 独立分析者 #2｜对应任务书：`structure-audit-task-spec-2026-08-08.md`（v1.0）
> 技术总监将与本报告交叉验证；本报告只读，未修改任何代码，未 commit。

## 0. 方法说明

| 项 | 说明 |
|---|---|
| 时间点 | 2026-08-08；HEAD=`b7c7390d`（任务书 docs 提交），代码态=`97445a6f3`（死代码清理） |
| 范围 | `src/Shared`(5) + `src/Server`(10) + `src/Client`(16) 共 31 项目；排除 `tests/`(3)、`src/Tools/`(4)（sln 共 34 项） |
| 工具 | serena MCP（符号级定义/引用）、codebase-memory 图谱（`LYBTZYZS`）、csproj 依赖图、rg 初筛 |
| 图谱新鲜度 | ⚠️ 图谱 head=`f2627210`，**早于**死代码清理 `97445a6f3`（索引含已删文件）。所有图谱结论均已与磁盘文件核对；死代码结论一律以磁盘为准 |
| 局限 | 本会话子代理派发（actor spawn/run）不可用（工具解析故障），全部审计由主代理直接执行；未运行测试/应用，功能级结论基于静态证据链；报表端点"是否有客户端消费"仅核对了 `HttpClientApiClient` 族 |

## 1. 项目依赖图 + 违规边

### 1.1 依赖方向（csproj 层，31 项目）

```
Server 层
  LYBT.WebAPI ──> Module.Auth/Users/Patients/Herbs/Formula/MedicalCase/Registration/Reports ──> Infrastructure ──> Entities ──> Shared.Models
Shared 层（自包含，无反向）
  Shared.Models <── Entities / Shared.Configuration / Shared.ExceptionHandling / Shared.Logging（全部经 Shared.Models）
Client 层（内核单向）
  Contracts <── Foundation <── Controls <── Infrastructure（依赖顺序成立，无反向边）
  Modules ──> Infrastructure/Foundation/Contracts；Roles(Admin/Clinical) ──> Modules；Shell ──> 全部
跨层（唯一例外，有意设计）
  LocalWebAPI ──> Infrastructure + 8 个 Server 模块（ADR-0010 统一服务层；任务书称 7 个模块，实际 8 个）
```

### 1.2 违规边标注

| 边 | 类型 | 判定 | 证据 |
|---|---|---|---|
| `Desktop.Admin` → Herbs/Formula/Patients/MedicalCase/Users | 模块→模块（Desktop） | ⚠️ 违规候选（P1） | `LYBT.Desktop.Admin.csproj` |
| `Desktop.Clinical` → Herbs/Formula/Patients/MedicalCase/Registration | 模块→模块（Desktop） | ⚠️ 违规候选（P1） | `LYBT.Desktop.Clinical.csproj` |
| `Desktop.Registration` → `Desktop.MedicalCase` | 模块→模块（Desktop） | ⚠️ 违规候选（P1） | `LYBT.Desktop.Registration.csproj` |
| `LocalWebAPI` → 8 Server 模块 | Client→Server | 合法例外 | ADR-0010；架构测试 `P21` 显式跳过 |
| Server 模块互引 | — | 无（合规） | 8 模块 csproj 仅引 Infrastructure/Entities/Shared |
| Shared 反向引用 | — | 无（合规） | 5 个 Shared 项目 csproj 无 Server/Client 引用 |

> Desktop 侧约束来源：`src/Client/Desktop/AGENTS.md` 明确「Business modules MUST NOT reference each other」，且无对应架构测试（P01-P07 仅覆盖 UI/Server 边界）——规则声明了、未强制、且被违反。Roles（Admin/Clinical）对 Modules 的引用属工作区编排，可视为正常；但 `Desktop.Registration → Desktop.MedicalCase` 属模块间直接引用，无明确编排依据。

## 2. 按 7 类分节的问题清单

### 类别 1：分层/依赖违规

**F1-1｜Desktop 模块间直接引用（P1，应修）**
- 证据：`LYBT.Desktop.Admin.csproj` 引用 Herbs/Formula/Patients/MedicalCase/Users；`LYBT.Desktop.Clinical.csproj` 引用 Herbs/Formula/Patients/MedicalCase/Registration；`LYBT.Desktop.Registration.csproj` 引用 MedicalCase。
- 推理链：Desktop AGENTS.md 声明「业务模块 MUST NOT reference each other，跨模块通信走共享服务/事件聚合」；csproj 层直接引用即违反该规则。同时架构测试套（P01-P22）无任何针对 Desktop 模块间引用的规则 → 规则悬空。
- 建议方向：确认 Admin/Clinical/Registration 对模块的引用是否全部走模块公开接口（IModule/服务接口）；将模块间依赖收敛为事件聚合或共享服务；补架构测试。

**F1-2｜Desktop.Infrastructure 职责过载（P2，可后议）**
- 证据：`src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/` 同时容纳 Http/ ViewModels/ Views/ Windows/ CardReader/ Navigation/ Behaviors/ Roles/ Security/ Helpers/ Commands/ Events/ LocalData/ Performance 等 14 类职责。
- 推理链：Core AGENTS.md 定义层级「Contracts ← Foundation ← Infrastructure ← Controls」，其中 Infrastructure 描述为「WPF services — ViewModel base classes, Dialog, Navigation, Behaviors」；实际混入 HTTP 客户端适配（Http/）、硬件（CardReader/）、本地数据（LocalData/）、角色（Roles/）等应属 Foundation/Controls/Modules 的职责。
- 建议方向：按职责拆分目录归属，CardReader/LocalData 独立或归入 Foundation。

**F1-3｜Shared.Models 一项目八职责（P2，可后议）**
- 证据：`src/Shared/LYBT.Shared.Models/` 下 Attributes/ Contracts/ DTOs/ Enums/ Extensions/ Primitives/ Utilities/ Validators 八个文件夹（原设计为 8 个独立 Shared 项目）。
- 推理链：08-shared.md 的 8 项目结构（Primitives/Utilities/Components/Validators…）已坍缩为单项目文件夹；从"物理隔离"退化到"逻辑分层"，依赖约束（如 Primitives 零依赖）不再可编译期强制。
- 建议方向：文档先行（08-shared 已滞后，见 §5-1）；如需恢复物理边界再拆分项目。

### 类别 2：Shared 归属问题

**结论：核心共享类型全部单源，未发现 Gender 式反例。** 已符号级确认：`Gender`(Shared.Models/Enums/Gender.cs)、`MedicalCaseStatus`(MedicalCaseEnums.cs)、`CommonStatus`(SystemEnums.cs)、`ErrorCode`(Primitives/ErrorCodes/ErrorCode.cs)、`ValidationConstants`(Primitives/Validation/)、`ApiResponse`(Contracts/Common/)、`Result`(Contracts/Common/)、Patient/Herb/Formula/User 各 `*InputDto`/`*DetailDto` 均唯一定义于 Shared.Models。`ApiResponse` 在 `Desktop.Infrastructure/Http/ApiResponseHelper.cs` 的 grep 命中为 `class ApiResponseHelper` 前缀误报（serena 确认单源）。

**F2-1｜文档 Utilities 结构与代码不符（P2）**
- 证据：08-shared.md 声称 Utilities 含 `ConfigurationHelper / PasswordHasher / JwtHelper / PinYinConverter / StringExtensions / DateTimeHelper`；实际 `Shared.Models/Utilities/` 仅 4 个文件：`CacheExtensions.cs / PasswordHelper.cs / PasswordPolicyValidator.cs / PinYinHelper.cs`（serena 查 `DateTimeHelper`/`PinYinConverter` 均 0 结果）。
- 推理链：工具类经历改名/迁移（PinYinConverter→PinYinHelper、PasswordHasher→PasswordHelper）后文档未同步；引用文档的读者会找到不存在的类型。
- 建议方向：08-shared §Utilities 章节按代码现状重写。

**F2-2｜LocalWebApiSeedData 内嵌样例业务数据（P2）**
- 证据：`LocalWebApiSeedData.cs:22-53` 插入 Herb "Ginseng"、Formula "Sample Formula"、Patient "Sample Patient"（英文样例）。
- 推理链：样例数据仅存在于本地模式（远程无对应种子），且为英文名，与中文产品语义不符；属"该下沉 Shared/该进配置"的种子数据归属问题。

### 类别 3：多机制并存（全量清单）

| # | 机制 | 存活双方（证据） | 活跃度 | 判定 |
|---|---|---|---|---|
| 3-1 | 日志 CorrelationId | `AsyncLocalCorrelationIdProvider`（Shared.Logging/Abstractions/AsyncLocalCorrelationIdProvider.cs:7）+ `ActivityCorrelationIdProvider`（同目录 ActivityCorrelationIdProvider.cs:15） | AsyncLocal：注册扩展 `AddAsyncLocalCorrelationIdProvider`（ServiceCollectionExtensions.cs:61）**全仓 0 调用点** → 未注册；Activity：`DesktopSerilogConfiguration.cs:34` 实际使用 | **并存但 AsyncLocal 侧已死**；且 `CorrelationIdEnricher.cs:14` 注释声称"Desktop端使用AsyncLocalCorrelationIdProvider"与代码矛盾 |
| 3-2 | DbContext | `AppDbContext`(Infrastructure/Data/AppDbContext.cs:25, 13 DbSet 含 Identity) + `AuthDbContext`(Module.Auth) + `UsersDbContext`(Module.Users, IdentityDbContext) + `HerbsDbContext`(Module.Herbs) + `FormulaDbContext`(Module.Formula) | 仓储注入矩阵：Patient/Registration→AppDbContext；AuthSession→AuthDbContext；User→UsersDbContext；Herb→HerbsDbContext；Formula→FormulaDbContext。模块 DbContext **均无独立迁移**，表结构靠 AppDbContext 迁移链（Infrastructure/Migrations 22 个） | 5 个 DbContext 并存但共享同一 schema 模型；文档 03-server 声称的 `ReportsDbContext` **代码中不存在** |
| 3-3 | 密码哈希 | BCrypt `PasswordHelper`（Shared.Models/Utilities/Security/PasswordHelper.cs:17，含 GenerateSecurePassword/VerifyPassword）+ Identity PBKDF2（`AddIdentity...AddEntityFrameworkStores`，LocalWebApiProgram.cs:84-95；登录验证） | PasswordHelper 引用：ResetPasswordCommandHandler.cs:32（生成随机密码）、PasswordPolicyValidator、Tools；工具注释明示"不能用 BCrypt（PasswordHelper）：直接写入 Users.PasswordHash 将导致无法登录"（PasswordHashGenerator/Program.cs:126） | **双机制并存，运行时主机制=Identity PBKDF2，BCrypt 已边缘化但仍存活**；00-architecture-summary 的"BCrypt 密码"表述过时 |
| 3-4 | 对象映射 | Mapperly 编译期（各模块 `Mapping/*Mapper.cs`）+ 手动映射（Controller 手工 ApiResponse 包装、Service 内 DTO 组装） | 设计文档 08-shared 已记录 23 Mapper + 内联例外；手动映射热区未穷举 | 并存，属已知设计；未发现新增问题 |
| 3-5 | 批量操作 | `BatchOperationHandlerBase`(Infrastructure/BatchOperations/BatchOperationHandlerBase.cs:10) + 3 个 `BatchImport*CommandHandler`（Patients:14 / Herbs:15 / Formulas:11）+ `BatchEnableUsersCommandHandler`(Users:10) + BaseCrudController 批量模板（ExecuteBatchDeleteAsync/BatchStatusAsync/BatchCheckReferenceAsync，BaseCrudController.cs:76-141） | 全部存活：模板执行器被两端 Controller 委托；Handler 走 MediatR | 三形态并存（Handler / 基类模板 / Service 直调），职责有重叠但可辨识 |
| 3-6 | API 契约双套 | `Contracts/Api/*`（11 个 Refit 特性接口）+ `Contracts/ApiClient/*`（12 个统一无特性接口，`IApiClient` 定义于 ApiClient/IApiClient.cs:20） | **双活**：Api 套被 `RefitApiClient.cs:68-100`（RestService.For\<IAuthApi\> 等）用于远程模式 + Admin 3 个 ViewModel（SystemSettings/LogLevelControl/Deployment）直用；ApiClient 套被 SwitchingApiClient/HttpClientApiClient/全部桌面 Repository 使用（55 文件引用 vs 26） | **真并存非死代码**；合并方向：以 ApiClient（统一面）为主，Api 套内化为其远程实现细节 |
| 3-7 | 异常处理 | `ExceptionFactory`：**已删除（0 残留）**；`ServiceResult`：**已删除（0 残留）** | 现机制=直接 throw + `AppException` 层次 + ProblemDetails + IExceptionHandler 链 | 已解决，与任务书预期一致 |
| 3-8 | 配置存储 | 远程 `ISystemConfigurationService`（WebAPI ConfigurationController.cs:20 注入）+ 本地 `ConcurrentDictionary` 静态内存（LocalWebAPI ConfigurationController.cs:22） | 双活 | **并存且语义不同：本地配置重启即失**（见 §3 链 D） |

### 类别 4：死代码（独立重扫，不抄上轮）

| 符号 | 位置 | 证据（查法） | 判定 |
|---|---|---|---|
| `AsyncLocalCorrelationIdProvider` + `AddAsyncLocalCorrelationIdProvider` | Shared.Logging/Abstractions/AsyncLocalCorrelationIdProvider.cs:7；Extensions/ServiceCollectionExtensions.cs:61 | `rg AddAsyncLocalCorrelationIdProvider` 全仓仅 1 处=自身定义，**0 调用点** | 新发现，未注册未使用（P1：若 Server 侧亦走 Activity 则可删） |
| `LocalDbContext` | Desktop.Infrastructure/LocalData/Context/LocalDbContext.cs | 生产代码 0 引用；仅 tests 5 文件引用 | 新发现，休眠代码（P2）+ 文档冲突（Core AGENTS 仍描述 LocalData 为本地数据路径，与 ADR-0009 的 HTTP 本地路径矛盾） |
| `DtoConversionExtensions` | Shared.Models/Extensions/DtoConversionExtensions.cs | 全仓仅自身 1 文件 | 新发现（P2） |
| `CacheExtensions` | Shared.Models/Utilities/Extensions/ServiceCollection/CacheExtensions.cs | 生产 0 引用（tests 1） | 新发现（P2） |
| `AuthSessionModel` | Entities/Auth（按 03-server 目录表） | 全仓 0 引用 | 候选，需人工复核（AppDbContext 使用 `AuthSession`，可能与 `AuthSessionModel` 同名类/文件命名差异） |

已知项核实：上轮清理（97445a6f3，删 7 文件+6 方法+60 using）无残留引用（抽查引用计数为 0 的类型均无悬垂）。本类未对全部 1045 个源文件穷举，样本集中于 Shared/Infrastructure/Desktop.Infrastructure 候选区。

### 类别 5：命名/组织一致性（只列证据）

| 现象 | 证据 | 数量 |
|---|---|---|
| 后缀混用：Manager/Service/Handler/Helper/Provider/Coordinator | `class *Manager` 15 / `*Service` 66 / `*Handler` 61 / `*Helper` 10 / `*Provider` 5 / `*Coordinator` 5；同职责异后缀：`PatientSearchManager`(Desktop.Patients) vs `HerbSearchProvider`/`FormulaSearchProvider`(Desktop.Herbs/Formula)；`SessionManager` vs `SessionLifecycleManager` vs `LoginStateManager`(Desktop.Infrastructure/Shell) | 同类职责 ≥4 种后缀 |
| 命名空间复数已统一 | `namespace *Repositories` 21、`*Enums` 13、`*Controllers` 27；单数形态 0 | Q-03 效果确认（无新问题） |
| 项目名 vs 命名空间单复数不一致 | 项目 `LYBT.Module.MedicalCase`（单数）vs 命名空间 `LYBT.Module.MedicalCases`（LocalWebApiProgram.cs:19 `using LYBT.Module.MedicalCases;`）；同理 `LYBT.Module.Registration` vs `LYBT.Module.Registrations`、`LYBT.Module.Formula` vs `LYBT.Module.Formulas` | 3 组 |

### 类别 6：顺带发现 → 见 §4（本审计中顺带发现单列，不评级）

### 类别 7：数据流走查 → 见 §3（本审计核心专项，含双轨对称性 6 检查点）

## 3. 数据流走查（4 条链 × 每层 4 问 + 双轨对称性 6 检查点）

### 链 A：患者链（Desktop Patients 模块 → API → 患者域 → DB）

| 层 | 职责/实际 | 转换点 | 路径对称性 | 异常传导 |
|---|---|---|---|---|
| Desktop ViewModel/Repository | `PatientRepository`(Desktop.Patients/Repositories/PatientRepository.cs:15) 经 `IApiClient` 路由（注释："所有调用均通过 IApiClient 路由"）| 无（DTO 直达 VM） | 对称（SwitchingApiClient 透明） | Repository 层收到 ApiResponse，错误经 `ApiResponseHelper` 解析为异常/消息 |
| SwitchingApiClient | `IsLocal → HttpClientApiClient : RefitApiClient`（SwitchingApiClient.cs:75-77），双重检查锁，URL 驱动 | — | 分支对称，接口面一致 | — |
| 本地 HttpClientApiClient | `PatientsHttpApiClient.cs:20-60` 调 `GET/POST/PUT /api/v1/patients`、`batch-delete`、`import` | URL 拼接 | **不对称（缺陷）**：客户端调用的端点本地控制器未实现（见检查点 1） | — |
| WebAPI/Local PatientsController | 远程 12 端点（GetList/GetById/Create/Update/Delete/Toggle/Restore/BatchDelete/BatchImport/CheckReference/ByIdNumber/BatchCheckReference）；本地 7 端点（GetById/ByIdNumber/CheckReference/Delete/Toggle/Restore/BatchCheckReference） | 两端均注入 `IPatientService`+`ISender` | 远程端调 `_patientService.GetPagedAsync`；本地 GetList **未 override → 基类 `NotSupportedException`（BaseCrudController.cs:34）→ 500**；Create/Update 无路由 → 405 | 两端均 `Result<T>` → `BusinessFail/NotFound`（本地 Delete 对含"医案记录"错误返回 BusinessFail，远程返回 NotFound——语义微差） |
| PatientService/MediatR | `PatientService`(600 行) + Create/Delete/Toggle 等 CommandHandler | `PatientMapper`(Mapperly, Module.Patients/Mapping)；实体↔DTO 在 Service/Mapper 完成 | 两端共享同一 Service/Handler（ADR-0010） | 业务失败返回 `Result.Failure`，不抛异常 |
| PatientRepository/DbContext | `PatientRepository → AppDbContext`(Module.Patients/Infrastructure/PatientRepository.cs:15-17) | — | 远程=AppDbContext(SQL Server)；本地=同一 AppDbContext(LocalDB, 显式传入连接串) | — |

**链 A 结论**：本地模式患者列表/创建/更新/批量删除/导入全部不可用（500/405），而本地客户端明确调用这些端点 —— **本地模式核心 CRUD 断裂（P0 候选，建议运行验证）**。

### 链 B：挂号接诊链（Registration → RegistrationsController → 挂号域 → SignalR）

| 层 | 4 问要点 |
|---|---|
| Desktop Registration 模块 | `RegistrationsHttpApiClient` 调 `GET /registrations`、`GET /{id}`、`/queue`、`quick-visit`、`start-visit`、`cancel`、`POST /registrations` |
| RegistrationsController | 两端均继承 `BaseRegistrationsController`（Module.Registration/Controllers/BaseRegistrationsController.cs）；本地派生类（LocalWebAPI/Controllers/RegistrationsController.cs:18）仅重写 QuickVisit/StartVisit/Create/Cancel 并加 `[Authorize(DoctorOnly/DoctorOrReceptionist)]`，CRUD 走基类默认（基类实现经 ISender 派发 Module.Registration 的 MediatR Handler → 本地同样注册 `AddRegistrationModule` → **基类默认实现本地可用**） |
| 业务逻辑 | CreateRegistrationCommand 等 Handler 双端共享（真复用）；SignalR 推送仅在远程生效（RegistrationHub，WebAPI Program.cs），本地无 Hub（单机无意义，符合设计） |
| 异常传导 | 同链 A：Result → BusinessFail/NotFound |

**链 B 结论**：挂号链双轨可用（本地经基类+模块 Handler），路径基本对称；差异点=本地无 SignalR、无速率限制（`[EnableRateLimiting("ApiCalls")]` 仅远程）。

### 链 C：处方打印链（MedicalCase 开方 → 打印 → 打印审计）

| 层 | 4 问要点 |
|---|---|
| Desktop MedicalCase 模块 | `MedicalCasesHttpApiClient`（本地）调 `GET /medicalcases?page=`、`/query?queryType=`、`/search?page=`、`/{id}`、`/{id}/print-completed`、`/prescription-flag` 等；远程经 Refit `IMedicalCaseApi`（`MedicalCaseApiClient.cs:20`，RefitApiClient.cs:87 注入） |
| MedicalCasesController | 两端继承 `BaseMedicalCasesController`（Module.MedicalCase/Controllers/BaseMedicalCasesController.cs，13+ virtual 默认实现经 ISender 派发）；本地派生类重写 GetById/Query/ByStatus/Pending/Create/Close/Suspend/Cancel/Status |
| 本地缺口 | 本地客户端调用的 `print-completed`、`prescription-flag`、`audit-logs`、`permissions`、`search`、裸 `GET /medicalcases`（列表）在本地控制器无对应路由/实现 → 404 或基类缺失。文档 05-dual-mode 称"打印日志本地不记录，端点返回空结果"，**实际为端点缺失（404）**，行为不符 |
| 打印实现 | `Desktop.Printing` 项目（PrescriptionPrintService/Executor/DocumentBuilder/PdfExporter + 4 个 XAML 模板）；打印完成回调 `RecordPrintAsync` → 远程写 `MedicalCasePrintLogs`（AppDbContext DbSet 存在），本地端点缺失 → 打印审计在本地静默失败（客户端容错则无感） |
| 转换点 | `PrescriptionPrintModel` 手工组装（Desktop.Printing），非 Mapperly；医案 DTO↔Entity 走 MedicalCase Mapper |

**链 C 结论**：远程打印审计闭环存在（print-completed → MedicalCasePrintLog）；本地打印审计端点缺失（文档声称"返回空结果"不符实际 404）—— 属双轨不对称，低风险但文档需修正。

### 链 D：配置链（ConfigurationController → 配置存储 → Desktop 消费）

| 层 | 4 问要点 |
|---|---|
| 远程 ConfigurationController | 注入 `ISystemConfigurationService`（WebAPI/Controllers/ConfigurationController.cs:20-23），5 端点（Get/Get{key}/Put{key}/Put/validate）→ 持久化存储 |
| 本地 ConfigurationController | **注入仅 ILogger**，`private static ConcurrentDictionary<string,string> _store`（LocalWebAPI/Controllers/ConfigurationController.cs:22），4 端点（无裸 Put）→ **进程内内存，重启即失**；文档未记载此差异 |
| Desktop 消费 | ClinicSettings/连接设置走 `IConnectionSettingsService`（本地 URL 持久化于桌面侧）；本地配置不落库 |
| 异常传导 | 本地配置键缺失返回 404（与远程同），但"保存成功"无持久化保证 |

**链 D 结论**：配置链双轨不对称——远程持久化 vs 本地内存（功能语义不同，若本地配置属 v1.0 需求则为 P1）。

### 双轨对称性 6 检查点

| # | 检查点 | 结论 | 证据 |
|---|---|---|---|
| 1 | Controller 对称性 | **任务书前提过时**：两端各 12 个同名 Controller（Auth/Configuration/Deploy/Diagnostics/Formulas/Health/Herbs/MedicalCases/Patients/Registrations/Reports/Users），1:1 无缺。但**端点集严重不对称**：本地缺 患者(GetList/Create/Update/BatchDelete/Import/Export)、药材(CRUD 大部分)、验方(CRUD 大部分)、医案(列表/search/audit-logs/permissions/prescription-flag/print-completed/batch-delete)、报表(8→3)；本地独有的 query/by-status/pending/by-id-number/clone 等 8 个便捷端点在。**缺的是 CRUD 主干而非便捷端点** | §2-类别7 端点矩阵；PatientsController 两端全文对比 |
| 2 | 业务逻辑共享 | **真复用，非各写一套**：本地 Patients/Registrations/MedicalCases 等 Controller 注入 `ISender`+`I*Service`，与远程同一 Service/Handler/DbContext 层（ADR-0010，`AddXxxModule` 双端注册）。文档 05-dual-mode"本地 DI 精简 Controller→DbContext 直连"**已过时** | LocalWebAPI PatientsController.cs:24-30；LocalWebApiProgram.cs:64-71 |
| 3 | 数据访问一致性 | **schema 一致**：本地复用同一 `AppDbContext`（LocalWebApiProgram.cs:47）+ 同一 `MigrateAsync` 迁移链（:139），无 schema 漂移路径。⚠️ 风险点：模块级 DbContext（Users/Herbs/Formula/Auth）连接串经 `ConnectionStringResolver`（三级回退）解析，而 AppDbContext 用桌面显式传入的 LocalDB 串 —— **同一进程两条连接串解析路径，本地模式两者可能指向不同库（需运行时验证）**。文档"本地 EnsureCreated"已过时（实际 MigrateAsync） | AppDbContext.cs:25；UsersModule.cs:30-36；ConnectionStringResolver.cs:10-14 |
| 4 | 切换逻辑 | SwitchingApiClient 分支对称（IsLocal→HttpClientApiClient，否则 RefitApiClient，SwitchingApiClient.cs:75-77）；本地不绕过认证（两端 JWT + Policy，Local PatientsController.cs:19）；**但本地端点缺失使切换后核心功能断裂**（链 A） | — |
| 5 | 种子数据 | **一致**：`IdentitySeedData.SeedRolesAndAdminAsync`（4 角色 + sysadmin，Module.Users/Services/IdentitySeedData.cs:14-32）双端共享调用；本地额外 `LocalWebApiSeedData` 插入 3 条英文样例业务数据（LocalWebApiSeedData.cs:22-53，远程无对应） | LocalWebApiProgram.cs:140-141 |
| 6 | 契约双套 | **双套均存活**：`Api/*`（Refit，远程 RefitApiClient + Admin 3 ViewModel 直用）与 `ApiClient/*`（统一 IApiClient，55 引用）互不替代；合并方向=以 ApiClient 为统一面、Api 内化。另外本地客户端 URL 与远程端点路径不一致：`PatientsHttpApiClient` 调 `POST /patients/import` 而远程端点是 `POST /patients/batch-import` —— 客户端契约与两端服务端均不对齐 | PatientsHttpApiClient.cs:39-40；WebAPI PatientsController.cs:212 |

## 4. 顺带发现附录（不评级，只记录）

| # | 证据 | 疑点 | 为什么觉得有问题 |
|---|---|---|---|
| A1 | LocalWebApiSeedData.cs:17 `EnsureCreatedAsync()` 在 LocalWebApiProgram.cs:139 `MigrateAsync()` 之后再次调用 | 迁移后再 EnsureCreated | EnsureCreated 与 Migrate 混用属冲突模式；若模型演化可能掩盖迁移状态 |
| A2 | LocalWebApiSeedData.cs:24-53 英文样例（Ginseng/Sample Formula/Sample Patient） | 生产样例数据与中文产品语义不符 | 用户离线首启会看到英文假数据 |
| A3 | Local PatientsController.cs:85-86 Delete 对"医案记录"错误返回 `BusinessFail`，远程同场景返回 `NotFound`（WebAPI PatientsController.cs:137-139） | 同业务错误两端状态码语义不同 | 双轨异常语义漂移，客户端错误处理不一致 |
| A4 | 远程 ReportsController 8 端点 vs 桌面 `ReportsHttpApiClient` 仅调 3 个 daily/* | 5 个报表端点（trend/doctor-performance/herbs/ranking/patient-flow）无桌面客户端消费 | 远程死端点候选；或桌面报表功能未完成 |
| A5 | LocalWebApiProgram.cs:108-118 注册 `LocalLogin` 限流（5/60s），文档 05-dual-mode 称本地"不限制" | 文档 vs 代码不一致 | 本地登录实际受限流（单用户场景影响小，但文档误导） |
| A6 | `MedicalCaseApiClient`（Refit 适配，RefitApiClient.cs:87）与 `MedicalCasesHttpApiClient`（HTTP 适配）双实现 IApiClientMedicalCases | 两个适配器方法面是否 1:1 未验证 | 双实现漂移风险：一端加了端点另一端忘加（链 C 已见痕迹） |
| A7 | Shared.Models/Utilities/Security/PasswordHelper.cs:517 嵌套自定义 `PasswordVerificationResult` | 与 Identity 同名类型 | 名称撞车风险，易误用 |
| A8 | 模块 DbContext（Users/Herbs/Formula/Auth）在本地模式经 Resolver 解析连接串 | 与 AppDbContext 的显式 LocalDB 串可能不同库 | 若不同库：本地模式下用户/药材/配方数据落在"另一个库"，用户感知为数据丢失（需运行时验证） |
| A9 | LocalWebAPI 缺失 `print-completed`/`permissions`/`audit-logs` 端点，而本地客户端照常调用 | 打印审计/权限查询/审计日志在本地静默失败 | 静默失败比显式报错更难排查 |
| A10 | CorrelationIdEnricher.cs:14 注释"Desktop端使用AsyncLocalCorrelationIdProvider" vs DesktopSerilogConfiguration.cs:34 实际用 Activity | 注释与实现矛盾 | 误导后续维护者 |
| A11 | PatientsHttpApiClient.cs:39 `POST /patients/import` vs 远程 `batch-import` | 本地客户端 URL 与远程端点不同名 | 即使本地补端点，也应对齐命名或客户端（检查点 6 已列） |
| A12 | 桌面 `PatientSearchManager`（Desktop.Patients）与 `HerbSearchProvider`/`FormulaSearchProvider`（Desktop.Herbs/Formula）同职责异后缀 | 命名不一致 | 检索辅助类命名风格分裂 |
| A13 | `SystemLog` 实体（AppDbContext.cs:77 DbSet）+ `SystemLogRepository`（LocalWebApiProgram.cs:51 注册） | SystemLog 20 文件引用，跨模块共享写日志表 | 实体共享日志表设计的耦合面（观察项，非问题） |

## 5. 与文档偏差清单（文档=设计态 vs 代码=当前态）

| # | 文档（权威） | 文档表述 | 代码现状 | 偏差性质 |
|---|---|---|---|---|
| D1 | 08-shared.md | Shared 层 8 个项目（Primitives/Utilities/Components/Validators…） | 5 个项目，原 4 项坍缩为 Shared.Models 内文件夹 | 结构性过时 |
| D2 | 08-shared.md §Utilities | ConfigurationHelper/PasswordHasher/JwtHelper/PinYinConverter/StringExtensions/DateTimeHelper | 仅 CacheExtensions/PasswordHelper/PasswordPolicyValidator/PinYinHelper | 类型清单过时 |
| D3 | 03-server.md §模块独立 DbContext | ReportsDbContext 已注册（ReportRepository 待清理） | `ReportsDbContext` 代码不存在（Reports 仓储注入 AppDbContext） | 文档超前/代码未实现（或已删除未更新文档） |
| D4 | 03-server.md / Core AGENTS | LocalData 项目提供本地仓储路径 | `LYBT.Desktop.LocalData` 项目不存在，仅 Desktop.Infrastructure/LocalData/LocalDbContext.cs 且生产零引用 | 结构与活性均过时 |
| D5 | 05-dual-mode.md | 本地 URL 前缀 `/api/`（无版本段） | 本地路由 `api/v1/[controller]`（LocalWebAPI 全部控制器） | 实现已收敛但文档未更 |
| D6 | 05-dual-mode.md | 本地 `Database.EnsureCreated` | 本地 `MigrateAsync()` + 双种子（LocalWebApiProgram.cs:139-141） | 迁移策略已变 |
| D7 | 05-dual-mode.md | 本地 DI 精简（Controller→DbContext 直连） | 本地 Controller→ISender/I*Service 全复用（ADR-0010） | 文档滞后于 2026-06-14 统一服务层决策 |
| D8 | 05-dual-mode.md 端点覆盖表 | 患者 12↔14（117%）等 | 本地患者显式端点 7 个、CRUD 主干缺失（500/405） | 覆盖表严重失真 |
| D9 | 05-dual-mode.md TBD-01 | 打印日志本地"端点返回空结果" | 本地无 print-completed 端点（404） | 行为不符（更坏：404 而非空结果） |
| D10 | 05-dual-mode.md | 本地不限制速率 | 本地注册 `LocalLogin` 5/60s 限流 | 小偏差 |
| D11 | 00-architecture-summary.md | JWT 密码=BCrypt | 运行时=Identity PBKDF2；BCrypt 仅残留工具类 | 技术栈表述过时 |
| D12 | 任务书 v1.0 | WebAPI 14 vs LocalWebAPI 12；LocalWebAPI 引用 7 个 Server 模块 | 12 vs 12 同名；引用 8 个模块 | 任务书前提需勘误 |
| D13 | Core AGENTS.md | LocalData 为本地模式数据访问路径 | 本地走 HTTP（HttpClientApiClient→LocalWebAPI）；LocalData 休眠 | 架构路径描述过时 |

## 6. 统计汇总

| 维度 | 数量 |
|---|---|
| 发现问题总数 | 33（F1 3 + F2 2 + F3 8 + F4 5 + F5 3 + 顺带 13） |
| 严重度 | P0 候选 1（本地模式患者/药材/验方 CRUD 主干缺失，建议运行验证后定级）；P1 5（F1-1、F3-1、F3-2、F3-3、F3-6）；P2 其余 |
| 数据流走查 | 4 条链 × 4 问 + 6 检查点（检查点 1/2/3/4/5/6 结论齐备） |
| 文档偏差 | 13 项（08-shared 2、03-server 2、05-dual-mode 5、00-arch 1、任务书 1、AGENTS 2） |
| 死代码 | 新发现 4 确证 + 1 待复核；上轮清理无残留 |
| 机制并存 | 7 项核实（3 项已解决/单机制：ExceptionFactory 已删、ServiceResult 已删、命名空间复数已统一；4 项真并存：CorrelationId、DbContext×5、BCrypt/Identity、契约双套；另 3 项设计内并存：映射、批量、配置存储） |

**审计声明**：全部结论基于静态代码证据（文件:行号 + 引用链），未修改任何代码、未 commit、未运行应用。P0/P1 级功能类结论（本地 CRUD 断裂、模块 DbContext 连接串分歧）建议技术总监运行验证后再定级处置。报告文件由技术总监统一提交。

