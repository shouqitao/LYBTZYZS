# 2025-09-24 Users 模块 SQLite 基础设施稳定化任务

## 背景问题
- `2025-09-24-users-SQLite兼容推广-summary.md` 显示虽然解决了 RowVersion 约束，但 SQLite 测试仍有 31 个失败，集中在事务、并发及原始 SQL 场景。
- 根因主要有：
  1. `SqliteUsersTestFixture` 将 `AppDbContext` 注册为 Singleton，多个测试共用 ChangeTracker，导致事务与并发行为失真。
  2. 仓储/测试内的 SQL 语句仍按 SQL Server 语法使用 `@p0` 参数，与 SQLite 的 `?` 占位符不兼容。
  3. 清理与数据隔离策略不足，导致测试间状态相互污染。
- 必须稳定 SQLite 基础设施，才能继续推广混合测试策略。

## 工作目标
1. 将 SQLite Fixture 调整为“连接常驻、DbContext 每测试重建”的模式，确保事务、并发、跟踪行为与生产一致。
2. 修正原始 SQL 的参数化语法与执行方式，避免 SQLite 报错。
3. 建立可靠的测试隔离与清理机制，杜绝跨测试污染。

## 工作项
1. **DbContext 生命周期重构**
   - 在 Fixture 中仅持有打开的 `SqliteConnection`，使用 `AddDbContext<AppDbContext>(options => options.UseSqlite(_connection))`，默认 Scoped 生命周期。
   - 提供 `CreateContext()`/`CreateScope()` 方法为每个测试显式获取新 DbContext，并在测试结束后释放。
   - 若需共享事务，由测试显式创建 `IDbContextTransaction`，禁止复用同一 DbContext。

2. **SQL 语法适配**
   - 审核仓储/测试中使用的 `ExecuteSqlRaw`/`ExecuteSqlInterpolated`，统一改为 `?` 占位符或使用 `FormattableString`，确保 SQLite 与 SQL Server 均可执行。
   - 为批量更新/删除方法增加 provider 判断或使用 EF Core 方法链（`ExecuteUpdateAsync`/`ExecuteDeleteAsync`）替代手写 SQL。

3. **测试隔离与清理**
   - 调整 Fixture 提供的 `ClearData`，避免使用共享 DbContext；改为开启新作用域进行清理。
   - 在需要的测试类中实现 `IAsyncLifetime` 或 `Dispose`，确保用完即清理。
   - 检查 MemoryCache、DefaultPasswordService 等依赖是否需要按测试刷新。

4. **回归验证**
   - 重新运行 `dotnet test tests/UnitTests/Modules/Users.UnitTests/LYBT.Module.Users.Tests.csproj -c Release --no-build`，观察 SQLite 套件是否稳定。
   - 对比 InMemory 与 SQLite 结果，将差异记录在总结中。

## 验收标准
- SQLite 套件（批量操作、事务、并发、原始 SQL 测试）全部通过，不再出现 DbContext 复用或参数化错误。
- Fixture 代码经代码审查确认生命周期配置正确，无资源泄漏。
- 在 `docs/tasks/completed/2025-09-24-users-SQLite基础设施稳定化-summary.md` 输出总结，附最新测试日志与剩余风险。

## 风险提示
- 修改 DbContext 生命周期后，现有依赖注入代码需同步更新（如测试中直接从 Fixture 读取 DbContext 的地方）。
- SQLite 与 SQL Server 并发行为仍有差异，必要时添加条件编译或 Provider 检测。
- 注意保持连接在整个测试进程存活，避免因误释放导致内存数据库丢失。

---
文件：docs/tasks/pending/2025-09-24-users-SQLite基础设施稳定化任务.md