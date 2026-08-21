# 技术债看板

> SSOT：本文件为技术债唯一看板。需求/架构债以 `13-project-master-plan.md` §六为权威，此处仅索引。
> 更新：2026-08-20 | 版本：v1.0

## 看板

| ID | 事项 | 类型 | 优先级 | 状态 | 计划 |
|----|------|------|--------|------|------|
| TD-001 | 移除 `MediatR.Extensions.Microsoft.DependencyInjection` 归档包，改 `services.AddMediatR`（MediatR 12 自带） | 依赖 | P1 | ✅ 完成 | — |
| TD-002 | 移除 `StyleCop.Analyzers` 1.2.0-beta.556（长期 beta，规则由 EditorConfig/架构测试承接） | 依赖 | P1 | ✅ 完成 | — |
| TD-003 | Desktop 162 失败（存量环境：STA/WPF 线程 + SysAdmin 登录）— 需容器化/单测去环境 | 测试 | P1 | ⬜ 待办 | Phase2 |
| TD-004 | 部署仅 HTTP 明文（公网需 HTTPS，ADR-0014） | 安全 | P2 | ⬜ 待办 | 运维 |
| TD-005 | 项目数 29（含 Legacy 兼容）— 待方案 A 收敛后评估合并 | 结构 | P2 | ⬜ 待办 | 后续 |
| TD-006 | `AesGcmValueConverter.Encrypt` 写容错（异常返回原文）为历史明文迁移期软着陆，下版本收紧为抛异常（与 `Decrypt` 读严格 `CryptographicException→422` 对齐） | 安全 | P3 | ⬜ 待办 | 下版本 |

## 依赖升级策略

- 每月 `dotnet outdated` 扫描；安全补丁周内升级，功能升级随 Sprint 评估。
- 归档/长期 beta 包优先移除（本期 TD-001/TD-002）。

## 度量

- `dotnet build --no-incremental` 0 警告 0 错误常态化（Directory.Build.props `TreatWarningsAsErrors`）。
- 架构测试 87/87 为门禁，新增依赖须过 P07/P08/P10 守卫。
- 健康度：B+ (82) → A- (88)（Sprint1-2 P1 闭环后达成，Sprint6 T6.4 `phase2-migration-sequencing.md` 确认）。
  公式 `健康度 = 100 - 2*P1 - 1*P2`（仅 P0/P1 阻塞可发布），详见 `design-optimization-plan.md` 与 `phase2-migration-sequencing.md`。
