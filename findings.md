# Testing Trophy Redesign - Findings

## Date: 2026-03-03
## Scope: 全量测试架构重新设计

---

## 行业调研

### Testing Trophy vs Testing Pyramid (2025/2026 共识)
- Testing Trophy 以集成测试为核心，mock 最小化
- EF Core InMemoryProvider 已被广泛认为不可信 (LINQ 翻译差异、无约束)
- Testcontainers 是主流方案，但需要 Docker
- Respawn 是数据库重置标准工具 (按外键拓扑序 DELETE)

### 关键参考
- Milan Jovanovic: 不再推荐 mock-heavy 单元测试
- Jimmy Bogard: Vertical Slice Testing
- Martin Fowler: 不要争论测试比例，关注行为边界

### 当前环境约束
- Docker: 未安装，Testcontainers 不可用
- SQL Server: 2012 (本地 localhost)，需验证 Respawn 兼容性
- 预留 ITestDatabaseProvider 接口，未来可切换 Testcontainers

---

## 当前测试基础设施分析

### NuGet 包
- 测试框架: xunit + FluentAssertions
- Mock: NSubstitute (待从 Server 项目移除)
- DB: EF Core InMemory (待替代) + SQLite (Desktop 保留) + SQL Server (Integration)
- 架构: NetArchTest.Rules
- 缺失: Respawn, Testcontainers

### Mock 偏差根因
- 服务层 ~200+ mock 配置，随代码演进不断漂移
- NSubstitute auto-wrap 掩盖了 async 返回类型不匹配
- Mock 默认行为 (返回 null/default) 与生产代码抛异常行为不一致

### 数据库引擎差异
| 层级 | 引擎 | 问题 |
|------|------|------|
| TestBase.CreateInMemoryContext() | EF InMemory | 无关系约束、无 RowVersion |
| AuthServiceTests 等 | SQLite InMemory | 更接近但不等于 SQL Server |
| WebApiFixture | SQL Server LYBT_Test | 真实但无测试间隔离 |

### 当前 Fixture 模式
- WebApiFixture: WAF + 本地 SQL Server + 硬编码 JWT
- RateLimitingFixture: 独立 DB + 独立 WAF
- DesktopE2ETestFixture: SQLite + 真实 Repository + 14 mocks
- TestBase: DI 容器 + CreateMock<T>() + EF InMemory

---

## 设计方案选择

| 方案 | 核心策略 | 选择 |
|------|----------|------|
| A: 演进式修复 | 修补现有 mock 偏差 | 否 |
| B: Testing Trophy | 消灭 mock，集成测试为核心 | **是** |
| C: 混合现代化 | 基础设施升级 + 修复 mock | 否 |

理由: mock 是偏差的结构性来源，战术修补无法根治。
