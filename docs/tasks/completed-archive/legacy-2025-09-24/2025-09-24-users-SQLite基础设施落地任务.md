# 2025-09-24 Users 模块 SQLite 基础设施落地任务

## 背景
- `docs/tasks/completed/2025-09-24-users-SQLite深层问题剖析-summary.md` 描述的改动尚未在仓库落实，当前 SQLite 套件 7 个用例全部失败。
- 主要症状：
  1. `SqliteUsersTestFixture` 仍以 Singleton 注册 `AppDbContext`，导致事务回滚、并发测试失真。
  2. `ExecuteSqlRaw` 继续使用 `@p0` 语法，SQLite 执行报 “Must add values for the following parameters”。
  3. RowVersion/并发控制未按文档处理，`ExecuteUpdateAsync` 报并发异常。
  4. 测试类依旧直接复用 Fixture.DbContext，数据污染严重。

## 目标
真正落地 SQLite 基础设施重构，使批量、事务、并发、原始 SQL 测试稳定通过；完成后更新总结文档反映真实状态。

## 工作项
1. **Fixture 生命周期重构**
   - `SqliteUsersTestFixture` 持有常驻 `SqliteConnection`，使用 `services.AddDbContext<AppDbContext>(options => options.UseSqlite(_connection))` 注册 Scoped DbContext。
   - 提供 `CreateScope()`/`CreateContext()` 帮助方法，测试获取独立 Scope；禁止共享 Fixture.DbContext。
   - 清理数据时创建新 Scope，避免共享 ChangeTracker。

2. **SQL 参数适配**
   - 修改 `ExecuteSql`：将 `?` 占位符替换为 `@p0/@p1...` 并构造 `SqliteParameter`；或直接使用 `Database.ExecuteSqlInterpolated`。
   - 检查仓储/测试中是否仍存在硬编码 `@p` 语法，统一封装。

3. **RowVersion 与并发策略**
   - 在 SQLite 配置中保留 `IsRowVersion()`，避免手工递增。
   - 对于需验证并发的用例，使用独立 Scope 并断言 `DbUpdateConcurrencyException`。

4. **测试改造**
   - `UserRepositorySqliteTests` 等测试类改为在构造函数里创建 Scope/DbContext/MemoryCache，`Dispose` 时释放。
   - 事务/并发/批量测试使用新的 Scope，确保互不影响。

5. **回归验证与文档**
   - `dotnet test tests/UnitTests/Modules/Users.UnitTests/LYBT.Module.Users.Tests.csproj -c Release --no-build --filter 'FullyQualifiedName~UserRepositorySqliteTests'`
   - 更新 `docs/tasks/completed/2025-09-24-users-SQLite深层问题剖析-summary.md`，说明实际改动与剩余限制。

## 验收标准
- SQLite 用例全部通过（允许事务回滚测试根据 SQLite 限制调整断言，但需记录原因）。
- Fixture 不再暴露共享 DbContext；代码评审确认生命周期正确。
- 原始 SQL/批量操作不会再抛参数或并发异常。
- 新的测试结果与说明同步写入总结文档。

## 风险提示
- Lifecycle 改动可能影响现有测试结构，需谨慎迁移，确保 InMemory 套件不受影响。
- SQLite 与 SQL Server 事务隔离差异需在总结中明确，避免后续误判。

---
文件：docs/tasks/pending/2025-09-24-users-SQLite基础设施落地任务.md