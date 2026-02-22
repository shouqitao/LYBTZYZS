# Design: 测试体系完整重构 - 集成优先方案

## Context

当前 37 个测试项目采用过度 Mock 的金字塔策略，导致单元测试全绿但运行时各种错误。WebAPI 集成测试 246/255 失败（API 合约漂移），证明测试体系与真实运行环境严重脱节。

系统有两条数据链路:
- **在线模式**: WPF Client -> HTTP API -> Controller -> Service -> Repository -> SQL Server
- **本地模式**: WPF Client -> ViewModel -> DataSource -> SQLite

## Goals

- 测试能真正拦截运行时错误（而非仅验证 Mock 行为）
- 37 个项目合并为 4 个核心项目
- 覆盖全部核心业务流程
- 维护成本降低

## Non-Goals

- 不测试 XAML/UI 渲染
- 不使用 Playwright/UI 自动化
- 不追求 100% 代码覆盖率

## Decisions

### 1. 集成优先 (Integration-First / 钻石模型)

**选择**: 大幅减少 Mock 单元测试，用真实组件端到端测试替代。

**替代方案**: 经典金字塔（大量Mock单元测试）-- 已证明无法拦截运行时错误。

### 2. 4 个测试项目

| 项目 | 职责 | DB | Mock |
|------|------|-----|------|
| LYBT.Tests.Unit | 纯逻辑（算法/验证器/实体/Mapper） | 无 | 无 |
| LYBT.Tests.Server.Integration | HTTP -> Controller -> Service -> Repository -> DB | SQL Server (开发环境) | 无 |
| LYBT.Tests.Desktop.Integration | ViewModel -> DataSource -> DB | SQLite InMemory | 仅 Prism |
| LYBT.Tests.Architecture | 依赖方向/命名规范/层级边界 | 无 | 无 |

### 3. NSubstitute 统一

**选择**: 去除 Moq，全部使用 NSubstitute。
**理由**: 语法更简洁，与现有 Desktop 测试一致。

### 4. SQL Server (开发环境) 用于 Server 集成测试

**选择**: 使用开发环境 SQL Server 实例，创建专用测试数据库 (LYBT_Test)。
**理由**: 与运行时完全一致的数据库引擎，消除 InMemory/SQLite 与 SQL Server 的行为差异。
**隔离**: 每个测试类用事务包裹，测试后回滚。

### 5. ViewModel 层止步

**选择**: Desktop 测试到 ViewModel 层为止，不涉及 XAML/UI 渲染。
**理由**: WPF UI 测试需要 STA 线程/Application 初始化，极不稳定。ViewModel 已覆盖 90% 业务逻辑。

## Fixture Architecture

### WebApiFixture (Server)

```csharp
public class WebApiFixture : IAsyncLifetime
{
    private WebApplicationFactory<Program> _factory;
    public HttpClient Client { get; private set; }
    public IServiceProvider Services => _factory.Services;

    public async Task InitializeAsync()
    {
        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Test");
                builder.ConfigureServices(services =>
                {
                    // 使用开发环境 SQL Server + LYBT_Test 数据库
                    // 移除长运行后台服务
                    // 配置测试 JWT
                });
            });
        Client = _factory.CreateClient();
        await SeedAdminUser();
    }

    public HttpClient CreateClientAs(string role, Guid userId);
    public async Task<T> SeedAsync<T>(T entity);
}
```

### DesktopFixture (Desktop)

```csharp
public class DesktopFixture : IAsyncLifetime
{
    private SqliteConnection _connection;
    public LocalDbContext DbContext { get; private set; }
    public IServiceProvider Services { get; private set; }

    public async Task InitializeAsync()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        await _connection.OpenAsync();

        var services = new ServiceCollection();
        services.AddDbContext<LocalDbContext>(o => o.UseSqlite(_connection));
        // 注册全部真实 DataSource
        // 仅 Mock Prism: IRegionManager, IDialogService, IEventAggregator

        Services = services.BuildServiceProvider();
        DbContext = Services.GetRequiredService<LocalDbContext>();
        await DbContext.Database.EnsureCreatedAsync();
    }

    public T GetViewModel<T>() where T : class;
    public T GetDataSource<T>() where T : class;
}
```

## Test Scenarios

### Server Integration (~30-40 tests)

- Auth: 登录/Token/权限验证
- Users: CRUD + 角色管理
- Patients: CRUD + 搜索
- Herbs: CRUD + 价格
- Formulas: CRUD + 药材项
- MedicalCases: 聚合根完整流程（创建/诊断/处方/编号/状态）
- Sync: 数据同步

### Desktop Integration (~25-35 tests)

- Auth: 本地登录流程
- 各模块 ViewModel -> DataSource -> SQLite CRUD
- MedicalCases: 工作区 ViewModel 端到端
- LocalMode: DataSource 聚合根持久化

### Unit (~50-80 tests)

- 迁移现有纯逻辑测试（验证器、工具类、实体计算、Mapper）
- 不含任何 Mock

### Architecture (~15-20 tests)

- 依赖方向检查
- 命名规范验证
- 层级边界约束

## Migration Strategy

1. 创建 4 个新项目（与旧项目并存）
2. 逐步实现新测试
3. 验证新测试全部通过
4. 删除旧项目
5. 更新 sln

## Risks

| Risk | Mitigation |
|------|------------|
| SQL Server 连接在 CI 不可用 | 使用 TestContainers 或 SQL Server Docker |
| 测试速度变慢 | 事务回滚比 recreate DB 快；并行执行 |
| 迁移期间两套测试并存 | 分阶段推进，每阶段验证 |

---
**Created**: 2026-02-08
