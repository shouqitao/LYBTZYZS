# Desktop 测试架构重设计方案 — 2026-08-19

> **状态**：设计态（系统应该是什么）— 待用户评审后进入实施  
> **版本**：v1.0 | 2026-08-19 | 作者：技术统筹（Hermes）  
> **前置**：`desktop-test-audit-2026-08-19.md` 方法级评估（122 方法 75% 覆盖、12 类 0 覆盖、162 环境 fail） + `desktop-l4-audit` + `desktop-architecture-design-eval`  
> **决策**：推翻存量 3 套基类/弱断言/环境耦合历史包袱，重建统一基类、统一 Mock、统一断言规范；存量测试保留至新架构验证通过后分批迁移，不一刀切删除

---

## 1. 背景与目标

### 1.1 为什么重推

- **历史包袱**：`UserJourneyTestBase`（LocalDB） / `IntegrationTestBase`（Respawn，已随 T2-1 删除） / `WebApiE2ETestBase`（`localhost:5000`）3 套基类并存，职责重叠、生命周期不一，新人无法判断该继承谁。
- **质量债务**：`ToastServiceTests` 13 用例仅 `Record.Exception.Should().BeNull()`（虚假安全，已删），`NotificationTypeMapping` 2 恒真 `BeTrue(true)`，`PatientMasterDetail` 2 处 `BeNull` 弱断言（B3 已补），命名 10% 非 `Should_When`。
- **环境耦合**：`777` 声明中 `162 fail` 为环境性（154 `localhost:5000` 未起 + 8 `STA`），`dotnet test` 全量无法在 CI 一键绿，非设计缺陷却长期误判为回归。
- **覆盖幻觉**：`MedicalCaseWorkspaceViewModel 549 行` 仅 33%（4/12 方法），`FormulaImportDialog`/`HistoryCopyDialog` 0 覆盖，但 `arch 87/87` 与 `build 0/0` 仍绿，测试与需求脱节。

### 1.2 目标

- **可重复**：`dotnet test` 在无 `localhost`、无 LocalDB 额外进程的纯净 CI 上一键绿；I/O 隔离由 `TestContainers`/`WebApplicationFactory` 承载，不依赖开发者本机服务。
- **可读**：1 套基类 `DesktopTestBase`，1 套 Mock 契约（UI 层 `NSubstitute` + Server 层 `Fake`），1 套断言命名 `Method_Scenario_Expected` + `FluentAssertions` 充实断言。
- **可追溯**：147 US 中 122 已≥1 单测（83%）→ 新架构要求 100% P0 US 直连单测，缺口清单自动化（`US ↔ Test` 双向索引）。

---

## 2. 存量问题全景（方法级）

### 2.1 项目结构：三层名不副实

| 层 | 存量 | 问题 |
|----|------|------|
| `Unit/` 129 cs | 纯 VM 单元（`NSubstitute` Mock `IApiClient`）与 `LocalDB` 真仓储混放同一目录 | 单元/集成边界模糊，`[Fact]` 是否触库仅靠注释区分 |
| `Integration/` | `WebApiE2ETestBase` 直连 `http://localhost:5000` 真服务 + `LocalDbContext` 真库（`_Infrastructure` 5 夹具） | 需手工起 WebAPI，CI 必 fail 154 |
| `PureLogic/` | `FrameworkVerificationTests` 2 用例 `#pragma CS1998` 空跑 | 占位，无业务价值 |
| `Architecture/` 87 | 扎实（`ServerArchTests 87`），但与 Desktop 测试分治，无统一入口 `dotnet test --filter` 跨项目聚合报告 | — |

### 2.2 基类体系：3 套并存

| 基类 | 职责 | 残留 | 后果 |
|------|------|------|------|
| `UserJourneyTestBase` | LocalDB + `IClassFixture<UserJourneyFixture>`（T2-2 后已去 `LocalDB`，但 12 类仍继承） | 12 类纯 VM 测试仍继承 `IClassFixture` 空转 | 每次 `dotnet test` 多建 12 空 DB |
| `IntegrationTestBase`（Server）+ `WebApiE2ETestBase`（Desktop） | `Respawn`（已删） vs `localhost:5000` 直连 | 命名相似、生命周期相反（前者每测重置 DB，后者共享长连接） | 新人误用 `IntegrationTestBase` 写 Desktop 集成 |
| `PureLogic` 无基类 | 各自 `new` | 重复 `CreateSut` 样板 90 行（`PatientMasterDetail` 3 文件复制） | T3-1 已收敛 `CreateMasterDetailServicesMock`，但基类未统一 |

### 2.3 Mock/Fake 不一致

| 层 | 存量策略 | 问题 |
|----|----------|------|
| **Server** | `AntiMockRuleTests AM01/02` 强制零 Mock，手写 `FakeUserManager`/`FakeRepository` | 正确，但 `HerbBatchItemError` 等手写 Fake 需 30 行样板 |
| **Desktop Unit** | `NSubstitute` Mock `IApiClient` 边界（`Arg.Any` + `Returns`） | 正确，但 `Castle` 无法代理 `private` 嵌套 DTO 的 `IEntityApiSegment`（Batch6 坑2：`private` DTO→`public` 修复） |
| **Desktop Integration** | 真 `LocalDB` + 真 `HttpClientFactory` | 正确，但 `DIM` 的 `GetPagedAsync` 需显式配置 `segment.GetPagedAsync(2,50,"kw",null)`（坑3） |

### 2.4 弱断言与命名

| 类型 | 存量数 | 示例 | 已处置 | 残留 |
|------|--------|------|--------|------|
| 恒真 | 2 | `Should().BeTrue(true)`（`NotificationTypeMapping`） | ✅ T2 已删 | 0 |
| `Record.Exception.Should().BeNull()` | 13 | `ToastServiceTests` 13 用例仅“不抛” | ✅ 已删 `BreadcrumbBarTests`/`ToastServiceTests` | 0 |
| `BeNull` 弱断言 | 2 | `PatientMasterDetail` 2 处 `result.Should().BeNull()` | ✅ B3 已改为 `Message.Should().NotBeNullOrEmpty()` + `Items.Should().HaveCount(2)` | 0 |
| 非 `Should_When` | ~10% | `Test1`/`CanRestore_ReturnsFalse` | T3-2 已正名 50 个中的 90% | ~3% 残留（如 `IsNavigationTarget_ReturnsTrue` 空跑） |

### 2.5 环境耦合：162 fail 根因

| 套件 | 总数 | 通过 | 失败 | 根因分类 | 占比 |
|------|------|------|------|----------|------|
| `LYBT.Tests.Desktop` | 777 | 615 | **162** | 154 `HttpRequestException: localhost:5000` + 8 `STAThread`/`WPF` | 100% 环境 |
| `LYBT.Tests.Server` | 58 | 58 | 0 | — | — |
| `LYBT.Tests.Architecture` | 87 | 87 | 0 | — | — |

> **结论**：非回归，是 `C-01` 已知环境项（`13c-current-status #13`）：需运行中 WebAPI。存量 `WebApiE2ETestBase` 设计即依赖外部进程，CI 无法自闭环。

### 2.6 覆盖缺口（方法级 122 抽样）

| 层 | 类 | 未覆盖示例 | 优先级 |
|----|----|------------|--------|
| VM | `FormulaImportDialogViewModel` 0%（180 行） | `Search/Select/Preview/Import` 4 方法 | **P1**（B1 已补） |
| VM | `HistoryCopyDialogViewModel` 0%（160 行） | `LoadHistory/FilterByDate/Preview/Copy` | **P1**（B1 已补） |
| VM | `MedicalCaseWorkspaceViewModel 549 行` 33%→70% | `SaveComplete/SaveFailed/LeaveConfirm/EditMode` 4 方法 | **P1**（B1 已补） |
| Repo | `MedicalCaseRepository` 71% | `RecordPrint/Suspend/UpdateStatus` 的 `null` vs `throw` 双路径 | **P1**（B1 已补 5） |
| Repo | `PatientRepository.GetByIdNumberAsync` | 身份证读卡 `PAT-014` | P2 |
| Http | `HttpApiClientBase.BuildPagedUrl` | 纯工具 | P3 |

### 2.7 需求映射：25 零映射 US 集中

`US-REPORT-004` 5 趋势端点、`US-SHELL-011` 5 步向导、`US-SHELL-014/016` 审计/导入导出、`US-SHELL-021..023` 上线/培训 — 与 `desktop-ui-requirements §七` 缺口一致，非测试遗漏而是功能未实现（已于 B4 标 `🧲 v2.0`）。

---

## 3. 新架构设计

### 3.1 原则

1. **单入口**：`dotnet test` 在无外部服务时全绿；`--filter Category=Integration` 显式拉起 `TestContainers` 才触库/触网。
2. **单基类**：`DesktopTestBase` 唯一 VM 基类，`ServerTestBase` 唯一 Server 基类，`ArchTestBase` 不变；`UserJourneyTestBase`/`WebApiE2ETestBase` 标记 `[Obsolete]` 6 个月后删除。
3. **单 Mock 契约**：UI 层 `NSubstitute` 只 Mock `IApiClient` 边界；Server 层 `Fake` 手写；`Repository` 真 `HttpClientFactory` 仅在 `Integration` 层。
4. **单断言规范**：`FluentAssertions` + `Should_When_Expected` + 至少 1 个业务断言（`Items.Count/Message/State`），禁 `BeNull` 单断言与 `BeTrue(true)`。

### 3.2 分层重设计

```
tests/LYBT.Tests.Desktop/
├── Unit/                     # 纯内存，0 I/O，NSubstitute 仅 IApiClient
│   ├── ViewModels/           # VM（MasterDetail/Dialog/Workspace）
│   ├── Services/             # Service（CrudServiceBase.ExecuteAsync）
│   ├── Repositories/         # Repository（EntityApiClientRepositoryBase）
│   └── Mappers/              # Mapperly 生成校验
├── Integration/              # TestContainers: LocalDB + WebApplicationFactory
│   ├── Repositories/         # 真仓储 + 真 Http（WebApplicationFactory.CreateClient）
│   └── Workflows/            # 跨模块流程（PatientVisit/HerbFormula/DataIntegrity）
├── Architecture/             # 已有 87/87，新增 Test 命名/断言规范 2 条
└── _Infrastructure/          # 仅 1 套 TestContainersFixture + DesktopTestBase
```

| 层 | 基类 | I/O | 依赖 | 并行 |
|----|------|-----|------|------|
| **Unit** | `DesktopTestBase`（`IClassFixture<TestContainersFixture>` 按需，不触库） | 0 | `NSubstitute` | 完全并行 |
| **Integration** | `DesktopIntegrationTestBase : DesktopTestBase`（`IAsyncLifetime` 拉起 `MsSqlContainer` + `WebApplicationFactory<Program>`） | LocalDB + In-memory Http | 真 `Repository` + 真 `IApiClient` | `Collection("Integration")` 串行 |
| **E2E**（保留） | `WebApiE2ETestBase` 标记 `[Trait("Category","E2E")]` 仅 nightly | 真服务 `localhost:5000` | 真 | 手工触发 |

### 3.3 统一基类 `DesktopTestBase`

```csharp
public abstract class DesktopTestBase : IAsyncLifetime
{
    protected ILoggerFactory LoggerFactory { get; } = Substitute.For<ILoggerFactory>();
    protected IUiThreadDispatcher UiDispatcher { get; } // 同步 InvokeAsync（测试专用）
    protected IViewModelServices Services { get; }
    protected DesktopTestBase()
    {
        // UiThreadDispatcher 同步版（B1 已用）
        var dispatcher = Substitute.For<IUiThreadDispatcher>();
        dispatcher.InvokeAsync(Arg.Any<Action>()).Returns(ci => { ci.Arg<Action>()(); return Task.CompletedTask; });
        dispatcher.InvokeAsync(Arg.Any<Func<Task>>()).Returns(ci => ci.Arg<Func<Task>>()());
        Services = CreateViewModelServices(dispatcher);
    }
    // 统一 Mock 工厂：CreateMasterDetailServicesMock<TList,TDetail>()
    protected IMasterDetailServices<TList,TDetail> CreateMasterDetailServicesMock<TList,TDetail>() where TList : class where TDetail : class;
    // 统一 Builder：CreatePatientListDto()/CreateHerbDto() 等
}
```

- **职责**：仅提供 `LoggerFactory`/`EventAggregator`/`RegionManager` 空 Mock + `UiDispatcher` 同步版 + `CreateMasterDetailServicesMock` 收敛（T3-1 已验证 90 行去重）。
- **生命周期**：`IAsyncLifetime.InitializeAsync` 按需启动 `TestContainers`，`DisposeAsync` 回收；Unit 层不启动容器（`0` I/O）。

### 3.4 Mock 策略

| 层 | 策略 | 示例 | 禁止 |
|----|------|------|------|
| **Server Unit** | 手写 `Fake`（`FakeUserManager` 30 行） | `HerbReferenceCheckTests.FakeHerbRepository` | `NSubstitute`（`AM01/02` 强制） |
| **Desktop Unit VM/Service** | `NSubstitute` 仅 `IApiClient` 边界（`Arg.Any` + `Returns`） | `HerbRepositoryTests:143` `GetPagedAsync(Arg.Any<int>()…)` | `Castle` 代理 `private` DTO（坑2：DTO 须 `public`） |
| **Desktop Integration** | 真 `IHttpClientFactory` + `WebApplicationFactory` | `MedicalCaseRepositoryTests` 真 `LocalDB`（`TestContainers`） | `DIM` 默认方法需显式 `Returns`（坑3） |

### 3.5 数据管理

- **Builder**：`TestDataBuilders` 6 Builder 保留，统一 `Fluent` 命名 `PatientBuilder.Default().WithName("张三").Build()`。
- **隔离**：`Respawn` 已删，`TestContainers.MsSqlContainer` 每 `Collection` 一库，`Respawn` 替为 `TRUNCATE` + `Respawn` 兼容的 `Respring`（`TestContainers` 推荐）。
- **流量**：`MedicalCase` 聚合构造 `Builder.MedicalCase().WithConsultation().WithPrescription(3).Build()` 替代手写 20 行 `Dto`。

### 3.6 断言规范

| 规则 | 示例（正） | 反例（禁） |
|------|------------|------------|
| 命名 | `SearchAsync_WithKeyword_ReturnsFilteredList` | `Test1` |
| 断言数 | ≥1 业务断言（`Items.Count/Message/State`） | `result.Should().NotBeNull()` 单断言 |
| 风格 | `result.Success.Should().BeTrue(); result.Data!.TotalIncome.Should().Be(1234.56m);` | `Should().BeTrue(true)` 恒真 |
| 异步 | `await act.Should().NotThrowAsync()` | `Task.Delay(300).Wait()` 阻塞（`xUnit1031`） |

### 3.7 需求追溯

- `147 US` 基线（`13-traceability-matrix v1.11`）— 新架构要求 `US ↔ Test` 双向索引自动化：`[Trait("US","US-PAT-001")]` 标记每测，`dotnet test --filter US=US-PAT-001` 可直接拉取。
- 缺口清单（25 零映射）由 `desktop-ui-requirements §七` 驱动，非测试遗漏；`REPORT-004`/`SHELL-011` 已标 `🧲 v2.0`，新架构不为其补测。

---

## 4. 迁移计划（3 阶段，存量保留）

| 阶段 | 内容 | 产出 | 风险 |
|------|------|------|------|
| **P0 冻结** | 新测试一律继承 `DesktopTestBase`；`UserJourneyTestBase`/`WebApiE2ETestBase` 标 `[Obsolete("Use DesktopTestBase")]`，CI 加 `warn` | 新档 0 债务 | 低 |
| **P1 迁移** | 存量 `UserJourneyTestBase` 12 类 VM 测试逐批迁 `DesktopTestBase`（每批 ≤5 类，`dotnet test --filter` 双跑对比） | `12→0` | 低（T3-1 已验证 `CreateMasterDetailServicesMock` 等价） |
| **P2 集成收敛** | `WebApiE2ETestBase` 直连 `localhost:5000` 的 8 集成迁 `WebApplicationFactory` + `TestContainers`（每 `Collection` 一库） | `154 fail → 0` | 中（需 `MsSqlContainer` 镜像，`1ff688d12` 125/125 已验证可行） |
| **P3 清理** | 存量 3 基类删除，`PureLogic` 2 空跑删除，`_Infrastructure/LocalDbContext` 休眠上下文删除 | `12→1` 基类 | 低 |

> **原则**：存量 777 声明中 615 通过的保持不动，新档按新规范；P1/P2 每批 `build 0/0` + `arch 87/87` + `新测 绿` 再推，禁一刀切。

---

## 5. 风险与缓解

| 风险 | 缓解 |
|------|------|
| `TestContainers` 拉取 `mssql` 镜像慢 | `Docker` 层缓存 + `CI` 预热；`Unit` 层 0 依赖，`Integration` 仅 `Collection("Integration")` 串行拉取一次 |
| `Castle` 无法代理 `private` DTO | 架构测试新增 `DTO 须 public` 1 条（Batch6 坑2 已固化） |
| `DIM` 默认方法需显式 `Returns` | 基类文档 `CreateMasterDetailServicesMock` 注释坑3，已于 `PatientRepositoryTests:68` 示例化 |

---

## 6. 成功标准

- `dotnet test`（无 `--filter`）在纯净 `CI`（无 `localhost:5000`）一键绿（`Unit 100% + Integration 100%`，`E2E` 仅 `Category=E2E` nightly）。
- `12→1` 基类，`3→1` Mock 契约，`BeNull` 单断言 0。
- `147 US` 中 122→147 映射（`REPORT-004`/`SHELL-011` 除外已标 v2.0）。

---

## 附录

- **基线**：`fb295c017`（`build 0/0 arch 87/87`）+ `b8ae03645`（`ReportServiceTests 3/3`）+ `794afa623`（B2 `CommandResult 7/7`）+ `1ff688d12`（`CatalogPermission/ExportDetail/CategoryFilter 9/9`）+ `4cdf32c7c`（`FormulaImport/HistoryCopy/Workspace 23/23`）
- **命令**：`dotnet test tests/LYBT.Tests.Desktop --filter "Category!=E2E" --no-build`（Unit+Integration）；`dotnet test --filter "Category=E2E"`（nightly）
- **文件清单**：`_Infrastructure/DesktopTestBase.cs`（新建）/ `WebApiE2ETestBase.cs`（标 Obsolete）/ `UserJourneyTestBase.cs`（标 Obsolete）

