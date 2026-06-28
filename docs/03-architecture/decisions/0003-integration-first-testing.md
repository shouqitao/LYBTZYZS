# ADR-0003: 集成优先测试策略

**状态**: 已采纳
**日期**: 2026-02-08
**来源**: test-restructure-plan

## 背景

传统单元测试在三层架构中大量 mock，测试价值低。项目测试覆盖率需要提升，但资源有限，需要最大化测试投入回报。

## 决策

采用 Testing Trophy 架构 (2026-03-04 升级):

| 层级 | 项目 | 覆盖范围 | Tests |
|------|------|----------|-------|
| Server 全量 | LYBT.Tests.Server | 真实 HTTP + SQL Server + Respawn (零 mock) | ~1185 |
| Desktop 全量 | LYBT.Tests.Desktop | SQL Server LocalDB + 真实 Repository (最小 WPF mock) | ~760 |
| 架构防护 | LYBT.Tests.Architecture | 层依赖 + AntiMockRules | 76 |

> 测试数为近似值，以最新 `dotnet test` 输出为准；与 [`01-system-overview.md`](../01-system-overview.md) 测试章节保持一致。

### 约束

| 约束 | 值 | 理由 |
|------|-----|------|
| Server 测试数量下限 | ≥1100 | 防止测试退化 |
| Desktop 测试数量下限 | ≥700 | 防止测试退化 |
| Architecture 测试数量下限 | ≥70 | 防止架构约束松弛 |
| US 覆盖率 | 每个 US 至少 1 个集成测试 | 确保需求可追溯 |
| PR 回归门禁 | 新 PR 必须包含 ≥1 个对应 US 的测试 | 质量守门 |
| CI 必过项 | `dotnet build` + `dotnet test` 三项全部通过 | 合并前必须通过 |

### 关键原则
- Server 测试零 mock: 真实 SQL Server + Respawn 每测试重置 + 真实 JWT 登录
- Desktop 测试最小 mock: 仅限 WPF Runtime 边界接口 (IRegionManager 等)
- AntiMockRuleTests 架构测试强制 Server 项目不引用 NSubstitute
- 测试命名: `方法名_场景_期望结果`
- AAA 模式: Arrange-Act-Assert
- WPF Desktop 测试需要 net8.0-windows 目标框架，不与 Server 混合

### 演进历史
- 2026-02-08: 初始 "集成优先" 策略 (5 项目, EF InMemory + Mock)
- 2026-03-04: 升级为 Testing Trophy (3 项目, 真实 DB + Respawn, 零 Mock)

## 变更记录

| 日期 | 变更 |
|------|------|
| 2026-02-08 | 初始决策，5 个测试项目结构 |
| 2026-03-04 | 升级为 Testing Trophy: 5->3 项目, Server 零 mock, Respawn 隔离 |

## 关联 US

无直接关联（测试策略 ADR，影响 `LYBT.Tests.Server` / `LYBT.Tests.Desktop` / `LYBT.Tests.Architecture` 三个测试项目对全部 US 的验证方式，不绑定单一 US）。
