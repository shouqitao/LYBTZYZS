# 测试体系重构设计

**日期**: 2026-03-05
**状态**: 设计阶段 (brainstorm 完成，待细化 plan)
**目标**: 化繁为简，确保核心流程端到端跑通，测试真实反映代码现状

---

## 1. 核心决策

### 1.1 本地模式数据库变更

**需求调整**: 本地模式从 SQLite 迁移到 SQL Server LocalDB。

| 项 | 之前 | 之后 |
|----|------|------|
| 远程模式 | Desktop -> HTTP -> Server -> SQL Server | 不变 |
| 本地模式 | Desktop -> LocalDataSource -> **SQLite** | Desktop -> Service -> Repository -> **SQL Server LocalDB** |
| DbContext Provider | 2 个 (SQLite + SQL Server) | 1 个 (SQL Server) |
| SQL 方言差异风险 | 存在 | 消除 |
| 测试数据库 | Desktop 用 SQLite, Server 用 SQL Server | 全部用 SQL Server |

**连锁简化**:
- LocalDataSource 5 个类可能不再需要 (共享 Repository)
- DesktopFixture 从 SQLite InMemory 改为 SQL Server + Respawn
- Desktop 和 Server 测试共享同一套 Fixture 基础设施

### 1.2 测试路线图 (三阶段)

```
阶段 0: 本地模式 SQLite -> SQL Server LocalDB 迁移
   |
阶段 1: WebAPI 正确性              已完成 (Server 1017 tests)
   |
阶段 2: Desktop + API 联通         新建 LYBT.Tests.Integration (~30 tests)
   |
阶段 3: Desktop 本地模式跑通        精简现有 + 补缺口 (~330 tests)
   |
横切层: 架构防护                    已有 (68 tests), 随阶段扩展
```

### 1.3 测试原则

- **流程跑通优先**: 核心业务流有 E2E 测试证明能跑
- **删除假绿测试**: mock 交互验证 (Received/DidNotReceive) 全部删除
- **边界后补**: 本阶段不追求覆盖率
- **Mock 白名单**: 仅 mock 硬件(读卡器)/WPF Shell(对话框/导航)/HTTP 管道
- **统一数据库**: 全部测试项目使用 SQL Server (消除 SQLite 方言差异)

---

## 2. 测试项目架构

### 2.1 项目结构

```
tests/
  LYBT.Tests.Server/        API 正确性 (已有, 1017 tests, SQL Server + Respawn)
  LYBT.Tests.Integration/   Desktop<->Server 联通 (新建, ~30 tests, SQL Server)
  LYBT.Tests.Desktop/       本地模式 + 纯逻辑 (精简, ~330 tests, SQL Server)
  LYBT.Tests.Architecture/  架构防护 (已有, 68 tests)
```

### 2.2 新建: LYBT.Tests.Integration

**目标**: 验证 Desktop RemoteDataSource 通过 Refit 调用 Server API 全链路正确。

**技术方案**: WebApplicationFactory<Program> (in-process TestServer)

```
Desktop RemoteDataSource
  -> IPatientApi (Refit HttpClient)
    -> WebApplicationFactory (in-process)
      -> ASP.NET Middleware (Auth, CORS, RateLimit)
        -> Controller -> Service -> Repository -> SQL Server
```

**项目配置**:
- Target: net8.0-windows
- References: LYBT.WebAPI, LYBT.Desktop.Contracts, LYBT.Desktop.Infrastructure, LYBT.Shared.Models
- NuGet: Microsoft.AspNetCore.Mvc.Testing, Respawn, xUnit, FluentAssertions

**核心 Fixture**:
- IntegrationFixture: WebApplicationFactory + Refit HttpClient + Respawn
- 复用 Server 测试的数据库 seed 逻辑

**测试内容 (按业务流组织)**:

| 测试文件 | 验证什么 | 测试数 |
|----------|---------|--------|
| AuthFlowTests | 登录->Token->刷新->登出, Refit 全链路 | ~5 |
| PatientFlowTests | 创建->查询->修改->删除, RemoteDataSource 全链路 | ~5 |
| HerbFlowTests | 药材 CRUD + 状态切换 | ~4 |
| FormulaFlowTests | 验方 CRUD + 药材关联 | ~4 |
| MedicalCaseFlowTests | 创建->诊断->处方->完成, 聚合保存全链路 | ~8 |
| PrescriptionImportFlowTests | 经验方导入 + 历史导入 | ~4 |
| **合计** | | **~30** |

### 2.3 Desktop 测试精简

**保留不动 (~231 tests)**:
- LocalData/ (70) - 真实数据库 CRUD (迁移后改用 SQL Server)
- EndToEnd/ (95) - 真实业务流 (迁移后改用 SQL Server)
- PureLogic/ 0-mock (65) - 纯计算逻辑
- _Infrastructure/ (5) - Fixture 验证

**删除 (~170 tests)**: mock 交互验证, 代码改坏不会红

| 文件 | 删除 | 理由 |
|------|------|------|
| ChildViewModelBaseTests | 5 全删 | trivial |
| ConsultationEditorVMTests | 8 全删 | 4 mocks, 重写为纯逻辑 |
| PrescriptionEditorVMTests | 10 全删 | 4 mocks, 重写为纯逻辑 |
| MedicalCaseCommandsVMTests | 20 全删 | 9 mocks, 测 Received() |
| PendingQueueViewModelTests | 12 全删 | 7 mocks |
| CardReaderViewModelTests | 22 全删 | 重写精简版 |
| AdminHomeVMTests | 删 4 / 保 3 | 删导航 mock |
| ClinicalHomeVMTests | 删 4 / 保 3 | 同上 |
| LoginCoordinatorTests | 删 10 / 保 12 | 删 Received() |
| StartupStepsTests | 删 15 / 保 6 | 删 Received() |
| LogoutServiceTests | 删 8 / 保 12 | 删 Received() |
| StartupPipelineTests | 删 10 / 保 5 | 删 Received() |
| CredentialVaultTests | 删 12 / 保 10 | 删 mock 交互 |
| UserActivityTrackerTests | 删 10 / 保 10 | 删 mock 交互 |
| AuthStateMachineTests | 删 10 / 保 23 | 保状态测试 |
| LocalTokenValidatorTests | 删 3 / 保 5 | 删 Received() |

**重写 (~50 tests)**:

| 文件 | 新测试数 | 方式 |
|------|---------|------|
| WorkspaceStateTests | 15 | 补全 DetermineFromContext 全分支 |
| ChangeTrackerTests | 12 | 覆盖实际 14 字段 |
| ConsultationEditorTests | 6 | 纯逻辑: 直接 new, 测映射+验证 |
| PrescriptionEditorTests | 8 | 纯逻辑: 测集合事件+验证 |
| CardReaderTests | 8 | 仅 mock ICardReaderService (硬件) |

**新增 (~18 tests)**:

| 文件 | 测试数 | 补什么 |
|------|--------|--------|
| MedicalCaseStatusFlowTests | 6 | 完整生命周期 |
| PrescriptionHistoryImportTests | 4 | 历史处方导入 |
| MedicalCaseWorkspaceIntegrationTests | 8 | Commands 走真实 Service |

---

## 3. 阶段 0: 本地模式数据库迁移

**范围**: LocalDbContext 从 SQLite Provider 切换到 SQL Server Provider

**影响文件**:
- `Shell/Extensions/DataSourceRegistrationExtensions.cs` - DI 注册
- `LocalData/Context/LocalDbContext.cs` - Provider 配置
- `_Infrastructure/DesktopFixture.cs` - 测试 Fixture
- `_Infrastructure/LocalDbContextFixture.cs` - 测试 Fixture
- 可能: LocalDataSource 5 个类的简化/合并

**风险**: 中等，需要仔细验证 EF Core 迁移和种子数据
**前提**: 需要确认 SQL Server LocalDB 的安装/配置策略

---

## 4. 预期最终结果

| 项目 | 测试数 | 数据库 | 验证范围 |
|------|--------|--------|---------|
| Server | 1017 | SQL Server | API 正确性 |
| Integration (新) | ~30 | SQL Server | Desktop<->Server 联通 |
| Desktop | ~330 | SQL Server | 本地模式 + 纯逻辑 |
| Architecture | 68+ | 无 | 代码结构守护 |
| **合计** | **~1445** | | |

## 5. 不做的事情 (YAGNI)

- 不追求覆盖率数字
- 不测 trivial getter/setter
- 不测构造函数 null guard (DI 容器保证)
- 不为边界条件写测试 (后续补)
- 不改 Server/Architecture 测试结构
