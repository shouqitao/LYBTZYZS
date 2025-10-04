# 2025-09-24 Users 模块 SQLite In-Memory 评估任务

## 任务背景
- 当前 Users 模块单测使用 EF Core InMemory Provider，无法覆盖 `ExecuteUpdateAsync` 等行为，导致批量操作与事务测试无法真实复现生产逻辑。
- 需快速验证将测试数据库替换为 SQLite In-Memory 是否能改善批量操作、并发行为与缓存一致性。
- 目标是在不影响现有代码结构的前提下，引入可复用的 SQLite In-Memory 测试基础设施，并对关键测试模块回归。

## 工作项
1. **测试基础设施搭建**
   - 在 `tests/UnitTests/Modules/Users.UnitTests` 新增 `SqliteUsersTestFixture`：
     - 使用 `Microsoft.Data.Sqlite` 创建内存连接（`DataSource=:memory:`，`Cache=Shared`）。
     - 为每个测试上下文调用 `connection.Open()` 并保持连接生命周期。
     - 使用 `UseSqlite(connection)` 配置 `AppDbContext`，执行 `Database.EnsureCreated()` 构建架构。
   - 若需共享 AutoMapper/Cache，可复用现有 Fixture 或在此 Fixture 中统一提供。

2. **测试模块切换**
   - 将 `UserRepositoryTests`/`UserRepositoryIntegrationTests`/`UserBusinessServiceTests` 重构为使用新的 SQLite Fixture。
   - 清理原有 InMemory Provider 初始化代码，确保所有 DbContext 都来自 Fixture。
   - 根据 SQLite 行为（如约束、事务）调整必要断言。

3. **依赖与配置**
   - 在测试项目中添加 `Microsoft.Data.Sqlite` 与 `Microsoft.EntityFrameworkCore.Sqlite` 引用（若尚未引入，需更新 `*.csproj`）。
   - 确保 `Directory.Build.props` 或集中包管理同步更新。

4. **回归验证**
   - `dotnet test tests/UnitTests/Modules/Users.UnitTests/LYBT.Module.Users.Tests.csproj -c Release --no-build`
   - 观察批量操作、缓存相关测试的行为差异，与原先 InMemory Provider 结果对比。
   - 在总结中记录改进点及可能新增的失败（若有）。

## 验收标准
- 新增 Fixture 可被仓储与服务测试复用，无资源泄漏（连接在测试结束释放）。
- 所有迁移到 SQLite 的测试执行成功（允许存在其它已知业务失败，但不得因数据库配置引入新异常）。
- 在 `docs/tasks/completed/2025-09-24-users-SQLite-InMemory评估-summary.md` 记录评估结果、性能/稳定性变化、后续建议。

## 风险提示
- SQLite In-Memory 需要保持连接常驻，否则库会丢失；Fixture 设计需谨慎管理生命周期。
- 若测试需并发执行，需评估 SQLite In-Memory 的线程限制，必要时串行化相关测试。
- 引入新依赖需确认与 CI 环境兼容，更新 README/测试说明。

---
文件：docs/tasks/pending/2025-09-24-users-SQLite-InMemory评估任务.md