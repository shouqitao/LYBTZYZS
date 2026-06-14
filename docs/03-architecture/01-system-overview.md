# 系统架构总览

## 概述

凌隐宝堂中医诊所管理系统采用 Server/Shared/Client 三层架构。Server 层提供 RESTful API 服务，Client 层为 WPF 桌面应用，Shared 层提供两端共享的 DTO、工具类和组件。系统支持**远程模式**（连接 Server WebAPI + 远程 SQL Server）和**本地模式**（嵌入式 LocalWebAPI + 本地 LocalDB）双模式运行，通过 URL 自动切换，业务代码完全复用。详见 [双模式架构](05-dual-mode.md)。

## 系统架构图

```mermaid
graph TB
    subgraph Client["Client 层 (WPF Desktop)"]
        Shell["Shell (应用外壳)"]
        Roles["Roles (Admin/Clinical)"]
        Modules_C["业务模块 x8"]
        Core_C["Core (基础设施)"]
        Shell --> Roles --> Modules_C --> Core_C
    end

    subgraph Shared["Shared 层 (.NET 类库)"]
        Models["Shared.Models (DTO)"]
        Components["Shared.Components"]
        Utilities["Shared.Utilities"]
    end

    subgraph Server["Server 层 (ASP.NET Core)"]
        WebAPI["WebAPI (入口)"]
        Modules_S["业务模块 (8 active + 2 dormant)"]
        Infra["Infrastructure"]
        Entities["Entities"]
        WebAPI --> Modules_S --> Infra --> Entities
    end

    subgraph Data["数据层"]
        SQLServer["SQL Server"]
    end

    Core_C -->|"HTTP API"| WebAPI
    Modules_C --> Models
    Modules_S --> Models
    Infra --> SQLServer
```

### 双模式运行架构

系统支持两种运行模式，通过 URL 自动切换，Repository 层完全无感知：

```mermaid
graph LR
    subgraph Desktop["WPF Desktop"]
        Repo["Repository 层<br/>(统一接口)"]
        Proxy["SwitchingApiClient<br/>(URL 路由代理)"]
        Repo --> Proxy
    end

    subgraph Remote["远程模式 — 多终端联网"]
        Server["Server WebAPI<br/>(独立进程)"]
        RemoteDB["SQL Server<br/>(共享)"]
        Server --> RemoteDB
    end

    subgraph Local["本地模式 — 单终端离线"]
        Kestrel["LocalWebAPI<br/>(嵌入式 Kestrel)"]
        LocalDB["SQL Server LocalDB<br/>(每机独立)"]
        Kestrel --> LocalDB
    end

    Proxy -->|"非 localhost"| Server
    Proxy -->|"localhost"| Kestrel
```

| 维度 | 远程模式 | 本地模式 |
|------|----------|----------|
| **触发条件** | URL 为非 localhost | URL 为 localhost/127.0.0.1 |
| **API 宿主** | 独立 ASP.NET Core 服务 | WPF 进程内嵌 Kestrel |
| **数据库** | 远程 SQL Server（共享） | 本地 SQL Server LocalDB |
| **适用场景** | 多终端联网诊所 | 单终端离线/网络不稳定 |
| **认证** | 完整 JWT（30min + Refresh） | 简化 JWT（1 年长效） |
| **业务代码** | 完全相同 | 完全相同 |

**设计要点**: 两端共享相同的 Repository 接口、DTO 契约、实体配置和 EF Core 模型，差异仅在宿主进程和数据库连接。详见 [双模式架构](05-dual-mode.md)。

## 解决方案结构

```
LYBTZYZS/
src/
  Client/Desktop/                    # WPF 桌面客户端
    Core/                            # 核心库 (8个项目)
      LYBT.Desktop.Contracts/        # 接口定义
      LYBT.Desktop.Foundation/       # 基础设施 (配置、网络、安全)
      LYBT.Desktop.Infrastructure/   # 通用服务、控件
      LYBT.Desktop.Models/           # 客户端模型
      LYBT.Desktop.Printing/         # 打印服务
      LYBT.Desktop.Utilities/        # 工具类库
      LYBT.Desktop.LocalData/        # 本地数据访问 (LocalWebAPI HTTP Proxy Repository)
      LYBT.Desktop.CardReader/       # 身份证读卡器硬件集成
    Modules/                         # 业务模块 (8个)
      LYBT.Desktop.Auth/             # 认证
      LYBT.Desktop.Formula/          # 验方
      LYBT.Desktop.Herbs/            # 药材
      LYBT.Desktop.MedicalCase/      # 医案 (含处方+编辑状态机)
      LYBT.Desktop.Patients/         # 患者 (含读卡器集成)
      LYBT.Desktop.Registration/     # 挂号
      LYBT.Desktop.Sync/             # 数据同步 (含 SyncPhase FSM)
      LYBT.Desktop.Users/            # 用户
    Roles/                           # 角色入口 (3个)
      LYBT.Desktop.Admin/            # 管理员端
      LYBT.Desktop.Clinical/         # 临床端
      LYBT.Desktop.Receptionist/     # 前台接待端
    Shell/
      LYBT.Desktop.Shell/            # 应用外壳

  Server/                            # 后端服务
    Core/                            # 核心层 (2个项目)
      LYBT.Entities/                 # 领域实体 (贫血模型)
      LYBT.Infrastructure/           # 基础设施 (DbContext, Repository基类)
    Modules/                         # 业务模块 (8 active)
      LYBT.Module.Auth/
      LYBT.Module.Formula/
      LYBT.Module.Herbs/
      LYBT.Module.MedicalCase/
      LYBT.Module.Patients/
      LYBT.Module.Registration/      # 挂号管理
      LYBT.Module.Sync/
      LYBT.Module.Users/
      LYBT.Module.Users/
    Services/
      LYBT.WebAPI/                   # Web API 入口

  Shared/                            # 共享库 (3个核心 + 5个工具)
    LYBT.Shared.Components/          # 共享UI组件
    LYBT.Shared.Models/              # DTO、Contract
    LYBT.Shared.Utilities/           # 工具类
    LYBT.Shared.Logging/             # 统一日志抽象
    LYBT.Shared.Validators/          # 共享验证规则 (从 Module Validators 迁移)
    LYBT.Shared.Configuration/       # 共享配置模型
    LYBT.Shared.Primitives/          # 基础类型和常量
    LYBT.Shared.ExceptionHandling/   # 统一异常类型定义

tests/                               # 测试 (4 个项目, Testing Trophy 架构)
    LYBT.Tests.Server/               # Server 全量测试 (~1185 tests, 真实 SQL Server + Respawn, 零 mock)
    LYBT.Tests.Desktop/              # Desktop 全量测试 (~760 tests, SQLite InMemory + 真实 Repository)
    LYBT.Tests.Architecture/         # 架构防护测试 (76 tests, 含 AntiMockRules)
    LYBT.Tests.Integration/          # 集成测试 (Desktop+Server, WebApplicationFactory)
docs/                                # 文档
openspec/                            # OpenSpec 规范 (将废弃)
```

**项目总数**: 约 40+ 个项目

## 依赖方向

### Server 层依赖

```mermaid
graph LR
    WebAPI --> Modules_S["Module.*"]
    Modules_S --> Infrastructure
    Infrastructure --> Entities
    Modules_S --> Shared_M["Shared.Models"]
    Infrastructure --> Shared_M
```

**规则**:
- WebAPI -> Modules -> Infrastructure -> Entities (单向)
- 所有层可引用 Shared.Models
- Module 之间禁止直接依赖，跨模块通过 ICrossModuleService 通信

### Client 层依赖

```mermaid
graph LR
    Shell --> Roles
    Roles --> Modules_C["Desktop.*"]
    Modules_C --> Infrastructure_C["Desktop.Infrastructure"]
    Infrastructure_C --> Foundation["Desktop.Foundation"]
    Foundation --> Contracts["Desktop.Contracts"]
    Modules_C --> Models_C["Desktop.Models"]
```

**规则**:
- Shell -> Roles -> Modules -> Infrastructure -> Foundation -> Contracts (单向)
- 业务模块之间禁止直接依赖

### 跨层依赖

```mermaid
graph TB
    Server["Server 层"] -.->|"引用"| Shared["Shared 层"]
    Client["Client 层"] -.->|"引用"| Shared
    Server x-->|"禁止"| Client
    Client x-->|"禁止"| Server
```

**铁律**:
- Server 和 Client 之间只通过 HTTP API 通信，禁止项目引用
- Shared 层不引用 Server 或 Client 层
- 所有依赖方向必须单向，禁止循环引用

## 模块通信

### Server 端跨模块通信

```mermaid
sequenceDiagram
    participant MC as MedicalCaseService
    participant CMS as ICrossModuleService
    participant PS as PatientRepository

    MC->>CMS: GetPatientBasicInfoAsync(patientId)
    CMS->>PS: 查询患者基本信息
    PS-->>CMS: PatientBasicInfo
    CMS-->>MC: PatientBasicInfo
```

- 使用跨模块服务接口 (ISP 原则，按域拆分):
  - `IPatientCrossModuleService` -- 患者查询 + 引用检查
  - `IHerbCrossModuleService` -- 药材查询 + 引用检查
  - `IUserCrossModuleService` -- 用户查询 + 凭证操作
  - `ICrossModuleAuthService` -- Token 撤销 (独立接口，6 个触发场景)
- 旧 `ICrossModuleService` 标记 `[Obsolete]`，渐进迁移到域专用接口 (S3 实施，详见 d2-d5-design)
- 禁止直接注入其他模块的 Repository
- 返回轻量级 BasicInfo DTO

### Client 端模块通信

- 使用 Prism `IEventAggregator` 发布/订阅事件
- 使用 `IRegionManager` 进行导航
- 禁止模块间直接引用

## 项目命名规范

| 层级 | 前缀 | 示例 |
|------|------|------|
| Server Core | `LYBT.` | LYBT.Entities, LYBT.Infrastructure |
| Server Module | `LYBT.Module.` | LYBT.Module.Patients |
| Server Service | `LYBT.` | LYBT.WebAPI |
| Shared | `LYBT.Shared.` | LYBT.Shared.Models |
| Client Core | `LYBT.Desktop.` | LYBT.Desktop.Foundation |
| Client Module | `LYBT.Desktop.` | LYBT.Desktop.Patients |
| Client Role | `LYBT.Desktop.` | LYBT.Desktop.Clinical |
| Client Shell | `LYBT.Desktop.` | LYBT.Desktop.Shell |

## 架构决策记录

- [ADR-0003: 集成优先测试策略](decisions/0003-integration-first-testing.md) — 真实数据库测试优于 mock 单元测试的 Testing Trophy 策略

## 变更记录

| 日期 | 版本 | 变更内容 |
|------|------|----------|
| 2026-02-10 | v1.0 | 初始版本，从 project-architecture spec 整合 |
| 2026-02-26 | v1.1 | Sprint3-Batch5a DOC3: 标注 Consultation/Prescriptions 空壳模块; 项目数更新为 40+; 新增 Desktop.LocalData/CardReader; 新增 Shared 工具项目 (Logging/Validators/Configuration/Primitives/ExceptionHandling) |
| 2026-02-26 | v1.2 | DOC3-15: 工具层 4 个辅助项目文档化 (Benchmarks/PerformanceTests/CompatibilityTests/TestConfiguration); tests/ 主项目列表展开 |
| 2026-03-04 | v1.3 | Testing Trophy 重构: 5+4 项目 -> 3 项目; 辅助测试项目已删除 |
| 2026-03-09 | v1.4 | Sprint 4: 补充 Registration 模块; Desktop 测试数更新 (482); Integration 测试项目已创建; Consultation Desktop 模块移除 (集成到 MedicalCase) |
| 2026-03-09 | v1.5 | Sprint 5: SQLite->LocalDB 描述修正 (架构图/目录注释/测试描述); Desktop 测试数更新 (493) |
| 2026-06-13 | v1.6 | 补充双模式运行架构章节：Mermaid 架构图展示远程/本地两条数据路径 + 对比表 |
