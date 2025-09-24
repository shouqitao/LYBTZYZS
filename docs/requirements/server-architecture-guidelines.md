# 服务端架构约束：当前阶段避免引入完整 CQRS + MediatR

## 背景
- 参考 `docs/architecture/ADR-001-cqrs-mediatr-rejection.md`（状态：已拒绝），系统规模与业务复杂度尚不足以承担完整 CQRS/MediatR 带来的工程成本。
- 现有查询层已具备 `QueryService + ReadRepository` 的读优化能力，写路径由 `BusinessService` 统筹，满足 Phase 2 之前的性能与稳定性指标。

## 现阶段要求
- 服务器端继续采用既有分层模式：Controller → Service → Repository，不得引入全量 Command/Query Handler 分拆或通用消息调度层。
- 读写职责仍以“读优化仓储 + 写服务”分离为度，禁止在未通过 Thinker 评审的情况下叠加 MediatR、后台消息总线等额外中间层。
- 新增业务功能需优先复用现有 QueryService/Repository 模板，并遵循 Phase 2 缓存治理成果（统一缓存键、空值穿透保护、诊断脚本接入）。

## 触发再评估的阈值（任一满足需复审 ADR-001）
- 活跃并发用户 ≥ 50，或 API 调用峰值 ≥ 200 RPS。
- 查询命中率连续两个迭代低于 70%，且经缓存/索引调整后仍无法恢复。
- 关键聚合根的读写耦合导致跨模块事务争用显著增加（例如锁等待>200ms，或冲突重试率>5%）。

## 与当前路线图的协同
- Phase 3 的缓存治理与可观测性是为未来可能的 CQRS 演进做前置准备，但**不等同于立即切换架构**。
- 若后续评审决定推进 CQRS/MediatR，需先更新该文档、ADR-001 以及任务清单，明确迁移范围、重构顺序与测试策略。

## 文档维护
- Thinker 负责在触发阈值或战略方向调整时更新本约束。
- Coder 在评审或提案中若建议引入 CQRS/MediatR，必须引用本文件并附带最新的数据依据。
