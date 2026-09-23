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
| LYBT.Tests.Desktop/ | Desktop 测试 — 1038 用例，纯 VM 单元测试（NSubstitute mock，无 DB）+ LocalWebAPI 控制器集成测试（LocalDB）+ E2E（本地模式自举；RemoteApi 需 localhost:5000，未启动则 Skip）；全量 **1014/0 失败/24 Skip**（2026-09-23 实测，25m40s） |
| LYBT.Tests.Architecture/ | Architecture guard tests — **107/107 pass**（2026-09-23 实测），enforcing dependency rules, naming conventions, anti-mock policies |
| postman/ | Postman/Newman API test collections |

## For AI Agents

### Working In This Directory
- Run all tests: `dotnet test LYBTZYZS.sln --filter "FullyQualifiedName~LYBT.Tests"`
- Run individual projects: `dotnet test tests/LYBT.Tests.Server/`
- Run single test: `dotnet test tests/LYBT.Tests.Server/ --filter "FullyQualifiedName~ClassName.MethodName"`
- Run module-specific: `dotnet test tests/LYBT.Tests.Server/ --filter "FullyQualifiedName~MedicalCase"`

### Testing Requirements
- **Server tests**: 分层——`Integration/SqlCore` 真 SQL Server + Respawn（LocalDB `LYBT_Test` 或 `TEST_CONNECTION_STRING`）；单元测试 EF InMemory 真实实现 + 手写 fake，ZERO mocks（AntiMock 规则 AM01/AM02 强制）。
- **Desktop tests**: 纯 VM 单元测试无 DB（T2-2 后 UserJourneyTestBase 不再挂 LocalDB）；LocalWebAPI 控制器测试用真实 Kestrel + LocalDB；**本地模式 E2E（`E2E/**`、`Integration/LocalApi/**`）自举**——自行启动 LocalWebAPI（Kestrel 随机端口 / `ModeSwitch` 用固定 5300）+ 每类独立 LocalDB，无需外部服务；**仅 `Integration/RemoteApi/**` 需要运行中的远程 WebAPI（localhost:5000）**，未启动时以 `[Fact(Skip=…)]` 明确标注跳过（24 项）。
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
