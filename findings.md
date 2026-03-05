# 测试体系重构 - Findings

## Date: 2026-03-05

## 全面审计结果

### 三项目总览
- Server: 1017 tests, 零 mock, SQL Server + Respawn -- 不动
- Architecture: 68 tests, 零 mock -- 不动
- Desktop: 494 tests, 143 Substitute.For -- 重构重点

### Desktop 问题分布
- 194 个测试依赖 4+ mocks (占 39%)
- ~80 个测试为假绿 (代码改坏不会红)
- 核心业务流缺口: 历史处方导入/医案状态流转/Desktop-Server 联通

## 本地模式需求变更调研 (2026-03-05 补充)

### 两层架构变更

**变更 1 -- SYNC-D02 (架构层, Sprint 4)**:
- 废除 IDataSource 策略模式，远程/本地共享 Service/Repository
- 仅切换 DbContext Provider (SQL Server vs SQLite/LocalDB)
- 状态: 已确认，待实施
- 文档: `docs/03-architecture/dual-mode.md`

**变更 2 -- 本地模式数据库 (测试设计层)**:
- 本地模式从 SQLite 迁移到 SQL Server LocalDB
- 消除 SQL 方言差异，统一测试基础设施
- 状态: 设计阶段
- 文档: `docs/plans/2026-03-05-desktop-test-simplification-design.md`

### 当前代码实际状态 (全部仍是 SQLite)

| 组件 | 文件 | 当前状态 |
|------|------|---------|
| LocalDbContext | `LocalData/Context/LocalDbContext.cs` | SQLite Provider |
| DI 注册 | `Shell/Extensions/DataSourceRegistrationExtensions.cs:107` | `UseSqlite(...)` |
| 5 个 LocalDataSource | `LocalData/DataSources/Local*DataSource.cs` | 直连 LocalDbContext |
| 5 个 IDataSource 接口 | `Contracts/DataSources/I*DataSource.cs` | 仍然存在 |
| Desktop 测试 Fixture | `DesktopFixture.cs:107` + `LocalDbContextFixture.cs:28` | `UseSqlite(connection)` |
| NuGet 依赖 | LocalData.csproj, Shell.csproj, Tests.Desktop.csproj | `Microsoft.EntityFrameworkCore.Sqlite` |

### SYNC-D02 实施后将被整体删除的组件

1. `IDataSourceBase<TDetail, TInput>` + 5 个子接口 (Contracts/DataSources/)
2. 5 个 `LocalXxxDataSource` 类 (LocalData/DataSources/)
3. 5 个 `RemoteXxxDataSource` 类 (Infrastructure/DataSources/Remote/)
4. `DataSourceRegistrationExtensions` 双路注册逻辑

### 测试无用功风险评估

| 现有测试 | 数量 | SYNC-D02 后命运 | 建议 |
|----------|------|----------------|------|
| LocalData/DataSources/ 测试 | ~70 | 全部废弃 (DataSource 层消失) | 不投入精力改善 |
| EndToEnd/ 测试 | ~95 | 需重写 (不再经过 DataSource) | 保留但不扩展 |
| PureLogic/ 纯逻辑测试 | ~65 | 安全保留 (不依赖数据层) | 继续投入 |
| DesktopFixture 迁移 | Phase 0 | 合理但 Fixture 注入路径会变 | 做，保持最简 |

### 最终决策: 避免无用功

1. **Phase 0 取消**: 不做本地 DB 迁移，等 SYNC-D02 (Sprint 4) 统一处理
2. **LocalData/ 测试全部删除 (~70)**: DataSource 层在 SYNC-D02 后整体废弃，等实施后再补
3. **Phase 2 与本地模式解耦**: 远程链路 (RemoteDataSource->Refit->Server) 与本地 DB 选择无关
4. **Phase 3 重写全部面向纯逻辑**: WorkspaceState/ChangeTracker/ConsultationEditor 等与数据层无关，SYNC-D02 后仍有效
5. **Desktop 测试预估从 ~330 降至 ~260**: 删除 70 个 LocalData 测试后的新基数

## 调研: 5 个可操作测试模式

1. Subcutaneous Testing (ViewModel 做测试边界)
2. SQLite InMemory -> 改为 SQL Server + Respawn (统一引擎)
3. Mock 白名单 (默认真实，显式列出允许 mock 的)
4. Scope-per-Test (EF Core 写隔离)
5. Sociable Tests (测真实协作链)

## WebApplicationFactory 评估

- 在测试进程内启动完整 ASP.NET Core 管道
- 覆盖: JSON 序列化、HTTP 路由、JWT 认证、Controller 逻辑、SQL Server
- 不覆盖: TCP 网络传输、连接超时 (运维层面)
- 结论: 满足 "投入运行时完美运行" 的功能正确性要求
