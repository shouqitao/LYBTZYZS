# LYBT.Tests.Server - Server Tests

**Purpose**: Server 端测试——EF InMemory 单元测试 + WebApplicationFactory 系统测试 + 真 SQL Server + Respawn 领域路径集成测试，零 mock。

## Structure

```
LYBT.Tests.Server/
├── _Infrastructure/           # SQL 集成基建：TestDbFactory / RespawnCheckpoint / IntegrationTestBase / SqlServerIntegrationCollection
├── Integration/
│   ├── SqlCore/               # 真 SQL Server 领域路径集成（MedicalCase / Registration / Patient）
│   ├── System/                # WebApplicationFactory 系统测试
│   ├── Permissions/           # 角色授权、跨角色
│   ├── P0Endpoints/           # 关键端点
│   ├── Deployment/            # 部署配置、Swagger 暴露面
│   └── Data/                  # DbContext 映射烟测、公式 SQL
└── Unit/                      # 纯逻辑 + EF InMemory 单元测试 (零 mock)
```

## WHERE TO LOOK

| Task | Location | Notes |
|------|----------|-------|
| SQL 集成基类 | `_Infrastructure/IntegrationTestBase.cs` | EnsureCreated + Respawn |
| 连接串 | `_Infrastructure/TestDbFactory.cs` | `TEST_CONNECTION_STRING` → LocalDB `LYBT_Test` |
| SQL 领域路径测试 | `Integration/SqlCore/` | 真 SQL + Respawn |
| WebApplicationFactory 系统测试 | `Integration/System/` | WebApiTestFactory |
| 单元测试 | `Unit/` | EF InMemory + 手写 fake |

## CONVENTIONS

- **SQL 集成** — 新增 `Integration/SqlCore` 测试类必须加 `[Collection(SqlServerIntegrationCollection.Name)]`（共享库 + Respawn 禁并行）
- **测试数据自包含** — 工厂方法内联构造，不依赖种子库
- **Respawn** — 每测后清库（忽略 `__EFMigrationsHistory`）
- **xUnit** — 测试框架
- **NSubstitute banned** — AntiMockRuleTests enforce no mocking in server tests

## ANTI-PATTERNS

- **NSubstitute/Moq** — Architecture test `AntiMockRuleTests` forbids mocking
- **并行跑 SqlCore 测试** — 共享 `LYBT_Test` + Respawn 会互清表，必须挂串行集合
- **InMemory 代替 SQL 路径** — 列映射/过滤唯一索引/全局软删过滤只在真 SQL 可验证
- **共享状态 between tests** — Each test gets fresh database via Respawn
