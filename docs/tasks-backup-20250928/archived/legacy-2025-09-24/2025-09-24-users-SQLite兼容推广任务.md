# 2025-09-24 Users 模块 SQLite 兼容性修复与推广任务

## 任务背景
- SQLite In-Memory 评估已完成，确认其在批量操作、事务、并发控制方面优于 EF Core InMemory Provider。
- 目前阻塞点：测试数据未初始化 RowVersion 导致 NOT NULL 约束错误，影响 SQLite 场景下的仓储与业务测试。
- 目标：修复 SQLite 兼容问题，推广关键用例迁移，并建立混合测试策略的基础设施。

## 工作项
1. **RowVersion 初始化修复**
   - 更新所有测试数据构造逻辑（含 UserBuilder、手工 new User 的位置），统一设置默认 RowVersion：`new byte[] {0,0,0,0,0,0,0,1}`。
   - 校验 `UserRepository`/`UserBusinessService` 相关测试在 SQLite 环境下能通过。

2. **SQLite Fixture 推广**
   - 将已有的 `SqliteUsersTestFixture` 抽象为可复用组件，供仓储和业务测试引用。
   - 替换批量操作、事务、并发相关测试使用 SQLite Fixture（至少覆盖批量启用/禁用、ExecuteUpdateAsync、事务回滚场景）。

3. **混合测试策略落地**
   - 在测试项目中标注不同 Provider 使用场景（注释/README 或测试分类）。
   - 为 CI 配置预备脚本：默认运行 InMemory 套件，可选开关运行 SQLite 套件（若影响较大可暂记录计划）。

## 验收标准
- `dotnet test tests/UnitTests/Modules/Users.UnitTests/LYBT.Module.Users.Tests.csproj -c Release --no-build` 在启用 SQLite Fixture 的用例上不再出现 RowVersion 约束错误。
- 批量操作相关测试在 SQLite 环境下通过，执行结果记录在测试日志中。
- `docs/tasks/completed/2025-09-24-users-SQLite兼容推广-summary.md` 完成总结，说明迁移范围与剩余计划。

## 风险提示
- 批量迁移测试前需确保 Fixture 生命周期管理正确，避免连接关闭导致数据库丢失。
- 若业务代码对 RowVersion 有自动赋值逻辑，需确保测试写入与生产保持一致。
- CI 调整前先在本地脚本验证执行耗时与资源占用。

---
文件：docs/tasks/pending/2025-09-24-users-SQLite兼容推广任务.md