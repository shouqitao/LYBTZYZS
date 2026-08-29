# 技术债看板

> SSOT：本文件为技术债唯一看板。需求/架构债以 `13-project-master-plan.md` §六为权威，此处仅索引。
> 更新：2026-08-20 | 版本：v1.0

## 看板

| ID | 事项 | 类型 | 优先级 | 状态 | 计划 |
|----|------|------|--------|------|------|
| TD-001 | 移除 `MediatR.Extensions.Microsoft.DependencyInjection` 归档包，改 `services.AddMediatR`（MediatR 12 自带） | 依赖 | P1 | ✅ 完成 | — |
| TD-002 | 移除 `StyleCop.Analyzers` 1.2.0-beta.556（长期 beta，规则由 EditorConfig/架构测试承接） | 依赖 | P1 | ✅ 完成 | — |
| TD-003 | Desktop 162 失败 — `LocalWebApiControllerTestBase` 手工单模块缺 5 业务模块致 500（主因，`td003-architecture-review.md`）+ Local/Server 双轨漂移 — 方案 D `SharedHost` 统一宿主（业务模块 3→1、基类 2→1、Local 230→~120 行）| 测试 | P1 | ✅ 完成 (`SharedHost` + 基类合并) | 2026-08-21 |
| TD-004 | 部署仅 HTTP 明文（公网需 HTTPS，ADR-0014） | 安全 | P2 | ⬜ 待办 | 运维 |
| TD-005 | 项目数 29（含 Legacy 兼容）— 待方案 A 收敛后评估合并 | 结构 | P2 | ⬜ 待办 | 后续 |
|| TD-006 | `AesGcmValueConverter.Encrypt` 写容错（异常返回原文）为历史明文迁移期软着陆，下版本收紧为抛异常（与 `Decrypt` 读严格 `CryptographicException→422` 对齐） | 安全 | P3 | ⬜ 待办 | 下版本 ||
| TD-007 | `ReceptionistHomeViewModel`（ClinicalModule）依赖 `ICardReaderService`（CardReaderModule）和 `IPatientCardReaderIntegration`（PatientsModule），但 `ReceptionistRoleDefinition` 未直接声明这两个 OnDemand 模块。当前安全（ClinicalModule 的 `[ModuleDependency("CardReaderModule")]` 隐式触发加载），但隐式依赖链易在重构时断裂 | DI | P3 | ⬜ 待办 | 后续 |

## 依赖升级策略

- 每月 `dotnet outdated` 扫描；安全补丁周内升级，功能升级随 Sprint 评估。
- 归档/长期 beta 包优先移除（本期 TD-001/TD-002）。

## 度量

- `dotnet build --no-incremental` 0 警告 0 错误常态化（Directory.Build.props `TreatWarningsAsErrors`）。
- 架构测试 87/87 为门禁，新增依赖须过 P07/P08/P10 守卫。
- 健康度：B+ (82) → A- (88) → A (90+)（SharedHost 收敛后，`adr-shared-host-abstraction.md`，TD-003 500 清零）；
  `phase2-migration-sequencing.md` 依赖图已补 `SharedHost` 节点。
  公式 `健康度 = 100 - 2*P1 - 1*P2`（仅 P0/P1 阻塞可发布），详见 `design-optimization-plan.md` 与 `phase2-migration-sequencing.md`。
