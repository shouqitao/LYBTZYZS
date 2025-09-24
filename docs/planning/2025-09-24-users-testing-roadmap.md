# 2025-09-24 Users 测试体系阶段规划

## 短期（立即执行）
- 修复剩余 65 个失败用例，先聚焦业务逻辑类测试（ServiceResult 契约、批量操作、密码策略）。
- 参照 `docs/tasks/pending/2025-09-24-users-模块单元测试核心修复任务.md`，逐项落实缓存兼容性、用户创建持久化、批量规则与测试基建增强。

## 中期（3-5 天）
- 评估并引入 SQLite In-Memory 替代 EF Core InMemory Provider，解决 ExecuteUpdateAsync 等行为差异。
- 更新测试 Fixture，使所有仓储/业务测试统一使用 SQLite In-Memory，并调整断言适应该环境的事务与并发特性。
- 梳理受影响的测试脚本与运行文档，在 README/测试手册中同步说明环境要求。

## 长期（1 周+）
- 建立完整的测试基础设施：
  - 通用 Fixture（DbContext、Cache、AutoMapper、配置）。
  - Builder Pattern（UserBuilder/ServiceResultBuilder 等），减少重复初始化。
  - 自定义断言工具集，统一校验 ServiceResult、缓存命中、审计字段等。
- 引入测试分层策略（单元/集成/端到端）和覆盖率监控，将 Users 模块覆盖率提升至 80%+。
- 持续完善 `docs/development/server-testing-architecture-completion-report.md`，记录每个阶段的改造成果与治理规则。

---
文件：docs/planning/2025-09-24-users-testing-roadmap.md