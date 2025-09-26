# 2025-09-24 Users 模块 SQLite 深层问题剖析任务

## 问题背景
- `2025-09-24-users-SQLite兼容推广-summary.md` 指出 SQLite 套件仍有大量失败（事务回滚、并发、批量更新、原始 SQL 等），影响评估结果。
- 经代码/测试执行复盘，发现以下核心原因：
  1. `SqliteUsersTestFixture` 将 `AppDbContext` 注册为 Singleton，所有测试共用一个上下文 → 事务回滚后仍能查询到数据、并发测试出现多实体跟踪冲突。
  2. 原始 SQL 调用依然使用 `@p0` 参数占位符 → SQLite 解析失败，提示必须提供参数。
  3. RowVersion 在 SQLite 中被配置为 `ValueGeneratedNever` 并由测试手动递增，导致 `ExecuteUpdateAsync` 实际更新行数为 0，引发并发异常。
  4. Fixture 清理逻辑仍使用共享 DbContext，测试间数据污染仍未完全消除。

## 分析结论
- 若不彻底重构 Fixture 生命周期并适配 RowVersion/SQL 语法，SQLite 套件无法稳定运行；当前的失败不是单个断言问题，而是基础设施/配置层面的系统性缺陷。
- 对于混合测试策略而言，必须先解决“连接常驻、上下文按测试创建、RowVersion 与 SQLite 行为一致”的基础问题，再考虑推广。

## 工作方向
1. **生命周期重构**
   - Fixture 持有常驻 `SqliteConnection`，但 `AppDbContext` 通过 `AddDbContext<AppDbContext>(options => options.UseSqlite(_connection))` 注册为 Scoped。
   - 每个测试通过 `CreateScope()` 获取新的 DbContext、MemoryCache、ServiceResult 等依赖，用完立即释放。
   - 清理数据时创建独立 Scope，避免共享上下文。

2. **RowVersion 与并发策略调整**
   - 取消测试中对 RowVersion 的手工递增，改为使用 EF Core 的并发控制配置（`IsRowVersion`）。
   - 如需模拟行版本，可在 SQLite 场景下使用触发器或在回滚测试中放宽断言（验证 SaveChanges 抛出并发异常，而非行数统计）。

3. **原始 SQL 适配**
   - 将 `ExecuteSqlRaw` 改为 `ExecuteSqlInterpolated`，或在调用处手动构造 `DbParameter`，确保 SQLite 能正确绑定参数。
   - 对于批量更新操作，优先使用 `ExecuteUpdateAsync`，避免手写 SQL。

4. **测试隔离与覆盖**
   - 对已有 SQLite 测试类进行 Scope 重构，确保不直接引用 Fixture.DbContext。
   - 扩充测试覆盖，验证事务提交/回滚、副作用、缓存清理等场景在新架构下符合预期。

5. **分层策略落地**
   - 完成上述基础修复后，重新评估混合策略（InMemory vs SQLite）——只在关键路径启用 SQLite 项目，保持执行效率。

## 验收标准
- SQLite 套件核心测试（事务、批量更新、原始 SQL、并发）全部通过。
- Fixture 重构后无 Singleton DbContext，测试间状态互不影响。
- RowVersion 引发的并发异常消除或在测试中明确断言期望行为。
- 新的工作总结写入 `docs/tasks/completed/2025-09-24-users-SQLite深层问题剖析-summary.md`。

## 提醒
- 重构 Fixture 前需评估对已有测试的影响，必要时分阶段迁移。
- 若短期内难以稳定 SQLite，可暂停推广，将重构后的结果先记录为暂存方案。
- 任务完成后请更新 README/测试策略文档，说明两种 Provider 的适用场景。

---
文件：docs/tasks/pending/2025-09-24-users-SQLite深层问题剖析任务.md