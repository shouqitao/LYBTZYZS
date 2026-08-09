# LYBTZYZS 结构设计蓝图（SSOT）

> 版本: v1.5 | 日期: 2026-08-09 | 维护者: 技术总监
> **本蓝图是全项目结构的唯一权威设计文档**——每个 project 的职责、每个 class 的设计依据，都可在此追溯。
> 依据来源：A-16 全局审计（`docs/compose/reports/structure-audit-2026-08-08.md`）+ 模块级审计（`docs/compose/reports/structure-audit-module-level-2026-08-08.md`）+ 逐 class 验证（`docs/compose/reports/structure-audit-perclass-*-2026-08-08.md`）+ 双交叉验证 + 16 份 ADR + 架构文档（00-architecture-summary / 03-server / 05-dual-mode / 08-shared / 06-error-handling / 09-security-architecture / 07-configuration）+ 86 条架构测试守卫。
> 文档与代码冲突时以本蓝图为设计态定义，代码须按蓝图演进。

---

## 0. 设计总纲

### 0.1 系统定位

凌隐宝堂中医诊所管理系统：.NET 8 | WPF/Prism 桌面端 + ASP.NET Core WebAPI 服务端 + SQL Server 数据库，支持**远程（Remote）+ 本地（Local）双模式**。

### 0.2 架构分层（3 层 + 测试）

```
┌─────────────────────────────────────────────────────────┐
│                    DESKTOP 层 (16 项目)                   │
│  Shell(组合根) → Roles(角色工作台) → Modules(业务模块)     │
│  Core: Contracts / Foundation / Infrastructure / Controls │
│  / Printing / LocalWebAPI(本地宿主)                       │
├─────────────────────────────────────────────────────────┤
│                    SHARED 层 (5 项目)                     │
│  Entities(实体) / Shared.Models(DTO/契约/枚举/工具)        │
│  Shared.Configuration(配置) / Shared.ExceptionHandling    │
│  / Shared.Logging(日志)                                   │
├─────────────────────────────────────────────────────────┤
│                    SERVER 层 (10 项目)                     │
│  Core: Infrastructure │ Modules: 8 业务模块               │
│  Services: WebAPI(远程宿主)                                │
└─────────────────────────────────────────────────────────┘
                    TESTS 层 (3 项目)
  Architecture(81 方法/88 用例) / Server / Desktop
```

### 0.3 依赖规则（架构测试强制）

| 规则 | 内容 | 守卫 |
|------|------|------|
| 单向依赖 | Desktop → Shared → (无)；Server → Shared → (无) | ✅ |
| Shared 零反向 | Shared 禁止引用 Server/Client | ✅ |
| 模块隔离 | Server 模块间禁止直接引用（走 ICrossModuleService） | P07 ✅ |
| Desktop 模块隔离 | Desktop 业务模块间禁止直接引用 | DP07 ✅ |
| Service 层约束 | Service 禁注入 AppDbContext（仅 Repository/Base） | P10 ✅ |
| Repository 自治 | Repository 注入自己模块的 DbContext（A-20 后） | ✅ 新增 |
| Controller 继承 | Controller 继承 Base* 基类 | ✅ |
| MVVM | VM 只注入 Service/ViewModelServices，不直连 IApiClient（A-21 M5 修复） | ✅ 新增 |

### 0.4 设计原则（贯穿所有 project）

1. **模块自治**（ADR-0017）：每个业务模块完整 Domain/Application/Infrastructure 三层，独立 DbContext（A-20 落地）
2. **契约单一**（A-18）：桌面对外唯一接口面 `IApiClient`，Refit 特性接口 internal 化
3. **双轨设计**（ADR-0002/0009/0010）：Remote/Local 共享同一业务逻辑（同一 Service 双宿主），切换由用户自主（A-19）
4. **映射单一**（ADR-0011 + A-18 P1-4）：Mapperly 编译期映射，Target 策略
5. **共享单源**：公共类型只在 Shared 定义一次（Gender 样板），禁止两端重复
6. **拒绝屎山**（用户红线）：发现错误直接重写，不做兼容层
7. **Status vs State 语义边界**（2026-08-08 A-26 定案）：**域内持久化状态用 `Status` 枚举**（`MedicalCaseStatus`/`RegistrationStatus`/`FormulaStatus`/`CommonStatus`，存于 `Shared.Models/Enums/`）；**客户端 UI/会话状态用 `State` 枚举**（`WorkspaceEditState`/`EditState`/`AuthState`/`SessionState`/`TokenLifecycleState`）。禁止域状态用 State、会话状态用 Status 的混用

### 0.5 技术栈合理性评估（2026-08-08）

> 评估日期：2026-08-08｜维护者：技术总监｜依据：A-27 任务书（`docs/compose/specs/task-a27-stack-subtraction-2026-08-08.md`）+ 报告（`docs/compose/reports/a27-stack-subtraction.md`）

#### 0.5.1 技术栈全景

| 层 | 技术 | 职责 | 使用量 |
|----|------|------|--------|
| 运行时 | .NET 8 | 全栈运行时（LTS） | 全部 34 项目 |
| 数据 | EF Core 8 | 唯一 ORM/数据访问（Remote SQL Server + Local LocalDB） | Server 8 模块 + Infrastructure + LocalWebAPI |
| 数据 | SQL Server / LocalDB | Remote / Local 双模式数据库 | 生产 + 开发 |
| 桌面 UI | WPF + Prism（DryIoc） | 桌面壳 + MVVM 模块化 | 16 项目 |
| 桌面 UI | CommunityToolkit.Mvvm | MVVM 源生成器（ObservableProperty/RelayCommand） | 全部 ViewModel |
| 桌面 UI | MaterialDesignThemes | 界面主题（M2） | Shell + 模块 |
| 服务端 | ASP.NET Core 8 | WebAPI 双宿主（WebAPI + LocalWebAPI 复用 Server 模块） | 2 宿主 |
| 服务端 | MediatR | CQRS 命令管道（验证+审计） | 7 模块（MedicalCase 例外）|
| 服务端 | SignalR | 实时通知（挂号队列） | Registration + Desktop |
| 契约/映射 | Mapperly | 编译期对象映射（映射单一原则） | Server + Desktop |
| 契约/映射 | FluentValidation | 输入验证唯一管道 | 全部 DTO 验证器 |
| 契约/映射 | Refit | IApiClient 统一契约客户端 | Desktop 全部模块 |
| 认证 | ASP.NET Core Identity（PBKDF2）| 密码哈希/验证唯一方案（UserManager） | Auth + Users |
| 认证 | JWT（JwtBearer） | 令牌认证授权 | WebAPI |
| 日志 | Serilog | 结构化日志（文件/控制台/MSSqlServer）——**A-31-C1 收敛后 Serilog 包仅 `LYBT.Shared.Logging` 持有** | Server + Desktop |
| 文档 | Swashbuckle（Swagger） | OpenAPI 文档（非生产启用） | WebAPI |
| 打印 | QuestPDF | 处方 PDF 导出 | Desktop.Printing |
| 工具 | pinyin4net | 拼音搜索/排序 | Server 导入 + Desktop 搜索 |

> **技术引入治理记录（A-31-C1，2026-08-08 审批）**：`LYBT.Shared.Logging` 升级为独立完整日志项目，获准补充 ASP.NET Core 依赖（`Microsoft.AspNetCore.Http.Abstractions` / `Mvc.Abstractions` / `Mvc.Core`，承载 CorrelationId 中间件、ApiLoggingFilter 及其注册扩展）与 `LYBT.Shared.Configuration` 项目引用（MSSQL sink 读取 DatabaseOptions 连接串）。架构测试 P05b 豁免清单同步：`LYBT.Shared.Logging` 为 Shared 层唯一 AspNetCore 依赖例外。

#### 0.5.2 评估框架（4 标准）

| 标准 | 定义 |
|------|------|
| 必要性 | 承担不可替代的职责（无其他组件可替换） |
| 活跃度 | 实际引用/调用数量（零引用 = 死重量候选） |
| 替代成本 | 迁移/替换所需工作量 |
| 复杂度预算 | 引入的认知负担与维护成本是否可控 |

#### 0.5.3 核心必选 10 项（SSOT，无争议）

`.NET 8`｜`EF Core`｜`WPF+Prism`｜`ASP.NET Core`｜`Identity（PBKDF2）+JWT`｜`Mapperly`｜`FluentValidation`｜`Refit`｜`Serilog`｜`SignalR`

（各项均为对应层唯一实现/唯一方案，使用量见 0.5.1 全景表）

#### 0.5.4 有成本但合理的 3 项

| 项 | 成本 | 合理性 |
|----|------|--------|
| MediatR + Service 双轨 | 双执行路径认知成本 | 命令走管道（验证+审计），查询走 Service 绕过管道——CQRS 经典形态（A-03 简化后保留） |
| Prism 模块化（16 项目） | 项目数多、编译链长 | 模块自治（ADR-0017）与角色工作台（Admin/Clinical）复用的结构代价 |
| Dual-Mode 双轨（Remote+Local） | 双宿主维护 | 同一业务逻辑双宿主（ADR-0002/0009/0010），A-19 后由用户显式切换 |

#### 0.5.5 已移除死重量（2026-08-08 A-27）

| 项 | 处置 | 原因 |
|----|------|------|
| BCrypt（BCrypt.Net-Next） | ✅ 已移除 | 哈希/验证零调用，Identity PBKDF2（UserManager）取代；PasswordHelper 瘦身为纯工具类 |
| Swagger（Swashbuckle） | ➡️ 评估保留 | 已完整接线（AddSwaggerGen + UseSwagger/UseSwaggerUI，非生产启用），成本≈0，B-16 待实现 |
| Velopack | ✅ 未引入 | B-09（自动更新）未做，全仓 0 引用，无包条目 |
| Sqlite（EFCore.Sqlite / Data.Sqlite） | ✅ 已移除 | 本地模式定 LocalDB（SQL Server），仅 Directory.Packages.props 条目残留 |

#### 0.5.6 已配置未启用

| 项 | 状态 |
|----|------|
| Asp.Versioning.Mvc | v1 生效（URL 段版本读取器），v2 预留 |

---

## 1. SHARED 层（5 项目）

> 权威文档：`08-shared.md`（A-18 P1-7 重写版）+ `02-ssot-architecture.md`

| 项目 | 文件数 | 职责 | 设计依据 |
|------|--------|------|---------|
| **LYBT.Entities** | 18 | 领域实体（Patient/Herb/Formula/MedicalCase/Registration/Consultation/Prescription/User/AuthSession 等）| 实体唯一源（2026-08-02 决策）；Server/Desktop 共用 |
| **LYBT.Shared.Models** | 120 | DTO/契约/枚举/工具/验证器（Contracts/Enums/Primitives/Utilities/Validators 八目录）| 原 8 项目坍缩为 1（A-16 发现，08-shared v1.6 文档化）；API 契约双端共享 |
| **LYBT.Shared.Configuration** | 25 | Options 类 + ConnectionStringResolver + 配置验证器 | 07-configuration.md；Server/Client 双端消费 IOptions |
| **LYBT.Shared.ExceptionHandling** | 7 | AppException 层次 + ProblemDetails + 异常处理器 | 06-error-handling.md；异常映射 SSOT |
| **LYBT.Shared.Logging** | 15 | 独立完整日志项目：Serilog 单持有者（含 Sinks.MSSqlServer/AspNetCore）+ Bootstrap 单入口（`AddLybtLogging`/`LoggingBootstrap`）+ CorrelationId 单点（Provider/中间件/Filter/HttpHandler）+ 脱敏。**获准依赖 ASP.NET Core（Http.Abstractions/Mvc.Abstractions，承载 CorrelationIdMiddleware 与 ApiLoggingFilter）** | 08-shared §Logging + 11d-observability + A-31-C1 |

### 1.1 Shared 关键类设计依据

| Class | 依据 |
|-------|------|
| `Gender`/`CommonStatus`/`MedicalCaseStatus` 等枚举 | 共享单源样板——只在 Shared.Models/Enums 定义一次，双端引用 |
| `ApiResponse<T>` | 所有 Controller 响应信封（契约强制），06-error-handling |
| `ErrorCode` | 错误码枚举 SSOT（0xxxx~8xxxx 分区），06-error-handling |
| `Result<T>` | 服务层统一返回类型，避免裸 throw |
| `IEntityInputDto` | A-06 Repository 泛型化的更新 ID 提取约束 |
| `ConnectionStringResolver` | 三级回退（Database:ConnectionString → DefaultConnection → CONNECTION_STRING），03-server P2 |
| `AppException` 层次 | 异常继承树（Business/Validation/NotFound/Conflict/Unauthorized/Api），统一映射 HTTP 状态码 |

---

## 2. SERVER 层（10 项目）

> 权威文档：`03-server.md`（v2.4）+ 各模块 US 需求

### 2.1 Core：LYBT.Infrastructure（87 文件）

**职责**：跨模块基础设施——DbContext（AppDbContext 迁移所有者）、Repository 基类、Base*Controller、批处理基类、跨模块服务接口、验证管道、事务、日志清理、缓存、异常中间件。

| 关键类型 | 依据 |
|---------|------|
| `AppDbContext` | **唯一迁移链所有者**（A-20 方案 A）：物理 schema 单一管理；模块 DbContext 复用其建的表 |
| `BaseRepository<T>` / `BaseApiController` / `BaseCrudController` | Controller 继承规范（A-14 文档化三种路径）|
| `BaseUsersController` / `BaseRegistrationsController` / `BaseMedicalCasesController` | **模块级 Controller 基类**（A-26 补记）：继承链 `BaseApiController:ControllerBase` → `BaseCrudController` → 模块级基类（Users:BaseUsersController.cs:23 / Registration:BaseRegistrationsController.cs:16 / MedicalCase:BaseMedicalCasesController.cs:18），共 5 条终态路径（BaseApiController×6 / BaseCrudController×3 / 模块级×3）。三层继承合理，定案不合并 |
| `BatchOperationHandlerBase<T>` | Q-01 批处理泛型化（模板方法模式）——**第一模板**：按 ID 批量操作（删除/启停）|
| `BatchImport*CommandHandler`（Formula/Herbs/Patients ×3）| **批处理第二模板**（A-29 P2-12 定案）：批量导入独立实现，不继承 `BatchOperationHandlerBase`——因输入（List\<TRowDto\> vs List\<Guid\>）、返回形状（模块专用 ImportResultDto 含行级失败明细/DataSnapshot vs BatchOperationResultDto）根本不同，泛型化需 5+ 抽象钩子 + 结果类型泛型，成本高于 3 处共性。**新增批量导入必须走此模板或先评估收敛，禁止第三个手写导入** |
| `ICrossModuleService` + 各域接口 | 模块间通信唯一通道（P07）|
| `ValidationBehavior<TReq,TRes>` | FluentValidation 管道（2026-08-06 补）|
| `SystemExceptionHandler` | 异常→HTTP 映射（403/404/409/501 已对齐，2026-08-08 批次）|
| `DatabaseInitializationService` | MigrateAsync 幂等迁移 + 重试 |

### 2.2 Modules（8 个业务模块）

| 模块 | 文件数 | 结构模式 | DbContext | 关键依据 |
|------|--------|---------|-----------|---------|
| **LYBT.Module.Auth** | 26 | CQRS（Application/Domain/Infrastructure/Interfaces/Services）| AuthDbContext | ADR-0005（superadmin auth）+ B-21（token 族旋转）+ S-01/S-02 |
| **LYBT.Module.Users** | 29 | CQRS | UsersDbContext | 用户管理（层级恢复 08-04 决策）|
| **LYBT.Module.Patients** | 23 | CQRS | **PatientsDbContext**（A-20 新建）| 患者域（Excel 导入/导出 B-03）|
| **LYBT.Module.Herbs** | 22 | CQRS | HerbsDbContext | 药材域（仅 Admin+ 管理 08-02 决策）|
| **LYBT.Module.Formula** | 21 | CQRS | FormulaDbContext | 验方域 |
| **LYBT.Module.MedicalCase** | 22 | **Service 化**（Command/Query/State 三 Service，无 MediatR）| **MedicalCaseDbContext**（A-20 新建）| ADR-0001（聚合根）+ A-03（MediatR 简化）+ A-14 文档化 |
| **LYBT.Module.Registration** | 27 | CQRS + SignalR Hubs | **RegistrationDbContext**（A-20 新建）| US-REG-008（实时推送）+ D-01（接诊即建）|
| **LYBT.Module.Reports** | 7 | 只读聚合（Service+Repository）| AppDbContext | B-04 报表增强（只读聚合查询，无自有表）|

> **Reports DTO 契约约定（2026-08-08 A-26 定案，P2-18）**：`Infrastructure/ReportQueryModels.cs` 内的 record（ReportDayValueDto/ReportDayCountDto/DoctorPerformancePointDto/PatientFlowPointDto）为**仓储返回形状（模块私有）**；对外 DTO 一律在 `Shared.Models/Contracts/Reports/`（如 DoctorPerformanceDto）。禁止把仓储私有 record 直接当 API 契约返回。

#### 请求处理边界规则（2026-08-08 A-26 定案，T2）

> **统一规则（SSOT，架构测试守卫后强制执行）**：CQRS 模块（Auth/Users/Patients/Herbs/Formula/Registration）内——
> - **写操作**（Create/Update/Delete/Status 变更/Import/Restore）→ 走 **MediatR Handler**（`ISender.Send`）——保证 `ValidationBehavior` 验证管道 + 审计事件统一生效
> - **读操作**（Get/List/Search/Export）→ 走 **Service 直查**（`IXxxService`）——无状态查询不需要管道，省 Handler 样板
> - **禁止**：Controller 层混用同一操作两条路径（如 Update 既走 Service 又走 Handler）；写操作绕过 Handler 直接调 Repository
> - MedicalCase（纯 Service 化）/Reports（只读聚合）为已定案例外，不适用本规则
>
> 依据：A-26 收敛审查（`docs/compose/reports/structure-convergence-mimo-2026-08-08.md` §2.2）——「读走 Service + 写走 Handler」正是 CQRS 经典形态，当前代码方向正确，缺的是规则固化；技术总监判断「移除次要（Service 写操作丢验证管道）保留优秀（Handler 管道 + Service 读直查）」。

### 2.3 Services：LYBT.WebAPI（30 文件）

**职责**：远程宿主——Program.cs 组合根、Controller 层、中间件管道、健康检查、配置端点、部署端点。

| 关键类型 | 依据 |
|---------|------|
| `Program.cs` | 两阶段 Serilog + 中间件 6 阶段顺序 + AddMediatR/AddDbContext 注册（03-server §请求生命周期）|
| `AuthController` / `PatientsController` 等 12 个 | API 端点契约（13b-api-endpoints.md）|
| `CorrelationIdMiddleware` | W3C traceparent 端到端追踪（A-18 P1-3 后 Server 单机制）|
| `ConfigurationController` | 配置修改 API（B-02）+ JsonFileConfigurationStore 持久化 |
| `DeployController` | restart 确认机制（A-13）|

### 2.4 Server 分层规则（三态模板，2026-08-08 A-26 定案）

> 蓝图 v1.3 及之前以「七目录理想模板」表述，实际代码为三态并存（§2.2 表格为准）。本版改为三态模板，标注各模块实际形态，**七目录模板从未完整落地**（全模块无 `Domain/`，实体下沉 LYBT.Entities）。

**状态一：CQRS 模块（Auth/Users/Patients/Herbs/Formula/Registration）**

```
Controllers/           # HTTP 边界（继承 Base*，返回 IActionResult）
Application/           # CQRS：Commands/Queries/Validators/Handlers（写走 Handler、读走 Service，§2.2 边界规则）
Infrastructure/        # Repository（注入模块 DbContext）+ 模块 DbContext
Interfaces/            # 服务/仓储接口
Services/              # Service 实现
Application/Mappers/   # Mapperly（Target 策略）
```

**状态二：Service 化模块（MedicalCase，A-03 定案）**

```
Controllers/           # HTTP 边界
Services/              # Command/Query/State/Prescription/CrossModule 五 Service（无 MediatR）
Repositories/          # Repository（MedicalCaseRepository/MedicalCaseReferenceRepository 等）
Interfaces/            # 11 个服务接口
Mappers/               # MedicalCaseMapper（模块根 Mappers/）
```

**状态三：只读聚合模块（Reports，B-04 定案）**

```
Controllers/           # HTTP 边界
Services/              # ReportService（只读聚合查询）
Infrastructure/        # ReportRepository + ReportQueryModels（复用 AppDbContext，无自有表）
```

**目录差异注记**：
- `Domain/` 目录全模块不存在——实体统一下沉 `LYBT.Entities`（2026-08-02 决策）
- `Mappers/` 位置两种放法：**MedicalCase/Registration 在模块根 `Mappers/`**，其余 CQRS 模块在 `Application/Mappers/`（A-28 定案并存，蓝图记录差异）
- `Infrastructure/` vs `Repositories/` 目录名并存：**MedicalCase 用 `Repositories/`**（4 文件，`Infrastructure/` 仅放 DbContext），其他模块用 `Infrastructure/` 放 Repository + DbContext（A-28 定案并存）

---

## 3. DESKTOP 层（16 项目）

> 权威文档：`05-dual-mode.md` + `08-shared.md` + 各模块 View 需求

### 3.1 Core（6 项目）

| 项目 | 文件数 | 职责 | 设计依据 |
|------|--------|------|---------|
| **LYBT.Desktop.Contracts** | 78 | **统一 API 契约**（IApiClient + 子接口，A-18 方案 A）+ Service 接口 + 导航契约（A-18 P1-5 下沉）| 契约单一（0.4-2）|
| **LYBT.Desktop.Foundation** | 70 | Http 客户端实现（RefitApiClient/HttpClientApiClient/SwitchingApiClient/adapter）+ 基础服务 | ADR-0009（URL 驱动双轨）|
| **LYBT.Desktop.Infrastructure** | 92 | WPF 服务（VM 基类/Dialog/Navigation/Behaviors/Roles/Security）+ IApiClient 实现细节 | Core AGENTS；职责过载已审计（C1，LocalData 已废弃）|
| **LYBT.Desktop.Controls** | 41 | 可复用控件（HerbList/PatientCard 等）+ 事件参数 | 组件解耦（ADR-0006）|
| **LYBT.Desktop.Printing** | 12 | 打印（PrescriptionPrintService/DocumentBuilder/PdfExporter + XAML 模板）| 打印规则（2026-08-03 定案）|
| **LYBT.LocalWebAPI** | 25 | **本地宿主**——薄 ASP.NET Core + 复用 Server 8 模块（ADR-0010）| 双轨设计（ADR-0002）；A-17 补 CRUD |

#### 接口命名三层矩阵（2026-08-08 A-26 补记，A-18 契约单一既定结构）

| 层 | 命名 | 可见性 | 位置 | 职责 |
|----|------|--------|------|------|
| Refit 契约 | `IXxxApi` | **internal**（A-18 后）| `Contracts/Api/` | Refit 特性接口，仅供 Foundation 消费 |
| 唯一对外面 | `IApiClientXxx` | public | `Contracts/ApiClient/` | Desktop 模块唯一注入面（VM 不直连，走 Service）|
| 服务接口 | `IXxxService` | public | `Contracts/Services/` | Service 层接口（Desktop 侧 API 客户端仓储接口在 `Contracts/Repositories/`）|

**跨层镜像接口清单（同名字、不同程序集，设计内镜像防误改）**：`IFormulaRepository` / `IHerbRepository` / `IUserRepository` / `IRegistrationRepository` / `IMedicalCaseRepository` / `IPatientRepository` / `IFormulaService` —— Server 侧为 EF 仓储/服务接口（`src/Server/Modules/*/Interfaces/`），Desktop.Contracts 侧为 API 客户端仓储/服务接口。语义不同不可互相替换，改名须两处同步。

### 3.2 Modules（7 个业务模块）

| 模块 | 文件数 | 结构 | 依据 |
|------|--------|------|------|
| LYBT.Desktop.Auth | 10 | ViewModels/Views/Models + LoginCoordinator | ADR-0005；FirstRunSetup/ServerConfig |
| LYBT.Desktop.Users | 15 | 全目录（Controls/Mappers/Models/Repositories/Services/ViewModels）| 用户管理 UI |
| LYBT.Desktop.Patients | 18 | 全目录 + Interfaces（D1 观察项）| 患者管理 UI |
| LYBT.Desktop.Herbs | 13 | 全目录 | 药材管理 UI |
| LYBT.Desktop.Formula | 14 | 全目录 | 验方管理 UI |
| LYBT.Desktop.MedicalCase | 49 | 目录最全（6 子目录，Dialogs/Reports 等）| 医案工作台（核心）|
| LYBT.Desktop.Registrations | 9 | 精简（Dialogs/Events/Repositories/Services/ViewModels）| 挂号 UI + SignalRClient |

### 3.3 Roles（2 个角色工作台）

| 项目 | 文件数 | 引用模块 | 依据 |
|------|--------|---------|------|
| **LYBT.Desktop.Admin** | 17 | Herbs/Formula/Patients/MedicalCase/Users | 业务管理角色（08-04 角色画像）|
| **LYBT.Desktop.Clinical** | 21 | Herbs/Formula/Patients/MedicalCase/Registration | 临床看诊角色 |

### 3.4 Shell（组合根）

**LYBT.Desktop.Shell**（49 文件）：Prism 组合根——ModuleCatalog/Region/导航/状态栏/统一 ApiClient 注册。

### 3.5 Desktop 分层规则

```
View(XAML) ← binding → ViewModel（[ObservableProperty]/[RelayCommand]）
    → Service 接口（注入，不直连 IApiClient——A-21 M5 强制）
    → Repository → IApiClient{Module}（统一契约）
    → SwitchingApiClient →（Remote: Refit | Local: HttpClient → LocalWebAPI）
```

---

## 4. TESTS 层（3 项目）

| 项目 | 职责 | 依据 |
|------|------|------|
| **LYBT.Tests.Architecture** | 架构守卫（分层/依赖/DbContext/命名/映射）| 架构测试是设计决策的强制约束（2026-08-06 规则）|

> **守卫计数口径（2026-08-08 A-26 定案）**：蓝图「守卫数」= `[Fact]/[Theory]` **方法数**（单方法计 1）。2026-08-09 实测：81 方法（80 Fact + 1 Theory）；**Theory 数据展开后多于方法数**（dotnet test 实际执行 88 用例）。早期蓝图版本（v1.2 起）记 86 为口径演变前的估算值，以实测为准。
| **LYBT.Tests.Server** | Server 集成/单元测试（含 Respawn）| ADR-0003（Integration-first）|
| **LYBT.Tests.Desktop** | Desktop 测试（LocalDB）| 需运行中 WebAPI（C-01 已知环境项）|

---

## 5. 设计依据索引（决策 → 文档追溯）

| 设计决策 | 依据文档 |
|---------|---------|
| 每模块独立 DbContext | ADR-0017 + A-20 落地（同库单迁移方案 A）|
| 双轨（Remote/Local）| ADR-0002 / ADR-0009 / ADR-0010 + 05-dual-mode.md |
| 用户自主切换模式（不自动降级）| A-19 决策（2026-08-08 用户拍板）|
| MedicalCase 聚合根 | ADR-0001 |
| 医案创建时机（接诊即建）| BR-000（2026-08-02）|
| 打印规则 | 2026-08-03 定案（仅 Doctor/IsPrinted/完成后软删）|
| 权限矩阵 | 04-permissions.md + 08-04 终局裁决 |
| 契约统一（IApiClient 唯一面）| A-18 方案 A（2026-08-08）|
| Mapperly 映射 | ADR-0011 + A-18 P1-4 |
| 异常→HTTP 映射 | 06-error-handling.md + 2026-08-08 对齐批次 |
| 领域事件模式（预留）| ADR-0018（当前无订阅者，机制保留）|

---

## 6. 变更记录

| 版本 | 日期 | 变更 |
|------|------|------|
| v1.5 | 2026-08-09 | ① A-29 蓝图维护（P2-8/9/10/16/17 + 顺带4）：§1/§3 文件数回写为实测值（Logging 8/Contracts 78/Foundation 70/Infrastructure 92/Controls 41/Patients 18/Formula 14/MedicalCase 49，§3.3 Roles 补文件数 Admin 17/Clinical 21）。② §2.1 补记 BaseUsersController/BaseRegistrationsController/BaseMedicalCasesController 三条模块级继承路径。③ §2.4 七目录模板改三态模板（CQRS/Service 化/只读聚合）+ Mappers 位置差异 + Infrastructure vs Repositories 目录差异注记。④ §3.1 补接口命名三层矩阵（IXxxApi/IApiClientXxx/IXxxService）+ 跨层镜像接口清单。⑤ §0.4 补 Status vs State 语义边界。⑥ §4 补守卫计数口径注记 |
| v1.4 | 2026-08-08 | ① §2.2 新增「请求处理边界规则」（A-26 T2 定案）：CQRS 模块写操作走 Handler（验证管道+审计）、读操作走 Service 直查；禁止混用。② A-27 成果：§0.5 技术栈合理性评估（全景 18 项/4 标准/必选 10 项/死重量处置）。③ 蓝图 v1.3 记录 A-24 成果 |
| v1.3 | 2026-08-08 | 新增 §0.5 技术栈合理性评估：全景表 / 4 标准 / 核心必选 10 项 / 有成本合理 3 项 / 已移除死重量 4 项（BCrypt 移除、Swagger 评估保留、Velopack 未引入、Sqlite 移除）/ 已配置未启用（Asp.Versioning）。对应 A-27 技术栈减法（`docs/compose/reports/a27-stack-subtraction.md`） |
| v1.2 | 2026-08-08 | ① 记录 A-24 成果：Server 模块 22 类死方法清理（-1175 行，删方法不删类，类保留 A 级依据不变）；机制残留 9 簇清理（-1013 行：AddSharedLogging 双重载、Foundation IApiService/ApiService/RequestDeduplicator 注册孤儿、3 惰性 AuthEvents、Tests.Desktop Traits 18 类型、UserJourneyTestBaseShared、LocalWebApiProgram.RunAsync、UnfinishedCaseChoice 复证已删、LoggingHttpHandler 下沉验证完成；Registration 命名空间复数漂移不改记录 P2）。② 架构守卫 86/86 保持（DP10 验证无新增违规） |
| v1.1 | 2026-08-08 | ① 修复文档偏差 2 处：03-server「ICrossModuleAuthService 未实现」→ 实际已落地为 IAuthCrossModuleService；WebAPI AGENTS.md「14 controllers」→ 实际 12 个（对应本蓝图 §2.3）。② 依据来源补入逐 class 验证（A-22）+ 架构守卫 85→86（DP10）。③ 记录 A-22/A-23 成果：1422 类型 93.6% 有设计依据、孤儿类 D=29 已清理、3 VM 越层已修复。④ 确认 08-shared「BaseEntity 通用字段」与 05-dual-mode「Repository 接口 6 个」为 A 级准确（无偏差） |
| v1.0 | 2026-08-08 | 初版：整合 A-16~A-21 全部审计成果 + 16 ADR + 架构文档，34 项目全量设计依据 |
