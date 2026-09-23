<!-- Parent: ../AGENTS.md -->
<!-- Generated: 2026-05-04 | Updated: 2026-09-27 -->

# tests

## Purpose
Test projects for the LYBTZYZS solution. Implements a Testing Trophy architecture across multiple projects: server unit tests (EF InMemory, zero mock) + SQL Server integration (`_Infrastructure`/`Integration/SqlCore`), desktop unit/integration tests, architecture guard tests. **SQL 集成基建重建（2026-09-27）**：`LYBT.Tests.Server/_Infrastructure/` + `Integration/SqlCore/`（真 SQL Server + Respawn 领域路径）。

## Key Files
| File | Description |
|------|-------------|
| .runsettings | Test run configuration (timeouts, parallelism, environment) |
| Directory.Build.props | Shared build properties for all test projects |
| Directory.Build.targets | Shared build targets for all test projects |

## Subdirectories
| Directory | Purpose |
|-----------|---------|
| LYBT.Tests.Server/ | Server 单元测试 — 359 方法/571 用例，EF InMemory + 手写 fake，零 mock（AntiMock 规则强制）；全量 **847/847 pass**（2026-09-23 实测） |
|LYBT.Tests.Desktop/|Desktop 测试 — 1038 用例，纯 VM 单元测试（NSubstitute mock，无 DB）+ LocalWebAPI 控制器集成测试（LocalDB）+ E2E（本地模式自举；RemoteApi 需 localhost:5000，未启动则 Skip）；全量 **1014/0 失败/24 Skip**（2026-09-23 实测，共享 LocalDB 提速后 **~2m30s**，原 25m40s；分层见下）|
| LYBT.Tests.Architecture/ | Architecture guard tests — **107/107 pass**（2026-09-23 实测），enforcing dependency rules, naming conventions, anti-mock policies |
| postman/ | Postman/Newman API test collections |

## 分层测试策略（L0/L1/L2，2026-09-23 确立，强制）

> 背景：Desktop 全量 1038 用例原需 **~25 分钟**——`LocalWebApiTestBase` 每测试方法重建 LocalDB + 建表 + 4 角色种子 + 启动宿主（约 11s/测试）。日常验证跑全量不可接受，**按时机分三层执行，全量只在收尾/发布前跑**。

### 三层定义与实测耗时（2026-09-23，Windows i7-10710U，`--no-build` 单次实测）

| 层 | 范围 | 验证时机 | 目标 | 实测 |
|---|---|---|---|---|
| **L0 快速** | Architecture + Desktop Unit | 每次代码改动后自检 | ≤30s | Architecture 107 用例 **10s** + Desktop Unit 877 用例 **17s**（墙钟约 31s） |
| **L1 集成** | L0 + Server 全量 + Desktop Integration(LocalWebAPI) | **commit 前** | ≤3min | Server 851 用例 **1m46s** + Desktop Integration 34 通过/24 Skip **23s** → 含 L0 约 **2m42s** |
| **L2 全量** | L0+L1 + Desktop E2E + 其余全部 | **仅任务收尾/发布前** | — | Desktop 全量 1038 用例（1014 通过/24 Skip）**1m43s**（原 25m42s）；L2 总耗时（Arch+Server+Desktop）约 **3m40s** |

### 可直接复制的命令

```bash
# ── L0 快速层（每次改动后自检；先构建一次）──
dotnet build LYBTZYZS.sln --no-incremental          # 门禁：0 错误 0 警告
dotnet test tests/LYBT.Tests.Architecture/ --no-build
dotnet test tests/LYBT.Tests.Desktop/ --no-build --filter "FullyQualifiedName~LYBT.Tests.Desktop.Unit"

# ── L1 集成层（commit 前）──
dotnet test tests/LYBT.Tests.Server/ --no-build
dotnet test tests/LYBT.Tests.Desktop/ --no-build --filter "FullyQualifiedName~LYBT.Tests.Desktop.Integration"

# ── L2 全量层（任务收尾/发布前，唯一需要跑全量的场景）──
dotnet test tests/LYBT.Tests.Architecture/ --no-build
dotnet test tests/LYBT.Tests.Server/ --no-build
dotnet test tests/LYBT.Tests.Desktop/ --no-build
```

### 切分依据（namespace ↔ 层）

| 层 | `--filter` | 命名空间 | 说明 |
|---|---|---|---|
| L0 | `~LYBT.Tests.Desktop.Unit` | `LYBT.Tests.Desktop.Unit.**` | 纯 VM 单元测试（NSubstitute mock，无 DB） |
| L1 | `~LYBT.Tests.Desktop.Integration` | `...Integration.LocalApi.**`、`...Integration.RemoteApi.**` | LocalApi 自举 LocalWebAPI+LocalDB；RemoteApi 需 localhost:5000，未启动则 24 项 Skip |
| L2 | `~LYBT.Tests.Desktop.E2E` | `...E2E.**` | 本地模式自举 E2E（真实 IApiClient → Kestrel → LocalDB，不 mock） |

**命名空间校正（2026-09-23）**：`Unit/**` 下 69 个文件原声明扁平根命名空间 `LYBT.Tests.Desktop`，`--filter` 无法切分（当时 Unit 层只切出 89/966 用例）→ 统一改为与目录一致的 `LYBT.Tests.Desktop.Unit`（嵌套命名空间仍可解析父命名空间类型，无 using 变更）。Server 项目根命名空间文件（70 个）保持原样——L1/L2 均按项目全量跑，无需切分。

### 并行 / 串行说明

- **Desktop `xunit.runner.json`**：`parallelizeTestCollections: true`（2026-09-23 由 `false` 改），`parallelizeTestMethods: false`（方法级串行——同类内共享数据库/端口），`parallelizeAssembly: false`。
- **必须串行的 collection**（`DisableParallelization = true`，保持不动）：
  - Desktop `LocalApi` / `E2ELocal`：进程级 `ASPNETCORE_URLS`/`ASPNETCORE_ENVIRONMENT` 环境变量 + `ModeSwitchE2ETests` 固定端口 5300 + 共享 LocalDB。
  - Desktop `RemoteApi`：依赖外部 localhost:5000。
  - Server `EnvIsolated` / `ConfigClosure` / `ForceResetTests`：进程级环境变量 / 配置文件 / 静态状态（`DatabaseInitializationService` 强制重置）。

### 提速措施（2026-09-23，不破坏隔离）

1. **共享 LocalDB + Respawn 清理**（`Integration/LocalApi/LocalWebApiTestBase.cs`）——原每测试方法建库+建表+身份种子；现每测试进程一库（`LYBTZYZS_LocalApiShared_{pid}`，`ProcessExit` 删除），每测试类首次运行时 Respawn 清库并确定性重建身份/业务种子；**每个测试仍启动独立宿主**（独立限流桶/内存状态，登录限流 5 次/分钟不受跨测试影响）。实测：Desktop Integration **6m12s → 23s**、E2E **19m22s → 1m23s**。
2. **命名空间校正**：见上（顺带让 Unit 层从 89 用例的「部分」变为 877 用例的「完整」）。
3. **collection 并行**：`parallelizeTestCollections` false→true（串行 collection 已显式标注原因，见上）。
4. **核查结论**：`Integration/E2E/**`、`Integration/LocalApi/**` 基建中**不存在固定硬等待**（`Task.Delay`/`Thread.Sleep` 零处；仅有 HttpClient 60s 超时），原 11s/测试的开销全部来自每测试重建栈，已由措施 1 消除——无需引入就绪探测轮询。

## For AI Agents

### Working In This Directory
- **日常/提交/收尾分别跑 L0/L1/L2（见上），不要每次跑全量。**
- Run all tests: `dotnet test LYBTZYZS.sln --filter "FullyQualifiedName~LYBT.Tests"`（等同 L2，仅收尾用）
- Run individual projects: `dotnet test tests/LYBT.Tests.Server/`
- Run single test: `dotnet test tests/LYBT.Tests.Server/ --filter "FullyQualifiedName~ClassName.MethodName"`
- Run module-specific: `dotnet test tests/LYBT.Tests.Server/ --filter "FullyQualifiedName~MedicalCase"`

### Testing Requirements
- **Server tests**: 分层——`Integration/SqlCore` 真 SQL Server + Respawn（LocalDB `LYBT_Test` 或 `TEST_CONNECTION_STRING`）；单元测试 EF InMemory 真实实现 + 手写 fake，ZERO mocks（AntiMock 规则 AM01/AM02 强制）。
- **Desktop tests**: 纯 VM 单元测试无 DB（T2-2 后 UserJourneyTestBase 不再挂 LocalDB）；LocalWebAPI 控制器测试用真实 Kestrel + LocalDB；**本地模式 E2E（`E2E/**`、`Integration/LocalApi/**`）自举**——自行启动 LocalWebAPI（Kestrel 随机端口 / `ModeSwitch` 用固定 5300）+ **每测试进程共享一个 LocalDB**（`LYBTZYZS_LocalApiShared_{pid}`，每测试类首次运行 Respawn 清库+重建种子；每测试仍独立宿主），无需外部服务；**仅 `Integration/RemoteApi/**` 需要运行中的远程 WebAPI（localhost:5000）**，未启动时以 `[Fact(Skip=…)]` 明确标注跳过（24 项）。
- **测试宿主密钥前置（2026-09-23）**：敏感字段（患者电话/身份证）经 `AesGcmValueConverter` 透明加解密，密钥解析**仅读环境变量**且非 Development 环境缺失即 fail-fast → 两个测试项目各自在 `TestEnvironmentSetup.cs` 用 `[ModuleInitializer]` 注入 `Security__AesKey`（测试密钥；外部已注入时不覆盖）。缺此密钥时任何触碰加密列的读写都会以 `敏感数据加密/解密失败`（ERR-00013 / HTTP 422）失败。
- **Architecture tests**: Verify dependency direction rules, naming conventions, and anti-mock policies (e.g., `P10_Services_Should_Not_Directly_Inject_AppDbContext`).

### Common Patterns
- **Test fixture**: xUnit class fixtures for database setup/teardown
- **Respawn**: Database reset between server integration tests
- **Builder pattern**: Test data builders for entity construction
- **Architecture guards**: Compile-time and runtime checks for architectural invariants

## Dependencies

### Internal
- [src/Server/](../src/Server/AGENTS.md) — All server projects under test
- [src/Client/Desktop/](../src/Client/Desktop/AGENTS.md) — All desktop projects under test
- [src/Shared/](../src/Shared/AGENTS.md) — Shared libraries under test

### External
- xUnit (test framework)
- Respawn (database reset)
- FluentAssertions
- Moq (limited, architecture tests enforce zero-mock in server tests)
- Newman (Postman CLI runner)

<!-- MANUAL: -->
