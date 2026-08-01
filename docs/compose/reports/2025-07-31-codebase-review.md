# 代码库架构审查报告

> **审查日期**: 2025-07-31
> **审查范围**: LYBTZYZS 全项目 (Server + Desktop + Shared + Tools)
> **构建状态**: ✅ 0 错误 / 0 警告
> **审查目的**: 摸底现有设计问题，为后续重构提供依据

---

## 一、项目规模概览

| 维度 | 数据 |
|---|---|
| 总 C# 文件 | 2,395 (不含 wpftmp) |
| 总 XAML 文件 | 77 |
| 解决方案项目数 | 38 (含 Tests/Tools) |
| 测试文件 | 173 (Architecture 8 / Desktop 109 / Server 56) |
| 数据库迁移 | 10 个 (Latest: `AddRowVersionToAspNetUsers`) |
| 实体数 | 20 个领域实体文件 |
| DTO/Contract | 124 个 Shared.Models 文件 |

### 代码分布

| 目录 | .cs 文件数 | 占比 |
|---|---|---|
| Server/Services (WebAPI) | 449 | 18.7% |
| Client/Desktop/Core | 519 | 21.7% |
| Client/Desktop/Modules | 410 | 17.1% |
| Server/Modules | 343 | 14.3% |
| Shared | 223 | 9.3% |
| Server/Core (Infrastructure) | 90 | 3.8% |
| Client/Desktop/Roles | 187 | 7.8% |
| Client/Desktop/Shell | 120 | 5.0% |
| Client/Desktop/LocalWebAPI | 36 | 1.5% |
| Tools | 16 | 0.7% |

---

## 二、架构结构

```
LYBTZYZS/
├── Server/
│   ├── Core/
│   │   └── LYBT.Infrastructure/          # DbContext, Repository, DI, Caching, Serialization
│   ├── Modules/
│   │   ├── LYBT.Module.Auth/             # 认证 (无跨模块依赖)
│   │   ├── LYBT.Module.Users/            # 用户管理 (依赖: Infrastructure, Entities)
│   │   ├── LYBT.Module.Herbs/            # 药材 (依赖: Infrastructure, Entities)
│   │   ├── LYBT.Module.Formula/          # 验方 (依赖: Infrastructure, Entities)
│   │   ├── LYBT.Module.Patients/         # 患者 (依赖: Infrastructure, Entities, MedicalCase)
│   │   ├── LYBT.Module.MedicalCase/      # 医案 (依赖: Infrastructure, Entities)
│   │   ├── LYBT.Module.Registration/     # 挂号 (依赖: Infrastructure, Entities)
│   │   └── LYBT.Module.Reports/          # 报表 (依赖: Infrastructure, Entities)
│   └── Services/
│       └── LYBT.WebAPI/                  # 入口 + Controllers + DI 配置
├── Client/Desktop/
│   ├── Core/
│   │   ├── LYBT.Desktop.Contracts/       # 60+ 接口定义
│   │   ├── LYBT.Desktop.Controls/        # 自定义控件
│   │   ├── LYBT.Desktop.Foundation/      # 基础工具
│   │   ├── LYBT.Desktop.Infrastructure/  # 客户端基础设施 (含 LocalDbContext!)
│   │   └── LYBT.Desktop.Printing/        # QuestPDF 打印
│   ├── Modules/
│   │   ├── LYBT.Desktop.Auth/            # 登录/认证
│   │   ├── LYBT.Desktop.Users/           # 用户管理 UI
│   │   ├── LYBT.Desktop.Patients/        # 患者管理 UI
│   │   ├── LYBT.Desktop.MedicalCase/     # 医案管理 UI
│   │   ├── LYBT.Desktop.Herbs/           # 药材管理 UI
│   │   ├── LYBT.Desktop.Formula/         # 验方管理 UI
│   │   └── LYBT.Desktop.Registration/    # 挂号管理 UI
│   ├── Roles/
│   │   ├── LYBT.Desktop.Admin/           # 管理员角色
│   │   └── LYBT.Desktop.Clinical/        # 临床角色
│   ├── LocalWebAPI/                      # 嵌入式本地 API
│   └── Shell/                            # WPF 应用入口 (Prism + DryIoc)
├── Shared/
│   ├── LYBT.Entities/                    # Server 端领域实体
│   ├── LYBT.Shared.Models/               # DTO / Contracts / Enums
│   ├── LYBT.Shared.Configuration/        # 配置管理
│   ├── LYBT.Shared.ExceptionHandling/    # 异常体系
│   └── LYBT.Shared.Logging/              # Serilog 日志
└── Tests/
    ├── LYBT.Tests.Architecture/          # 架构守护测试
    ├── LYBT.Tests.Desktop/               # Desktop 集成测试
    └── LYBT.Tests.Server/                # Server 集成测试
```

### 技术栈

| 层面 | 技术 |
|---|---|
| 框架 | .NET 8 |
| Desktop | WPF + Prism (DryIoc) + CommunityToolkit.Mvvm |
| WebAPI | ASP.NET Core + MediatR CQRS + FluentValidation |
| ORM | EF Core + SQL Server (Remote) / LocalDB (Local) |
| 认证 | ASP.NET Core Identity + JWT Bearer |
| 日志 | Serilog (两阶段: Bootstrap + Final) |
| 打印 | QuestPDF |
| 测试 | xUnit + WebApplicationFactory |
| 映射 | Riok.Mapperly |

---

## 三、发现的设计问题

### 🔴 P0 — 严重问题

#### 问题 1: LocalWebAPI 与 Server Controller 代码重复

**现象**: LocalWebAPI 有 12 个 Controller，与 Server WebAPI 的 Controller 名称几乎完全相同，各自继承 `BaseCrudController`。

**代码量对比**:

| Controller | Server (行) | LocalWebAPI (行) | 重复度 |
|---|---|---|---|
| HerbsController | 359 | 141 | 低 (Local 简化版) |
| FormulasController | 362 | 180 | 低 (Local 简化版) |
| PatientsController | 319 | 103 | 低 (Local 简化版) |
| MedicalCasesController | 221 | 170 | 中 |
| AuthController | 181 | 73 | 中 |
| 其余 7 个 | 110-200 | 22-59 | 高 (几乎相同) |

**关键发现**:
- 两者共享相同的 DTO (`Shared.Models`)、相同的 MediatR Query/Command
- 差异主要在: 路由前缀 (`api/v1` vs `api/v{version:apiVersion}`)、认证方式、输出缓存/限流
- Server 有 `ApiVersion`、`OutputCache`、`RateLimiting`，LocalWebAPI 没有

**根因**: 没有 `LYBT.Shared.WebAPI` 或 `LYBT.WebAPI.Core` 项目来承载共享的 Controller 逻辑。LocalWebAPI 是 Server WebAPI 的"独立复制品"。

**风险**: 改一个 Controller 的业务逻辑必须同步改两处。当前已出现分化 (Server 有 `BatchEnable`/`BatchDisable`，LocalWebAPI 也加了但实现不同)。

**建议**: 创建 `LYBT.WebAPI.Core` 项目，将 `BaseCrudController`、共享 Controller 方法、MediatR Pipeline 放入。两个 WebAPI 项目只保留各自的 `Program.cs`、路由配置和认证配置。

---

#### 问题 2: Desktop 直接引用 Server 领域实体

**现象**: `LYBT.Desktop.Infrastructure` 引用了 `LYBT.Entities` (Server 端领域模型)。

**引用链**:
```
LYBT.Desktop.Infrastructure
  └── LYBT.Entities (ProjectReference)
        └── LYBT.Shared.Models
```

**具体引用点**:

| Desktop 文件 | 引用的 Entity |
|---|---|
| `LocalData/Context/LocalDbContext.cs` | BaseEntity, Consultation, Formula, Herb, MedicalCase, Patient, Prescription, Registration, ApplicationUser |
| `LocalWebAPI/Auth/LocalJwtConfig.cs` | ApplicationUser |
| `LocalWebAPI/Controllers/HealthController.cs` | ApplicationUser |
| `LocalWebAPI/Data/LocalWebApiSeedData.cs` | BaseEntity, Patient, Herb, Formula |
| `LocalWebAPI/Handlers/Local*.cs` | ApplicationUser (4 个 Handler) |
| `LocalWebAPI/LocalWebApiProgram.cs` | ApplicationUser |

**根因**: LocalWebAPI 需要直接操作 EF Core + Identity，因此引用了 `AppDbContext` 和 `ApplicationUser`。

**影响**:
- Desktop 编译依赖 Server 领域实体变更
- 无法独立部署 Desktop (如果将来有此需求)
- 违反分层原则: 客户端不应依赖服务端领域模型

**建议**:
- 短期: 将 `ApplicationUser` 提取到 `Shared.Entities` 或 `Shared.Models`
- 长期: LocalWebAPI 使用独立的 DTO 层，通过 MediatR 与 Infrastructure 交互，不直接引用 Entity

---

### 🟡 P1 — 中等问题

#### 问题 3: Server 模块间直接依赖

**当前依赖关系**:
```
LYBT.Module.Patients → LYBT.Module.MedicalCase
```

**影响**: `Patients` 模块变更会触发 `MedicalCase` 模块重编译。虽然只有 1 处直接依赖，但违反了"模块间禁止直接引用"的 AGENTS.md 准则。

**现有缓解措施**: 项目已有 `CrossModuleService` 模式 (Herbs/Patients/Registration/Users 各有一个)，通过接口解耦。

**建议**: 检查 `Patients → MedicalCase` 的依赖是否可通过 `CrossModuleService` 接口替换。如果只是查询医案列表，应通过 `ICrossModuleService` 接口调用。

---

#### 问题 4: Desktop 模块间直接依赖

**当前依赖关系**:
```
LYBT.Desktop.Registration → MedicalCase + Patients + Users
LYBT.Desktop.Admin → Herbs + Formula + Patients + MedicalCase + Users
LYBT.Desktop.Clinical → Herbs + Formula + Patients + MedicalCase + Registration
```

**根因**: Registration 模块需要创建医案、关联患者、关联医生。Admin/Clinical 是角色模块，需要组合多个业务模块。

**影响**:
- Registration 变更会触发 MedicalCase/Patients/Users 重编译
- Admin/Clinical 是"God Role"，几乎引用了所有模块

**建议**:
- Registration 的依赖可通过 `Contracts` 层的接口解耦
- Admin/Clinical 的依赖是合理的 (角色需要组合功能)，但应通过 `Contracts` 而非直接引用 Module

---

#### 问题 5: Contracts 层职责过重

**`LYBT.Desktop.Contracts` 结构**:
```
Contracts/
├── Api/           (10 文件) - Refit API 接口
├── ApiClient/     (11 文件) - IApiClient 系列接口
├── Repositories/  (6 文件)  - 仓库接口
├── Services/      (30 文件) - 服务接口
├── Security/      (1 文件)  - 认证状态机
├── Roles/         (2 文件)  - 角色定义
├── Events/        (1 文件)  - 缓存事件
├── Models/        (3 文件)  - DTO
├── Results/       (1 文件)  - CommandResult
├── Enums/         (1 文件)  - UnfinishedCaseChoice
├── Performance/   (1 文件)  - 性能监控
└── UI/            (1 文件)  - BreadcrumbItem
```

**问题**: 60+ 接口混在一起，Api/ApiClient/Repositories/Services 职责不同但同属一个项目。变更任何一个接口都会触发所有引用者重编译。

**建议**: 拆分为 `Contracts.Api`、`Contracts.Services`、`Contracts.Repositories`。或保持单一项目但在目录层面做好隔离。

---

#### 问题 6: Infrastructure 职责过重

**`LYBT.Infrastructure` 子目录**:

| 子目录 | 文件数 | 职责 |
|---|---|---|
| Data | 19 | DbContext, Configurations, Factory, Initialization |
| Services | 10 | 业务服务 |
| Web | 8 | BaseApiController, BaseCrudController |
| SharedKernel | 6 | DomainEvent, AggregateRoot, Entity |
| Migrations | 18 | EF Core 迁移 |
| Constants | 4 | Policy, Role, ApiVersion, HttpHeader |
| Interfaces | 4 | IDbContextAccessor, IRepository |
| Configuration | 3 | SystemConfiguration |
| ExceptionHandling | 3 | Business/System ExceptionHandler |
| Caching | 2 | CacheInvalidation |
| Repositories | 2 | BaseRepository, SystemLogRepository |
| Logging | 1 | LogCleanupService |
| Serialization | 1 | SensitiveDataJsonConverterFactory |
| Extensions | 1 | QueryablePagingExtensions |
| DependencyInjection | 1 | RepositoryServiceCollectionExtensions |

**问题**: Infrastructure 承担了 Data + Caching + Configuration + DI + Serialization + Logging + Web + SharedKernel + Migrations 等 10+ 职责。

**建议**: 拆分为:
- `LYBT.Infrastructure.Data` (DbContext, Configurations, Migrations)
- `LYBT.Infrastructure.Web` (BaseApiController, BaseCrudController)
- `LYBT.Infrastructure.Caching`
- `LYBT.Infrastructure.SharedKernel` (DomainEvent, AggregateRoot)

---

### 🟢 P2 — 轻微问题

#### 问题 7: wpftmp 临时文件残留

**现象**: 6 个 `_wpftmp.csproj` 文件残留在项目目录中 (非 `obj/` 目录)。

**文件列表**:
- `LYBT.Desktop.Printing_v0cs05ia_wpftmp.csproj`
- `LYBT.Desktop.Auth_zixtcyr3_wpftmp.csproj`
- `LYBT.Desktop.Formula_wgkjflgs_wpftmp.csproj`
- `LYBT.Desktop.Herbs_ijj1lj21_wpftmp.csproj`
- `LYBT.Desktop.Patients_vhwc0ede_wpftmp.csproj`
- `LYBT.Desktop.Users_qwt1zhcv_wpftmp.csproj`

**状态**: `.gitignore` 已包含 `*_wpftmp.csproj` 规则，但这些文件是在 `.gitignore` 规则添加之前创建的，已被 git 跟踪。

**建议**: `git rm --cached` 移除跟踪，或直接删除。

---

#### 问题 8: Server WebAPI 缺少 `MedicalCaseProcessingController`

**现象**: Server 有 `MedicalCaseProcessingController.cs`，但 LocalWebAPI 没有对应文件。

**影响**: 这可能是有意为之 (LocalWebAPI 不支持某些处理流程)，但缺乏文档说明。需确认是否为功能缺失。

---

#### 问题 9: Desktop 没有 `Registration`/`Reports` 模块的 Role 集成

**现象**: Desktop Roles 目录下只有 `Admin` 和 `Clinical`，没有 `Receptionist` 角色模块。

**影响**: 挂号功能 (`RegistrationModule`) 按 AGENTS.md 应由 `Receptionist` 角色使用，但没有对应的 Role 模块来组合 Registration + Users 等功能。

**建议**: 确认是否需要 `LYBT.Desktop.Receptionist` 角色模块。

---

## 四、架构亮点

### ✅ 做得好的方面

| 方面 | 评价 |
|---|---|
| **双模式架构** | 远程/本地透明切换，`SwitchingApiClient` 设计优雅，业务代码完全模式无关 |
| **模块化设计** | Server 8 个模块 + Desktop 7 个模块，职责清晰 |
| **基础架构完善** | 异常体系 (AppException → Business/Validation/NotFound)、日志脱敏、配置验证、健康检查 |
| **架构守护测试** | `LYBT.Tests.Architecture` 包含 AggregateRoot、AntiMock、CustomControl、DesktopLayer、Server、LocalWebApiPattern 等测试 |
| **MVVM 规范** | 统一使用 CommunityToolkit.Mvvm `[ObservableProperty]`/`[RelayCommand]` |
| **CQRS 模式** | MediatR Command/Query 分离，Pipeline Behavior 统一处理 |
| **BaseCrudController** | 泛型 CRUD 基类消除 Controller 重复代码 (但 LocalWebAPI 的重复问题仍在) |
| **CrossModuleService** | 4 个模块通过接口解耦跨模块调用 |
| **构建质量** | 0 错误 0 警告，代码质量可控 |

### ✅ 与 AGENTS.md 一致的设计

| 准则 | 实现 |
|---|---|
| sysadmin=独立用户非角色 | `ApplicationUser.IsSysAdmin=true` |
| 术语规范 | Consultation=中医诊断, MedicalCase=医案, Formula=验方 |
| CommunityToolkit.Mvvm | 所有 51 个 ViewModel 已迁移 |
| Riok.Mapperly | 编译期映射 |
| 三级异常体系 | AppException → BusinessException/ValidationException/NotFoundException |
| API 版本管理 | `api/v1/[controller]` |

---

## 五、依赖关系全景

### Server 模块依赖

```
LYBT.WebAPI
  ├── LYBT.Module.Auth
  ├── LYBT.Module.Users
  ├── LYBT.Module.Herbs
  ├── LYBT.Module.Formula
  ├── LYBT.Module.Patients
  ├── LYBT.Module.MedicalCase
  ├── LYBT.Module.Registration
  └── LYBT.Module.Reports

LYBT.Module.Patients → LYBT.Module.MedicalCase (唯一跨模块依赖)
其他模块: 仅依赖 Infrastructure + Entities (无跨模块依赖)
```

### Desktop 模块依赖

```
LYBT.Desktop.Shell
  ├── LYBT.LocalWebAPI (→ Server Modules + Infrastructure + Entities)
  ├── LYBT.Desktop.Auth
  ├── LYBT.Desktop.Users
  ├── LYBT.Desktop.Patients
  ├── LYBT.Desktop.MedicalCase
  ├── LYBT.Desktop.Herbs
  ├── LYBT.Desktop.Formula
  ├── LYBT.Desktop.Registration
  ├── LYBT.Desktop.Clinical (→ Herbs+Formula+Patients+MedicalCase+Registration)
  ├── LYBT.Desktop.Admin (→ Herbs+Formula+Patients+MedicalCase+Users)
  └── LYBT.Desktop.Printing

LYBT.Desktop.Registration → MedicalCase + Patients + Users
```

### Shared 层依赖

```
LYBT.Entities → LYBT.Shared.Models
LYBT.Shared.ExceptionHandling → LYBT.Shared.Models
LYBT.Shared.Logging → LYBT.Shared.Models
LYBT.Shared.Configuration (独立)
```

---

## 六、重构建议优先级

| 优先级 | 问题 | 建议 | 预估工作量 | 影响范围 |
|---|---|---|---|---|
| **P0-1** | LocalWebAPI 重复 | 创建 `LYBT.WebAPI.Core` 共享 Controller 逻辑 | 中 (3-5 天) | Server + LocalWebAPI |
| **P0-2** | Desktop→Entities | 提取 `ApplicationUser` 到 Shared 层 | 小 (1-2 天) | Entities + Desktop |
| **P1-1** | Server 模块依赖 | 检查 Patients→MedicalCase 是否可通过 CrossModuleService | 小 (0.5 天) | 2 个 Module |
| **P1-2** | Desktop 模块依赖 | Registration 依赖通过 Contracts 接口解耦 | 中 (2-3 天) | Registration + Contracts |
| **P1-3** | Infrastructure 拆分 | 拆分为 Data/Web/Caching/SharedKernel 子项目 | 大 (5-7 天) | 所有 Server 项目 |
| **P2-1** | Contracts 拆分 | 按职责拆分 Api/Services/Repositories | 中 (2-3 天) | Desktop |
| **P2-2** | wpftmp 清理 | git rm --cached + 删除 | 小 (0.5 天) | 6 个文件 |
| **P2-3** | MedicalCaseProcessing | 确认 LocalWebAPI 是否需要此 Controller | 小 (0.5 天) | LocalWebAPI |

---

## 七、下一步行动

### 立即可做 (无需改动代码)
1. 删除 6 个 wpftmp 临时文件
2. 确认 `MedicalCaseProcessingController` 在 LocalWebAPI 的必要性
3. 确认是否需要 `Receptionist` 角色模块

### 第一批重构 (低风险)
1. 提取 `ApplicationUser` 到 Shared 层
2. 检查 `Patients → MedicalCase` 依赖的解耦可能性

### 第二批重构 (中风险)
1. 创建 `LYBT.WebAPI.Core` 消除 Controller 重复
2. Registration 依赖通过 Contracts 接口解耦

### 第三批重构 (高风险)
1. Infrastructure 拆分
2. Contracts 按职责拆分

---

*报告生成于 2025-07-31 by AI Agent*
*审查范围: 全项目代码结构 + 依赖关系 + 架构模式*
*未涉及: 具体业务逻辑正确性、性能、安全性深度审计*
