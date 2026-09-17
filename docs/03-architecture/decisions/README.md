# 架构决策记录 (ADR)

> **用户速览**：架构决策唯一索引。编号决策 + 命名 ADR 按时间排列。**权威目录**：本目录；规则见 [02-ssot-architecture.md](../../00-governance/02-ssot-architecture.md)。
>
> **编号说明**：`ADR-0016`、`ADR-0025` 预留跳号（无对应文件）。

## 编号 ADR

| 编号 | 标题 | 文件 |
|------|------|------|
| ADR-0001 | MedicalCase 作为唯一聚合根 | [0001-medicalcase-aggregate-root.md](0001-medicalcase-aggregate-root.md) |
| ADR-0002 | 双模式架构（远程 + 本地）— 历史决策，已被 ADR-0009 取代 | [0002-dual-mode-architecture.md](0002-dual-mode-architecture.md) |
| ADR-0003 | 集成优先测试策略 | [0003-integration-first-testing.md](0003-integration-first-testing.md) |
| ADR-0004 | 用户上下文传递模式 | [0004-user-context-propagation.md](0004-user-context-propagation.md) |
| ADR-0005 | SuperAdmin 归属 Auth 模块（双轨认证已废弃，见文内状态） | [0005-superadmin-auth-module.md](0005-superadmin-auth-module.md) |
| ADR-0006 | ViewModel 组件化分解模式 | [0006-component-decomposition-pattern.md](0006-component-decomposition-pattern.md) |
| ADR-0007 | ViewModel 组合模式 | [0007-viewmodel-composition-pattern.md](0007-viewmodel-composition-pattern.md) |
| ADR-0008 | Token 安全防御性设计 | [0008-token-security-defensive-design.md](0008-token-security-defensive-design.md) |
| ADR-0009 | URL 驱动双模式架构（Remote WebAPI + LocalWebAPI）— **当前**双模式权威 | [0009-url-driven-dual-mode.md](0009-url-driven-dual-mode.md) |
| ADR-0010 | LocalWebAPI 统一服务层 — 文档化跨层引用例外 | [0010-localwebapi-unified-service-layer.md](0010-localwebapi-unified-service-layer.md) |
| ADR-0011 | 从 AutoMapper 迁移到 Riok.Mapperly | [0011-mapperly-migration.md](0011-mapperly-migration.md) |
| ADR-0012 | 采用 CommunityToolkit.Mvvm 替代 Prism MVVM 基础设施 | [0012-communitytoolkit-mvvm-adoption.md](0012-communitytoolkit-mvvm-adoption.md) |
| ADR-0013 | SignalR 实时推送（v1.0 范围决策） | [0013-signalr-realtime-push.md](0013-signalr-realtime-push.md) |
| ADR-0014 | sysadmin 配置双模式 + 服务端 Configuration API + 延迟重启 | [0014-sysadmin-config-dual-mode.md](0014-sysadmin-config-dual-mode.md) |
| ADR-0015 | API 版本控制策略 | [0015-api-versioning-strategy.md](0015-api-versioning-strategy.md) |
| ADR-0016 | *（预留跳号，无文件）* | — |
| ADR-0017 | Modular Monolith with MediatR CQRS | [0017-modular-monolith-cqrs.md](0017-modular-monolith-cqrs.md) |
| ADR-0018 | Domain Events Pattern | [0018-domain-events-pattern.md](0018-domain-events-pattern.md) |
| ADR-0019 | 配置集中管理（Shared 类型集中 + config/ 文件集中 + 双端行为分层） | [0019-config-centralization.md](0019-config-centralization.md) |
| ADR-0020 | Desktop Service Layer Error Contract | [0020-service-error-contract.md](0020-service-error-contract.md) |
| ADR-0021 | SwitchingApiClient Client Lifecycle Management | [0021-switching-api-client-lifecycle.md](0021-switching-api-client-lifecycle.md) |
| ADR-0022 | Desktop JSON Serialization Unification | [0022-json-serialization-unification.md](0022-json-serialization-unification.md) |
| ADR-0023 | LocalWebAPI 架构测试白名单（P07 例外正名） | [0023-localwebapi-whitelist.md](0023-localwebapi-whitelist.md) |
| ADR-0024 | 远程/离线双 JWT 密钥隔离及聚合边界标注 | [0024-dual-jwt-isolation.md](0024-dual-jwt-isolation.md) |
| ADR-0025 | *（预留跳号，无文件）* | — |
| ADR-0026 | 授权矩阵 SSOT 收敛（产品层 04-permissions.md 为唯一权威） | [0026-authorization-matrix-ssot.md](0026-authorization-matrix-ssot.md) |
| ADR-0027 | 删除语义与批量归一（SoftDelete/Restore/HardDelete/BatchSoftDelete 等） | [0027-delete-semantics.md](0027-delete-semantics.md) |
| ADR-0028 | 桌面 HTTP 传输层池化与弹性（IHttpClientFactory + Polly） | [0028-desktop-http-resilience.md](0028-desktop-http-resilience.md) |
| ADR-0029 | 桌面 GET 响应缓存策略（进程内 + 传输层读写一体） | [0029-desktop-response-cache.md](0029-desktop-response-cache.md) |
| ADR-0030 | 跨聚合写入一致性策略（同上下文显式事务 + 跨上下文幂等/补偿） | [0030-cross-aggregate-write-consistency.md](0030-cross-aggregate-write-consistency.md) |

## 命名 ADR（无编号，按文件名）

| 标题 | 文件 |
|------|------|
| SharedHost 统一宿主抽象（方案 D） | [adr-shared-host-abstraction.md](adr-shared-host-abstraction.md) |
| Entity/DTO/Model 三层定义重构 | [adr-entity-dto-model-refactor.md](adr-entity-dto-model-refactor.md) |

---

*文档版本: v2.0 | 最后更新: 2026-09-17 | 可读性审查批次补全索引（编号 ADR 0001–0030 + 命名 ADR 2 份）*
