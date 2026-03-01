# LYBT.Tests.Configuration (TestConfiguration)

共享测试基础设施库，为服务端集成测试、单元测试和 WPF 桌面测试提供公共基类、工厂、断言辅助和数据构建器。本项目不含测试方法，仅作为依赖被其他测试项目引用。

## 项目基本信息

- **目标框架**: net8.0 + net8.0-windows (双目标)
- **类型**: 类库 (IsTestProject=true，但无测试方法)
- **WPF 代码**: 通过 `#if NET8_0_WINDOWS` 条件编译隔离

## 目录结构

```
tests/TestConfiguration/
├── TestBase.cs                           # 单元测试基类 (TestBase, DatabaseTestBase)
├── IntegrationTestBase.cs                # Web API 集成测试基类 + TestModelCustomizer
├── ClientRepositoryTestBase.cs           # Desktop Repository 测试基类 (泛型)
├── SqlServerTestDbContextFactory.cs      # SQL Server 测试数据库工厂
├── Database/SqliteTestDatabaseFactory.cs # SQLite InMemory 工厂 + SqliteTestContext<T>
├── AssertionHelpers/TestAssertions.cs    # HTTP/API/集合 断言扩展方法
├── TestDataBuilders/BaseTestDataBuilder.cs  # Builder 模式基类 + 常用 Builder
└── Wpf/WpfTestCollection.cs             # WPF 测试集合 (DisableParallelization) + Fixture
```

## 核心类

### TestBase (抽象)
通用单元测试基类: DI 容器 + Mock 创建 + InMemory DbContext

| 方法 | 说明 |
|------|------|
| ConfigureServices(IServiceCollection) | virtual 钩子，子类添加服务 |
| CreateMock\<T\>() | NSubstitute 创建并注册到 DI |
| CreateInMemoryContext() | EF Core InMemory DbContext (独立 Guid 数据库名) |

### IntegrationTestBase (抽象, IDisposable)
ASP.NET Core 集成测试基类: WebApplicationFactory + SQL Server + JWT

| 方法 | 说明 |
|------|------|
| GenerateTestToken() | HmacSha256 JWT (与 appsettings.Test.json 对应) |
| SeedTestData(AppDbContext) | virtual 种子数据钩子 |
| Cleanup() | EnsureDeleted 删除测试数据库 |

### ClientRepositoryTestBase\<TRepository, TApi\> (抽象)
Desktop Repository 测试基类: 泛型化 API Mock + Repository 创建

### SqliteTestDatabaseFactory (静态)
SQLite InMemory 连接和 DbContext 工厂。`SqliteTestContext<T>` 封装连接生命周期。

### TestAssertions (静态)
HTTP 状态码断言、API 响应断言、集合断言等 30+ 扩展方法。

### BaseTestDataBuilder\<T\> (抽象)
Builder 模式基类，支持隐式转换。包含 PagedQueryBuilder、UserDetailDtoBuilder、TestDateTime 等。

### WpfTestCollection + WpfTestFixture
`DisableParallelization=true` 防止 Application.Current 单例冲突。WpfTestFixture 初始化最小 WPF 资源字典。

## 已知问题

- `ConfigureInMemoryDatabase` 方法名误导: 实际配置真实 SQL Server
- JWT 密钥硬编码在 GenerateTestToken 中，与 appsettings.Test.json 分离管理
- BaseTestDataBuilder 内部使用 mutation (测试场景可接受)
- TestModelCustomizer 定义但可能未被注册使用

---
最后更新: 2026-03-01
