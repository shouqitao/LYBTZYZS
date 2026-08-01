# 代码审查报告：重复定义与不统一问题

- **日期**: 2026-08-01
- **范围**: 全代码库（codebase-memory 知识图谱，28,122 节点 / 67,287 边）
- **方法**: 图谱查询（同名符号、SIMILAR_TO 相似边、调用关系、死代码）+ 源码精读验证
- **结论**: 本阶段为**纯分析，未修改任何代码**

## 严重级别说明

| 级别 | 含义 |
|------|------|
| 🔴 | 高风险：重复定义造成数据/行为分叉，或死代码需立即决策 |
| 🟡 | 中风险：命名/模式不一致，增加维护成本，建议统一 |
| 🔵 | 低风险：可选的代码质量改进 |

---

## 一、重复定义

### 🔴 1. Server 模块 Domain 实体 与 Shared `LYBT.Entities` 双模型并存

Server 模块重构（2026-06 起）引入了 DDD `Domain/` 实体（`Entity, IAggregateRoot`），但旧的 `Shared/LYBT.Entities` POCO 模型仍在生产代码中使用，同一业务概念存在两套类。

| 概念 | Domain 版 | Shared.Entities 版 | 实际使用 |
|------|-----------|-------------------|----------|
| Herb | `src/Server/Modules/LYBT.Module.Herbs/Domain/Herb.cs:10`（205 行，DDD 带 `Create()`/`UpdateProfile()`） | `src/Shared/LYBT.Entities/Herbs/HerbModel.cs:16`（72 行 POCO） | **两套都在用**：Domain 版 24 次引用（HerbRepository/HerbsDbContext），Shared 版 4 次（HerbConfiguration/LocalDbContext/HerbReferenceRepository） |
| Patient | `src/Server/Modules/LYBT.Module.Patients/Domain/Patient.cs:11`（150 行） | `src/Shared/LYBT.Entities/Patients/PatientModel.cs:17`（157 行） | **Shared 版为主**（PatientRepository/IPatientRepository/6 个 Handler 全用 `LYBT.Entities.Patients`）；Domain 版仅自引用（callers=0）→ **死代码** |
| Formula | `src/Server/Modules/LYBT.Module.Formula/Domain/Formula.cs:10`（216 行） | `src/Shared/LYBT.Entities/Formulas/FormulaModel.cs:15`（187 行） | Domain 版仅被 FormulaConfiguration + 自引用（≈死代码）；业务路径用 Shared 版 |
| Registration | `src/Server/Modules/LYBT.Module.Registration/Domain/Registration.cs:12`（186 行） | `src/Shared/LYBT.Entities/Registrations/RegistrationModel.cs:13`（127 行） | Domain 版仅自引用 → **死代码**；业务用 Shared 版（3 次） |
| FormulaHerbItem | `src/Server/Modules/LYBT.Module.Formula/Domain/FormulaHerbItem.cs:10`（92 行） | `src/Shared/LYBT.Entities/Formulas/FormulaHerbItem.cs:14`（128 行） | **三份**：还有 `src/Client/Desktop/Modules/LYBT.Desktop.Formula/Models/Items/FormulaHerbItem.cs:10`（84 行） |
| AuthSession | `src/Server/Modules/LYBT.Module.Auth/Domain/AuthSession.cs:10`（105 行） | `src/Shared/LYBT.Entities/Auth/AuthSessionModel.cs:13`（51 行） | 两套并存 |

**修复建议**：
- 立即删除死代码：`Domain/Patient.cs`、`Domain/Registration.cs`、`Domain/Formula.cs`（如无隐含引用，以 `serena_find_referencing_symbols` 复核后删除）。
- 中长期：以 `Shared/LYBT.Entities` 为唯一实体源（18 个项目已引用它），将各模块 `Domain/*.cs` 中仍被引用的领域方法（如 `Herb.Create`/`UpdateProfile`）下沉为扩展方法或直接删除，消除双模型。
- Desktop `Models/Items/FormulaHerbItem.cs` 如仅做 UI 状态补充，改为 `Partial` 扩展或独立 UI 包装类，不再复制数据字段。

### 🔴 2. Controller 层批量操作复制粘贴（指纹完全相同）

图谱 SIMILAR_TO 边（jaccard=1.0）确认多组方法体逐字节相同（仅文案/命令名不同）：

- `src/Server/Services/LYBT.WebAPI/Controllers/HerbsController.cs:216` `BatchDelete` ↔ `src/Server/Services/LYBT.WebAPI/Controllers/PatientsController.cs:203` `BatchDelete`（指纹 `02028403...` 完全一致）
- `src/Server/Services/LYBT.WebAPI/Controllers/FormulasController.cs` 的 `BatchDelete`/`BatchEnable`/`BatchDisable` 与 Herbs/Patients 同名方法互为副本
- `src/Client/Desktop/LocalWebAPI/Controllers/*.cs` 全套 12 个 Controller 与 Server WebAPI 同名 Controller 重复（`BatchCheckReference`、`BatchDelete` 等 jaccard=1.0）

**修复建议**：`BaseCrudController`（`src/Server/Core/LYBT.Infrastructure/Web/BaseCrudController.cs:13`）已抽象 CRUD 模板，把 `BatchDelete`/`BatchEnable`/`BatchDisable`/`BatchCheckReference` 提升为受保护泛型模板方法（命令类型由子类注入），消除 4+ 处逐字复制。

### 🔴 3. Desktop Repository 批量操作复制粘贴（指纹完全相同）

`HerbRepository.BatchDeleteAsync` 与 `UserRepository.BatchDeleteAsync` 指纹完全一致（`00c634bd...`），仅日志前缀 `[REPO] Herb.`/`[REPO] User.` 与 `_apiClient.Herbs`/`_apiClient.Users` 不同：

- `src/Client/Desktop/Modules/LYBT.Desktop.Herbs/Repositories/HerbRepository.cs:230`
- `src/Client/Desktop/Modules/LYBT.Desktop.Users/Repositories/UserRepository.cs:286`
- 同指纹扩散到 Formula/MedicalCase Repository 与 RemoteHerbService/FormulaService/PatientService（SIMILAR_TO 显示 15+ 对 jaccard=1.0）

**修复建议**：提取 `BatchOperationBase`（或泛型 `IBatchOperationRepository<T>`），封装「try → 调 API → 失败构造 BatchOperationResultDto → catch → 记日志」公共逻辑，子类只传 `Func<List<Guid>, Task<ApiResponse<BatchOperationResultDto>>>` 与日志标签。

### 🟡 4. PrescriptionItem 同名不同职责（Shared POCO vs Desktop UI 模型）

- `src/Shared/LYBT.Entities/Prescriptions/PrescriptionItem.cs:13`（79 行，EF POCO）
- `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Models/Items/PrescriptionItem.cs:23`（446 行，`BindableBase + IDataProvider + IValidatable + INotifyDataErrorInfo` UI 模型）

同名类语义完全不同：一个持久化实体、一个 UI 视图模型。且 UI 版内部 `s_mapper = new PrescriptionMapper()` 持静态实例，与 `MedicalCaseMapper` 职责交叠。

**修复建议**：UI 版重命名为 `PrescriptionItemViewModel`（或 `PrescriptionEditableItem`），避免与实体同名混淆；静态 mapper 实例改注入。

### 🟡 5. 超大类型/方法（可拆分候选）

| 类型/方法 | 位置 | 规模 |
|-----------|------|------|
| `MedicalCaseCommandService` | `src/Server/Modules/LYBT.Module.MedicalCase/Services/MedicalCaseCommandService.cs` | 854 行 |
| `NavigableViewModelBase` | `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/ViewModels/Base/NavigableViewModelBase.cs` | 688 行 |
| `PrescriptionPrintService` | `src/Client/Desktop/Core/LYBT.Desktop.Printing/Services/PrescriptionPrintService.cs` | 688 行（其中 `CreateSettingsPanel` 单方法 129 行） |
| `HttpClientApiClient` | `src/Client/Desktop/Core/LYBT.Desktop.Foundation/Http/HttpClientApiClient.cs` | 674 行 |
| `MedicalCaseRepository` | `src/Server/Modules/LYBT.Module.MedicalCase/Repositories/MedicalCaseRepository.cs` | 663 行 |
| `PasswordHelper` | `src/Shared/LYBT.Shared.Models/Utilities/Security/PasswordHelper.cs` | 592 行 |
| `MedicalCaseWorkspaceViewModel` | `src/Client/Desktop/Roles/LYBT.Desktop.Clinical/ViewModels/MedicalCaseWorkspaceViewModel.cs` | 558 行 |
| `ErrorCode.ToHttpStatusCode`/`ToCategory` | `src/Shared/LYBT.Shared.Models/Primitives/ErrorCodes/ErrorCodeExtensions.cs:12,143` | 127/137 行巨型 switch |
| `LoginCommandHandler.Handle` | `src/Server/Modules/LYBT.Module.Auth/Application/Commands/LoginCommandHandler.cs:49` | 139 行，complexity 6 |

**修复建议**：`MedicalCaseCommandService` 按 命令侧/复制处方/打印/审计 拆分为多个服务（模块已有 `MedicalCasePrintService` 等先例）；`ErrorCodeExtensions` 巨型 switch 改查表；其余按 300 行/50 行经验阈值逐步拆。

### 🔵 6. 疑似死代码（无引用）

- `MedicalCaseCommandService.CopyHistoricalPrescriptionAsync`（`MedicalCaseCommandService.cs:256`）public 接口 in_degree=0，仅有同名私有 `ExecuteCopyHistoricalPrescriptionAsync` 被调用——公共包装疑似无消费者。
- `src/Tools/ApiTester/Program.cs`、`src/Tools/LoginTester/Program.cs`、`src/Tools/UserInfoVerifier/Program.cs` 中重复的 `JsonSerializer` 内部类（3 处，各 12 行）——各自独立可保留，但属于典型的复制型工具代码。

---

## 二、命名不统一

### 🟡 1. 同一概念三层命名风格不同

同一领域对象在 Entities / DTO(Contracts) / Desktop Models 三层命名后缀不一致：

| 领域 | Entities | Shared.Contracts DTO | Desktop UI Models |
|------|----------|---------------------|-------------------|
| 患者 | `PatientModel.cs` 内类名 `Patient` | `PatientListDto`/`PatientDetailDto`/`PatientInputDto` | `PatientDetailModel`/`PatientItem` |
| 药材 | `HerbModel.cs` 内类名 `Herb` | `HerbListDto`/`HerbInputDto` | `HerbItem` |
| 验方 | `FormulaModel.cs` 内类名 `Formula` | `FormulaListDto`/`FormulaDetailDto` | `FormulaDetailModel`/`FormulaItem` |
| 处方项 | `PrescriptionItem.cs` 内类名 `PrescriptionItem` | `PrescriptionItemDto`/`PrescriptionItemInputDto` | `PrescriptionItem`（446 行 UI 类） |

且 Entities 类名与文件名不一致（`HerbModel.cs` 里是 `class Herb`），PascalCase 后缀规则（Model/Item/Dto）未统一。

**修复建议**：确立统一后缀约定（如：实体无后缀 `Herb`、DTO `HerbDto`、UI 模型 `HerbItem`），文件名与类名一致；`HerbModel.cs`→ 重命名文件为 `Herb.cs` 或类改为 `HerbEntity`。

### 🟡 2. Mapper 方法命名三种风格

Desktop Mapper 中同样功能的方法命名不统一：

- `ToInputDtoCore`（`PrescriptionMapper.cs:123`、`FormulaMapper.cs:201`、`UserMapper.cs:79`、`MedicalCaseDetailModelMapper.cs:120`、`ConsultationMapper`）
- `ToItemCore` / `ToDtoCore`（`FormulaDetailModelMapper.cs:34,83`、`FormulaMapper.cs:37,94`、`MedicalCaseDetailModelMapper.cs:42`）
- 而 Server 侧（`MedicalCaseMapper`）用 `ToDetailDto`/`ToPrescriptionDetailDto` 直接命名

**修复建议**：统一为 `ToXxx`（公开）+ `ToXxxCore`（protected 抽象）或全部去掉 Core 后缀使用 `[UserMapping(Default=false)]`（Server 侧先例）。

### 🟡 3. 相似功能 Server vs Desktop 命名差异

| 功能 | Server | Desktop |
|------|--------|---------|
| 批量删除命令 | `BatchDeleteHerbsCommand`/`BatchDeletePatientsCommand` | `BatchDeleteAsync` |
| 映射器 | `Mapping/MedicalCaseMapper.cs`（命名空间 `Mapping`） | `Mappers/MedicalCaseDetailModelMapper.cs`（命名空间 `Mappers`） |
| 引用检查 | `BatchCheckHerbReferenceQuery` | `BatchCheckReference` |

文件夹命名 `Mapping` vs `Mappers` 并存（Server 模块内也有 `Application/Mappers/`）。**修复建议**：全库统一 `Mappers` 目录名。

### 🔵 4. 中英文混用

代码注释/文档中文为主（符合规范），但发现英文残留：`MedicalCaseCommandService.cs:309` `// TODO:价格刷新在后续实现` 与 `// 4) copy items (price refresh TODO noted...)`（316 行）混用中英；`RetryPolicyExtensions.cs:35` 日志模板中文、`Docs` 中 `TASK-06: LocalData Mapper` 标题乱码（`docs/03-architecture/implementation-tasks.md:70`）。**修复建议**：统一注释为中文、日志模板为中文。

---

## 三、模式不统一

### 🟡 1. ApiResponse&lt;T&gt; 信封一致性 — ✅ 基本统一

- Server WebAPI 12 个 Controller 全部继承 `BaseApiController` 或 `BaseCrudController`（`LYBT.Infrastructure/Web/BaseApiController.cs:18`），`Success()`/`BusinessFail()`/`ValidationFail()` 统一包装，未发现裸 `Ok()`。
- LocalWebAPI 12 个 Controller 同样统一（复用同一套基类）。
- **注意**：`BaseApiController`/`BaseCrudController` 与 3 个 `Base{Entity}Controller`（`BaseUsersController.cs:22`、`BaseRegistrationsController.cs:16`、`BaseMedicalCasesController.cs:19`）形成三层继承链，Controller 自身多为薄壳（如 `UsersController.cs:15` 仅 23 行）——抽象合理，但批量操作重复（见 🔴2）说明模板抽象不彻底。

### 🟡 2. Repository 模式 vs 直接 DbContext

- 各模块 Repository 模式总体遵守（`BaseRepository<T>`，Service 不注入 DbContext，有架构测试守护）。
- **例外**：`AuthService` 绕过 Repository 直接操作 RefreshToken DbContext —— 模块 AGENTS.md 自认 ANTI-PATTERN（`LYBT.Module.Auth/AGENTS.md`：`Direct DbContext for RefreshToken — AuthService bypasses Repository pattern`）。`src/Server/Modules/LYBT.Module.Auth/Services/AuthService.cs`。
- Desktop `LocalData/LocalDbContext` 直接使用 `Shared/LYBT.Entities` 实体（与 Server 双模型呼应，见 🔴1）。

**修复建议**：AuthService 的 RefreshToken 操作收敛到 `RefreshTokenRepository`；LocalData 保持现状但实体源统一后自动对齐。

### 🟢 3. async/await — ✅ 无阻塞调用

全库搜索 `.Result`/`.Wait()`/`GetAwaiter().GetResult()` ：
- 命中均为**误报**：`RetryPolicyExtensions.cs:32` 的 `outcome.Result` 是 Polly `DelegateResult.Result` 属性；`PatientSearchCache.cs:90` 的 `node.Value.Result` 是缓存条目属性。**未发现 `.Result`/`.Wait()` 同步阻塞**，async/await 使用一致。

### 🟡 4. Mapper 映射方式 — 编译期为主，存在手动映射

- Riok.Mapperly 编译期映射是主流：Server `MedicalCaseMapper`（`Mapping/MedicalCaseMapper.cs:21`，`[Mapper(RequiredMappingStrategy = Target)]`）、`RegistrationMapper`；Desktop 全部 Mapper 均 `[Mapper(RequiredMappingStrategy=...)]`。
- **不一致点**：
  - `LoginCommandHandler.MapToUserDetailDto`（`LoginCommandHandler.cs:189`）为纯手动 `new(){}` 赋值，未用 Mapperly——建议改为 `UserMapper` 或模块级 Mapperly。
  - Desktop Mapper 用「Mapperly 生成 + `ToXxxCore` 手动补充」混合模式，`Core` 后缀命名不统一（见 🟡2）。
  - `PrescriptionItem`（Desktop UI 模型）内部 `s_mapper = new PrescriptionMapper()` 静态持 mapper 实例（`PrescriptionItem.cs:25`），与依赖注入风格不一致。

---

## 四、误报排除（已核实）

| 疑点 | 结论 |
|------|------|
| `ExceptionFactory.cs` 中 `Herb`/`Patient`/`Formula`/`MedicalCase` 同名类 | 静态**异常工厂**类（非实体），合理设计，非重复 |
| `Program` 同名 ×5 / `ServiceCollectionExtensions` ×3 / 各 `XxxModule` ×2 | 每个项目入口/注册点，正常 |
| Migration `BuildTargetModel`/`Up`/`Down` 超大方法 | EF 自动生成，非手写，不列入 |
| 测试同名类（`HerbBuilder` 等 Server/Desktop 各一份） | 测试基建按项目隔离，可接受（如需复用可下沉 Shared） |
| `JsonSerializer` ×3 | Tools 各自独立 exe，可接受（🔵 备注） |

---

## 五、修复优先级建议

| 优先级 | 动作 | 涉及文件 |
|--------|------|----------|
| P0 | 删除死代码实体：`Domain/Patient.cs`、`Domain/Registration.cs`、`Domain/Formula.cs`（复核引用后） | `src/Server/Modules/*/Domain/` |
| P0 | `BaseCrudController` 模板化批量操作（BatchDelete/Enable/Disable/CheckReference） | `LYBT.Infrastructure/Web/BaseCrudController.cs` + 6 个 Controller |
| P1 | 提取 Desktop Repository 批量操作基类 | 4 个 Desktop Repository + 3 个 Service |
| P1 | 统一实体源（以 `Shared/LYBT.Entities` 为准，删除模块 Domain 双胞胎） | 全部 Server 模块 |
| P2 | 统一 Mapper 命名（`ToXxx`/`Core` 后缀）、`LoginCommandHandler` 改 Mapperly | Desktop `Mappers/` + `LoginCommandHandler.cs` |
| P2 | `PrescriptionItem` UI 类改名 + mapper 注入化 | `LYBT.Desktop.MedicalCase/Models/Items/` |
| P3 | 目录统一（`Mapping`→`Mappers`）、注释语言统一、超大方法拆分 | 全库 |

---

## 附：分析方法说明

- 图谱：`codebase-memory` MCP，`query_graph` Cypher 查询同名 Class/Enum/Method、`search_graph` 死代码通道、`SIMILAR_TO` 边（jaccard=1.0 指纹比对）、`USAGE`/`CALLS` 引用关系。
- 源码精读：`get_code_snippet` + 文件读取交叉验证，排除 5 类误报。
- 未修改任何代码；本报告为后续重构（P0-P3）的输入基线。
