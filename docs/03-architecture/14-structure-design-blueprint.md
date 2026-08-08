# LYBTZYZS 结构设计蓝图（SSOT）

> 版本: v1.1 | 日期: 2026-08-08 | 维护者: 技术总监
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
  Architecture(85 守卫) / Server / Desktop
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

---

## 1. SHARED 层（5 项目）

> 权威文档：`08-shared.md`（A-18 P1-7 重写版）+ `02-ssot-architecture.md`

| 项目 | 文件数 | 职责 | 设计依据 |
|------|--------|------|---------|
| **LYBT.Entities** | 18 | 领域实体（Patient/Herb/Formula/MedicalCase/Registration/Consultation/Prescription/User/AuthSession 等）| 实体唯一源（2026-08-02 决策）；Server/Desktop 共用 |
| **LYBT.Shared.Models** | 120 | DTO/契约/枚举/工具/验证器（Contracts/Enums/Primitives/Utilities/Validators 八目录）| 原 8 项目坍缩为 1（A-16 发现，08-shared v1.6 文档化）；API 契约双端共享 |
| **LYBT.Shared.Configuration** | 25 | Options 类 + ConnectionStringResolver + 配置验证器 | 07-configuration.md；Server/Client 双端消费 IOptions |
| **LYBT.Shared.ExceptionHandling** | 7 | AppException 层次 + ProblemDetails + 异常处理器 | 06-error-handling.md；异常映射 SSOT |
| **LYBT.Shared.Logging** | 9 | Serilog 配置 + CorrelationId（Activity 单机制，A-18 P1-3）+ 脱敏 | 08-shared §Logging + 11d-observability |

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
| `BatchOperationHandlerBase<T>` | Q-01 批处理泛型化（模板方法模式）|
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

### 2.3 Services：LYBT.WebAPI（30 文件）

**职责**：远程宿主——Program.cs 组合根、Controller 层、中间件管道、健康检查、配置端点、部署端点。

| 关键类型 | 依据 |
|---------|------|
| `Program.cs` | 两阶段 Serilog + 中间件 6 阶段顺序 + AddMediatR/AddDbContext 注册（03-server §请求生命周期）|
| `AuthController` / `PatientsController` 等 12 个 | API 端点契约（13b-api-endpoints.md）|
| `CorrelationIdMiddleware` | W3C traceparent 端到端追踪（A-18 P1-3 后 Server 单机制）|
| `ConfigurationController` | 配置修改 API（B-02）+ JsonFileConfigurationStore 持久化 |
| `DeployController` | restart 确认机制（A-13）|

### 2.4 Server 分层规则（每模块内部）

```
Controllers/           # HTTP 边界（继承 Base*，返回 IActionResult）
Application/           # CQRS：Commands/Queries/Validators/Handlers（MedicalCase 例外）
Domain/                # 实体/值对象/事件
Infrastructure/        # Repository（注入模块 DbContext）
Interfaces/            # 服务/仓储接口
Services/              # Service 实现
Mappers/               # Mapperly（Target 策略）
```

---

## 3. DESKTOP 层（16 项目）

> 权威文档：`05-dual-mode.md` + `08-shared.md` + 各模块 View 需求

### 3.1 Core（6 项目）

| 项目 | 文件数 | 职责 | 设计依据 |
|------|--------|------|---------|
| **LYBT.Desktop.Contracts** | 79 | **统一 API 契约**（IApiClient + 子接口，A-18 方案 A）+ Service 接口 + 导航契约（A-18 P1-5 下沉）| 契约单一（0.4-2）|
| **LYBT.Desktop.Foundation** | 72 | Http 客户端实现（RefitApiClient/HttpClientApiClient/SwitchingApiClient/adapter）+ 基础服务 | ADR-0009（URL 驱动双轨）|
| **LYBT.Desktop.Infrastructure** | 101 | WPF 服务（VM 基类/Dialog/Navigation/Behaviors/Roles/Security）+ IApiClient 实现细节 | Core AGENTS；职责过载已审计（C1，LocalData 已废弃）|
| **LYBT.Desktop.Controls** | 42 | 可复用控件（HerbList/PatientCard 等）+ 事件参数 | 组件解耦（ADR-0006）|
| **LYBT.Desktop.Printing** | 12 | 打印（PrescriptionPrintService/DocumentBuilder/PdfExporter + XAML 模板）| 打印规则（2026-08-03 定案）|
| **LYBT.LocalWebAPI** | 25 | **本地宿主**——薄 ASP.NET Core + 复用 Server 8 模块（ADR-0010）| 双轨设计（ADR-0002）；A-17 补 CRUD |

### 3.2 Modules（7 个业务模块）

| 模块 | 文件数 | 结构 | 依据 |
|------|--------|------|------|
| LYBT.Desktop.Auth | 10 | ViewModels/Views/Models + LoginCoordinator | ADR-0005；FirstRunSetup/ServerConfig |
| LYBT.Desktop.Users | 15 | 全目录（Controls/Mappers/Models/Repositories/Services/ViewModels）| 用户管理 UI |
| LYBT.Desktop.Patients | 25 | 全目录 + Interfaces（D1 观察项）| 患者管理 UI |
| LYBT.Desktop.Herbs | 13 | 全目录 | 药材管理 UI |
| LYBT.Desktop.Formula | 16 | 全目录 | 验方管理 UI |
| LYBT.Desktop.MedicalCase | 50 | 目录最全（6 子目录，Dialogs/Reports 等）| 医案工作台（核心）|
| LYBT.Desktop.Registration | 9 | 精简（Dialogs/Events/Repositories/Services/ViewModels）| 挂号 UI + SignalRClient |

### 3.3 Roles（2 个角色工作台）

| 项目 | 引用模块 | 依据 |
|------|---------|------|
| **LYBT.Desktop.Admin** | Herbs/Formula/Patients/MedicalCase/Users | 业务管理角色（08-04 角色画像）|
| **LYBT.Desktop.Clinical** | Herbs/Formula/Patients/MedicalCase/Registration | 临床看诊角色 |

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
| **LYBT.Tests.Architecture** | 85 条架构守卫（分层/依赖/DbContext/命名/映射）| 架构测试是设计决策的强制约束（2026-08-06 规则）|
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
| v1.2 | 2026-08-08 | ① 记录 A-24 成果：Server 模块 22 类死方法清理（-1175 行，删方法不删类，类保留 A 级依据不变）；机制残留 9 簇清理（-1013 行：AddSharedLogging 双重载、Foundation IApiService/ApiService/RequestDeduplicator 注册孤儿、3 惰性 AuthEvents、Tests.Desktop Traits 18 类型、UserJourneyTestBaseShared、LocalWebApiProgram.RunAsync、UnfinishedCaseChoice 复证已删、LoggingHttpHandler 下沉验证完成；Registration 命名空间复数漂移不改记录 P2）。② 架构守卫 86/86 保持（DP10 验证无新增违规） |
| v1.1 | 2026-08-08 | ① 修复文档偏差 2 处：03-server「ICrossModuleAuthService 未实现」→ 实际已落地为 IAuthCrossModuleService；WebAPI AGENTS.md「14 controllers」→ 实际 12 个（对应本蓝图 §2.3）。② 依据来源补入逐 class 验证（A-22）+ 架构守卫 85→86（DP10）。③ 记录 A-22/A-23 成果：1422 类型 93.6% 有设计依据、孤儿类 D=29 已清理、3 VM 越层已修复。④ 确认 08-shared「BaseEntity 通用字段」与 05-dual-mode「Repository 接口 6 个」为 A 级准确（无偏差） |
| v1.0 | 2026-08-08 | 初版：整合 A-16~A-21 全部审计成果 + 16 ADR + 架构文档，34 项目全量设计依据 |
